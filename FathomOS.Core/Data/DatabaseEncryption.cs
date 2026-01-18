// FathomOS.Core/Data/DatabaseEncryption.cs
// Database encryption utilities and migration helpers
// Provides tools for encrypting, decrypting, and migrating databases

using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Security.Cryptography;
using FathomOS.Core.Security;

namespace FathomOS.Core.Data;

/// <summary>
/// Database encryption status
/// </summary>
public enum EncryptionStatus
{
    /// <summary>
    /// Database is not encrypted
    /// </summary>
    Unencrypted,

    /// <summary>
    /// Database is encrypted with the current key
    /// </summary>
    EncryptedWithCurrentKey,

    /// <summary>
    /// Database is encrypted with a different key
    /// </summary>
    EncryptedWithDifferentKey,

    /// <summary>
    /// Database file does not exist
    /// </summary>
    NotExists,

    /// <summary>
    /// Unable to determine encryption status
    /// </summary>
    Unknown
}

/// <summary>
/// Result of a database encryption operation
/// </summary>
public class EncryptionResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Path to the original database (if backed up)
    /// </summary>
    public string? BackupPath { get; set; }

    /// <summary>
    /// Time taken for the operation
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Key derivation configuration
/// </summary>
public class KeyDerivationConfig
{
    /// <summary>
    /// Number of PBKDF2 iterations (default: 600000 for OWASP 2023 recommendation)
    /// </summary>
    public int Iterations { get; set; } = 600000;

    /// <summary>
    /// Salt length in bytes
    /// </summary>
    public int SaltLength { get; set; } = 32;

    /// <summary>
    /// Key length in bytes (256 bits for AES-256)
    /// </summary>
    public int KeyLength { get; set; } = 32;
}

/// <summary>
/// Provides database encryption utilities and migration tools
/// </summary>
public static class DatabaseEncryption
{
    private const int KeySize = 32; // 256 bits

    /// <summary>
    /// Checks the encryption status of a database file
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <returns>The encryption status</returns>
    public static EncryptionStatus CheckEncryptionStatus(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return EncryptionStatus.NotExists;
        }

