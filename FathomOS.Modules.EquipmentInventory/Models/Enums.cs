namespace FathomOS.Modules.EquipmentInventory.Models;

/// <summary>
/// Equipment status values - MUST match API enums
/// </summary>
public enum EquipmentStatus
{
    Available,
    InUse,
    InTransit,
    UnderRepair,
    InCalibration,
    Reserved,
    Condemned,
    Lost,
    Retired,
    OnHire,
    Disposed,
    InStorage,
    CheckedOut,
    InService,
    InStock
}

/// <summary>
/// Equipment condition values - MUST match API enums
/// </summary>
public enum EquipmentCondition
{
    New,
    Good,
    Fair,
    Poor,
    Damaged
}

/// <summary>
/// Ownership type values - MUST match API enums
/// </summary>
public enum OwnershipType
{
    Owned,
    Rented,
    Client,
    Loaned
}

/// <summary>
/// Manifest type values - MUST match API enums
/// </summary>
public enum ManifestType
{
    Inward,
    Outward
}

/// <summary>
/// Manifest status values - MUST match API enums
/// </summary>
public enum ManifestStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    InTransit,
    Shipped,
    PartiallyReceived,
    Received,
    Completed,
    Cancelled
}

/// <summary>
/// Project status values - MUST match API enums
/// </summary>
public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}

/// <summary>
/// Shipping method values - MUST match API enums
/// </summary>
public enum ShippingMethod
{
    Road,
    Sea,
    Air,
    Helicopter,
    Internal
}

/// <summary>
/// Photo type values - MUST match API enums
/// </summary>
public enum PhotoType
{
    Main,
    Condition,
    Damage,
    Label,
    Certificate,
    General,
    Packaging,
    Loading,
    Receipt
}

/// <summary>
/// Document type values - MUST match API enums
/// </summary>
public enum DocumentType
{
    Certificate,
    Manual,
    Datasheet,
    Inspection,
    Other
}

/// <summary>
/// Discrepancy type values - MUST match API enums
/// </summary>
public enum DiscrepancyType
{
    Missing,
    Damaged,
    Wrong,
    Excess
}

/// <summary>
/// Alert type values - MUST match API enums
/// </summary>
public enum AlertType
{
    CertificationExpiring,
    CalibrationDue,
    ServiceDue,
    LowStock,
    ConsumableExpiring,
    ManifestPending,
    TransferOverdue
}

/// <summary>
/// Alert severity values - MUST match API enums
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Sync operation values - MUST match API enums
/// </summary>
public enum SyncOperation
{
    Insert,
    Update,
    Delete
}

/// <summary>
/// Conflict resolution values - MUST match API enums
/// </summary>
public enum ConflictResolution
{
    Pending,
    UseLocal,
    UseServer,
    Merged
}

/// <summary>
/// Access level values - MUST match API enums
/// </summary>
public enum AccessLevel
{
    Read,
    Write,
    Admin
}

/// <summary>
/// Event type values for equipment history - MUST match API enums
/// </summary>
public enum EventType
{
    Created,
    Updated,
    StatusChanged,
    LocationChanged,
    CustodianChanged,
    Transferred,
    Received,
    Inspected,
    Serviced,
    Calibrated,
    Certified,
    Damaged,
    Repaired,
    Condemned,
    Retired,
    QuantityAdjusted
}

/// <summary>
/// Location type values
/// </summary>
public enum LocationType
{
    Base,
    Vessel,
    Warehouse,
    Workshop,
    Office,
    Project,
    Yard,
    Port,
    Region,
    Container,
    ProjectSite
}

/// <summary>
/// Verification status for manifest items during inward processing
/// </summary>
public enum VerificationStatus
{
    /// <summary>Item not yet scanned/verified</summary>
    Pending,
    
    /// <summary>Item scanned and verified as correct</summary>
    Verified,
    
    /// <summary>Item scanned but marked as damaged</summary>
    Damaged,
    
    /// <summary>Item not found/not received</summary>
    Missing,
    
    /// <summary>Item received but not on original manifest (extra item)</summary>
    Extra,
    
    /// <summary>Wrong item received</summary>
    Wrong
}

/// <summary>
/// Overall verification status for a shipment
/// </summary>
public enum ShipmentVerificationStatus
{
    /// <summary>Verification not started</summary>
    NotStarted,
    
    /// <summary>Verification in progress (some items scanned)</summary>
    InProgress,
    
    /// <summary>All items verified successfully</summary>
    Completed,
    
    /// <summary>Completed but with discrepancies (missing/damaged/extra items)</summary>
    CompletedWithDiscrepancies,
    
    /// <summary>Verification cancelled/abandoned</summary>
    Cancelled
}

/// <summary>
/// Status for unregistered items pending review
/// </summary>
public enum UnregisteredItemStatus
{
    /// <summary>Pending review by inventory management</summary>
    PendingReview,
    
    /// <summary>Converted to equipment record</summary>
    ConvertedToEquipment,
    
    /// <summary>Kept as consumable (not tracked)</summary>
    KeptAsConsumable,
    
    /// <summary>Rejected/removed</summary>
    Rejected
}

/// <summary>
/// Sync status for offline support - MUST match API enums
/// </summary>
public enum SyncStatus
{
    /// <summary>Record pending upload to server</summary>
    Pending,
    
    /// <summary>Record synced with server</summary>
    Synced,
    
    /// <summary>Record has local changes not yet uploaded</summary>
    Modified,
    
    /// <summary>Sync conflict detected</summary>
    Conflict,
    
    /// <summary>Sync failed - retry needed</summary>
    Failed
}
