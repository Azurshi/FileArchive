using AzurArchive.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    public async Task<long?> Delete(long folderId, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, "appData.db");
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            // Travel file and folder
            Queue<long> folderQ = [];
            Dictionary<long, (long FolderId, string Name)> fileMap = [];
            List<long> folderIds = [folderId];
            folderQ.Enqueue(folderId);
            Dictionary<Hash256, int> counter = [];
            var parentRows = writer.Select<long>($"""
                SELECT ParentId FROM FolderEntity WHERE Id = ?
                """, folderId).ToList();
            long parentFolderId;
            if (parentRows.Count > 0) {
                parentFolderId = parentRows[0].Item1;
            } else {
                writer.RollbackTransaction();
                progress.Report(new(-1, -1, "Failed", true));
                return null;
            }

            progress.Report(new(-1, -1, "Deleting folder and file", false));
            while (folderQ.Count > 0) {
                folderId = folderQ.Dequeue();
                var folderRows = writer.Select<long, string>($"""
                    SELECT Id, Name FROM FolderEntity
                    WHERE ParentId = ?
                    """, folderId);
                foreach (var row in folderRows) {
                    folderQ.Enqueue(row.Item1);
                    folderIds.Add(row.Item1);
                }
                // Get chunk count
                var hashRows = writer.Select<Hash256>($"""
                    DELETE FROM FileChunkRelation
                    WHERE FileId IN (
                        SELECT Id
                        FROM FileEntity
                        WHERE FolderId = ?
                    )
                    RETURNING Hash
                    """, folderId);
                foreach(var row in hashRows) {
                    var hash = row.Item1;
                    if (counter.TryGetValue(hash, out var count)) {
                        counter[hash] = count + 1;
                    } else {
                        counter[hash] = 1;
                    }
                }
                // Delete file
                writer.Delete($"""
                    DELETE FROM FileEntity
                    WHERE FolderId = ?
                    """, folderId);

            }
            // Delete folder
            writer.Delete($"""
                DELETE FROM FolderEntity
                WHERE Id IN ({GetPlaceholder(folderIds.Count)})
                """, folderIds.Cast<object>().ToArray());
            foreach(var (hash, decrease) in counter) {
                writer.Update("UPDATE ChunkEntity SET ReferenceCount = ReferenceCount - ? WHERE Hash = ?", decrease, hash);
            }
            List<Hash256> invalidHashes = [];
            var rows = writer.Select<Hash256>($"""
                DELETE FROM ChunkEntity
                WHERE ReferenceCount = ?
                RETURNING Hash
                """, 0);
            foreach(var row in rows) {
                invalidHashes.Add(row.Item1);
            }
            var chunkWriter = this._provider.GetRequiredService<ChunkContentWriter>();
            chunkWriter.BeginTransaction();
            try {
                var batches = invalidHashes.Chunk(Config.DeleteBatchSize).Select(c => c.ToList());
                int count = 0;
                int total = invalidHashes.Count;
                foreach(var batch in batches) {
                    token.ThrowIfCancellationRequested();
                    progress.Report(new(count, total, "Delete chunk", false));
                    chunkWriter.DeleteChunks(batch);
                    count += batch.Count;
                }
                progress.Report(new(total, total, "Delete chunk", false));
                token.ThrowIfCancellationRequested();
                chunkWriter.CommitTransaction();
                try {
                    Stopwatch sw = new();
                    sw.Start();
                    progress.Report(new(-1, -1, "Cleanup", false));
                    chunkWriter.LightMaintain();
                    sw.Stop();
                    Debug.WriteLine($"Maintain elapsed time: {sw.ElapsedMilliseconds} ms");
                }
                catch {}
            }
            catch {
                chunkWriter.RollbackTransaction();
                throw;
            }
            finally {
                chunkWriter.Dispose();
            }
            writer.CommitTransaction();
            writer.IncrementalVacuum(null);
            progress.Report(new(-1, -1, "Completed", true));
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
    public async Task<long?> DeleteFile(long fileId, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, "appData.db");
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            var parentRows = writer.Select<long>($"""
                SELECT FolderId FROM FileEntity WHERE Id = ?
                """, fileId).ToList();
            long parentFolderId;
            if (parentRows.Count > 0) {
                parentFolderId = parentRows[0].Item1;
            }
            else {
                writer.RollbackTransaction();
                progress.Report(new(-1, -1, "Failed", true));
                return null;
            }
            progress.Report(new(-1, -1, "Deleting file", false));
            writer.Delete("DELETE FROM FileEntity WHERE Id = ?", fileId);
            Dictionary<Hash256, int> counter = [];
            // Get chunk count
            var hashRows = writer.Select<Hash256>($"""
                    DELETE FROM FileChunkRelation
                    WHERE FileId = ?
                    RETURNING Hash
                    """, fileId);
            foreach (var row in hashRows) {
                var hash = row.Item1;
                if (counter.TryGetValue(hash, out var count)) {
                    counter[hash] = count + 1;
                }
                else {
                    counter[hash] = 1;
                }
            }
            foreach (var (hash, decrease) in counter) {
                writer.Update("UPDATE ChunkEntity SET ReferenceCount = ReferenceCount - ? WHERE Hash = ?", decrease, hash);
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
            var chunkWriter = this._provider.GetRequiredService<ChunkContentWriter>();
            chunkWriter.BeginTransaction();
            try {
                var batches = invalidHashes.Chunk(Config.DeleteBatchSize).Select(c => c.ToList());
                int count = 0;
                int total = invalidHashes.Count;
                foreach (var batch in batches) {
                    token.ThrowIfCancellationRequested();
                    progress.Report(new(count, total, "Delete chunk", false));
                    chunkWriter.DeleteChunks(batch);
                    count += batch.Count;
                }
                progress.Report(new(total, total, "Delete chunk", false));
                token.ThrowIfCancellationRequested();
                chunkWriter.CommitTransaction();
                try {
                    Stopwatch sw = new();
                    sw.Start();
                    progress.Report(new(-1, -1, "Cleanup", false));
                    chunkWriter.LightMaintain();
                    sw.Stop();
                    Debug.WriteLine($"Maintain elapsed time: {sw.ElapsedMilliseconds} ms");
                }
                catch { }
            }
            catch {
                chunkWriter.RollbackTransaction();
                throw;
            }
            finally {
                chunkWriter.Dispose();
            }
            writer.CommitTransaction();
            writer.IncrementalVacuum(null);
            progress.Report(new(-1, -1, "Completed", true));
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
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
}
