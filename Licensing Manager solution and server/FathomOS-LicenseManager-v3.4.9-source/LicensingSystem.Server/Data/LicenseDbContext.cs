// LicensingSystem.Server/Data/LicenseDbContext.cs
// Entity Framework Core database context for Fathom OS license data
// Updated with White-Label Branding and Certificate Support

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Shared;
using LicensingSystem.Server.Services;

namespace LicensingSystem.Server.Data;

public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options)
    {
    }

    public DbSet<LicenseKeyRecord> LicenseKeys => Set<LicenseKeyRecord>();
    public DbSet<LicenseActivationRecord> LicenseActivations => Set<LicenseActivationRecord>();
    public DbSet<RevocationRecord> Revocations => Set<RevocationRecord>();
    
    // Module-based licensing tables
    public DbSet<ModuleRecord> Modules => Set<ModuleRecord>();
    public DbSet<LicenseTierRecord> LicenseTiers => Set<LicenseTierRecord>();
    public DbSet<TierModuleRecord> TierModules => Set<TierModuleRecord>();
    
    // Certificate tables (New for Fathom OS)
    public DbSet<CertificateRecord> Certificates => Set<CertificateRecord>();
    public DbSet<CertificateSequenceRecord> CertificateSequences => Set<CertificateSequenceRecord>();
    
    // === NEW TABLES FOR v3.3 ===
    
    // License Transfer Portal
    public DbSet<LicenseTransferRecord> LicenseTransfers => Set<LicenseTransferRecord>();
    public DbSet<TransferVerificationRecord> TransferVerifications => Set<TransferVerificationRecord>();
    
    // Audit Log
    public DbSet<AuditLogRecord> AuditLogs => Set<AuditLogRecord>();
    
    // Admin Authentication & 2FA
    public DbSet<AdminUserRecord> AdminUsers => Set<AdminUserRecord>();
    public DbSet<AdminSessionRecord> AdminSessions => Set<AdminSessionRecord>();
    
    // Active Sessions (Heartbeat tracking)
    public DbSet<ActiveSessionRecord> ActiveSessions => Set<ActiveSessionRecord>();
    
    // Rate Limiting & Security
    public DbSet<RateLimitRecord> RateLimits => Set<RateLimitRecord>();
    public DbSet<BlockedIpRecord> BlockedIps => Set<BlockedIpRecord>();
    
    // Health Monitoring
    public DbSet<ServerHealthMetricRecord> ServerHealthMetrics => Set<ServerHealthMetricRecord>();
    
    // Database Backup Tracking
    public DbSet<DatabaseBackupRecord> DatabaseBackups => Set<DatabaseBackupRecord>();

    // First-Time Setup Configuration
    public DbSet<SetupConfigRecord> SetupConfigs => Set<SetupConfigRecord>();

    // API Key Authentication (replaces admin auth)
    public DbSet<ApiKeyRecord> ApiKeys => Set<ApiKeyRecord>();

    // Synced License Records (for tracking offline licenses)
    public DbSet<SyncedLicenseRecord> SyncedLicenses => Set<SyncedLicenseRecord>();

    // Webhook Delivery Records (for tracking webhook notifications)
    public DbSet<WebhookDeliveryRecord> WebhookDeliveries => Set<WebhookDeliveryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // LicenseKeyRecord configuration
        modelBuilder.Entity<LicenseKeyRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.LicenseId).IsUnique();
            entity.HasIndex(e => e.CustomerEmail);
            entity.HasIndex(e => e.LicenseeCode);

            entity.Property(e => e.Key).HasMaxLength(64).IsRequired();
            entity.Property(e => e.LicenseId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CustomerName).HasMaxLength(256);
            entity.Property(e => e.ProductName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Edition).HasMaxLength(64);
            entity.Property(e => e.Features).HasMaxLength(2048); // Increased for branding features
            
            // White-label branding fields
            entity.Property(e => e.Brand).HasMaxLength(256);
            entity.Property(e => e.LicenseeCode).HasMaxLength(3);
            entity.Property(e => e.SupportCode).HasMaxLength(20);
            entity.Property(e => e.BrandLogoUrl).HasMaxLength(512);
            
            // Database improvements v3.3
            entity.Property(e => e.Notes).HasMaxLength(2048);
            entity.Property(e => e.PurchaseOrderNumber).HasMaxLength(64);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.SalesRep).HasMaxLength(128);
            entity.Property(e => e.ReferralSource).HasMaxLength(256);
            entity.Property(e => e.QrVerificationToken).HasMaxLength(128);

            entity.HasMany(e => e.Activations)
                .WithOne(a => a.LicenseKey)
                .HasForeignKey(a => a.LicenseKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LicenseActivationRecord configuration
        modelBuilder.Entity<LicenseActivationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseKeyId);
            entity.HasIndex(e => e.HardwareFingerprint);

            entity.Property(e => e.HardwareFingerprint).HasMaxLength(128);
            entity.Property(e => e.MachineName).HasMaxLength(256);
            entity.Property(e => e.AppVersion).HasMaxLength(32);
            entity.Property(e => e.OsVersion).HasMaxLength(128);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
        });

        // RevocationRecord configuration
        modelBuilder.Entity<RevocationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseId);
            entity.Property(e => e.Reason).HasMaxLength(512);
        });

        // ModuleRecord configuration
        modelBuilder.Entity<ModuleRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ModuleId).IsUnique();
            entity.HasIndex(e => e.CertificateCode).IsUnique();
            
            entity.Property(e => e.ModuleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.DefaultTier).HasMaxLength(50);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.CertificateCode).HasMaxLength(3);
            entity.Property(e => e.Version).HasMaxLength(20);
        });

        // LicenseTierRecord configuration
        modelBuilder.Entity<LicenseTierRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TierId).IsUnique();
            
            entity.Property(e => e.TierId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // TierModuleRecord configuration (many-to-many)
        modelBuilder.Entity<TierModuleRecord>(entity =>
        {
            entity.HasKey(e => new { e.TierId, e.ModuleId });
            
            entity.Property(e => e.TierId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ModuleId).HasMaxLength(100).IsRequired();
        });

        // CertificateRecord configuration (New for Fathom OS)
        modelBuilder.Entity<CertificateRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CertificateId).IsUnique();
            entity.HasIndex(e => e.LicenseId);
            entity.HasIndex(e => e.LicenseeCode);
            entity.HasIndex(e => e.IssuedAt);
            
            entity.Property(e => e.CertificateId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.LicenseId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.LicenseeCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.ModuleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ModuleCertificateCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.ModuleVersion).HasMaxLength(20);
            entity.Property(e => e.ProjectName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ProjectLocation).HasMaxLength(256);
            entity.Property(e => e.Vessel).HasMaxLength(128);
            entity.Property(e => e.Client).HasMaxLength(256);
            entity.Property(e => e.SignatoryName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.SignatoryTitle).HasMaxLength(128);
            entity.Property(e => e.CompanyName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Signature).HasMaxLength(512);
            entity.Property(e => e.SignatureAlgorithm).HasMaxLength(32);
            
            // Store flexible JSON data as text
            entity.Property(e => e.ProcessingDataJson).HasColumnType("TEXT");
            entity.Property(e => e.InputFilesJson).HasColumnType("TEXT");
            entity.Property(e => e.OutputFilesJson).HasColumnType("TEXT");
        });

        // CertificateSequenceRecord configuration
        modelBuilder.Entity<CertificateSequenceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LicenseeCode, e.YearMonth }).IsUnique();
            
            entity.Property(e => e.LicenseeCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.YearMonth).HasMaxLength(4).IsRequired(); // YYMM format
        });

        // === NEW CONFIGURATIONS FOR v3.3 ===

        // LicenseTransferRecord configuration
        modelBuilder.Entity<LicenseTransferRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseId);
            entity.HasIndex(e => e.TransferToken).IsUnique();
            entity.HasIndex(e => e.RequestedAt);
            
            entity.Property(e => e.LicenseId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.TransferToken).HasMaxLength(64);
            entity.Property(e => e.OldHardwareFingerprint).HasMaxLength(256);
            entity.Property(e => e.NewHardwareFingerprint).HasMaxLength(256);
            entity.Property(e => e.OldMachineName).HasMaxLength(256);
            entity.Property(e => e.NewMachineName).HasMaxLength(256);
            entity.Property(e => e.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.Reason).HasMaxLength(512);
        });

        // TransferVerificationRecord configuration
        modelBuilder.Entity<TransferVerificationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VerificationCode).IsUnique();
            entity.HasIndex(e => e.Email);
            
            entity.Property(e => e.VerificationCode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.LicenseId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Purpose).HasMaxLength(50).IsRequired();
        });

        // AuditLogRecord configuration
        modelBuilder.Entity<AuditLogRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.UserId);
            
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserEmail).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.Property(e => e.OldValues).HasColumnType("TEXT");
            entity.Property(e => e.NewValues).HasColumnType("TEXT");
            entity.Property(e => e.Details).HasMaxLength(2048);
        });

        // AdminUserRecord configuration
        modelBuilder.Entity<AdminUserRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.TwoFactorSecret).HasMaxLength(128);
            entity.Property(e => e.Role).HasMaxLength(50);
        });

        // AdminSessionRecord configuration
        modelBuilder.Entity<AdminSessionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.AdminUserId);
            
            entity.Property(e => e.SessionToken).HasMaxLength(128).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
        });

        // ActiveSessionRecord configuration (heartbeat tracking)
        modelBuilder.Entity<ActiveSessionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseId);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.LastHeartbeat);
            
            entity.Property(e => e.LicenseId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SessionToken).HasMaxLength(128).IsRequired();
            entity.Property(e => e.HardwareFingerprint).HasMaxLength(256);
            entity.Property(e => e.MachineName).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.AppVersion).HasMaxLength(32);
        });

        // RateLimitRecord configuration
        modelBuilder.Entity<RateLimitRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Key, e.WindowStart });
            
            entity.Property(e => e.Key).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(256);
        });

        // BlockedIpRecord configuration
        modelBuilder.Entity<BlockedIpRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IpAddress).IsUnique();
            
            entity.Property(e => e.IpAddress).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(512);
            entity.Property(e => e.BlockedBy).HasMaxLength(256);
        });

        // ServerHealthMetricRecord configuration
        modelBuilder.Entity<ServerHealthMetricRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.MetricType);
            
            entity.Property(e => e.MetricType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(256);
        });

        // DatabaseBackupRecord configuration
        modelBuilder.Entity<DatabaseBackupRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.FileName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Checksum).HasMaxLength(128);
            entity.Property(e => e.EncryptionKey).HasMaxLength(256);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.Notes).HasMaxLength(1024);
        });

        // SetupConfigRecord configuration
        modelBuilder.Entity<SetupConfigRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SetupTokenHash);

            entity.Property(e => e.SetupTokenHash).HasMaxLength(128);
            entity.Property(e => e.SetupMethod).HasMaxLength(50);
            entity.Property(e => e.SetupCompletedByIp).HasMaxLength(64);
        });

        // ApiKeyRecord configuration (new for simplified auth)
        modelBuilder.Entity<ApiKeyRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyHash);

            entity.Property(e => e.KeyHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.KeyHint).HasMaxLength(10);
        });

        // SyncedLicenseRecord configuration (for tracking offline licenses)
        modelBuilder.Entity<SyncedLicenseRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseId).IsUnique();
            entity.HasIndex(e => e.ClientCode);
            entity.HasIndex(e => e.IssuedAt);

            entity.Property(e => e.LicenseId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ClientName).HasMaxLength(256);
            entity.Property(e => e.ClientCode).HasMaxLength(10);
            entity.Property(e => e.Edition).HasMaxLength(64);
            entity.Property(e => e.LicenseJson).HasColumnType("TEXT");
            entity.Property(e => e.RevokedReason).HasMaxLength(512);
        });

        // WebhookDeliveryRecord configuration (for webhook tracking)
        modelBuilder.Entity<WebhookDeliveryRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WebhookId);
            entity.HasIndex(e => e.AttemptedAt);
            entity.HasIndex(e => new { e.Success, e.AttemptedAt });

            entity.Property(e => e.WebhookId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EndpointName).HasMaxLength(128);
            entity.Property(e => e.EndpointUrl).HasMaxLength(512).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("TEXT");
            entity.Property(e => e.ResponseBody).HasColumnType("TEXT");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1024);
        });

        // Seed default data
        SeedDefaultData(modelBuilder);
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed default tiers
        modelBuilder.Entity<LicenseTierRecord>().HasData(
            new LicenseTierRecord { Id = 1, TierId = "Basic", DisplayName = "Basic", Description = "Essential survey processing tools", DisplayOrder = 1, IsActive = true },
            new LicenseTierRecord { Id = 2, TierId = "Professional", DisplayName = "Professional", Description = "Full suite of professional tools", DisplayOrder = 2, IsActive = true },
            new LicenseTierRecord { Id = 3, TierId = "Enterprise", DisplayName = "Enterprise", Description = "All modules plus priority support", DisplayOrder = 3, IsActive = true }
        );

        // Seed all 13 FathomOS modules with CertificateCode (3-letter codes)
        modelBuilder.Entity<ModuleRecord>().HasData(
            // Core Modules
            new ModuleRecord { Id = 1, ModuleId = "SurveyListing", DisplayName = "Survey Listing Generator", Description = "Process and generate survey listings from NPD files", Category = "Core", DefaultTier = "Basic", DisplayOrder = 1, Icon = "📋", CertificateCode = "SLG", IsActive = true },
            new ModuleRecord { Id = 2, ModuleId = "SurveyLogbook", DisplayName = "Survey Logbook", Description = "Real-time survey event logging with NaviPac integration", Category = "Core", DefaultTier = "Professional", DisplayOrder = 2, Icon = "📓", CertificateCode = "SLB", IsActive = true },
            // Utilities
            new ModuleRecord { Id = 3, ModuleId = "NetworkTimeSync", DisplayName = "Network Time Synchronization", Description = "Synchronize time across survey network equipment", Category = "Utilities", DefaultTier = "Professional", DisplayOrder = 3, Icon = "⏱️", CertificateCode = "NTS", IsActive = true },
            new ModuleRecord { Id = 4, ModuleId = "EquipmentInventory", DisplayName = "Equipment Inventory Management", Description = "Track survey equipment with barcode scanning and maintenance", Category = "Utilities", DefaultTier = "Professional", DisplayOrder = 4, Icon = "📦", CertificateCode = "EQI", IsActive = true },
            // Analysis
            new ModuleRecord { Id = 5, ModuleId = "SoundVelocity", DisplayName = "Sound Velocity Profiler", Description = "Process CTD cast data and calculate sound velocity", Category = "Analysis", DefaultTier = "Professional", DisplayOrder = 5, Icon = "🔊", CertificateCode = "SVP", IsActive = true },
            // Calibrations
            new ModuleRecord { Id = 6, ModuleId = "GnssCalibration", DisplayName = "GNSS Calibration", Description = "Compare GNSS positioning systems with statistical analysis", Category = "Calibrations", DefaultTier = "Enterprise", DisplayOrder = 6, Icon = "📡", CertificateCode = "GNS", IsActive = true },
            new ModuleRecord { Id = 7, ModuleId = "MruCalibration", DisplayName = "MRU Calibration", Description = "Calibrate vessel motion reference units", Category = "Calibrations", DefaultTier = "Enterprise", DisplayOrder = 7, Icon = "🧭", CertificateCode = "MRU", IsActive = true },
            new ModuleRecord { Id = 8, ModuleId = "UsblVerification", DisplayName = "USBL Verification", Description = "Verify underwater acoustic positioning systems", Category = "Calibrations", DefaultTier = "Enterprise", DisplayOrder = 8, Icon = "📍", CertificateCode = "USB", IsActive = true },
            new ModuleRecord { Id = 9, ModuleId = "TreeInclination", DisplayName = "Tree Inclination", Description = "Calibrate inclinometers using reference measurements", Category = "Calibrations", DefaultTier = "Enterprise", DisplayOrder = 9, Icon = "🌲", CertificateCode = "TRI", IsActive = true },
            new ModuleRecord { Id = 10, ModuleId = "RovGyroCalibration", DisplayName = "ROV Gyro Calibration", Description = "Calibrate ROV gyroscopic sensors", Category = "Calibrations", DefaultTier = "Enterprise", DisplayOrder = 10, Icon = "🤖", CertificateCode = "RGC", IsActive = true },
            new ModuleRecord { Id = 11, ModuleId = "VesselGyroCalibration", DisplayName = "Vessel Gyro Calibration", Description = "Calibrate vessel gyroscopic compass systems", Category = "Calibrations", DefaultTier = "Enterprise", DisplayOrder = 11, Icon = "🚢", CertificateCode = "VGC", IsActive = true },
            // Management
            new ModuleRecord { Id = 12, ModuleId = "PersonnelManagement", DisplayName = "Personnel Management", Description = "Manage survey personnel, certifications, and assignments", Category = "Management", DefaultTier = "Professional", DisplayOrder = 12, Icon = "👥", CertificateCode = "PRM", IsActive = true },
            new ModuleRecord { Id = 13, ModuleId = "ProjectManagement", DisplayName = "Project Management", Description = "Manage survey projects, timelines, and deliverables", Category = "Management", DefaultTier = "Professional", DisplayOrder = 13, Icon = "📁", CertificateCode = "PJM", IsActive = true }
        );

        // Seed tier-module relationships
        // Basic tier: SurveyListing only
        modelBuilder.Entity<TierModuleRecord>().HasData(
            new TierModuleRecord { TierId = "Basic", ModuleId = "SurveyListing" }
        );

        // Professional tier: Core, Utilities, Analysis, and Management modules
        modelBuilder.Entity<TierModuleRecord>().HasData(
            new TierModuleRecord { TierId = "Professional", ModuleId = "SurveyListing" },
            new TierModuleRecord { TierId = "Professional", ModuleId = "SurveyLogbook" },
            new TierModuleRecord { TierId = "Professional", ModuleId = "NetworkTimeSync" },
            new TierModuleRecord { TierId = "Professional", ModuleId = "EquipmentInventory" },
            new TierModuleRecord { TierId = "Professional", ModuleId = "SoundVelocity" },
            new TierModuleRecord { TierId = "Professional", ModuleId = "PersonnelManagement" },
            new TierModuleRecord { TierId = "Professional", ModuleId = "ProjectManagement" }
        );

        // Enterprise tier: All 13 modules (Professional + Calibrations)
        modelBuilder.Entity<TierModuleRecord>().HasData(
            // Core
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "SurveyListing" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "SurveyLogbook" },
            // Utilities
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "NetworkTimeSync" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "EquipmentInventory" },
            // Analysis
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "SoundVelocity" },
            // Calibrations (Enterprise only)
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "GnssCalibration" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "MruCalibration" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "UsblVerification" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "TreeInclination" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "RovGyroCalibration" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "VesselGyroCalibration" },
            // Management
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "PersonnelManagement" },
            new TierModuleRecord { TierId = "Enterprise", ModuleId = "ProjectManagement" }
        );
    }
}

