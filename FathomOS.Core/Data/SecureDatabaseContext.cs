// FathomOS.Core/Data/SecureDatabaseContext.cs
// Secure database context with SQLCipher encryption support
// Provides encrypted database access with key management

using Microsoft.Data.Sqlite;
using System.Data;
using System.Security.Cryptography;
using FathomOS.Core.Security;

namespace FathomOS.Core.Data;

/// <summary>
/// Interface for secure database context with encryption support
/// </summary>
public interface ISecureDatabaseContext : IDisposable
{
    /// <summary>
    /// Gets the path to the database file
    /// </summary>
    string DatabasePath { get; }

    /// <summary>
    /// Gets whether the database is encrypted
    /// </summary>
    bool IsEncrypted { get; }

    /// <summary>
    /// Gets whether the database is currently open
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the underlying database connection
    /// </summary>
    SqliteConnection Connection { get; }

    /// <summary>
    /// Sets the encryption key for the database
    /// </summary>
    /// <param name="key">The encryption key (base64 encoded)</param>
    void SetEncryptionKey(string key);

    /// <summary>
    /// Changes the encryption key for the database
    /// </summary>
    /// <param name="newKey">The new encryption key</param>
    void ChangeEncryptionKey(string newKey);

    /// <summary>
    /// Validates the integrity of the database
    /// </summary>
    /// <returns>True if the database passes integrity checks</returns>
    Task<bool> ValidateIntegrityAsync();

    /// <summary>
    /// Opens the database connection
    /// </summary>
    Task OpenAsync();

    /// <summary>
    /// Closes the database connection
    /// </summary>
    void Close();

    /// <summary>
    /// Creates a new command for the connection
    /// </summary>
    SqliteCommand CreateCommand();

    /// <summary>
    /// Begins a new transaction
    /// </summary>
    SqliteTransaction BeginTransaction();

    /// <summary>
    /// Begins a new transaction asynchronously
    /// </summary>
    Task<SqliteTransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes a non-query SQL command
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, CancellationToken ct = default);

    /// <summary>
    /// Executes a scalar SQL command
    /// </summary>
    Task<object?> ExecuteScalarAsync(string sql, CancellationToken ct = default);
}

/// <summary>
/// Secure database context implementation with SQLCipher encryption
/// </summary>
public sealed class SecureDatabaseContext : ISecureDatabaseContext
{
    private readonly string _databasePath;
    private readonly bool _useEncryption;
    private readonly object _lock = new();
    private SqliteConnection? _connection;
    private string? _encryptionKey;
    private bool _disposed;

    /// <summary>
    /// Creates a new secure database context
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <param name="useEncryption">Whether to use encryption (default: true)</param>
    public SecureDatabaseContext(string databasePath, bool useEncryption = true)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        _useEncryption = useEncryption;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc />
    public string DatabasePath => _databasePath;

    /// <inheritdoc />
    public bool IsEncrypted => _useEncryption;

