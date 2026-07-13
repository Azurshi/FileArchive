using SQLiteORM.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SQLiteORM.ORM;

internal static class TableORMConstructor {
    public static string ConstructCreateTableString(Type type) {
        TableAttribute? tableAttribute = ORMCommon.GetTable(type);
        if (tableAttribute != null) {
            string tableName = tableAttribute.TableName ?? type.Name;
            List<string> lines = [];
            List<string> primaryKeys = [];
            bool hasAutoIncrement = false;
            HashSet<string> columnNames = [];
            foreach (var property in type.GetProperties()) {
                bool hasAttribute = property.IsDefined(typeof(DatabaseFieldAttribute), inherit: true);
                Type propertyType = property.PropertyType;
                string fieldName = property.Name;
                columnNames.Add(fieldName.ToLower());
                string? typeString = null;
                if (hasAttribute) {
                    if (property.IsDefined(typeof(JsonFieldAttribute), inherit: true)) {
                        typeString = SQLiteKeyword.Json;
                    }
                    else if (TypeMap.Maps.TryGetValue(propertyType, out var typeEntry)) {
                        typeString = typeEntry.FieldKeyword;
                    }
                    else {
                        Type? baseType = Nullable.GetUnderlyingType(propertyType);
                        if (baseType != null && TypeMap.Maps.TryGetValue(baseType, out var baseTypeEntry)) {
                            typeString = baseTypeEntry.FieldKeyword;
                        } 
                        else {
                            throw new Exception($"Type not supported: {propertyType.FullName}");
                        }
                    }
                }
                if (typeString == null) {
                    throw new Exception();
                }
                bool nullable = ORMCommon.IsNullable(property);
                bool autoIncrement = property.IsDefined(typeof(AutoIncrementAttribute), inherit: true);
                bool isPrimaryKey = property.IsDefined(typeof(PrimaryKeyAttribute), inherit: true);
                if (isPrimaryKey) {
                    primaryKeys.Add(fieldName);
                }
                List<string> parts = [fieldName, typeString];

                if (!nullable) {
                    parts.Add(SQLiteKeyword.NotNull);
                }
                if (autoIncrement) {
                    if (hasAutoIncrement) {
                        throw new Exception($"SQLite does not support Auto Increment for multi primary key");
                    }
                    hasAutoIncrement = true;
                    parts.Add(SQLiteKeyword.AutoIncremental);
                }
                lines.Add(string.Join(" ", parts));
            }
            string query;
            if (hasAutoIncrement) {
                query = $"""
                CREATE TABLE IF NOT EXISTS {tableName} (
                    {string.Join(",\n    ", lines)}
                """;
            }
            else {
                query = $"""
                CREATE TABLE IF NOT EXISTS {tableName} (
                    {string.Join(",\n    ", lines)},
                    PRIMARY KEY ({string.Join(", ", primaryKeys)})
                """;
            }
            var extraArgs = tableAttribute.TableOptions;
            if (extraArgs.Length > 0) {
                query += ",\n    " + string.Join(",\n    ", extraArgs);
            }
            query += $"\n) {tableAttribute.AfterTableOption} ;\n";
            // Index handle
            List<string> indexes = [];
            foreach(var indexOption in tableAttribute.IndexOptions) {
                string[] columns = indexOption.Split(",").Select(i => i.Trim()).ToArray();
                foreach(var column in columns) {
                    if (!columnNames.Contains(column.ToLower())) {
                        throw new Exception("Column name not found when creating index");
                    }
                }
                indexes.Add($"CREATE INDEX IF NOT EXISTS IX_{string.Join("_", columns)}_{tableName} ON {tableName}({string.Join(", ", columns)})");
            }
            if (indexes.Count > 0) {
                query += string.Join(" ;\n", indexes) + " ;\n";
            }
            return query;
        }
        else {
            throw new Exception($"Not a table: {type.FullName}");
        }
    }
}