/// <summary>
/// Represents a purchased license key
/// </summary>
public class LicenseKeyRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// The license key the customer enters (e.g., "XXXX-XXXX-XXXX-XXXX")
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Internal license ID used for tracking
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;
    
    public string ProductName { get; set; } = LicenseConstants.ProductName;
    public string Edition { get; set; } = "Professional";
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    
    /// <summary>
    /// Comma-separated list of features
    /// </summary>
    public string Features { get; set; } = "Tier:Professional";
    
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    
    /// <summary>
    /// True if this license was generated offline and synced later
    /// </summary>
    public bool IsOfflineGenerated { get; set; }
    
    // === WHITE-LABEL BRANDING FIELDS (New for Fathom OS) ===
    
    /// <summary>
    /// Company/brand name for white-labeling.
    /// Example: "Subsea7 Survey Division"
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Unique 2-letter licensee code (A-Z).
    /// Used in Certificate IDs: FOS-{LicenseeCode}-2412-00001-X3B7
    /// </summary>
    public string? LicenseeCode { get; set; }
    
    /// <summary>
    /// Support verification code in format SUP-XX-XXXXX.
    /// Customer provides this when contacting support.
    /// </summary>
    public string? SupportCode { get; set; }
    
    /// <summary>
    /// Base64-encoded company logo PNG (≤20KB).
    /// Stored directly in database for offline use.
    /// </summary>
    public string? BrandLogo { get; set; }
    
    /// <summary>
    /// Optional HTTPS URL for high-resolution logo.
    /// </summary>
    public string? BrandLogoUrl { get; set; }
    
    // === OFFLINE LICENSE FIELDS ===
    
    /// <summary>
    /// License type: "Online" or "Offline"
    /// </summary>
    public string LicenseType { get; set; } = "Online";
    
    /// <summary>
    /// Hardware ID for offline licenses (customer's machine identifier)
    /// </summary>
    public string? HardwareId { get; set; }
    
    // === DATABASE IMPROVEMENTS v3.3 ===
    
    /// <summary>
    /// Admin notes about this license
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Customer's purchase order number for tracking
    /// </summary>
    public string? PurchaseOrderNumber { get; set; }
    
    /// <summary>
    /// Purchase price (for reporting)
    /// </summary>
    public decimal? PurchasePrice { get; set; }
    
    /// <summary>
    /// Currency code for purchase price (USD, EUR, GBP, etc.)
    /// </summary>
    public string? Currency { get; set; }
    
    /// <summary>
    /// Sales representative who sold this license
    /// </summary>
    public string? SalesRep { get; set; }
    
    /// <summary>
    /// How the customer found us (referral source)
    /// </summary>
    public string? ReferralSource { get; set; }
    
    /// <summary>
    /// Number of times this license has been renewed
    /// </summary>
    public int RenewalCount { get; set; }
    
    /// <summary>
    /// Date of last renewal
    /// </summary>
    public DateTime? LastRenewalDate { get; set; }
    
    /// <summary>
    /// QR code verification token for offline verification
    /// </summary>
    public string? QrVerificationToken { get; set; }
    
    // Navigation property
    public virtual ICollection<LicenseActivationRecord> Activations { get; set; } = new List<LicenseActivationRecord>();
}

