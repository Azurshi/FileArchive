using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AzurArchive.Data.ServiceImplemments;

internal class FolderService: IFolderService {
    private readonly FolderRepository _folderRepo;
    public FolderService(FolderRepository folderRepository) {
        this._folderRepo = folderRepository;
    }
    private static FolderEntry ToEntry(FolderEntity entity) {
        return new(entity.ParentId, entity.Id!.Value, entity.Name, entity.CreationTime, entity.ModifiedTime);
    }

    public bool ContainFile(long folderId, long childrenId) {
        return this._folderRepo.ContainFile(folderId, childrenId);
    }

    public bool ContainFolder(long folderId, long childrenId) {
        return this._folderRepo.ContainFolder(folderId, childrenId);
    }

    public async Task<bool> Copy(long folderId, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.LargeModifyLock.WaitAsync();
        try {
            return await this._folderRepo.Copy(folderId, toFolderId, progress);
        }
        finally {
            LockManager.LargeModifyLock.Release();
        }
    }

    public async Task<FolderEntry?> CreateFolder(long parentId, string folderName) {
        var entity = await _folderRepo.CreateFolder(parentId, folderName);
        if (entity != null) {
            return ToEntry(entity);
        } else {
            return null;
        }
    }

    public async Task<FolderEntry?> Get(long folderId) {
        var entity = await _folderRepo.GetFolder(folderId);
        if (entity != null) {
            return ToEntry(entity);
        } else {
            return null;
        }
    }

    public async Task<List<FolderEntry>> GetFolders(long parentId) {
        var entities = await _folderRepo.GetFolders(parentId);
        List<FolderEntry> result = [];
        foreach(var entity in entities) {
            result.Add(ToEntry(entity));
        }
        return result;
    }

    public Task<List<string>> GetPath(long folderId) {
        return this._folderRepo.GetPath(folderId);
    }

    public async Task<bool> Move(long folderId, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.LargeModifyLock.WaitAsync();
        try {
            return await this._folderRepo.Move(folderId, toFolderId, progress);
        }
        finally {
            LockManager.LargeModifyLock.Release();
        }
    }

    public async Task<bool> RenameFolder(long fiolderId, string folderName) {
        return await this._folderRepo.RenameFolder(fiolderId, folderName);
    }
}
