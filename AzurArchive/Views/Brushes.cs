using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AzurArchive.Views;

public static class Brushes {
    public static readonly SolidColorBrush Red = new(Colors.Red);
    public static readonly SolidColorBrush Gray = new(Colors.Gray);
    public static readonly SolidColorBrush Silver = new(Colors.Silver);
    public static readonly SolidColorBrush Transparent = new(Colors.Transparent);
    public static readonly SolidColorBrush Gray0 = new(Color.FromArgb(0x0F, 0x80, 0x80, 0x80));
    public static readonly SolidColorBrush Gray1 = new(Color.FromArgb(0x1F, 0x80, 0x80, 0x80));
    public static readonly SolidColorBrush Gray2 = new(Color.FromArgb(0x2F, 0x80, 0x80, 0x80));
    public static readonly SolidColorBrush Gray3 = new(Color.FromArgb(0x3F, 0x80, 0x80, 0x80));

}
