using AzurArchive.Domain;
using AzurArchive.ViewModels;
using AzurArchive.ViewModels.Items;
using AzurArchive.Views.Controls.ListComponents;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace AzurArchive.Views.Controls;

public sealed partial class FileExplorer: UserControl {
    private static readonly Type ThisType = typeof(FileExplorer);
    public ItemHeaderInfo HeaderInfo {
        get => (ItemHeaderInfo)GetValue(HeaderInfoProperty);
        set => SetValue(HeaderInfoProperty, value);
    }
    public static readonly DependencyProperty HeaderInfoProperty
        = Utility.Create<ItemHeaderInfo>(ThisType, new ItemHeaderInfo([]));
    public ObservableCollectionExtendAdvanced<ExplorerItemViewModel> ItemsSource {
        get => (ObservableCollectionExtendAdvanced<ExplorerItemViewModel>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    public static readonly DependencyProperty ItemsSourceProperty
        = Utility.Create<ObservableCollectionExtendAdvanced<ExplorerItemViewModel>>(
            ThisType, new ObservableCollectionExtendAdvanced<ExplorerItemViewModel>(),
            OnItemsSourcePropertyChanged
        );
    private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var This = (FileExplorer)d;
        var oldValue = (ObservableCollectionExtendAdvanced<ExplorerItemViewModel>)e.OldValue;
        var newValue = (ObservableCollectionExtendAdvanced<ExplorerItemViewModel>)e.NewValue;
        This.Container.ItemsSource = newValue.Items;
    }
    public ICommand? SelectItemsCommand {
        get => (ICommand?)GetValue(SelectItemsCommandProperty);
        set => SetValue(SelectItemsCommandProperty, value);
    }
    public static readonly DependencyProperty SelectItemsCommandProperty = Utility.Create<ICommand?>(ThisType);
    public ICommand? DeselectItemsCommand {
        get => (ICommand?)GetValue(DeselectItemsCommandProperty);
        set => SetValue(DeselectItemsCommandProperty, value);
    }
    public static readonly DependencyProperty DeselectItemsCommandProperty = Utility.Create<ICommand>(ThisType);
    public ICommand? OpenItemCommand {
        get => (ICommand?)GetValue(OpenItemCommandProperty);
        set => SetValue(OpenItemCommandProperty, value);
    }
    public static readonly DependencyProperty OpenItemCommandProperty = Utility.Create<ICommand?>(ThisType);
    public FileExplorer() {
        InitializeComponent();
    }
    private void Refresh() {

    }
    private ItemColumnInfo? _draggingColumn;
    private void ItemListColumnHeader_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        var element = (ItemListColumnHeader)sender;
        _draggingColumn = element.ColumnInfo;
        element.CapturePointer(e.Pointer);
    }

    private void ItemListColumnHeader_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        if (_draggingColumn == null) {
            return;
        }
        var p = e.GetCurrentPoint(HeaderColumns).Position;
        int targetIndex = GetInsertionIndex(p.X);
        int currentIndex = HeaderInfo.IndexOf(_draggingColumn.Field);
        if (targetIndex > currentIndex) {
            targetIndex--;
        }
        //Debug.WriteLine($"{currentIndex} {targetIndex}");
        if (targetIndex != currentIndex) {
            HeaderInfo.Move(_draggingColumn.Field, targetIndex);
        }
    }
    private int GetInsertionIndex(double x) {
        double left = 0;
        //Debug.WriteLine(x);
        for (int i = 0; i < HeaderInfo.ColumnCount; i++) {
            if (HeaderInfo[i].IsVisible) {
                double width = HeaderInfo[i].Width;
                double center = left + width / 2;
                if (x < center) {
                    return i;
                }
                left += width;
            }
        }
        return HeaderInfo.ColumnCount - 1;
    }
    private void ItemListColumnHeader_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        var element = (ItemListColumnHeader)sender;
        element.ReleasePointerCaptures();
        _draggingColumn = null;
    }

    private void ItemListColumnHeader_HeaderSelected(object sender, ItemListHeaderSelectedEventArgs e) {
        var element = (ItemListColumnHeader)sender;
        if (element.ColumnInfo?.OrderSupport ?? false) {
            foreach (var item in this.HeaderColumns.Children) {
                if (item is ItemListColumnHeader header) {
                    if (item != element) {
                        header.ResetOrder();
                    }
                }
            }
            _lastSort = e;
            SortByField();
        }
    }
    private ItemListHeaderSelectedEventArgs? _lastSort;
    private IReadOnlyList<ExplorerItemViewModel> Sorter(IReadOnlyList<ExplorerItemViewModel> items) {
        if (_lastSort == null || _lastSort.Order == OrderState.None) {
            return items;
        }
        else {
            items = items.OrderBy(c => c.GetRawValue(_lastSort.Field)).ToList();
            if (_lastSort.Order == OrderState.Descending) {
                return items.Reverse().ToList();
            }
            else {
                return items;
            }
        }
    }
    public void SortByField() {
        ItemsSource.SetFilter(Sorter, null);
    }
    public event EventHandler<ExplorerItemViewModel>? ItemDoubleClicked;
    private void Grid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) {
        if (sender is Grid grid) {
            if (grid.DataContext is ExplorerItemViewModel itemVm) {
                ItemDoubleClicked?.Invoke(this, itemVm);
            }
        }
    }

    private void Container_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        List<ExplorerItemViewModel> addedItems = [];
        List<ExplorerItemViewModel> removedItems = [];
        foreach(var item in e.AddedItems) {
            if (item is ExplorerItemViewModel vm) {
                addedItems.Add(vm);
            }
        }
        foreach(var item in e.RemovedItems) {
            if (item is ExplorerItemViewModel vm) {
                removedItems.Add(vm);
            }
        }
        DeselectItemsCommand?.Execute(removedItems);
        SelectItemsCommand?.Execute(addedItems);
    }

    private void Grid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e) {
        if (sender is Grid grid &&
            grid.ContextFlyout is FlyoutBase flyout) {
            var option = new FlyoutShowOptions() {
                Position = e.GetPosition(grid)
            };
            flyout.ShowAt(grid, option);
            e.Handled = true;
        }
    }

    private void Menu_Open_Clicked(object sender, RoutedEventArgs e) {
        if (_targetGrid != null) {
            if (_targetGrid.DataContext is ExplorerItemViewModel vm) {
                OpenItemCommand?.Execute(vm);
            }
        }
    }
    private Grid? _targetGrid;
    private void MenuFlyout_Opening(object sender, object e) {
        _targetGrid = (Grid)((MenuFlyout)sender).Target;
    }

    private void MenuFlyout_Closed(object sender, object e) {
        _targetGrid = null;
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) {
        if (_targetGrid != null) {
            foreach(var children in _targetGrid.Children) {
                if (children is ItemListColumn column) {
                    if (column.ColumnInfo != null && column.ColumnInfo.Field == ItemField.Name) {
                        if (column.Content is NameLabel label) {
                            label.Rename();
                        }
                    }
                }
            }
        }
    }
}
