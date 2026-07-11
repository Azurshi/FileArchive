using SQLiteORM;

namespace AzurArchive.Data.Database.Relations;

[Table]
internal class FileChunkRelation {
    [PrimaryKey] public long FileId { get; init; }
    [PrimaryKey] public int OrderIndex { get; init; }
    [DatabaseField] public Hash256 Hash { get; init; }
    public FileChunkRelation((long FileId, int OrderIndex, Hash256 Hash) t) {
        this.FileId = t.FileId;
        this.Hash = t.Hash;
        this.OrderIndex = t.OrderIndex;
    }
    internal FileChunkRelation WithFile(long fileId) {
        return new((fileId, this.OrderIndex, this.Hash));
    }
}
