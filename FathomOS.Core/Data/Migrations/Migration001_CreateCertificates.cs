// FathomOS.Core/Data/Migrations/Migration001_CreateCertificates.cs
// Initial migration to create the Certificates table
// Stores processing certificates for offline-first sync

using Microsoft.Data.Sqlite;

namespace FathomOS.Core.Data.Migrations;

/// <summary>
/// Initial migration: Creates the Certificates table for storing processing certificates.
/// Certificates are created locally and synced to the server when connectivity is available.
/// </summary>
public class Migration001_CreateCertificates : MigrationBase
{
    public override int Version => 1;
    public override string Description => "Create Certificates table for processing certificate storage";

    public override void Up(SqliteConnection connection, SqliteTransaction transaction)
    {
        // Create the Certificates table
        // Schema designed to store ProcessingCertificate model with sync support
        Execute(connection, transaction, @"
            CREATE TABLE Certificates (
                -- Primary identifier (format: FOS-{LicenseeCode}-{YYMM}-{Sequence}-{Check})
                CertificateId TEXT PRIMARY KEY NOT NULL,

                -- License information
                LicenseId TEXT NOT NULL,
                LicenseeCode TEXT NOT NULL,

                -- Module information
                ModuleId TEXT NOT NULL,
                ModuleCertificateCode TEXT NOT NULL,
                ModuleVersion TEXT NOT NULL,

                -- Certificate metadata
                IssuedAt TEXT NOT NULL,
                ProjectName TEXT NOT NULL,
                ProjectLocation TEXT,
                Vessel TEXT,
                Client TEXT,

                -- Signatory information
                SignatoryName TEXT NOT NULL,
                SignatoryTitle TEXT,
                CompanyName TEXT NOT NULL,

                -- Processing data (JSON)
                ProcessingDataJson TEXT,

                -- File lists (JSON arrays)
                InputFilesJson TEXT,
                OutputFilesJson TEXT,

                -- Cryptographic signature
                Signature TEXT NOT NULL,
                SignatureAlgorithm TEXT NOT NULL DEFAULT 'SHA256withECDSA',

                -- Data hash for integrity verification
                DataHash TEXT,

                -- Sync status tracking
                SyncStatus TEXT NOT NULL DEFAULT 'pending',
                SyncedAt TEXT,
                SyncError TEXT,
                SyncAttempts INTEGER NOT NULL DEFAULT 0,

                -- Audit fields (all UTC)
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                UpdatedAt TEXT,

                -- QR code for verification
                QrCodeUrl TEXT,

                -- Edition information from license
                EditionName TEXT
            );
        ");

        // Create indexes for common query patterns

        // Index for sync operations (most important for offline-first)
        Execute(connection, transaction, @"
            CREATE INDEX idx_certificates_sync_status
            ON Certificates(SyncStatus);
        ");

        // Index for client/licensee lookup
        Execute(connection, transaction, @"
            CREATE INDEX idx_certificates_licensee
            ON Certificates(LicenseeCode);
        ");

        // Index for license lookup
        Execute(connection, transaction, @"
            CREATE INDEX idx_certificates_license
            ON Certificates(LicenseId);
        ");

        // Index for module-based queries
        Execute(connection, transaction, @"
            CREATE INDEX idx_certificates_module
            ON Certificates(ModuleId);
        ");

        // Index for date-based queries
        Execute(connection, transaction, @"
            CREATE INDEX idx_certificates_issued
            ON Certificates(IssuedAt);
        ");

        // Composite index for common filter: pending sync by license
        Execute(connection, transaction, @"
            CREATE INDEX idx_certificates_pending_by_license
            ON Certificates(LicenseId, SyncStatus)
            WHERE SyncStatus = 'pending';
        ");

        // Index for project lookup
        Execute(connection, transaction, @"
            CREATE INDEX idx_certificates_project
            ON Certificates(ProjectName);
        ");
    }

    public override void Down(SqliteConnection connection, SqliteTransaction transaction)
    {
        // Drop indexes first
        DropIndexIfExists(connection, transaction, "idx_certificates_sync_status");
        DropIndexIfExists(connection, transaction, "idx_certificates_licensee");
        DropIndexIfExists(connection, transaction, "idx_certificates_license");
        DropIndexIfExists(connection, transaction, "idx_certificates_module");
        DropIndexIfExists(connection, transaction, "idx_certificates_issued");
        DropIndexIfExists(connection, transaction, "idx_certificates_pending_by_license");
        DropIndexIfExists(connection, transaction, "idx_certificates_project");

        // Drop the table
        DropTableIfExists(connection, transaction, "Certificates");
    }
}