/// <summary>
/// Represents a license activation on a specific device
/// </summary>
public class LicenseActivationRecord
{
    public int Id { get; set; }
    public int LicenseKeyId { get; set; }
    
    /// <summary>
    /// Hardware fingerprint of the activated device
    /// </summary>
    public string HardwareFingerprint { get; set; } = string.Empty;
    
    /// <summary>
    /// Machine name for easier identification
    /// </summary>
    public string? MachineName { get; set; }
    
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public bool IsDeactivated { get; set; }
    
    /// <summary>
    /// True if this was an offline activation synced later
    /// </summary>
    public bool IsOfflineActivation { get; set; }
    
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
    public string? IpAddress { get; set; }
    
    // Navigation property
    public virtual LicenseKeyRecord LicenseKey { get; set; } = null!;
}

/// <summary>
/// Record of revoked licenses
/// </summary>
public class RevocationRecord
{
    public int Id { get; set; }
    public string LicenseId { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public string? RevokedBy { get; set; }
}

/// <summary>
/// Represents a software module that can be licensed
/// </summary>
public class ModuleRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique module identifier (PascalCase, e.g., "SurveyListing")
    /// Must match the ModuleId in Fathom OS's ModuleInfo.json
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for UI (e.g., "Survey Listing")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Module description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Category for grouping (Core, Tools, Analysis, Export, Utilities)
    /// </summary>
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Default tier that includes this module
    /// </summary>
    public string DefaultTier { get; set; } = "Professional";
    
