using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.CompilerServices;
namespace AzurArchive.Views;

public static class Utility {
    public static DependencyProperty Create<IType>(Type ownerType, object? defaultValue = null, PropertyChangedCallback? callback = null, [CallerMemberName] string propertyName = "") {
        if (propertyName.EndsWith("Property")) {
            propertyName = propertyName[..^8];
        }
        return DependencyProperty.Register(
            propertyName,
            typeof(IType),
            ownerType,
            new PropertyMetadata(
                defaultValue,
                callback
            ));
    }
}
public partial class BindingProxy: FrameworkElement {
    public object? Data {
        get => (object?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
    public static readonly DependencyProperty DataProperty
        = Utility.Create<object?>(typeof(BindingProxy), nameof(Data), null);
}
public static class GridHelperAuto {
    public static readonly DependencyProperty AutoColumnsProperty =
        Utility.Create<int>(typeof(GridHelperAuto), 0, OnAutoColumnsPropertyChanged);
    public static int GetAutoColumns(DependencyObject obj)
        => (int)obj.GetValue(AutoColumnsProperty);
    public static void SetAutoColumns(DependencyObject obj, int value)
        => obj.SetValue(AutoColumnsProperty, value);
    private static void OnAutoColumnsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not Grid grid) {
            return;
        }
        grid.ColumnDefinitions.Clear();
        int count = (int)e.NewValue;
        for(int i=0;i<count;i++) {
            grid.ColumnDefinitions.Add(new ColumnDefinition() {
                Width = GridLength.Auto
            });
        }
    }
    public static readonly DependencyProperty AutoRowsProperty =
    Utility.Create<int>(typeof(GridHelperAuto), 0, OnAutoRowsPropertyChanged);
    public static int GetAutoRows(DependencyObject obj)
        => (int)obj.GetValue(AutoRowsProperty);
    public static void SetAutoRows(DependencyObject obj, int value)
        => obj.SetValue(AutoRowsProperty, value);
    private static void OnAutoRowsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not Grid grid) {
            return;
        }
        grid.RowDefinitions.Clear();
        int count = (int)e.NewValue;
        for (int i = 0; i < count; i++) {
            grid.RowDefinitions.Add(new RowDefinition() {
                Height = GridLength.Auto
            });
        }
    }
}

public static class GridHelperStar {
    public static readonly DependencyProperty AutoColumnsProperty =
    Utility.Create<int>(typeof(GridHelperStar), 0, OnAutoColumnsPropertyChanged);
    public static int GetAutoColumns(DependencyObject obj)
        => (int)obj.GetValue(AutoColumnsProperty);
    public static void SetAutoColumns(DependencyObject obj, int value)
        => obj.SetValue(AutoColumnsProperty, value);
    private static void OnAutoColumnsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not Grid grid) {
            return;
        }
        grid.ColumnDefinitions.Clear();
        int count = (int)e.NewValue;
        for (int i = 0; i < count; i++) {
            grid.ColumnDefinitions.Add(new ColumnDefinition() {
                Width = new GridLength(1, GridUnitType.Star)
            });
        }
    }
    public static readonly DependencyProperty AutoRowsProperty =
        Utility.Create<int>(typeof(GridHelperStar), 0, OnAutoRowsPropertyChanged);
    public static int GetAutoRows(DependencyObject obj)
        => (int)obj.GetValue(AutoRowsProperty);
    public static void SetAutoRows(DependencyObject obj, int value)
        => obj.SetValue(AutoRowsProperty, value);
    private static void OnAutoRowsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not Grid grid) {
            return;
        }
        grid.RowDefinitions.Clear();
        int count = (int)e.NewValue;
        for (int i = 0; i < count; i++) {
            grid.RowDefinitions.Add(new RowDefinition() {
                Height = new GridLength(1, GridUnitType.Star)
            });
        }
    }
}