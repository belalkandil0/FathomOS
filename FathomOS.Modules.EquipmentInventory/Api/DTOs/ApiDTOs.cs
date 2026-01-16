using System.Text.Json.Serialization;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Api.DTOs;

// ============ Authentication DTOs ============

public record LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;
    
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
    
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; init; }
    
    [JsonPropertyName("user")]
    public UserDto? User { get; init; }
}

public record RefreshTokenRequest
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
}

public record UserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }
    
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string? Email { get; init; }
    
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }
    
    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }
    
    [JsonPropertyName("roles")]
    public List<string> Roles { get; init; } = new();
    
    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; init; } = new();
    
    [JsonPropertyName("defaultLocationId")]
    public Guid? DefaultLocationId { get; init; }
}

// ============ Equipment DTOs ============

public record EquipmentDto
{
    [JsonPropertyName("equipmentId")]
    public Guid EquipmentId { get; init; }
    
    [JsonPropertyName("assetNumber")]
    public string AssetNumber { get; init; } = string.Empty;
    
    [JsonPropertyName("uniqueId")]
    public string? UniqueId { get; init; }
    
    [JsonPropertyName("sapNumber")]
    public string? SapNumber { get; init; }
    
    [JsonPropertyName("techNumber")]
    public string? TechNumber { get; init; }
    
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; init; }
    
    [JsonPropertyName("qrCode")]
    public string? QrCode { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; init; }
    
    [JsonPropertyName("model")]
    public string? Model { get; init; }
    
    [JsonPropertyName("category")]
    public CategoryDto? Category { get; init; }
    
    [JsonPropertyName("type")]
    public EquipmentTypeDto? Type { get; init; }
    
    [JsonPropertyName("currentLocation")]
    public LocationDto? CurrentLocation { get; init; }
    
    [JsonPropertyName("currentProject")]
    public ProjectDto? CurrentProject { get; init; }
    
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
    
    [JsonPropertyName("condition")]
    public string Condition { get; init; } = string.Empty;
    
    [JsonPropertyName("ownershipType")]
    public string OwnershipType { get; init; } = string.Empty;
    
    [JsonPropertyName("physical")]
    public PhysicalPropertiesDto? Physical { get; init; }
    
    [JsonPropertyName("certification")]
    public CertificationDto? Certification { get; init; }
    
    [JsonPropertyName("calibration")]
    public CalibrationDto? Calibration { get; init; }
    
    [JsonPropertyName("photos")]
    public List<PhotoDto> Photos { get; init; } = new();
    
    [JsonPropertyName("primaryPhotoUrl")]
    public string? PrimaryPhotoUrl { get; init; }
    
    [JsonPropertyName("qrCodeImageUrl")]
    public string? QrCodeImageUrl { get; init; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
    
    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; init; }
    
    [JsonPropertyName("syncVersion")]
    public long SyncVersion { get; init; }
}

public record PhysicalPropertiesDto
{
    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; init; }
    
    [JsonPropertyName("lengthCm")]
    public decimal? LengthCm { get; init; }
    
    [JsonPropertyName("widthCm")]
    public decimal? WidthCm { get; init; }
    
    [JsonPropertyName("heightCm")]
    public decimal? HeightCm { get; init; }
}

public record CertificationDto
{
    [JsonPropertyName("required")]
    public bool Required { get; init; }
    
    [JsonPropertyName("number")]
    public string? Number { get; init; }
    
    [JsonPropertyName("body")]
    public string? Body { get; init; }
    
    [JsonPropertyName("expiryDate")]
    public DateTime? ExpiryDate { get; init; }
}

public record CalibrationDto
{
    [JsonPropertyName("required")]
    public bool Required { get; init; }
    
    [JsonPropertyName("lastDate")]
    public DateTime? LastDate { get; init; }
    
    [JsonPropertyName("nextDate")]
    public DateTime? NextDate { get; init; }
    
    [JsonPropertyName("intervalDays")]
    public int? IntervalDays { get; init; }
}