    /// <summary>
    /// Display order in lists
    /// </summary>
    public int DisplayOrder { get; set; } = 100;
    
    /// <summary>
    /// Icon emoji or icon name
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// 3-letter certificate code for this module (e.g., "SLG" for Survey Listing Generator)
    /// Used in certificate IDs: OCS-SLG-20260118-0001
    /// </summary>
    public string? CertificateCode { get; set; }
    
    /// <summary>
    /// Current version of the module
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Whether this module is active and available for licensing
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a license tier (bundle of modules)
/// </summary>
public class LicenseTierRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique tier identifier (e.g., "Basic", "Professional", "Enterprise")
    /// </summary>
    public string TierId { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Tier description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Display order (lower = shown first)
    /// </summary>
    public int DisplayOrder { get; set; } = 100;
    
    /// <summary>
    /// Whether this tier is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Many-to-many relationship between tiers and modules
/// </summary>
public class TierModuleRecord
{
    /// <summary>
    /// Tier ID (e.g., "Professional")
    /// </summary>
    public string TierId { get; set; } = string.Empty;
    
    /// <summary>
    /// Module ID (e.g., "SurveyListing")
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;
}

// ============================================================================
// CERTIFICATE RECORDS (New for Fathom OS)
// ============================================================================

/// <summary>
/// Represents a processing certificate issued by Fathom OS modules.
/// Certificates are synced from client to server for verification.
/// </summary>
public class CertificateRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique certificate ID in format: FOS-{LicenseeCode}-{YYMM}-{Sequence}-{Check}
    /// Example: "FOS-S7-2412-00001-X3B7"
    /// </summary>
    public string CertificateId { get; set; } = string.Empty;
    
    /// <summary>
    /// License ID that created this certificate
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;
    
    /// <summary>
    /// 2-letter licensee code from license
    /// </summary>
    public string LicenseeCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Module that issued the certificate (e.g., "SurveyListing")
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;
    
    /// <summary>
    /// 3-letter module certificate code (e.g., "SLG" for Survey Listing Generator)
    /// </summary>
    public string ModuleCertificateCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Version of the module that created the certificate
    /// </summary>
    public string? ModuleVersion { get; set; }
    
    /// <summary>
    /// When the certificate was issued
    /// </summary>
    public DateTime IssuedAt { get; set; }
    
    /// <summary>
    /// When the certificate was synced to server
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Project name for the processing work
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
    
    /// <summary>
    /// Project location/area
    /// </summary>
    public string? ProjectLocation { get; set; }
    
    /// <summary>
    /// Vessel name (if applicable)
    /// </summary>
    public string? Vessel { get; set; }
    
    /// <summary>
    /// Client name
    /// </summary>
    public string? Client { get; set; }
    
    /// <summary>
    /// Name of person who signed/approved the certificate
    /// </summary>
    public string SignatoryName { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of signatory
    /// </summary>
    public string? SignatoryTitle { get; set; }
    
    /// <summary>
    /// Company name (from license Brand)
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Module-specific processing data as JSON.
    /// Each module defines their own content.
    /// Stored as flexible JSON, NOT fixed structure.
    /// Example: {"Total Survey Points": "15,234", "KP Range": "0.000 km — 45.678 km"}
    /// </summary>
    public string? ProcessingDataJson { get; set; }
    
    /// <summary>
    /// List of input files as JSON array
    /// </summary>
    public string? InputFilesJson { get; set; }
    
    /// <summary>
    /// List of output files as JSON array
    /// </summary>
    public string? OutputFilesJson { get; set; }
    
    /// <summary>
    /// ECDSA signature of the certificate data
    /// </summary>
    public string? Signature { get; set; }
    
    /// <summary>
    /// Algorithm used for signature (e.g., "SHA256withECDSA")
    /// </summary>
    public string SignatureAlgorithm { get; set; } = "SHA256withECDSA";
    
    /// <summary>
    /// Whether the signature has been verified by the server
    /// </summary>
    public bool IsSignatureVerified { get; set; }
}

/// <summary>
/// Tracks certificate sequence numbers per licensee per month.
/// Used to generate unique certificate IDs.
/// </summary>
public class CertificateSequenceRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// 2-letter licensee code
    /// </summary>
    public string LicenseeCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Year-month in YYMM format (e.g., "2412" for December 2024)
    /// </summary>
    public string YearMonth { get; set; } = string.Empty;
    
    /// <summary>
    /// Last used sequence number for this licensee/month
    /// </summary>
    public int LastSequence { get; set; }
    
    /// <summary>
    /// When this record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ============================================================================
// NEW RECORDS FOR v3.3 - License Transfer Portal, Audit, Security, etc.
// ============================================================================

/// <summary>
/// Tracks license transfers between devices
/// </summary>
public class LicenseTransferRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// License ID being transferred
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique token for this transfer request
    /// </summary>
    public string? TransferToken { get; set; }
    
    /// <summary>
    /// Hardware fingerprint of the old device
    /// </summary>
    public string? OldHardwareFingerprint { get; set; }
    
    /// <summary>
    /// Hardware fingerprint of the new device
    /// </summary>
    public string? NewHardwareFingerprint { get; set; }
    
    /// <summary>
    /// Machine name of the old device
    /// </summary>
    public string? OldMachineName { get; set; }
    
    /// <summary>
    /// Machine name of the new device
    /// </summary>
    public string? NewMachineName { get; set; }
    
    /// <summary>
    /// Customer email for verification
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Reason for transfer (optional)
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// When the transfer was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the transfer was completed (null if pending/cancelled)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Status: Pending, Verified, Completed, Cancelled, Expired
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// Whether email verification was completed
    /// </summary>
    public bool IsEmailVerified { get; set; }
}

/// <summary>
/// Email verification codes for transfers and other operations
/// </summary>
public class TransferVerificationRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// 6-digit verification code
    /// </summary>
    public string VerificationCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Email address this code was sent to
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// License ID this verification is for
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;
    
