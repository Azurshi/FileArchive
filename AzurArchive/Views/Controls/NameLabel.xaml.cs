using AzurArchive.Data.Services;
using AzurArchive.Domain;
using AzurArchive.Services;
using AzurArchive.ViewModels.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Windows.System;

namespace AzurArchive.Views.Controls;

public sealed partial class NameLabel: UserControl {
    private static readonly Type ThisType = typeof(NameLabel);
    public ExplorerItemViewModel? Data {
        get => (ExplorerItemViewModel?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
    public static readonly DependencyProperty DataProperty
        = Utility.Create<ExplorerItemViewModel?>(ThisType, null, (d, e) => {
            var This = (NameLabel)d;
            var newValue = (ExplorerItemViewModel?)e.NewValue;
            This.SetData(newValue);
        });
    private void SetData(ExplorerItemViewModel? data) {
        this.TextDisplay.Text = data?.Name ?? string.Empty;
        this.TextEdit.Text = data?.Name ?? string.Empty;
        if (data != null) {
            var iconService = AppLifeCycle.Service.GetRequiredService<IconService>();
            this.IconDisplay.Source = iconService.GetLarge(data.Name, data.IsFile);
        } else {
            this.IconDisplay.Source = null;
        }
    }

    public NameLabel() {
        InitializeComponent();
        this.ChangeMode(false);
    }
    private void ChangeMode(bool isEditing) {
        if (isEditing) {
            this.TextEdit.Focus(FocusState.Keyboard);
            string text = this.TextEdit.Text;
            text = text.Split(".")[0];
            this.TextEdit.Select(0, text.Length);
            this.TextDisplay.Visibility = Visibility.Collapsed;
            this.TextEdit.Visibility = Visibility.Visible;
        }
        else {
            this.TextDisplay.Visibility = Visibility.Visible;
            this.TextEdit.Visibility = Visibility.Collapsed;
        }
    }

    private void TextEdit_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) {
        if (e.Key == VirtualKey.Enter) {
            Debug.WriteLine("Enter pressed");
            ChangeMode(false); // This trigger LostFocus event
        }
    }

    private async void TextEdit_LostFocus(object sender, RoutedEventArgs e) {
        Debug.WriteLine("Lost focus");
        ChangeMode(false);
        var data = Data;
        if (data == null) {
            return;
        }
        var newName = this.TextEdit.Text.Trim();
        if (newName.Length < Config.MinNameLength || data.Name.Equals(newName)) {
            return;
        }
        var provider = AppLifeCycle.Service;
        if (data.IsFile) {
            var fileService = provider.GetRequiredService<IFileService>();
            await fileService.RenameFile(data.Id, newName);
        }
        else {
            var folderService = provider.GetRequiredService<IFolderService>();
            await folderService.RenameFolder(data.Id, newName);
        }
    }

    //private void TextDisplay_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
    //    this.ChangeMode(true);
    //    this.TextEdit.Focus(FocusState.Keyboard);
    //    string text = this.TextEdit.Text;
    //    text = text.Split(".")[0];
    //    this.TextEdit.Select(0, text.Length);
        
    //}
    public void Rename() {
        ChangeMode(true);
    }
}