        try
        {
            // Try to open without encryption
            var unencryptedConnection = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            using var connection = new SqliteConnection(unencryptedConnection);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM sqlite_master;";
            command.ExecuteScalar();

            return EncryptionStatus.Unencrypted;
        }
        catch (SqliteException)
        {
            // Database is likely encrypted, try with current key
            try
            {
                var key = SecretManager.GetOrCreateDatabaseKey();
                return TryOpenWithKey(databasePath, key)
                    ? EncryptionStatus.EncryptedWithCurrentKey
                    : EncryptionStatus.EncryptedWithDifferentKey;
            }
            catch
            {
                return EncryptionStatus.EncryptedWithDifferentKey;
            }
        }
        catch
        {
            return EncryptionStatus.Unknown;
        }
    }

    /// <summary>
    /// Attempts to open a database with a specific key
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <param name="key">Encryption key to try</param>
    /// <returns>True if the key works</returns>
    public static bool TryOpenWithKey(string databasePath, string key)
    {
        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var keyCommand = connection.CreateCommand();
            keyCommand.CommandText = $"PRAGMA key = '{key}';";
            keyCommand.ExecuteNonQuery();

            using var verifyCommand = connection.CreateCommand();
            verifyCommand.CommandText = "SELECT count(*) FROM sqlite_master;";
            verifyCommand.ExecuteScalar();

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Encrypts an unencrypted database
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <param name="key">Encryption key (if null, uses system key)</param>
    /// <param name="createBackup">Whether to create a backup first</param>
    /// <returns>Result of the encryption operation</returns>
    public static async Task<EncryptionResult> EncryptDatabaseAsync(
        string databasePath,
        string? key = null,
        bool createBackup = true)
    {
        var result = new EncryptionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!File.Exists(databasePath))
            {
                result.Success = false;
                result.ErrorMessage = "Database file does not exist.";
                return result;
            }

            var status = CheckEncryptionStatus(databasePath);
            if (status != EncryptionStatus.Unencrypted)
            {
                result.Success = false;
                result.ErrorMessage = $"Database is not unencrypted. Status: {status}";
                return result;
            }

            // Create backup
            if (createBackup)
            {
                result.BackupPath = CreateBackup(databasePath);
            }

            var encryptionKey = key ?? SecretManager.GetOrCreateDatabaseKey();
            var tempPath = databasePath + ".encrypted.tmp";

            try
            {
                // Export to new encrypted database using ATTACH and sqlcipher_export
                await EncryptUsingAttachAsync(databasePath, tempPath, encryptionKey);

                // Replace original with encrypted
                File.Delete(databasePath);
                File.Move(tempPath, databasePath);

                result.Success = true;
            }
            finally
            {
                // Clean up temp file if it exists
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Decrypts an encrypted database
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <param name="key">Decryption key (if null, uses system key)</param>
    /// <param name="createBackup">Whether to create a backup first</param>
    /// <returns>Result of the decryption operation</returns>
    public static async Task<EncryptionResult> DecryptDatabaseAsync(
        string databasePath,
        string? key = null,
        bool createBackup = true)
    {
        var result = new EncryptionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!File.Exists(databasePath))
            {
                result.Success = false;
                result.ErrorMessage = "Database file does not exist.";
                return result;
            }

            var decryptionKey = key ?? SecretManager.GetOrCreateDatabaseKey();

            if (!TryOpenWithKey(databasePath, decryptionKey))
            {
                result.Success = false;
                result.ErrorMessage = "Cannot open database with the provided key.";
                return result;
            }

            // Create backup
            if (createBackup)
            {
                result.BackupPath = CreateBackup(databasePath);
            }

            var tempPath = databasePath + ".decrypted.tmp";

            try
            {
                await DecryptUsingAttachAsync(databasePath, tempPath, decryptionKey);

                // Replace original with decrypted
                File.Delete(databasePath);
                File.Move(tempPath, databasePath);

                result.Success = true;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Changes the encryption key for a database
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <param name="currentKey">Current encryption key</param>
    /// <param name="newKey">New encryption key</param>
    /// <param name="createBackup">Whether to create a backup first</param>
    /// <returns>Result of the rekey operation</returns>
    public static async Task<EncryptionResult> ChangeKeyAsync(
        string databasePath,
        string currentKey,
        string newKey,
        bool createBackup = true)
    {
        var result = new EncryptionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!File.Exists(databasePath))
            {
                result.Success = false;
                result.ErrorMessage = "Database file does not exist.";
                return result;
            }

            if (!TryOpenWithKey(databasePath, currentKey))
            {
                result.Success = false;
                result.ErrorMessage = "Cannot open database with the current key.";
                return result;
            }

            // Create backup
            if (createBackup)
            {
                result.BackupPath = CreateBackup(databasePath);
            }

            // Open and rekey
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWrite
            }.ToString();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            await using var keyCommand = connection.CreateCommand();
            keyCommand.CommandText = $"PRAGMA key = '{currentKey}';";
            await keyCommand.ExecuteNonQueryAsync();

            // Verify we can read
            await using var verifyCommand = connection.CreateCommand();
            verifyCommand.CommandText = "SELECT count(*) FROM sqlite_master;";
            await verifyCommand.ExecuteScalarAsync();

            // Rekey
            await using var rekeyCommand = connection.CreateCommand();
            rekeyCommand.CommandText = $"PRAGMA rekey = '{newKey}';";
            await rekeyCommand.ExecuteNonQueryAsync();

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Derives an encryption key from a password and machine ID
    /// </summary>
    /// <param name="password">User password</param>
    /// <param name="machineId">Machine identifier (optional, uses system ID if null)</param>
    /// <param name="config">Key derivation configuration</param>
    /// <returns>Base64-encoded derived key</returns>
    public static string DeriveKey(
        string password,
        string? machineId = null,
        KeyDerivationConfig? config = null)
    {
        config ??= new KeyDerivationConfig();

        // Use machine ID as part of the salt for machine-binding
        var machine = machineId ?? GetMachineId();
        var saltSource = $"FathomOS.Database.{machine}";
        var salt = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(saltSource));

        // Use only the first SaltLength bytes of the hash as salt
        var actualSalt = new byte[config.SaltLength];
        Array.Copy(salt, actualSalt, Math.Min(salt.Length, config.SaltLength));

        // Derive key using PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            actualSalt,
            config.Iterations,
            HashAlgorithmName.SHA256);

        var key = pbkdf2.GetBytes(config.KeyLength);
        var result = Convert.ToBase64String(key);

        // Clear sensitive data
        CryptographicOperations.ZeroMemory(key);

        return result;
    }

    /// <summary>
    /// Validates database integrity
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <param name="key">Encryption key (if null, uses system key for encrypted DBs)</param>
    /// <returns>True if database passes integrity check</returns>
    public static async Task<bool> ValidateIntegrityAsync(string databasePath, string? key = null)
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Try with key if encrypted
            var status = CheckEncryptionStatus(databasePath);
            if (status == EncryptionStatus.EncryptedWithCurrentKey ||
                status == EncryptionStatus.EncryptedWithDifferentKey)
            {
                var encryptionKey = key ?? SecretManager.GetOrCreateDatabaseKey();
                await using var keyCommand = connection.CreateCommand();
                keyCommand.CommandText = $"PRAGMA key = '{encryptionKey}';";
                await keyCommand.ExecuteNonQueryAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetString(0).Equals("ok", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string CreateBackup(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(databasePath);
        var extension = Path.GetExtension(databasePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        var backupDir = Path.Combine(directory, "Backups");
        Directory.CreateDirectory(backupDir);

        var backupPath = Path.Combine(backupDir, $"{fileName}_{timestamp}{extension}");
        File.Copy(databasePath, backupPath, overwrite: true);

        return backupPath;
    }

    private static async Task EncryptUsingAttachAsync(string sourcePath, string targetPath, string key)
    {
        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        await using var connection = new SqliteConnection(sourceConnectionString);
        await connection.OpenAsync();

        // Attach an encrypted database
        await using var attachCommand = connection.CreateCommand();
        attachCommand.CommandText = $"ATTACH DATABASE '{targetPath}' AS encrypted KEY '{key}';";
        await attachCommand.ExecuteNonQueryAsync();

        // Export schema and data
        await using var exportCommand = connection.CreateCommand();
        exportCommand.CommandText = "SELECT sqlcipher_export('encrypted');";
        try
        {
            await exportCommand.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // sqlcipher_export may not be available, use manual copy
            await ManualExportAsync(connection, "encrypted");
        }

        // Detach
        await using var detachCommand = connection.CreateCommand();
        detachCommand.CommandText = "DETACH DATABASE encrypted;";
        await detachCommand.ExecuteNonQueryAsync();
    }

    private static async Task DecryptUsingAttachAsync(string sourcePath, string targetPath, string key)
    {
        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        await using var connection = new SqliteConnection(sourceConnectionString);
        await connection.OpenAsync();

        // Set key for source
        await using var keyCommand = connection.CreateCommand();
        keyCommand.CommandText = $"PRAGMA key = '{key}';";
        await keyCommand.ExecuteNonQueryAsync();

        // Verify source
        await using var verifyCommand = connection.CreateCommand();
        verifyCommand.CommandText = "SELECT count(*) FROM sqlite_master;";
        await verifyCommand.ExecuteScalarAsync();

        // Attach unencrypted database (empty key)
        await using var attachCommand = connection.CreateCommand();
        attachCommand.CommandText = $"ATTACH DATABASE '{targetPath}' AS plaintext KEY '';";
        await attachCommand.ExecuteNonQueryAsync();

        // Export
        await using var exportCommand = connection.CreateCommand();
        exportCommand.CommandText = "SELECT sqlcipher_export('plaintext');";
        try
        {
            await exportCommand.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            await ManualExportAsync(connection, "plaintext");
        }

        // Detach
        await using var detachCommand = connection.CreateCommand();
        detachCommand.CommandText = "DETACH DATABASE plaintext;";
        await detachCommand.ExecuteNonQueryAsync();
    }

    private static async Task ManualExportAsync(SqliteConnection connection, string targetSchema)
    {
        // Get all table creation SQL
        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = @"
            SELECT sql FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND sql IS NOT NULL;";

        var createStatements = new List<string>();
        await using (var reader = await tableCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var sql = reader.GetString(0);
                // Modify to create in target schema
                sql = sql.Replace("CREATE TABLE ", $"CREATE TABLE {targetSchema}.");
                createStatements.Add(sql);
            }
        }

        // Create tables in target
        foreach (var sql in createStatements)
        {
            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = sql;
            await createCommand.ExecuteNonQueryAsync();
        }

        // Copy data for each table
        await using var tableListCommand = connection.CreateCommand();
        tableListCommand.CommandText = @"
            SELECT name FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";

        var tableNames = new List<string>();
        await using (var reader = await tableListCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        foreach (var tableName in tableNames)
        {
            await using var copyCommand = connection.CreateCommand();
            copyCommand.CommandText = $"INSERT INTO {targetSchema}.{tableName} SELECT * FROM main.{tableName};";
            await copyCommand.ExecuteNonQueryAsync();
        }

        // Copy indexes
        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
            SELECT sql FROM sqlite_master
            WHERE type = 'index' AND name NOT LIKE 'sqlite_%' AND sql IS NOT NULL;";

        await using (var reader = await indexCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var sql = reader.GetString(0);
                sql = sql.Replace("CREATE INDEX ", $"CREATE INDEX {targetSchema}.")
                         .Replace(" ON ", $" ON {targetSchema}.");

                await using var createIndexCommand = connection.CreateCommand();
                createIndexCommand.CommandText = sql;
                try
                {
                    await createIndexCommand.ExecuteNonQueryAsync();
                }
                catch { /* Ignore index creation errors */ }
            }
        }
    }

    private static string GetMachineId()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid") as string ?? Environment.MachineName;
        }
        catch
        {
            return Environment.MachineName;
        }
    }
}