    /// <summary>
    /// Purpose: Transfer, Deactivation, PasswordReset, etc.
    /// </summary>
    public string Purpose { get; set; } = "Transfer";
    
    /// <summary>
    /// When the code was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the code expires (usually 15 minutes)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether the code has been used
    /// </summary>
    public bool IsUsed { get; set; }
    
    /// <summary>
    /// Number of failed attempts
    /// </summary>
    public int FailedAttempts { get; set; }
}

/// <summary>
/// Comprehensive audit log for all system actions
/// </summary>
public class AuditLogRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// When the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Action performed: CREATE, UPDATE, DELETE, ACTIVATE, DEACTIVATE, TRANSFER, LOGIN, etc.
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of entity affected: License, Activation, Module, Tier, Admin, etc.
    /// </summary>
    public string? EntityType { get; set; }
    
    /// <summary>
    /// ID of the entity affected
    /// </summary>
    public string? EntityId { get; set; }
    
    /// <summary>
    /// User ID who performed the action (admin or customer)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Email of the user who performed the action
    /// </summary>
    public string? UserEmail { get; set; }
    
    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Previous values as JSON (for updates)
    /// </summary>
    public string? OldValues { get; set; }
    
    /// <summary>
    /// New values as JSON (for creates/updates)
    /// </summary>
    public string? NewValues { get; set; }
    
    /// <summary>
    /// Additional details or notes
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Whether the action succeeded
    /// </summary>
    public bool Success { get; set; } = true;
}

