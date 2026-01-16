namespace S7Fathom.Core.Enums;

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
    OnHire
}

public enum EquipmentCondition
{
    New,
    Good,
    Fair,
    Poor,
    Damaged
}

public enum OwnershipType
{
    Owned,
    Rented,
    Client,
    Loaned
}

public enum ManifestType
{
    Inward,
    Outward
}

public enum ManifestStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    InTransit,
    PartiallyReceived,
    Received,
    Completed,
    Cancelled
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}

public enum ShippingMethod
{
    Road,
    Sea,
    Air,
    Helicopter,
    Internal
}

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

public enum DocumentType
{
    Certificate,
    Manual,
    Datasheet,
    Inspection,
    Other
}

public enum DiscrepancyType
{
    Missing,
    Damaged,
    Wrong,
    Excess
}

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

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum SyncOperation
{
    Insert,
    Update,
    Delete
}

public enum ConflictResolution
{
    Pending,
    UseLocal,
    UseServer,
    Merged
}

public enum AccessLevel
{
    Read,
    Write,
    Admin
}

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
