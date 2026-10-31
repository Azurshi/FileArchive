using SQLiteORM;

namespace AzurArchive.Data.Database.Entities;

[Table(null, [], [], "WITHOUT ROWID")]
internal class EntryContentEntity {
    [PrimaryKey] public long Id { get; init; }
    [DatabaseField] public byte[] Content { get; set; }
    public EntryContentEntity((long Id, byte[] Content) t) {
        this.Id = t.Id;
        this.Content = t.Content;
    }
}