/// <summary>
/// Admin user accounts with 2FA support
/// </summary>
public class AdminUserRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Email address (unique)
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Username for login
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Hashed password (PBKDF2 or similar)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Salt for password hashing
    /// </summary>
    public string? PasswordSalt { get; set; }
    
    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Role: SuperAdmin, Admin, ReadOnly
    /// </summary>
    public string Role { get; set; } = "Admin";
    
    /// <summary>
    /// Whether 2FA is enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; }
    
    /// <summary>
    /// TOTP secret key (encrypted)
    /// </summary>
    public string? TwoFactorSecret { get; set; }
    
    /// <summary>
    /// Backup codes (JSON array, encrypted)
    /// </summary>
    public string? BackupCodes { get; set; }
    
    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last login date
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Whether the account is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Failed login attempts (reset on success)
    /// </summary>
    public int FailedLoginAttempts { get; set; }
    
    /// <summary>
    /// Lockout end time (null if not locked)
    /// </summary>
    public DateTime? LockoutEnd { get; set; }
}

/// <summary>
/// Active admin sessions
/// </summary>
public class AdminSessionRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Admin user ID
    /// </summary>
    public int AdminUserId { get; set; }
    
    /// <summary>
    /// Session token (secure random)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Session creation time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Session expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Last activity time
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this session is still valid
    /// </summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>
    /// Whether 2FA was completed for this session
    /// </summary>
    public bool TwoFactorVerified { get; set; }
}

