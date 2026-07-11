using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AzurArchive.Views.Controls;

public sealed partial class TextLabel: UserControl {
    private static readonly Type ThisType = typeof(TextLabel);
    public string Text {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly DependencyProperty TextProperty = Utility.Create<string>(
        ThisType,
        string.Empty,
        callback: OnTextChanged
    );
    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        TextLabel This = (TextLabel)d;
        var text = (string)e.NewValue;
        This.TextContainer.Text = text;
    }
    public TextAlignment TextAlignment {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }
    public static readonly DependencyProperty TextAlignmentProperty = Utility.Create<double>(
        ThisType,
        12,
        callback: OnTextAlignmentChanged
    );

    private static async void OnTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        TextLabel This = (TextLabel)d;
        var textAlignment = (TextAlignment)e.NewValue;
        This.InnerTextBlock.TextAlignment = textAlignment;
    }
    public bool RightClickMenuEnabled {
        get => (bool)GetValue(RightClickMenuEnabledProperty);
        set => SetValue(RightClickMenuEnabledProperty, value);
    }
    public static readonly DependencyProperty RightClickMenuEnabledProperty = Utility.Create<bool>(
        ThisType,
        true
        //callback: OnTextSelectionEnabledChanged
    );
    //private static async void OnTextSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    //    TextLabel This = (TextLabel)d;
    //    var value = (bool)e.NewValue;
    //    This.InnerTextBlock.IsTextSelectionEnabled = value;
    //}
    public TextLabel() {
        InitializeComponent();
    }

    private void InnerTextBlock_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args) {
        if (!RightClickMenuEnabled) {
            args.Handled = true;
        }
    }
}
