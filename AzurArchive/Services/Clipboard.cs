using AzurArchive.Core;
using AzurArchive.Data.Services;
using AzurArchive.ViewModels.Items;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AzurArchive.Services;

public static class Clipboard {
    private enum State {
        None,
        Copy,
        Cut
    }
    private record FilePackage(
        long Id);
    private record FolderPackage(
        long Id);
    private static object? _package = null;
    private static State _state = State.None;
    public static void Copy(ExplorerItemViewModel item) {
        _state = State.Copy;
        if (item.IsFile) {
            _package = new FilePackage(item.Id);
        }
        else {
            _package = new FolderPackage(item.Id);
        }
        PasteCommand.NotifyCanExecute();
    }
    public static void Cut(ExplorerItemViewModel item) {
        _state = State.Cut;
        if (item.IsFile) {
            _package = new FilePackage(item.Id);
        }
        else {
            _package = new FolderPackage(item.Id);
        }
        PasteCommand.NotifyCanExecute();
    }
    private static void Print(MovingProgress progress) {
        Debug.WriteLine($"Moving: {progress.Message} : {progress.Remaining}");
    }
    private static bool _busy = false;
    private static readonly AsyncCommandExtend<long> _pasteCommand = new(Paste, CanPaste);
    private static bool CanPaste(long folderId) {
        if (_package == null) {
            return false;
        }
        else if (_package is FolderPackage folderPackage) {
            var folderService = AppLifeCycle.Service.GetRequiredService<IFolderService>();
            return !folderService.ContainFolder(folderId, folderPackage.Id);
        }
        else if (_package is FilePackage filePackage) {
            var folderService = AppLifeCycle.Service.GetRequiredService<IFolderService>();
            return !folderService.ContainFile(folderId, filePackage.Id);
        }
        return true;
    }
    public static AsyncCommandExtend<long> PasteCommand => _pasteCommand;
    public static async Task Paste(long folderId) {
        if (_busy || _package != null && _state != State.None) {
            _busy = true;
            if (_package is FilePackage filePackage) {
                var fileRepo = AppLifeCycle.Service.GetRequiredService<IFileService>();
                if (_state == State.Copy) {
                    await fileRepo.Copy(filePackage.Id, folderId, new Progress<MovingProgress>(Print));
                }
                else if (_state == State.Cut) {
                    await fileRepo.Move(filePackage.Id, folderId, new Progress<MovingProgress>(Print));
                }
            }
            else if (_package is FolderPackage folderPackage) {
                var folderRepo = AppLifeCycle.Service.GetRequiredService<IFolderService>();
                if (_state == State.Copy) {
                    await folderRepo.Copy(folderPackage.Id, folderId, new Progress<MovingProgress>(Print));
                }
                else if (_state == State.Cut) {
                    await folderRepo.Move(folderPackage.Id, folderId, new Progress<MovingProgress>(Print));
                }
            }
            _busy = false;
        }
    }
}
