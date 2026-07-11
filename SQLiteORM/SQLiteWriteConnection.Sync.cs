using SQLiteORM.Internal;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLiteORM;

public partial class SQLiteWriteConnection {
    public void BeginTransaction() {
        using sqlite3_stmt stmt = Preprate("BEGIN TRANSACTION;");
        raw.sqlite3_step(stmt);
    }
    public void CommitTransaction() {
        using sqlite3_stmt stmt = Preprate("COMMIT;");
        int rc = raw.sqlite3_step(stmt);
        if (rc != raw.SQLITE_DONE && rc != raw.SQLITE_OK) {
            throw new Exception(raw.sqlite3_errmsg(this._database).utf8_to_string());
        }
    }
    public void RollbackTransaction() {
        using sqlite3_stmt stmt = Preprate("ROLLBACK;");
        raw.sqlite3_step(stmt);
    }
    public int Delete(string sql, params object[] args) {
        using sqlite3_stmt stmt = Preprate(sql);
        for (int i = 0; i < args.Length; i++) {
            TypeMap.BindMap(stmt, i + 1, args[i]);
        }
        return raw.sqlite3_step(stmt);
    }
    public int Update(string sql, params object?[] args) {
        using sqlite3_stmt stmt = Preprate(sql);
        for (int i = 0; i < args.Length; i++) {
            TypeMap.BindMap(stmt, i + 1, args[i]);
        }
        return raw.sqlite3_step(stmt);
    }
    public int Execute(string sql, params object[] args) {
        using sqlite3_stmt stmt = Preprate(sql);
        for (int i = 0; i < args.Length; i++) {
            TypeMap.BindMap(stmt, i + 1, args[i]);
        }
        return raw.sqlite3_step(stmt);
    }
    public int CheckpointWAL() {
        return raw.sqlite3_wal_checkpoint_v2(_database, null, raw.SQLITE_CHECKPOINT_TRUNCATE, out int logFrames, out int checkpointFrames);
    }
}
