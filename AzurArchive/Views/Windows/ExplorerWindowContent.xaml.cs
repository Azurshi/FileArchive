using AzurArchive.Core;
using AzurArchive.Domain;
using AzurArchive.ViewModels.Windows;
using Microsoft.UI.Xaml.Controls;

namespace AzurArchive.Views.Windows;

public sealed partial class ExplorerWindowContent: UserControl {
    public ExplorerWindowViewModel ViewModel => (ExplorerWindowViewModel)this.DataContext;
    public ExplorerWindowContent(ExplorerWindowViewModel viewModel) {
        InitializeComponent();
        this.DataContext = viewModel;
        this.ViewModel.SetId(-1).FireAndForgetAsync();
        EventSystem.Connect<LogEventArgs>(OnLogEvent);
    }
    
    private void FileExplorer_ItemDoubleClicked(object sender, ViewModels.Items.ExplorerItemViewModel e) {
        ViewModel.OpenItemCommand.Execute(e);
    }
    private void OnLogEvent(object? sender, LogEventArgs e) {
        this.LogOutput.Text = e.Message;
    }
}
