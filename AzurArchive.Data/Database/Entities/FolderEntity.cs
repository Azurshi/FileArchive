using SQLiteORM;
using System;

namespace AzurArchive.Data.Database.Entities;

[Table]
internal class FolderEntity {
    [PrimaryKey, AutoIncrement] public long? Id { get; init; }
    [DatabaseField] public long ParentId { get; init; }
    [DatabaseField] public string Name { get; init; }
    [DatabaseField] public DateTime CreationTime { get; init; }
    [DatabaseField] public DateTime ModifiedTime { get; init; }
    public FolderEntity((long Id, long ParentId, string Name, DateTime CreationTime, DateTime ModifiedTime) t) {
        if (t.Id < 0) {
            this.Id = null;
        } else {
            this.Id = t.Id;
        }
        this.ParentId = t.ParentId;
        this.Name = t.Name;
        this.CreationTime = t.CreationTime;
        this.ModifiedTime = t.ModifiedTime;
    }
    internal FolderEntity WithId(long id) {
        return new((id, this.ParentId, this.Name, this.CreationTime, this.ModifiedTime));
    }
    internal FolderEntity WithIdAndParent(long id, long parentId) {
        return new((id, parentId, this.Name, this.CreationTime, this.ModifiedTime));
    }
}
