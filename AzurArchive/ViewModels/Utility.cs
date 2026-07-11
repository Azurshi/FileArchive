using AzurArchive.Core;
using AzurArchive.Domain;
using Microsoft.UI.Xaml;
using System;

namespace AzurArchive.ViewModels;

public static class Formatter {
    public static string SizeFormat(long value) {
        const long Step = 1000L;
        const long KB = Step;
        const long MB = Step * Step;
        const long GB = Step * Step * Step;
        const long TB = Step * Step * Step * Step;
        if (value <= 0) {
            return string.Empty;
        }
        else if (value < KB) {
            return $"{value} B";
        }
        else if (value < MB) {
            return ((double)value / KB).ToString("0.##") + " KB";
        }
        else if (value < GB) {
            return ((double)value / MB).ToString("0.##") + " MB";
        }
        else if (value < TB) {
            return ((double)value / GB).ToString("0.##") + " GB";
        }
        else {
            return ((double)value / TB).ToString("0.##") + " TB";
        }
    }
    public static string SizeFormatRound(long value) {
        const long Step = 1000L;
        const long KB = Step;
        const long MB = Step * Step;
        const long GB = Step * Step * Step;
        const long TB = Step * Step * Step * Step;
        if (value <= 0) {
            return string.Empty;
        }
        else if (value < KB) {
            return $"{value} B";
        }
        else if (value < MB) {
            return ((double)value / KB).ToString("0") + " KB";
        }
        else if (value < GB) {
            return ((double)value / MB).ToString("0") + " MB";
        }
        else if (value < TB) {
            return ((double)value / GB).ToString("0" ) + " GB";
        }
        else {
            return ((double)value / TB).ToString("0") + " TB";
        }
    }
    public static string Format(DateTime? value) {
        if (value == null) {
            return string.Empty;
        }
        else {
            return value.Value.ToString("G");
        }
    }
}