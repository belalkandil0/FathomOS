using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for managing favorites (bookmarks) and recent items.
/// Provides quick access to frequently used equipment.
/// </summary>
public class FavoritesService
{
    private readonly LocalDatabaseService _dbService;
    private readonly ModuleSettings _settings;
    
    public FavoritesService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _settings = ModuleSettings.Load();
    }
    
    #region Favorites
    
    /// <summary>
    /// Add equipment to favorites
    /// </summary>
    public void AddToFavorites(Guid equipmentId)
    {
        if (!_settings.FavoriteEquipmentIds.Contains(equipmentId))
        {
            _settings.FavoriteEquipmentIds.Add(equipmentId);
            _settings.Save();
        }
    }
    
    /// <summary>
    /// Remove equipment from favorites
    /// </summary>
    public void RemoveFromFavorites(Guid equipmentId)
    {
        if (_settings.FavoriteEquipmentIds.Remove(equipmentId))
        {
            _settings.Save();
        }
    }
    
    /// <summary>
    /// Check if equipment is in favorites
    /// </summary>
    public bool IsFavorite(Guid equipmentId)
    {
        return _settings.FavoriteEquipmentIds.Contains(equipmentId);
    }
    
    /// <summary>
    /// Toggle favorite status
    /// </summary>
    public bool ToggleFavorite(Guid equipmentId)
    {
        if (IsFavorite(equipmentId))
        {
            RemoveFromFavorites(equipmentId);
            return false;
        }
        else
        {
            AddToFavorites(equipmentId);
            return true;
        }
    }
    
    /// <summary>
    /// Get all favorite equipment
    /// </summary>
    public async Task<List<Equipment>> GetFavoritesAsync()
    {
        var favoriteIds = _settings.FavoriteEquipmentIds;
        if (favoriteIds.Count == 0)
            return new List<Equipment>();
        
        return await _dbService.Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .Where(e => favoriteIds.Contains(e.EquipmentId) && e.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }
    
    /// <summary>
    /// Get favorite count
    /// </summary>
    public int GetFavoriteCount()
    {
        return _settings.FavoriteEquipmentIds.Count;
    }
    
    /// <summary>
    /// Clear all favorites
    /// </summary>
    public void ClearFavorites()
    {
        _settings.FavoriteEquipmentIds.Clear();
        _settings.Save();
    }
    
    #endregion
    
    #region Recent Items
    
    /// <summary>
    /// Add equipment to recent items
    /// </summary>
    public void AddToRecent(Guid equipmentId)
    {
        // Remove if already exists (will be re-added at start)
        _settings.RecentEquipmentIds.Remove(equipmentId);
        
        // Add to start of list
        _settings.RecentEquipmentIds.Insert(0, equipmentId);
        
        // Trim to max size
        while (_settings.RecentEquipmentIds.Count > _settings.MaxRecentItems)
        {
            _settings.RecentEquipmentIds.RemoveAt(_settings.RecentEquipmentIds.Count - 1);
        }
        
        _settings.Save();
    }
    
    /// <summary>
    /// Get recent equipment
    /// </summary>
    public async Task<List<Equipment>> GetRecentAsync(int? limit = null)
    {
        var recentIds = _settings.RecentEquipmentIds;
        if (recentIds.Count == 0)
            return new List<Equipment>();
        
        var take = limit ?? _settings.MaxRecentItems;
        var idsToFetch = recentIds.Take(take).ToList();
        
        var equipment = await _dbService.Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .Where(e => idsToFetch.Contains(e.EquipmentId) && e.IsActive)
            .AsNoTracking()
            .ToListAsync();
        
        // Maintain order from recentIds
        return idsToFetch
            .Select(id => equipment.FirstOrDefault(e => e.EquipmentId == id))
            .Where(e => e != null)
            .Cast<Equipment>()
            .ToList();
    }
    
    /// <summary>
    /// Get recent count
    /// </summary>
    public int GetRecentCount()
    {
        return _settings.RecentEquipmentIds.Count;
    }
    
    /// <summary>
    /// Clear recent items
    /// </summary>
    public void ClearRecent()
    {
        _settings.RecentEquipmentIds.Clear();
        _settings.Save();
    }
    
    #endregion
    
    #region Combined Quick Access
    
    /// <summary>
    /// Get all quick access items (favorites + recent, deduplicated)
    /// </summary>
    public async Task<QuickAccessData> GetQuickAccessAsync()
    {
        var favorites = await GetFavoritesAsync();
        var recent = await GetRecentAsync(10);
        
        // Remove favorites from recent to avoid duplicates
        var favoriteIds = _settings.FavoriteEquipmentIds.ToHashSet();
        var recentNotFavorite = recent.Where(e => !favoriteIds.Contains(e.EquipmentId)).ToList();
        
        return new QuickAccessData
        {
            Favorites = favorites,
            Recent = recentNotFavorite
        };
    }
    
    /// <summary>
    /// Search in favorites
    /// </summary>
    public async Task<List<Equipment>> SearchFavoritesAsync(string searchText)
    {
        var favorites = await GetFavoritesAsync();
        
        if (string.IsNullOrWhiteSpace(searchText))
            return favorites;
        
        searchText = searchText.ToLowerInvariant();
        
        return favorites.Where(e =>
            (e.Name?.ToLowerInvariant().Contains(searchText) ?? false) ||
            (e.AssetNumber?.ToLowerInvariant().Contains(searchText) ?? false) ||
            (e.SerialNumber?.ToLowerInvariant().Contains(searchText) ?? false) ||
            (e.UniqueId?.ToLowerInvariant().Contains(searchText) ?? false)
        ).ToList();
    }
    
    #endregion
    
    #region Favorite Collections
    
    /// <summary>
    /// Export favorites to file
    /// </summary>
    public async Task<string> ExportFavoritesAsync(string destinationPath)
    {
        var data = new FavoriteExportData
        {
            ExportDate = DateTime.UtcNow,
            FavoriteIds = _settings.FavoriteEquipmentIds,
            RecentIds = _settings.RecentEquipmentIds
        };
        
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(destinationPath, json);
        return destinationPath;
    }
    
    /// <summary>
    /// Import favorites from file
    /// </summary>
    public async Task<(int favorites, int recent)> ImportFavoritesAsync(string sourcePath, bool merge = true)
    {
        var json = await File.ReadAllTextAsync(sourcePath);
        var data = JsonSerializer.Deserialize<FavoriteExportData>(json);
        
        if (data == null)
            return (0, 0);
        
        if (!merge)
        {
            _settings.FavoriteEquipmentIds.Clear();
            _settings.RecentEquipmentIds.Clear();
        }
        
        int favoritesAdded = 0;
        foreach (var id in data.FavoriteIds)
        {
            if (!_settings.FavoriteEquipmentIds.Contains(id))
            {
                _settings.FavoriteEquipmentIds.Add(id);
                favoritesAdded++;
            }
        }
        
        int recentAdded = 0;
        foreach (var id in data.RecentIds)
        {
            if (!_settings.RecentEquipmentIds.Contains(id))
            {
                _settings.RecentEquipmentIds.Add(id);
                recentAdded++;
            }
        }
        
        _settings.Save();
        return (favoritesAdded, recentAdded);
    }
    
    #endregion
}

#region Models

public class QuickAccessData
{
    public List<Equipment> Favorites { get; set; } = new();
    public List<Equipment> Recent { get; set; } = new();
    
    public int TotalCount => Favorites.Count + Recent.Count;
}

public class FavoriteExportData
{
    public DateTime ExportDate { get; set; }
    public List<Guid> FavoriteIds { get; set; } = new();
    public List<Guid> RecentIds { get; set; } = new();
}

#endregion
