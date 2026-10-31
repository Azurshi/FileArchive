using AzurArchive.Data.Database.Entities;
using SQLiteORM;
using System;
using System.Collections.Generic;

namespace AzurArchive.Data;

[Flags]
public enum EntryAttributes {
    None                = 0x00000000,
    ReadOnly            = 0x00000001,
    Hidden              = 0x00000002,
    System              = 0x00000004,
    Directory           = 0x00000010,
    Archive             = 0x00000020,
    Device              = 0x00000040,
    Normal              = 0x00000080,
    Temporary           = 0x00000100,
    SparseFile          = 0x00000200,
    ReparsePoint        = 0x00000400,
    Compressed          = 0x00000800,
    Offline             = 0x00001000,
    NotContentIndexed   = 0x00002000,
    Encrypted           = 0x00004000,
    IntegrityStream     = 0x00008000,
    NoScrubData         = 0x00020000,

    Virtual             = 0x00010000,
    EA, RecallOnOpen    = 0x00040000,
    Pinned              = 0x00080000,
    Unpinned            = 0x00100000,
    RecallOnDataAccess  = 0x00400000,
}

public class Entry {
    public long ParentId { get; init; }
    public long Id { get; init; }
    public EntryAttributes Attributes { get; init; }
    public long Size { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime ModifiedTime { get; init; }
    public DateTime AccessTime { get; init; }

    public string Name { get; init; }
    public ReadOnlyMemory<byte>? EmbeddedIcon { get; init; }
    public Dictionary<string, string> ExtendedData { get; init; }

    public bool IsFolder => (this.Attributes & EntryAttributes.Directory) != 0;
    public bool IsFile => !this.IsFolder;
    public string FileExtension => Name.Split(".")[^1];
    public string FileExtensions => string.Join(".", Name.Split(".")[1..]);
    public string FileName => Name.Split(".")[0];

    public Entry(
        long parentId, long id, EntryAttributes attributes, long size,
        DateTime creationTime, DateTime modifiedTime, DateTime accessTime, 
        string name, ReadOnlyMemory<byte>? embeddedIcon, Dictionary<string, string> extendedData
        ) {
        this.ParentId = parentId;
        this.Id = id; 
        this.Attributes = attributes;
        this.Size = size;
        this.CreationTime = creationTime;
        this.ModifiedTime = modifiedTime;
        this.AccessTime = accessTime;
        this.Name = name;
        this.EmbeddedIcon = embeddedIcon;
        this.ExtendedData = extendedData;
    }
    public Entry WithId(long id) {
        return new(this.ParentId, id, this.Attributes, this.Size,
            this.CreationTime, this.ModifiedTime, this.AccessTime,
            this.Name, this.EmbeddedIcon, this.ExtendedData);
    }

    internal static Entry FromEntity(EntryEntity entity) {
        return new(entity.ParentId, entity.Id!.Value, (EntryAttributes)entity.Attributes, entity.Size,
            entity.CreationTime, entity.ModifiedTime, entity.AccessTime,
            entity.Name, entity.EmbeddedIcon, []);
    }
}
