using System;

namespace AzurArchive.Data;

public class FileEntry {
    public long FolderId { get; init; }
    public long Id { get; init; }
    public string Name { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime ModifiedTime { get; init; }

    public long OriginalSize { get; init; }
    public string Extension => Name.Split(".")[^1];
    public string Extensions => string.Join(".", Name.Split(".")[1..]);
    public string BaseName => Name.Split(".")[0];

    public FileEntry(long folderId, long id, string name, DateTime creationTime, DateTime modifiedTime, long originalSize) {
        this.FolderId = folderId;
        this.Id = id;
        this.Name = name;
        this.CreationTime = creationTime;
        this.ModifiedTime = modifiedTime;
        this.OriginalSize = originalSize;
    }
}
