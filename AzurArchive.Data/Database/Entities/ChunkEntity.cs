using SQLiteORM;

namespace AzurArchive.Data.Database.Entities;

[Table]
internal class ChunkEntity {
    [PrimaryKey] public Hash256 Hash { get; init; }
    [DatabaseField] public int ReferenceCount { get; init; }
    [DatabaseField] public int OriginalSize { get; init; }
    [DatabaseField] public int CompressedSize { get; init; }
    [DatabaseField] public int CompressLevel { get; init; }
    public ChunkEntity((Hash256 Hash, int ReferenceCount, int OriginalSize, int CompressedSize, int CompressLevel) t) {
        this.Hash = t.Hash;
        this.ReferenceCount = t.ReferenceCount;
        this.OriginalSize = t.OriginalSize;
        this.CompressedSize = t.CompressedSize;
        this.CompressLevel = t.CompressLevel;
    }
}
