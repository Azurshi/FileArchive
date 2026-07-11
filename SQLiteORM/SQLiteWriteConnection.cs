using SQLiteORM.Internal;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SQLiteORM.ORM;
using System.Diagnostics;

namespace SQLiteORM;

public partial class SQLiteWriteConnection : SQLiteReadConnection {
    private const int Flags = raw.SQLITE_OPEN_READWRITE | raw.SQLITE_OPEN_CREATE | raw.SQLITE_OPEN_NOMUTEX;
    private readonly SemaphoreSlim _semaphore = new(1);
    public SQLiteWriteConnection(string filePath) : base(Flags, filePath) {
        EnableWAL();
    }
    private void EnableWAL() {
        this.Execute("PRAGMA journal_mode=WAL;");
    }
    public void IncrementalVacuum(int? count) {
        string query; ;
        if (count == null) {
            query = "PRAGMA incremental_vacuum;";
        } else {
            query = $"PRAGMA incremental_vacuum({count.Value})";
        }
        using sqlite3_stmt stmt = Preprate(query);
        while (true) {
            int rc = raw.sqlite3_step(stmt);
            switch (rc) {
                case raw.SQLITE_ROW:
                    // Continue
                    break;
                case raw.SQLITE_DONE:
                    // Completed
                    return;
                default:
                    throw new Exception(raw.sqlite3_errmsg(this._database).utf8_to_string());
            }
        }
    }
    private static readonly Dictionary<Type, CachedInsertReflection> _insertCaches = [];
    private static readonly Dictionary<Type, CachedUpdateReflection> _updateCaches = [];
    public List<object[]> Insert<T>(List<T> items, bool returnPrimaryKey) where T : class {
        Type type = typeof(T);
        if (!_insertCaches.TryGetValue(type, out var cachedReflection)) {
            cachedReflection = InsertORMConstructor.ConstructInsertTableString(type);
            _insertCaches[type] = cachedReflection;
        }
        string sql = cachedReflection.GetSQL(returnPrimaryKey);
        using sqlite3_stmt stmt = Preprate(sql);
        List<object[]> primaryKeys = [];
        foreach (var item in items) {
            for (int i = 0; i < cachedReflection.getters.Count; i++) {
                object? value = cachedReflection.getters[i](item);
                TypeMap.BindMap(stmt, i + 1, value);
            }
            int rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_DONE && rc != raw.SQLITE_ROW) {
                Debug.WriteLine("Insert error: " + raw.sqlite3_errmsg(_database).utf8_to_string());
            }
            if (returnPrimaryKey) {
                object[] primaryKey = new object[cachedReflection.primaryKeyTypes.Count];
                for (int j = 0; j < primaryKey.Length; j++) {
                    // Primary Key should not null
                    primaryKey[j] = TypeMap.ReadMap(stmt, j, cachedReflection.primaryKeyTypes[j])!;
                }
                primaryKeys.Add(primaryKey);
            }
            // Cleanup
            raw.sqlite3_reset(stmt);
            raw.sqlite3_clear_bindings(stmt);
        }
        return primaryKeys;
    }
    public int Update<T>(T item, string condition, params object?[] args) where T : class {
        Type type = typeof(T);
        if (!_updateCaches.TryGetValue(type, out var cachedReflection)) {
            cachedReflection = UpdateORMConstructor.ConstructUpdateTableString(type);
            _updateCaches[type] = cachedReflection;
        }
        using sqlite3_stmt stmt = Preprate(cachedReflection.GetSQL(condition));
        for (int i = 0; i < cachedReflection.Getters.Count; i++) {
            object? value = cachedReflection.Getters[i](item);
            TypeMap.BindMap(stmt, i + 1, value);
        }
        int offset = cachedReflection.Getters.Count;
        for (int i = 0; i < args.Length; i++) {
            TypeMap.BindMap(stmt, i + 1 + offset, args[i]);
        }
        return raw.sqlite3_step(stmt);
    }
}