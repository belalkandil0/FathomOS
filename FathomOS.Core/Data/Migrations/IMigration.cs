// FathomOS.Core/Data/Migrations/IMigration.cs
// Migration interface for SQLite schema evolution
// Based on DATABASE-AGENT specifications

using Microsoft.Data.Sqlite;

namespace FathomOS.Core.Data.Migrations;

/// <summary>
/// Interface for database migrations.
/// Each migration represents a discrete change to the database schema.
/// </summary>
public interface IMigration
{
    /// <summary>
    /// Unique version number for this migration.
    /// Migrations are executed in order of version number.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Human-readable description of what this migration does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Applies the migration (upgrades the schema)
    /// </summary>
    /// <param name="connection">Open SQLite connection</param>
    /// <param name="transaction">Active transaction</param>
    void Up(SqliteConnection connection, SqliteTransaction transaction);

    /// <summary>
    /// Reverts the migration (downgrades the schema)
    /// </summary>
    /// <param name="connection">Open SQLite connection</param>
    /// <param name="transaction">Active transaction</param>
    void Down(SqliteConnection connection, SqliteTransaction transaction);
}

/// <summary>
/// Base class for migrations with common helper methods
/// </summary>
public abstract class MigrationBase : IMigration
{
    public abstract int Version { get; }
    public abstract string Description { get; }

    public abstract void Up(SqliteConnection connection, SqliteTransaction transaction);
    public abstract void Down(SqliteConnection connection, SqliteTransaction transaction);

    /// <summary>
    /// Executes a non-query SQL command
    /// </summary>
    protected void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Executes multiple SQL statements
    /// </summary>
    protected void ExecuteAll(SqliteConnection connection, SqliteTransaction transaction, params string[] statements)
    {
        foreach (var sql in statements)
        {
            if (!string.IsNullOrWhiteSpace(sql))
            {
                Execute(connection, transaction, sql);
            }
        }
    }

    /// <summary>
    /// Checks if a table exists
    /// </summary>
    protected bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='table' AND name=@tableName;";
        command.Parameters.AddWithValue("@tableName", tableName);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Checks if a column exists in a table
    /// </summary>
    protected bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if an index exists
    /// </summary>
    protected bool IndexExists(SqliteConnection connection, string indexName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='index' AND name=@indexName;";
        command.Parameters.AddWithValue("@indexName", indexName);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Adds a column to a table if it doesn't exist
    /// </summary>
    protected void AddColumnIfNotExists(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        string columnName,
        string columnType,
        string? defaultValue = null)
    {
        if (!ColumnExists(connection, tableName, columnName))
        {
            var sql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
            if (defaultValue != null)
            {
                sql += $" DEFAULT {defaultValue}";
            }
            Execute(connection, transaction, sql);
        }
    }

    /// <summary>
    /// Creates an index if it doesn't exist
    /// </summary>
    protected void CreateIndexIfNotExists(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string indexName,
        string tableName,
        params string[] columns)
    {
        if (!IndexExists(connection, indexName))
        {
            var columnList = string.Join(", ", columns);
            Execute(connection, transaction,
                $"CREATE INDEX {indexName} ON {tableName}({columnList});");
        }
    }

    /// <summary>
    /// Drops a table if it exists
    /// </summary>
    protected void DropTableIfExists(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        Execute(connection, transaction, $"DROP TABLE IF EXISTS {tableName};");
    }

    /// <summary>
    /// Drops an index if it exists
    /// </summary>
    protected void DropIndexIfExists(SqliteConnection connection, SqliteTransaction transaction, string indexName)
    {
        Execute(connection, transaction, $"DROP INDEX IF EXISTS {indexName};");
    }
}

/// <summary>
/// Result of a migration operation
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Whether the migration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Migration version that was executed
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Description of the migration
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Error message if migration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the migration was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time taken to execute the migration
    /// </summary>
    public TimeSpan Duration { get; set; }
}
