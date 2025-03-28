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

        public T? GetById(object id)
        {
            var type = typeof(T);
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr == null)
                throw new Exception($"Class {type.Name} must have a [Table] attribute.");

            string tableName = tableAttr.Name;

            // Get Key Property
            var keyProp = type.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp == null)
                throw new Exception($"Class {type.Name} must have a property marked with [Key].");

            var keyColumnAttr = keyProp.GetCustomAttribute<ColumnAttribute>();
            if (keyColumnAttr == null)
                throw new Exception($"Key property {keyProp.Name} must have a [Column] attribute.");

            string keyColumn = keyColumnAttr.Name;

            string sql = $"SELECT * FROM {tableName} WHERE {keyColumn} = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            var entity = new T();
            foreach (var prop in type.GetProperties())
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr == null || reader[columnAttr.Name] is DBNull)
                    continue;

                prop.SetValue(entity, reader[columnAttr.Name]);
            }

            return entity;
        }

        public void Update(T entity)
        {
            var type = typeof(T);

            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr == null)
                throw new Exception($"Class {type.Name} must have a [Table] attribute.");

            string tableName = tableAttr.Name;

            // Find the key property
            var keyProp = type.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            if (keyProp == null)
                throw new Exception($"Class {type.Name} must have a property marked with [Key].");

            var keyColumnAttr = keyProp.GetCustomAttribute<ColumnAttribute>();
            if (keyColumnAttr == null)
                throw new Exception($"Key property {keyProp.Name} must have a [Column] attribute.");

            string keyColumn = keyColumnAttr.Name;
            object keyValue = keyProp.GetValue(entity)!;

            // Get all updatable properties (exclude key + [NotMapped])
            var props = type.GetProperties()
                .Where(p =>
                    p.GetCustomAttribute<ColumnAttribute>() != null &&
                    p.GetCustomAttribute<KeyAttribute>() == null &&
                    p.GetCustomAttribute<NotMappedAttribute>() == null
                )
                .ToList();

            var setClauses = props.Select(p =>
            {
                var columnName = p.GetCustomAttribute<ColumnAttribute>()!.Name;
                return $"{columnName} = @{p.Name}";
            });

            string sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyColumn} = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            foreach (var prop in props)
            {
                var value = prop.GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue("@" + prop.Name, value);
            }

            command.Parameters.AddWithValue("@Id", keyValue);

            connection.Open();
            command.ExecuteNonQuery();
        }



    }
}
