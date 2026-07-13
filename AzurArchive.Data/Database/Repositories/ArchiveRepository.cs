using System.Linq;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private readonly string _saveFolder;
    private const int CompressionLevel = 5;
    private const FastCDC.CDCImplementation CDCAlgorithm = FastCDC.CDCImplementation.Normalized;
    public ArchiveRepository(DataManager manager) {
        this._saveFolder = manager.SaveDirectory;
    }
    private record FileToArchive(long Id, string Name, string AbsPath);
    private static int AtomicIncrease(Hash256 hash, int refCount) {
        return refCount + 1;
    }
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
}