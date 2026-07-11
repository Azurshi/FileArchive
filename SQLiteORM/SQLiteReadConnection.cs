using SQLiteORM.Internal;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteORM;

public partial class SQLiteReadConnection : SQLiteConnection {
    private const int Flags = raw.SQLITE_OPEN_READONLY | raw.SQLITE_OPEN_NOMUTEX;
    private readonly SemaphoreSlim _semaphore = new(1);
    protected SQLiteReadConnection(int flags, string filePath) : base(flags, filePath) { }
    public SQLiteReadConnection(string filePath) : base(Flags, filePath) {
        
    }
    private IEnumerable<object?[]> SelectInner(string sql, Type[] columnTypes, params object[] args) {
        int columnLength = columnTypes.Length;
        using sqlite3_stmt stmt = Preprate(sql);
        for (int i = 0; i < args.Length; i++) {
            TypeMap.BindMap(stmt, i + 1, args[i]);
        }
        while (true) {
            int rc = raw.sqlite3_step(stmt);
            if (rc == raw.SQLITE_ROW) {
                object?[] row = new object[columnLength];
                for (int i = 0; i < columnLength; i++) {
                    row[i] = TypeMap.ReadMap(stmt, i, columnTypes[i]);
                }
                yield return row;
            }
            else if (rc == raw.SQLITE_DONE) {
                yield break;
            }
            else {
                throw new Exception("Select error: " + raw.sqlite3_errmsg(this._database).utf8_to_string());
            }
        }
    }
    public IEnumerable<int> Select(string sql, Action<ReadOnlySpan<byte>> consumer, params object[] args) {
        using (sqlite3_stmt stmt = Preprate(sql)) {
            for (int i = 0; i < args.Length; i++) {
                TypeMap.BindMap(stmt, i + 1, args[i]);
            }
            int index = 0;
            while (true) {
                int rc = raw.sqlite3_step(stmt);
                if (rc == raw.SQLITE_ROW) {
                    var span = raw.sqlite3_column_blob(stmt, 0);
                    consumer(span);
                    yield return index;
                }
                else if (rc == raw.SQLITE_DONE) {
                    yield break;
                }
                else {
                    throw new Exception("Select error: " + raw.sqlite3_errmsg(this._database).utf8_to_string());
                }
                index++;
            }
        }
    }
    private List<object?[]> SelectToList(string sql, Type[] columnTypes, params object[] args) {
        return SelectInner(sql, columnTypes, args).ToList();
    }
    protected static List<TTuple> Cast<TTuple>(List<object?[]> rows, Func<object?[], TTuple> map) {
        List<TTuple> result = new List<TTuple>(rows.Count);
        foreach (var r in rows) {
            result.Add(map(r));
        }
        return result;
    }
}

