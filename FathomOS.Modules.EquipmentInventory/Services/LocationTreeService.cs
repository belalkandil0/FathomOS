using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for managing hierarchical location tree view.
/// Provides tree structure for Region → Base → Warehouse → Container navigation.
/// </summary>
public class LocationTreeService
{
    private readonly LocalDatabaseService _dbService;
    
    public LocationTreeService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
    }
    
    #region Tree Building
    
    /// <summary>
    /// Build complete location tree
    /// </summary>
    public async Task<List<LocationTreeNode>> GetLocationTreeAsync(bool includeEquipmentCounts = true)
    {
        var locations = await _dbService.Context.Locations
            .Include(l => l.LocationTypeRecord)
            .Where(l => l.IsActive)
            .AsNoTracking()
            .ToListAsync();
        
        Dictionary<Guid, int>? equipmentCounts = null;
        if (includeEquipmentCounts)
        {
            equipmentCounts = await _dbService.Context.Equipment
                .Where(e => e.IsActive && e.CurrentLocationId.HasValue)
                .GroupBy(e => e.CurrentLocationId!.Value)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.LocationId, x => x.Count);
        }
        
        // Build tree from root locations (no parent)
        var rootNodes = BuildTreeNodes(locations, null, equipmentCounts);
        
        return rootNodes.OrderBy(n => n.Name).ToList();
    }
    
    /// <summary>
    /// Build tree for a specific region
    /// </summary>
    public async Task<LocationTreeNode?> GetRegionTreeAsync(Guid regionId, bool includeEquipmentCounts = true)
    {
        var tree = await GetLocationTreeAsync(includeEquipmentCounts);
        return FindNode(tree, regionId);
    }
    
    /// <summary>
    /// Get flat list with hierarchy indicators
    /// </summary>
    public async Task<List<FlatLocationItem>> GetFlatLocationListAsync()
    {
        var tree = await GetLocationTreeAsync(true);
        var flatList = new List<FlatLocationItem>();
        FlattenTree(tree, flatList, 0);
        return flatList;
    }
    
    #endregion
    
    #region Tree Operations
    
    /// <summary>
    /// Get location path (breadcrumb)
    /// </summary>
    public async Task<List<LocationPathItem>> GetLocationPathAsync(Guid locationId)
    {
        var path = new List<LocationPathItem>();
        var current = await _dbService.Context.Locations
            .Include(l => l.ParentLocation)
            .FirstOrDefaultAsync(l => l.LocationId == locationId);
        
        while (current != null)
        {
            path.Insert(0, new LocationPathItem
            {
                LocationId = current.LocationId,
                Name = current.Name,
                Code = current.Code,
                Type = current.Type
            });
            
            current = current.ParentLocation;
        }
        
        return path;
    }
    
    /// <summary>
    /// Get all child locations (recursive)
    /// </summary>
    public async Task<List<Location>> GetAllChildLocationsAsync(Guid parentLocationId)
    {
        var allLocations = await _dbService.Context.Locations
            .Where(l => l.IsActive)
            .AsNoTracking()
            .ToListAsync();
        
        var children = new List<Location>();
        CollectChildren(allLocations, parentLocationId, children);
        return children;
    }
    
    /// <summary>
    /// Get equipment in location and all children
    /// </summary>
    public async Task<List<Equipment>> GetEquipmentInLocationTreeAsync(Guid locationId)
    {
        var locationIds = new List<Guid> { locationId };
        var children = await GetAllChildLocationsAsync(locationId);
        locationIds.AddRange(children.Select(c => c.LocationId));
        
        return await _dbService.Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .Where(e => e.IsActive && e.CurrentLocationId.HasValue && locationIds.Contains(e.CurrentLocationId.Value))
            .AsNoTracking()
            .ToListAsync();
    }
    
    /// <summary>
    /// Search locations in tree
    /// </summary>
    public async Task<List<LocationTreeNode>> SearchLocationsAsync(string searchText)
    {
        var tree = await GetLocationTreeAsync(false);
        var results = new List<LocationTreeNode>();
        
        searchText = searchText.ToLowerInvariant();
        SearchInTree(tree, searchText, results);
        
        return results;
    }
    
    /// <summary>
    /// Get location statistics
    /// </summary>
    public async Task<LocationStatistics> GetLocationStatisticsAsync(Guid locationId)
    {
        var equipment = await GetEquipmentInLocationTreeAsync(locationId);
        
        return new LocationStatistics
        {
            TotalEquipment = equipment.Count,
            EquipmentByStatus = equipment.GroupBy(e => e.Status)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            EquipmentByCategory = equipment.Where(e => e.Category != null)
                .GroupBy(e => e.Category!.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            CertificationsExpiring = equipment.Count(e => 
                e.RequiresCertification && 
                e.CertificationExpiryDate.HasValue &&
                e.CertificationExpiryDate.Value <= DateTime.Today.AddDays(30)),
            CalibrationsOverdue = equipment.Count(e =>
                e.RequiresCalibration &&
                e.NextCalibrationDate.HasValue &&
                e.NextCalibrationDate.Value < DateTime.Today)
        };
    }
    
    #endregion
    
    #region Helpers
    
    private List<LocationTreeNode> BuildTreeNodes(List<Location> allLocations, Guid? parentId, Dictionary<Guid, int>? equipmentCounts)
    {
        var nodes = new List<LocationTreeNode>();
        
        var children = allLocations.Where(l => l.ParentLocationId == parentId).ToList();
        
        foreach (var location in children)
        {
            var node = new LocationTreeNode
            {
                LocationId = location.LocationId,
                Name = location.Name,
                Code = location.Code,
                Type = location.Type,
                TypeName = location.LocationTypeRecord?.Name ?? location.Type.ToString(),
                IsOffshore = location.IsOffshore,
                EquipmentCount = equipmentCounts?.GetValueOrDefault(location.LocationId) ?? 0,
                Children = BuildTreeNodes(allLocations, location.LocationId, equipmentCounts)
            };
            
            // Calculate total equipment including children
            node.TotalEquipmentCount = node.EquipmentCount + node.Children.Sum(c => c.TotalEquipmentCount);
            
            nodes.Add(node);
        }
        
        return nodes;
    }
    
    private static LocationTreeNode? FindNode(List<LocationTreeNode> nodes, Guid locationId)
    {
        foreach (var node in nodes)
        {
            if (node.LocationId == locationId)
                return node;
            
            var found = FindNode(node.Children, locationId);
            if (found != null)
                return found;
        }
        return null;
    }
    
    private static void FlattenTree(List<LocationTreeNode> nodes, List<FlatLocationItem> flatList, int depth)
    {
        foreach (var node in nodes)
        {
            flatList.Add(new FlatLocationItem
            {
                LocationId = node.LocationId,
                Name = node.Name,
                Code = node.Code,
                Type = node.Type,
                Depth = depth,
                IndentedName = new string(' ', depth * 3) + (depth > 0 ? "└─ " : "") + node.Name,
                EquipmentCount = node.EquipmentCount,
                TotalEquipmentCount = node.TotalEquipmentCount,
                HasChildren = node.Children.Count > 0
            });
            
            FlattenTree(node.Children, flatList, depth + 1);
        }
    }
    
    private static void CollectChildren(List<Location> allLocations, Guid parentId, List<Location> result)
    {
        var children = allLocations.Where(l => l.ParentLocationId == parentId).ToList();
        foreach (var child in children)
        {
            result.Add(child);
            CollectChildren(allLocations, child.LocationId, result);
        }
    }
    
    private static void SearchInTree(List<LocationTreeNode> nodes, string searchText, List<LocationTreeNode> results)
    {
        foreach (var node in nodes)
        {
            if (node.Name.ToLowerInvariant().Contains(searchText) ||
                node.Code.ToLowerInvariant().Contains(searchText))
            {
                results.Add(node);
            }
            
            SearchInTree(node.Children, searchText, results);
        }
    }
    
    #endregion
}

