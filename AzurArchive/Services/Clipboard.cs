using AzurArchive.Core;
using AzurArchive.Data.Services;
using AzurArchive.Domain;
using AzurArchive.ViewModels.Items;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzurArchive.Services;

public static class Clipboard {
    private static IEntryService EntryService {
        get {
            field ??= AppLifeCycle.Service.GetRequiredService<IEntryService>();
            return field;
        }
    } = null;
    private enum State {
        None,
        Copy,
        Cut
    }
    private record EntryPackage(
        long Id, bool IsFile);
    private static List<EntryPackage> _packages = [];
    private static State _state = State.None;
    public static void Copy(List<ExplorerItemViewModel> items) {
        _state = State.Copy;
        _packages = items.Select(i => new EntryPackage(i.Id, i.IsFile)).ToList();
        PasteCommand.NotifyCanExecute();
    }
    public static void Cut(List<ExplorerItemViewModel> items) {
        _state = State.Cut;
        _packages = items.Select(i => new EntryPackage(i.Id, i.IsFile)).ToList();
        PasteCommand.NotifyCanExecute();
    }
    private static void Print(MovingProgress progress) {
        Debug.WriteLine($"Moving: {progress.Message} : {progress.Remaining}");
    }
    private static bool _busy = false;
    private static readonly AsyncCommandExtend<long> _pasteCommand = new(Paste, CanPaste);
    private static bool CanPaste(long folderId) {
        if (_packages.Count == 0) {
            return false;
        }
        else {
            foreach (var package in _packages) {
                if (EntryService.Contain(folderId, package.Id)) {
                    return false;
                }
            }
        }
        return true;
    }
    public static AsyncCommandExtend<long> PasteCommand => _pasteCommand;
    public static async Task Paste(long folderId) {
        if (!_busy && _packages.Count != 0 && _state != State.None) {
            _busy = true;
            var ids = _packages.Select(i => i.Id).ToList();
            bool success = false;
            if (_state == State.Copy) {
                success = await EntryService.Copy(ids, folderId, new Progress<MovingProgress>(Print));
            }
            else if (_state == State.Cut) {
                success = await EntryService.Move(ids, folderId, new Progress<MovingProgress>(Print));
            }
            if (success) {
                bool hasFolder = _packages.Any(i => !i.IsFile);
                ItemsChangedEventArgs args = new(hasFolder);
                EventSystem.Publish(null, args);
            }
            _busy = false;
        }
    }
}
