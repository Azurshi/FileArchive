using AzurArchive.Domain;
using Microsoft.UI.Xaml.Media;

namespace AzurArchive.Services;

public partial class IconService {
    private readonly IconCache _cache;
    public IconService() {
        this._cache = new(Config.IconCacheCapacity);
    }
    public ImageSource GetSmall(string name, bool isFile) {
        string extension;
        if (isFile) {
            extension = "." + string.Join(".", name.Split(".")[..^1]);
        }
        else {
            extension = "folder";
        }
        if (_cache.TryGetSmall(extension, out var image)) {
            return image!;
        } else {
            image = WindowIcon.WindowIcon.GetIcon(name, true, isFile);
            _cache.AddSmall(extension, image);
            return image;
        }
    }
    public ImageSource GetLarge(string name, bool isFile) {
        string extension;
        if (isFile) {
            extension = "." + string.Join(".", name.Split(".")[..^1]);
        }
        else {
            extension = "folder";
        }
        if (_cache.TryGetLarge(extension, out var image)) {
            return image!;
        }
        else {
            image = WindowIcon.WindowIcon.GetIcon(name, false, isFile);
            _cache.AddLarge(extension, image);
            return image;
        }
    }
}
