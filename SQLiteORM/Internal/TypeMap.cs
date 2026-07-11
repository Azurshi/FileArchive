using SQLitePCL;
using System;
using System.Collections.Generic;
namespace SQLiteORM.Internal;

internal class SQLiteTypeEntry {
    public readonly string FieldKeyword;
    public readonly Func<sqlite3_stmt, int, object, int> Binder;
    public readonly Func<sqlite3_stmt, int, object> Reader;
    public SQLiteTypeEntry(string fieldKeyWord, Func<sqlite3_stmt, int, object, int> binder, Func<sqlite3_stmt, int, object> reader) {
        this.FieldKeyword = fieldKeyWord;
        this.Binder = binder;
        this.Reader = reader;
    }
}

internal static class TypeMap {
    public static readonly Dictionary<Type, SQLiteTypeEntry> Maps = new() {
        [typeof(int)] = new(
            SQLiteKeyword.Integer, 
            static (stmt, index, value) => raw.sqlite3_bind_int(stmt, index, (int)value),
            static (stmt, index) => raw.sqlite3_column_int(stmt, index)
        ),
        [typeof(long)] = new(
            SQLiteKeyword.Integer,
            static (stmt, index, value) => raw.sqlite3_bind_int64(stmt, index, (long)value),
            static (stmt, index) => raw.sqlite3_column_int64(stmt, index)
        ),
        [typeof(bool)] = new(
            SQLiteKeyword.Integer,
            static (stmt, index, value) => {
                if ((bool)value) {
                    return raw.sqlite3_bind_int(stmt, index, 1);
                }
                else {
                    return raw.sqlite3_bind_int(stmt, index, 0);
                }
            },
            static (stmt, index) => raw.sqlite3_column_int(stmt, index) != 0
        ),
        [typeof(float)] = new(
            SQLiteKeyword.Real,
            static (stmt, index, value) => raw.sqlite3_bind_double(stmt, index, (float)value),
            static (stmt, index) => (float)raw.sqlite3_column_double(stmt, index)
        ),
        [typeof(double)] = new(
            SQLiteKeyword.Real,
            static (stmt, index, value) => raw.sqlite3_bind_double(stmt, index, (double)value),
            static (stmt, index) => raw.sqlite3_column_double(stmt, index)
        ),
        [typeof(string)] = new(
            SQLiteKeyword.Text,
            static (stmt, index, value) => raw.sqlite3_bind_text(stmt, index, (string)value),
            static (stmt, index) => raw.sqlite3_column_text(stmt, index).utf8_to_string()
        ),
        [typeof(byte[])] = new(
            SQLiteKeyword.Blob,
            static (stmt, index, value) => raw.sqlite3_bind_blob(stmt, index, (byte[])value),
            static (stmt, index) => raw.sqlite3_column_blob(stmt, index).ToArray()
        ),
        [typeof(DateTime)] = new(
            SQLiteKeyword.Integer,
            static (stmt, index, value) => raw.sqlite3_bind_int64(stmt, index, ((DateTime)value).Ticks),
            static (stmt, index) => new DateTime(raw.sqlite3_column_int64(stmt, index))
        ),
        [typeof(DateOnly)] = new(
            SQLiteKeyword.Integer,
            static (stmt, index, value) => raw.sqlite3_bind_int(stmt, index, ((DateOnly)value).DayNumber),
            static (stmt, index) => DateOnly.FromDayNumber(raw.sqlite3_column_int(stmt, index))
        ),
        [typeof(TimeSpan)] = new(
            SQLiteKeyword.Integer,
            static (stmt, index, value) => raw.sqlite3_bind_int64(stmt, index, ((TimeSpan)value).Ticks),
            static (stmt, index) => new TimeSpan(raw.sqlite3_column_int64(stmt, index))
        )
    };
    public static int BindMap(sqlite3_stmt stmt, int index, object? value) {
        if (value == null) {
            return raw.sqlite3_bind_null(stmt, index);
        }
        Type type = value.GetType();
        Type? baseType = Nullable.GetUnderlyingType(type);
        if (baseType != null) {
            type = baseType;
        }
        if (Maps.TryGetValue(type, out var typeEntry)) {
            return typeEntry.Binder(stmt, index, value);
        }
        else {
            throw new Exception($"Type not supported: {type}");
        }
    }
    public static object? ReadMap(sqlite3_stmt stmt, int index, Type type) {
        if (raw.sqlite3_column_type(stmt, index) == raw.SQLITE_NULL) {
            return null;
        }
        var baseType = Nullable.GetUnderlyingType(type);
        if (baseType != null) {
            type = baseType;
        }
        if (Maps.TryGetValue(type, out var typeEntry)) {
            return typeEntry.Reader(stmt, index);
        }
        else {
            throw new Exception($"Type not supported: {type}");
        }
    }
}