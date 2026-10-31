using SQLiteORM;
using System;
using Windows.Storage.Search;

namespace AzurArchive.Data.Database.Entities;

[Table(
    tableName: null,
    tableOptions: [],
    indexOptions: [$"{nameof(ParentId)}, {nameof(Name)}"],
    afterTableOption: ""
    )]
internal class EntryEntity {
    [PrimaryKey, AutoIncrement] public long? Id { get; init; }
    [DatabaseField] public long ParentId { get; init; }
    [DatabaseField] public int Attributes { get; init; }
    [DatabaseField] public long Size { get; init; }

    [DatabaseField] public DateTime CreationTime { get; init; }
    [DatabaseField] public DateTime ModifiedTime { get; init; }
    [DatabaseField] public DateTime AccessTime { get; init; }

    [DatabaseField] public string Name { get; init; }
    [DatabaseField] public ReadOnlyMemory<byte>? EmbeddedIcon { get; init; }
    [JsonField] public string ExtendedData { get; init; }
    public EntryEntity((
        long Id, long ParentId, int Attributes, long Size,
        DateTime CreationTime, DateTime ModifiedTime, DateTime AccessTime,
        string Name, ReadOnlyMemory<byte>? EmbeddedIcon, string ExtendedData) t) {
        if (t.Id < 0) {
            this.Id = null;
        }
        else {
            this.Id = t.Id;
        }
        this.ParentId = t.ParentId;
        this.Size = t.Size;
        this.Attributes = t.Attributes;

        this.CreationTime = t.CreationTime;
        this.ModifiedTime = t.ModifiedTime;
        this.AccessTime = t.AccessTime;

        this.Name = t.Name;
        this.EmbeddedIcon = t.EmbeddedIcon;
        this.ExtendedData = t.ExtendedData;
    }
    public EntryEntity WithId(long id) {
        return new((id, this.ParentId, this.Attributes, this.Size,
            this.CreationTime, this.ModifiedTime, this.AccessTime,
            this.Name, this.EmbeddedIcon, this.ExtendedData));
    }
    public EntryEntity WithIdAndParent(long id, long parentId) {
        return new((id, parentId, this.Attributes, this.Size,
            this.CreationTime, this.ModifiedTime, this.AccessTime,
            this.Name, this.EmbeddedIcon, this.ExtendedData));
    }
    public EntryEntity WithExtendedData(string extendedData) {
        return new((this.Id ?? -1L, this.ParentId, this.Attributes, this.Size,
            this.CreationTime, this.ModifiedTime, this.AccessTime,
            this.Name, this.EmbeddedIcon, extendedData));
    }
    public bool IsFolder() {
        return ((EntryAttributes)this.Attributes & EntryAttributes.Directory) != 0;
    }
    public bool IsFile() {
        return !this.IsFolder();
    }
}