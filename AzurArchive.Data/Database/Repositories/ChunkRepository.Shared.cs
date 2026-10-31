using SQLiteORM;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ChunkRepository {
    /// <summary>
    /// This check stale database when reader is created.
    /// </summary>
    internal static bool CheckHashExists(SQLiteReadConnection reader, Hash256 hash) {
        return reader.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM ChunkEntity
                WHERE Hash = ?
            )
            """, hash).First().Item1;
    }
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
    internal static Dictionary<Hash256, int> ConstructHashTable(SQLiteReadConnection reader) {
        //var count = reader.Select<int>("""
        //    SELECT Count(Hash)
        //    FROM ChunkEntity
        //    """).First().Item1;
        Dictionary<Hash256, int> hashes = new(1024*1024);
        var rows = reader.Select<Hash256>("""
            SELECT Hash
            FROM ChunkEntity
            """);
        foreach(var row in rows) {
            hashes[row.Item1] = 0;
        }
        return hashes;
    }
}
