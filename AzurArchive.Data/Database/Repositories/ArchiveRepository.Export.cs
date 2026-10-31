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
    private static ValueTuple<List<ValueTuple<EntryEntity, string>>, List<ValueTuple<string, DateTime, DateTime, DateTime>>> ExportFolderAndScanFile(SQLiteReadConnection connection, long rootId, string rootFolderPath) {
        List<ValueTuple<EntryEntity, string>> result = [];
        // Id, Parent folder path
        Queue<ValueTuple<EntryEntity, string>> q = [];
        List<ValueTuple<string, DateTime, DateTime, DateTime>> timeStack = [];
        var root = EntryRepository.GetEntry(connection, rootId)!;
        q.Enqueue((root, Path.Join(rootFolderPath, root.Name)));
        while(q.Count > 0) {
            var (entry, folderPath) = q.Dequeue();
            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
                timeStack.Add((folderPath, entry.CreationTime, entry.ModifiedTime, entry.AccessTime));
            }
            var children = EntryRepository.GetChildren(connection, entry.Id!.Value);
            foreach(var child in children) {
                if (child.IsFolder()) {
                    var childFolderPath = Path.Join(folderPath, child.Name);
                    q.Enqueue((child, childFolderPath));
                }
                else {
                    result.Add((child, folderPath));
                }
            }
        }
        return (result, timeStack);
    }
    private static void ExportFiles(string saveDirectory, List<ValueTuple<EntryEntity, string>> fileDtos, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
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
        object reportLock = new();
        Stopwatch throttleSw = Stopwatch.StartNew();
        TimeSpan lastReport = TimeSpan.Zero;
        int completedCount = 0;
        void Job(int tIndex, int fileIndex, EntryEntity file, string outputFolder, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            var reader = readers[tIndex];
            var chunkReader = chunkReaders[tIndex];
            var compressBuffer = compressBuffers[tIndex].Span;
            var decompressor = decompressors[tIndex];
            var fileHashesBuffer = fileHashesBuffers[tIndex].Span;
            foreach(var stmt in reader.IterateSelect("""
                SELECT Content FROM EntryContentEntity
                WHERE Id = ?
                """, file.Id!.Value)) {
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
            var path = Path.Join(outputFolder, file.Name);
            using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
                for (int offset = 0; offset < fileHashesBuffer.Length; offset += 32) {
                    token.ThrowIfCancellationRequested();
                    Hash256 hash = new(fileHashesBuffer.Slice(offset, 32));
                    var compressionLevel = reader.Select<int>("""
                        SELECT CompressionLevel FROM ChunkEntity
                        WHERE Hash = ?
                        """, hash).First().Item1;
                    var chunkConnection = chunkReader.GetConnection(hash);
                    foreach (var stmt in chunkConnection.IterateSelect("""
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
                File.SetCreationTimeUtc(stream.SafeFileHandle, file.CreationTime);
                File.SetLastWriteTimeUtc(stream.SafeFileHandle, file.ModifiedTime);
                File.SetLastAccessTimeUtc(stream.SafeFileHandle, file.AccessTime);
            }
            lock(reportLock) {
                TimeSpan elapsed = throttleSw.Elapsed;
                completedCount++;
                bool shouldReport = completedCount == 1 || completedCount == fileCount || elapsed - lastReport > Config.UpdateInterval;
                if (shouldReport) {
                    lastReport = elapsed;
                    progress.Report(new(completedCount + 1, fileCount, $"Export: {file.Name}", false));
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
    public bool ExportEntry(List<long> entryIds, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteReadConnection reader = new(dbPath);
        try {
            // TODO:Check for inclusive paths
            List<ValueTuple<EntryEntity, string>> totalFileDtos = [];
            List<ValueTuple<string, DateTime, DateTime, DateTime>> totalTimeStack = [];
            foreach (var entryId in entryIds) {
                var (fileDtos, timeStack) = ExportFolderAndScanFile(reader, entryId, outputFolderPath);
                totalFileDtos.AddRange(fileDtos);
                totalTimeStack.AddRange(timeStack);
            }
            ExportFiles(this._saveFolder, totalFileDtos, nWorkers, progress, token);
            for(int i=totalTimeStack.Count-1;i>=0; i--) {
                var (path, creation, modified, access) = totalTimeStack[i];
                var dir = new DirectoryInfo(path) {
                    CreationTimeUtc = creation,
                    LastWriteTimeUtc = modified,
                    LastAccessTimeUtc = access
                };
            }
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
