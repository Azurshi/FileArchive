using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AzurArchive.Data.ServiceImplemments;

internal class FolderService: IFolderService {
    private readonly EntryRepository _entryRepo;
    public FolderService(EntryRepository entryRepository) {
        this._entryRepo = entryRepository;
    }

    public Task<bool> ContainFile(long folderId, long childrenId) {
        return this._entryRepo.Contain(folderId, childrenId);
    }

    public Task<bool> ContainFolder(long folderId, long childrenId) {
        return this._entryRepo.Contain(folderId, childrenId);
    }

    public async Task<bool> Copy(long folderId, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.Copy(folderId, toFolderId, progress);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    public async Task<Entry?> CreateFolder(long parentId, string folderName) {
        var entity = await this._entryRepo.CreateFolder(parentId, folderName);
        if (entity != null) {
            return Entry.FromEntity(entity);
        } else {
            return null;
        }
    }

    public async Task<Entry?> Get(long folderId) {
        var entity = await this._entryRepo.Get(folderId);
        if (entity != null) {
            return Entry.FromEntity(entity);
        }
        return null;
    }

    public async Task<List<Entry>> GetFolders(long parentId) {
        var entities = await this._entryRepo.GetChildren(parentId);
        List<Entry> result = [];
        foreach (var entity in entities) {
            result.Add(Entry.FromEntity(entity));
        }
        return result;
    }

    public async Task<List<string>> GetPath(long folderId) {
        return await this._entryRepo.GetPath(folderId);
    }

    public async Task<bool> Move(long folderId, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.Move(folderId, toFolderId, progress);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    public async Task<bool> RenameFolder(long folderId, string folderName) {
        return await this._entryRepo.Rename(folderId, folderName);
    }
}
