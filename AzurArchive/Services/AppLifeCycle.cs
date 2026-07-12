using AzurArchive.Core;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AzurArchive.Services;

public static class AppLifeCycle {
    private class Handler {
        private readonly Action? _syncHandler;
        private readonly Func<Task>? _asyncHandler;
        public Handler(Action handler) {
            this._syncHandler = handler;
            this._asyncHandler = null;
        }
        public Handler(Func<Task> handler) {
            this._syncHandler = null;
            this._asyncHandler = handler;
        }
        public async Task Invoke() {
            _syncHandler?.Invoke();
            if (_asyncHandler != null) {
                await _asyncHandler.Invoke();
            }
        }
    }
    private static int _windowCount = 0;
    public static int WindowCount => _windowCount;
    private static readonly List<Handler> _closeHandlers = [];
    private static readonly List<Handler> _startHandlers = [];
    private static readonly List<Handler> _uiHandlers = [];
    private static bool _exited = false;
    public static bool Exited => _exited;
    public static IServiceProvider Service
        => ((App)Application.Current).Services ?? throw new NotInitializedException();
    public static void OnCreateWindow() {
        _windowCount += 1;
    }
    public static async Task OnCloseWindow() {
        _windowCount -= 1;
        if (_windowCount == 0) {
            await CloseApp();
        }
    }
    public static async Task StartApp() {
        Debug.WriteLine("!!!---App starting---!!!");
        Stopwatch sw = new();
        sw.Start();
        foreach (var handler in _startHandlers) {
            await handler.Invoke();
        }
        sw.Stop();
        _startHandlers.Clear();
        Debug.WriteLine($"!!!---App started---!!! {sw.ElapsedMilliseconds} ms");
    }
    public static async Task AfterFirstWindow() {
        Debug.WriteLine("!!!---Window created---!!!");
        Stopwatch sw = new();
        sw.Start();
        foreach (var handler in _uiHandlers) {
            await handler.Invoke();
        }
        sw.Stop();
        _uiHandlers.Clear();
        Debug.WriteLine($"!!!---After window created---!!! {sw.ElapsedMilliseconds} ms");
    }
    private static async Task CloseApp() {
        if (_exited) {
            Debug.WriteLine("Duplicated exit");
            return;
        }
        Debug.WriteLine("!!!---App closing---!!!");
        _exited = true;
        Stopwatch sw = new();
        sw.Start();
        foreach (var handler in _closeHandlers) {
            await handler.Invoke();
        }
        sw.Stop();
        _closeHandlers.Clear();
        Debug.WriteLine($"!!!---App closed---!!! {sw.ElapsedMilliseconds} ms");
        Application.Current.Exit();
    }

    public static void RegisterAppClose(Action action) {
        _closeHandlers.Add(new(action));
    }
    public static void RegisterAppClose(Func<Task> action) {
        _closeHandlers.Add(new(action));
    }
    public static void RegisterAppStart(Action action) {
        _startHandlers.Add(new(action));
    }
    public static void RegisterAppStart(Func<Task> action) {
        _startHandlers.Add(new(action));
    }
    public static void RegisterAfterWindow(Action action) {
        _uiHandlers.Add(new(action));
    }
    public static void RegisterAfterWindow(Func<Task> action) {
        _uiHandlers.Add(new(action));
    }
}
