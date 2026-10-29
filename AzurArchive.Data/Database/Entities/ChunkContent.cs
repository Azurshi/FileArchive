using SQLiteORM;
using System;

namespace AzurArchive.Data.Database.Entities;

[Table(null, [], [], "WITHOUT ROWID")]
internal class ChunkContent {
    [PrimaryKey] public Hash256 Hash { get; init; }
    [DatabaseField] public byte[] Content { get; init; }
    public ChunkContent((Hash256 Hash, byte[] Content) t) {
        this.Hash = t.Hash;
        this.Content = t.Content;
    }
}