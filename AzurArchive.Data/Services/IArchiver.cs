using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Data.Services;

public class ArchiveProgress {
    public readonly int Current;
    public readonly int Total;
    public readonly string Message;
    public readonly bool Completed;
    public ArchiveProgress(int current, int total, string message, bool completed) {
        this.Current = current;
        this.Total = total;
        this.Message = message;
        this.Completed = completed;
    }
}
public class MovingProgress {
    public readonly int Remaining;
    public readonly string Message;
    public readonly bool Completed;
    public MovingProgress(int remaining, string message, bool completed) {
        this.Remaining = remaining;
        this.Message = message;
        this.Completed = completed;
    }
}

public interface IArchiver {
    public bool Busy { get; }
    public event EventHandler? BusyChanged;
    public Task<FolderEntry?> Import(long folderId, string inputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token);
    public Task<bool> Restore(long folderId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token);
    public Task<bool> RestoreFile(long fileId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token);
    public Task<long> GetEstimatedFileArchiveSize(long fileId, CancellationToken token);
    public Task<long> GetEstimatedFolderArchiveSize(long folderId, CancellationToken token);
    public Task<long> GetOriginalByteCount(CancellationToken token);
    public Task<long> GetCompressedByteCount(CancellationToken token);
    public Task<long?> DeleteFolder(long folderId, IProgress<ArchiveProgress> progress, CancellationToken token);
    public Task<long?> DeleteFile(long fileId, IProgress<ArchiveProgress> progress, CancellationToken token);
}
