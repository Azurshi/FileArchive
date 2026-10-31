using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.Storage.Pickers;
using WinRT.Interop;

namespace AzurArchive.Core;

public class FileNameComparer : IComparer<string> {
    public int Compare(string? x, string? y) {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        if (!int.TryParse(x.Split(".")[0], out int valueX)) {
            valueX = int.MaxValue;
        }
        if (!int.TryParse(y.Split(".")[0], out int valueY)) {
            valueY = int.MaxValue;
        }
        if (valueX != int.MaxValue && valueY != int.MaxValue) {
            return valueX.CompareTo(valueY);
        }
        else {
            return x.CompareTo(y);
        }
    }
}
public delegate void VoidEventHandler();
public interface IErrorHandler {
    public void HandleError(Exception ex);
}

public sealed partial class SemaphoreSingle : SemaphoreSlim {
    public SemaphoreSingle() : base(1, 1) {
    }
}
public sealed class AsyncPauseToken {
    private TaskCompletionSource _tcs = new();
    public AsyncPauseToken() {
        _tcs.SetResult();
    }
    public bool IsPaused => !_tcs.Task.IsCompleted;
    public bool IsCancellationRequested { get; private set; } = false;
    public Task WaitAsync() => _tcs.Task;
    public void Pause() {
        if (!IsCancellationRequested && _tcs.Task.IsCompleted) {
            _tcs = new();
        }
    }
    public void Resume() {
        if (!IsCancellationRequested) {
            _tcs.TrySetResult();
        }
    }
    public void Cancel() {
        IsCancellationRequested = true;
        _tcs.TrySetResult();
    }
    public void Reset() {
        IsCancellationRequested = false;
        _tcs = new();
        _tcs.SetResult();
    }
}
public static class ExplorerPicker {
    public static Window? Window;
    public static async Task<string?> PickFile(List<string> fileTypes) {
        if (Window == null) {
            throw new NullReferenceException("Window is not set");
        }
        FileOpenPicker picker = new(Window.AppWindow.Id);
        picker.ViewMode = PickerViewMode.List;
        foreach (var fileType in fileTypes) {
            picker.FileTypeFilter.Add(fileType);
        }
        var file = await picker.PickSingleFileAsync();
        if (file != null) {
            return file.Path;
        }
        else {
            return null;
        }
    }
    public static async Task<List<string>> PickFiles(List<string> fileTypes) {
        if (Window == null) {
            throw new NullReferenceException("Window is not set");
        }
        FileOpenPicker picker = new(Window.AppWindow.Id);
        picker.ViewMode = PickerViewMode.List;
        foreach (var fileType in fileTypes) {
            picker.FileTypeFilter.Add(fileType);
        }
        var files = await picker.PickMultipleFilesAsync();
        List<string> result = [];
        foreach(var file in files) {
            result.Add(file.Path);
        }
        return result;
    }
    public static async Task<string?> PickFolder() {
        if (Window == null) {
            throw new NullReferenceException("Window is not set");
        }
        FolderPicker picker = new(Window.AppWindow.Id);
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null) {
            return folder.Path;
        }
        else {
            return null;
        }
    }
    public static async Task<List<string>> PickFolders() {
        if (Window == null) {
            throw new NullReferenceException("Window is not set");
        }
        FolderPicker picker = new(Window.AppWindow.Id);
        var folders = await picker.PickMultipleFoldersAsync();
        List<string> result = [];
        foreach (var file in folders) {
            result.Add(file.Path);
        }
        return result;
    }
}