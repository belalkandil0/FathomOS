// LicensingSystem.Server/Services/HealthMonitorService.cs
// Server health monitoring and metrics collection

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using System.Diagnostics;

namespace LicensingSystem.Server.Services;

public interface IHealthMonitorService
{
    Task RecordResponseTimeAsync(string endpoint, double milliseconds);
    Task RecordErrorAsync(string endpoint, string errorMessage);
    Task RecordActivationFailureAsync(string licenseId, string reason);
    Task<HealthDashboard> GetDashboardAsync();
    Task<List<ServerHealthMetricRecord>> GetMetricsAsync(string metricType, DateTime from, DateTime to);
    Task<List<HealthAlert>> GetActiveAlertsAsync();
    Task CleanupOldMetricsAsync(int daysToKeep = 30);
}

public class HealthMonitorService : IHealthMonitorService
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<HealthMonitorService> _logger;
    
    // Alert thresholds
    private const double ResponseTimeAlertMs = 1000; // 1 second
    private const int ActivationFailuresPerHourAlert = 10;
    private const int ErrorsPerHourAlert = 20;

    public HealthMonitorService(LicenseDbContext db, ILogger<HealthMonitorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordResponseTimeAsync(string endpoint, double milliseconds)
    {
        var metric = new ServerHealthMetricRecord
        {
            Timestamp = DateTime.UtcNow,
            MetricType = "ResponseTime",
            Endpoint = endpoint,
            Value = milliseconds,
            IsAlert = milliseconds > ResponseTimeAlertMs
        };

        _db.ServerHealthMetrics.Add(metric);
        await _db.SaveChangesAsync();

        if (metric.IsAlert)
        {
            _logger.LogWarning("Slow response on {Endpoint}: {Ms}ms", endpoint, milliseconds);
        }
    }

    public async Task RecordErrorAsync(string endpoint, string errorMessage)
    {
        var metric = new ServerHealthMetricRecord
        {
            Timestamp = DateTime.UtcNow,
            MetricType = "Error",
            Endpoint = endpoint,
            Value = 1,
            IsAlert = true,
            Details = errorMessage
        };

        _db.ServerHealthMetrics.Add(metric);
        await _db.SaveChangesAsync();

        _logger.LogError("Error recorded for {Endpoint}: {Error}", endpoint, errorMessage);
    }

    public async Task RecordActivationFailureAsync(string licenseId, string reason)
    {
        var metric = new ServerHealthMetricRecord
        {
            Timestamp = DateTime.UtcNow,
            MetricType = "ActivationFailure",
            Value = 1,
            Details = $"{licenseId}: {reason}"
        };

        _db.ServerHealthMetrics.Add(metric);
        await _db.SaveChangesAsync();

        // Check for spike
        var failuresLastHour = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "ActivationFailure" && 
                            m.Timestamp > DateTime.UtcNow.AddHours(-1));

        if (failuresLastHour >= ActivationFailuresPerHourAlert)
        {
            metric.IsAlert = true;
            await _db.SaveChangesAsync();
            
            _logger.LogWarning("Activation failure spike detected: {Count} in last hour", failuresLastHour);
        }
    }

    public async Task<HealthDashboard> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var hourAgo = now.AddHours(-1);
        var dayAgo = now.AddDays(-1);

        // Response time stats (last hour)
        var recentResponseTimes = await _db.ServerHealthMetrics
            .Where(m => m.MetricType == "ResponseTime" && m.Timestamp > hourAgo)
            .ToListAsync();

        var avgResponseTime = recentResponseTimes.Count > 0 
            ? recentResponseTimes.Average(m => m.Value) 
            : 0;

        var maxResponseTime = recentResponseTimes.Count > 0 
            ? recentResponseTimes.Max(m => m.Value) 
            : 0;

        // Error counts
        var errorsLastHour = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "Error" && m.Timestamp > hourAgo);

        var errorsLastDay = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "Error" && m.Timestamp > dayAgo);

        // Activation failures
        var activationFailuresLastHour = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "ActivationFailure" && m.Timestamp > hourAgo);

        var activationFailuresLastDay = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "ActivationFailure" && m.Timestamp > dayAgo);

        // Active sessions
        var activeSessions = await _db.ActiveSessions
            .CountAsync(s => s.IsActive);

        // License stats
        var totalLicenses = await _db.LicenseKeys.CountAsync();
        var activeLicenses = await _db.LicenseKeys
            .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now);
        var expiringThisWeek = await _db.LicenseKeys
            .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt < now.AddDays(7));

        // Recent activations
        var activationsLastDay = await _db.LicenseActivations
            .CountAsync(a => a.ActivatedAt > dayAgo);

        // Uptime (based on first metric recorded)
        var firstMetric = await _db.ServerHealthMetrics
            .OrderBy(m => m.Timestamp)
            .FirstOrDefaultAsync();
        var uptimeHours = firstMetric != null 
            ? (now - firstMetric.Timestamp).TotalHours 
            : 0;

        return new HealthDashboard
        {
            Timestamp = now,
            ServerStatus = errorsLastHour < ErrorsPerHourAlert ? "Healthy" : "Degraded",
            UptimeHours = uptimeHours,
            
            ResponseTimeMs = new ResponseTimeStats
            {
                Average = avgResponseTime,
                Max = maxResponseTime,
                RequestCount = recentResponseTimes.Count
            },
            
            Errors = new ErrorStats
            {
                LastHour = errorsLastHour,
                Last24Hours = errorsLastDay
            },
            
            ActivationFailures = new ActivationFailureStats
            {
                LastHour = activationFailuresLastHour,
                Last24Hours = activationFailuresLastDay
            },
            
            Sessions = new SessionStats
            {
                Active = activeSessions
            },
            
            Licenses = new LicenseStats
            {
                Total = totalLicenses,
                Active = activeLicenses,
                ExpiringThisWeek = expiringThisWeek,
                ActivationsLast24Hours = activationsLastDay
            },
            
            Alerts = await GetActiveAlertsAsync()
        };
    }

    public async Task<List<ServerHealthMetricRecord>> GetMetricsAsync(string metricType, DateTime from, DateTime to)
    {
        return await _db.ServerHealthMetrics
            .Where(m => m.MetricType == metricType && m.Timestamp >= from && m.Timestamp <= to)
            .OrderByDescending(m => m.Timestamp)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<List<HealthAlert>> GetActiveAlertsAsync()
    {
        var alerts = new List<HealthAlert>();
        var now = DateTime.UtcNow;
        var hourAgo = now.AddHours(-1);

        // Check for high response times
        var slowResponses = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "ResponseTime" && 
                            m.IsAlert && 
                            m.Timestamp > hourAgo);

        if (slowResponses > 5)
        {
            alerts.Add(new HealthAlert
            {
                Severity = "Warning",
                Title = "Slow Response Times",
                Message = $"{slowResponses} slow responses in the last hour",
                Timestamp = now
            });
        }

        // Check for errors
        var errors = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "Error" && m.Timestamp > hourAgo);

        if (errors >= ErrorsPerHourAlert)
        {
            alerts.Add(new HealthAlert
            {
                Severity = "Critical",
                Title = "High Error Rate",
                Message = $"{errors} errors in the last hour",
                Timestamp = now
            });
        }

        // Check for activation failures
        var failures = await _db.ServerHealthMetrics
            .CountAsync(m => m.MetricType == "ActivationFailure" && m.Timestamp > hourAgo);

        if (failures >= ActivationFailuresPerHourAlert)
        {
            alerts.Add(new HealthAlert
            {
                Severity = "Warning",
                Title = "Activation Failure Spike",
                Message = $"{failures} activation failures in the last hour",
                Timestamp = now
            });
        }

        // Check for expiring licenses
        var expiringToday = await _db.LicenseKeys
            .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt < now.AddDays(1));

        if (expiringToday > 0)
        {
            alerts.Add(new HealthAlert
            {
                Severity = "Info",
                Title = "Licenses Expiring Today",
                Message = $"{expiringToday} licenses expiring today",
                Timestamp = now
            });
        }

        return alerts;
    }

    public async Task CleanupOldMetricsAsync(int daysToKeep = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);

        var oldMetrics = await _db.ServerHealthMetrics
            .Where(m => m.Timestamp < cutoff)
            .ToListAsync();

        _db.ServerHealthMetrics.RemoveRange(oldMetrics);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old health metrics", oldMetrics.Count);
    }
}

// Dashboard models
public class HealthDashboard
{
    public DateTime Timestamp { get; set; }
    public string ServerStatus { get; set; } = "Unknown";
    public double UptimeHours { get; set; }
    public ResponseTimeStats ResponseTimeMs { get; set; } = new();
    public ErrorStats Errors { get; set; } = new();
    public ActivationFailureStats ActivationFailures { get; set; } = new();
    public SessionStats Sessions { get; set; } = new();
    public LicenseStats Licenses { get; set; } = new();
    public List<HealthAlert> Alerts { get; set; } = new();
}

public class ResponseTimeStats
{
    public double Average { get; set; }
    public double Max { get; set; }
    public int RequestCount { get; set; }
}

public class ErrorStats
{
    public int LastHour { get; set; }
    public int Last24Hours { get; set; }
}

public class ActivationFailureStats
{
    public int LastHour { get; set; }
    public int Last24Hours { get; set; }
}

public class SessionStats
{
    public int Active { get; set; }
}

public class LicenseStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int ExpiringThisWeek { get; set; }
    public int ActivationsLast24Hours { get; set; }
}

public class HealthAlert
{
    public string Severity { get; set; } = "Info"; // Info, Warning, Critical
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
