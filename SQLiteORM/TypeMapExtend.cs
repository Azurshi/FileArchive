using SQLiteORM.Internal;
using SQLitePCL;
using System;
namespace SQLiteORM;

public class TypeMapExtend {
    public static bool Register(Type type, string fieldType, Func<sqlite3_stmt, int, object, int> binder, Func<sqlite3_stmt, int, object> reader) {
        if (TypeMap.Maps.ContainsKey(type)) {
            return false;
        } else {
            TypeMap.Maps[type] = new(fieldType, binder, reader);
            return true;
        }
    }
}