/// <summary>
/// Active license sessions (heartbeat tracking for concurrent use prevention)
/// </summary>
public class ActiveSessionRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// License ID
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique session token for this active session
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Hardware fingerprint
    /// </summary>
    public string? HardwareFingerprint { get; set; }
    
    /// <summary>
    /// Machine name
    /// </summary>
    public string? MachineName { get; set; }
    
    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// App version
    /// </summary>
    public string? AppVersion { get; set; }
    
    /// <summary>
    /// Session start time
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last heartbeat time
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this session is still active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Session ended time (null if still active)
    /// </summary>
    public DateTime? EndedAt { get; set; }
    
    /// <summary>
    /// Reason for session end: Logout, Timeout, Terminated, NewSession
    /// </summary>
    public string? EndReason { get; set; }
}

/// <summary>
/// Rate limiting tracking
/// </summary>
public class RateLimitRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Rate limit key (IP address, license key, etc.)
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Endpoint being rate limited
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// Start of the rate limit window
    /// </summary>
    public DateTime WindowStart { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Number of requests in this window
    /// </summary>
    public int RequestCount { get; set; }
    
    /// <summary>
    /// Whether this key is currently blocked
    /// </summary>
    public bool IsBlocked { get; set; }
    
    /// <summary>
    /// Block expires at (null if not blocked)
    /// </summary>
    public DateTime? BlockExpiresAt { get; set; }
}

