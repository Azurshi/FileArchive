using AzurArchive.ViewModels.Items;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AzurArchive.Views.Controls.ListComponents;

public partial class ItemListColumn: UserControl {
    private static readonly Type ThisType = typeof(ItemListColumn);
    public ItemColumnInfo? ColumnInfo {
        get => (ItemColumnInfo?)GetValue(ColumnInfoProperty);
        set => SetValue(ColumnInfoProperty, value);
    }
    public static readonly DependencyProperty ColumnInfoProperty
        = Utility.Create<ItemColumnInfo?>(ThisType, null, OnColumnInfoPropertyChanged);
    private static void OnColumnInfoPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var This = (ItemListColumn)d;
        var oldValue = (ItemColumnInfo?)e.OldValue;
        oldValue?.PropertyChanged -= This.OnColumnInfoPropertyChanged;
        var newValue = (ItemColumnInfo?)e.NewValue;
        newValue?.PropertyChanged += This.OnColumnInfoPropertyChanged;
        This.SetInfo(newValue);
    }
    protected virtual void SetInfo(ItemColumnInfo? info) {
        this.Width = info?.Width ?? 64;
        this.Visibility = info?.Visibility ?? Visibility.Visible;
        this.Content?.Visibility = this.Visibility;
        Grid.SetColumn(this, info?.ColumnIndex ?? 0);
    }
    protected virtual void OnColumnInfoPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(ItemColumnInfo.Width):
                this.Width = ColumnInfo?.Width ?? 64;
                break;
            case nameof(ItemColumnInfo.Visibility):
                this.Visibility = ColumnInfo?.Visibility ?? Visibility.Visible;
                this.Content?.Visibility = this.Visibility;
                break;
            case nameof(ItemColumnInfo.ColumnIndex):
                Grid.SetColumn(this, ColumnInfo?.ColumnIndex ?? 0);
                break;
            default:
                break;
        }
    }
    public ItemListColumn() {
        InitializeComponent();
    }
}
