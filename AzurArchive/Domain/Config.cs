using System.Collections.Generic;
using System.Numerics;

namespace AzurArchive.Domain;

public static class Config {
    //public const string AppPath = "D:\\Workstation\\Storage\\WinUI\\AzurArchiveM";
    //public const string DefaultImagePath = $"{AppPath}\\Assets\\Images\\default_icon.png";
    public const double GridColumns = 2;
    public const double GridRows = 2.5;
    public const int MaxHomePreview = 100;
    public const int MaxTagPreview = 5;
    public const int MinQueryLength = 3;
    public const int MinNameLength = 3;
    public const int NewImportMinCount = 10;
    public const int CollectionDisplayItemsAmount = 10;
    public const float ScrollSensitiveY = 5.0f;
    public const int MaxFrame = 1000000;
    public static readonly int IconCacheCapacity = 100;
    public static readonly IReadOnlyList<float> CropWidthOptions = [0.0f, 0.2f, 0.4f, 0.5f, 0.6f, 0.8f];
    public static readonly int MaxImageResolution = 16384;
    public static readonly Vector2 OriginalSize = new(MaxImageResolution, 1080);
    public static readonly Vector2 ScreenSize = new(1920, 1080);
    public static readonly Vector2 LargeIconSize = new(1024, 768);
    public static readonly Vector2 MediumIconSize = new(512, 512);
    public static readonly Vector2 SmallIconSize = new(128, 128);

    public static readonly int[] PageSizes = [5, 10, 25, 50, 100];
    public const int QueryLimit = 999999999;
    public const int UIItemDelayMs = 10;
}