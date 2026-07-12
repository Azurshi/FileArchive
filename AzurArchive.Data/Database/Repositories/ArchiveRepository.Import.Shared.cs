using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Relations;
using AzurArchive.Data.ServiceImplemments;
using AzurArchive.Data.Services;
using Blake3;
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
    private record FileToArchive(long Id, string Name, string AbsPath);
    private static bool CheckFolderExists(SQLiteWriteConnection connection, long folderId) {
        return folderId == -1 || connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM FolderEntity
                WHERE Id = ?
            )
            """, folderId).First().Item1;            
    }
    private static bool CheckSubFolderExists(SQLiteReadConnection connection, long parentId, string folderName) {
        return connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM FolderEntity
                WHERE ParentId = ? AND LOWER(Name) = LOWER(?)
            )
            """, parentId, folderName).First().Item1;
    }
    private static bool CheckSubFileExists(SQLiteReadConnection connection, long folderId, string fileName) {
        return connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM FileEntity
                WHERE FolderId = ? AND LOWER(Name) = LOWER(?)
            )
            """, folderId, fileName).First().Item1;
    }
    private static FolderEntity? CreateRootFolder(SQLiteWriteConnection connection, long rootId, string folderName) {
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
    private static FileEntity? InsertFile(SQLiteWriteConnection connection, long folderId, string filePath) {
        var now = DateTime.Now;
        FileInfo file = new(filePath);
        FileEntity fileEntity = new((-1, folderId, file.Name, now, now, file.Length));
        List<object[]> ids = connection.Insert([fileEntity], true);
        if (ids.Count > 0) {
            long id = (long)ids[0][0];
            fileEntity = fileEntity.WithId(id);
            return fileEntity;
        }
        else {
            return null;
        }
    }
    private static List<FileToArchive> InsertFileAndFolder(SQLiteWriteConnection connection, FolderDto rootDto, long rootFolderId) {
        var now = DateTime.Now;
        Queue<ValueTuple<FolderDto, long>> q = [];
        List<FileEntity> files = [];
        List<long> fileIds;
        List<string> fileAbsPaths = [];
        List<object[]> ids;
        q.Enqueue((rootDto, rootFolderId));
        while (q.Count > 0) {
            var (folderDto, folderId) = q.Dequeue();
            List<FolderEntity> subFolders = [];
            foreach (var subFolderDto in folderDto.Children) {
                FolderEntity subFolder = new((-1, folderId, subFolderDto.Name, now, now));
                subFolders.Add(subFolder);
            }
            foreach (var fileName in folderDto.Files) {
                string filePath = Path.Combine(folderDto.AbsPath, fileName);
                FileInfo fileInfo = new(filePath);
                files.Add(new((-1, folderId, fileName, now, now, fileInfo.Length)));
                fileAbsPaths.Add(filePath);
            }
            ids = connection.Insert(subFolders, true);
            for (int i = 0; i < subFolders.Count; i++) {
                long id = (long)ids[i][0];
                q.Enqueue((folderDto.Children[i], id));
            }
        }
        ids = connection.Insert(files, true);
        fileIds = ids.Select(row => (long)row[0]).ToList();
        for (int i = 0; i < files.Count; i++) {
            files[i] = files[i].WithId(fileIds[i]);
        }
        List<FileToArchive> result = [];
        for (int i = 0; i < files.Count; i++) {
            var file = files[i];
            result.Add(new(file.Id!.Value, file.Name, fileAbsPaths[i]));
        }
        return result;
    }
    /// <summary>
    /// This check stale database when reader is created.
    /// </summary>
    private static bool CheckHashExists(SQLiteReadConnection reader, Hash256 hash) {
        return reader.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM ChunkEntity
                WHERE Hash = ?
            )
            """, hash).First().Item1;
    }
    private static int AtomicIncrease(Hash256 hash, int refCount) {
        return refCount + 1;
    }
    /// <summary>
    /// Throw exception if failed
    /// </summary>
    private static void ImportChunks(string saveDirectory, SQLiteWriteConnection writer, string dbName, List<FileToArchive> files, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        var chunkWriter = new ChunkContentWriter(saveDirectory);
        string dbPath = Path.Join(saveDirectory, dbName);
        // Per-thread resources
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
        // Locks
        object dataLock = new();
        object chunkLock = new();
        object relationLock = new();
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
        // Shared resources
        ConcurrentDictionary<Hash256, int> hashIncrease = [];
        int fileCount = files.Count;
        List<FileChunkRelation> relations = [];
        // Job descrition
        void Job(int tIndex, int fileIndex, FileToArchive file, CancellationToken token) {
            progress.Report(new(fileIndex + 1, fileCount, $"Adding: {file.Name}", false));
            // Get resource
            var compressBuffer = compressBuffers[tIndex];
            var chunkBuffer = chunkBuffers[tIndex];
            var compressor = compressors[tIndex];
            var reader = readers[tIndex];
            using (var stream = File.OpenRead(file.AbsPath)) {
                int chunkIndex = 0;
                List<FileChunkRelation> threadRelations = [];
                foreach (var chunk in FastCDC.FastCDC.SplitToChunks(stream, chunkBuffer, Config.MinChunkSize, Config.MidChunkSize, Config.MaxChunkSize, CDCAlgorithm)) {
                    token.ThrowIfCancellationRequested();
                    Hash256 hash = new();
                    Hasher.Hash(chunk.Span, hash.AsSpan());
                    // First, check if Hash is exists (added or scheduled in this session)
                    // If not, add with ref count = 0
                    // In multi-thread context, only one hash may pass with count = 0
                    int refCount = hashIncrease.AddOrUpdate(hash, 0, AtomicIncrease);
                    // Ref count = 0 if not exist;
                    bool exist = refCount != 0;
                    // Second, check if hash exist inside datbase
                    if (!exist) {
                        // May double check in multi-thread, so we guard with AddOrUpdate
                        exist = CheckHashExists(reader, hash);
                        // If exist in database
                        if (exist) {
                            // Hash already guaranted to exists inside dictionary
                            // So this method only perform update operation
                            refCount = hashIncrease.AddOrUpdate(hash, 1, AtomicIncrease);
                        }
                        // Else, already handle with refCount increase = 0
                    }
                    // Finnally, if hash neither already added or exist in database
                    // We perform database insert
                    if (!exist) {
                        // This only execute when hash is first scheduled and not exists yet
                        int compressedLength = compressor.Wrap(chunk.Span, compressBuffer.Span);
                        var compressed = compressBuffer[..compressedLength];
                        if (compressed.Length < chunk.Length) {
                            InsertChunk(hash, compressed, chunk.Length, CompressionLevel);
                        }
                        else {
                            InsertChunk(hash, chunk, chunk.Length, 0);
                        }

                    }
                    threadRelations.Add(new((file.Id, chunkIndex, hash)));
                    chunkIndex++;
                }
                lock (relationLock) {
                    relations.AddRange(threadRelations);
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
                            if (nextFile >= files.Count) {
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
            progress.Report(new(fileCount, fileCount, "Insert relation", false));
            writer.Insert(relations, false);
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
            foreach (var reader in readers) {
                reader.Dispose(false);
            }
            chunkWriter.Dispose();
        }
    }
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
}
