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
    private static IEntryService EntryService {
        get {
            field ??= AppLifeCycle.Service.GetRequiredService<IEntryService>();
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
        ImportFolderCommand.NotifyCanExecute();
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
                        await Archiver.Export([vm.Id], path, 4, new Progress<ArchiveProgress>(window.Report), window.Token);
                    }
                    else {
                        string fileName = Path.Combine(path, vm.Name);
                        if (File.Exists(fileName)) {
                            Debug.WriteLine("File already exists");
                            return;
                        }
                        var window = OperationWindow.Create(OperationType.Export);
                        window.Activate();
                        await Archiver.Export([vm.Id], path, 4, new Progress<ArchiveProgress>(window.Report), window.Token);
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
    public static AsyncCommandExtend<long> ImportFolderCommand { get; } = new(
        async (id) => {
            var paths = await ExplorerPicker.PickFolders();
            if (paths.Count > 0) {
                var window = OperationWindow.Create(OperationType.Import);
                window.Activate();
                var folder = await Archiver.Import(id, paths, 4, new Progress<ArchiveProgress>(window.Report), window.Token);
                ;
                if (folder != null) {
                    ItemsChangedEventArgs args = new(true);
                    EventSystem.Publish(null, args);
                }
            }
        },
        (id) => {
            return !Archiver.Busy;
        });
    public static AsyncCommandExtend<long> ImportFilesCommand { get; } = new(
        async (id) => {
            var paths = await ExplorerPicker.PickFiles(["*"]);
            if (paths.Count > 0) {
                var window = OperationWindow.Create(OperationType.Import);
                window.Activate();
                var folder = await Archiver.Import(id, paths, 4, new Progress<ArchiveProgress>(window.Report), window.Token); ;
                if (folder != null) {
                    ItemsChangedEventArgs args = new(true);
                    EventSystem.Publish(null, args);
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
            var success = await Archiver.Delete([vm.Id], new Progress<ArchiveProgress>(window.Report), window.Token);
            if (success) {
                ItemsChangedEventArgs args = new(vm.IsFolder);
                EventSystem.Publish(null, args);
            }
        },
        (vm) => {
            return !Archiver.Busy;
        });
}
