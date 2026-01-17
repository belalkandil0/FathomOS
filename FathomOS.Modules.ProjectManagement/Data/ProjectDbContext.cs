using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.ProjectManagement.Models;

namespace FathomOS.Modules.ProjectManagement.Data;

/// <summary>
/// Entity Framework Core database context for the Project Management module.
/// </summary>
public class ProjectDbContext : DbContext
{
    private readonly string _dbPath;

    public ProjectDbContext()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "ProjectManagement");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "projects.db");
    }

    public ProjectDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
    {
        _dbPath = string.Empty;
    }

    #region DbSets - Core Entities

    /// <summary>
    /// Client companies
    /// </summary>
    public DbSet<Client> Clients { get; set; } = null!;

    /// <summary>
    /// Client contacts
    /// </summary>
    public DbSet<ClientContact> ClientContacts { get; set; } = null!;

    /// <summary>
    /// Survey projects
    /// </summary>
    public DbSet<SurveyProject> Projects { get; set; } = null!;

    #endregion

    #region DbSets - Assignments

    /// <summary>
    /// Vessel assignments to projects
    /// </summary>
    public DbSet<ProjectVesselAssignment> VesselAssignments { get; set; } = null!;

    /// <summary>
    /// Equipment assignments to projects
    /// </summary>
    public DbSet<ProjectEquipmentAssignment> EquipmentAssignments { get; set; } = null!;

    /// <summary>
    /// Personnel assignments to projects
    /// </summary>
    public DbSet<ProjectPersonnelAssignment> PersonnelAssignments { get; set; } = null!;

    #endregion

    #region DbSets - Milestones & Deliverables

    /// <summary>
    /// Project milestones
    /// </summary>
    public DbSet<ProjectMilestone> Milestones { get; set; } = null!;

    /// <summary>
    /// Project deliverables
    /// </summary>
    public DbSet<ProjectDeliverable> Deliverables { get; set; } = null!;

    #endregion

    #region DbSets - Sync

    /// <summary>
    /// Offline queue items pending sync
    /// </summary>
    public DbSet<OfflineQueueItem> OfflineQueue { get; set; } = null!;

    /// <summary>
    /// Sync conflicts requiring resolution
    /// </summary>
    public DbSet<SyncConflict> SyncConflicts { get; set; } = null!;

    /// <summary>
    /// Sync settings and state
    /// </summary>
    public DbSet<SyncSettings> SyncSettings { get; set; } = null!;

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

        // ===== CLIENT =====
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(c => c.ClientId);

            // Indexes
            entity.HasIndex(c => c.ClientCode).IsUnique();
            entity.HasIndex(c => c.SapCustomerNumber);
            entity.HasIndex(c => c.CompanyName);
            entity.HasIndex(c => c.ClientType);
            entity.HasIndex(c => c.IsActive);
            entity.HasIndex(c => c.SyncVersion);
            entity.HasIndex(c => new { c.IsActive, c.ClientType });
        });

        // ===== CLIENT CONTACT =====
        modelBuilder.Entity<ClientContact>(entity =>
        {
            entity.HasKey(cc => cc.ContactId);

            // Indexes
            entity.HasIndex(cc => cc.ClientId);
            entity.HasIndex(cc => cc.Email);
            entity.HasIndex(cc => cc.ContactType);
            entity.HasIndex(cc => cc.IsPrimaryContact);
            entity.HasIndex(cc => cc.IsActive);
            entity.HasIndex(cc => cc.SyncVersion);

            // Relationships
            entity.HasOne(cc => cc.Client)
                .WithMany(c => c.Contacts)
                .HasForeignKey(cc => cc.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== SURVEY PROJECT =====
        modelBuilder.Entity<SurveyProject>(entity =>
        {
            entity.HasKey(p => p.ProjectId);

            // Indexes
            entity.HasIndex(p => p.ProjectNumber).IsUnique();
            entity.HasIndex(p => p.ProjectCode);
            entity.HasIndex(p => p.SapProjectNumber);
            entity.HasIndex(p => p.ClientId);
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.Phase);
            entity.HasIndex(p => p.ProjectType);
            entity.HasIndex(p => p.PlannedStartDate);
            entity.HasIndex(p => p.PlannedEndDate);
            entity.HasIndex(p => p.ProjectManagerId);
            entity.HasIndex(p => p.IsActive);
            entity.HasIndex(p => p.SyncVersion);
            entity.HasIndex(p => new { p.IsActive, p.Status });
            entity.HasIndex(p => new { p.ClientId, p.Status });
            entity.HasIndex(p => new { p.Status, p.Phase });

            // Relationships
            entity.HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.PrimaryContact)
                .WithMany()
                .HasForeignKey(p => p.PrimaryContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== PROJECT VESSEL ASSIGNMENT =====
        modelBuilder.Entity<ProjectVesselAssignment>(entity =>
        {
            entity.HasKey(va => va.AssignmentId);

            // Indexes
            entity.HasIndex(va => va.ProjectId);
            entity.HasIndex(va => va.VesselId);
            entity.HasIndex(va => va.Role);
            entity.HasIndex(va => va.Status);
            entity.HasIndex(va => va.PlannedStartDate);
            entity.HasIndex(va => va.IsActive);
            entity.HasIndex(va => va.SyncVersion);
            entity.HasIndex(va => new { va.ProjectId, va.VesselId });
            entity.HasIndex(va => new { va.ProjectId, va.Role });

            // Relationships
            entity.HasOne(va => va.Project)
                .WithMany(p => p.VesselAssignments)
                .HasForeignKey(va => va.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== PROJECT EQUIPMENT ASSIGNMENT =====
        modelBuilder.Entity<ProjectEquipmentAssignment>(entity =>
        {
            entity.HasKey(ea => ea.AssignmentId);

            // Indexes
            entity.HasIndex(ea => ea.ProjectId);
            entity.HasIndex(ea => ea.EquipmentId);
            entity.HasIndex(ea => ea.VesselAssignmentId);
            entity.HasIndex(ea => ea.Role);
            entity.HasIndex(ea => ea.Status);
            entity.HasIndex(ea => ea.PlannedStartDate);
            entity.HasIndex(ea => ea.IsActive);
            entity.HasIndex(ea => ea.SyncVersion);
            entity.HasIndex(ea => new { ea.ProjectId, ea.EquipmentId });
            entity.HasIndex(ea => new { ea.ProjectId, ea.Role });

            // Relationships
            entity.HasOne(ea => ea.Project)
                .WithMany(p => p.EquipmentAssignments)
                .HasForeignKey(ea => ea.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ea => ea.VesselAssignment)
                .WithMany()
                .HasForeignKey(ea => ea.VesselAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== PROJECT PERSONNEL ASSIGNMENT =====
        modelBuilder.Entity<ProjectPersonnelAssignment>(entity =>
        {
            entity.HasKey(pa => pa.AssignmentId);

            // Indexes
            entity.HasIndex(pa => pa.ProjectId);
            entity.HasIndex(pa => pa.UserId);
            entity.HasIndex(pa => pa.VesselAssignmentId);
            entity.HasIndex(pa => pa.Role);
            entity.HasIndex(pa => pa.Status);
            entity.HasIndex(pa => pa.PlannedStartDate);
            entity.HasIndex(pa => pa.IsActive);
            entity.HasIndex(pa => pa.SyncVersion);
            entity.HasIndex(pa => new { pa.ProjectId, pa.UserId });
            entity.HasIndex(pa => new { pa.ProjectId, pa.Role });
            entity.HasIndex(pa => pa.Email);

            // Relationships
            entity.HasOne(pa => pa.Project)
                .WithMany(p => p.PersonnelAssignments)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pa => pa.VesselAssignment)
                .WithMany()
                .HasForeignKey(pa => pa.VesselAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== PROJECT MILESTONE =====
        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(m => m.MilestoneId);

            // Indexes
            entity.HasIndex(m => m.ProjectId);
            entity.HasIndex(m => m.ParentMilestoneId);
            entity.HasIndex(m => m.Type);
            entity.HasIndex(m => m.Status);
            entity.HasIndex(m => m.PlannedDate);
            entity.HasIndex(m => m.IsPaymentMilestone);
            entity.HasIndex(m => m.OwnerId);
            entity.HasIndex(m => m.IsActive);
            entity.HasIndex(m => m.SyncVersion);
            entity.HasIndex(m => new { m.ProjectId, m.Status });
            entity.HasIndex(m => new { m.ProjectId, m.Type });
            entity.HasIndex(m => new { m.ProjectId, m.SortOrder });

            // Relationships
            entity.HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.ParentMilestone)
                .WithMany(m => m.ChildMilestones)
                .HasForeignKey(m => m.ParentMilestoneId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== PROJECT DELIVERABLE =====
        modelBuilder.Entity<ProjectDeliverable>(entity =>
        {
            entity.HasKey(d => d.DeliverableId);

            // Indexes
            entity.HasIndex(d => d.ProjectId);
            entity.HasIndex(d => d.MilestoneId);
            entity.HasIndex(d => d.ParentDeliverableId);
            entity.HasIndex(d => d.DeliverableNumber);
            entity.HasIndex(d => d.Type);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.PlannedDueDate);
            entity.HasIndex(d => d.OwnerId);
            entity.HasIndex(d => d.IsActive);
            entity.HasIndex(d => d.SyncVersion);
            entity.HasIndex(d => new { d.ProjectId, d.Status });
            entity.HasIndex(d => new { d.ProjectId, d.Type });
            entity.HasIndex(d => new { d.ProjectId, d.SortOrder });
            entity.HasIndex(d => new { d.MilestoneId, d.Status });

            // Relationships
            entity.HasOne(d => d.Project)
                .WithMany(p => p.Deliverables)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Milestone)
                .WithMany(m => m.Deliverables)
                .HasForeignKey(d => d.MilestoneId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.ParentDeliverable)
                .WithMany(d => d.ChildDeliverables)
                .HasForeignKey(d => d.ParentDeliverableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== SYNC ENTITIES =====
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
