using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZstdNet;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private static List<FileToArchive> GetFiles(SQLiteReadConnection connection, long rootFolderId, string outputFolderPath) {
        // Handle first folder
        Queue<(long Id, string Path)> folderQ = [];
        List<FileToArchive> result = [];
        var firstFolderRows = connection.Select<string>($"""
                SELECT Name FROM FolderEntity
                WHERE Id = ?
                """, rootFolderId).ToList();
        if (firstFolderRows.Count > 0) {
            string firstFolderPath = Path.Join(outputFolderPath, firstFolderRows[0].Item1);
            Directory.CreateDirectory(firstFolderPath);
            folderQ.Enqueue((rootFolderId, firstFolderPath));
        }
        else {
            throw new KeyNotFoundException();
        }
        // Handle Folder and File scan
        while (folderQ.Count > 0) {
            var (folderId, folderPath) = folderQ.Dequeue();
            var folderRows = connection.Select<long, string>($"""
                    SELECT Id, Name FROM FolderEntity
                    WHERE ParentId = ?
                    """, folderId);
            foreach (var row in folderRows) {
                string subFolderPath = Path.Combine(folderPath, row.Item2);
                folderQ.Enqueue((row.Item1, subFolderPath));
                Directory.CreateDirectory(subFolderPath);
            }
            var fileRows = connection.Select<long, string>($"""
                    SELECT Id, Name FROM FileEntity
                    WHERE FolderId = ?
                    """, folderId);
            foreach (var row in fileRows) {
                string filePath = Path.Combine(folderPath, row.Item2);
                result.Add(new(row.Item1, row.Item2, filePath));
            }

        }
        return result;
    }
    private static void RestoreFiles(List<FileToArchive> files, string saveDirectory, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        int fileCount = files.Count;
        string dbPath = Path.Join(saveDirectory, Config.DatabaseName);
        // Per-thread resources
        SQLiteReadConnection[] readers = new SQLiteReadConnection[nWorkers];
        ChunkContentReader[] chunkReaders = new ChunkContentReader[nWorkers];
        Memory<byte>[] compressBuffers = new Memory<byte>[nWorkers];
        Decompressor[] decompressors = new Decompressor[nWorkers];
        for (int i = 0; i < nWorkers; i++) {
            readers[i] = new(dbPath);
            chunkReaders[i] = new ChunkContentReader(saveDirectory);
            compressBuffers[i] = new byte[Config.MaxChunkSize];
            decompressors[i] = new();
        }
        // Job description
        void Job(int tIndex, int fileIndex, FileToArchive file, CancellationToken token) {
            progress.Report(new(fileIndex + 1, fileCount, $"Extracting: {file.AbsPath}", false));
            token.ThrowIfCancellationRequested();
            var reader = readers[tIndex];
            var chunkReader = chunkReaders[tIndex];
            var compressBuffer = compressBuffers[tIndex];
            var decompressor = decompressors[tIndex];
            var hashRows = reader.Select<Hash256>($"""
                SELECT Hash FROM FileChunkRelation
                WHERE FileId = ?
                ORDER BY OrderIndex ASC
                """, file.Id);
            var chunkRows = reader.Select<Hash256, int>($"""
                SELECT Hash, CompressLevel
                FROM ChunkEntity
                WHERE Hash IN (
                    SELECT DISTINCT Hash FROM FileChunkRelation
                    WHERE FileId = ?
                )
                """, file.Id);
            Dictionary<Hash256, int> configMap = [];
            foreach (var row in chunkRows) {
                configMap[row.Item1] = row.Item2;
            }
            using (var stream = File.OpenWrite(file.AbsPath)) {
                foreach (var hash in hashRows.Select(r => r.Item1)) {
                    token.ThrowIfCancellationRequested();
                    void ConsumeChunk(ReadOnlySpan<byte> content) {
                        var level = configMap[hash];
                        if (level > 0) {
                            int decompressedSize = decompressor.Unwrap(content, compressBuffer.Span, false);
                            ReadOnlyMemory<byte> decompressed = compressBuffer[..decompressedSize];
                            stream.Write(decompressed.Span);
                        }
                        else {
                            stream.Write(content);
                        }
                    }
                    chunkReader.ProcessContent(hash, ConsumeChunk);
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
                        var file = files[fileIndex];
                        Job(workerIndex, fileIndex, file, token);
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
    public bool RestoreFolder(long folderId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteReadConnection reader = new(dbPath);
        try {
            // Get file data
            var files = GetFiles(reader, folderId, outputFolderPath);
            // Restore file
            RestoreFiles(files, this._saveFolder, nWorkers, progress, token);
            progress.Report(new(-1, -1, "Completed", true));
            return true;
        }
        catch {
            progress.Report(new(-1, -1, "Failed", false));
            return false;
        }
        finally {
            reader.Dispose(false);
        }
    }
    public bool RestoreFile(long fileId, string outputFolderPath, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteReadConnection reader = new(dbPath);
        try {
            // Get file data
            var rows = reader.Select<string>(
                "SELECT Name FROM FileEntity WHERE Id = ?", fileId).ToList();
            if (rows.Count == 0) {
                throw new KeyNotFoundException();
            }
            string fileName = rows[0].Item1;
            string filePath = Path.Combine(outputFolderPath, fileName);
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(outputFolderPath);
            // Restore file
            RestoreFiles([new(fileId, fileName, filePath)], this._saveFolder, 1, progress, token);
            progress.Report(new(-1, -1, "Completed", true));
            return true;
        }
        catch {
            progress.Report(new(-1, -1, "Failed", false));
            return false;
        }
        finally {
            reader.Dispose(false);
        }
    }
}
