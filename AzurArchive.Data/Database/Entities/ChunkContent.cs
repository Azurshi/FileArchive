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

[Table(nameof(ChunkContent), [], [], "WITHOUT ROWID")]
internal class ChunkContentTempory {
    [PrimaryKey] public Hash256 Hash { get; init; }
    [DatabaseField] public Memory<byte> Content { get; init; }
    public ChunkContentTempory((Hash256 Hash, Memory<byte> Content) t) {
        this.Hash = t.Hash;
        this.Content = t.Content;
    }
}