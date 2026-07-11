using System;
using System.Text.Json.Serialization;

namespace AzurArchive.Data.Services;

public interface IAppSetting: IDisposable {
    public void Set(string key, object? value);
    public T Get<T>(string key, T defaultValue);
    public bool Register(Type type, JsonConverter converter);
    public bool Register<T>(JsonConverter<T> converter);
}
