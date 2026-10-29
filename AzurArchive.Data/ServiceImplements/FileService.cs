using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AzurArchive.Data.ServiceImplemments;

internal class FileService: IFileService {
    private readonly EntryRepository _entryRepo;
    public FileService(EntryRepository entryRepository) {
        this._entryRepo = entryRepository;
    }
    public async Task<bool> Copy(long fileId, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.Copy(fileId, toFolderId, progress);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    public async Task<Entry?> Get(long fileId) {
        var entity = await this._entryRepo.Get(fileId);
        if (entity != null) {
            return Entry.FromEntity(entity);
        }
        return null;
    }

    public async Task<List<Entry>> GetFiles(long folderId) {
        var entities = await this._entryRepo.GetChildren(folderId);
        List<Entry> result = [];
        foreach(var entity in entities) {
            result.Add(Entry.FromEntity(entity));
        }
        return result;
    }

    public async Task<List<string>> GetPath(long folderId) {
        return await this._entryRepo.GetPath(folderId);
    }

    public async Task<bool> Move(long fileId, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.Move(fileId, toFolderId, progress);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    public async Task<bool> RenameFile(long fileId, string fileName) {
        return await this._entryRepo.Rename(fileId, fileName);
    }
}
