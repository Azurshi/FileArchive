namespace AzurArchive.Data;

public interface ICompressConfig {
    public string Algorithm { get; }
}

public class ChunkEntry {
    public Hash256 Hash { get; init; }
    public int ReferenceCount { get; init; }
    public int OriginalSize { get; init; }
    public int CompressedSize { get; init; }
    public ICompressConfig CompressConfig { get; init; }
    public ChunkEntry(Hash256 hash, int referenceCount, int originalSize, int compressedSize, ICompressConfig compressConfig) {
        this.Hash = hash;
        this.ReferenceCount = referenceCount;
        this.CompressConfig = compressConfig;
        this.OriginalSize = originalSize;
        this.CompressedSize = compressedSize;
    }
    public override bool Equals(object? obj) {
        if (obj is ChunkEntry model) {
            return this.Hash.Equals(model.Hash)
                && this.ReferenceCount.Equals(model.ReferenceCount)
                && this.OriginalSize.Equals(model.OriginalSize)
                && this.OriginalSize.Equals(model.CompressedSize)
                && this.CompressConfig.Equals(model.CompressConfig);
        }
        return false;
    }

    public override int GetHashCode() {
        return this.Hash.GetHashCode();
    }
}
