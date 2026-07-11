using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzurArchive.Data.Services;

public interface IFileService {
    public Task<FileEntry?> Get(long fileId);
    public Task<List<FileEntry>> GetFiles(long folderId);
    public Task<bool> RenameFile(long fileId, string fileName);
    public Task<List<string>> GetPath(long folderId);
    public Task<bool> Move(long fileId, long toFolderId, IProgress<MovingProgress> progress);
    public Task<bool> Copy(long fileId, long toFolderId, IProgress<MovingProgress> progress);
}
