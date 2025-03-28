using MyOrm.Core.Attributes;
using MyOrm.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyOrm.SqlClient.Repository
{
    public class SqlRepository<T> where T : class, IEntity, new()
    {
        private readonly string _connectionString;

        public SqlRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Insert(T entity)
        {
            var type = typeof(T);
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr == null)
                throw new Exception($"Class {type.Name} must have a [Table] attribute.");

            string tableName = tableAttr.Name;

            // Get properties with [Column] attribute, skip Id
            var props = type.GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null && p.Name != "Id")
                .ToList();

            var columnsAndProps = props
                .Select(p => new
                {
                    Property = p,
                    ColumnName = p.GetCustomAttribute<ColumnAttribute>()!.Name
                })
                .ToList();

            var columnNames = columnsAndProps.Select(cp => cp.ColumnName);
            var parameters = columnsAndProps.Select(cp => "@" + cp.Property.Name);

            string sql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameters)});";

            Console.WriteLine("Generated SQL: " + sql); // Optional logging

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            foreach (var cp in columnsAndProps)
            {
                var value = cp.Property.GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue("@" + cp.Property.Name, value);
            }

            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
