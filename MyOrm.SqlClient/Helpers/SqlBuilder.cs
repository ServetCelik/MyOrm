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
    }
}