#region Models

/// <summary>
/// Tree node for hierarchical location display
/// </summary>
public class LocationTreeNode
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public LocationType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public bool IsOffshore { get; set; }
    public int EquipmentCount { get; set; }
    public int TotalEquipmentCount { get; set; }
    public List<LocationTreeNode> Children { get; set; } = new();
    
    public bool HasChildren => Children.Count > 0;
    public bool IsExpanded { get; set; }
    
    public string Icon => Type switch
    {
        LocationType.Region => "MapMarkerRadius",
        LocationType.Base => "OfficeBuilding",
        LocationType.Vessel => "Ferry",
        LocationType.Warehouse => "Warehouse",
        LocationType.Container => "PackageVariant",
        LocationType.ProjectSite => "HardHat",
        LocationType.Workshop => "Tools",
        _ => "MapMarker"
    };
}

/// <summary>
/// Flat list item with hierarchy info
/// </summary>
public class FlatLocationItem
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public LocationType Type { get; set; }
    public int Depth { get; set; }
    public string IndentedName { get; set; } = string.Empty;
    public int EquipmentCount { get; set; }
    public int TotalEquipmentCount { get; set; }
    public bool HasChildren { get; set; }
}

/// <summary>
/// Breadcrumb path item
/// </summary>
public class LocationPathItem
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public LocationType Type { get; set; }
}

/// <summary>
/// Location statistics summary
/// </summary>
public class LocationStatistics
{
    public int TotalEquipment { get; set; }
    public Dictionary<string, int> EquipmentByStatus { get; set; } = new();
    public Dictionary<string, int> EquipmentByCategory { get; set; } = new();
    public int CertificationsExpiring { get; set; }
    public int CalibrationsOverdue { get; set; }
}

#endregion
