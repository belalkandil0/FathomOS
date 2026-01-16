using System.Text.Json.Serialization;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Api.DTOs;

// ============ Manifest DTOs ============

public record ManifestDto
{
    [JsonPropertyName("manifestId")]
    public Guid ManifestId { get; init; }
    
    [JsonPropertyName("manifestNumber")]
    public string ManifestNumber { get; init; } = string.Empty;
    
    [JsonPropertyName("qrCode")]
    public string? QrCode { get; init; }
    
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
    
    [JsonPropertyName("fromLocation")]
    public LocationDto? FromLocation { get; init; }
    
    [JsonPropertyName("fromContact")]
    public ContactDto? FromContact { get; init; }
    
    [JsonPropertyName("toLocation")]
    public LocationDto? ToLocation { get; init; }
    
    [JsonPropertyName("toContact")]
    public ContactDto? ToContact { get; init; }
    
    [JsonPropertyName("project")]
    public ProjectDto? Project { get; init; }
    
    [JsonPropertyName("dates")]
    public ManifestDatesDto Dates { get; init; } = new();
    
    [JsonPropertyName("shipping")]
    public ShippingInfoDto? Shipping { get; init; }
    
    [JsonPropertyName("totals")]
    public ManifestTotalsDto Totals { get; init; } = new();
    
    [JsonPropertyName("items")]
    public List<ManifestItemDto> Items { get; init; } = new();
    
    [JsonPropertyName("signatures")]
    public ManifestSignaturesDto? Signatures { get; init; }
    
    [JsonPropertyName("photos")]
    public List<PhotoDto> Photos { get; init; } = new();
    
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
    
    [JsonPropertyName("hasDiscrepancies")]
    public bool HasDiscrepancies { get; init; }
    
    [JsonPropertyName("syncVersion")]
    public long SyncVersion { get; init; }
}

public record ContactDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }
    
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

public record ManifestDatesDto
{
    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }
    
    [JsonPropertyName("submitted")]
    public DateTime? Submitted { get; init; }
    
    [JsonPropertyName("approved")]
    public DateTime? Approved { get; init; }
    
    [JsonPropertyName("shipped")]
    public DateTime? Shipped { get; init; }
    
    [JsonPropertyName("expectedArrival")]
    public DateTime? ExpectedArrival { get; init; }
    
    [JsonPropertyName("received")]
    public DateTime? Received { get; init; }
}

public record ShippingInfoDto
{
    [JsonPropertyName("method")]
    public string? Method { get; init; }
    
    [JsonPropertyName("carrier")]
    public string? Carrier { get; init; }
    
    [JsonPropertyName("trackingNumber")]
    public string? TrackingNumber { get; init; }
}

public record ManifestTotalsDto
{
    [JsonPropertyName("items")]
    public int Items { get; init; }
    
    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; init; }
    
    [JsonPropertyName("volumeCm3")]
    public decimal? VolumeCm3 { get; init; }
}

public record ManifestSignaturesDto
{
    [JsonPropertyName("sender")]
    public SignatureDto? Sender { get; init; }
    
    [JsonPropertyName("receiver")]
    public SignatureDto? Receiver { get; init; }
    
    [JsonPropertyName("approver")]
    public SignatureDto? Approver { get; init; }
}

public record SignatureDto
{
    [JsonPropertyName("signature")]
    public string? Signature { get; init; }
    
    [JsonPropertyName("signedAt")]
    public DateTime? SignedAt { get; init; }
    
    [JsonPropertyName("signedBy")]
    public string? SignedBy { get; init; }
}

public record ManifestItemDto
{
    [JsonPropertyName("itemId")]
    public Guid ItemId { get; init; }
    
    [JsonPropertyName("equipment")]
    public EquipmentSummaryDto? Equipment { get; init; }
    
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; init; }
    
    [JsonPropertyName("conditionAtSend")]
    public string? ConditionAtSend { get; init; }
    
    [JsonPropertyName("conditionNotes")]
    public string? ConditionNotes { get; init; }
    
    [JsonPropertyName("isReceived")]
    public bool IsReceived { get; init; }
    
    [JsonPropertyName("receivedQuantity")]
    public decimal? ReceivedQuantity { get; init; }
    
    [JsonPropertyName("conditionAtReceive")]
    public string? ConditionAtReceive { get; init; }
    
    [JsonPropertyName("hasDiscrepancy")]
    public bool HasDiscrepancy { get; init; }
    
    [JsonPropertyName("discrepancyType")]
    public string? DiscrepancyType { get; init; }
    
    [JsonPropertyName("discrepancyNotes")]
    public string? DiscrepancyNotes { get; init; }
}

