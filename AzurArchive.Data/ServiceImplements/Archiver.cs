using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    //private readonly UpdateRepository _updateRep~o;
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
    private static FolderDto? WalkDirectory(string inputFolderPath) {
        DirectoryInfo directoryInfo = new (inputFolderPath);
        if (directoryInfo.Parent == null) {
            return null;
        }
        Queue<FolderDto> q = [];
        FolderDto rootFolder = new(directoryInfo.FullName, directoryInfo.Name, null, [], []);
        q.Enqueue(rootFolder);
        while (q.Count > 0) {
            var folder = q.Dequeue();
            foreach (string subFolderPath in Directory.GetDirectories(folder.AbsPath)) {
                string subFolderName = new DirectoryInfo(subFolderPath).Name;
                FolderDto subFolder = new(subFolderPath, subFolderName, folder, [], []);
                folder.Children.Add(subFolder);
                q.Enqueue(subFolder);
            }
            foreach (string filePath in Directory.GetFiles(folder.AbsPath)) {
                string fileName = Path.GetFileName(filePath);
                folder.Files.Add(fileName);
            }
        }
        return rootFolder;
    }
    public Task<long> GetCompressedByteCount(CancellationToken _) {
        return _chunkRepo.GetCompressedByteCount();
    }

    public Task<long> GetEstimatedFileArchiveSize(long fileId, CancellationToken token) {
        return _chunkRepo.GetEstimatedFileArchiveSize(fileId, token);
    }

    public Task<long> GetEstimatedFolderArchiveSize(long folderId, CancellationToken token) {
        return _chunkRepo.GetEstimatedFolderArchiveSize(folderId, token);
    }

    public Task<long> GetOriginalByteCount(CancellationToken _) {
        return _chunkRepo.GetOriginalByteCount();
    }

    public async Task<FolderEntry?> ImportFolder(long folderId, string inputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        await LockManager.LargeModifyLock.WaitAsync(token);
        this.Busy = true;
        try {
            Stopwatch sw = new();
            sw.Start();
            FolderDto? root = WalkDirectory(inputFolderPath);
            if (root != null) {
                var entity = await Task.Run(() => _archiveRepo.ImportFolder(folderId, root, nWorkers, progress, token));
                sw.Stop();
                Debug.WriteLine($"Elapsed time: {sw.Elapsed.TotalSeconds} s");
                if (entity != null) {
                    return new(entity.ParentId, entity.Id!.Value, entity.Name, entity.CreationTime, entity.ModifiedTime);
                }
                else {
                    return null;
                }
            }
            else {
                return null;
            }
        }
        finally {
            this.Busy = false;
            LockManager.LargeModifyLock.Release();
        }
    }
    public async Task<bool> RestoreFolder(long folderId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        await LockManager.LargeModifyLock.WaitAsync(token);
        this.Busy = true;
        try {
            Stopwatch sw = new();
            sw.Start();
            var result = await Task.Run(() => _archiveRepo.RestoreFolder(folderId, outputFolderPath, nWorkers, progress, token));
            sw.Stop();
            Debug.WriteLine($"Elapsed time: {sw.Elapsed.TotalSeconds} s");
            return result;
        }
        finally {
            this.Busy = false;
            LockManager.LargeModifyLock.Release();
        }
    }

    public async Task<bool> RestoreFile(long fileId, string outputFolderPath, IProgress<ArchiveProgress> progress, CancellationToken token) {
        return await Task.Run(() => _archiveRepo.RestoreFile(fileId, outputFolderPath, progress, token));
    }
    public async Task<long?> DeleteFolder(long folderId, IProgress<ArchiveProgress> progress, CancellationToken token) {
        await LockManager.LargeModifyLock.WaitAsync(token);
        this.Busy = true;
        try {
            return await Task.Run(() => this._archiveRepo.DeleteFolder(folderId, progress, token));
        }
        finally {
            this.Busy = false;
            LockManager.LargeModifyLock.Release();
        }
    }

    public async Task<long?> DeleteFile(long fileId, IProgress<ArchiveProgress> progress, CancellationToken token) {
        await LockManager.LargeModifyLock.WaitAsync(token);
        this.Busy = true;
        try {
            return await Task.Run(() => this._archiveRepo.DeleteFile(fileId, progress, token));
        }
        finally {
            this.Busy = false;
            LockManager.LargeModifyLock.Release();
        }
    }

    public async Task<FileEntry?> ImportFile(long folderId, string inputFilePath, IProgress<ArchiveProgress> progress, CancellationToken token) {
        await LockManager.LargeModifyLock.WaitAsync(token);
        this.Busy = true;
        try {
            Stopwatch sw = new();
            sw.Start();
            var entity = await Task.Run(() => _archiveRepo.ImportFile(folderId, inputFilePath, progress, token));
            if (entity != null) {
                return new(entity.FolderId, entity.Id!.Value, entity.Name, entity.CreationTime, entity.ModifiedTime, entity.OriginalSize);
            }
            else {
                return null;
            }
        }
        finally {
            this.Busy = false;
            LockManager.LargeModifyLock.Release();
        }
    }
}
