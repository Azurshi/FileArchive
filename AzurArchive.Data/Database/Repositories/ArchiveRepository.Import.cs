using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Relations;
using AzurArchive.Data.ServiceImplemments;
using AzurArchive.Data.Services;
using Blake3;
using Microsoft.Extensions.DependencyInjection;
using SQLiteORM;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZstdNet;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private bool CheckSubFolderExists(SQLiteReadConnection connection, long parentId, string folderName) {
        var folderRows = connection.Select<string>("SELECT Name FROM FolderEntity WHERE ParentId = ?", parentId);
        foreach (var row in folderRows) {
            if (row.Item1.Equals(folderName)) {
                return true;
            }
        }
        return false;
    }
    private FolderEntity? CreateRootFolder(SQLiteWriteConnection connection, long rootId, string folderName) {
        var now = DateTime.Now;
        FolderEntity rootFolder = new((-1, rootId, folderName, now, now));
        List<object[]> ids = connection.Insert([rootFolder], true);
        if (ids.Count > 0) {
            long id = (long)ids[0][0];
            rootFolder = rootFolder.WithId(id);
            return rootFolder;
        }
        else {
            return null;
        }
    }
    private List<ValueTuple<FileEntity, string>> InsertFileAndFolder(SQLiteWriteConnection connection, FolderDto rootDto, long rootFolderId) {
        var now = DateTime.Now;
        Queue<ValueTuple<FolderDto, long>> q = [];
        List<FileEntity> files = [];
        List<long> fileIds;
        List<string> fileAbsPaths = [];
        List<object[]> ids;
        q.Enqueue((rootDto, rootFolderId));
        while (q.Count > 0) {
            var (folderDto, folderId) = q.Dequeue();
            //Debug.WriteLine(folderDto.AbsPath);
            List<FolderEntity> subFolders = [];
            foreach (var subFolderDto in folderDto.Children) {
                //Debug.WriteLine($"+{subFolderDto.Name}");
                FolderEntity subFolder = new((-1, folderId, subFolderDto.Name, now, now));
                subFolders.Add(subFolder);
            }
            foreach (var fileName in folderDto.Files) {
                //Debug.WriteLine($"-{fileName}");
                string filePath = Path.Combine(folderDto.AbsPath, fileName);
                FileInfo fileInfo = new(filePath);
                files.Add(new((-1, folderId, fileName, now, now, fileInfo.Length)));
                fileAbsPaths.Add(filePath);
            }
            ids = connection.Insert(subFolders, true);
            if (ids.Count != folderDto.Children.Count) {
                connection.RollbackTransaction();
                return [];
            }
            else {
                for (int i = 0; i < subFolders.Count; i++) {
                    long id = (long)ids[i][0];
                    q.Enqueue((folderDto.Children[i], id));
                }
            }
        }
        ids = connection.Insert(files, true);
        fileIds = ids.Select(row => (long)row[0]).ToList();
        for (int i = 0; i < files.Count; i++) {
            files[i] = files[i].WithId(fileIds[i]);
        }
        List<ValueTuple<FileEntity, string>> result = [];
        for(int i=0; i<files.Count; i++) {
            result.Add((files[i], fileAbsPaths[i]));
        }
        return result;
    }
    private async Task ProcessChunk(SQLiteWriteConnection writer,string dbPath, List<ValueTuple<FileEntity, string>> files, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {        
        Memory<byte>[] compressBuffers = new Memory<byte>[nWorkers];
        Memory<byte>[] chunkBuffers = new Memory<byte>[nWorkers];
        SQLiteReadConnection[] readers = new SQLiteReadConnection[nWorkers];
        Compressor[] compressors = new Compressor[nWorkers];
        for (int i = 0; i < nWorkers; i++) {
            compressBuffers[i] = new byte[Config.MaxChunkSize];
            chunkBuffers[i] = new byte[Config.ChunkBufferSize];
            compressors[i] = new Compressor(new CompressionOptions(CompressionLevel));
            readers[i] = new SQLiteReadConnection(dbPath);
        }
        var chunkWriter = this._provider.GetRequiredService<ChunkContentWriter>();
        object dataLock = new();
        object chunkLock = new();
        object hashLock = new();
        // This check stale database so does not need limit
        bool CheckExists(SQLiteReadConnection reader, Hash256 hash) {
            var rows = reader.Select<bool>("SELECT EXISTS ( SELECT 1 FROM ChunkEntity WHERE Hash = ?)", hash);
            foreach(var row in rows) {
                return row.Item1;
            }
            return false;
        }
        // Only one writer is allowed
        void InsertChunk(Hash256 hash, Memory<byte> content, int originalSize, int compressionLevel) {
            ChunkEntity entity = new((hash, 1, originalSize, content.Length, compressionLevel));
            lock (dataLock) {
                writer.Insert([entity], false);
            }
            lock (chunkLock) {
                chunkWriter.InserChunk(hash, content);
            }
        }
        ConcurrentDictionary<Hash256, int> hashIncrease = [];
        //ConcurrentDictionary<Hash256, int> scheduledHash = [];
        HashSet<Hash256> scheduledHash = [];
        int fileCount = files.Count;
        List<FileChunkRelation> relations = [];
        void Job(int tIndex, int fileIndex, FileEntity file, string fileAbsPath, CancellationToken token) {
            progress.Report(new(fileIndex + 1, fileCount, $"Adding: {file.Name}", false));
            // Get resource
            var compressBuffer = compressBuffers[tIndex];
            var chunkBuffer = chunkBuffers[tIndex];
            var compressor = compressors[tIndex];
            var reader = readers[tIndex];
            //
            using(var stream = File.OpenRead(fileAbsPath)) {
                int chunkIndex = 0;
                foreach(var chunk in FastCDC.FastCDC.SplitToChunks(stream, chunkBuffer, Config.MinChunkSize, Config.MidChunkSize, Config.MaxChunkSize, CDCAlgorithm)) {
                    token.ThrowIfCancellationRequested();
                    Hash256 hash = new();
                    Hasher.Hash(chunk.Span, hash.AsSpan());
                    bool exist = false;
                    if (hashIncrease.TryGetValue(hash, out var refCount)) {
                        hashIncrease[hash] = refCount + 1;
                        exist = true;
                    }
                    if (!exist) {
                        exist = CheckExists(reader, hash);
                        if (exist) {
                            hashIncrease[hash] = 1;
                            exist = true;
                        }
                    }
                    if (!exist) {
                        lock(hashLock) {
                            if (scheduledHash.Contains(hash)) {
                                hashIncrease[hash] += 1;
                                exist = true;
                            }
                            else {
                                hashIncrease[hash] = 0;
                                scheduledHash.Add(hash);
                            }
                        }
                        if (!exist) {
                            int compressedLength = compressor.Wrap(chunk.Span, compressBuffer.Span);
                            var compressed = compressBuffer[..compressedLength];
                            if (compressed.Length < chunk.Length) {
                                InsertChunk(hash, compressed, chunk.Length, CompressionLevel);
                            }
                            else {
                                InsertChunk(hash, chunk, chunk.Length, 0);
                            }

                            lock(hashLock) {
                                scheduledHash.Remove(hash);
                            }
                        }
                    }
                    relations.Add(new((file.Id!.Value, chunkIndex, hash)));
                    chunkIndex++;
                }
            }
        }
        chunkWriter.BeginTransaction();
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
                        lock(lockObj) {
                            if (nextFile >= files.Count) {
                                return;
                            }
                            fileIndex = nextFile++;
                        }
                        var (file, path) = files[fileIndex];
                        Job(workerIndex, fileIndex, file, path, token);
                    }
                });
            }
            await Task.WhenAll(workers);
            writer.Insert(relations, false);
            // Increase count
            foreach (var (hash, increase) in hashIncrease) {
                if (increase > 0) {
                    writer.Update("UPDATE ChunkEntity SET ReferenceCount = ReferenceCount + ? WHERE Hash = ?", increase, hash);
                }
            }
            chunkWriter.CommitTransaction();
        }
        catch {
            chunkWriter.RollbackTransaction();
            throw;
        }
        finally {
            foreach(var reader in readers) {
                reader.Dispose(false);
            }
            chunkWriter.Dispose();
        }
    }
    public async Task<FolderEntity?> Import(long rootId, FolderDto rootDto, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, "appData.db");
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            // Pre-check
            if (CheckSubFolderExists(writer, rootId, rootDto.Name)) {
                writer.RollbackTransaction();
                progress.Report(new(-1, -1, "Failed", true));
                return null;
            }
            // Create root folder
            FolderEntity? rootFolder = CreateRootFolder(writer, rootId, rootDto.Name);
            if (rootFolder == null) {
                writer.RollbackTransaction();
                progress.Report(new(-1, -1, "Failed", true));
                return null;
            }
            var now = DateTime.Now;
            // Insert file and folder
            var files = InsertFileAndFolder(writer, rootDto, rootFolder.Id!.Value);
            if (files.Count == 0) {
                progress.Report(new(-1, -1, "Failed", true));
                return rootFolder;
            }
            // Process chunk
            await ProcessChunk(writer, dbPath, files, nWorkers, progress, token);
            // Finnalize
            writer.CommitTransaction();
            progress.Report(new(-1, -1, "Completed", true));
            return rootFolder;
        }
        catch {
            writer.RollbackTransaction();
            progress.Report(new(-1, -1, "Failed", true));
            return null;
        }
        finally {
            writer.Dispose(false);
        }
    }
}
