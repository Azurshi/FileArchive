
using AzurArchive.Core;
using AzurArchive.Services;
using AzurArchive.ViewModels.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace AzurArchive.Views.Windows;
public sealed partial class ExplorerWindow: Window {
    private readonly IServiceProvider _provider;
    private ExplorerWindowContent MainContent => (ExplorerWindowContent)this.Content;
    public ExplorerWindow(IServiceProvider serviceProvider) {
        this._provider = serviceProvider;
        AppLifeCycle.OnCreateWindow();
        InitializeComponent();
        this.AppWindow.Closing += this.AppWindow_Closing;
        this.Activated += this.ExplorerWindow_Activated;
    }

    private void ExplorerWindow_Activated(object sender, WindowActivatedEventArgs args) {
        if (!AppLifeCycle.Exited) {
            if (this.Content == null) {
                var content = this._provider.GetRequiredService<ExplorerWindowContent>();
                content.ViewModel.WindowNameChangeRequest += this.ViewModel_WindowNameChangeRequest;
                this.Content = content;
            }
        }
        ExplorerPicker.Window = this;
    }

    private void ViewModel_WindowNameChangeRequest(object? sender, string e) {
        this.Title = e;
    }

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args) {
        args.Cancel = true;
        await AppLifeCycle.OnCloseWindow();
        sender.Destroy();

    }
    public async Task SetId(long id) {
        await this.MainContent.ViewModel.SetId(id);
    }
}
