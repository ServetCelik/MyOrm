using MyOrm.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyOrm.SqlClient.Helpers
{
    public static class SqlBuilder
    {
        public static (string Sql, List<PropertyInfo> Properties) BuildInsert<T>(T entity)
        {
            var type = typeof(T);
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr == null)
                throw new Exception($"Class {type.Name} must have a [Table] attribute.");

            var tableName = tableAttr.Name;

            var props = type.GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null && p.Name != "Id")
                .ToList();

            var columnNames = props.Select(p => p.GetCustomAttribute<ColumnAttribute>()!.Name);
            var parameters = props.Select(p => "@" + p.Name);

            var sql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameters)});";

            return (sql, props);
        }

        public static (string Sql, List<PropertyInfo> Props, PropertyInfo KeyProp) BuildUpdate<T>(T entity)
        {
            var type = typeof(T);
            var tableAttr = type.GetCustomAttribute<TableAttribute>()
                ?? throw new Exception($"Class {type.Name} must have a [Table] attribute.");

            var tableName = tableAttr.Name;

            var keyProp = type.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                ?? throw new Exception($"Class {type.Name} must have a property marked with [Key].");

            var keyColumnAttr = keyProp.GetCustomAttribute<ColumnAttribute>()
                ?? throw new Exception($"Key property {keyProp.Name} must have a [Column] attribute.");

            var keyColumn = keyColumnAttr.Name;

            var props = type.GetProperties()
                .Where(p =>
                    p.GetCustomAttribute<ColumnAttribute>() != null &&
                    p.GetCustomAttribute<KeyAttribute>() == null &&
                    p.GetCustomAttribute<NotMappedAttribute>() == null)
                .ToList();

            var setClauses = props.Select(p =>
            {
                var col = p.GetCustomAttribute<ColumnAttribute>()!.Name;
                return $"{col} = @{p.Name}";
            });

            var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyColumn} = @Id;";

            return (sql, props, keyProp);
        }

        public static (string Sql, PropertyInfo KeyProp) BuildDelete<T>()
        {
            var type = typeof(T);

            var tableAttr = type.GetCustomAttribute<TableAttribute>()
                ?? throw new Exception($"Class {type.Name} must have a [Table] attribute.");

            var tableName = tableAttr.Name;

            var keyProp = type.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                ?? throw new Exception($"Class {type.Name} must have a property marked with [Key].");

            var keyColumnAttr = keyProp.GetCustomAttribute<ColumnAttribute>()
                ?? throw new Exception($"Key property {keyProp.Name} must have a [Column] attribute.");

            var keyColumn = keyColumnAttr.Name;

            var sql = $"DELETE FROM {tableName} WHERE {keyColumn} = @Id;";
            return (sql, keyProp);
        }
    }
}
