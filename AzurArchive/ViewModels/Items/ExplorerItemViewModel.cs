using AzurArchive.Core;
using AzurArchive.Data;
using AzurArchive.Data.Services;
using AzurArchive.Services;
using AzurArchive.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
namespace AzurArchive.ViewModels.Items;

public partial class ExplorerItemViewModel: ViewOnlyListItem, IUpdateble {
    // This would not needed since C# use tracing GC not reference GC
    // But just for safety
    private readonly WeakReference<ItemHeaderInfo> _ref;
    public ItemHeaderInfo? HeaderInfo {
        get {
            if (_ref.TryGetTarget(out var res)) {
                return res;
            }
            return null;
        }
    }

    public object Identify => (IsFile, ParentId, Id, ModifiedTime, Name, CreationTime, ModifiedTime, OriginalSize);
    public long Id { get; init; }
    public long ParentId { get; init; }
    public bool IsFile { get; init; }
    public bool IsFolder => !IsFile;
    public string IdString => Id.ToString();
    public string Name { get; init; }
    public DateTime CreationTime { get; init; }
    public string CreationTimeString => Formatter.Format(CreationTime);
    public DateTime ModifiedTime { get; init; }
    public string ModifiedTimeString => Formatter.Format(ModifiedTime);
    public long OriginalSize { get; init; }
    public string OriginalSizeString => Formatter.SizeFormatRound(OriginalSize);

    public AsyncCommandExtend OpenInNewWindowCommand { get; init; }
    public SyncCommand CutCommand { get; init; }
    public SyncCommand CopyCommand { get; init; }
    public ExplorerItemViewModel(FileEntry file, ItemHeaderInfo headerInfo) {
        this._ref = new(headerInfo);
        this.Id = file.Id;
        this.ParentId = file.FolderId;
        this.IsFile = true;
        this.Name = file.Name;
        this.CreationTime = file.CreationTime;
        this.ModifiedTime = file.ModifiedTime;
        this.OriginalSize = file.OriginalSize;
        this.OpenInNewWindowCommand = new(OpenInNewWindow, () => IsFolder);
        this.CutCommand = new(Cut);
        this.CopyCommand = new(Copy);
    }
    public ExplorerItemViewModel(FolderEntry folder, ItemHeaderInfo headerInfo) {
        this._ref = new(headerInfo);
        this.Id = folder.Id;
        this.ParentId = folder.ParentId;
        this.IsFile = false;
        this.Name = folder.Name;
        this.CreationTime = folder.CreationTime;
        this.ModifiedTime = folder.ModifiedTime;
        this.OriginalSize = 0;
        this.OpenInNewWindowCommand = new(OpenInNewWindow, () => IsFolder);
        this.CutCommand = new(Cut);
        this.CopyCommand = new(Copy);
    }

    public object? GetRawValue(ItemField field) {
        return field switch {
            ItemField.Id => Id,
            ItemField.Name => Name,
            ItemField.CreationTime => CreationTime,
            ItemField.ModifiedTime => ModifiedTime,
            ItemField.OriginalSize => OriginalSize,
            _ => null,
        };
    }
    private async Task OpenInNewWindow() {
        var provider = AppLifeCycle.Service;
        var window = provider.GetRequiredService<ExplorerWindow>();
        window.Activated += async (_, _) => {
            await window.SetId(Id);
        };
        window.Activate();
    }
    private void Cut() {
        Clipboard.Cut(this);
    }
    private void Copy() {
        Clipboard.Copy(this);
    }
}
