using SQLiteORM.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SQLiteORM.ORM;

internal class CachedInsertReflection {
    public readonly string insertSql;
    public readonly string returnSql;
    public readonly List<Type> primaryKeyTypes;
    public readonly List<Func<object, object?>> getters;
    public CachedInsertReflection(string insertSql, string returnSql, List<Type> primaryKeyTypes, List<Func<object, object?>> getters) {
        this.insertSql = insertSql;
        this.returnSql = returnSql;
        this.getters = getters;
        this.primaryKeyTypes = primaryKeyTypes;
    }
    public string GetSQL(bool needReturn) {
        if (needReturn) {
            return insertSql + "\n" + returnSql;
        }
        else {
            return insertSql + ";";
        }
    }
}

internal static class InsertORMConstructor {
    public static CachedInsertReflection ConstructInsertTableString(Type type) {
        TableAttribute? tableAttribute = ORMCommon.GetTable(type);
        if (tableAttribute != null) {
            string tableName = tableAttribute.TableName ?? type.Name;
            List<string> propertyNames = [];
            List<string> placeholders = [];
            List<string> primaryKeys = [];
            List<Type> primaryKeyTypes = [];
            List<Func<object, object?>> getters = [];
            foreach (var property in type.GetProperties()) {
                bool hasAttribute = property.IsDefined(typeof(DatabaseFieldAttribute), inherit: true);
                Type propertyType = property.PropertyType;
                string propertyName = property.Name;
                if (hasAttribute) {
                    propertyNames.Add(propertyName);
                    placeholders.Add("?");
                    MethodInfo method = property.GetGetMethod() ?? throw new Exception("Method has not getter");
                    var typedDelegate = method.CreateDelegate(
                        typeof(Func<,>).MakeGenericType(type, propertyType)
                    );
                    getters.Add((object obj) => typedDelegate.DynamicInvoke(obj));
                }
                bool isPrimaryKey = property.IsDefined(typeof(PrimaryKeyAttribute), inherit: true);
                if (isPrimaryKey) {
                    primaryKeys.Add(propertyName);
                    primaryKeyTypes.Add(propertyType);
                }
            }
            string insertQuery = $"""
                INSERT INTO {tableName} ({string.Join(", ", propertyNames)})
                VALUES ({string.Join(", ", placeholders)})
                """;
            string returnQuery = $"""
                RETURNING ({string.Join(", ", primaryKeys)});
                """;
            CachedInsertReflection cachedReflection = new(insertQuery, returnQuery, primaryKeyTypes, getters);
            return cachedReflection;
        }
        else {
            throw new Exception($"Not a table: {type.FullName}");
        }
    }
}