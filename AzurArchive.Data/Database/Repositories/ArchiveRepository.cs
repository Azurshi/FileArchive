using System;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    private readonly IServiceProvider _provider;
    private readonly string _saveFolder;
    private const int CompressionLevel = 5;
    private const FastCDC.CDCImplementation CDCAlgorithm = FastCDC.CDCImplementation.Normalized;
    public ArchiveRepository(IServiceProvider serviceProvider, DataManager manager) {
        this._provider = serviceProvider;
        this._saveFolder = manager.SaveDirectory;
    }
}

