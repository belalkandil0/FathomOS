// FathomOS.Core/Data/SqliteConnectionFactory.cs
// SQLite connection management for FathomOS
// Provides centralized connection creation and configuration

using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace FathomOS.Core.Data;

/// <summary>
/// Factory for creating and managing SQLite database connections.
/// Ensures consistent connection configuration across all modules.
/// </summary>
public class SqliteConnectionFactory : IDisposable
{
    private readonly string _connectionString;
    private readonly string _databasePath;
    private SqliteConnection? _sharedConnection;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the database file path
    /// </summary>
    public string DatabasePath => _databasePath;

    /// <summary>
    /// Gets the connection string
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// Creates a connection factory for the specified database path
    /// </summary>
    /// <param name="databasePath">Full path to the SQLite database file</param>
    public SqliteConnectionFactory(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));

        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Build connection string with recommended SQLite settings
        // Cache=Shared enables connection pooling
        // Mode=ReadWriteCreate creates the file if it doesn't exist
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    /// <summary>
    /// Creates a connection factory using the default FathomOS data directory
    /// </summary>
    /// <param name="databaseName">Name of the database file (e.g., "certificates.db")</param>
    /// <param name="moduleName">Optional module name for subdirectory</param>
    /// <returns>A new SqliteConnectionFactory instance</returns>
    public static SqliteConnectionFactory CreateDefault(string databaseName, string? moduleName = null)
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS");

        if (!string.IsNullOrEmpty(moduleName))
        {
            basePath = Path.Combine(basePath, moduleName);
        }

        var dbPath = Path.Combine(basePath, databaseName);
        return new SqliteConnectionFactory(dbPath);
    }

    /// <summary>
    /// Creates a new database connection.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    /// <returns>A new opened SQLite connection</returns>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable foreign key support (off by default in SQLite)
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        return connection;
    }

    /// <summary>
    /// Creates a new database connection asynchronously.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    /// <returns>A new opened SQLite connection</returns>
    public async Task<SqliteConnection> CreateConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Enable foreign key support
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        await cmd.ExecuteNonQueryAsync();

        return connection;
    }

    /// <summary>
    /// Gets or creates a shared connection for the current factory.
    /// Use for scenarios requiring connection reuse within a scope.
    /// </summary>
    /// <returns>A shared SQLite connection</returns>
    public SqliteConnection GetSharedConnection()
    {
        lock (_lock)
        {
            if (_sharedConnection == null || _sharedConnection.State != System.Data.ConnectionState.Open)
            {
                _sharedConnection?.Dispose();
                _sharedConnection = CreateConnection();
            }
            return _sharedConnection;
        }
    }

    /// <summary>
    /// Executes SQL within a transaction
    /// </summary>
    /// <param name="action">Action to execute within the transaction</param>
    public void ExecuteInTransaction(Action<SqliteConnection, SqliteTransaction> action)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            action(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Executes SQL within a transaction asynchronously
    /// </summary>
    /// <param name="action">Async action to execute within the transaction</param>
    public async Task ExecuteInTransactionAsync(Func<SqliteConnection, SqliteTransaction, Task> action)
    {
        await using var connection = await CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await action(connection, transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Executes SQL within a transaction and returns a result
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="func">Function to execute within the transaction</param>
    /// <returns>The result of the function</returns>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<SqliteConnection, SqliteTransaction, Task<T>> func)
    {
        await using var connection = await CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            var result = await func(connection, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Tests if the database connection can be established
    /// </summary>
    /// <returns>True if connection successful</returns>
    public bool TestConnection()
    {
        try
        {
            using var connection = CreateConnection();
            return connection.State == System.Data.ConnectionState.Open;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tests if the database connection can be established asynchronously
    /// </summary>
    /// <returns>True if connection successful</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            return connection.State == System.Data.ConnectionState.Open;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets database file size in bytes
    /// </summary>
    /// <returns>File size in bytes, or 0 if file doesn't exist</returns>
    public long GetDatabaseSize()
    {
        if (File.Exists(_databasePath))
        {
            return new FileInfo(_databasePath).Length;
        }
        return 0;
    }

    /// <summary>
    /// Runs VACUUM to compact the database
    /// </summary>
    public void Vacuum()
    {
        using var connection = CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "VACUUM;";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Runs VACUUM asynchronously to compact the database
    /// </summary>
    public async Task VacuumAsync()
    {
        await using var connection = await CreateConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "VACUUM;";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates a backup of the database
    /// </summary>
    /// <param name="backupPath">Path for the backup file</param>
    public void Backup(string backupPath)
    {
        if (File.Exists(_databasePath))
        {
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }
            File.Copy(_databasePath, backupPath, overwrite: true);
        }
    }

    /// <summary>
    /// Creates a timestamped backup of the database
    /// </summary>
    /// <returns>Path to the backup file</returns>
    public string BackupWithTimestamp()
    {
        var directory = Path.GetDirectoryName(_databasePath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(_databasePath);
        var extension = Path.GetExtension(_databasePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        var backupPath = Path.Combine(directory, "Backups", $"{fileName}_{timestamp}{extension}");
        Backup(backupPath);

        return backupPath;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sharedConnection?.Dispose();
                _sharedConnection = null;
            }
            _disposed = true;
        }
    }
}
