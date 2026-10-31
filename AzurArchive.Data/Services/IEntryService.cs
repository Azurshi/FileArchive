using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzurArchive.Data.Services;

public interface IEntryService {
    public Task<Entry?> Get(long entryId);
    public Task<List<Entry>> GetEntries(long parentId);
    public Task<List<Entry>> GetFiles(long parentId);
    public Task<List<Entry>> GetFolders(long parentId);
    public Task<List<string>> GetPath(long entryId);
    public Task<bool> Rename(long entryId, string newName);
    public Task<bool> Move(List<long> entryIds, long toFolderId, IProgress<MovingProgress> progress);
    public Task<bool> Copy(List<long> entryIds, long toFolderId, IProgress<MovingProgress> progress);
    public bool Contain(long entryId, long childId);
    public Task<bool> ContainAsync(long entryId, long childId);
    public Task<bool> UpdateExtendedData(long entryId, Dictionary<string, string> extendedData);
    public Task<bool> UpdateEmbeddedIcon(long entryId, ReadOnlyMemory<byte>? embeddedIcon);
    //public Task<List<Entry>> GetTrashEntries();
    //public Task<bool> MoveToTrashCan(List<long> entryIds, IProgress<MovingProgress> progress);
    //public Task<bool> RestoreFromTrashCan(List<long> entryIds, IProgress<MovingProgress> progress);
}
