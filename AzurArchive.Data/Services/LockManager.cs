using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Data.Services;

internal static partial class LockManager {
    public static readonly SemaphoreSlim ModifyLock = new(1);
    private static readonly SemaphoreSlim AppLock = new(1);
    private partial class AppLockHandler: IDisposable {
        public void Dispose() {
            AppLock.Release();
        }
    }
    public static IDisposable AcquireAppLock() {
        var lockHandler = new AppLockHandler();
        AppLock.Wait();
        return lockHandler;
    }
    public static async Task<IDisposable> AcquireAppLockAsync() {
        var lockHandler = new AppLockHandler();
        await AppLock.WaitAsync();
        return lockHandler;
    }
}
