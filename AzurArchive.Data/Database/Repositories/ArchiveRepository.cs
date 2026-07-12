using System;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private readonly string _saveFolder;
    private const int CompressionLevel = 5;
    private const FastCDC.CDCImplementation CDCAlgorithm = FastCDC.CDCImplementation.Normalized;
    public ArchiveRepository(DataManager manager) {
        this._saveFolder = manager.SaveDirectory;
    }
}