using SQLiteORM;
using System;

namespace AzurArchive.Data.Database.Entities;

[Table]
internal class FileEntity {
    [PrimaryKey, AutoIncrement] public long? Id { get; init; }
    [DatabaseField] public long FolderId { get; init; }
    [DatabaseField] public string Name { get; init; }
    [DatabaseField] public DateTime CreationTime { get; init; }
    [DatabaseField] public DateTime ModifiedTime { get; init; }
    [DatabaseField] public long OriginalSize { get; init; }
    public FileEntity((long Id, long FolderId, string Name, DateTime CreationTime, DateTime ModifiedTime, long OriginalSize) t) {
        if (t.Id < 0) {
            this.Id = null;
        }
        else {
            this.Id = t.Id;
        }
        this.FolderId = t.FolderId;
        this.Name = t.Name;
        this.CreationTime = t.CreationTime;
        this.ModifiedTime = t.ModifiedTime;
        this.OriginalSize = t.OriginalSize;
    }
    internal FileEntity WithId(long id) {
        return new((id, this.FolderId, this.Name, this.CreationTime, this.ModifiedTime, this.OriginalSize));
    }
    internal FileEntity WithIdAndFolder(long id, long folderId) {
        return new((id, folderId, this.Name, this.CreationTime, this.ModifiedTime, this.OriginalSize));
    }
}
