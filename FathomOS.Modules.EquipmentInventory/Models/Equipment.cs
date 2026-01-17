using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Models;

/// <summary>
/// Represents a piece of equipment or inventory item.
/// </summary>
public class Equipment
{
    #region Identification
    
    [Key]
    public Guid EquipmentId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// System-generated asset number (e.g., EQ-2024-00001)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AssetNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique human-readable ID for labels and search (e.g., S7WSS04068)
    /// Format: {OrgCode}{CategoryCode}{SequenceNumber}
    /// </summary>
    [MaxLength(20)]
    public string? UniqueId { get; set; }
    
    /// <summary>
    /// SAP system reference number
    /// </summary>
    [MaxLength(50)]
    public string? SapNumber { get; set; }
    
    /// <summary>
    /// Technical reference number
    /// </summary>
    [MaxLength(50)]
    public string? TechNumber { get; set; }
    
    /// <summary>
    /// Manufacturer serial number
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// Generated QR code value (e.g., foseq:EQ-2024-00001|S7WSS04068)
    /// </summary>
    [MaxLength(100)]
    public string? QrCode { get; set; }
    
    /// <summary>
    /// External barcode if exists
    /// </summary>
    [MaxLength(100)]
    public string? Barcode { get; set; }
    
    #endregion
    
    #region Classification
    
    public Guid? TypeId { get; set; }
    
    [ForeignKey(nameof(TypeId))]
    [JsonIgnore]
    public virtual EquipmentType? Type { get; set; }
    
    public Guid? CategoryId { get; set; }
    
    [ForeignKey(nameof(CategoryId))]
    [JsonIgnore]
    public virtual EquipmentCategory? Category { get; set; }
    
    #endregion
    
    #region Description
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [MaxLength(200)]
    public string? Manufacturer { get; set; }
    
    [MaxLength(200)]
    public string? Model { get; set; }
    
    [MaxLength(100)]
    public string? PartNumber { get; set; }
    
    /// <summary>
    /// Flexible specifications stored as JSON
    /// </summary>
    public string? SpecificationsJson { get; set; }
    
    #endregion
    
    #region Physical Properties
    
    public decimal? WeightKg { get; set; }
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    
    [NotMapped]
    public decimal? VolumeCm3 => LengthCm * WidthCm * HeightCm;
    
    #endregion
    
    #region Packaging
    
    [MaxLength(50)]
    public string? PackagingType { get; set; }
    
    public decimal? PackagingWeightKg { get; set; }
    public decimal? PackagingLengthCm { get; set; }
    public decimal? PackagingWidthCm { get; set; }
    public decimal? PackagingHeightCm { get; set; }
    
    public string? PackagingDescription { get; set; }
    
    [NotMapped]
    public decimal? TotalWeightKg => (WeightKg ?? 0) + (PackagingWeightKg ?? 0);
    
    #endregion
    
    #region Location & Status
    
    public Guid? CurrentLocationId { get; set; }

    [ForeignKey(nameof(CurrentLocationId))]
    [JsonIgnore]
    public virtual Location? CurrentLocation { get; set; }
    
    public Guid? CurrentProjectId { get; set; }
    
    [ForeignKey(nameof(CurrentProjectId))]
    [JsonIgnore]
    public virtual Project? CurrentProject { get; set; }
    
    public Guid? CurrentCustodianId { get; set; }
    
    [ForeignKey(nameof(CurrentCustodianId))]
    [JsonIgnore]
    public virtual User? CurrentCustodian { get; set; }
    
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;
    
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Good;
    
    #endregion
    
    #region Ownership & Procurement
    
    public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned;
    
    public Guid? SupplierId { get; set; }
    
    [ForeignKey(nameof(SupplierId))]
    [JsonIgnore]
    public virtual Supplier? Supplier { get; set; }
    
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    
    [MaxLength(3)]
    public string? PurchaseCurrency { get; set; } = "USD";
    
    [MaxLength(50)]
    public string? PurchaseOrderNumber { get; set; }
    
