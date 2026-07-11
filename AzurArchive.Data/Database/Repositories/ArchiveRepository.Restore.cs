using AzurArchive.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZstdNet;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private ValueTuple<List<long>, Dictionary<long, string>, Dictionary<long, ValueTuple<long, string>>>? HandleFolder(SQLiteReadConnection connection, long folderId, string outputFolderPath) {
        // Handle first folder
        Queue<long> folderQ = [];
        Dictionary<long, string> folderMap = [];
        Dictionary<long, (long FolderId, string Name)> fileMap = [];
        List<long> fileIds = [];
        folderQ.Enqueue(folderId);
        Dictionary<Hash256, int> counter = [];
        var firstFolderRows = connection.Select<string>($"""
                SELECT Name FROM FolderEntity
                WHERE Id = ?
                """, folderId).ToList();
        if (firstFolderRows.Count > 0) {
            string firstFolderPath = Path.Join(outputFolderPath, firstFolderRows[0].Item1);
            folderMap[folderId] = firstFolderPath;
            Directory.CreateDirectory(firstFolderPath);
        }
        else {
            return null;
        }
        // Handle Folder and File scan
        while (folderQ.Count > 0) {
            folderId = folderQ.Dequeue();
            var folderRows = connection.Select<long, string>($"""
                    SELECT Id, Name FROM FolderEntity
                    WHERE ParentId = ?
                    """, folderId);
            string folderPath = folderMap[folderId];
            foreach (var row in folderRows) {
                folderQ.Enqueue(row.Item1);
                string subFolderPath = Path.Combine(folderPath, row.Item2);
                folderMap[row.Item1] = subFolderPath;
                Directory.CreateDirectory(subFolderPath);
            }
            var fileRows = connection.Select<long, string>($"""
                    SELECT Id, Name FROM FileEntity
                    WHERE FolderId = ?
                    """, folderId);
            foreach (var row in fileRows) {
                fileMap[row.Item1] = (folderId, row.Item2);
                fileIds.Add(row.Item1);
            }
        }
        return (fileIds, folderMap, fileMap);
    }
    public async Task<bool> Restore(long folderId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, "appData.db");
        SQLiteReadConnection[] readers = new SQLiteReadConnection[nWorkers];
        for(int i=0; i<nWorkers; i++) {
            readers[i] = new(dbPath);
        }
        var pack = HandleFolder(readers[0], folderId, outputFolderPath);
        if(pack == null) {
            progress.Report(new(-1, -1, "Failed", true));
            foreach (var reader in readers) {
                reader.Dispose(false);
            }
            return false;
        }
        var (fileIds, folderMap, fileMap) = pack.Value;
        // Handle file restore
        ChunkContentReader[] chunkReaders = new ChunkContentReader[nWorkers];
        Memory<byte>[] compressBuffers = new Memory<byte>[nWorkers];
        Decompressor[] decompressors = new Decompressor[nWorkers];
        for(int i=0; i<nWorkers; i++) {
            chunkReaders[i] = _provider.GetRequiredService<ChunkContentReader>();
            compressBuffers[i] = new byte[Config.MaxChunkSize];
            decompressors[i] = new();
        }
        int fileCount = fileIds.Count;
            
        void Job(int tIndex, int fileIndex, long fileId, string filePath, CancellationToken token) {
            progress.Report(new(fileIndex + 1, fileCount, $"Extracting: {filePath}", false));
            token.ThrowIfCancellationRequested();
            var reader = readers[tIndex];
            var chunkReader = chunkReaders[tIndex];
            var compressBuffer = compressBuffers[tIndex];
            var decompressor = decompressors[tIndex];
            var hashRows = reader.Select<Hash256>($"""
                SELECT Hash FROM FileChunkRelation
                WHERE FileId = ?
                ORDER BY OrderIndex ASC
                """, fileId);
            var chunkRows = reader.Select<Hash256, int>($"""
                SELECT Hash, CompressLevel
                FROM ChunkEntity
                WHERE Hash IN (
                    SELECT DISTINCT Hash FROM FileChunkRelation
                    WHERE FileId = ?
                )
                """, fileId);
            Dictionary<Hash256, int> configMap = [];
            foreach (var row in chunkRows) {
                configMap[row.Item1] = row.Item2;
            }
            using (var stream = File.OpenWrite(filePath)) {
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
                            if (nextFile >= fileCount) {
                                return;
                            }
                            fileIndex = nextFile++;
                        }
                        var fileId = fileIds[fileIndex];
                        var (parentId, name) = fileMap[fileId];
                        var parentPath = folderMap[parentId];
                        string filePath = Path.Combine(parentPath, name);
                        Job(workerIndex, fileIndex, fileId, filePath, token);
                    }
                });
            }
            await Task.WhenAll(workers);
            progress.Report(new(-1, -1, "Completed", true));
            return true;
        }
        catch {
            progress.Report(new(-1, -1, "Failed", true));
            return false;
        }
        finally {
            for(int i=0; i<nWorkers; i++) {
                readers[i].Dispose(false);
                chunkReaders[i].Dispose();
                decompressors[i].Dispose();
            }
        }
        
    }
    public bool RestoreFile(long fileId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, "appData.db");
        var reader = new SQLiteReadConnection(dbPath);
        var rows = reader.Select<string>(
            "SELECT Name FROM FileEntity WHERE FileId = ?", fileId).ToList();
        if (rows.Count == 0) {
            reader.Dispose(false);
            return false;
        }
        string filePath = Path.Combine(outputFolderPath, rows[0].Item1);
        token.ThrowIfCancellationRequested();
        var chunkReader = _provider.GetRequiredService<ChunkContentReader>();
        Memory<byte> compressBuffer = new byte[Config.MaxChunkSize];
        var decompressor = new Decompressor();
        try {
            using (var stream = File.OpenWrite(filePath)) {
                var hashRows = reader.Select<Hash256>($"""
                SELECT Hash FROM FileChunkRelation
                WHERE FileId = ?
                ORDER BY OrderIndex ASC
                """, fileId);
                var chunkRows = reader.Select<Hash256, int>($"""
                SELECT Hash, CompressLevel
                FROM ChunkEntity
                WHERE Hash IN (
                    SELECT DISTINCT Hash FROM FileChunkRelation
                    WHERE FileId = ?
                )
                """, fileId);
                Dictionary<Hash256, int> configMap = [];
                foreach (var row in chunkRows) {
                    configMap[row.Item1] = row.Item2;
                }
                foreach (var hash in hashRows.Select(r => r.Item1)) {
                    token.ThrowIfCancellationRequested();
                    var content = chunkReader.GetContent(hash);
                    var level = configMap[hash];
                    if (level > 0) {
                        int decompressedSize = decompressor.Unwrap(content.Span, compressBuffer.Span, false);
                        ReadOnlyMemory<byte> decompressed = compressBuffer[..decompressedSize];
                        stream.Write(decompressed.Span);
                    }
                    else {
                        stream.Write(content.Span);
                    }
                }
            }
            return true;
        }
        catch {
            return false;
        }
        finally {
            reader.Dispose(false);
            chunkReader.Dispose();
            decompressor.Dispose();
        }
    }
}
