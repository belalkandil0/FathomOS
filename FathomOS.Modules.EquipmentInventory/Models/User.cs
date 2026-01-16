using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Models;

public class User
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();
    [Required][MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    [Required][MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? PasswordHash { get; set; }
    [MaxLength(100)]
    public string? Salt { get; set; }
    [MaxLength(100)]
    public string? FirstName { get; set; }
    [MaxLength(100)]
    public string? LastName { get; set; }
    [MaxLength(50)]
    public string? Phone { get; set; }
    [MaxLength(100)]
    public string? PinHash { get; set; }
    [MaxLength(500)]
    public string? ProfilePhotoUrl { get; set; }
    
    // Professional Information
    [MaxLength(50)]
    public string? EmployeeId { get; set; }
    [MaxLength(100)]
    public string? JobTitle { get; set; }
    [MaxLength(100)]
    public string? Department { get; set; }
    [MaxLength(100)]
    public string? Division { get; set; }
    [MaxLength(100)]
    public string? CostCenter { get; set; }
    public Guid? ManagerId { get; set; }
    [ForeignKey(nameof(ManagerId))]
    [JsonIgnore]
    public virtual User? Manager { get; set; }
    [MaxLength(20)]
    public string? Extension { get; set; }
    [MaxLength(50)]
    public string? MobilePhone { get; set; }
    [MaxLength(200)]
    public string? OfficeLocation { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    
    // Certifications & Training
    [MaxLength(500)]
    public string? Certifications { get; set; }
    public DateTime? SafetyTrainingExpiry { get; set; }
    public DateTime? OffshoreCertExpiry { get; set; }
    
    // Signature for approvals
    [MaxLength(500)]
    public string? SignatureImageUrl { get; set; }
    
    // Default location assignment
    public Guid? DefaultLocationId { get; set; }
    [ForeignKey(nameof(DefaultLocationId))]
    [JsonIgnore]
    public virtual Location? DefaultLocation { get; set; }
    
    // Active Directory integration
    public bool IsAdUser { get; set; }
    [MaxLength(100)]
    public string? AdObjectId { get; set; }
    [MaxLength(200)]
    public string? AdDistinguishedName { get; set; }
    
    // Account status
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// If true, user must change password on next login
    /// </summary>
    public bool MustChangePassword { get; set; } = true;
    
    /// <summary>
    /// Date when password was last changed
    /// </summary>
    public DateTime? PasswordChangedAt { get; set; }
    
    /// <summary>
    /// Password expiry in days (0 = never expires)
    /// </summary>
    public int PasswordExpiryDays { get; set; } = 90;
    
    /// <summary>
    /// Temporary password for initial setup (cleared after first change)
    /// </summary>
    [MaxLength(100)]
    public string? TemporaryPassword { get; set; }
    
    /// <summary>
    /// Is this the super administrator account
    /// </summary>
    public bool IsSuperAdmin { get; set; }
    
    /// <summary>
    /// Can user approve manifests
    /// </summary>
    public bool CanApproveManifests { get; set; }
    
    /// <summary>
    /// Can user approve defect reports
    /// </summary>
    public bool CanApproveDefects { get; set; }
    
    /// <summary>
    /// User's equipment checkout limit
    /// </summary>
    public int? EquipmentCheckoutLimit { get; set; }
    
    [JsonIgnore]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    [JsonIgnore]
    public virtual ICollection<UserLocation> UserLocations { get; set; } = new List<UserLocation>();
    
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Username : FullName;
    [NotMapped]
    public string DisplayWithTitle => string.IsNullOrWhiteSpace(JobTitle) ? DisplayName : $"{DisplayName} ({JobTitle})";
    [NotMapped]
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    [NotMapped]
    public bool IsPasswordExpired => PasswordExpiryDays > 0 && 
        PasswordChangedAt.HasValue && 
        DateTime.UtcNow > PasswordChangedAt.Value.AddDays(PasswordExpiryDays);
    [NotMapped]
    public bool HasValidSafetyTraining => !SafetyTrainingExpiry.HasValue || SafetyTrainingExpiry.Value > DateTime.UtcNow;
    [NotMapped]
    public bool HasValidOffshoreCert => !OffshoreCertExpiry.HasValue || OffshoreCertExpiry.Value > DateTime.UtcNow;
    
    /// <summary>
    /// Gets the user's primary role (first role from UserRoles collection)
    /// </summary>
    [NotMapped]
    public Role? Role => UserRoles?.FirstOrDefault()?.Role;
}

public class Role
{
    [Key]
    public Guid RoleId { get; set; } = Guid.NewGuid();
    [Required][MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonIgnore]
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    [Key]
    public Guid PermissionId { get; set; } = Guid.NewGuid();
    [Required][MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? Category { get; set; }
    public string? Description { get; set; }
}

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    public virtual User? User { get; set; }
    [ForeignKey(nameof(RoleId))]
    [JsonIgnore]
    public virtual Role? Role { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    [ForeignKey(nameof(RoleId))]
    [JsonIgnore]
    public virtual Role? Role { get; set; }
    [ForeignKey(nameof(PermissionId))]
    [JsonIgnore]
    public virtual Permission? Permission { get; set; }
}

public class UserLocation
{
    public Guid UserId { get; set; }
    public Guid LocationId { get; set; }
    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    public virtual User? User { get; set; }
    [ForeignKey(nameof(LocationId))]
    [JsonIgnore]
    public virtual Location? Location { get; set; }
    [MaxLength(20)]
    public string AccessLevel { get; set; } = "Read";
}
