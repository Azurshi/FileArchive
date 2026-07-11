using AzurArchive.Domain;
using AzurArchive.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;

namespace AzurArchive.Views.Controls.ListComponents;

public class ItemListHeaderSelectedEventArgs(OrderState order, ItemField field): EventArgs {
    public OrderState Order = order;
    public ItemField Field = field;
}
public sealed partial class ItemListColumnHeader: ItemListColumn {
    protected override void SetInfo(ItemColumnInfo? info) {
        base.SetInfo(info);
        this.InnerTextBlock.Text = info?.Name ?? string.Empty;
        this.TextContainer.MaxWidth = info?.Width ?? 64 - this.ResizeRect.Width;
    }
    protected override void OnColumnInfoPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        base.OnColumnInfoPropertyChanged(sender, e);
        if (e.PropertyName == nameof(ItemColumnInfo.Width)) {
            this.TextContainer.MaxWidth = ColumnInfo?.Width ?? 64 - this.ResizeRect.Width;
        }
    }
    // Others
    public event EventHandler<ItemListHeaderSelectedEventArgs>? HeaderSelected;
    private OrderState _state = OrderState.None;
    // Contructor
    public ItemListColumnHeader() {
        InitializeComponent();

    }
    public void ResetOrder() {
        this._state = OrderState.None;
        this.StateTextBlock.Text = "";
    }
    // Signals
    private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        this.Background = Brushes.Gray;
    }

    private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        this.Background = Brushes.Transparent;
    }

    private void OnTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) {
        if (this.ColumnInfo != null && this.ColumnInfo.OrderSupport) {
            OrderState state;
            if (this._state == OrderState.None) {
                state = OrderState.Ascending;
                this.StateTextBlock.Text = "^";
            }
            else if (this._state == OrderState.Ascending) {
                state = OrderState.Descending;
                this.StateTextBlock.Text = "v";
            }
            else {
                state = OrderState.None;
                this.StateTextBlock.Text = "";
            }
            this._state = state;
            ItemListHeaderSelectedEventArgs args = new(state, this.ColumnInfo.Field);
            this.HeaderSelected?.Invoke(this, args);
        }
    }

    private bool _isDragging;
    private double _startDragX;
    private double _startWidth;
    private void Rectangle_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        if (!_isDragging) {
            return;
        }
        var point = e.GetCurrentPoint(this);
        double deltaX = point.Position.X - _startDragX;
        if (this.ColumnInfo != null) {
            this.ColumnInfo.Width = Math.Max(TextContainer.ActualWidth + this.ResizeRect.ActualWidth, _startWidth + deltaX);
        }
    }

    private void Rectangle_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        _isDragging = true;
        var point = e.GetCurrentPoint(this);
        _startDragX = point.Position.X;
        _startWidth = this.Width;
        this.ResizeRect.CapturePointer(e.Pointer);
    }

    private void Rectangle_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        _isDragging = false;
        this.ResizeRect.ReleasePointerCaptures();
    }

    private void StateTextBlock_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) {
        if (this.ColumnInfo == null) {
            return;
        }
        this.ColumnInfo.Width = Math.Max(TextContainer.ActualWidth + this.ResizeRect.ActualWidth, this.ColumnInfo.Width);
    }
}
