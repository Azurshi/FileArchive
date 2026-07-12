using System;
namespace SQLiteORM;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class PrimaryKeyAttribute : DatabaseFieldAttribute {
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ForeignKeyAttribute : DatabaseFieldAttribute {
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class DatabaseFieldAttribute : Attribute {
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class JsonFieldAttribute : DatabaseFieldAttribute {
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AutoIncrementAttribute : DatabaseFieldAttribute {
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute {
    public string? TableName;
    public string[] IndexOptions;
    public string[] TableOptions;
    public string AfterTableOption;
    public TableAttribute() {
        this.TableName = null;
        this.IndexOptions = [];
        this.TableOptions = [];
        this.AfterTableOption = string.Empty;
    }
    /// <summary>
    /// Array of indexOption: each is column names separtated by ',' .
    /// </summary>
    public TableAttribute(string? tableName, string[] indexOptions, string[] tableOptions, string afterTableOption) {
        this.TableName = tableName;
        this.IndexOptions = indexOptions;
        this.TableOptions = tableOptions;
        this.AfterTableOption = afterTableOption;
    }
}
