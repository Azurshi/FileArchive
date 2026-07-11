using SQLiteORM.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SQLiteORM.ORM;

internal class CachedUpdateReflection {
    public readonly string BaseSQL;
    public readonly List<Func<object, object?>> Getters;
    public CachedUpdateReflection(string baseSQL, List<Func<object, object?>> getters) {
        this.BaseSQL = baseSQL;
        this.Getters = getters;
    }
    public string GetSQL(string condition) {
        return BaseSQL + "\n" + condition;
    }
}

internal static class UpdateORMConstructor {
    public static CachedUpdateReflection ConstructUpdateTableString(Type type) {
        TableAttribute? tableAttribute = ORMCommon.GetTable(type);
        if (tableAttribute != null) {
            string tableName = tableAttribute.TableName ?? type.Name;
            List<string> propertyNames = [];
            List<string> placeholders = [];
            List<Func<object, object?>> getters = [];
            foreach (var property in type.GetProperties()) {
                bool hasAttribute = property.IsDefined(typeof(DatabaseFieldAttribute), inherit: true);
                Type propertyType = property.PropertyType;
                if (hasAttribute) {
                    propertyNames.Add(property.Name + " = ?");
                    placeholders.Add("?");
                    MethodInfo method = property.GetGetMethod() ?? throw new Exception("Method has not getter");
                    var typedDelegate = method.CreateDelegate(
                        typeof(Func<,>).MakeGenericType(type, propertyType)
                    );
                    getters.Add((object obj) => typedDelegate.DynamicInvoke(obj));
                }
            }
            string query = $"""
                UPDATE {tableName}
                SET {string.Join(", ", propertyNames)}
                """;
            CachedUpdateReflection cachedReflection = new(query, getters);
            return cachedReflection;
        }
        else {
            throw new Exception($"Not a table: {type.FullName}");
        }
    }
}