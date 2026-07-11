using AzurArchive.Data.Services;
using AzurArchive.Services;
using AzurArchive.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace AzurArchive.Views.Windows;

public sealed partial class InfoWindow: Window {
    private readonly IServiceProvider _provider;
    private readonly IArchiver _archiver;
    public InfoWindow(IServiceProvider serviceProvider, IArchiver archiver) {
        this._provider = serviceProvider;
        this._archiver = archiver;
        AppLifeCycle.OnCreateWindow();
        InitializeComponent();
        this.AppWindow.Closing += this.AppWindow_Closing;
        this.Activated += this.ExplorerWindow_Activated;
    }
    private async void ExplorerWindow_Activated(object sender, WindowActivatedEventArgs args) {
        long totalSize = await _archiver.GetOriginalByteCount(CancellationToken.None);
        this.TotalSizeDisplay.Text = Formatter.SizeFormat(totalSize);
        long compressedSize = await _archiver.GetCompressedByteCount(CancellationToken.None);
        this.CompressedSizeDisplay.Text = Formatter.SizeFormat(compressedSize);
    }

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args) {
        args.Cancel = true;
        await AppLifeCycle.OnCloseWindow();
        sender.Destroy();
    }
}

