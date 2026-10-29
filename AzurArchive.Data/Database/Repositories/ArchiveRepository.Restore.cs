using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using SQLiteORM;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZstdNet;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    // Return list of (File entry, Output folder path)
    private static List<ValueTuple<EntryEntity, string>> RestoreFolderAndScanFile(SQLiteReadConnection connection, long rootId, string rootFolderPath) {
        List<ValueTuple<EntryEntity, string>> result = [];
        // Id, Parent folder path
        Queue<ValueTuple<long, string>> q = [];
        q.Enqueue((rootId, rootFolderPath));
        while(q.Count > 0) {
            var (entryId, folderPath) = q.Dequeue();
            var children = EntryRepository.GetChildren(connection, entryId);
            foreach(var child in children) {
                if (child.IsFolder()) {
                    var childFolderPath = Path.Join(folderPath, child.Name);
                    q.Enqueue((child.Id!.Value, childFolderPath));
                    if (!Directory.Exists(childFolderPath)) {
                        Directory.CreateDirectory(childFolderPath);
                    }
                }
                else {
                    result.Add((child, folderPath));
                }
            }
        }
        return result;
    }
    private static void RestoreFiles(string saveDirectory, List<ValueTuple<EntryEntity, string>> fileDtos, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        int fileCount = fileDtos.Count;
        string dbPath = Path.Join(saveDirectory, Config.DatabaseName);
        // Per-thread resources
        SQLiteReadConnection[] readers = new SQLiteReadConnection[nWorkers];
        ChunkContentReader[] chunkReaders = new ChunkContentReader[nWorkers];
        Memory<byte>[] compressBuffers = new Memory<byte>[nWorkers];
        Memory<byte>[] fileHashesBuffers = new Memory<byte>[nWorkers];
        Decompressor[] decompressors = new Decompressor[nWorkers];
        for (int i = 0; i < nWorkers; i++) {
            readers[i] = new(dbPath);
            chunkReaders[i] = new ChunkContentReader(saveDirectory);
            compressBuffers[i] = new byte[Config.MaxChunkSize];
            fileHashesBuffers[i] = new byte[Config.FileContentBufferInitialSize];
            decompressors[i] = new();
        }
        void Job(int tIndex, int fileIndex, EntryEntity fileEntry, string outputFolder, CancellationToken token) {
            progress.Report(new(fileIndex + 1, fileCount, $"Extracting: {Path.Join(outputFolder, fileEntry.Name)}", false));
            token.ThrowIfCancellationRequested();
            var reader = readers[tIndex];
            var chunkReader = chunkReaders[tIndex];
            var compressBuffer = compressBuffers[tIndex].Span;
            var decompressor = decompressors[tIndex];
            var fileHashesBuffer = fileHashesBuffers[tIndex].Span;
            foreach(var stmt in reader.Iterate("""
                SELECT Content FROM EntryContentEntity
                WHERE Id = ?
                """, fileEntry.Id!.Value)) {
                var span = raw.sqlite3_column_blob(stmt, 0);
                if (span.Length > fileHashesBuffer.Length) {
                    var newLength = fileHashesBuffer.Length * 2;
                    var newBuffer = new byte[newLength];
                    fileHashesBuffers[tIndex] = newBuffer;
                    fileHashesBuffer = newBuffer;
                }
                span.CopyTo(fileHashesBuffer);
                fileHashesBuffer = fileHashesBuffer[..span.Length];
                break;
            }
            using (var stream = File.OpenWrite(Path.Join(outputFolder, fileEntry.Name))) {
                for (int offset = 0; offset < fileHashesBuffer.Length; offset += 32) {
                    token.ThrowIfCancellationRequested();
                    Hash256 hash = new();
                    fileHashesBuffer.Slice(offset, 32).CopyTo(hash.AsSpan());
                    var compressionLevel = reader.Select<int>("""
                        SELECT CompressionLevel FROM ChunkEntity
                        WHERE Hash = ?
                        """, hash).First().Item1;
                    var chunkConnection = chunkReader.GetConnection(hash);
                    foreach (var stmt in chunkConnection.Iterate("""
                        SELECT Content FROM ChunkContent
                        WHERE Hash = ?
                        """, hash)) {
                        var content = raw.sqlite3_column_blob(stmt, 0);
                        if (compressionLevel > 0) {
                            int decompressedSize = decompressor.Unwrap(content, compressBuffer, false);
                            var decompressed = compressBuffer[..decompressedSize];
                            stream.Write(decompressed);
                        }
                        else {
                            stream.Write(content);
                        }
                        break;
                    }
                }
            }
        }
        // Schedule
        try {
            // Schedule job
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
                            if (nextFile >= fileCount) {
                                return;
                            }
                            fileIndex = nextFile++;
                        }
                        var (file, folder) = fileDtos[fileIndex];
                        Job(workerIndex, fileIndex, file, folder, token);
                    }
                });
            }
            Task.WaitAll(workers);
        }
        catch {
            throw;
        }
        finally {
            for (int i = 0; i < nWorkers; i++) {
                readers[i].Dispose(false);
                chunkReaders[i].Dispose();
                decompressors[i].Dispose();
            }
        }
    }
    public bool RestoreEntry(long entryId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteReadConnection reader = new(dbPath);
        try {
            var fileDtos = RestoreFolderAndScanFile(reader, entryId, outputFolderPath);
            RestoreFiles(this._saveFolder, fileDtos, nWorkers, progress, token);
            progress.Report(new(-1, -1, "Completed", true));
            return true;
        }
        catch (Exception e){
            Debug.WriteLine(e);
            progress.Report(new(-1, -1, "Failed", false));
            return false;
        }
        finally {
            reader.Dispose(false);
        }
    }
}
