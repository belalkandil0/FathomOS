// FathomOS.Core/Data/Migrations/Migration002_CreateOfflineQueue.cs
// Migration to create the OfflineQueue table
// Stores operations for offline-first sync pattern

using Microsoft.Data.Sqlite;

namespace FathomOS.Core.Data.Migrations;

/// <summary>
/// Creates the OfflineQueue table for storing offline operations.
/// Operations are queued when offline and processed when connectivity is restored.
/// </summary>
public class Migration002_CreateOfflineQueue : MigrationBase
{
    public override int Version => 2;
    public override string Description => "Create OfflineQueue table for offline operation storage";

    public override void Up(SqliteConnection connection, SqliteTransaction transaction)
    {
        // Create the OfflineQueue table
        Execute(connection, transaction, @"
            CREATE TABLE IF NOT EXISTS OfflineQueue (
                -- Primary identifier
                Id TEXT PRIMARY KEY NOT NULL,

                -- Operation details
                OperationType TEXT NOT NULL,
                EntityType TEXT NOT NULL,
                EntityId TEXT NOT NULL,
                PayloadJson TEXT,

                -- Status tracking
                Status TEXT NOT NULL DEFAULT 'Pending',
                Attempts INTEGER NOT NULL DEFAULT 0,
                MaxAttempts INTEGER NOT NULL DEFAULT 5,

                -- Timestamps (all UTC, ISO 8601 format)
                CreatedAt TEXT NOT NULL,
                LastAttemptAt TEXT,
                CompletedAt TEXT,

                -- Error handling
                ErrorMessage TEXT,

                -- Priority (lower = higher priority)
                Priority INTEGER NOT NULL DEFAULT 100,

                -- Correlation for grouping related operations
                CorrelationId TEXT,

                -- User tracking
                UserId TEXT
            );
        ");

        // Create indexes for common query patterns

        // Index for getting pending operations
        Execute(connection, transaction, @"
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_status
            ON OfflineQueue(Status);
        ");

        // Composite index for ordered pending retrieval
        Execute(connection, transaction, @"
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_pending_ordered
            ON OfflineQueue(Status, Priority, CreatedAt)
            WHERE Status IN ('Pending', 'Failed');
        ");

        // Index for entity-based queries
        Execute(connection, transaction, @"
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_entity
            ON OfflineQueue(EntityType, EntityId);
        ");

        // Index for correlation-based queries
        Execute(connection, transaction, @"
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_correlation
            ON OfflineQueue(CorrelationId)
            WHERE CorrelationId IS NOT NULL;
        ");

        // Index for user-based queries
        Execute(connection, transaction, @"
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_user
            ON OfflineQueue(UserId)
            WHERE UserId IS NOT NULL;
        ");

        // Index for cleanup of completed operations
        Execute(connection, transaction, @"
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_completed
            ON OfflineQueue(CompletedAt)
            WHERE Status = 'Completed';
        ");
    }

    public override void Down(SqliteConnection connection, SqliteTransaction transaction)
    {
        // Drop indexes first
        DropIndexIfExists(connection, transaction, "idx_offlinequeue_status");
        DropIndexIfExists(connection, transaction, "idx_offlinequeue_pending_ordered");
        DropIndexIfExists(connection, transaction, "idx_offlinequeue_entity");
        DropIndexIfExists(connection, transaction, "idx_offlinequeue_correlation");
        DropIndexIfExists(connection, transaction, "idx_offlinequeue_user");
        DropIndexIfExists(connection, transaction, "idx_offlinequeue_completed");

        // Drop the table
        DropTableIfExists(connection, transaction, "OfflineQueue");
    }
}
