using System;
namespace AzurArchive.Domain;

public class GlobalKeyboardEventArgs(Windows.System.VirtualKey key): EventArgs {
    public Windows.System.VirtualKey Key { get; init; } = key;
}


public class LogEventArgs(string message): EventArgs {
    public string Message { get; init; } = message;
}

public class ItemModifiedEventArgs(long id, bool isFile): EventArgs {
    public long Id => id;
    public bool IsFile => isFile;
}
public class ItemDeletedEventArgs(long parentId, long id, bool isFile): EventArgs {
    public long ParentId => parentId;
    public long Id => id;
    public bool IsFile => isFile;
}
public class ItemAddedEventArgs(long id, bool isFile): EventArgs {
    public long Id => id;
    public bool IsFile => isFile;
}