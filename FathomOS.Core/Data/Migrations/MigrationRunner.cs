// FathomOS.Core/Data/Migrations/MigrationRunner.cs
// Executes database migrations in order
// Tracks migration history and supports rollback

using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace FathomOS.Core.Data.Migrations;

/// <summary>
/// Runs database migrations and tracks migration history.
/// Ensures migrations are applied in version order and only once.
/// </summary>
public class MigrationRunner
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly List<IMigration> _migrations = new();
    private const string MigrationTableName = "__MigrationHistory";

    /// <summary>
    /// Creates a new migration runner
    /// </summary>
    /// <param name="connectionFactory">Connection factory for database access</param>
    public MigrationRunner(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Registers a migration with the runner
    /// </summary>
    /// <param name="migration">Migration to register</param>
    public MigrationRunner Register(IMigration migration)
    {
        if (migration == null) throw new ArgumentNullException(nameof(migration));

        // Check for duplicate versions
        if (_migrations.Any(m => m.Version == migration.Version))
        {
            throw new InvalidOperationException(
                $"Migration version {migration.Version} is already registered.");
        }

        _migrations.Add(migration);
        return this;
    }

    /// <summary>
    /// Registers multiple migrations
    /// </summary>
    /// <param name="migrations">Migrations to register</param>
    public MigrationRunner RegisterAll(IEnumerable<IMigration> migrations)
    {
        foreach (var migration in migrations)
        {
            Register(migration);
        }
        return this;
    }

    /// <summary>
    /// Discovers and registers all migrations from an assembly
    /// </summary>
    /// <param name="assembly">Assembly to scan for migrations</param>
    public MigrationRunner DiscoverMigrations(System.Reflection.Assembly assembly)
    {
        var migrationTypes = assembly.GetTypes()
            .Where(t => typeof(IMigration).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in migrationTypes)
        {
            if (Activator.CreateInstance(type) is IMigration migration)
            {
                Register(migration);
            }
        }

        return this;
    }

    /// <summary>
    /// Runs all pending migrations
    /// </summary>
    /// <param name="createBackup">Whether to create a backup before migrating</param>
    /// <returns>Results of all executed migrations</returns>
    public async Task<List<MigrationResult>> MigrateAsync(bool createBackup = true)
    {
        var results = new List<MigrationResult>();

        // Create backup before migrations
        if (createBackup && File.Exists(_connectionFactory.DatabasePath))
        {
            try
            {
                var backupPath = _connectionFactory.BackupWithTimestamp();
                Debug.WriteLine($"Migration backup created: {backupPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create migration backup: {ex.Message}");
            }
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync();

        // Ensure migration history table exists
        await EnsureMigrationTableAsync(connection);

        // Get applied migrations
        var appliedVersions = await GetAppliedMigrationsAsync(connection);

        // Get pending migrations (ordered by version)
        var pendingMigrations = _migrations
            .Where(m => !appliedVersions.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pendingMigrations.Count == 0)
        {
            Debug.WriteLine("No pending migrations to apply.");
            return results;
        }

        Debug.WriteLine($"Applying {pendingMigrations.Count} pending migration(s)...");

        // Apply each migration in a separate transaction
        foreach (var migration in pendingMigrations)
        {
            var result = await ApplyMigrationAsync(connection, migration);
            results.Add(result);

            if (!result.Success)
            {
                Debug.WriteLine($"Migration {migration.Version} failed. Stopping migration process.");
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Rolls back to a specific version
    /// </summary>
    /// <param name="targetVersion">Target version to roll back to</param>
    /// <returns>Results of all rolled back migrations</returns>
    public async Task<List<MigrationResult>> RollbackToAsync(int targetVersion)
    {
        var results = new List<MigrationResult>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await EnsureMigrationTableAsync(connection);

        var appliedVersions = await GetAppliedMigrationsAsync(connection);

        // Get migrations to rollback (in reverse order)
        var migrationsToRollback = _migrations
            .Where(m => appliedVersions.Contains(m.Version) && m.Version > targetVersion)
            .OrderByDescending(m => m.Version)
            .ToList();

        foreach (var migration in migrationsToRollback)
        {
            var result = await RollbackMigrationAsync(connection, migration);
            results.Add(result);

            if (!result.Success)
            {
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Gets the current database version
    /// </summary>
    /// <returns>Highest applied migration version, or 0 if none applied</returns>
    public async Task<int> GetCurrentVersionAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await EnsureMigrationTableAsync(connection);

        var appliedVersions = await GetAppliedMigrationsAsync(connection);
        return appliedVersions.Count > 0 ? appliedVersions.Max() : 0;
    }

    /// <summary>
    /// Gets list of pending migrations
    /// </summary>
    /// <returns>List of migration info for pending migrations</returns>
    public async Task<List<(int Version, string Description)>> GetPendingMigrationsAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await EnsureMigrationTableAsync(connection);

        var appliedVersions = await GetAppliedMigrationsAsync(connection);

        return _migrations
            .Where(m => !appliedVersions.Contains(m.Version))
            .OrderBy(m => m.Version)
            .Select(m => (m.Version, m.Description))
            .ToList();
    }

    /// <summary>
    /// Gets migration history
    /// </summary>
    /// <returns>List of applied migrations with timestamps</returns>
    public async Task<List<(int Version, string Description, DateTime AppliedAt)>> GetMigrationHistoryAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await EnsureMigrationTableAsync(connection);

        var results = new List<(int Version, string Description, DateTime AppliedAt)>();

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT Version, Description, AppliedAt
            FROM {MigrationTableName}
            ORDER BY Version;";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((
                reader.GetInt32(0),
                reader.GetString(1),
                DateTime.Parse(reader.GetString(2))
            ));
        }

        return results;
    }

    private async Task EnsureMigrationTableAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {MigrationTableName} (
                Version INTEGER PRIMARY KEY,
                Description TEXT NOT NULL,
                AppliedAt TEXT NOT NULL
            );";
        await command.ExecuteNonQueryAsync();
    }

    private async Task<HashSet<int>> GetAppliedMigrationsAsync(SqliteConnection connection)
    {
        var versions = new HashSet<int>();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Version FROM {MigrationTableName};";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private async Task<MigrationResult> ApplyMigrationAsync(SqliteConnection connection, IMigration migration)
    {
        var result = new MigrationResult
        {
            Version = migration.Version,
            Description = migration.Description
        };

        var stopwatch = Stopwatch.StartNew();

        await using var transaction = connection.BeginTransaction();
        try
        {
            Debug.WriteLine($"Applying migration {migration.Version}: {migration.Description}");

            // Execute the migration
            migration.Up(connection, transaction);

            // Record the migration
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                INSERT INTO {MigrationTableName} (Version, Description, AppliedAt)
                VALUES (@version, @description, @appliedAt);";
            command.Parameters.AddWithValue("@version", migration.Version);
            command.Parameters.AddWithValue("@description", migration.Description);
            command.Parameters.AddWithValue("@appliedAt", DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            result.Success = true;
            Debug.WriteLine($"Migration {migration.Version} completed successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Debug.WriteLine($"Migration {migration.Version} failed: {ex.Message}");
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.ExecutedAt = DateTime.UtcNow;

        return result;
    }

    private async Task<MigrationResult> RollbackMigrationAsync(SqliteConnection connection, IMigration migration)
    {
        var result = new MigrationResult
        {
            Version = migration.Version,
            Description = $"Rollback: {migration.Description}"
        };

        var stopwatch = Stopwatch.StartNew();

        await using var transaction = connection.BeginTransaction();
        try
        {
            Debug.WriteLine($"Rolling back migration {migration.Version}: {migration.Description}");

            // Execute the rollback
            migration.Down(connection, transaction);

            // Remove the migration record
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {MigrationTableName} WHERE Version = @version;";
            command.Parameters.AddWithValue("@version", migration.Version);

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            result.Success = true;
            Debug.WriteLine($"Rollback of migration {migration.Version} completed successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Debug.WriteLine($"Rollback of migration {migration.Version} failed: {ex.Message}");
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.ExecutedAt = DateTime.UtcNow;

        return result;
    }
}
