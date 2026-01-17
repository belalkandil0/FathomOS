using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Data;

public class PersonnelDbContext : DbContext
{
    private readonly string _dbPath;

    public PersonnelDbContext()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "PersonnelManagement");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "personnel.db");
    }

    public PersonnelDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    public PersonnelDbContext(DbContextOptions<PersonnelDbContext> options) : base(options)
    {
        _dbPath = string.Empty;
    }

    #region Core Entities

    public DbSet<Personnel> Personnel { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<RotationPattern> RotationPatterns { get; set; } = null!;

    #endregion

    #region Certifications

    public DbSet<CertificationType> CertificationTypes { get; set; } = null!;
    public DbSet<PersonnelCertification> PersonnelCertifications { get; set; } = null!;

    #endregion

    #region Assignments

    public DbSet<VesselAssignment> VesselAssignments { get; set; } = null!;

    #endregion

    #region Timesheets

    public DbSet<Timesheet> Timesheets { get; set; } = null!;
    public DbSet<TimesheetEntry> TimesheetEntries { get; set; } = null!;

    #endregion

    #region Sync

    public DbSet<OfflineQueueItem> OfflineQueue { get; set; } = null!;
    public DbSet<SyncConflict> SyncConflicts { get; set; } = null!;
    public DbSet<SyncSettings> SyncSettings { get; set; } = null!;

    #endregion

    #region Audit

    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    #endregion

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured && !string.IsNullOrEmpty(_dbPath))
        {
            options.UseSqlite($"Data Source={_dbPath}");
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(true);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Personnel indexes and relationships
        modelBuilder.Entity<Personnel>(entity =>
        {
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => e.SapNumber);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.EmploymentStatus);
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.PositionId);
            entity.HasIndex(e => e.IsOffshore);
            entity.HasIndex(e => e.CurrentVesselId);
            entity.HasIndex(e => e.SyncVersion);
            entity.HasIndex(e => new { e.IsActive, e.EmploymentStatus });
            entity.HasIndex(e => new { e.Department, e.PositionId });

            // Relationships
            entity.HasOne(e => e.Position)
                .WithMany(p => p.Personnel)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RotationPattern)
                .WithMany(r => r.Personnel)
                .HasForeignKey(e => e.RotationPatternId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Supervisor)
                .WithMany(p => p.DirectReports)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Position indexes and relationships
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.DefaultRotationPattern)
                .WithMany(r => r.Positions)
                .HasForeignKey(e => e.DefaultRotationPatternId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // RotationPattern indexes
        modelBuilder.Entity<RotationPattern>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // CertificationType indexes
        modelBuilder.Entity<CertificationType>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsMandatory);
            entity.HasIndex(e => e.IsActive);
        });

        // PersonnelCertification indexes and relationships
        modelBuilder.Entity<PersonnelCertification>(entity =>
        {
            entity.HasIndex(e => e.PersonnelId);
            entity.HasIndex(e => e.CertificationTypeId);
            entity.HasIndex(e => e.ExpiryDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.PersonnelId, e.CertificationTypeId });
            entity.HasIndex(e => new { e.Status, e.ExpiryDate });

            entity.HasOne(e => e.Personnel)
                .WithMany(p => p.Certifications)
                .HasForeignKey(e => e.PersonnelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CertificationType)
                .WithMany(ct => ct.PersonnelCertifications)
                .HasForeignKey(e => e.CertificationTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // VesselAssignment indexes and relationships
        modelBuilder.Entity<VesselAssignment>(entity =>
        {
            entity.HasIndex(e => e.PersonnelId);
            entity.HasIndex(e => e.VesselId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledStartDate);
            entity.HasIndex(e => e.ScheduledEndDate);
            entity.HasIndex(e => e.SignOnDateTime);
            entity.HasIndex(e => e.SignOffDateTime);
            entity.HasIndex(e => new { e.PersonnelId, e.Status });
            entity.HasIndex(e => new { e.VesselId, e.Status });
            entity.HasIndex(e => new { e.IsActive, e.Status });

            entity.HasOne(e => e.Personnel)
                .WithMany(p => p.VesselAssignments)
                .HasForeignKey(e => e.PersonnelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Position)
                .WithMany()
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Timesheet indexes and relationships
        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.HasIndex(e => e.TimesheetNumber).IsUnique();
            entity.HasIndex(e => e.PersonnelId);
            entity.HasIndex(e => e.VesselId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PeriodStartDate);
            entity.HasIndex(e => e.PeriodEndDate);
            entity.HasIndex(e => e.ApprovedBy);
            entity.HasIndex(e => e.SyncVersion);
            entity.HasIndex(e => new { e.PersonnelId, e.PeriodStartDate });
            entity.HasIndex(e => new { e.Status, e.PeriodStartDate });
            entity.HasIndex(e => new { e.IsActive, e.Status });

            entity.HasOne(e => e.Personnel)
                .WithMany(p => p.Timesheets)
                .HasForeignKey(e => e.PersonnelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TimesheetEntry indexes and relationships
        modelBuilder.Entity<TimesheetEntry>(entity =>
        {
            entity.HasIndex(e => e.TimesheetId);
            entity.HasIndex(e => e.EntryDate);
            entity.HasIndex(e => e.EntryType);
            entity.HasIndex(e => new { e.TimesheetId, e.EntryDate });

            entity.HasOne(e => e.Timesheet)
                .WithMany(t => t.Entries)
                .HasForeignKey(e => e.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Sync entities
        modelBuilder.Entity<OfflineQueueItem>(entity =>
        {
            entity.HasKey(o => o.QueueId);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => new { o.TableName, o.RecordId });
        });

        modelBuilder.Entity<SyncConflict>(entity =>
        {
            entity.HasKey(s => s.ConflictId);
            entity.HasIndex(s => new { s.TableName, s.RecordId });
        });

        modelBuilder.Entity<SyncSettings>(entity =>
        {
            entity.HasKey(s => s.Id);
        });

        // AuditLog indexes
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.EntityType);
            entity.HasIndex(a => a.EntityId);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => a.ChangedBy);
            entity.HasIndex(a => a.ChangedAt);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => new { a.EntityType, a.ChangedAt });
        });
    }
}

/// <summary>
/// Tracks items waiting to be synced to the server
/// </summary>
public class OfflineQueueItem
{
    [Key]
    public Guid QueueId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string TableName { get; set; } = string.Empty;

    public Guid RecordId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Operation { get; set; } = string.Empty; // Insert, Update, Delete

    public string? DataJson { get; set; }

    public int Priority { get; set; } = 0;

    public int Attempts { get; set; } = 0;

    public DateTime? LastAttempt { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stores sync conflicts requiring user resolution
/// </summary>
public class SyncConflict
{
    [Key]
    public Guid ConflictId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string TableName { get; set; } = string.Empty;

    public Guid RecordId { get; set; }

    public string? LocalData { get; set; }

    public string? ServerData { get; set; }

    [MaxLength(500)]
    public string? ConflictDescription { get; set; }

    [MaxLength(20)]
    public string? Resolution { get; set; } // UseLocal, UseServer, Merged

    public string? ResolvedData { get; set; }

    public Guid? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stores sync settings and state
/// </summary>
public class SyncSettings
{
    [Key]
    public int Id { get; set; } = 1;

    public long LastSyncVersion { get; set; } = 0;

    public DateTime? LastFullSyncAt { get; set; }

    public DateTime? LastDeltaSyncAt { get; set; }

    [MaxLength(100)]
    public string? DeviceId { get; set; }

    public Guid? LastUserId { get; set; }
}
