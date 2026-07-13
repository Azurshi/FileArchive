using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private static void DeleteChunks(SQLiteWriteConnection writer, string saveDirectory, List<long> fileIds, IProgress<ArchiveProgress> progress, CancellationToken token) {
        Dictionary<Hash256, int> counter = [];
        foreach(var batch in fileIds.Chunk(Config.MaxParameterCount)) {
            var hashRows = writer.Select<Hash256>($"""
                DELETE FROM FileChunkRelation
                WHERE FileId IN ({GetPlaceholder(batch.Length)})
                RETURNING Hash
                """, batch.Cast<object>().ToArray());
            foreach(var row in hashRows) {
                var hash = row.Item1;
                if (counter.TryGetValue(hash, out var count)) {
                    counter[hash] = count + 1;
                }
                else {
                    counter[hash] = 1;
                }
            }
        }
        // Reverse dictionary to reduce call to database
        Dictionary<int, List<Hash256>> reversedCounter = [];
        foreach(var(key, value) in counter) {
            if (reversedCounter.TryGetValue(value, out var hashes)) {
                hashes.Add(key);
            } else {
                reversedCounter[value] = [key];
            }
        }
        foreach (var (decrease, hashes) in reversedCounter) {
            foreach(var batch in hashes.Chunk(Config.MaxParameterCount)) {
                writer.Update($"""
                    UPDATE ChunkEntity
                    SET ReferenceCount = ReferenceCount - {decrease}
                    WHERE Hash IN ({GetPlaceholder(batch.Length)})
                    """, batch.Cast<object>().ToArray());
            }
        }
        List<Hash256> invalidHashes = [];
        var rows = writer.Select<Hash256>($"""
                DELETE FROM ChunkEntity
                WHERE ReferenceCount = ?
                RETURNING Hash
                """, 0);
        foreach (var row in rows) {
            invalidHashes.Add(row.Item1);
        }
        ChunkContentWriter chunkWriter = new(saveDirectory);
        chunkWriter.BeginTransaction();
        try {
            chunkWriter.DeleteChunks(invalidHashes, progress, token);
            chunkWriter.CommitTransaction();
        }
        catch {
            chunkWriter.RollbackTransaction();
            throw;
        }
        finally {
            chunkWriter.Dispose();
        }
    }
    private static List<long> DeleteFolderAndFiles(SQLiteWriteConnection writer, long folderId) {
        // Travel file and folder
        Queue<long> folderQ = [];
        List<long> folderIds = [folderId];
        List<long> fileIds = [];
        folderQ.Enqueue(folderId);
        while (folderQ.Count > 0) {
            folderId = folderQ.Dequeue();
            var folderRows = writer.Select<long>($"""
                SELECT Id FROM FolderEntity
                WHERE ParentId = ?
                """, folderId);
            foreach (var row in folderRows) {
                folderQ.Enqueue(row.Item1);
                folderIds.Add(row.Item1);
            }
        }
        foreach (var batch in folderIds.Chunk(Config.MaxParameterCount)) {
            // Delete folder
            writer.Delete($"""
                DELETE FROM FolderEntity
                WHERE Id IN ({GetPlaceholder(batch.Length)})
                """, batch.Cast<object>().ToArray());
            // Delete file and get id
            var fileRows = writer.Select<long>($"""
                DELETE FROM FileEntity
                WHERE FolderId IN ({GetPlaceholder(batch.Length)})
                RETURNING Id
                """, batch.Cast<object>().ToArray());
            foreach (var row in fileRows) {
                fileIds.Add(row.Item1);
            }
        }
        return fileIds;
    }
    public long? DeleteFolder(long folderId, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            long parentFolderId = FolderRepository.GetFolderParentId(writer, folderId) ?? throw new KeyNotFoundException();
            var fileIds = DeleteFolderAndFiles(writer, folderId);
            token.ThrowIfCancellationRequested();
            DeleteChunks(writer, this._saveFolder, fileIds, progress, token);
            writer.CommitTransaction();
            // At this point, data already commited
            progress.Report(new(-1, -1, "Maintain", false));
            ChunkContentWriter chunkWriter = new(this._saveFolder);
            try {
                writer.IncrementalVacuum(null);
                Stopwatch sw = new();
                sw.Start();
                chunkWriter.LightMaintain();
                sw.Stop();
                Debug.WriteLine($"Maintain elapsed time: {sw.ElapsedMilliseconds} ms");
                progress.Report(new(-1, -1, "Completed", true));
            }
            catch {
                // Hide exception, since data already commited
            }
            finally {
                // metadata writer already handled by outer finally
                chunkWriter.Dispose();
            }
            return parentFolderId;
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
    public long? DeleteFile(long fileId, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            long parentFolderId = FileRepository.GetFileParentId(writer, fileId) ?? throw new KeyNotFoundException();
            writer.Delete("DELETE FROM FileEntity WHERE Id = ?", fileId);
            token.ThrowIfCancellationRequested();
            DeleteChunks(writer, this._saveFolder, [fileId], progress, token);
            writer.CommitTransaction();
            // At this point, data already commited
            progress.Report(new(-1, -1, "Maintain", false));
            ChunkContentWriter chunkWriter = new(this._saveFolder);
            try {
                writer.IncrementalVacuum(null);
                Stopwatch sw = new();
                sw.Start();
                chunkWriter.LightMaintain();
                sw.Stop();
                Debug.WriteLine($"Maintain elapsed time: {sw.ElapsedMilliseconds} ms");
                progress.Report(new(-1, -1, "Completed", true));
            }
            catch {
                // Hide exception, since data already commited
            }
            finally {
                // metadata writer already handled by outer finally
                chunkWriter.Dispose();
            }
            return parentFolderId;
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
