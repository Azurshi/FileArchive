using SQLiteORM;
using System;

namespace AzurArchive.Data.Database.Entities;

[Obsolete]
[Table]
internal class Statistic {
    [PrimaryKey] public string Key { get; init; }
    [JsonField] public string Value { get; init; }
    public Statistic((string Key, string Value) t) {
        this.Key = t.Key;
        this.Value = t.Value;
    }
}

internal static class StatisticKeys {
    public static readonly string TotalOriginalBytes = nameof(TotalOriginalBytes);
    public static readonly string TotalCompressedBytes = nameof(TotalCompressedBytes);
}