using AzurArchive.Core;
using AzurArchive.Data.Services;
using AzurArchive.Services;
using AzurArchive.ViewModels;
using AzurArchive.ViewModels.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Views.Controls;

public sealed partial class CompressedSizeLabel: UserControl {
    private static readonly Type ThisType = typeof(CompressedSizeLabel);
    public ExplorerItemViewModel? Data {
        get => (ExplorerItemViewModel?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
    public static readonly DependencyProperty DataProperty
        = Utility.Create<ExplorerItemViewModel?>(ThisType, null, (d, e) => {
            var This = (CompressedSizeLabel)d;
            var newValue = (ExplorerItemViewModel?)e.NewValue;
            This.SetData(newValue);
        });
    private static bool _busy = false;
    private static readonly AsyncCommandExtend<ValueTuple<ExplorerItemViewModel?, CompressedSizeLabel>> _command = new(
        async (data) => {
            var (vm, This) = data;
            if (vm == null) {
                return;
            }
            _busy = true;
            _command?.NotifyCanExecute();
            var archiver = AppLifeCycle.Service.GetRequiredService<IArchiver>();
            long size;
            if (vm.IsFile) {
                size = await archiver.GetEstimatedFileArchiveSize(vm.Id, CancellationToken.None);
            }
            else {
                size = await archiver.GetEstimatedFolderArchiveSize(vm.Id, CancellationToken.None);
            }
            This.SetSize(size);
            _busy = false;
            _command?.NotifyCanExecute();
        },
        (data) => data.Item1 != null && !_busy);
    private void SetData(ExplorerItemViewModel? data) {
        this.DisplayButton.CommandParameter = (data, this);
        this.DisplayButton.Content = "Get size";
        this.DisplayButton.Visibility = Visibility.Visible;
        this.DisplayText.Text = string.Empty;
        this.DisplayText.Visibility = Visibility.Collapsed;
    }
    private void SetSize(long size) {
        this.DisplayButton.Visibility = Visibility.Collapsed;
        this.DisplayText.Visibility = Visibility.Visible;
        this.DisplayText.Text = Formatter.SizeFormat(size);
    } 
    public CompressedSizeLabel() {
        InitializeComponent();
        this.DisplayButton.Command = _command;
    }
}
