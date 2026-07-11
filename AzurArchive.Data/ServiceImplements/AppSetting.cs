using AzurArchive.Data.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.System;

namespace AzurArchive.Data.ServiceImplements;

internal partial class AppSetting: IAppSetting {
    private readonly string _saveFile;
    private readonly Dictionary<string, object?> _settings;
    private readonly JsonSerializerOptions _options;
    private DispatcherQueueTimer? _saveTimer;
    public AppSetting(DataManager manager) {
        this._saveTimer = null;
        this._saveFile = Path.Join(manager.SaveDirectory, "settings.json");
        this._options = new();
        this._settings = [];
        if (File.Exists(this._saveFile)) {
            try {
                using (var stream = File.OpenRead(this._saveFile)) {
                    var loadedSetting = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stream) ?? [];
                    foreach (var (key, value) in loadedSetting) {
                        this._settings[key] = value;
                    }
                }
            }
            catch {

            }
        }
    }
    private void SaveData() {
        string json = JsonSerializer.Serialize(this._settings, _options);
        string temp = this._saveFile + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _saveFile, true);
    }
    public void Dispose() {
        _saveTimer?.Stop();
        SaveData();
    }

    public T Get<T>(string key, T defaultValue) {
        if (this._settings.TryGetValue(key, out var valueObj)) {
            if (valueObj is T value) {
                return value;
            }
            else if (valueObj is JsonElement json) {
                value = JsonSerializer.Deserialize<T>(json, _options) ?? defaultValue;
                this._settings[key] = value;
                return value;
            }
            else {
                string foundType = "null";
                if (valueObj != null) {
                    foundType = valueObj.GetType().ToString();
                }
                throw new InvalidCastException($"Type mismatch. Required type: {typeof(T)}. Found type: {foundType}");
            }
        } else {
            return defaultValue;
        }
    }
    public void Set(string key, object? value) {
        this._settings[key] = value;
        ScheduleSave();
    }
    public bool Register(Type type, JsonConverter converter) {
        this._options.Converters.Add(converter);
        return true;
    }

    public bool Register<T>(JsonConverter<T> converter) {
        this._options.Converters.Add(converter);
        return true;
    }

    private void ScheduleSave() {
        if (this._saveTimer == null) {
            this._saveTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            this._saveTimer.Interval = TimeSpan.FromMilliseconds(Config.SaveDelayMs);
            this._saveTimer.Tick += (_, _) => {
                _saveTimer.Stop();
                SaveData();
            };
        }
        else {
            _saveTimer.Stop();
            _saveTimer.Start();
        }
    }
}
