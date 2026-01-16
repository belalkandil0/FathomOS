using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for comparing equipment items side-by-side.
/// Highlights differences and similarities between 2-3 items.
/// </summary>
public class EquipmentComparisonService
{
    private readonly LocalDatabaseService _dbService;
    
    public EquipmentComparisonService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
    }
    
    #region Comparison
    
    /// <summary>
    /// Compare multiple equipment items
    /// </summary>
    public async Task<ComparisonResult> CompareEquipmentAsync(params Guid[] equipmentIds)
    {
        if (equipmentIds.Length < 2 || equipmentIds.Length > 4)
            throw new ArgumentException("Please select 2-4 items to compare");
        
        var equipment = await _dbService.Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.Type)
            .Include(e => e.CurrentLocation)
            .Include(e => e.Supplier)
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .AsNoTracking()
            .ToListAsync();
        
        // Order by the input order
        var orderedEquipment = equipmentIds
            .Select(id => equipment.FirstOrDefault(e => e.EquipmentId == id))
            .Where(e => e != null)
            .Cast<Equipment>()
            .ToList();
        
        var result = new ComparisonResult
        {
            Items = orderedEquipment.Select(e => new ComparisonItem
            {
                EquipmentId = e.EquipmentId,
                AssetNumber = e.AssetNumber,
                Name = e.Name ?? "Unknown",
                PhotoUrl = e.PrimaryPhotoUrl
            }).ToList()
        };
        
        // Build comparison rows
        result.Rows = BuildComparisonRows(orderedEquipment);
        
        // Calculate similarity score
        result.SimilarityScore = CalculateSimilarity(result.Rows);
        
        return result;
    }
    
    /// <summary>
    /// Find similar equipment
    /// </summary>
    public async Task<List<SimilarEquipmentItem>> FindSimilarEquipmentAsync(Guid equipmentId, int maxResults = 10)
    {
        var source = await _dbService.Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.Type)
            .FirstOrDefaultAsync(e => e.EquipmentId == equipmentId);
        
        if (source == null)
            return new List<SimilarEquipmentItem>();
        
        var candidates = await _dbService.Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.Type)
            .Include(e => e.CurrentLocation)
            .Where(e => e.IsActive && e.EquipmentId != equipmentId)
            .AsNoTracking()
            .ToListAsync();
        
        var results = new List<SimilarEquipmentItem>();
        
        foreach (var candidate in candidates)
        {
            var score = CalculateItemSimilarity(source, candidate);
            if (score > 0.3) // At least 30% similar
            {
                results.Add(new SimilarEquipmentItem
                {
                    Equipment = candidate,
                    SimilarityScore = score,
                    MatchReasons = GetMatchReasons(source, candidate)
                });
            }
        }
        
        return results
            .OrderByDescending(r => r.SimilarityScore)
            .Take(maxResults)
            .ToList();
    }
    
    #endregion
    
    #region Comparison Building
    
    private List<ComparisonRow> BuildComparisonRows(List<Equipment> equipment)
    {
        var rows = new List<ComparisonRow>();
        
        // Basic Info
        rows.Add(CreateRow("Asset Number", equipment.Select(e => e.AssetNumber).ToArray()));
        rows.Add(CreateRow("Unique ID", equipment.Select(e => e.UniqueId).ToArray()));
        rows.Add(CreateRow("Serial Number", equipment.Select(e => e.SerialNumber).ToArray()));
        rows.Add(CreateRow("Status", equipment.Select(e => e.Status.ToString()).ToArray()));
        rows.Add(CreateRow("Condition", equipment.Select(e => e.Condition.ToString()).ToArray()));
        
        // Classification
        rows.Add(CreateRow("Category", equipment.Select(e => e.Category?.Name).ToArray(), "Classification"));
        rows.Add(CreateRow("Type", equipment.Select(e => e.Type?.Name).ToArray(), "Classification"));
        rows.Add(CreateRow("Manufacturer", equipment.Select(e => e.Manufacturer).ToArray(), "Classification"));
        rows.Add(CreateRow("Model", equipment.Select(e => e.Model).ToArray(), "Classification"));
        rows.Add(CreateRow("Part Number", equipment.Select(e => e.PartNumber).ToArray(), "Classification"));
        
        // Location
        rows.Add(CreateRow("Location", equipment.Select(e => e.CurrentLocation?.Name).ToArray(), "Location"));
        
        // Physical
        rows.Add(CreateRow("Weight (kg)", equipment.Select(e => e.WeightKg?.ToString("F2")).ToArray(), "Physical"));
        rows.Add(CreateRow("Length (cm)", equipment.Select(e => e.LengthCm?.ToString("F1")).ToArray(), "Physical"));
        rows.Add(CreateRow("Width (cm)", equipment.Select(e => e.WidthCm?.ToString("F1")).ToArray(), "Physical"));
        rows.Add(CreateRow("Height (cm)", equipment.Select(e => e.HeightCm?.ToString("F1")).ToArray(), "Physical"));
        
        // Certification
        rows.Add(CreateRow("Requires Cert", equipment.Select(e => e.RequiresCertification ? "Yes" : "No").ToArray(), "Certification"));
        rows.Add(CreateRow("Cert Number", equipment.Select(e => e.CertificationNumber).ToArray(), "Certification"));
        rows.Add(CreateRow("Cert Expiry", equipment.Select(e => e.CertificationExpiryDate?.ToString("d")).ToArray(), "Certification"));
        
        // Calibration
        rows.Add(CreateRow("Requires Cal", equipment.Select(e => e.RequiresCalibration ? "Yes" : "No").ToArray(), "Calibration"));
        rows.Add(CreateRow("Last Cal Date", equipment.Select(e => e.LastCalibrationDate?.ToString("d")).ToArray(), "Calibration"));
        rows.Add(CreateRow("Next Cal Date", equipment.Select(e => e.NextCalibrationDate?.ToString("d")).ToArray(), "Calibration"));
        rows.Add(CreateRow("Cal Interval", equipment.Select(e => e.CalibrationIntervalDays?.ToString()).ToArray(), "Calibration"));
        
        // Purchase
        rows.Add(CreateRow("Purchase Price", equipment.Select(e => e.PurchasePrice?.ToString("C")).ToArray(), "Purchase"));
        rows.Add(CreateRow("Purchase Date", equipment.Select(e => e.PurchaseDate?.ToString("d")).ToArray(), "Purchase"));
        rows.Add(CreateRow("Supplier", equipment.Select(e => e.Supplier?.Name).ToArray(), "Purchase"));
        rows.Add(CreateRow("Warranty Expiry", equipment.Select(e => e.WarrantyExpiryDate?.ToString("d")).ToArray(), "Purchase"));
        
        return rows;
    }
    
    private ComparisonRow CreateRow(string label, string?[] values, string category = "Basic")
    {
        var row = new ComparisonRow
        {
            Label = label,
            Category = category,
            Values = values.Select(v => v ?? "-").ToList()
        };
        
        // Determine if all values are the same
        var nonEmpty = values.Where(v => !string.IsNullOrEmpty(v)).ToList();
        row.AllSame = nonEmpty.Count > 0 && nonEmpty.Distinct().Count() == 1;
        row.AllDifferent = nonEmpty.Count > 1 && nonEmpty.Distinct().Count() == nonEmpty.Count;
        
        return row;
    }
    
    #endregion
    
    #region Similarity Calculation
    
    private static double CalculateSimilarity(List<ComparisonRow> rows)
    {
        if (rows.Count == 0) return 0;
        
        int sameCount = rows.Count(r => r.AllSame);
        return (double)sameCount / rows.Count;
    }
    
    private static double CalculateItemSimilarity(Equipment source, Equipment candidate)
    {
        double score = 0;
        int factors = 0;
        
        // Category match (high weight)
        if (source.CategoryId == candidate.CategoryId)
        {
            score += 0.3;
        }
        factors++;
        
        // Type match
        if (source.TypeId == candidate.TypeId)
        {
            score += 0.2;
        }
        factors++;
        
        // Manufacturer match
        if (!string.IsNullOrEmpty(source.Manufacturer) && 
            source.Manufacturer.Equals(candidate.Manufacturer, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.2;
        }
        factors++;
        
        // Model match
        if (!string.IsNullOrEmpty(source.Model) && 
            source.Model.Equals(candidate.Model, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.15;
        }
        factors++;
        
        // Same certification requirements
        if (source.RequiresCertification == candidate.RequiresCertification)
        {
            score += 0.075;
        }
        factors++;
        
        // Same calibration requirements
        if (source.RequiresCalibration == candidate.RequiresCalibration)
        {
            score += 0.075;
        }
        factors++;
        
        return score;
    }
    
    private static List<string> GetMatchReasons(Equipment source, Equipment candidate)
    {
        var reasons = new List<string>();
        
        if (source.CategoryId == candidate.CategoryId)
            reasons.Add("Same category");
        
        if (source.TypeId == candidate.TypeId)
            reasons.Add("Same type");
        
        if (!string.IsNullOrEmpty(source.Manufacturer) && 
            source.Manufacturer.Equals(candidate.Manufacturer, StringComparison.OrdinalIgnoreCase))
            reasons.Add("Same manufacturer");
        
        if (!string.IsNullOrEmpty(source.Model) && 
            source.Model.Equals(candidate.Model, StringComparison.OrdinalIgnoreCase))
            reasons.Add("Same model");
        
        return reasons;
    }
    
    #endregion
    
    #region Export
    
    /// <summary>
    /// Export comparison to text format
    /// </summary>
    public string ExportComparisonToText(ComparisonResult result)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("EQUIPMENT COMPARISON");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();
        
        // Header
        sb.Append("Field".PadRight(20));
        foreach (var item in result.Items)
        {
            sb.Append(item.AssetNumber.PadRight(20));
        }
        sb.AppendLine();
        sb.AppendLine(new string('-', 20 + result.Items.Count * 20));
        
        // Rows
        string currentCategory = "";
        foreach (var row in result.Rows)
        {
            if (row.Category != currentCategory)
            {
                currentCategory = row.Category;
                sb.AppendLine();
                sb.AppendLine($"--- {currentCategory} ---");
            }
            
            sb.Append(row.Label.PadRight(20));
            foreach (var value in row.Values)
            {
                var display = value.Length > 18 ? value.Substring(0, 15) + "..." : value;
                sb.Append(display.PadRight(20));
            }
            
            if (row.AllSame && row.Values.Any(v => v != "-"))
                sb.Append(" [SAME]");
            else if (row.AllDifferent)
                sb.Append(" [DIFFERENT]");
            
            sb.AppendLine();
        }
        
        sb.AppendLine();
        sb.AppendLine($"Similarity Score: {result.SimilarityScore:P0}");
        
        return sb.ToString();
    }
    
    #endregion
}

#region Models

public class ComparisonResult
{
    public List<ComparisonItem> Items { get; set; } = new();
    public List<ComparisonRow> Rows { get; set; } = new();
    public double SimilarityScore { get; set; }
    
    public string SimilarityDisplay => $"{SimilarityScore:P0} Similar";
}

public class ComparisonItem
{
    public Guid EquipmentId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}

public class ComparisonRow
{
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
    public bool AllSame { get; set; }
    public bool AllDifferent { get; set; }
    
    public string RowStyle => AllSame ? "Same" : AllDifferent ? "Different" : "Mixed";
}

public class SimilarEquipmentItem
{
    public Equipment Equipment { get; set; } = null!;
    public double SimilarityScore { get; set; }
    public List<string> MatchReasons { get; set; } = new();
    
    public string ScoreDisplay => $"{SimilarityScore:P0}";
}

#endregion
