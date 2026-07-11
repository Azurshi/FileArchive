using AzurArchive.Core;
using AzurArchive.Data;
using AzurArchive.Data.Services;
using AzurArchive.Domain;
using AzurArchive.ViewModels.Items;
using AzurArchive.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzurArchive.ViewModels.Windows;

public partial class ExplorerWindowViewModel: ObservableObject {
    private readonly IFolderService _folderSerivce;
    private readonly IFileService _fileService;
    private readonly IAppSetting _setting;
    private long _folderId = -1;
    public long FolderId {
        get => _folderId;
        private set {
            if (_folderId != value) {
                _folderId = value;
                OnPropertyChanged();
            }
        }
    }
    public ObservableCollectionExtendAdvanced<ExplorerItemViewModel> Items { get; init; }
    public ItemHeaderInfo HeaderInfo { get; init; }
    private string HeaderInfoFieldName => nameof(ExplorerItemViewModel) + "." + nameof(HeaderInfo);
    public AsyncCommand<ExplorerItemViewModel> OpenItemCommand { get; init; }
    public SyncCommand<List<ExplorerItemViewModel>> SelectItemsCommand { get; init; }
    public SyncCommand<List<ExplorerItemViewModel>> DeselectItemsCommand { get; init; }
    public event EventHandler<string>? WindowNameChangeRequest;
    public AsyncCommandExtend BackwardCommand { get; init; }
    public AsyncCommandExtend ForwardCommand { get; init; }
    public AsyncCommandExtend RefreshCommand { get; init; }
    public AsyncCommandExtend UpCommand { get; init; }
    public SyncCommand InfoCommand { get; init; }
    public string FolderPath { get; private set; }
    private readonly FolderStack _stack;
    private bool _busy;
    public ExplorerWindowViewModel(IServiceProvider provider, IFolderService folderService, IFileService fileService, IAppSetting setting) {
        this._folderSerivce = folderService;
        this._fileService = fileService;
        this.Items = new();
        this._setting = setting;
        this._stack = new();
        this.FolderPath = string.Empty;
        this.HeaderInfo = setting.Get(HeaderInfoFieldName, new ItemHeaderInfo());
        this.HeaderInfo.SaveRequest += this.HeaderInfo_SaveRequest;
        this.OpenItemCommand = new(OpenItem);
        this.SelectItemsCommand = new(SelectItems);
        this.DeselectItemsCommand = new(DeselectItems);
        this.BackwardCommand = new(async () => {
            long? id = this._stack.Backward();
            if (id != null) {
                this.FolderId = id.Value;
                await Refresh();
            }
        }, () => !this._busy && _stack.CanBackward());
        this.ForwardCommand = new(async () => {
            long? id = this._stack.Forward();
            if (id != null) {
                this.FolderId = id.Value;
                await Refresh();
            }
        }, () => !this._busy && _stack.CanForward());
        this.RefreshCommand = new(Refresh, () => !_busy);
        this.UpCommand = new(async () => {
            FolderEntry? folder = await _folderSerivce.Get(_folderId);
            if (folder != null) {
                await SetId(folder.ParentId);
            }
        }, () => !this._busy && FolderId != -1);
        this.InfoCommand = new(() => {
            var window = provider.GetRequiredService<InfoWindow>();
            window.Activate();
        });
        HookSignal();
    }
    private void HookSignal() {
        async Task CheckAndRefresh(long id, bool isFile) {
            if ((isFile && _folderSerivce.ContainFile(_folderId, id))
                || (!isFile && _folderSerivce.ContainFolder(_folderId, id))) {
                await Refresh();
            }
        }
        EventSystem.Connect<ItemAddedEventArgs>(async (_, e) => await CheckAndRefresh(e.Id, e.IsFile));
        EventSystem.Connect<ItemModifiedEventArgs>(async (_, e) => await CheckAndRefresh(e.Id, e.IsFile));
        EventSystem.Connect<ItemDeletedEventArgs>(async (_, e) => {
            if (e.ParentId == _folderId) {
                await Refresh();
            }
        });

    }
    private void HeaderInfo_SaveRequest(object? sender, System.EventArgs e) {
        _setting.Set(HeaderInfoFieldName, HeaderInfo);
    }

    public async Task Refresh() {
        this._busy = true;
        NotifyNavigation();
        List<string> path = await _folderSerivce.GetPath(_folderId);
        this.FolderPath = string.Join("\\", path);
        OnPropertyChanged(nameof(FolderPath));
        FolderEntry? currentFolder = await _folderSerivce.Get(_folderId);
        WindowNameChangeRequest?.Invoke(this, currentFolder?.Name ?? "Home");
        List<FolderEntry> folders = await _folderSerivce.GetFolders(_folderId);
        List<FileEntry> files = await _fileService.GetFiles(_folderId);
        List<ExplorerItemViewModel> items = [];
        foreach (var folder in folders) {
            items.Add(new(folder, this.HeaderInfo));
        }
        foreach (var file in files) {
            items.Add(new(file, this.HeaderInfo));
        }
        Items.Update(items, null);
        this._busy = false;
        NotifyNavigation();
    }
    public async Task SetId(long folderId) {
        this.FolderId = folderId;
        this._stack.Move(folderId);
        await Refresh();
    }
    private async Task OpenItem(ExplorerItemViewModel? vm) {
        if (vm == null || vm.IsFile) {
            return;
        }
        await SetId(vm.Id);
    }
    private void SelectItems(List<ExplorerItemViewModel>? vms) {
        vms ??= [];
        foreach (var vm in vms) {
            vm.Selected = true;
        }
    }
    private void DeselectItems(List<ExplorerItemViewModel>? vms) {
        vms ??= [];
        foreach (var vm in vms) {
            vm.Selected = false;
        }
    }
    private void NotifyNavigation() {
        BackwardCommand.NotifyCanExecute();
        ForwardCommand.NotifyCanExecute();
        RefreshCommand.NotifyCanExecute();
        UpCommand.NotifyCanExecute();
    }
}