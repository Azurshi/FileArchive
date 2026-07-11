using System;

namespace AzurArchive.Data;

public class FolderEntry {
    public long ParentId { get; init; }
    public long Id { get; init; }
    public string Name { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime ModifiedTime { get; init; }

    public bool IsRoot => ParentId < 0;

    public FolderEntry(long parentId, long id, string name, DateTime creationTime, DateTime modifiedTime) {
        this.ParentId = parentId;
        this.Id = id;
        this.Name = name;
        this.CreationTime = creationTime;
        this.ModifiedTime = modifiedTime;
    }
}
