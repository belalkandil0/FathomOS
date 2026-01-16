using System.ComponentModel.DataAnnotations;
using S7Fathom.Core.Enums;

namespace S7Fathom.Api.DTOs;

// ============ Equipment DTOs ============

public record EquipmentDto
{
    public Guid EquipmentId { get; init; }
    public string AssetNumber { get; init; } = string.Empty;
    public string? SapNumber { get; init; }
    public string? TechNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? QrCode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public CategoryDto? Category { get; init; }
    public EquipmentTypeDto? Type { get; init; }
    public LocationDto? CurrentLocation { get; init; }
    public ProjectDto? CurrentProject { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Condition { get; init; } = string.Empty;
    public string OwnershipType { get; init; } = string.Empty;
    public PhysicalPropertiesDto? Physical { get; init; }
    public PackagingDto? Packaging { get; init; }
    public CertificationDto? Certification { get; init; }
    public CalibrationDto? Calibration { get; init; }
    public List<PhotoDto> Photos { get; init; } = new();
    public string? PrimaryPhotoUrl { get; init; }
    public string? QrCodeImageUrl { get; init; }
    public DateTime? LastUpdated { get; init; }
    public long SyncVersion { get; init; }
}

public record PhysicalPropertiesDto
{
    public decimal? WeightKg { get; init; }
    public decimal? LengthCm { get; init; }
    public decimal? WidthCm { get; init; }
    public decimal? HeightCm { get; init; }
}

public record PackagingDto
{
    public string? Type { get; init; }
    public decimal? WeightKg { get; init; }
    public decimal? LengthCm { get; init; }
    public decimal? WidthCm { get; init; }
    public decimal? HeightCm { get; init; }
    public string? Description { get; init; }
}

public record CertificationDto
{
    public bool Required { get; init; }
    public string? Number { get; init; }
    public string? Body { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public record CalibrationDto
{
    public bool Required { get; init; }
    public DateTime? LastDate { get; init; }
    public DateTime? NextDate { get; init; }
    public int? IntervalDays { get; init; }
}

public record PhotoDto
{
    public Guid PhotoId { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Caption { get; init; }
}

public record CreateEquipmentRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;
    
    public string? Description { get; init; }
    
    public Guid? CategoryId { get; init; }
    
    public Guid? TypeId { get; init; }
    
    [MaxLength(100)]
    public string? SerialNumber { get; init; }
    
    [MaxLength(200)]
    public string? Manufacturer { get; init; }
    
    [MaxLength(200)]
    public string? Model { get; init; }
    
    public Guid? CurrentLocationId { get; init; }
    
    public Guid? CurrentProjectId { get; init; }
    
    public EquipmentStatus Status { get; init; } = EquipmentStatus.Available;
    
    public EquipmentCondition Condition { get; init; } = EquipmentCondition.New;
    
    public OwnershipType OwnershipType { get; init; } = OwnershipType.Owned;
    
    public decimal? WeightKg { get; init; }
    public decimal? LengthCm { get; init; }
    public decimal? WidthCm { get; init; }
    public decimal? HeightCm { get; init; }
    
    public DateTime? PurchaseDate { get; init; }
    public decimal? PurchasePrice { get; init; }
    public string? PurchaseCurrency { get; init; }
    
    public bool RequiresCertification { get; init; }
    public bool RequiresCalibration { get; init; }
    
    public string? Notes { get; init; }
}

public record UpdateEquipmentRequest
{
    [MaxLength(200)]
    public string? Name { get; init; }
    
    public string? Description { get; init; }
    
    public Guid? CategoryId { get; init; }
    public Guid? TypeId { get; init; }
    public Guid? CurrentLocationId { get; init; }
    public Guid? CurrentProjectId { get; init; }
    
    public EquipmentStatus? Status { get; init; }
    public EquipmentCondition? Condition { get; init; }
    
    public decimal? WeightKg { get; init; }
    public decimal? LengthCm { get; init; }
    public decimal? WidthCm { get; init; }
    public decimal? HeightCm { get; init; }
    
    public string? Notes { get; init; }
}

public record EquipmentSearchRequest
{
    public string? Search { get; init; }
    public Guid? LocationId { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? TypeId { get; init; }
    public Guid? ProjectId { get; init; }
    public EquipmentStatus? Status { get; init; }
    public EquipmentCondition? Condition { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ============ Manifest DTOs ============

public record ManifestDto
{
    public Guid ManifestId { get; init; }
    public string ManifestNumber { get; init; } = string.Empty;
    public string? QrCode { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public LocationDto? FromLocation { get; init; }
    public ContactDto? FromContact { get; init; }
    public LocationDto? ToLocation { get; init; }
    public ContactDto? ToContact { get; init; }
    public ProjectDto? Project { get; init; }
    public ManifestDatesDto Dates { get; init; } = new();
    public ShippingInfoDto? Shipping { get; init; }
    public ManifestTotalsDto Totals { get; init; } = new();
    public List<ManifestItemDto> Items { get; init; } = new();
    public ManifestSignaturesDto? Signatures { get; init; }
    public List<PhotoDto> Photos { get; init; } = new();
    public string? Notes { get; init; }
    public bool HasDiscrepancies { get; init; }
    public long SyncVersion { get; init; }
}

public record ContactDto
{
    public string? Name { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
}

public record ManifestDatesDto
{
    public DateTime? Created { get; init; }
    public DateTime? Submitted { get; init; }
    public DateTime? Approved { get; init; }
    public DateTime? Shipped { get; init; }
    public DateTime? ExpectedArrival { get; init; }
    public DateTime? Received { get; init; }
}

public record ShippingInfoDto
{
    public string? Method { get; init; }
    public string? Carrier { get; init; }
    public string? TrackingNumber { get; init; }
}

public record ManifestTotalsDto
{
    public int Items { get; init; }
    public decimal? WeightKg { get; init; }
    public decimal? VolumeCm3 { get; init; }
}

public record ManifestSignaturesDto
{
    public SignatureDto? Sender { get; init; }
    public SignatureDto? Receiver { get; init; }
    public SignatureDto? Approver { get; init; }
}

public record SignatureDto
{
    public string? Signature { get; init; }
    public DateTime? SignedAt { get; init; }
    public string? SignedBy { get; init; }
}

public record ManifestItemDto
{
    public Guid ItemId { get; init; }
    public EquipmentSummaryDto? Equipment { get; init; }
    public decimal Quantity { get; init; }
    public string? ConditionAtSend { get; init; }
    public string? ConditionNotes { get; init; }
    public bool IsReceived { get; init; }
    public decimal? ReceivedQuantity { get; init; }
    public string? ConditionAtReceive { get; init; }
    public bool HasDiscrepancy { get; init; }
    public string? DiscrepancyType { get; init; }
    public string? DiscrepancyNotes { get; init; }
}

public record EquipmentSummaryDto
{
    public Guid EquipmentId { get; init; }
    public string AssetNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string? CategoryName { get; init; }
}

public record CreateManifestRequest
{
    [Required]
    public ManifestType Type { get; init; }
    
    [Required]
    public Guid FromLocationId { get; init; }
    
    [MaxLength(100)]
    public string? FromContactName { get; init; }
    
    [Phone]
    public string? FromContactPhone { get; init; }
    
    [EmailAddress]
    public string? FromContactEmail { get; init; }
    
    [Required]
    public Guid ToLocationId { get; init; }
    
    [MaxLength(100)]
    public string? ToContactName { get; init; }
    
    [Phone]
    public string? ToContactPhone { get; init; }
    
    [EmailAddress]
    public string? ToContactEmail { get; init; }
    
    public Guid? ProjectId { get; init; }
    
    public DateTime? ExpectedArrivalDate { get; init; }
    
    public ShippingMethod? ShippingMethod { get; init; }
    
    public string? Notes { get; init; }
}

public record AddManifestItemsRequest
{
    public List<ManifestItemInput> Items { get; init; } = new();
}

public record ManifestItemInput
{
    [Required]
    public Guid EquipmentId { get; init; }
    
    public decimal Quantity { get; init; } = 1;
    
    public EquipmentCondition? ConditionAtSend { get; init; }
    
    public string? ConditionNotes { get; init; }
}

public record ReceiveManifestRequest
{
    public List<ReceivedItemInput> ReceivedItems { get; init; } = new();
    
    public string? GeneralNotes { get; init; }
    
    public string? Signature { get; init; }
}

public record ReceivedItemInput
{
    [Required]
    public Guid ItemId { get; init; }
    
    public decimal ReceivedQuantity { get; init; }
    
    public EquipmentCondition? ConditionAtReceive { get; init; }
    
    public string? ReceiptNotes { get; init; }
    
    public bool HasDiscrepancy { get; init; }
    
    public DiscrepancyType? DiscrepancyType { get; init; }
    
    public string? DiscrepancyNotes { get; init; }
}

public record AddSignatureRequest
{
    [Required]
    public string SignatureType { get; init; } = string.Empty; // "Sender", "Receiver", "Approver"
    
    [Required]
    public string Signature { get; init; } = string.Empty; // Base64 encoded
    
    [MaxLength(100)]
    public string? SignerName { get; init; }
}

public record RejectManifestRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}

public record ShipManifestRequest
{
    public string? ShippingMethod { get; init; }
    
    [MaxLength(200)]
    public string? CarrierName { get; init; }
    
    [MaxLength(100)]
    public string? TrackingNumber { get; init; }
    
    [MaxLength(50)]
    public string? VehicleNumber { get; init; }
    
    [MaxLength(100)]
    public string? DriverName { get; init; }
    
    [Phone]
    public string? DriverPhone { get; init; }
    
    public string? Signature { get; init; }
}

// ============ Location DTOs ============

public record LocationDto
{
    public Guid LocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Type { get; init; }
    public Guid? ParentLocationId { get; init; }
    public string? ContactPerson { get; init; }
    public string? ContactPhone { get; init; }
    public bool IsOffshore { get; init; }
}

// ============ Category DTOs ============

public record CategoryDto
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public bool IsConsumable { get; init; }
    public bool RequiresCertification { get; init; }
    public bool RequiresCalibration { get; init; }
    public List<CategoryDto>? Children { get; init; }
}

// ============ Type DTOs ============

public record EquipmentTypeDto
{
    public Guid TypeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
}

// ============ Project DTOs ============

public record ProjectDto
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Status { get; init; }
    public Guid? LocationId { get; init; }
}

// ============ Sync DTOs ============

public record SyncPullRequest
{
    public string DeviceId { get; init; } = string.Empty;
    public long LastSyncVersion { get; init; }
    public List<string> Tables { get; init; } = new();
}

public record SyncPullResponse
{
    public long NewSyncVersion { get; init; }
    public SyncChangesDto Changes { get; init; } = new();
    public bool HasMore { get; init; }
}

public record SyncChangesDto
{
    public List<SyncRecord<EquipmentDto>> Equipment { get; init; } = new();
    public List<SyncRecord<ManifestDto>> Manifests { get; init; } = new();
    public List<SyncRecord<LocationDto>> Locations { get; init; } = new();
    public List<SyncRecord<CategoryDto>> Categories { get; init; } = new();
    public List<SyncRecord<ProjectDto>> Projects { get; init; } = new();
}

public record SyncRecord<T>
{
    public Guid Id { get; init; }
    public string Operation { get; init; } = string.Empty; // "Insert", "Update", "Delete"
    public T? Data { get; init; }
    public long SyncVersion { get; init; }
}

public record SyncPushRequest
{
    public string DeviceId { get; init; } = string.Empty;
    public List<SyncPushChange> Changes { get; init; } = new();
}

public record SyncPushChange
{
    public string Table { get; init; } = string.Empty;
    public Guid Id { get; init; }
    public string Operation { get; init; } = string.Empty;
    public object? Data { get; init; }
    public DateTime LocalTimestamp { get; init; }
}

public record SyncPushResponse
{
    public int Applied { get; init; }
    public List<SyncConflictDto> Conflicts { get; init; } = new();
}

public record SyncConflictDto
{
    public Guid ConflictId { get; init; }
    public string Table { get; init; } = string.Empty;
    public Guid RecordId { get; init; }
    public object? LocalData { get; init; }
    public object? ServerData { get; init; }
}

public record SyncChangeDto
{
    public string Table { get; init; } = string.Empty;
    public Guid Id { get; init; }
    public string Operation { get; init; } = string.Empty;
    public Dictionary<string, object>? Data { get; init; }
    public DateTime LocalTimestamp { get; init; }
}

public record SyncStatusResponse
{
    public long CurrentSyncVersion { get; init; }
    public DateTime? LastSyncTime { get; init; }
    public int PendingConflicts { get; init; }
    public bool IsOnline { get; init; } = true;
}

public record ResolveConflictRequest
{
    [Required]
    public string Resolution { get; init; } = string.Empty; // "UseLocal", "UseServer", "Merged"
    
    public Dictionary<string, object>? MergedData { get; init; }
}

// ============ Common DTOs ============

public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string>? Errors { get; init; }
}
