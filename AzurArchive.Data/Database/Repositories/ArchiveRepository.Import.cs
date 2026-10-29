using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using Blake3;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZstdNet;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private static void ImportFileContents(
        string saveDirectory, 
        SQLiteWriteConnection writer, 
        string dbName, 
        List<ValueTuple<EntryEntity, string>> fileDtos, 
        int nWorkers, 
        IProgress<ArchiveProgress> progress, 
        CancellationToken token) {
        var now = DateTime.UtcNow;
        var chunkWriter = new ChunkContentWriter(saveDirectory);
        string dbPath = Path.Join(saveDirectory, dbName);
        // Per-thread resources
        Memory<byte>[] compressBuffers = new Memory<byte>[nWorkers];
        Memory<byte>[] chunkBuffers = new Memory<byte>[nWorkers];
        Memory<byte>[] fileHashesBuffers = new Memory<byte>[nWorkers];
        SQLiteReadConnection[] readers = new SQLiteReadConnection[nWorkers];
        Compressor[] compressors = new Compressor[nWorkers];
        for (int i = 0; i < nWorkers; i++) {
            compressBuffers[i] = new byte[Config.MaxChunkSize];
            chunkBuffers[i] = new byte[Config.ChunkBufferSize];
            fileHashesBuffers[i] = new byte[Config.FileContentBufferInitialSize];
            compressors[i] = new Compressor(new CompressionOptions(CompressionLevel));
            readers[i] = new SQLiteReadConnection(dbPath);
        }
        // Locks
        object chunkLock = new();
        object relationLock = new();
        object writeLock = new();
        object hashLock = new();
        // Only one writer is allowed
        void InsertChunk(Hash256 hash, Memory<byte> content, int originalSize, int compressionLevel) {
            ChunkEntity entity = new((hash, 1, originalSize, content.Length, compressionLevel));
            lock (writeLock) {
                writer.Insert([entity], false);
            }
            lock (chunkLock) {
                chunkWriter.InserChunk(hash, content);
            }
        }
        // Shared resources
        // Need ~30M item to reach 1GB
        Dictionary<Hash256, int> hashIncrease = ChunkRepository.ConstructHashTable(writer);
        int fileCount = fileDtos.Count;
        void Job(int tIndex, int fileIndex, EntryEntity file, string absPath, CancellationToken token) {
            progress.Report(new(fileIndex + 1, fileCount, $"Adding: {file.Name}", false));
            // Get resource
            var compressBuffer = compressBuffers[tIndex];
            var chunkBuffer = chunkBuffers[tIndex];
            var compressor = compressors[tIndex];
            var fileHashesBuffer = fileHashesBuffers[tIndex];
            var reader = readers[tIndex];
            int offset = 0;
            //int length = 0;
            using (var stream = File.OpenRead(absPath)) {
                foreach (var chunk in FastCDC.FastCDC.SplitToChunks(stream, chunkBuffer, Config.MinChunkSize, Config.MidChunkSize, Config.MaxChunkSize, CDCAlgorithm)) {
                    token.ThrowIfCancellationRequested();
                    // TODO: Reuse from content buffer, check MusicEco for impl
                    Hash256 hash = new();
                    Hasher.Hash(chunk.Span, hash.AsSpan());
                    bool exists = false;
                    // Handle hash check
                    lock(hashLock) {
                        if (hashIncrease.TryGetValue(hash, out var increase)) {
                            exists = true;
                            hashIncrease[hash] = increase + 1;
                        } else {
                            exists = false;
                            hashIncrease[hash] = 1;
                        }
                    }
                    // Add if not exists
                    if (!exists) {
                        int compressedLength = compressor.Wrap(chunk.Span, compressBuffer.Span);
                        var compressed = compressBuffer[..compressedLength];
                        if (compressed.Length < chunk.Length) {
                            InsertChunk(hash, compressed, chunk.Length, CompressionLevel);
                        }
                        else {
                            InsertChunk(hash, chunk, chunk.Length, 0);
                        }
                    }
                    // Buffer overflow, reallocate
                    if (offset + 32 > fileHashesBuffer.Length) {
                        var newLength = fileHashesBuffer.Length * 2;
                        var newBuffer = new byte[newLength];
                        fileHashesBuffers[tIndex] = newBuffer;
                        fileHashesBuffer.CopyTo(newBuffer);
                        // fileContentBuffer can be released now
                    }
                    var end = offset + 32;
                    var dstSpan = fileHashesBuffer.Span[offset..end];
                    offset = end;
                    hash.AsReadOnlySpan().CopyTo(dstSpan);
                }
                var fileContent = fileHashesBuffer[..offset];
                lock(writeLock) {
                    writer.Execute("""
                        INSERT INTO EntryContentEntity (Id, Content)
                        VALUES (?, ?)
                        """, file.Id!.Value, fileContent);
                }
            }
        }
        chunkWriter.BeginTransaction();
        // Schedule job
        try {
            int nextFile = 0;
            object lockObj = new();
            Task[] workers = new Task[nWorkers];
            for (int tIndex = 0; tIndex < nWorkers; tIndex++) {
                token.ThrowIfCancellationRequested();
                int workerIndex = tIndex;
                workers[tIndex] = Task.Run(() => {
                    while (true) {
                        token.ThrowIfCancellationRequested();
                        int fileIndex;
                        // Grab next job when available
                        lock (lockObj) {
                            if (nextFile >= fileDtos.Count) {
                                return;
                            }
                            fileIndex = nextFile++;
                        }
                        var (file, path) = fileDtos[fileIndex];
                        Job(workerIndex, fileIndex, file, path, token);
                    }
                });
            }
            Task.WaitAll(workers);
            progress.Report(new(fileCount, fileCount, "Insert relation", false));
            // Increase count
            foreach (var (hash, increase) in hashIncrease) {
                if (increase > 0) {
                    writer.Update("UPDATE ChunkEntity SET ReferenceCount = ReferenceCount + ? WHERE Hash = ?", increase, hash);
                }
            }
            progress.Report(new(fileCount, fileCount, "Comitting transaction", false));
            chunkWriter.CommitTransaction();
        }
        catch {
            chunkWriter.RollbackTransaction();
            throw;
        }
        finally {
            for (int i = 0; i < nWorkers; i++) {
                readers[i].Dispose(false);
                compressors[i].Dispose();
            }
            chunkWriter.Dispose();
        }
    }
    public EntryEntity? ImportEntry(long parentId, string rootPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            var rootName = Path.GetFileName(rootPath);
            if (parentId == -1 
                || (EntryRepository.CheckExists(writer, parentId)
                    && EntryRepository.Contain(writer, parentId, rootName)
                    )
                ) {
                var (rootEntry, fileDtos) = EntryRepository.InsertRecursive(writer, parentId, rootPath);
                if (fileDtos.Count > 0) {
                    ImportFileContents(this._saveFolder, writer, Config.DatabaseName, fileDtos, nWorkers, progress, token);
                }
                writer.CommitTransaction();
                progress.Report(new(-1, -1, "Completed", true));
                return rootEntry;
            }
            else {
                writer.RollbackTransaction();
                progress.Report(new(-1, -1, "Failed", true));
                return null;
            }
        }
        catch (Exception e) {
            Debug.WriteLine(e);
            writer.RollbackTransaction();
            progress.Report(new(-1, -1, "Failed", true));
            return null;
        }
        finally {
            writer.Dispose(false);
        }
    }
}