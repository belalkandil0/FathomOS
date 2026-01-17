using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for managing equipment templates.
/// Templates allow saving common equipment configurations for quick creation.
/// </summary>
public class EquipmentTemplateService
{
    private readonly LocalDatabaseService _dbService;
    private readonly string _templatesPath;
    
    public EquipmentTemplateService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _templatesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "EquipmentInventory", "Templates");
        Directory.CreateDirectory(_templatesPath);
    }
    
    #region Template Management
    
    /// <summary>
    /// Save equipment as a template
    /// </summary>
    public async Task<EquipmentTemplate> SaveAsTemplateAsync(Equipment equipment, string templateName, string? description = null)
    {
        var template = new EquipmentTemplate
        {
            TemplateId = Guid.NewGuid(),
            Name = templateName,
            Description = description,
            CategoryId = equipment.CategoryId,
            CategoryName = equipment.Category?.Name,
            TypeId = equipment.TypeId,
            TypeName = equipment.Type?.Name,
            
            // Copy equipment data
            EquipmentName = equipment.Name,
            EquipmentDescription = equipment.Description,
            Manufacturer = equipment.Manufacturer,
            Model = equipment.Model,
            PartNumber = equipment.PartNumber,
            
            // Physical specs
            WeightKg = equipment.WeightKg,
            LengthCm = equipment.LengthCm,
            WidthCm = equipment.WidthCm,
            HeightCm = equipment.HeightCm,
            
            // Certification/Calibration settings
            RequiresCertification = equipment.RequiresCertification,
            RequiresCalibration = equipment.RequiresCalibration,
            CalibrationIntervalDays = equipment.CalibrationIntervalDays,
            ServiceIntervalDays = equipment.ServiceIntervalDays,
            
            // Consumable settings
            IsConsumable = equipment.IsConsumable,
            MinimumQuantity = equipment.MinimumQuantity,
            ReorderPoint = equipment.ReorderPoint,
            UnitOfMeasure = equipment.UnitOfMeasure,
            
            // Defaults
            DefaultCondition = equipment.Condition.ToString(),
            DefaultLocationId = equipment.CurrentLocationId,
            DefaultSupplierId = equipment.SupplierId,
            
            // Metadata
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null, // Set from context if available
            UsageCount = 0
        };
        
        await SaveTemplateToFileAsync(template);
        return template;
    }
    
    /// <summary>
    /// Create equipment from a template
    /// </summary>
    public async Task<Equipment> CreateFromTemplateAsync(Guid templateId, string? customName = null, Guid? locationId = null)
    {
        var template = await GetTemplateAsync(templateId);
        if (template == null)
            throw new ArgumentException("Template not found", nameof(templateId));
        
        var equipment = new Equipment
        {
            EquipmentId = Guid.NewGuid(),
            AssetNumber = await _dbService.GenerateAssetNumberAsync(template.CategoryName?.Substring(0, 3)),
            Name = customName ?? template.EquipmentName,
            Description = template.EquipmentDescription,
            
            CategoryId = template.CategoryId,
            TypeId = template.TypeId,
            
            Manufacturer = template.Manufacturer,
            Model = template.Model,
            PartNumber = template.PartNumber,
            
            WeightKg = template.WeightKg,
            LengthCm = template.LengthCm,
            WidthCm = template.WidthCm,
            HeightCm = template.HeightCm,
            
            RequiresCertification = template.RequiresCertification,
            RequiresCalibration = template.RequiresCalibration,
            CalibrationIntervalDays = template.CalibrationIntervalDays,
            ServiceIntervalDays = template.ServiceIntervalDays,
            
            IsConsumable = template.IsConsumable,
            MinimumQuantity = template.MinimumQuantity,
            ReorderPoint = template.ReorderPoint,
            UnitOfMeasure = template.UnitOfMeasure,
            
            Condition = Enum.TryParse<EquipmentCondition>(template.DefaultCondition, out var cond) ? cond : EquipmentCondition.Good,
            CurrentLocationId = locationId ?? template.DefaultLocationId,
            SupplierId = template.DefaultSupplierId,
            
            Status = EquipmentStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Update template usage count
        template.UsageCount++;
        template.LastUsedAt = DateTime.UtcNow;
        await SaveTemplateToFileAsync(template);
        
        return equipment;
    }
    
    /// <summary>
    /// Get all templates
    /// </summary>
    public async Task<List<EquipmentTemplate>> GetAllTemplatesAsync()
    {
        var templates = new List<EquipmentTemplate>();
        
        foreach (var file in Directory.GetFiles(_templatesPath, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var template = JsonSerializer.Deserialize<EquipmentTemplate>(json);
                if (template != null)
                    templates.Add(template);
            }
            catch
            {
                // Skip invalid files
            }
        }
        
        return templates.OrderByDescending(t => t.UsageCount).ThenBy(t => t.Name).ToList();
    }
    
    /// <summary>
    /// Get templates by category
    /// </summary>
    public async Task<List<EquipmentTemplate>> GetTemplatesByCategoryAsync(Guid categoryId)
    {
        var all = await GetAllTemplatesAsync();
        return all.Where(t => t.CategoryId == categoryId).ToList();
    }
    
    /// <summary>
    /// Get a specific template
    /// </summary>
    public async Task<EquipmentTemplate?> GetTemplateAsync(Guid templateId)
    {
        var filePath = Path.Combine(_templatesPath, $"{templateId}.json");
        if (!File.Exists(filePath))
            return null;
        
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<EquipmentTemplate>(json);
    }
    
    /// <summary>
    /// Update a template
    /// </summary>
    public async Task<bool> UpdateTemplateAsync(EquipmentTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        await SaveTemplateToFileAsync(template);
        return true;
    }
    
    /// <summary>
    /// Delete a template
    /// </summary>
    public bool DeleteTemplate(Guid templateId)
    {
        var filePath = Path.Combine(_templatesPath, $"{templateId}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Export templates to file
    /// </summary>
    public async Task<string> ExportTemplatesAsync(string destinationPath)
    {
        var templates = await GetAllTemplatesAsync();
        var json = JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(destinationPath, json);
        return destinationPath;
    }
    
    /// <summary>
    /// Import templates from file
    /// </summary>
    public async Task<int> ImportTemplatesAsync(string sourcePath)
    {
        var json = await File.ReadAllTextAsync(sourcePath);
        var templates = JsonSerializer.Deserialize<List<EquipmentTemplate>>(json);
        
        if (templates == null) return 0;
        
        int imported = 0;
        foreach (var template in templates)
        {
            template.TemplateId = Guid.NewGuid(); // New ID to avoid conflicts
            await SaveTemplateToFileAsync(template);
            imported++;
        }
        
        return imported;
    }
    
    #endregion
    
    #region Helpers
    
    private async Task SaveTemplateToFileAsync(EquipmentTemplate template)
    {
        var filePath = Path.Combine(_templatesPath, $"{template.TemplateId}.json");
        var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
    
    #endregion
}

#region Models

/// <summary>
/// Equipment template for quick creation
/// </summary>
public class EquipmentTemplate
{
    public Guid TemplateId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Alias for Name property for backwards compatibility</summary>
    public string TemplateName
    {
        get => Name;
        set => Name = value;
    }

    // Category/Type
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid? TypeId { get; set; }
    public string? TypeName { get; set; }

    /// <summary>Alias for CategoryName property for backwards compatibility</summary>
    public string? Category
    {
        get => CategoryName;
        set => CategoryName = value;
    }
    
    // Equipment Details
    public string? EquipmentName { get; set; }
    public string? EquipmentDescription { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? PartNumber { get; set; }
    
    // Physical Specs
    public decimal? WeightKg { get; set; }
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    
    // Certification/Calibration
    public bool RequiresCertification { get; set; }
    public bool RequiresCalibration { get; set; }
    public int? CalibrationIntervalDays { get; set; }
    public int? ServiceIntervalDays { get; set; }
    
    // Consumable Settings
    public bool IsConsumable { get; set; }
    public decimal? MinimumQuantity { get; set; }
    public decimal? ReorderPoint { get; set; }
    public string? UnitOfMeasure { get; set; }
    
    // Defaults
    public string? DefaultCondition { get; set; }
    public Guid? DefaultLocationId { get; set; }
    public Guid? DefaultSupplierId { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    // Display
    public string DisplayName => $"{Name} ({CategoryName ?? "General"})";
}

#endregion