    /// <inheritdoc />
    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                return _connection?.State == ConnectionState.Open;
            }
        }
    }

    /// <inheritdoc />
    public SqliteConnection Connection
    {
        get
        {
            lock (_lock)
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                {
                    throw new InvalidOperationException("Database connection is not open. Call OpenAsync first.");
                }
                return _connection;
            }
        }
    }

    /// <inheritdoc />
    public void SetEncryptionKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        lock (_lock)
        {
            _encryptionKey = key;
        }
    }

    /// <inheritdoc />
    public void ChangeEncryptionKey(string newKey)
    {
        if (string.IsNullOrEmpty(newKey))
        {
            throw new ArgumentNullException(nameof(newKey));
        }

        lock (_lock)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Database connection must be open to change encryption key.");
            }

            try
            {
                using var command = _connection.CreateCommand();
                // SQLCipher PRAGMA rekey command changes the encryption key
                command.CommandText = $"PRAGMA rekey = '{newKey}';";
                command.ExecuteNonQuery();

                _encryptionKey = newKey;
            }
            catch (SqliteException ex)
            {
                throw new CryptographicException("Failed to change database encryption key.", ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateIntegrityAsync()
    {
        EnsureConnection();

        try
        {
            // Run SQLite integrity check
            await using var command = _connection!.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var result = reader.GetString(0);
                return result.Equals("ok", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task OpenAsync()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecureDatabaseContext));
            }

            if (_connection?.State == ConnectionState.Open)
            {
                return;
            }
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        try
        {
            // Apply encryption key if enabled
            if (_useEncryption)
            {
                await ApplyEncryptionKeyAsync(connection);
            }

            // Enable foreign keys
            await using var fkCommand = connection.CreateCommand();
            fkCommand.CommandText = "PRAGMA foreign_keys = ON;";
            await fkCommand.ExecuteNonQueryAsync();

            // Set WAL mode for better concurrency
            await using var walCommand = connection.CreateCommand();
            walCommand.CommandText = "PRAGMA journal_mode = WAL;";
            await walCommand.ExecuteNonQueryAsync();

            lock (_lock)
            {
                _connection?.Dispose();
                _connection = connection;
            }
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        lock (_lock)
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
                _connection.Dispose();
                _connection = null;
            }
        }
    }

    /// <inheritdoc />
    public SqliteCommand CreateCommand()
    {
        return Connection.CreateCommand();
    }

    /// <inheritdoc />
    public SqliteTransaction BeginTransaction()
    {
        return Connection.BeginTransaction();
    }

    /// <inheritdoc />
    public async Task<SqliteTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        EnsureConnection();
        return await Task.FromResult(_connection!.BeginTransaction());
    }

    /// <inheritdoc />
    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken ct = default)
    {
        EnsureConnection();

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteNonQueryAsync(ct);
    }

    /// <inheritdoc />
    public async Task<object?> ExecuteScalarAsync(string sql, CancellationToken ct = default)
    {
        EnsureConnection();

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(ct);
    }

    /// <summary>
    /// Creates a secure database context with default encryption using the system key
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <returns>A configured SecureDatabaseContext</returns>
    public static SecureDatabaseContext CreateWithSystemKey(string databasePath)
    {
        var context = new SecureDatabaseContext(databasePath, useEncryption: true);
        var key = SecretManager.GetOrCreateDatabaseKey();
        context.SetEncryptionKey(key);
        return context;
    }

    /// <summary>
    /// Creates a secure database context for a FathomOS module
    /// </summary>
    /// <param name="databaseName">Name of the database file</param>
    /// <param name="moduleName">Optional module subdirectory name</param>
    /// <returns>A configured SecureDatabaseContext</returns>
    public static SecureDatabaseContext CreateForModule(string databaseName, string? moduleName = null)
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS");

        if (!string.IsNullOrEmpty(moduleName))
        {
            basePath = Path.Combine(basePath, moduleName);
        }

        var dbPath = Path.Combine(basePath, databaseName);
        return CreateWithSystemKey(dbPath);
    }

    private async Task ApplyEncryptionKeyAsync(SqliteConnection connection)
    {
        // Use provided key or get system key
        var key = _encryptionKey ?? SecretManager.GetOrCreateDatabaseKey();

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA key = '{key}';";
        await command.ExecuteNonQueryAsync();

        // Verify the key worked
        await using var verifyCmd = connection.CreateCommand();
        verifyCmd.CommandText = "SELECT count(*) FROM sqlite_master;";
        try
        {
            await verifyCmd.ExecuteScalarAsync();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 26)
        {
            throw new CryptographicException(
                "Database decryption failed. The database may be encrypted with a different key.", ex);
        }
    }

    private void EnsureConnection()
    {
        lock (_lock)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Database connection is not open. Call OpenAsync first.");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            Close();
            _disposed = true;
        }
    }
}
