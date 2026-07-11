using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace AzurArchive.Views.Controls.ListComponents;

public sealed partial class ResizeRectangle: UserControl {
    public ResizeRectangle() {
        InitializeComponent();
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
