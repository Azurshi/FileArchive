using AzurArchive.Core;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzurArchive.ViewModels.Items;

public enum ItemField {
    None,
    Id,
    Name,

    CreationTime,
    ModifiedTime,
    OriginalSize,
    CompressedSize,

    Menu
}

public partial class ItemColumnInfo: ObservableObject {
    public string Name {
        get => Field switch {
            ItemField.Id => "Id",
            ItemField.Name => "Name",
            ItemField.CreationTime => "Creation time",
            ItemField.ModifiedTime => "Modified time",
            ItemField.OriginalSize => "Size",
            ItemField.CompressedSize => "Compressed size",
            ItemField.Menu => "Menu",
            _ => string.Empty
        };
    }
    public bool OrderSupport {
        get => Field switch {
            ItemField.Menu => false,
            _ => true
        };
    }
    public ItemField Field { get; init; }
    private double _width;
    public double Width {
        get => _width;
        set {
            if (_width != value) {
                _width = value;
                OnPropertyChanged();
            }
        }
    }
    private Visibility _visibility;
    public Visibility Visibility {
        get => _visibility;
        set {
            if (_visibility != value) {
                _visibility = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
    public bool IsVisible {
        get => _visibility == Visibility.Visible;
        set {
            if (IsVisible == value) {
                return;
            }
            if (value) {
                _visibility = Visibility.Visible;
            }
            else {
                _visibility = Visibility.Collapsed;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(Visibility));
        }
    }
    private int _columnIndex;
    public int ColumnIndex {
        get => _columnIndex;
        set {
            if (_columnIndex != value) {
                _columnIndex = value;
                OnPropertyChanged();
            }
        }
    }
    public ItemColumnInfo(ItemField field, double width, Visibility visibility, int columnIndex) {
        Field = field;
        _width = width;
        _visibility = visibility;
        _columnIndex = columnIndex;
    }
    public ItemColumnInfo(ItemField field, double width, bool visible, int columnIndex) {
        Field = field;
        _width = width;
        _columnIndex = columnIndex;
        if (visible) {
            _visibility = Visibility.Visible;
        }
        else {
            _visibility = Visibility.Collapsed;
        }
    }
    public ItemColumnInfo(ItemField field, double width, int columnIndex) : this(field, width, Visibility.Collapsed, columnIndex) { }
}

public sealed class ItemColumnInfoJsonConverter: JsonConverter<ItemColumnInfo> {
    public override ItemColumnInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader)) {
            JsonElement root = doc.RootElement;
            ItemField field = root.GetProperty("field").Deserialize<ItemField>(options);
            double width = root.GetProperty("width").GetDouble();
            bool visible = root.GetProperty("visible").GetBoolean();
            int index = root.GetProperty("index").GetInt32();
            return new(field, width, visible, index);
        }
    }
    public override void Write(Utf8JsonWriter writer, ItemColumnInfo value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        writer.WritePropertyName("field");
        JsonSerializer.Serialize(writer, value.Field, options);
        writer.WriteNumber("width", value.Width);
        writer.WriteBoolean("visible", value.Visibility == Visibility.Visible);
        writer.WriteNumber("index", value.ColumnIndex);
        writer.WriteEndObject();
    }
}
public partial class ItemHeaderInfo: ObservableObject, IDisposable {
    public event EventHandler? SaveRequest;
    private static readonly List<ValueTuple<ItemField, double, bool>> DefaultValues = [
        (ItemField.Id, 96, false),
        (ItemField.Name, 256, true),
        (ItemField.CreationTime, 96, false),
        (ItemField.ModifiedTime, 96, false),
        (ItemField.OriginalSize, 64, false),
        (ItemField.CompressedSize, 64, false),
        (ItemField.Menu, 128, true)
        ];
    public int ColumnCount => this._columns.Count;
    public int VisibleColumnCount => this.VisibleColumns.Count;
    public IReadOnlyList<ItemColumnInfo> VisibleColumns {
        get {
            List<ItemColumnInfo> result = [];
            foreach (var column in this._columns) {
                if (column.IsVisible) {
                    result.Add(column);
                }
            }
            return result;
        }
    }
    public ItemColumnInfo IdColumn => this[ItemField.Id];
    public ItemColumnInfo NameColumn => this[ItemField.Name];
    public ItemColumnInfo CreationTimeColumn => this[ItemField.CreationTime];
    public ItemColumnInfo ModifiedTimeColumn => this[ItemField.ModifiedTime];
    public ItemColumnInfo OriginalSizeColumn => this[ItemField.OriginalSize];
    public ItemColumnInfo CompressedSizeColumn => this[ItemField.CompressedSize];
    public ItemColumnInfo MenuColumn => this[ItemField.Menu];
    private readonly List<ItemColumnInfo> _columns;
    public ItemColumnInfo this[ItemField field] {
        get {
            foreach (var column in _columns) {
                if (column.Field == field) {
                    return column;
                }
            }
            throw new KeyNotFoundException();
        }
    }
    public ItemColumnInfo this[int index] => this._columns[index];
    public ItemHeaderInfo() {
        this._columns = [];
        LoadDefault();
        foreach (var column in this._columns) {
            column.PropertyChanged += Info_PropertyChanged;
        }
    }
    public ItemHeaderInfo(IReadOnlyList<ItemColumnInfo> columns) {
        if (columns.Count == DefaultValues.Count) {
            this._columns = new(columns);
        }
        else {
            this._columns = [];
            LoadDefault();
        }
        foreach (var column in this._columns) {
            column.PropertyChanged += Info_PropertyChanged;
        }
    }
    private void LoadDefault() {
        for (int i = 0; i < DefaultValues.Count; i++) {
            var (field, width, visible) = DefaultValues[i];
            this._columns.Add(new(field, width, visible, i));
        }
    }

    private void Info_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        SaveRequest?.Invoke(this, EventArgs.Empty);
    }

    internal IReadOnlyList<ItemColumnInfo> GetColumnsData() {
        return this._columns;
    }
    public int IndexOf(ItemField field) {
        for (int i = 0; i < _columns.Count; i++) {
            if (_columns[i].Field == field) {
                return i;
            }
        }
        throw new KeyNotFoundException();
    }
    public void Move(ItemField field, int index) {
        int fromIndex = -1;
        for (int i = 0; i < _columns.Count; i++) {
            if (_columns[i].Field == field) {
                fromIndex = i;
            }
        }
        if (fromIndex < 0) {
            throw new KeyNotFoundException();
        }
        var info = this._columns[fromIndex];
        this._columns.RemoveAt(fromIndex);
        this._columns.Insert(index, info);
        for (int i = 0; i < _columns.Count; i++) {
            _columns[i].ColumnIndex = i;
        }
    }

    public void Dispose() {
        foreach (var column in this._columns) {
            column.PropertyChanged -= Info_PropertyChanged;
        }
    }
}
public sealed class ItemHeaderInfoJsonConverter: JsonConverter<ItemHeaderInfo> {
    public override ItemHeaderInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var raw = reader.GetString() ?? "{}";
        var columns = JsonSerializer.Deserialize<List<ItemColumnInfo>>(raw, options);
        return new(columns ?? []);
    }
    public override void Write(Utf8JsonWriter writer, ItemHeaderInfo value, JsonSerializerOptions options) {
        writer.WriteStringValue(JsonSerializer.Serialize(value.GetColumnsData(), options));
    }
}