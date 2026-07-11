using SQLitePCL;
using System;
using System.Diagnostics;

namespace SQLiteORM.Internal;

public class SQLiteConnection {
    protected sqlite3 _database;
    public SQLiteConnection(int flags, string filePath) {
        if (raw.sqlite3_open_v2(filePath, out sqlite3 database, flags, null) == raw.SQLITE_OK) {
            database.enable_sqlite3_next_stmt(true);
            this._database = database;
        }
        else {
            throw new System.Exception($"Failed to open database: {filePath} {flags}");
        }
    }
    protected sqlite3_stmt Preprate(string sql) {
        int rc = raw.sqlite3_prepare_v2(this._database, sql, out sqlite3_stmt stmt);
        if (rc != raw.SQLITE_OK) {
            throw new Exception(raw.sqlite3_errmsg(this._database).utf8_to_string());
        }
        return stmt;
    }
    public void Dispose(bool verbose) {
        if (verbose) {
            Debug.WriteLine("Closing database connection");
        }
        sqlite3_stmt stmt;
        while ((stmt = raw.sqlite3_next_stmt(_database, null)) != null) {
            raw.sqlite3_finalize(stmt);
        }
        var rc = raw.sqlite3_close_v2(_database);
        if (verbose) {
            if (rc != raw.SQLITE_OK) {
                Debug.WriteLine("Database connection failed to close");
            }
            else {
                Debug.WriteLine("Database connection closed");
            }
        }
    }
}