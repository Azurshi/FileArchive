using System.Threading;

namespace AzurArchive.Data.Services;

internal static class LockManager {
    public static SemaphoreSlim ModifyLock = new(1);
}
