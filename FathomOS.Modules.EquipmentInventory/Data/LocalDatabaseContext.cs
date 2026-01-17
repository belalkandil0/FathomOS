using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Data;

public class LocalDatabaseContext : DbContext
{
    private readonly string _dbPath;
    
    public LocalDatabaseContext()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "EquipmentInventory");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "local.db");
    }
    
    public LocalDatabaseContext(string dbPath)
    {
        _dbPath = dbPath;
    }
    
    public LocalDatabaseContext(DbContextOptions<LocalDatabaseContext> options) : base(options)
    {
        _dbPath = string.Empty;
    }
    
    // Core entities
    public DbSet<Equipment> Equipment { get; set; } = null!;
    public DbSet<EquipmentCategory> Categories { get; set; } = null!;
    public DbSet<EquipmentType> EquipmentTypes { get; set; } = null!;
    public DbSet<EquipmentPhoto> EquipmentPhotos { get; set; } = null!;
    public DbSet<EquipmentDocument> EquipmentDocuments { get; set; } = null!;
    public DbSet<EquipmentHistory> EquipmentHistory { get; set; } = null!;
    public DbSet<EquipmentEvent> EquipmentEvents { get; set; } = null!;
    
    // Manifests
    public DbSet<Manifest> Manifests { get; set; } = null!;
    public DbSet<ManifestItem> ManifestItems { get; set; } = null!;
    public DbSet<ManifestPhoto> ManifestPhotos { get; set; } = null!;
    public DbSet<UnregisteredItem> UnregisteredItems { get; set; } = null!;
    public DbSet<ManifestNotification> ManifestNotifications { get; set; } = null!;
    
    // Locations
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<LocationTypeRecord> LocationTypes { get; set; } = null!;
    public DbSet<Models.Region> Regions { get; set; } = null!;
    public DbSet<Vessel> Vessels { get; set; } = null!;
    
    // Users & Security
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserLocation> UserLocations { get; set; } = null!;
    
    // Other
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<Alert> Alerts { get; set; } = null!;
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; } = null!;
    public DbSet<Certification> Certifications { get; set; } = null!;
    public DbSet<EquipmentCheckout> EquipmentCheckouts { get; set; } = null!;
    
    // Defect Reports / EFN
    public DbSet<DefectReport> DefectReports { get; set; } = null!;
    public DbSet<DefectReportPart> DefectReportParts { get; set; } = null!;
    public DbSet<DefectReportAttachment> DefectReportAttachments { get; set; } = null!;
    public DbSet<DefectReportHistory> DefectReportHistory { get; set; } = null!;
    
    // Sync
    public DbSet<OfflineQueueItem> OfflineQueue { get; set; } = null!;
    public DbSet<SyncConflict> SyncConflicts { get; set; } = null!;
    
    // Settings
    public DbSet<SyncSettings> SyncSettings { get; set; } = null!;
    
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
        
        // Composite keys for junction tables
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
        modelBuilder.Entity<UserLocation>().HasKey(ul => new { ul.UserId, ul.LocationId });
        
        // Equipment indexes
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasIndex(e => e.AssetNumber).IsUnique();
            entity.HasIndex(e => e.QrCode);
            entity.HasIndex(e => e.SerialNumber);
            entity.HasIndex(e => e.CurrentLocationId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SyncVersion);
            entity.HasIndex(e => new { e.IsActive, e.Status });
            
            // Relationships
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Type).WithMany().HasForeignKey(e => e.TypeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CurrentLocation).WithMany(l => l.Equipment).HasForeignKey(e => e.CurrentLocationId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CurrentProject).WithMany().HasForeignKey(e => e.CurrentProjectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.SetNull);
        });
        
        // Manifest indexes
        modelBuilder.Entity<Manifest>(entity =>
        {
            entity.HasIndex(m => m.ManifestNumber).IsUnique();
            entity.HasIndex(m => m.QrCode);
            entity.HasIndex(m => m.Status);
            entity.HasIndex(m => m.Type);
            entity.HasIndex(m => m.CreatedDate);
            entity.HasIndex(m => new { m.IsActive, m.Status });
        });
        
        modelBuilder.Entity<ManifestItem>(entity =>
        {
            entity.HasIndex(mi => mi.ManifestId);
            entity.HasIndex(mi => mi.EquipmentId);
        });
        
        // Location indexes
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(l => l.Code).IsUnique();
            entity.HasIndex(l => l.ParentLocationId);
            entity.HasOne(l => l.ParentLocation).WithMany(l => l.ChildLocations).HasForeignKey(l => l.ParentLocationId).OnDelete(DeleteBehavior.Restrict);
        });
        
        // User indexes
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });
        
        // Category self-reference
        modelBuilder.Entity<EquipmentCategory>(entity =>
        {
            entity.HasOne(c => c.ParentCategory).WithMany(c => c.ChildCategories).HasForeignKey(c => c.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
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
        
        // DefectReport indexes and relationships
        modelBuilder.Entity<DefectReport>(entity =>
        {
            entity.HasIndex(d => d.ReportNumber).IsUnique();
            entity.HasIndex(d => d.QrCode);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.ReportDate);
            entity.HasIndex(d => d.CreatedByUserId);
            entity.HasIndex(d => d.EquipmentId);
            entity.HasIndex(d => d.LocationId);
            entity.HasIndex(d => d.FaultCategory);
            entity.HasIndex(d => new { d.Status, d.ReplacementUrgency });
            
            entity.HasOne(d => d.Equipment).WithMany().HasForeignKey(d => d.EquipmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(d => d.Location).WithMany().HasForeignKey(d => d.LocationId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(d => d.EquipmentCategory).WithMany().HasForeignKey(d => d.EquipmentCategoryId).OnDelete(DeleteBehavior.SetNull);
        });
        
        modelBuilder.Entity<DefectReportPart>(entity =>
        {
            entity.HasIndex(p => p.DefectReportId);
            entity.HasIndex(p => p.SapNumber);
        });
        
        modelBuilder.Entity<DefectReportAttachment>(entity =>
        {
            entity.HasIndex(a => a.DefectReportId);
        });
        
        modelBuilder.Entity<DefectReportHistory>(entity =>
        {
            entity.HasIndex(h => h.DefectReportId);
            entity.HasIndex(h => h.PerformedAt);
        });

        // Equipment Checkout indexes and relationships
        modelBuilder.Entity<EquipmentCheckout>(entity =>
        {
            entity.HasKey(c => c.CheckoutId);
            entity.HasIndex(c => c.EquipmentId);
            entity.HasIndex(c => c.ProjectId);
            entity.HasIndex(c => c.CheckedOutToUserId);
            entity.HasIndex(c => c.CheckedOutAt);
            entity.HasIndex(c => c.ReturnedAt);
            entity.HasIndex(c => new { c.EquipmentId, c.ReturnedAt }); // For finding active checkouts

            entity.HasOne(c => c.Equipment).WithMany().HasForeignKey(c => c.EquipmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Project).WithMany().HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(c => c.CheckedOutToUser).WithMany().HasForeignKey(c => c.CheckedOutToUserId).OnDelete(DeleteBehavior.SetNull);
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
