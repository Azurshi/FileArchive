using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace AzurArchive.Core;

public static class TaskExtensions {
    public static async void FireAndForgetAsync(this Task task, IErrorHandler? handler = null) {
        try {
            await task;
        }
        catch (Exception ex) {
            handler?.HandleError(ex);
        }
    }
}
public static class VisibilityExtensions {
    public static Visibility Map(this Visibility visibility, bool value) {
        if (value) {
            return Visibility.Visible;
        }
        else {
            return Visibility.Collapsed;
        }
    }
    public static Visibility Map(bool value) {
        if (value) {
            return Visibility.Visible;
        }
        else {
            return Visibility.Collapsed;
        }
    }
}