// FathomOS.Core/Data/Migrations/IMigrationRunner.cs
// Interface for the migration runner
// Provides contract for database schema evolution

namespace FathomOS.Core.Data.Migrations;

/// <summary>
/// Interface for running database migrations
/// </summary>
public interface IMigrationRunner
{
    /// <summary>
    /// Gets the current database version (highest applied migration)
    /// </summary>
    int CurrentVersion { get; }

    /// <summary>
    /// Gets the target version (highest registered migration)
    /// </summary>
    int TargetVersion { get; }

    /// <summary>
    /// Gets whether any migrations are pending
    /// </summary>
    bool HasPendingMigrations { get; }

    /// <summary>
    /// Migrates the database to the latest version or specified target
    /// </summary>
    /// <param name="targetVersion">Target version (null for latest)</param>
    /// <returns>Combined result of all migration operations</returns>
    Task<MigrationBatchResult> MigrateAsync(int? targetVersion = null);

    /// <summary>
    /// Rolls back the specified number of migrations
    /// </summary>
    /// <param name="steps">Number of migrations to roll back</param>
    /// <returns>Combined result of all rollback operations</returns>
    Task<MigrationBatchResult> RollbackAsync(int steps = 1);

    /// <summary>
    /// Rolls back to a specific version
    /// </summary>
    /// <param name="targetVersion">Target version to roll back to</param>
    /// <returns>Combined result of all rollback operations</returns>
    Task<MigrationBatchResult> RollbackToAsync(int targetVersion);

    /// <summary>
    /// Gets all pending migrations that would be applied
    /// </summary>
    /// <returns>Collection of pending migration information</returns>
    IEnumerable<MigrationInfo> GetPendingMigrations();

    /// <summary>
    /// Gets the history of applied migrations
    /// </summary>
    /// <returns>Collection of applied migration information</returns>
    Task<IEnumerable<MigrationHistoryEntry>> GetMigrationHistoryAsync();

    /// <summary>
    /// Registers a migration with the runner
    /// </summary>
    /// <param name="migration">Migration to register</param>
    IMigrationRunner Register(IMigration migration);

    /// <summary>
    /// Registers multiple migrations with the runner
    /// </summary>
    /// <param name="migrations">Migrations to register</param>
    IMigrationRunner RegisterAll(IEnumerable<IMigration> migrations);

    /// <summary>
    /// Discovers and registers all migrations from an assembly
    /// </summary>
    /// <param name="assembly">Assembly to scan</param>
    IMigrationRunner DiscoverMigrations(System.Reflection.Assembly assembly);
}

/// <summary>
/// Information about a migration
/// </summary>
public class MigrationInfo
{
    /// <summary>
    /// Migration version number
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Description of the migration
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the migration was created
    /// </summary>
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Entry in the migration history
/// </summary>
public class MigrationHistoryEntry
{
    /// <summary>
    /// Migration version number
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Description of the migration
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the migration was applied
    /// </summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// Duration of the migration
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// User or system that applied the migration
    /// </summary>
    public string? AppliedBy { get; set; }
}

/// <summary>
/// Result of a batch migration operation
/// </summary>
public class MigrationBatchResult
{
    /// <summary>
    /// Whether all migrations succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total number of migrations attempted
    /// </summary>
    public int TotalMigrations { get; set; }

    /// <summary>
    /// Number of successful migrations
    /// </summary>
    public int SuccessfulMigrations { get; set; }

    /// <summary>
    /// Number of failed migrations
    /// </summary>
    public int FailedMigrations { get; set; }

    /// <summary>
    /// Starting version before migration
    /// </summary>
    public int StartVersion { get; set; }

    /// <summary>
    /// Final version after migration
    /// </summary>
    public int EndVersion { get; set; }

    /// <summary>
    /// Total time for all migrations
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Individual migration results
    /// </summary>
    public List<MigrationResult> Results { get; set; } = new();

    /// <summary>
    /// Error message if batch failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Path to backup created before migration
    /// </summary>
    public string? BackupPath { get; set; }
}

/// <summary>
/// Extended migration interface with metadata
/// </summary>
public interface IMigrationWithMetadata : IMigration
{
    /// <summary>
    /// When this migration was created
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Author of the migration
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// Additional notes about the migration
    /// </summary>
    string? Notes { get; }

    /// <summary>
    /// Whether this migration can be safely rolled back
    /// </summary>
    bool IsReversible { get; }

    /// <summary>
    /// Estimated duration for large databases
    /// </summary>
    TimeSpan? EstimatedDuration { get; }
}

/// <summary>
/// Extended base class for migrations with metadata
/// </summary>
public abstract class MigrationWithMetadata : MigrationBase, IMigrationWithMetadata
{
    /// <inheritdoc />
    public virtual DateTime CreatedAt => DateTime.UtcNow;

    /// <inheritdoc />
    public virtual string? Author => null;

    /// <inheritdoc />
    public virtual string? Notes => null;

    /// <inheritdoc />
    public virtual bool IsReversible => true;

    /// <inheritdoc />
    public virtual TimeSpan? EstimatedDuration => null;
}
