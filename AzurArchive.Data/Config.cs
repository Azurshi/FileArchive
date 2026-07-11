namespace AzurArchive.Data;

internal static class Config {
    public const int KB = 1024;
    public const int MB = 1024 * 1024;
    public const int MinChunkSize = KB * 1;
    public const int MidChunkSize = KB * 32;
    public const int MaxChunkSize = KB * 64;
    public const int ChunkBufferSize = MB * 1;
    public const int MaxParameterCount = 32766;
    public const int DeleteBatchSize = 10000;

    public const int SaveDelayMs = 1000 * 5;

    public const float FragmentationThreshold = 0.1f;
    public const float CompactationPercent = 0.5f;
    public const int Shards = 8;
    public const int MaxMaintainShard = 8;
}
