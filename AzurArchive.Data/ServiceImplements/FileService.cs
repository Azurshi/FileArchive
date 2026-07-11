using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AzurArchive.Data.ServiceImplemments;

internal class FileService: IFileService {
    private readonly FileRepository _fileRepo;
    public FileService(FileRepository fileRepository) {
        this._fileRepo = fileRepository;
    }
    private static FileEntry ToEntry(FileEntity entity) {
        return new(entity.FolderId, entity.Id!.Value, entity.Name, entity.CreationTime, entity.ModifiedTime, entity.OriginalSize);
    }

    public Task<bool> Copy(long fileId, long toFolderId, IProgress<MovingProgress> progress) {
        return this._fileRepo.Copy(fileId, toFolderId, progress);
    }

    public async Task<FileEntry?> Get(long fileId) {
        var entity = await this._fileRepo.GetFile(fileId);
        if (entity != null) {
            return ToEntry(entity);
        }
        return null;
    }

    public async Task<List<FileEntry>> GetFiles(long folderId) {
        var entities = await this._fileRepo.GetFiles(folderId);
        List<FileEntry> result = [];
        foreach(var entity in entities) {
            result.Add(ToEntry(entity));
        }
        return result;
    }

    public Task<List<string>> GetPath(long folderId) {
        return this._fileRepo.GetPath(folderId);
    }

    public async Task<bool> Move(long fileId, long toFolderId, IProgress<MovingProgress> progress) {
        await LockManager.LargeModifyLock.WaitAsync();
        try {
            return await this._fileRepo.Move(fileId, toFolderId, progress);
        }
        finally {
            LockManager.LargeModifyLock.Release();
        }
    }

    public async Task<bool> RenameFile(long fileId, string fileName) {
        return await this._fileRepo.RenameFile(fileId, fileName);
    }

}
