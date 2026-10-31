using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using SQLiteORM;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ChunkContentWriter: IDisposable {
    private readonly List<SQLiteWriteConnection> _shards;
    public ChunkContentWriter(string saveDirectory) {
        this._shards = [];
        string shardFolder = Path.Combine(saveDirectory, Config.ShardFolder);
        for (int i = 0; i < Config.Shards; i++) {
            string shardPath = Path.Join(shardFolder, $"shard{i}.db");
            SQLiteWriteConnection connection = new(shardPath);
            _shards.Add(connection);
        }
    }
    public void BeginTransaction() {
        foreach(var shard in this._shards) {
            shard.BeginTransaction();
        }
    }
    public void RollbackTransaction() {
        foreach (var shard in this._shards) {
            shard.RollbackTransaction();
        }
    }
    public void CommitTransaction() {
        foreach (var shard in this._shards) {
            shard.CommitTransaction();
        }
    }
    public void InserChunk(Hash256 hash, Memory<byte> memory) {
        var shard = this._shards[hash.ShardIndex];
        shard.Execute("""
            INSERT INTO ChunkContent (Hash, Content)
            VALUES (?, ?)
            """, hash, memory);
    }
    public ChunkContentRepository.InsertSession CreateInsertSession() {
        return new(this._shards);
    }
    public void DeleteChunks(IReadOnlyList<Hash256> hashes) {
        List<List<Hash256>> distributedHashes = [];
        for (int i = 0; i < Config.Shards; i++) {
            distributedHashes.Add([]);
        }
        foreach(var hash in hashes) {
            distributedHashes[hash.ShardIndex].Add(hash);
        }
        for (int i = 0; i < Config.Shards; i++) {
            var shard = this._shards[i];
            var items = distributedHashes[i];
            var iterator = shard.IterateExecute("""
                DELETE FROM ChunkContent
                WHERE Hash = ?
                """).GetEnumerator();
            iterator.MoveNext();
            foreach (var item in items) {
                var stmt = iterator.Current;
                raw.sqlite3_bind_blob(stmt, 1, item.AsReadOnlySpan());
                iterator.MoveNext();
            }
            iterator.Dispose();
        }
    }
    public void DeleteChunks(IReadOnlyList<Hash256> hashes, IProgress<ArchiveProgress> progress, CancellationToken token) {
        List<List<Hash256>> distributedHashes = [];
        for (int i = 0; i < Config.Shards; i++) {
            distributedHashes.Add([]);
        }
        foreach (var hash in hashes) {
            distributedHashes[hash.ShardIndex].Add(hash);
        }
        int totalCount = hashes.Count;
        Stopwatch throttleSw = Stopwatch.StartNew();
        TimeSpan lastReport = TimeSpan.Zero;
        int completedCount = 0;
        for (int i = 0; i < Config.Shards; i++) {
            var shard = this._shards[i];
            var items = distributedHashes[i];
            var iterator = shard.IterateExecute("""
                DELETE FROM ChunkContent
                WHERE Hash = ?
                """).GetEnumerator();
            iterator.MoveNext();
            foreach (var item in items) {
                var stmt = iterator.Current;
                raw.sqlite3_bind_blob(stmt, 1, item.AsReadOnlySpan());
                iterator.MoveNext();

                TimeSpan elapsed = throttleSw.Elapsed;
                completedCount++;
                bool shouldReport = completedCount == 1 || completedCount == totalCount || elapsed - lastReport > Config.UpdateInterval;
                if (shouldReport) {
                    lastReport = elapsed;
                    progress.Report(new(completedCount+1, totalCount, "Delete", false));
                }
            }
            iterator.Dispose();
        }
    }
    public void LightMaintain() {
        int remainAttempt = Config.MaxMaintainShard;
        float fragmentThreshold = Config.FragmentationThreshold;
        foreach(var shard in _shards) {
            int totalPage = shard.Select<int>("PRAGMA page_count;").First().Item1;
            int freePage = shard.Select<int>("PRAGMA freelist_count;").First().Item1;
            if (((float)freePage / totalPage) >= fragmentThreshold) {
                shard.IncrementalVacuum(Math.Max(1, (int)(freePage * Config.CompactationPercent)));
                remainAttempt--;
                if (remainAttempt == 0) {
                    break;
                }
            }
        }
    }
    public void Dispose() {
        foreach (var shard in _shards) {
            shard.Dispose(false);
        }
    }
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
}

internal partial class ChunkContentReader: IDisposable {
    private readonly List<SQLiteReadConnection> _shards;
    public ChunkContentReader(string saveDirectory) {
        this._shards = [];
        string shardFolder = Path.Combine(saveDirectory, Config.ShardFolder);
        for (int i = 0; i < Config.Shards; i++) {
            string shardPath = Path.Join(shardFolder, $"shard{i}.db");
            SQLiteWriteConnection connection = new(shardPath);
            _shards.Add(connection);
        }
    }

    public Memory<byte> GetContent(Hash256 hash) {
        var reader = this._shards[hash.ShardIndex];
        return reader.Select<Memory<byte>>($"""
            SELECT Content FROM ChunkContent
            WHERE Hash = ?
            """, hash).First().Item1;
    }
    public SQLiteReadConnection GetConnection(Hash256 hash) {
        return this._shards[hash.ShardIndex];
    }
    public void Dispose() {
        foreach(var shard in _shards) {
            shard.Dispose(false);
        }
    }
}