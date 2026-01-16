using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for generating dashboard statistics and chart data.
/// Provides real-time analytics for equipment, certifications, and inventory.
/// </summary>
public class DashboardService
{
    private readonly LocalDatabaseService _dbService;
    
    public DashboardService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
    }
    
    #region Summary Statistics
    
    /// <summary>
    /// Get overall dashboard summary
    /// </summary>
    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        var summary = new DashboardSummary();
        
        var equipment = await _dbService.Context.Equipment
            .Where(e => e.IsActive)
            .AsNoTracking()
            .ToListAsync();
        
        summary.TotalEquipment = equipment.Count;
        summary.AvailableEquipment = equipment.Count(e => e.Status == EquipmentStatus.Available);
        summary.InUseEquipment = equipment.Count(e => e.Status == EquipmentStatus.InUse);
        summary.InTransitEquipment = equipment.Count(e => e.Status == EquipmentStatus.InTransit);
        summary.UnderRepairEquipment = equipment.Count(e => e.Status == EquipmentStatus.UnderRepair);
        summary.RetiredEquipment = equipment.Count(e => e.Status == EquipmentStatus.Retired);
        
        // Certifications
        var today = DateTime.Today;
        var thirtyDaysFromNow = today.AddDays(30);
        
        summary.CertificationsExpiringSoon = equipment.Count(e => 
            e.RequiresCertification && 
            e.CertificationExpiryDate.HasValue && 
            e.CertificationExpiryDate.Value <= thirtyDaysFromNow &&
            e.CertificationExpiryDate.Value >= today);
        
        summary.CertificationsExpired = equipment.Count(e => 
            e.RequiresCertification && 
            e.CertificationExpiryDate.HasValue && 
            e.CertificationExpiryDate.Value < today);
        
        // Calibrations
        var sevenDaysFromNow = today.AddDays(7);
        
        summary.CalibrationsDueSoon = equipment.Count(e => 
            e.RequiresCalibration && 
            e.NextCalibrationDate.HasValue && 
            e.NextCalibrationDate.Value <= sevenDaysFromNow &&
            e.NextCalibrationDate.Value >= today);
        
        summary.CalibrationsOverdue = equipment.Count(e => 
            e.RequiresCalibration && 
            e.NextCalibrationDate.HasValue && 
            e.NextCalibrationDate.Value < today);
        
        // Manifests
        summary.PendingManifests = await _dbService.Context.Manifests
            .CountAsync(m => m.Status == ManifestStatus.Submitted || m.Status == ManifestStatus.Approved);
        
        summary.InTransitManifests = await _dbService.Context.Manifests
            .CountAsync(m => m.Status == ManifestStatus.Shipped);
        
        // Locations
        summary.TotalLocations = await _dbService.Context.Locations.CountAsync(l => l.IsActive);
        
        // Value (if tracked)
        summary.TotalValue = equipment.Sum(e => e.PurchasePrice ?? 0);
        
        return summary;
    }
    
    #endregion
    
    #region Chart Data
    
    /// <summary>
    /// Get equipment count by status for pie/donut chart
    /// </summary>
    public async Task<List<ChartDataPoint>> GetEquipmentByStatusAsync()
    {
        var data = await _dbService.Context.Equipment
            .Where(e => e.IsActive)
            .GroupBy(e => e.Status)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString(),
                Value = g.Count(),
                Color = GetStatusColor(g.Key)
            })
            .AsNoTracking()
            .ToListAsync();
        
        return data.OrderByDescending(d => d.Value).ToList();
    }
    
    /// <summary>
    /// Get equipment count by category for bar chart
    /// </summary>
    public async Task<List<ChartDataPoint>> GetEquipmentByCategoryAsync()
    {
        var data = await _dbService.Context.Equipment
            .Where(e => e.IsActive && e.CategoryId.HasValue)
            .GroupBy(e => e.Category!.Name)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key ?? "Uncategorized",
                Value = g.Count()
            })
            .AsNoTracking()
            .ToListAsync();
        
        return data.OrderByDescending(d => d.Value).ToList();
    }
    
    /// <summary>
    /// Get equipment count by location for bar chart
    /// </summary>
    public async Task<List<ChartDataPoint>> GetEquipmentByLocationAsync(int topN = 10)
    {
        var data = await _dbService.Context.Equipment
            .Where(e => e.IsActive && e.CurrentLocationId.HasValue)
            .GroupBy(e => e.CurrentLocation!.Name)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key ?? "Unknown",
                Value = g.Count()
            })
            .OrderByDescending(g => g.Value)
            .Take(topN)
            .AsNoTracking()
            .ToListAsync();
        
        return data;
    }
    
    /// <summary>
    /// Get certifications expiring by month for timeline chart
    /// </summary>
    public async Task<List<ChartDataPoint>> GetCertificationExpiryTimelineAsync(int months = 6)
    {
        var startDate = DateTime.Today;
        var endDate = startDate.AddMonths(months);
        
        var equipment = await _dbService.Context.Equipment
            .Where(e => e.IsActive && 
                       e.RequiresCertification && 
                       e.CertificationExpiryDate.HasValue &&
                       e.CertificationExpiryDate.Value >= startDate &&
                       e.CertificationExpiryDate.Value <= endDate)
            .AsNoTracking()
            .ToListAsync();
        
        var data = new List<ChartDataPoint>();
        for (int i = 0; i < months; i++)
        {
            var monthStart = startDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var count = equipment.Count(e => 
                e.CertificationExpiryDate >= monthStart && 
                e.CertificationExpiryDate < monthEnd);
            
            data.Add(new ChartDataPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = count,
                Color = count > 5 ? "#E53935" : count > 2 ? "#FF9800" : "#4CAF50"
            });
        }
        
        return data;
    }
    
    /// <summary>
    /// Get equipment value by category for pie chart
    /// </summary>
    public async Task<List<ChartDataPoint>> GetValueByCategoryAsync()
    {
        var data = await _dbService.Context.Equipment
            .Where(e => e.IsActive && e.PurchasePrice.HasValue && e.CategoryId.HasValue)
            .GroupBy(e => e.Category!.Name)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key ?? "Uncategorized",
                Value = (double)g.Sum(e => e.PurchasePrice ?? 0)
            })
            .AsNoTracking()
            .ToListAsync();
        
        return data.OrderByDescending(d => d.Value).ToList();
    }
    
    /// <summary>
    /// Get recent activity for activity feed
    /// </summary>
    public async Task<List<ActivityItem>> GetRecentActivityAsync(int count = 20)
    {
        var activities = new List<ActivityItem>();
        
        // Recent equipment changes
        var recentEquipment = await _dbService.Context.Equipment
            .Where(e => e.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(e => e.UpdatedAt)
            .Take(count / 2)
            .AsNoTracking()
            .ToListAsync();
        
        foreach (var eq in recentEquipment)
        {
            activities.Add(new ActivityItem
            {
                Type = "Equipment",
                Title = eq.Name ?? eq.AssetNumber,
                Description = eq.CreatedAt == eq.UpdatedAt ? "Created" : "Updated",
                Timestamp = eq.UpdatedAt,
                Icon = "Package"
            });
        }
        
        // Recent manifests
        var recentManifests = await _dbService.Context.Manifests
            .Where(m => m.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(m => m.UpdatedAt)
            .Take(count / 2)
            .AsNoTracking()
            .ToListAsync();
        
        foreach (var m in recentManifests)
        {
            activities.Add(new ActivityItem
            {
                Type = "Manifest",
                Title = m.ManifestNumber,
                Description = $"Status: {m.Status}",
                Timestamp = m.UpdatedAt,
                Icon = "FileDocument"
            });
        }
        
        return activities.OrderByDescending(a => a.Timestamp).Take(count).ToList();
    }
    
    /// <summary>
    /// Get equipment requiring attention (alerts)
    /// </summary>
    public async Task<List<AlertItem>> GetEquipmentAlertsAsync()
    {
        var alerts = new List<AlertItem>();
        var today = DateTime.Today;
        
        var equipment = await _dbService.Context.Equipment
            .Where(e => e.IsActive)
            .Include(e => e.CurrentLocation)
            .AsNoTracking()
            .ToListAsync();
        
        // Expired certifications
        var expiredCerts = equipment.Where(e => 
            e.RequiresCertification && 
            e.CertificationExpiryDate.HasValue && 
            e.CertificationExpiryDate < today);
        
        foreach (var eq in expiredCerts)
        {
            alerts.Add(new AlertItem
            {
                EquipmentId = eq.EquipmentId,
                AssetNumber = eq.AssetNumber,
                EquipmentName = eq.Name ?? "Unknown",
                AlertType = AlertType.CertificationExpired,
                Message = $"Certification expired {(today - eq.CertificationExpiryDate!.Value).Days} days ago",
                Severity = AlertSeverity.High,
                Location = eq.CurrentLocation?.Name
            });
        }
        
        // Expiring soon (30 days)
        var expiringSoon = equipment.Where(e => 
            e.RequiresCertification && 
            e.CertificationExpiryDate.HasValue && 
            e.CertificationExpiryDate >= today &&
            e.CertificationExpiryDate <= today.AddDays(30));
        
        foreach (var eq in expiringSoon)
        {
            alerts.Add(new AlertItem
            {
                EquipmentId = eq.EquipmentId,
                AssetNumber = eq.AssetNumber,
                EquipmentName = eq.Name ?? "Unknown",
                AlertType = AlertType.CertificationExpiring,
                Message = $"Certification expires in {(eq.CertificationExpiryDate!.Value - today).Days} days",
                Severity = (eq.CertificationExpiryDate.Value - today).Days <= 7 
                    ? AlertSeverity.High : AlertSeverity.Medium,
                Location = eq.CurrentLocation?.Name
            });
        }
        
        // Overdue calibrations
        var overdueCal = equipment.Where(e => 
            e.RequiresCalibration && 
            e.NextCalibrationDate.HasValue && 
            e.NextCalibrationDate < today);
        
        foreach (var eq in overdueCal)
        {
            alerts.Add(new AlertItem
            {
                EquipmentId = eq.EquipmentId,
                AssetNumber = eq.AssetNumber,
                EquipmentName = eq.Name ?? "Unknown",
                AlertType = AlertType.CalibrationOverdue,
                Message = $"Calibration overdue by {(today - eq.NextCalibrationDate!.Value).Days} days",
                Severity = AlertSeverity.High,
                Location = eq.CurrentLocation?.Name
            });
        }
        
        return alerts.OrderByDescending(a => a.Severity).ThenBy(a => a.AssetNumber).ToList();
    }
    
    #endregion
    
    #region Helpers
    
    private static string GetStatusColor(EquipmentStatus status) => status switch
    {
        EquipmentStatus.Available => "#4CAF50",
        EquipmentStatus.InUse => "#2196F3",
        EquipmentStatus.InTransit => "#FF9800",
        EquipmentStatus.UnderRepair => "#9C27B0",
        EquipmentStatus.Reserved => "#00BCD4",
        EquipmentStatus.Retired => "#9E9E9E",
        EquipmentStatus.Lost => "#F44336",
        EquipmentStatus.Disposed => "#795548",
        _ => "#607D8B"
    };
    
    #endregion
}

#region Models

public class DashboardSummary
{
    public int TotalEquipment { get; set; }
    public int AvailableEquipment { get; set; }
    public int InUseEquipment { get; set; }
    public int InTransitEquipment { get; set; }
    public int UnderRepairEquipment { get; set; }
    public int RetiredEquipment { get; set; }
    
    public int CertificationsExpiringSoon { get; set; }
    public int CertificationsExpired { get; set; }
    public int CalibrationsDueSoon { get; set; }
    public int CalibrationsOverdue { get; set; }
    
    public int PendingManifests { get; set; }
    public int InTransitManifests { get; set; }
    
    public int TotalLocations { get; set; }
    public decimal TotalValue { get; set; }
    
    public int TotalAlerts => CertificationsExpired + CalibrationsOverdue;
    public int TotalWarnings => CertificationsExpiringSoon + CalibrationsDueSoon;
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Color { get; set; }
}

public class ActivityItem
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = string.Empty;
    
    public string TimeAgo
    {
        get
        {
            var span = DateTime.UtcNow - Timestamp;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return Timestamp.ToString("MMM d");
        }
    }
}

public class AlertItem
{
    public Guid EquipmentId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string? Location { get; set; }
    
    public string SeverityColor => Severity switch
    {
        AlertSeverity.High => "#E53935",
        AlertSeverity.Medium => "#FF9800",
        AlertSeverity.Low => "#4CAF50",
        _ => "#9E9E9E"
    };
}

public enum AlertType
{
    CertificationExpired,
    CertificationExpiring,
    CalibrationOverdue,
    CalibrationDue,
    MaintenanceDue,
    LowStock
}

public enum AlertSeverity
{
    Low,
    Medium,
    High
}

#endregion
