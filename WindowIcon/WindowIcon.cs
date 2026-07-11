
using Microsoft.UI.Xaml.Media;

namespace WindowIcon;

public static class WindowIcon {
    public static ImageSource GetIcon(string path, bool small, bool isFile) {
        return PInvoke.GetIcon(path, small, isFile);
    }
}
