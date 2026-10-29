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
    public Task<Entry?> Import(long folderId, string inputPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token);
    public Task<bool> Restore(long entryId, string outputFolderPath, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token);
    public Task<long?> Delete(long entryId, IProgress<ArchiveProgress> progress, CancellationToken token);
    public Task<long> GetEstimatedArchiveSize(long entryId, CancellationToken token);
    public Task<long> GetOriginalByteCount(CancellationToken token);
    public Task<long> GetCompressedByteCount(CancellationToken token);
}
