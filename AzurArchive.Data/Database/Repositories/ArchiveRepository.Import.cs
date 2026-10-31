using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using Blake3;
using Microsoft.VisualBasic;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
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
        var insertSession = chunkWriter.CreateInsertSession();
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
        // Shared resources
        // Need ~25M item to reach 1GB (each dict entry need 44 bytes: 4 hash code, 4 next, 32 key, 4 value
        Dictionary<Hash256, int> hashIncrease = ChunkRepository.ConstructHashTable(writer);
        int fileCount = fileDtos.Count;
        List<ChunkEntity> chunkBatch = new(1024);
        // Report
        object reportLock = new();
        Stopwatch throttleSw = Stopwatch.StartNew();
        TimeSpan lastReport = TimeSpan.Zero;
        int completedCount = 0;
        void Job(int tIndex, int fileIndex, EntryEntity file, string absPath, CancellationToken token) {
            // Get resource
            var compressBuffer = compressBuffers[tIndex].Span;
            var chunkBuffer = chunkBuffers[tIndex];
            var compressor = compressors[tIndex];
            var fileHashesBuffer = fileHashesBuffers[tIndex];
            var reader = readers[tIndex];
            int offset = 0;
            //int length = 0;
            using (var stream = File.OpenRead(absPath)) {
                foreach (var chunk in FastCDC.FastCDC.SplitToChunks(stream, chunkBuffer, Config.MinChunkSize, Config.MidChunkSize, Config.MaxChunkSize, CDCAlgorithm)) {
                    token.ThrowIfCancellationRequested();
                    // Buffer overflow, reallocate
                    if (offset + 32 > fileHashesBuffer.Length) {
                        var newLength = fileHashesBuffer.Length * 2;
                        var newBuffer = new byte[newLength];
                        fileHashesBuffers[tIndex] = newBuffer;
                        fileHashesBuffer.CopyTo(newBuffer);
                        // fileContentBuffer can be released now
                    }
                    var dstSpan = fileHashesBuffer.Span.Slice(offset, 32);
                    Hasher.Hash(chunk.Span, dstSpan);
                    Hash256 hash = new(dstSpan);
                    offset += 32;
                    bool exists = false;
                    // Handle hash check
                    lock(hashLock) {
                        ref int increase = ref CollectionsMarshal.GetValueRefOrAddDefault(
                            hashIncrease, hash, out exists);
                        increase++;
                    }
                    // Add if not exists
                    if (!exists) {
                        int compressedLength = compressor.Wrap(chunk.Span, compressBuffer);
                        ReadOnlySpan<byte> content = compressBuffer[..compressedLength];
                        int compressionLevel = CompressionLevel;
                        if (content.Length >= chunk.Length) {
                            content = chunk.Span;
                            compressionLevel = 0;
                        }
                        // Will become 1 with hashIncrease
                        ChunkEntity entity = new((hash, 0, chunk.Length, content.Length, compressionLevel));
                        lock (writeLock) {
                            if (chunkBatch.Count >= chunkBatch.Capacity) {
                                writer.Insert(chunkBatch, false);
                                chunkBatch.Clear();
                            }
                            chunkBatch.Add(entity);
                        }
                        lock (chunkLock) {
                            insertSession.Insert(hash, content);
                        }
                    }
                }
                var fileContent = fileHashesBuffer[..offset];
                lock(writeLock) {
                    writer.Execute("""
                        INSERT INTO EntryContentEntity (Id, Content)
                        VALUES (?, ?)
                        """, file.Id!.Value, fileContent);
                }
                lock(reportLock) {
                    TimeSpan elapsed = throttleSw.Elapsed;
                    completedCount++;
                    bool shouldReport = completedCount == 1 || completedCount == fileCount || elapsed - lastReport > Config.UpdateInterval;
                    if (shouldReport) {
                        lastReport = elapsed;
                        progress.Report(new(completedCount + 1, fileCount, $"Add: {file.Name}", false));
                    }
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
            // Flush batch
            if (chunkBatch.Count > 0) {
                writer.Insert(chunkBatch, false);
                chunkBatch.Clear();
            }
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
            insertSession.Dispose();
            chunkWriter.Dispose();
        }
    }
    public EntryEntity? ImportEntry(long parentId, List<string> rootPaths, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            // TODO:Check for inclusive paths
            List<ValueTuple<EntryEntity, string>> totalFileDtos = [];
            EntryEntity? rootEntry = null;
            foreach(var rootPath in rootPaths) {
                token.ThrowIfCancellationRequested();
                var rootName = Path.GetFileName(rootPath);
                if (parentId == -1
                    || (EntryRepository.CheckExists(writer, parentId)
                        && EntryRepository.Contain(writer, parentId, rootName)
                        )
                    ) {
                    (rootEntry, var fileDtos) = EntryRepository.InsertRecursive(writer, parentId, rootPath);
                    totalFileDtos.AddRange(fileDtos);
                }
                else {
                    writer.RollbackTransaction();
                    progress.Report(new(-1, -1, "Failed", true));
                    return null;
                }
            }
            if (totalFileDtos.Count > 0) {
                ImportFileContents(this._saveFolder, writer, Config.DatabaseName, totalFileDtos, nWorkers, progress, token);
            }
            writer.CommitTransaction();
            progress.Report(new(-1, -1, "Completed", true));
            return rootEntry;
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