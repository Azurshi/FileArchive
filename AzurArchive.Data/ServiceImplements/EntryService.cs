using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AzurArchive.Data.ServiceImplements;

internal class EntryService: IEntryService {
    private readonly EntryRepository _entryRepo;
    public EntryService(EntryRepository entryRepository) {
        this._entryRepo = entryRepository;
    }

    public bool Contain(long entryId, long childId) {
        return this._entryRepo.Contain(entryId, childId);
    }

    public Task<bool> ContainAsync(long entryId, long childId) {
        return this._entryRepo.ContainAsync(entryId, childId);
    }

    public async Task<bool> Copy(List<long> entryIds, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.Copy(entryIds, toFolderId, progress);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    public async Task<Entry?> Get(long entryId) {
        var entity = await this._entryRepo.Get(entryId);
        if (entity != null) {
            return Entry.FromEntity(entity);
        }
        return null;
    }

    public async Task<List<Entry>> GetEntries(long parentId) {
        var entities = await this._entryRepo.GetChildren(parentId);
        List<Entry> result = [];
        foreach (var entity in entities) {
            if (!entity.Name.StartsWith('*')) {
                result.Add(Entry.FromEntity(entity));
            }
        }
        return result;
    }

    public async Task<List<Entry>> GetFiles(long parentId) {
        var entities = await this._entryRepo.GetChildren(parentId);
        List<Entry> result = [];
        foreach (var entity in entities) {
            if (!entity.Name.StartsWith('*') && entity.IsFile()) {
                result.Add(Entry.FromEntity(entity));
            }
        }
        return result;
    }

    public async Task<List<Entry>> GetFolders(long parentId) {
        var entities = await this._entryRepo.GetChildren(parentId);
        List<Entry> result = [];
        foreach (var entity in entities) {
            if (!entity.Name.StartsWith('*') && entity.IsFolder()) {
                result.Add(Entry.FromEntity(entity));
            }
        }
        return result;
    }

    public async Task<List<string>> GetPath(long entryId) {
        return await this._entryRepo.GetPath(entryId);
    }

    public async Task<bool> Move(List<long> entryIds, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.Move(entryIds, toFolderId, progress);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    public Task<bool> Rename(long entryId, string newName) {
        return this._entryRepo.Rename(entryId, newName);
    }

    public async Task<bool> UpdateEmbeddedIcon(long entryId, ReadOnlyMemory<byte>? embeddedIcon) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.UpdateEmeddedIcon(entryId, embeddedIcon);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    public async Task<bool> UpdateExtendedData(long entryId, Dictionary<string, string> extendedData) {
        await LockManager.ModifyLock.WaitAsync();
        try {
            return await this._entryRepo.UpdateExtendedData(entryId, extendedData);
        }
        finally {
            LockManager.ModifyLock.Release();
        }
    }

    //public Task<List<Entry>> GetTrashEntries() {
    //    throw new NotImplementedException();
    //}
    //public async Task<bool> MoveToTrashCan(List<long> entryIds, IProgress<MovingProgress> progress) {
    //    // Store inside special folder
    //    await LockManager.ModifyLock.WaitAsync();
    //    try {
    //        var trashCanId = await this._entryRepo.GetTrashCanOrCreateIfNotExists();
    //        return await this._entryRepo.MoveToTrashCan(entryIds, trashCanId, progress);
    //    }
    //    finally {
    //        LockManager.ModifyLock.Release();
    //    }
    //}
    //public async Task<bool> RestoreFromTrashCan(List<long> entryIds, IProgress<MovingProgress> progress) {
    //    // Store inside special folder
    //    await LockManager.ModifyLock.WaitAsync();
    //    try {
    //        return await this._entryRepo.RestoreFromTrashCan(entryIds, progress);
    //    }
    //    finally {
    //        LockManager.ModifyLock.Release();
    //    }
    //}
}