    public DateTime? WarrantyExpiryDate { get; set; }
    
    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }
    public decimal? RentalRate { get; set; }
    
    [MaxLength(20)]
    public string? RentalRatePeriod { get; set; }
    
    #endregion
    
    #region Depreciation
    
    [MaxLength(20)]
    public string? DepreciationMethod { get; set; }
    
    public int? UsefulLifeYears { get; set; }
    public decimal? ResidualValue { get; set; }
    public decimal? CurrentValue { get; set; }
    
    #endregion
    
    #region Certification & Calibration
    
    public bool RequiresCertification { get; set; }
    
    [MaxLength(100)]
    public string? CertificationNumber { get; set; }
    
    [MaxLength(200)]
    public string? CertificationBody { get; set; }
    
    public DateTime? CertificationDate { get; set; }
    public DateTime? CertificationExpiryDate { get; set; }
    
    public bool RequiresCalibration { get; set; }
    public DateTime? LastCalibrationDate { get; set; }
    public DateTime? NextCalibrationDate { get; set; }
    public int? CalibrationIntervalDays { get; set; }
    
    // Alias for compatibility
    [NotMapped]
    public int? CalibrationInterval { get => CalibrationIntervalDays; set => CalibrationIntervalDays = value; }
    
    #endregion
    
    #region Service & Maintenance
    
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public int? ServiceIntervalDays { get; set; }
    public DateTime? LastInspectionDate { get; set; }
    
    // Alias for maintenance view compatibility
    [NotMapped]
    public DateTime? NextMaintenanceDate { get => NextServiceDate; set => NextServiceDate = value; }
    
    #endregion
    
    #region Consumables
    
    public bool IsConsumable { get; set; }
    public decimal QuantityOnHand { get; set; } = 1;
    
    [MaxLength(20)]
    public string UnitOfMeasure { get; set; } = "Each";
    
    public decimal? MinimumStockLevel { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal? MaximumStockLevel { get; set; }
    
    // Aliases for compatibility
    [NotMapped]
    public decimal? MinimumQuantity { get => MinimumStockLevel; set => MinimumStockLevel = value; }
    [NotMapped]
    public decimal? ReorderPoint { get => ReorderLevel; set => ReorderLevel = value; }
    
    [MaxLength(50)]
    public string? BatchNumber { get; set; }
    
    [MaxLength(50)]
    public string? LotNumber { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    #endregion
    
    #region Assignment
    
    public bool IsPermanentEquipment { get; set; }
    public bool IsProjectEquipment { get; set; }
    
    #endregion
    
    #region Photos & Documents
    
    [MaxLength(500)]
    public string? PrimaryPhotoUrl { get; set; }
    
    [MaxLength(500)]
    public string? QrCodeImageUrl { get; set; }
    
    #endregion
    
    #region Notes
    
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    
    #endregion
    
    #region Audit
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long SyncVersion { get; set; }
    public bool IsModifiedLocally { get; set; }
    
    #endregion
    
    #region Navigation Properties
    
    [JsonIgnore]
    public virtual ICollection<EquipmentPhoto> Photos { get; set; } = new List<EquipmentPhoto>();
    [JsonIgnore]
    public virtual ICollection<EquipmentDocument> Documents { get; set; } = new List<EquipmentDocument>();
    [JsonIgnore]
    public virtual ICollection<EquipmentHistory> History { get; set; } = new List<EquipmentHistory>();
    
    #endregion
    
    #region Computed Properties
    
    [NotMapped]
    public bool IsCertificationExpiring => 
        RequiresCertification && 
        CertificationExpiryDate.HasValue && 
        CertificationExpiryDate.Value <= DateTime.Today.AddDays(30);
    
    [NotMapped]
    public bool IsCertificationExpired => 
        RequiresCertification && 
        CertificationExpiryDate.HasValue && 
        CertificationExpiryDate.Value < DateTime.Today;
    
    [NotMapped]
    public bool IsCalibrationDue => 
        RequiresCalibration && 
        NextCalibrationDate.HasValue && 
        NextCalibrationDate.Value <= DateTime.Today.AddDays(7);
    
    [NotMapped]
    public bool IsCalibrationOverdue => 
        RequiresCalibration && 
        NextCalibrationDate.HasValue && 
        NextCalibrationDate.Value < DateTime.Today;
    
    [NotMapped]
    public bool IsLowStock => 
        IsConsumable && 
        MinimumStockLevel.HasValue && 
        QuantityOnHand <= MinimumStockLevel.Value;
    
    [NotMapped]
    public int? DaysUntilCertificationExpiry => 
        CertificationExpiryDate.HasValue 
            ? (int)(CertificationExpiryDate.Value - DateTime.Today).TotalDays 
            : null;
    
    [NotMapped]
    public int? DaysUntilCalibrationDue => 
        NextCalibrationDate.HasValue 
            ? (int)(NextCalibrationDate.Value - DateTime.Today).TotalDays 
            : null;
    
    [NotMapped]
    public string StatusDisplay => Status.ToString();
    
    [NotMapped]
    public string ConditionDisplay => Condition.ToString();
    
    [NotMapped]
    public string DimensionsDisplay => 
        LengthCm.HasValue && WidthCm.HasValue && HeightCm.HasValue
            ? $"{LengthCm:F1} × {WidthCm:F1} × {HeightCm:F1} cm"
            : "-";
    
    #endregion
}

// Note: Enums (EquipmentStatus, EquipmentCondition, OwnershipType) are defined in Enums.cs
