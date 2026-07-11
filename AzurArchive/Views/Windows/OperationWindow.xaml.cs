using AzurArchive.Core;
using AzurArchive.Data.Services;
using AzurArchive.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Views.Windows;
public enum OperationType {
    None,
    Move,
    Copy,
    Delete,
    Import,
    Export
}
public sealed partial class OperationWindow: Window {
    private static OperationWindow? _current;
    public static bool Busy => _current != null;
    private readonly CancellationTokenSource _cts;
    public CancellationToken Token => _cts.Token;
    private bool _closed = false;
    private OperationWindow(OperationType operation) {
        _current = this;
        this._cts = new();
        InitializeComponent();
        AppLifeCycle.OnCreateWindow();
        InitializeComponent();
        this.AppWindow.Closing += this.AppWindow_Closing;
        this.AppWindow.Resize(new(512, 200));
        //this.Activated += this.OperationWindow_Activated;
    }

    private void OperationWindow_Activated(object sender, WindowActivatedEventArgs args) {
        //if (!AppLifeCycle.Closed) {
        //    if (this.Content == null) {
        //        var content = this._provider.GetRequiredService<OperationWindowContent>();
        //        this.Content = content;
        //    }
        //}
    }

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args) {
        args.Cancel = true;
        await HandleClose();
    }
    private async Task HandleClose() {
        _current = null;
        this._closed = true;
        await _cts.CancelAsync();
        _cts.Dispose();
        await AppLifeCycle.OnCloseWindow();
        this.AppWindow.Destroy();
    }
    public static OperationWindow Create(OperationType operation) {
        if (Busy) {
            throw new InvalidOperationException();
        }
        var window = new OperationWindow(operation);
        window.OperationTypeText.Text = operation switch {
            OperationType.None => string.Empty,
            OperationType.Move => "Moving",
            OperationType.Copy => "Copy",
            OperationType.Delete => "Deleting",
            OperationType.Import => "Importing",
            OperationType.Export => "Exporting",
            _ => string.Empty
        };
        if (operation == OperationType.Delete || operation == OperationType.Move || operation == OperationType.Copy) {
            window.ProgressDisplay.Visibility = Visibility.Collapsed;
        }
        return window;
    }
    public void Report(ArchiveProgress p) {
        //Debug.WriteLine("Reporting");
        if (p.Total >= 0 && p.Current >= 0) {
            this.ProgressDisplay.Maximum = p.Total;
            this.ProgressDisplay.Value = p.Current;
            this.ProgressText.Text = $"{p.Current} / {p.Total}";
        }
        this.MessageText.Text = p.Message;
        if (p.Completed && !this._closed) {
            this.HandleClose().FireAndForgetAsync();
        }
    }
    public void Report(MovingProgress p) {
        if (p.Remaining >= 0) {
            this.ProgressText.Text = $"{p.Remaining} left";
        }
        this.MessageText.Text = p.Message;
        if (p.Completed && !this._closed) {
            this.HandleClose().FireAndForgetAsync();
        }
    }
}
