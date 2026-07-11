using System.Threading;

namespace AzurArchive.Data.Services;

internal static class LockManager {
    public static SemaphoreSlim LargeModifyLock = new(1);
}
