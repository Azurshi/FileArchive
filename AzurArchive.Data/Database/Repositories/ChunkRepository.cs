using SQLiteORM;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ChunkRepository {
    private readonly DatabaseContextAsync _db;
    public ChunkRepository(DataManager manager) {
        this._db = manager.Database;
    }
    public async Task<long> GetOriginalByteCount() {
        using(var db = await _db.GetReader()) {
            var rows = await db.Connection.SelectAsync<long?>(
                "SELECT SUM(OriginalSize*ReferenceCount) FROM ChunkEntity");
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
    public async Task<long> GetEstimatedArchiveSize(long rootId, CancellationToken token) {
        using (var db = await _db.GetReader()) {
            if (token.IsCancellationRequested) {
                return 0;
            }
            var connection = db.Connection;
            Queue<long> q = [];
            q.Enqueue(rootId);
            Dictionary<Hash256, int> hashCount = [];
            while (q.Count > 0) {
                token.ThrowIfCancellationRequested();
                var entryId = q.Dequeue();
                var children = EntryRepository.GetChildren(connection, entryId);
                foreach (var child in children) {
                    q.Enqueue(child.Id!.Value);
                }
                var stmts = connection.IterateSelect("""
                    SELECT Content FROM EntryContentEntity
                    WHERE Id = ?
                    """, entryId);
                foreach (var stmt in stmts) {
                    var fileContent = raw.sqlite3_column_blob(stmt, 0);
                    for (int offset = 0; offset < fileContent.Length; offset += 32) {
                        Hash256 hash = new(fileContent.Slice(offset, 32));
                        ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(
                            hashCount, hash, out _);
                        count++;
                    }
                    break;
                }
            }
            List<Hash256> tobeRemoved = [];
            double byteCount = 0;
            foreach (var (hash, count) in hashCount) {
                token.ThrowIfCancellationRequested();
                var (compressedSize, totalRefCount) = connection.Select<int, int>("""
                    SELECT CompressedSize, ReferenceCount
                    FROM ChunkEntity
                    WHERE Hash = ?
                    """, hash).First();
                double ratio = count / (double)totalRefCount;
                byteCount += compressedSize * ratio;
            }
            return (long)byteCount;
        }
    }
}