using MyOrm.Core.Attributes;
using MyOrm.Core.Interfaces;
using MyOrm.SqlClient.Helpers;
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

            var (sql, properties) = SqlBuilder.BuildInsert(entity);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue("@" + prop.Name, value);
            }

            connection.Open();
            command.ExecuteNonQuery();
        }

        public T? GetById(object id)
        {
            var type = typeof(T);

            var (sql, keyProp) = SqlBuilder.BuildSelectById<T>();

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

            var (sql, props, keyProp) = SqlBuilder.BuildUpdate(entity);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            foreach (var prop in props)
            {
                var value = prop.GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue("@" + prop.Name, value);
            }

            var keyValue = keyProp.GetValue(entity) ?? throw new Exception("Key value cannot be null.");
            command.Parameters.AddWithValue("@Id", keyValue);

            connection.Open();
            command.ExecuteNonQuery();
        }

        public void Delete(object id)
        {
            var (sql, keyProp) = SqlBuilder.BuildDelete<T>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