public record EquipmentSummaryDto
{
    [JsonPropertyName("equipmentId")]
    public Guid EquipmentId { get; init; }
    
    [JsonPropertyName("assetNumber")]
    public string AssetNumber { get; init; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; init; }
    
    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; init; }
}

// ============ Manifest Request DTOs ============

public record CreateManifestRequest
{
    [JsonPropertyName("type")]
    public ManifestType Type { get; init; }
    
    [JsonPropertyName("fromLocationId")]
    public Guid FromLocationId { get; init; }
    
    [JsonPropertyName("fromContactName")]
    public string? FromContactName { get; init; }
    
    [JsonPropertyName("fromContactPhone")]
    public string? FromContactPhone { get; init; }
    
    [JsonPropertyName("fromContactEmail")]
    public string? FromContactEmail { get; init; }
    
    [JsonPropertyName("toLocationId")]
    public Guid ToLocationId { get; init; }
    
    [JsonPropertyName("toContactName")]
    public string? ToContactName { get; init; }
    
    [JsonPropertyName("toContactPhone")]
    public string? ToContactPhone { get; init; }
    
    [JsonPropertyName("toContactEmail")]
    public string? ToContactEmail { get; init; }
    
    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }
    
    [JsonPropertyName("expectedArrivalDate")]
    public DateTime? ExpectedArrivalDate { get; init; }
    
    [JsonPropertyName("shippingMethod")]
    public ShippingMethod? ShippingMethod { get; init; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public record AddManifestItemsRequest
{
    [JsonPropertyName("items")]
    public List<ManifestItemInput> Items { get; init; } = new();
}

public record ManifestItemInput
{
    [JsonPropertyName("equipmentId")]
    public Guid EquipmentId { get; init; }
    
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; init; } = 1;
    
    [JsonPropertyName("conditionAtSend")]
    public EquipmentCondition? ConditionAtSend { get; init; }
    
    [JsonPropertyName("conditionNotes")]
    public string? ConditionNotes { get; init; }
}

public record ReceiveManifestRequest
{
    [JsonPropertyName("receivedItems")]
    public List<ReceivedItemInput> ReceivedItems { get; init; } = new();
    
    [JsonPropertyName("generalNotes")]
    public string? GeneralNotes { get; init; }
    
    [JsonPropertyName("signature")]
    public string? Signature { get; init; }
}

public record ReceivedItemInput
{
    [JsonPropertyName("itemId")]
    public Guid ItemId { get; init; }
    
    [JsonPropertyName("receivedQuantity")]
    public decimal ReceivedQuantity { get; init; }
    
    [JsonPropertyName("conditionAtReceive")]
    public EquipmentCondition? ConditionAtReceive { get; init; }
    
    [JsonPropertyName("receiptNotes")]
    public string? ReceiptNotes { get; init; }
    
    [JsonPropertyName("hasDiscrepancy")]
    public bool HasDiscrepancy { get; init; }
    
    [JsonPropertyName("discrepancyType")]
    public DiscrepancyType? DiscrepancyType { get; init; }
    
    [JsonPropertyName("discrepancyNotes")]
    public string? DiscrepancyNotes { get; init; }
}

public record AddSignatureRequest
{
    [JsonPropertyName("signatureType")]
    public string SignatureType { get; init; } = string.Empty; // "Sender", "Receiver", "Approver"
    
    [JsonPropertyName("signature")]
    public string Signature { get; init; } = string.Empty; // Base64 encoded
    
    [JsonPropertyName("signerName")]
    public string? SignerName { get; init; }
}

public record RejectManifestRequest
{
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}

public record ShipManifestRequest
{
    [JsonPropertyName("shippingMethod")]
    public string? ShippingMethod { get; init; }
    
    [JsonPropertyName("carrierName")]
    public string? CarrierName { get; init; }
    
    [JsonPropertyName("trackingNumber")]
    public string? TrackingNumber { get; init; }
    
    [JsonPropertyName("vehicleNumber")]
    public string? VehicleNumber { get; init; }
    
    [JsonPropertyName("driverName")]
    public string? DriverName { get; init; }
    
    [JsonPropertyName("driverPhone")]
    public string? DriverPhone { get; init; }
    
    [JsonPropertyName("signature")]
    public string? Signature { get; init; }
}
