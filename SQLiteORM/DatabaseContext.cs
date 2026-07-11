using SQLiteORM.ORM;
using System;
using System.Collections.Generic;
using System.IO;

namespace SQLiteORM;

public class DatabaseContext {
    public SQLiteWriteConnection Writer { get; init; }
    public SQLiteReadConnection Reader { get; init; }
    public DatabaseContext(string path, List<Type> tables, List<string> onNewDatabase) {
        bool isNew = true;
        if (File.Exists(path)) {
            isNew = false;
        }
        this.Writer = new(path);
        if (isNew) {
            foreach (var query in onNewDatabase) {
                this.Writer.Execute(query);
            }
        }
        foreach(var type in tables) {
            string query = TableORMConstructor.ConstructCreateTableString(type);
            this.Writer.Execute(query);
        }
        this.Reader = new(path);
    }

    public void Dispose(bool verbose = false) {
        this.Writer.Dispose(verbose);
        this.Reader.Dispose(verbose);
    }
}
