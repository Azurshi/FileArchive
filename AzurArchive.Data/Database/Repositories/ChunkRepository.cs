using SQLiteORM;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal class ChunkRepository {
    private readonly DatabaseContextAsync _db;
    public ChunkRepository(DataManager manager) {
        this._db = manager.Database;
    }
    public async Task<long> GetOriginalByteCount() {
        using(var db = await _db.GetReader()) {
            var rows = await db.Connection.SelectAsync<long?>(
                "SELECT SUM(OriginalSize) FROM ChunkEntity");
            if (rows.Count > 0) {
                return rows[0].Item1 ?? 0;
            }
            else {
                return 0;
            }
        }
    }
    public async Task<long> GetCompressedByteCount() {
        using (var db = await _db.GetReader()) {
            var rows = await db.Connection.SelectAsync<long?>(
                "SELECT SUM(CompressedSize) FROM ChunkEntity");
            if (rows.Count > 0) {
                return rows[0].Item1 ?? 0;
            }
            else {
                return 0;
            }
        }
    }
    public async Task<long> GetEstimatedFileArchiveSize(long fileId, CancellationToken token) {
        using(var db = await _db.GetReader()) {
            if (token.IsCancellationRequested) {
                return 0;
            }
            var connection = db.Connection;
            var relRows = await connection.SelectAsync<Hash256, int>("""
                SELECT Hash, COUNT(*)
                FROM FileChunkRelation
                WHERE FileId = ?
                GROUP BY Hash
                """);
            if (token.IsCancellationRequested) {
                return 0;
            }
            Dictionary<Hash256, int> countMap = [];
            foreach(var row in relRows) {
                countMap[row.Item1] = row.Item2;
            }
            var hashes = countMap.Keys.Cast<object>().ToArray();
            var rows = await db.Connection.SelectAsync<Hash256, int, int>($"""
                SELECT Hash, ReferenceCount, CompressedSize
                FROM ChunkEntity
                WHERE Hash IN ({GetPlaceholder(hashes.Length)})
                """, hashes);
            if (token.IsCancellationRequested) {
                return 0;
            }
            double value = 0;
            foreach(var row in rows) {
                Hash256 hash = row.Item1; ;
                int occupiedCount = countMap[hash];
                int totalCount = row.Item2;
                int compressedSize = row.Item3;
                value += compressedSize * (occupiedCount / (double)totalCount);
            }
            return (long)value;
        }
    }
    public async Task<long> GetEstimatedFolderArchiveSize(long folderId, CancellationToken token) {
        using(var db = await _db.GetReader()) {
            if (token.IsCancellationRequested) {
                return 0;
            }
            var connection = db.Connection;
            Queue<long> folderQ = [];
            List<long> folderIds = [folderId];
            folderQ.Enqueue(folderId);
            Dictionary<Hash256, int> counter = [];
            while(folderQ.Count > 0) {
                folderId = folderQ.Dequeue();
                if (token.IsCancellationRequested) {
                    return 0;
                }
                var folderRows = await db.Connection.SelectAsync<long>($"""
                    SELECT Id FROM FileEntity
                    WHERE FolderId = ?
                    """, folderId);
                foreach(var row in folderRows) {
                    folderQ.Enqueue(row.Item1);
                    folderIds.Add(row.Item1);
                }
            }
            if (token.IsCancellationRequested) {
                return 0;
            }
            var relRows = await db.Connection.SelectAsync<Hash256, int>($"""
                SELECT Hash, COUNT(*)
                FROM FileChunkRelation
                WHERE FileId IN (
                    SELECT Id FROM FolderEntity
                    WHERE ParentId IN ({GetPlaceholder(folderIds.Count)})
                )
                GROUP BY Hash
                """, [.. folderIds.Cast<object>()]);
            Dictionary<Hash256, int> countMap = [];
            foreach(var row in relRows) {
                countMap[row.Item1] = row.Item2;
            }
            var hashes = countMap.Keys.Cast<object>().ToArray();
            if (token.IsCancellationRequested) {
                return 0;
            }
            var rows = await db.Connection.SelectAsync<Hash256, int, int>($"""
                SELECT Hash, ReferenceCount, CompressedSize
                FROM ChunkEntity
                WHERE Hash IN ({GetPlaceholder(hashes.Length)})
                """, hashes);
            double value = 0;
            foreach (var row in rows) {
                Hash256 hash = row.Item1;
                int occupiedCount = countMap[hash];
                int totalCount = row.Item2;
                int compressedSize = row.Item3;
                value += compressedSize * (occupiedCount / (double)totalCount);
            }
            return (long)value;
        }
    }
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
}
