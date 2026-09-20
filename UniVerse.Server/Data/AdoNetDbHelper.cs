using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UniVerse.Server.Data
{
    /// <summary>
    /// Pure ADO.NET Data Access Layer Helper.
    /// Fulfills university course requirements by explicitly using SqliteConnection,
    /// SqliteCommand, SqliteParameter, and SqliteDataReader with strictly parameterized queries.
    /// </summary>
    public class AdoNetDbHelper
    {
        private readonly string _connectionString;
        private readonly ILogger<AdoNetDbHelper> _logger;

        public AdoNetDbHelper(IConfiguration configuration, ILogger<AdoNetDbHelper> logger)
        {
            _logger = logger;
            var rawConn = configuration.GetConnectionString("DefaultConnection") 
                          ?? "Data Source=universe.db";
            
            var builder = new SqliteConnectionStringBuilder(rawConn);
            if (!Path.IsPathRooted(builder.DataSource))
            {
                builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);
            }
            _connectionString = builder.ConnectionString;
        }

        public string ConnectionString => _connectionString;

        /// <summary>
        /// Creates and returns a new ADO.NET SqliteConnection instance.
        /// </summary>
        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// Creates an ADO.NET SqliteParameter, handling null values as DBNull.
        /// </summary>
        public static SqliteParameter CreateParameter(string parameterName, object? value)
        {
            return new SqliteParameter(parameterName, value ?? DBNull.Value);
        }

        /// <summary>
        /// Executes an INSERT, UPDATE, or DELETE command using ADO.NET SqliteCommand.
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string query, params SqliteParameter[] parameters)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqliteCommand(query, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            try
            {
                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADO.NET ExecuteNonQueryAsync failed for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes a query returning a scalar value using ADO.NET SqliteCommand.
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string query, params SqliteParameter[] parameters)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqliteCommand(query, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            try
            {
                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    return default;
                }

                if (typeof(T) == typeof(Guid) && result is string strGuid)
                {
                    return (T)(object)Guid.Parse(strGuid);
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADO.NET ExecuteScalarAsync failed for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes a SELECT query and maps each row via SqliteDataReader using a strongly typed mapper.
        /// </summary>
        public async Task<List<T>> ExecuteReaderAsync<T>(
            string query, 
            Func<SqliteDataReader, T> mapFunction, 
            params SqliteParameter[] parameters)
        {
            var results = new List<T>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqliteCommand(query, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            try
            {
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(mapFunction(reader));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADO.NET ExecuteReaderAsync failed for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes a SELECT query expecting at most one row, or default if none found.
        /// </summary>
        public async Task<T?> ExecuteSingleAsync<T>(
            string query, 
            Func<SqliteDataReader, T> mapFunction, 
            params SqliteParameter[] parameters)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqliteCommand(query, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            try
            {
                await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
                if (await reader.ReadAsync())
                {
                    return mapFunction(reader);
                }

                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADO.NET ExecuteSingleAsync failed for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes multiple ADO.NET commands inside an explicit atomic SqliteTransaction.
        /// </summary>
        public async Task ExecuteTransactionAsync(Func<SqliteConnection, SqliteTransaction, Task> transactionAction)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction();
            try
            {
                await transactionAction(connection, transaction);
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADO.NET Transaction aborted and rolled back.");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