public record PhotoDto
{
    [JsonPropertyName("photoId")]
    public Guid PhotoId { get; init; }
    
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
    
    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; init; }
    
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }
}

public record CreateEquipmentRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("uniqueId")]
    public string? UniqueId { get; init; }
    
    [JsonPropertyName("categoryId")]
    public Guid? CategoryId { get; init; }
    
    [JsonPropertyName("typeId")]
    public Guid? TypeId { get; init; }
    
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; init; }
    
    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; init; }
    
    [JsonPropertyName("model")]
    public string? Model { get; init; }
    
    [JsonPropertyName("currentLocationId")]
    public Guid? CurrentLocationId { get; init; }
    
    [JsonPropertyName("currentProjectId")]
    public Guid? CurrentProjectId { get; init; }
    
    [JsonPropertyName("status")]
    public EquipmentStatus Status { get; init; } = EquipmentStatus.Available;
    
    [JsonPropertyName("condition")]
    public EquipmentCondition Condition { get; init; } = EquipmentCondition.New;
    
    [JsonPropertyName("ownershipType")]
    public OwnershipType OwnershipType { get; init; } = Models.OwnershipType.Owned;
    
    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; init; }
    
    [JsonPropertyName("lengthCm")]
    public decimal? LengthCm { get; init; }
    
    [JsonPropertyName("widthCm")]
    public decimal? WidthCm { get; init; }
    
    [JsonPropertyName("heightCm")]
    public decimal? HeightCm { get; init; }
    
    [JsonPropertyName("requiresCertification")]
    public bool RequiresCertification { get; init; }
    
    [JsonPropertyName("requiresCalibration")]
    public bool RequiresCalibration { get; init; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public record UpdateEquipmentRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("uniqueId")]
    public string? UniqueId { get; init; }
    
    [JsonPropertyName("categoryId")]
    public Guid? CategoryId { get; init; }
    
    [JsonPropertyName("typeId")]
    public Guid? TypeId { get; init; }
    
    [JsonPropertyName("currentLocationId")]
    public Guid? CurrentLocationId { get; init; }
    
    [JsonPropertyName("currentProjectId")]
    public Guid? CurrentProjectId { get; init; }
    
    [JsonPropertyName("status")]
    public EquipmentStatus? Status { get; init; }
    
    [JsonPropertyName("condition")]
    public EquipmentCondition? Condition { get; init; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

// ============ Location DTOs ============

public record LocationDto
{
    [JsonPropertyName("locationId")]
    public Guid LocationId { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    
    [JsonPropertyName("parentLocationId")]
    public Guid? ParentLocationId { get; init; }
    
    [JsonPropertyName("contactPerson")]
    public string? ContactPerson { get; init; }
    
    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; init; }
    
    [JsonPropertyName("isOffshore")]
    public bool IsOffshore { get; init; }
}

// ============ Category DTOs ============

public record CategoryDto
{
    [JsonPropertyName("categoryId")]
    public Guid CategoryId { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
    
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }
    
    [JsonPropertyName("color")]
    public string? Color { get; init; }
    
    [JsonPropertyName("isConsumable")]
    public bool IsConsumable { get; init; }
    
    [JsonPropertyName("requiresCertification")]
    public bool RequiresCertification { get; init; }
    
    [JsonPropertyName("requiresCalibration")]
    public bool RequiresCalibration { get; init; }
}

// ============ Type DTOs ============

public record EquipmentTypeDto
{
    [JsonPropertyName("typeId")]
    public Guid TypeId { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
    
    [JsonPropertyName("categoryId")]
    public Guid? CategoryId { get; init; }
}

// ============ Project DTOs ============

public record ProjectDto
{
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string? Status { get; init; }
    
    [JsonPropertyName("locationId")]
    public Guid? LocationId { get; init; }
}

// ============ Common DTOs ============

public record PagedResult<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; init; } = new();
    
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
    
    [JsonPropertyName("page")]
    public int Page { get; init; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
    
    [JsonPropertyName("data")]
    public T? Data { get; init; }
    
    [JsonPropertyName("message")]
    public string? Message { get; init; }
    
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; init; }
}
