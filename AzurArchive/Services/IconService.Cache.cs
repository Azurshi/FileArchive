using Microsoft.Extensions.Caching.Memory;
using Microsoft.UI.Xaml.Media;
using System;

namespace AzurArchive.Services;

public partial class IconService {
    private readonly struct IconKey(string extension, int size) {
        public readonly string Extension = extension;
        public readonly int Size = size;
    }
    private const int SmallSize = 1;
    private const int LargeSize = 4;
    private class IconCache {
        private readonly MemoryCache _cache;
        public IconCache(int capacity) {
            MemoryCacheOptions option = new() {
                ExpirationScanFrequency = TimeSpan.FromSeconds(30),
                SizeLimit = capacity,
                CompactionPercentage = 0.1
            };
            _cache = new(option);
        }
        public void AddSmall(string extension, ImageSource source) {
            MemoryCacheEntryOptions options = new() {
                Size = SmallSize,
            };
            _cache.Set(new IconKey(extension, SmallSize), source, options);
        }
        public void AddLarge(string extension, ImageSource source) {
            MemoryCacheEntryOptions options = new() {
                Size = LargeSize,
            };
            _cache.Set(new IconKey(extension, LargeSize), source, options);
        }
        public bool TryGetSmall(string extension, out ImageSource? image) {
            if (_cache.TryGetValue(new IconKey(extension, SmallSize), out var result)) {
                if (result is ImageSource ImageSource) {
                    image = ImageSource;
                    return true;
                }
            }
            image = null;
            return false;
        }
        public bool TryGetLarge(string extension, out ImageSource? image) {
            if (_cache.TryGetValue(new IconKey(extension, LargeSize), out var result)) {
                if (result is ImageSource ImageSource) {
                    image = ImageSource;
                    return true;
                }
            }
            image = null;
            return false;
        }
    }
}
