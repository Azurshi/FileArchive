using AzurArchive.Data.Services;
using SQLiteORM;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private static List<Hash256> DeleteRecursive(SQLiteWriteConnection connection, List<long> rootIds, CancellationToken token) {
        Queue<long> q = [];
        foreach (var rootId in rootIds) {
            q.Enqueue(rootId);
        }
        Dictionary<Hash256, int> hashDecrease = [];
        while (q.Count > 0) {
            token.ThrowIfCancellationRequested();
            var entryId = q.Dequeue();
            var children = EntryRepository.GetChildren(connection, entryId);
            foreach (var child in children) {
                q.Enqueue(child.Id!.Value);
            }
            connection.Delete("""
                DELETE FROM EntryEntity
                WHERE Id = ?
                """, entryId);
            var stmts = connection.IterateSelect("""
                DELETE FROM EntryContentEntity
                WHERE Id = ?
                RETURNING Content
                """, entryId);
            foreach (var stmt in stmts) {
                var fileContent = raw.sqlite3_column_blob(stmt, 0);
                for (int offset = 0; offset < fileContent.Length; offset += 32) {
                    Hash256 hash = new(fileContent.Slice(offset, 32));
                    if (hashDecrease.TryGetValue(hash, out var decrease)) {
                        hashDecrease[hash] = decrease + 1;
                    }
                    else {
                        hashDecrease[hash] = 1;
                    }
                }
                break;
            }
        }
        List<Hash256> tobeRemoved = [];
        foreach(var (hash, decrease) in hashDecrease) {
            token.ThrowIfCancellationRequested();
            var refCount = connection.Select<int>("""
                UPDATE ChunkEntity
                SET ReferenceCount = ReferenceCount - ?
                WHERE Hash = ?
                RETURNING ReferenceCount
                """, decrease, hash).First().Item1;
            if (refCount <= 0) {
                connection.Delete("""
                    DELETE FROM ChunkEntity
                    WHERE Hash = ?
                    """, hash);
                tobeRemoved.Add(hash);
            }
        }
        return tobeRemoved;
    }
    public bool DeleteEntry(List<long> entryIds, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteWriteConnection writer = new(dbPath);
        ChunkContentWriter chunkWriter = new(this._saveFolder);
        chunkWriter.BeginTransaction();
        writer.BeginTransaction();
        try {
            //HashSet<long> parentIds = [];
            //foreach(var entryId in entryIds) {
            //    var parentId = EntryRepository.GetParentId(writer, entryId);
            //    if (parentId != null) {
            //        parentIds.Add(parentId.Value);
            //    }
            //}
            var tobeRemovedHashes = DeleteRecursive(writer, entryIds, token);
            chunkWriter.DeleteChunks(tobeRemovedHashes, progress, token);
            token.ThrowIfCancellationRequested();
            // Cant's cancel from here
            using(var appLock = LockManager.AcquireAppLock()) {
                chunkWriter.CommitTransaction();
                writer.CommitTransaction();
            }
            try {
                writer.IncrementalVacuum(null);
                chunkWriter.LightMaintain();
            }
            catch {
                // Hide exception, since data already commited
            }
            progress.Report(new(-1, -1, "Completed", true));
            return true;
        }
        catch {
            chunkWriter.RollbackTransaction();
            writer.RollbackTransaction();
            progress.Report(new(-1, -1, "Failed", true));
            return false;
        }
        finally {
            chunkWriter.Dispose();
            writer.Dispose(false);
        }
    }
}
