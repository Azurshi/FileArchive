using System;
using System.Linq;
using System.Reflection;

namespace SQLiteORM.Internal;

internal static class ORMCommon {
    public static TableAttribute? GetTable(Type type) {
        return type.GetCustomAttribute<TableAttribute>(inherit: true);
    }
    public static bool IsNullable(PropertyInfo property) {
        Type propertyType = property.PropertyType;
        if (propertyType.IsValueType) {
            return Nullable.GetUnderlyingType(propertyType) != null;
        }
        else {
            var attribute = propertyType.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
            if (attribute != null && attribute.ConstructorArguments.Count == 1) {
                var flag = (byte?)attribute.ConstructorArguments[0].Value;
                return flag == 1;
            }
            return false;
        }
    }
}
public static class SQLiteKeyword {
    public const string Integer = "INTEGER";
    public const string Text = "TEXT";
    public const string Real = "REAL";
    public const string Blob = "BLOB";
    public const string Json = "JSON";
    public const string JsonB = "JSONB";
    public const string NotNull = "NOT NULL";
    public const string AutoIncremental = "PRIMARY KEY AUTOINCREMENT";
}