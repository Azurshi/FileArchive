using SQLiteORM;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ChunkContentRepository {
    internal partial class InsertSession: IDisposable {
        private readonly IEnumerator<sqlite3_stmt>[] _shards;
        public InsertSession(List<SQLiteWriteConnection> shards) {
            this._shards = new IEnumerator<sqlite3_stmt>[shards.Count];
            for(int i=0; i<shards.Count; i++) {
                this._shards[i] = shards[i].IterateExecute("""
                    INSERT INTO ChunkContent (Hash, Content)
                    VALUES (?, ?)
                    """).GetEnumerator();
                this._shards[i].MoveNext();
            }
        }
        public void Insert(Hash256 hash, ReadOnlySpan<byte> content) {
            var shard = this._shards[hash.ShardIndex];
            var stmt = shard.Current;
            raw.sqlite3_bind_blob(stmt, 0 + 1, hash.AsReadOnlySpan());
            raw.sqlite3_bind_blob(stmt, 1 + 1, content);
            shard.MoveNext();
        }
        public void Dispose() {
            foreach(var shard in this._shards) {
                shard.Dispose();
            }
        }
    }
}
