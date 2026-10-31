using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace AzurArchive.Data.ServiceImplemments;

internal record FolderDto(
    string AbsPath,
    string Name,
    FolderDto? Parent,
    List<string> Files,
    List<FolderDto> Children
    );

internal class Archiver: IArchiver {
    private readonly ChunkRepository _chunkRepo;
    private readonly ArchiveRepository _archiveRepo;

    public event EventHandler? BusyChanged;
    private bool _busy = false;
    public bool Busy {
        get => _busy;
        set {
            if (_busy != value) {
                _busy = value;
                BusyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public Archiver(ChunkRepository chunkRepository, ArchiveRepository archiveRepository) {
        this._chunkRepo = chunkRepository;
        this._archiveRepo = archiveRepository;
    }
    public async Task<Entry?> Import(long folderId, List<string> inputPaths, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        var sw = Stopwatch.StartNew();
        var result = await Task.Run(() => this._archiveRepo.ImportEntry(folderId, inputPaths, nWorkers, progress, token));
        Debug.WriteLine($"Import elapsed: {sw.Elapsed} s");
        if (result != null) {
            return Entry.FromEntity(result);
        } else {
            return null;
        }
    }

    public async Task<bool> Export(List<long> entryIds, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        var sw = Stopwatch.StartNew();
        var result = await Task.Run(() => this._archiveRepo.ExportEntry(entryIds, outputFolderPath, nWorkers, progress, token));
        Debug.WriteLine($"Restore elapsed: {sw.Elapsed} s");
        return result;
    }

    public async Task<bool> Delete(List<long> entryIds, IProgress<ArchiveProgress> progress, CancellationToken token) {
        var sw = Stopwatch.StartNew();
        var result = await Task.Run(() => this._archiveRepo.DeleteEntry(entryIds, progress, token));
        Debug.WriteLine($"Delete elapsed: {sw.Elapsed} s");
        return result;
    }

    public Task<long> GetEstimatedArchiveSize(long entryId, CancellationToken token) {
        return Task.FromResult(0L);
    }

    public Task<long> GetOriginalByteCount(CancellationToken token) {
        return Task.FromResult(0L);
    }

    public Task<long> GetCompressedByteCount(CancellationToken token) {
        return Task.FromResult(0L);
    }
}
