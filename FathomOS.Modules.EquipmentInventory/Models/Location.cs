using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Models;

public class Location
{
    [Key]
    public Guid LocationId { get; set; } = Guid.NewGuid();
    public Guid? RegionId { get; set; }
    public Guid? ParentLocationId { get; set; }
    [ForeignKey(nameof(ParentLocationId))]
    [JsonIgnore]
    public virtual Location? ParentLocation { get; set; }
    public Guid? LocationTypeId { get; set; }
    [ForeignKey(nameof(LocationTypeId))]
    [JsonIgnore]
    public virtual LocationTypeRecord? LocationTypeRecord { get; set; }
    
    /// <summary>
    /// Quick reference location type (Base, Vessel, Warehouse, etc.)
    /// </summary>
    public LocationType Type { get; set; } = LocationType.Base;

    /// <summary>
    /// Alias for Type property for backwards compatibility
    /// </summary>
    [NotMapped]
    public LocationType LocationType
    {
        get => Type;
        set => Type = value;
    }
    
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required][MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    [MaxLength(50)]
    public string? ContactPhone { get; set; }
    [MaxLength(100)]
    public string? ContactEmail { get; set; }
    public bool IsOffshore { get; set; }
    public bool IsActive { get; set; } = true;
    public int? Capacity { get; set; }
    [MaxLength(100)]
    public string? QrCode { get; set; }
    DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long SyncVersion { get; set; }
    
    [JsonIgnore]
    public virtual ICollection<Location> ChildLocations { get; set; } = new List<Location>();
    [JsonIgnore]
    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    
    [NotMapped]
    public string DisplayName => $"{Name} ({Code})";
}

/// <summary>
/// Location type lookup table entity (renamed from LocationType to avoid conflict with enum)
/// </summary>
public class LocationTypeRecord
{
    [Key]
    public Guid LocationTypeId { get; set; } = Guid.NewGuid();
    [Required][MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? Icon { get; set; }
    [MaxLength(7)]
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Region
{
    [Key]
    public Guid RegionId { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    [Required][MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Vessel
{
    [Key]
    public Guid VesselId { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    [ForeignKey(nameof(LocationId))]
    public virtual Location? Location { get; set; }
    [MaxLength(20)]
    public string? IMONumber { get; set; }
    [MaxLength(20)]
    public string? CallSign { get; set; }
    [MaxLength(50)]
    public string? Flag { get; set; }
    [MaxLength(50)]
    public string? VesselType { get; set; }
    public decimal? GrossTonnage { get; set; }
    public bool IsActive { get; set; } = true;
}
