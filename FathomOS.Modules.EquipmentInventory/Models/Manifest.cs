using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Models;

public class Manifest
{
    [Key]
    public Guid ManifestId { get; set; } = Guid.NewGuid();
    [Required][MaxLength(50)]
    public string ManifestNumber { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? QrCode { get; set; }
    [Required]
    public ManifestType Type { get; set; }
    
    public Guid? FromLocationId { get; set; }
    [ForeignKey(nameof(FromLocationId))]
    [JsonIgnore]
    public virtual Location? FromLocation { get; set; }
    [MaxLength(100)]
    public string? FromContactName { get; set; }
    [MaxLength(50)]
    public string? FromContactPhone { get; set; }
    [MaxLength(200)]
    public string? FromContactEmail { get; set; }
    
    public Guid? ToLocationId { get; set; }
    [ForeignKey(nameof(ToLocationId))]
    [JsonIgnore]
    public virtual Location? ToLocation { get; set; }
    [MaxLength(100)]
    public string? ToContactName { get; set; }
    [MaxLength(50)]
    public string? ToContactPhone { get; set; }
    [MaxLength(200)]
    public string? ToContactEmail { get; set; }
    
    public Guid? ProjectId { get; set; }
    [ForeignKey(nameof(ProjectId))]
    [JsonIgnore]
    public virtual Project? Project { get; set; }
    
    public ManifestStatus Status { get; set; } = ManifestStatus.Draft;
    
    // === VERIFICATION FIELDS ===
    
    /// <summary>
    /// Link to related manifest (Outward links to Inward, Inward links to Outward)
    /// </summary>
    public Guid? LinkedManifestId { get; set; }
    
    /// <summary>
    /// Overall verification status for this shipment
    /// </summary>
    public ShipmentVerificationStatus VerificationStatus { get; set; } = ShipmentVerificationStatus.NotStarted;
    
    /// <summary>
    /// When verification was started
    /// </summary>
    public DateTime? VerificationStartedAt { get; set; }
    
    /// <summary>
    /// When verification was completed
    /// </summary>
    public DateTime? VerificationCompletedAt { get; set; }
    
    /// <summary>
    /// User who performed/is performing verification
    /// </summary>
    public Guid? VerifiedBy { get; set; }
    
    /// <summary>
    /// Number of items verified so far
    /// </summary>
    public int VerifiedItemCount { get; set; }
    
    /// <summary>
    /// Number of items with discrepancies
    /// </summary>
    public int DiscrepancyCount { get; set; }
    
    /// <summary>
    /// Summary of verification (auto-generated)
    /// </summary>
    [MaxLength(500)]
    public string? VerificationSummary { get; set; }
    
    // === DATE FIELDS ===
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get => ExpectedArrivalDate; set => ExpectedArrivalDate = value; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    
    [MaxLength(50)]
    public string? ShippingMethod { get; set; }
    [MaxLength(200)]
    public string? CarrierName { get; set; }
    [MaxLength(100)]
    public string? TrackingNumber { get; set; }
    
    public int TotalItems { get; set; }
    public decimal? TotalWeight { get; set; }
    
    public Guid? CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public Guid? ReceivedBy { get; set; }
    
    public string? SenderSignature { get; set; }
    public DateTime? SenderSignedAt { get; set; }
    public string? ReceiverSignature { get; set; }
    public DateTime? ReceiverSignedAt { get; set; }
    
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public bool HasDiscrepancies { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long SyncVersion { get; set; }
    public bool IsModifiedLocally { get; set; }
    
    [JsonIgnore]
    public virtual ICollection<ManifestItem> Items { get; set; } = new List<ManifestItem>();
    [JsonIgnore]
    public virtual ICollection<ManifestPhoto> Photos { get; set; } = new List<ManifestPhoto>();
    
    [NotMapped]
    public string StatusDisplay => Status.ToString();
    
    [NotMapped]
    public string VerificationProgress => TotalItems > 0 
        ? $"{VerifiedItemCount}/{TotalItems}" 
        : "0/0";
    
    [NotMapped]
    public double VerificationPercentage => TotalItems > 0 
        ? (double)VerifiedItemCount / TotalItems * 100 
        : 0;
}

public class ManifestItem
{
    [Key]
    public Guid ItemId { get; set; } = Guid.NewGuid();
    public Guid ManifestId { get; set; }
    public Guid? EquipmentId { get; set; }  // Nullable for unregistered items
    
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
    
    [MaxLength(50)]
    public string? AssetNumber { get; set; }
    [MaxLength(100)]
    public string? UniqueId { get; set; }  // Label ID (e.g., S7WSS04068)
    [MaxLength(200)]
    public string? Name { get; set; }
    [MaxLength(500)]
    public string? Description { get; set; }
    [MaxLength(100)]
    public string? SerialNumber { get; set; }
    
    public decimal Quantity { get; set; } = 1;
    public decimal? Weight { get; set; }
    
    [MaxLength(20)]
    public string? ConditionAtSend { get; set; }
    [MaxLength(20)]
    public string? ConditionAtReceive { get; set; }
    public string? ConditionNotes { get; set; }
    
    // === VERIFICATION FIELDS ===
    
    /// <summary>
    /// Current verification status of this item
    /// </summary>
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    
    /// <summary>
    /// When this item was scanned/verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
    
    /// <summary>
    /// User who verified this item
    /// </summary>
    public Guid? VerifiedBy { get; set; }
    
    /// <summary>
    /// Damage description if item is damaged
    /// </summary>
    [MaxLength(1000)]
    public string? DamageNotes { get; set; }
    
    /// <summary>
    /// Photo IDs of damage photos
    /// </summary>
    public string? DamagePhotoIds { get; set; }
    
    /// <summary>
    /// Whether this item was added during verification (not on original manifest)
    /// </summary>
    public bool IsExtraItem { get; set; }
    
    /// <summary>
    /// Whether this item was manually added (no QR scan)
    /// </summary>
    public bool IsManuallyAdded { get; set; }
    
    /// <summary>
    /// For unregistered items - links to UnregisteredItem record
    /// </summary>
    public Guid? UnregisteredItemId { get; set; }
    
    // === LEGACY FIELDS ===
    
    public bool IsReceived { get; set; }
    public decimal? ReceivedQuantity { get; set; }
    public bool HasDiscrepancy { get; set; }
    public DiscrepancyType? DiscrepancyType { get; set; }
    public string? DiscrepancyNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long SyncVersion { get; set; }
    
    // === COMPUTED PROPERTIES ===
    
    [NotMapped]
    public bool IsVerified => VerificationStatus == VerificationStatus.Verified 
                            || VerificationStatus == VerificationStatus.Damaged;
    
    [NotMapped]
    public string VerificationStatusDisplay => VerificationStatus switch
    {
        VerificationStatus.Pending => "⏳ Pending",
        VerificationStatus.Verified => "✓ Verified",
        VerificationStatus.Damaged => "⚠️ Damaged",
        VerificationStatus.Missing => "❌ Missing",
        VerificationStatus.Extra => "➕ Extra",
        VerificationStatus.Wrong => "✗ Wrong Item",
        _ => "Unknown"
    };
}

public class ManifestPhoto
{
    [Key]
    public Guid PhotoId { get; set; } = Guid.NewGuid();
    public Guid ManifestId { get; set; }
    public Guid? ItemId { get; set; }
    [MaxLength(500)]
    public string PhotoUrl { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? PhotoType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Items added during verification that are not in the system.
/// Pending review by inventory management.
/// </summary>
public class UnregisteredItem
{
    [Key]
    public Guid UnregisteredItemId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Manifest where this item was added
    /// </summary>
    public Guid ManifestId { get; set; }
    
    /// <summary>
    /// ManifestItem ID that references this
    /// </summary>
    public Guid? ManifestItemId { get; set; }
    
    // === ITEM DETAILS ===
    
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? SerialNumber { get; set; }
    
    [MaxLength(200)]
    public string? Manufacturer { get; set; }
    
    [MaxLength(100)]
    public string? Model { get; set; }
    
    [MaxLength(100)]
    public string? PartNumber { get; set; }
    
    public decimal Quantity { get; set; } = 1;
    
    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; }
    
    /// <summary>
    /// Category suggestion from user
    /// </summary>
    public Guid? SuggestedCategoryId { get; set; }
    
    /// <summary>
    /// Type suggestion from user
    /// </summary>
    public Guid? SuggestedTypeId { get; set; }
    
    /// <summary>
    /// Whether this appears to be a consumable (vs. tracked equipment)
    /// </summary>
    public bool IsConsumable { get; set; }
    
    /// <summary>
    /// Current location where item was received
    /// </summary>
    public Guid? CurrentLocationId { get; set; }
    
    // === REVIEW STATUS ===
    
    public UnregisteredItemStatus Status { get; set; } = UnregisteredItemStatus.PendingReview;
    
    /// <summary>
    /// Equipment ID if converted to equipment
    /// </summary>
    public Guid? ConvertedEquipmentId { get; set; }
    
    /// <summary>
    /// User who reviewed this item
    /// </summary>
    public Guid? ReviewedBy { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    
    [MaxLength(500)]
    public string? ReviewNotes { get; set; }
    
    // === PHOTOS ===
    
    /// <summary>
    /// Comma-separated photo URLs
    /// </summary>
    public string? PhotoUrls { get; set; }
    
    // === AUDIT ===
    
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long SyncVersion { get; set; }
    public bool IsModifiedLocally { get; set; }
    
    // === NAVIGATION ===
    
    [ForeignKey(nameof(ManifestId))]
    [JsonIgnore]
    public virtual Manifest? Manifest { get; set; }
    
    [ForeignKey(nameof(CurrentLocationId))]
    [JsonIgnore]
    public virtual Location? CurrentLocation { get; set; }
    
    // === COMPUTED PROPERTIES (for UI binding) ===
    
    /// <summary>
    /// Whether this item is pending review
    /// </summary>
    [NotMapped]
    public bool IsPendingReview => Status == UnregisteredItemStatus.PendingReview;
    
    /// <summary>
    /// Friendly display name for the status
    /// </summary>
    [NotMapped]
    public string StatusDisplayName => Status switch
    {
        UnregisteredItemStatus.PendingReview => "Pending Review",
        UnregisteredItemStatus.ConvertedToEquipment => "Converted",
        UnregisteredItemStatus.KeptAsConsumable => "Consumable",
        UnregisteredItemStatus.Rejected => "Rejected",
        _ => Status.ToString()
    };
    
    /// <summary>
    /// Source manifest number for display
    /// </summary>
    [NotMapped]
    public string? SourceManifestNumber => Manifest?.ManifestNumber;
    
    /// <summary>
    /// Alias for CurrentLocationId for easier binding
    /// </summary>
    [NotMapped]
    public Guid? DestinationLocationId => CurrentLocationId;
    
    /// <summary>
    /// Alias for CreatedAt for easier binding
    /// </summary>
    [NotMapped]
    public DateTime CreatedDate => CreatedAt;
    
    /// <summary>
    /// Alias for ReviewedAt for easier binding
    /// </summary>
    [NotMapped]
    public DateTime? ReviewedDate
    {
        get => ReviewedAt;
        set => ReviewedAt = value;
    }
}

/// <summary>
/// Notification for missing items and other issues
/// </summary>
public class ManifestNotification
{
    [Key]
    public Guid NotificationId { get; set; } = Guid.NewGuid();
    
    public Guid ManifestId { get; set; }
    public Guid? ManifestItemId { get; set; }
    public Guid? EquipmentId { get; set; }
    
    [Required][MaxLength(50)]
    public string NotificationType { get; set; } = string.Empty; // "Missing", "Damaged", "Discrepancy"
    
    [Required][MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Details { get; set; }
    
    /// <summary>
    /// Location where the issue was reported
    /// </summary>
    public Guid? LocationId { get; set; }
    
    /// <summary>
    /// User who should be notified
    /// </summary>
    public Guid? NotifyUserId { get; set; }
    
    /// <summary>
    /// Whether the notification requires action
    /// </summary>
    public bool RequiresAction { get; set; } = true;
    
    /// <summary>
    /// Whether the action has been taken
    /// </summary>
    public bool IsResolved { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    [MaxLength(500)]
    public string? ResolutionNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    
    [ForeignKey(nameof(ManifestId))]
    [JsonIgnore]
    public virtual Manifest? Manifest { get; set; }
    
    [ForeignKey(nameof(LocationId))]
    [JsonIgnore]
    public virtual Location? Location { get; set; }
    
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
}

// Note: ManifestType, ManifestStatus, DiscrepancyType enums are defined in Enums.cs
