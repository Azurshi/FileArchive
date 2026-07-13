using AzurArchive.Core;
using AzurArchive.Data.Services;
using AzurArchive.Domain;
using AzurArchive.ViewModels.Items;
using AzurArchive.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;

namespace AzurArchive.Services;

public static class Commands {
    private static IFolderService FolderService {
        get {
            field ??= AppLifeCycle.Service.GetRequiredService<IFolderService>();
            return field;
        }
    } = null;
    private static IFileService FileService {
        get {
            field ??= AppLifeCycle.Service.GetRequiredService<IFileService>();
            return field;
        }
    } = null;
    private static IArchiver Archiver {
        get {
            field ??= AppLifeCycle.Service.GetRequiredService<IArchiver>();
            return field;
        }
    } = null;
    public static void HookEvent() {
        Archiver.BusyChanged += Archiver_BusyChanged;
    }

    private static void Archiver_BusyChanged(object? sender, EventArgs e) {
        ExportCommand.NotifyCanExecute();
        ImportCommand.NotifyCanExecute();
        DeleteCommand.NotifyCanExecute();
    }

    public static AsyncCommandExtend<ExplorerItemViewModel> ExportCommand { get; } = new(
        async (vm) => {
            if (vm == null || OperationWindow.Busy) {
                return;
            }
            string? path = await ExplorerPicker.PickFolder();
            if (path != null) {
                if (Directory.Exists(path)) {
                    if (vm.IsFolder) {
                        string folderName = Path.Combine(path, vm.Name);
                        if (Directory.Exists(folderName)) {
                            Debug.WriteLine("Folder already exists");
                            return;
                        }
                        var window = OperationWindow.Create(OperationType.Export);
                        window.Activate();
                        await Archiver.RestoreFolder(vm.Id, path, 4, new Progress<ArchiveProgress>(window.Report), window.Token);
                    }
                    else {
                        string fileName = Path.Combine(path, vm.Name);
                        if (File.Exists(fileName)) {
                            Debug.WriteLine("File already exists");
                            return;
                        }
                        var window = OperationWindow.Create(OperationType.Export);
                        window.Activate();
                        await Archiver.RestoreFile(vm.Id, path, new Progress<ArchiveProgress>(window.Report), window.Token);
                    }
                }
                else {
                    Debug.WriteLine("Folder does not exists");
                }
            }
        },
        (vm) => {
            return !Archiver.Busy;
        });
    public static AsyncCommandExtend<long> ImportCommand { get; } = new(
        async (id) => {
            string? path = await ExplorerPicker.PickFolder();
            if (path != null) {
                if (Directory.Exists(path)) {
                    var window = OperationWindow.Create(OperationType.Import);
                    window.Activate();
                    var folder = await Archiver.ImportFolder(id, path, 4, new Progress<ArchiveProgress>(window.Report), window.Token);;
                    if (folder != null) {
                        ItemAddedEventArgs args = new(folder.Id, false);
                        EventSystem.Publish(null, args);
                    }
                }
                else {
                    Debug.WriteLine("Folder does not exists");
                }
            }
        },
        (id) => {
            return !Archiver.Busy;
        });
    public static AsyncCommandExtend<ExplorerItemViewModel> DeleteCommand { get; } = new(
        async (vm) => {
            if (vm == null) {
                return;
            }
            var window = OperationWindow.Create(OperationType.Delete);
            window.Activate();
            if (vm.IsFolder) {
                var parentId = await Archiver.DeleteFolder(vm.Id, new Progress<ArchiveProgress>(window.Report), window.Token);
                if (parentId != null) {
                    ItemDeletedEventArgs args = new(parentId.Value, vm.Id, vm.IsFile);
                    EventSystem.Publish(null, args);
                }
            }
            else {
                var folerId = await Archiver.DeleteFile(vm.Id, new Progress<ArchiveProgress>(window.Report), window.Token);
                if (folerId != null) {
                    ItemDeletedEventArgs args = new(folerId.Value, vm.Id, vm.IsFile);
                    EventSystem.Publish(null, args);
                }
            }

        },
        (vm) => {
            return !Archiver.Busy;
        });
}
