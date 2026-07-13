using SQLiteORM;
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
}
