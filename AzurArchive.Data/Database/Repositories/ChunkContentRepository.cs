using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    public void InsertChunks(IReadOnlyList<ChunkContentTempory> chunks) {
        List<List<ChunkContentTempory>> distributedChunks = [];
        for (int i = 0; i < Config.Shards; i++) {
            distributedChunks.Add([]);
        }
        foreach (var chunk in chunks) {
            int shardIndex = chunk.Hash.ShardIndex;
            distributedChunks[shardIndex].Add(chunk);
        }
        for (int i = 0; i < Config.Shards; i++) {
            var shard = this._shards[i];
            shard.Insert(distributedChunks[i], false);
        }
    }
    public void InserChunk(Hash256 hash, Memory<byte> memory) {
        var shard = this._shards[hash.ShardIndex];
        shard.Insert([new ChunkContentTempory((hash, memory))], false);
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
            var batches = items.Chunk(Config.MaxParameterCount).Select(c => c.ToList());
            foreach(var batch in batches) {
                shard.Delete($"""
                    DELETE FROM ChunkContent
                    WHERE Hash IN ({GetPlaceholder(batch.Count)})
                    """, batch.Cast<object>().ToArray());
            }
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
        int deletedCount = 0;
        int totalCount = hashes.Count;
        progress.Report(new(deletedCount, totalCount, "Delete", false));
        for (int i = 0; i < Config.Shards; i++) {
            var shard = this._shards[i];
            var items = distributedHashes[i];
            var batches = items.Chunk(Config.MaxParameterCount).Select(c => c.ToList());
            foreach (var batch in batches) {
                token.ThrowIfCancellationRequested();
                shard.Delete($"""
                    DELETE FROM ChunkContent
                    WHERE Hash IN ({GetPlaceholder(batch.Count)})
                    """, batch.Cast<object>().ToArray());
                deletedCount += batch.Count;
                progress.Report(new(deletedCount, totalCount, "Delete", false));
            }
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
        var rows = reader.Select<Memory<byte>>($"""
            SELECT Content FROM ChunkContent
            WHERE Hash = ?
            """, hash).ToList();
        return rows[0].Item1;
    }
    public void ProcessContent(Hash256 hash, Action<ReadOnlySpan<byte>> consumer) {
        var reader = this._shards[hash.ShardIndex];
        var rows = reader.Select($"""
            SELECT Content FROM ChunkContent
            WHERE Hash = ?
            """, consumer, hash).ToList();
    }

    public void Dispose() {
        foreach(var shard in _shards) {
            shard.Dispose(false);
        }
    }
}