using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzurArchive.Data.Services;

public interface IFolderService {
    public Task<Entry?> Get(long folderId);
    public Task<List<Entry>> GetFolders(long parentId);
    public Task<Entry?> CreateFolder(long parentId, string folderName);
    public Task<bool> RenameFolder(long fileId, string folderName);
    public Task<List<string>> GetPath(long folderId);
    public Task<bool> Move(long folderId, long toFolderId, IProgress<MovingProgress> progress);
    public Task<bool> Copy(long folderId, long toFolderId, IProgress<MovingProgress> progress);
    public Task<bool> ContainFolder(long folderId, long childrenId);
    public Task<bool> ContainFile(long folderId, long childrenId);
}
