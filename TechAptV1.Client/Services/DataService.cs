// Copyright © 2025 Always Active Technologies PTY Ltd

using System.Text;
using Microsoft.Data.Sqlite;
using TechAptV1.Client.Models;

namespace TechAptV1.Client.Services;

/// <summary>
/// Data Access Service for interfacing with the SQLite Database
/// </summary>
public sealed class DataService
{
    private readonly ILogger<DataService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Default constructor providing DI Logger and Configuration
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    public DataService(ILogger<DataService> logger, IConfiguration configuration)
    {
        this._logger = logger;
        this._configuration = configuration;
    }

    /// <summary>
    /// Save the list of data to the SQLite Database
    /// </summary>
    /// <param name="dataList"></param>
    public async Task Save(List<Number> dataList)
    {
        try
        {
            if (dataList == null || dataList.Count == 0)
            {
                _logger.LogInformation("No data to save.");
                return;
            }
            this._logger.LogInformation("Save");
            const int batchSize = 500; // Keep batch size reasonable so that it doesn't overload the database
            using var connection = CreateConnection();
            await connection.OpenAsync();

            //insert data in batches of 500 to avoid overloading the database and improve performance by reducing number of round trips
            //since insert one record at a time will result in multiple round trips
            //using transaction to ensure data is saved atomically
            using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
            for (int i = 0; i < dataList.Count; i += batchSize)
            {
                var batch = dataList.Skip(i).Take(batchSize).ToList();
                await InsertBatch(batch, connection, transaction);
            }
            await transaction.CommitAsync();
            _logger.LogInformation("Data successfully saved.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving data to SQLite, Data not saved. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetch N records from the SQLite Database where N is specified by the count parameter
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Number>> Get(int count)
    {
        var numbers = new List<Number>();
        try
        {
            this._logger.LogInformation($"Get top {count} records");
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value, IsPrime FROM Number ORDER BY Value LIMIT @count";
            command.Parameters.AddWithValue("@count", count);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                numbers.Add(new Number
                {
                    Value = reader.GetInt32(0),
                    IsPrime = reader.GetInt32(1)
                });
            }
            this._logger.LogInformation("Daata successfully fetched.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching top {count} records. Error: {ex.Message}");
        }
        return numbers;
    }

    /// <summary>
    /// Fetch All the records from the SQLite Database
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Number>> GetAll()
    {
        var numbers = new List<Number>();
        try
        {
            this._logger.LogInformation("GetAll");
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value, IsPrime FROM Number ORDER BY Value";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                numbers.Add(new Number
                {
                    Value = reader.GetInt32(0),
                    IsPrime = reader.GetInt32(1)
                });
            }
            this._logger.LogInformation("Daata successfully fetched.");
        }
        catch (Exception)
        {
            _logger.LogError($"Error fetching all records. Error: {ex.Message}");
        }
        return numbers;
    }


    public async Task InitializeDatabaseAsync()
    {
        await EnsureDatabaseInitializedAsync();
    }

    #region private methods

    private SqliteConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("Default")
                            ?? throw new ArgumentNullException(nameof(_configuration), "Connection string is missing!");
        return new SqliteConnection(connectionString);
    }

    private async Task EnsureDatabaseInitializedAsync()
    {
        _logger.LogInformation("Initializing database...");

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Number (
                Value INTEGER PRIMARY KEY NOT NULL,
                IsPrime INTEGER NOT NULL DEFAULT 0
            );
        ";

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Database successfully initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database.");
            throw; // Rethrow the exception if needed
        }
    }


    private async Task InsertBatch(List<Number> batch, SqliteConnection connection, SqliteTransaction transaction)
    {
        try
        {
            using var command = connection.CreateCommand();
            //var sqlBuilder = new StringBuilder("INSERT INTO Number (Value, IsPrime) VALUES ");
            var sqlBuilder = new StringBuilder("INSERT OR IGNORE INTO Number (Value, IsPrime) VALUES ");

            var parameters = new List<SqliteParameter>();
            for (int i = 0; i < batch.Count; i++)
            {
                sqlBuilder.Append($"(@value{i}, @isPrime{i}),");
                parameters.Add(new SqliteParameter($"@value{i}", batch[i].Value));
                parameters.Add(new SqliteParameter($"@isPrime{i}", batch[i].IsPrime));
            }
            command.CommandText = sqlBuilder.ToString().TrimEnd(','); // Remove last comma
            command.Parameters.AddRange(parameters.ToArray());
            command.Transaction = transaction;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving data to SQLite, Data not saved. Error: {ex.Message}");
        }
    }


    #endregion
}
