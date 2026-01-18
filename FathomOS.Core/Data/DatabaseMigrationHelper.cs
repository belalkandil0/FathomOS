// FathomOS.Core/Data/DatabaseMigrationHelper.cs
// SECURITY FIX: VULN-004 / MISSING-001 - Database encryption migration utility
// Provides utilities to migrate existing unencrypted databases to encrypted format

using Microsoft.Data.Sqlite;
using FathomOS.Core.Security;

namespace FathomOS.Core.Data;

/// <summary>
/// SECURITY FIX: Helper class for migrating unencrypted SQLite databases to encrypted format.
/// This utility handles the transition from plaintext to SQLCipher encrypted databases.
/// </summary>
public static class DatabaseMigrationHelper
{
    /// <summary>
    /// SECURITY FIX: Checks if a database file is encrypted.
    /// An unencrypted SQLite database starts with "SQLite format 3" header.
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    /// <returns>True if the database appears to be encrypted, false if plaintext</returns>
    public static bool IsDatabaseEncrypted(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            // New database - will be created as encrypted
            return true;
        }

        try
        {
            // SECURITY FIX: Read first 16 bytes to check SQLite header
            // Unencrypted SQLite databases start with "SQLite format 3\0"
            using var fs = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[16];
            var bytesRead = fs.Read(header, 0, 16);

            if (bytesRead < 16)
            {
                // File too small - could be empty or corrupted
                return true;
            }

            // Check for SQLite format 3 header (plaintext database)
            var sqliteHeader = System.Text.Encoding.ASCII.GetString(header, 0, 15);
            return sqliteHeader != "SQLite format 3";
        }
        catch
        {
            // If we can't read the file, assume it's encrypted (safe default)
            return true;
        }
    }

    /// <summary>
    /// SECURITY FIX: Migrates an unencrypted database to encrypted format.
    /// Creates a backup of the original database before migration.
    /// </summary>
    /// <param name="databasePath">Path to the unencrypted database</param>
    /// <param name="backupPath">Optional path for the backup. If null, uses default backup location.</param>
    /// <returns>Migration result with status and any error messages</returns>
    public static async Task<MigrationResult> MigrateToEncryptedAsync(string databasePath, string? backupPath = null)
    {
        if (!File.Exists(databasePath))
        {
            return MigrationResult.Success("Database file does not exist. A new encrypted database will be created.");
        }

        if (IsDatabaseEncrypted(databasePath))
        {
            return MigrationResult.Success("Database is already encrypted or will be created as encrypted.");
        }

        // SECURITY FIX: Create backup before migration
        backupPath ??= databasePath + ".unencrypted.backup";
        try
        {
            File.Copy(databasePath, backupPath, overwrite: true);
        }
        catch (Exception ex)
        {
            return MigrationResult.Failure($"Failed to create backup: {ex.Message}");
        }

        var tempEncryptedPath = databasePath + ".encrypted.tmp";

        try
        {
            // SECURITY FIX: Get the encryption key that will be used
            var encryptionKey = SecretManager.GetOrCreateDatabaseKey();

            // Open source (unencrypted) database
            var sourceConnString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            await using var sourceConn = new SqliteConnection(sourceConnString);
            await sourceConn.OpenAsync();

            // Create destination (encrypted) database
            var destConnString = new SqliteConnectionStringBuilder
            {
                DataSource = tempEncryptedPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            await using var destConn = new SqliteConnection(destConnString);
            await destConn.OpenAsync();

            // SECURITY FIX: Apply encryption key to destination
            await using (var keyCmd = destConn.CreateCommand())
            {
                keyCmd.CommandText = $"PRAGMA key = '{encryptionKey}';";
                await keyCmd.ExecuteNonQueryAsync();
            }

            // SECURITY FIX: Use SQLite backup API through VACUUM INTO or manual copy
            // Export schema and data from source to encrypted destination
            await CopyDatabaseContentsAsync(sourceConn, destConn);

            // Close both connections
            sourceConn.Close();
            destConn.Close();

            // SECURITY FIX: Replace original with encrypted version
            // Use a temporary name swap for atomic replacement
            var originalBackup = databasePath + ".original.tmp";

            try
            {
                File.Move(databasePath, originalBackup);
                File.Move(tempEncryptedPath, databasePath);
                File.Delete(originalBackup);
            }
            catch
            {
                // Restore original if swap failed
                if (File.Exists(originalBackup))
                {
                    File.Move(originalBackup, databasePath, overwrite: true);
                }
                throw;
            }

            return MigrationResult.Success(
                $"Database successfully migrated to encrypted format. Backup saved at: {backupPath}");
        }
        catch (Exception ex)
        {
            // Clean up temp file if it exists
            if (File.Exists(tempEncryptedPath))
            {
                try { File.Delete(tempEncryptedPath); } catch { }
            }

            return MigrationResult.Failure($"Migration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// SECURITY FIX: Synchronous version of MigrateToEncryptedAsync
    /// </summary>
    public static MigrationResult MigrateToEncrypted(string databasePath, string? backupPath = null)
    {
        return MigrateToEncryptedAsync(databasePath, backupPath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// SECURITY FIX: Copies all tables and data from source to destination database
    /// </summary>
    private static async Task CopyDatabaseContentsAsync(SqliteConnection source, SqliteConnection dest)
    {
        // Get list of all user tables
        var tables = new List<string>();
        await using (var cmd = source.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT name FROM sqlite_master
                WHERE type='table' AND name NOT LIKE 'sqlite_%'
                ORDER BY name;";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        foreach (var tableName in tables)
        {
            // Get CREATE TABLE statement
            string? createSql = null;
            await using (var cmd = source.CreateCommand())
            {
                cmd.CommandText = $"SELECT sql FROM sqlite_master WHERE type='table' AND name=@name;";
                cmd.Parameters.AddWithValue("@name", tableName);
                createSql = (await cmd.ExecuteScalarAsync())?.ToString();
            }

            if (string.IsNullOrEmpty(createSql))
                continue;

            // Create table in destination
            await using (var cmd = dest.CreateCommand())
            {
                cmd.CommandText = createSql;
                await cmd.ExecuteNonQueryAsync();
            }

            // Copy data
            await CopyTableDataAsync(source, dest, tableName);
        }

        // Copy indexes
        await CopyIndexesAsync(source, dest);
    }

    /// <summary>
    /// SECURITY FIX: Copies data from a single table
    /// </summary>
    private static async Task CopyTableDataAsync(SqliteConnection source, SqliteConnection dest, string tableName)
    {
        // Get column names
        var columns = new List<string>();
        await using (var cmd = source.CreateCommand())
        {
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1)); // Column name is at index 1
            }
        }

        if (columns.Count == 0)
            return;

        var columnList = string.Join(", ", columns);
        var paramList = string.Join(", ", columns.Select(c => $"@{c}"));

        // Read all rows from source
        await using var selectCmd = source.CreateCommand();
        selectCmd.CommandText = $"SELECT {columnList} FROM {tableName};";

        await using var reader2 = await selectCmd.ExecuteReaderAsync();

        // Begin transaction for batch insert
        await using var transaction = dest.BeginTransaction();

        try
        {
            while (await reader2.ReadAsync())
            {
                await using var insertCmd = dest.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = $"INSERT INTO {tableName} ({columnList}) VALUES ({paramList});";

                for (int i = 0; i < columns.Count; i++)
                {
                    var value = reader2.IsDBNull(i) ? DBNull.Value : reader2.GetValue(i);
                    insertCmd.Parameters.AddWithValue($"@{columns[i]}", value);
                }

                await insertCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// SECURITY FIX: Copies indexes from source to destination
    /// </summary>
    private static async Task CopyIndexesAsync(SqliteConnection source, SqliteConnection dest)
    {
        await using var cmd = source.CreateCommand();
        cmd.CommandText = @"
            SELECT sql FROM sqlite_master
            WHERE type='index' AND sql IS NOT NULL
            ORDER BY name;";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var indexSql = reader.GetString(0);
            await using var createCmd = dest.CreateCommand();
            createCmd.CommandText = indexSql;
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// SECURITY FIX: Checks if migration is needed and performs it if necessary.
    /// Safe to call on every application startup.
    /// </summary>
    /// <param name="databasePath">Path to the database</param>
    /// <returns>True if database is ready (either was already encrypted or migration succeeded)</returns>
    public static async Task<bool> EnsureEncryptedAsync(string databasePath)
    {
        if (IsDatabaseEncrypted(databasePath))
        {
            return true;
        }

        var result = await MigrateToEncryptedAsync(databasePath);
        return result.IsSuccess;
    }
}

/// <summary>
/// SECURITY FIX: Result of a database migration operation
/// </summary>
public class MigrationResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;

    private MigrationResult() { }

    public static MigrationResult Success(string message) => new()
    {
        IsSuccess = true,
        Message = message
    };

    public static MigrationResult Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}