/// <summary>
/// Blocked IP addresses
/// </summary>
public class BlockedIpRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// IP address (can be CIDR range)
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for block
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// When the IP was blocked
    /// </summary>
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the block expires (null for permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Who blocked this IP
    /// </summary>
    public string? BlockedBy { get; set; }
    
    /// <summary>
    /// Whether the block is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Server health metrics
/// </summary>
public class ServerHealthMetricRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// When the metric was recorded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Type of metric: ResponseTime, Uptime, Error, ActivationFailure, etc.
    /// </summary>
    public string MetricType { get; set; } = string.Empty;
    
    /// <summary>
    /// Endpoint for response time metrics
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// Metric value (milliseconds for response time, count for errors, etc.)
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// Whether this metric indicates a problem
    /// </summary>
    public bool IsAlert { get; set; }
    
    /// <summary>
    /// Additional details
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Database backup records
/// </summary>
public class DatabaseBackupRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Backup file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full path to backup file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
    
    /// <summary>
    /// SHA256 checksum of the backup
    /// </summary>
    public string? Checksum { get; set; }
    
    /// <summary>
    /// Encryption key hint (not the actual key!)
    /// </summary>
    public string? EncryptionKey { get; set; }
    
    /// <summary>
    /// Whether the backup is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }
    
    /// <summary>
    /// When the backup was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who created the backup
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Backup type: Manual, Scheduled, PreUpdate
    /// </summary>
    public string BackupType { get; set; } = "Manual";
    
    /// <summary>
    /// Number of license records in this backup
    /// </summary>
    public int LicenseCount { get; set; }
    
    /// <summary>
    /// Number of activation records in this backup
    /// </summary>
    public int ActivationCount { get; set; }
    
    /// <summary>
    /// Notes about the backup
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Whether this backup has been verified
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// When the backup was last verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
}

// ============================================================================
// SETUP CONFIGURATION RECORD (First-time Admin Setup)
// ============================================================================

/// <summary>
/// Tracks first-time admin setup configuration and tokens
/// </summary>
public class SetupConfigRecord
{
    public int Id { get; set; }

    /// <summary>
    /// SHA-256 hash of the setup token (32 bytes random token, hashed for security)
    /// </summary>
    public string? SetupTokenHash { get; set; }

    /// <summary>
    /// When the setup token was generated
    /// </summary>
    public DateTime? SetupTokenGeneratedAt { get; set; }

    /// <summary>
    /// When the setup token expires (24 hours after generation)
    /// </summary>
    public DateTime? SetupTokenExpiresAt { get; set; }

    /// <summary>
    /// Whether initial setup has been completed
    /// </summary>
    public bool IsSetupCompleted { get; set; }

    /// <summary>
    /// When setup was completed
    /// </summary>
    public DateTime? SetupCompletedAt { get; set; }

    /// <summary>
    /// IP address that completed the setup
    /// </summary>
    public string? SetupCompletedByIp { get; set; }

    /// <summary>
    /// How setup was completed: Token, Environment, Console
    /// </summary>
    public string? SetupMethod { get; set; }
}

// ============================================================================
// API KEY AUTHENTICATION (New for simplified server)
// ============================================================================

/// <summary>
/// Stores API key hashes for server authentication.
/// Replaces the complex admin username/password system.
/// </summary>
public class ApiKeyRecord
{
    public int Id { get; set; }

    /// <summary>
    /// SHA-256 hash of the API key (never store plain key)
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Last 4 characters of the key for identification
    /// </summary>
    public string? KeyHint { get; set; }

    /// <summary>
    /// When the API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the API key was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether this API key is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

// ============================================================================
// SYNCED LICENSE RECORDS (For tracking offline licenses)
// ============================================================================

/// <summary>
/// Stores license records synced from the License Generator UI.
/// Used for tracking and analytics - license validation is performed offline.
/// </summary>
public class SyncedLicenseRecord
{
    public int Id { get; set; }

    /// <summary>
    /// Unique license identifier
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>
    /// Client/customer name
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Client/licensee code (2-letter code)
    /// </summary>
    public string? ClientCode { get; set; }

    /// <summary>
    /// License edition (Basic, Professional, Enterprise)
    /// </summary>
    public string? Edition { get; set; }

    /// <summary>
    /// Full license JSON data (for reference)
    /// </summary>
    public string? LicenseJson { get; set; }

    /// <summary>
    /// When the license was issued
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// When the license expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the license was synced to this server
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this license has been revoked
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Reason for revocation (if revoked)
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// When the license was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Customer email (for lookup)
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Features included in the license
    /// </summary>
    public string? Features { get; set; }

    /// <summary>
    /// Brand/company name for white-labeling
    /// </summary>
    public string? Brand { get; set; }
}
