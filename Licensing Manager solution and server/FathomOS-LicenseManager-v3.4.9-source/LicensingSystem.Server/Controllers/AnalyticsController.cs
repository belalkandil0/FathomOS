// LicensingSystem.Server/Controllers/AnalyticsController.cs
// Analytics endpoints for license statistics and usage tracking
// Provides insights into license usage, activation patterns, and system health

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;

namespace LicensingSystem.Server.Controllers;

/// <summary>
/// Controller for analytics and statistics endpoints.
/// Provides license usage data, trends, and operational insights.
///
/// PROTECTED ENDPOINTS (require X-API-Key header):
///   - GET /api/analytics/licenses/stats - Overall license statistics
///   - GET /api/analytics/usage/{licenseId} - Usage stats for a specific license
///   - GET /api/analytics/activations - Activation trends
///   - GET /api/analytics/expiring - Licenses expiring soon
///   - GET /api/analytics/certificates - Certificate analytics
///   - GET /api/analytics/dashboard - Complete analytics dashboard
/// </summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<AnalyticsController> _logger;
    private readonly IHealthMonitorService _healthMonitor;

    public AnalyticsController(
        LicenseDbContext db,
        ILogger<AnalyticsController> logger,
        IHealthMonitorService healthMonitor)
    {
        _db = db;
        _logger = logger;
        _healthMonitor = healthMonitor;
    }

    /// <summary>
    /// Get overall license statistics.
    /// GET /api/analytics/licenses/stats
    /// Returns aggregate statistics about all licenses.
    /// </summary>
    [HttpGet("licenses/stats")]
    public async Task<ActionResult<LicenseStatsResponse>> GetLicenseStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var sevenDaysAgo = now.AddDays(-7);
            var oneYearAgo = now.AddYears(-1);

            // Total counts
            var totalLicenses = await _db.LicenseKeys.CountAsync();
            var syncedLicenses = await _db.SyncedLicenses.CountAsync();
            var totalActivations = await _db.LicenseActivations.CountAsync();
            var totalCertificates = await _db.Certificates.CountAsync();

            // License status breakdown
            var activeLicenses = await _db.LicenseKeys
                .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now);
            var expiredLicenses = await _db.LicenseKeys
                .CountAsync(l => !l.IsRevoked && l.ExpiresAt <= now);
            var revokedLicenses = await _db.LicenseKeys
                .CountAsync(l => l.IsRevoked);

            // Expiring soon
            var expiringThisWeek = await _db.LicenseKeys
                .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= now.AddDays(7));
            var expiringThisMonth = await _db.LicenseKeys
                .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= now.AddDays(30));

            // Recent activity
            var activationsLast30Days = await _db.LicenseActivations
                .CountAsync(a => a.ActivatedAt >= thirtyDaysAgo);
            var activationsLast7Days = await _db.LicenseActivations
                .CountAsync(a => a.ActivatedAt >= sevenDaysAgo);

            // License types
            var onlineLicenses = await _db.LicenseKeys
                .CountAsync(l => l.LicenseType == "Online");
            var offlineLicenses = await _db.LicenseKeys
                .CountAsync(l => l.LicenseType == "Offline" || l.IsOfflineGenerated);

            // Edition breakdown
            var byEdition = await _db.LicenseKeys
                .GroupBy(l => l.Edition)
                .Select(g => new EditionStats
                {
                    Edition = g.Key ?? "Unknown",
                    Count = g.Count(),
                    Active = g.Count(l => !l.IsRevoked && l.ExpiresAt > now)
                })
                .ToListAsync();

            // Subscription type breakdown
            var bySubscription = await _db.LicenseKeys
                .GroupBy(l => l.SubscriptionType)
                .Select(g => new SubscriptionStats
                {
                    Type = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            // Monthly trends (last 12 months)
            var monthlyLicenses = await _db.LicenseKeys
                .Where(l => l.CreatedAt >= oneYearAgo)
                .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
                .Select(g => new MonthlyTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            return Ok(new LicenseStatsResponse
            {
                Timestamp = now,
                Summary = new LicenseSummary
                {
                    TotalLicenses = totalLicenses,
                    SyncedLicenses = syncedLicenses,
                    TotalActivations = totalActivations,
                    TotalCertificates = totalCertificates
                },
                Status = new LicenseStatusBreakdown
                {
                    Active = activeLicenses,
                    Expired = expiredLicenses,
                    Revoked = revokedLicenses
                },
                Expiring = new ExpiringLicenses
                {
                    ThisWeek = expiringThisWeek,
                    ThisMonth = expiringThisMonth
                },
                RecentActivity = new RecentActivity
                {
                    ActivationsLast7Days = activationsLast7Days,
                    ActivationsLast30Days = activationsLast30Days
                },
                LicenseTypes = new LicenseTypeBreakdown
                {
                    Online = onlineLicenses,
                    Offline = offlineLicenses
                },
                ByEdition = byEdition,
                BySubscription = bySubscription,
                MonthlyTrend = monthlyLicenses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get license statistics");
            return StatusCode(500, new { error = "Failed to retrieve license statistics", message = ex.Message });
        }
    }

    /// <summary>
    /// Get usage statistics for a specific license.
    /// GET /api/analytics/usage/{licenseId}
    /// Returns detailed usage information for a license.
    /// </summary>
    [HttpGet("usage/{licenseId}")]
    public async Task<ActionResult<LicenseUsageResponse>> GetUsageStats(string licenseId)
    {
        try
        {
            // Find in both LicenseKeys and SyncedLicenses
            var license = await _db.LicenseKeys
                .Include(l => l.Activations)
                .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

            SyncedLicenseRecord? syncedLicense = null;
            if (license == null)
            {
                syncedLicense = await _db.SyncedLicenses
                    .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

                if (syncedLicense == null)
                {
                    return NotFound(new { message = "License not found", licenseId });
                }
            }

            // Get activation history
            var activations = license != null
                ? license.Activations.OrderByDescending(a => a.ActivatedAt).ToList()
                : new List<LicenseActivationRecord>();

            // Get active sessions
            var activeSessions = await _db.ActiveSessions
                .Where(s => s.LicenseId == licenseId && s.IsActive)
                .ToListAsync();

            // Get certificates issued under this license
            var certificates = await _db.Certificates
                .Where(c => c.LicenseId == licenseId)
                .OrderByDescending(c => c.IssuedAt)
                .Take(50)
                .ToListAsync();

            // Get heartbeat history (from active sessions)
            var sessionHistory = await _db.ActiveSessions
                .Where(s => s.LicenseId == licenseId)
                .OrderByDescending(s => s.StartedAt)
                .Take(20)
                .ToListAsync();

            // Calculate usage metrics
            var totalActiveHours = 0.0;
            foreach (var session in sessionHistory)
            {
                var endTime = session.EndedAt ?? session.LastHeartbeat;
                totalActiveHours += (endTime - session.StartedAt).TotalHours;
            }

            if (license != null)
            {
                return Ok(new LicenseUsageResponse
                {
                    LicenseId = license.LicenseId,
                    Edition = license.Edition,
                    Status = license.IsRevoked ? "Revoked" : (license.ExpiresAt < DateTime.UtcNow ? "Expired" : "Active"),
                    CreatedAt = license.CreatedAt,
                    ExpiresAt = license.ExpiresAt,
                    CustomerEmail = MaskEmail(license.CustomerEmail),
                    CustomerName = license.CustomerName,
                    Brand = license.Brand,
                    LicenseeCode = license.LicenseeCode,

                    Usage = new UsageMetrics
                    {
                        TotalActivations = activations.Count,
                        ActiveActivations = activations.Count(a => !a.IsDeactivated),
                        TotalSessions = sessionHistory.Count,
                        ActiveSessions = activeSessions.Count,
                        TotalActiveHours = Math.Round(totalActiveHours, 2),
                        CertificatesIssued = certificates.Count,
                        LastSeenAt = activations.FirstOrDefault()?.LastSeenAt,
                        LastActiveSession = sessionHistory.FirstOrDefault()?.StartedAt
                    },

                    Activations = activations.Take(10).Select(a => new LicenseActivationInfo
                    {
                        HardwareFingerprint = MaskFingerprint(a.HardwareFingerprint),
                        MachineName = a.MachineName,
                        ActivatedAt = a.ActivatedAt,
                        LastSeenAt = a.LastSeenAt,
                        IsDeactivated = a.IsDeactivated,
                        AppVersion = a.AppVersion,
                        OsVersion = a.OsVersion
                    }).ToList(),

                    RecentCertificates = certificates.Take(5).Select(c => new RecentCertificate
                    {
                        CertificateId = c.CertificateId,
                        ModuleId = c.ModuleId,
                        ProjectName = c.ProjectName,
                        IssuedAt = c.IssuedAt
                    }).ToList()
                });
            }
            else if (syncedLicense != null)
            {
                return Ok(new LicenseUsageResponse
                {
                    LicenseId = syncedLicense.LicenseId,
                    Edition = syncedLicense.Edition ?? "Unknown",
                    Status = syncedLicense.IsRevoked ? "Revoked" : (syncedLicense.ExpiresAt < DateTime.UtcNow ? "Expired" : "Active"),
                    CreatedAt = syncedLicense.IssuedAt,
                    ExpiresAt = syncedLicense.ExpiresAt,
                    CustomerEmail = MaskEmail(syncedLicense.CustomerEmail),
                    CustomerName = syncedLicense.ClientName,
                    Brand = syncedLicense.Brand,
                    LicenseeCode = syncedLicense.ClientCode,

                    Usage = new UsageMetrics
                    {
                        TotalActivations = 0,
                        ActiveActivations = 0,
                        TotalSessions = sessionHistory.Count,
                        ActiveSessions = activeSessions.Count,
                        TotalActiveHours = Math.Round(totalActiveHours, 2),
                        CertificatesIssued = certificates.Count
                    },

                    Activations = new List<LicenseActivationInfo>(),
                    RecentCertificates = certificates.Take(5).Select(c => new RecentCertificate
                    {
                        CertificateId = c.CertificateId,
                        ModuleId = c.ModuleId,
                        ProjectName = c.ProjectName,
                        IssuedAt = c.IssuedAt
                    }).ToList()
                });
            }

            return NotFound(new { message = "License not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage stats for license {LicenseId}", licenseId);
            return StatusCode(500, new { error = "Failed to retrieve usage statistics", message = ex.Message });
        }
    }

    /// <summary>
    /// Get activation trends.
    /// GET /api/analytics/activations
    /// Returns activation trends over time.
    /// </summary>
    [HttpGet("activations")]
    public async Task<ActionResult<ActivationTrendsResponse>> GetActivationTrends(
        [FromQuery] int days = 30)
    {
        try
        {
            if (days > 365) days = 365;
            if (days < 1) days = 30;

            var cutoff = DateTime.UtcNow.AddDays(-days);

            var activations = await _db.LicenseActivations
                .Where(a => a.ActivatedAt >= cutoff)
                .ToListAsync();

            // Daily breakdown
            var dailyActivations = activations
                .GroupBy(a => a.ActivatedAt.Date)
                .Select(g => new DailyActivations
                {
                    Date = g.Key,
                    Count = g.Count(),
                    UniqueDevices = g.Select(a => a.HardwareFingerprint).Distinct().Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            // By app version
            var byVersion = activations
                .Where(a => !string.IsNullOrEmpty(a.AppVersion))
                .GroupBy(a => a.AppVersion)
                .Select(g => new VersionStats
                {
                    Version = g.Key ?? "Unknown",
                    Count = g.Count()
                })
                .OrderByDescending(v => v.Count)
                .Take(10)
                .ToList();

            // By OS
            var byOs = activations
                .Where(a => !string.IsNullOrEmpty(a.OsVersion))
                .GroupBy(a => ExtractOsFamily(a.OsVersion))
                .Select(g => new OsStats
                {
                    Os = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(o => o.Count)
                .ToList();

            return Ok(new ActivationTrendsResponse
            {
                Period = $"Last {days} days",
                TotalActivations = activations.Count,
                UniqueDevices = activations.Select(a => a.HardwareFingerprint).Distinct().Count(),
                DailyTrend = dailyActivations,
                ByVersion = byVersion,
                ByOs = byOs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activation trends");
            return StatusCode(500, new { error = "Failed to retrieve activation trends", message = ex.Message });
        }
    }

    /// <summary>
    /// Get licenses expiring soon.
    /// GET /api/analytics/expiring
    /// Returns licenses that are about to expire.
    /// </summary>
    [HttpGet("expiring")]
    public async Task<ActionResult<ExpiringLicensesResponse>> GetExpiringLicenses(
        [FromQuery] int days = 30)
    {
        try
        {
            if (days > 365) days = 365;
            if (days < 1) days = 7;

            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(days);

            var expiring = await _db.LicenseKeys
                .Where(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= cutoff)
                .OrderBy(l => l.ExpiresAt)
                .Select(l => new ExpiringLicenseInfo
                {
                    LicenseId = l.LicenseId,
                    CustomerName = l.CustomerName,
                    CustomerEmail = l.CustomerEmail,
                    Edition = l.Edition,
                    ExpiresAt = l.ExpiresAt,
                    DaysRemaining = (int)(l.ExpiresAt - now).TotalDays,
                    Brand = l.Brand,
                    LicenseeCode = l.LicenseeCode
                })
                .ToListAsync();

            // Mask emails
            foreach (var license in expiring)
            {
                license.CustomerEmail = MaskEmail(license.CustomerEmail);
            }

            // Group by urgency
            var critical = expiring.Where(l => l.DaysRemaining <= 7).ToList();
            var warning = expiring.Where(l => l.DaysRemaining > 7 && l.DaysRemaining <= 14).ToList();
            var upcoming = expiring.Where(l => l.DaysRemaining > 14).ToList();

            return Ok(new ExpiringLicensesResponse
            {
                Period = $"Next {days} days",
                TotalExpiring = expiring.Count,
                Critical = new ExpiringCategory
                {
                    Label = "Within 7 days",
                    Count = critical.Count,
                    Licenses = critical
                },
                Warning = new ExpiringCategory
                {
                    Label = "7-14 days",
                    Count = warning.Count,
                    Licenses = warning
                },
                Upcoming = new ExpiringCategory
                {
                    Label = "14+ days",
                    Count = upcoming.Count,
                    Licenses = upcoming
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expiring licenses");
            return StatusCode(500, new { error = "Failed to retrieve expiring licenses", message = ex.Message });
        }
    }

    /// <summary>
    /// Get certificate analytics.
    /// GET /api/analytics/certificates
    /// Returns certificate issuance statistics.
    /// </summary>
    [HttpGet("certificates")]
    public async Task<ActionResult<CertificateAnalyticsResponse>> GetCertificateAnalytics(
        [FromQuery] int days = 30)
    {
        try
        {
            if (days > 365) days = 365;
            if (days < 1) days = 30;

            var cutoff = DateTime.UtcNow.AddDays(-days);

            var certificates = await _db.Certificates
                .Where(c => c.IssuedAt >= cutoff)
                .ToListAsync();

            // By module
            var byModule = certificates
                .GroupBy(c => c.ModuleId)
                .Select(g => new ModuleCertStats
                {
                    ModuleId = g.Key,
                    Count = g.Count(),
                    Verified = g.Count(c => c.IsSignatureVerified)
                })
                .OrderByDescending(m => m.Count)
                .ToList();

            // By licensee
            var byLicensee = certificates
                .GroupBy(c => c.LicenseeCode)
                .Select(g => new LicenseeCertStats
                {
                    LicenseeCode = g.Key,
                    CompanyName = g.First().CompanyName,
                    Count = g.Count()
                })
                .OrderByDescending(l => l.Count)
                .Take(10)
                .ToList();

            // Daily trend
            var dailyTrend = certificates
                .GroupBy(c => c.IssuedAt.Date)
                .Select(g => new DailyCertificates
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            // All-time stats
            var totalCertificates = await _db.Certificates.CountAsync();
            var totalVerified = await _db.Certificates.CountAsync(c => c.IsSignatureVerified);

            return Ok(new CertificateAnalyticsResponse
            {
                Period = $"Last {days} days",
                PeriodCertificates = certificates.Count,
                PeriodVerified = certificates.Count(c => c.IsSignatureVerified),
                TotalCertificates = totalCertificates,
                TotalVerified = totalVerified,
                ByModule = byModule,
                ByLicensee = byLicensee,
                DailyTrend = dailyTrend
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get certificate analytics");
            return StatusCode(500, new { error = "Failed to retrieve certificate analytics", message = ex.Message });
        }
    }

    /// <summary>
    /// Get complete analytics dashboard.
    /// GET /api/analytics/dashboard
    /// Returns a comprehensive analytics overview.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AnalyticsDashboardResponse>> GetDashboard()
    {
        try
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var sevenDaysAgo = now.AddDays(-7);

            // Quick stats
            var totalLicenses = await _db.LicenseKeys.CountAsync();
            var activeLicenses = await _db.LicenseKeys.CountAsync(l => !l.IsRevoked && l.ExpiresAt > now);
            var totalActivations = await _db.LicenseActivations.CountAsync();
            var activeSessions = await _db.ActiveSessions.CountAsync(s => s.IsActive);
            var totalCertificates = await _db.Certificates.CountAsync();
            var expiringThisWeek = await _db.LicenseKeys.CountAsync(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= now.AddDays(7));

            // Recent activity
            var recentActivations = await _db.LicenseActivations
                .CountAsync(a => a.ActivatedAt >= sevenDaysAgo);
            var recentCertificates = await _db.Certificates
                .CountAsync(c => c.IssuedAt >= sevenDaysAgo);

            // Health dashboard
            var health = await _healthMonitor.GetDashboardAsync();

            return Ok(new AnalyticsDashboardResponse
            {
                Timestamp = now,

                QuickStats = new QuickStats
                {
                    TotalLicenses = totalLicenses,
                    ActiveLicenses = activeLicenses,
                    TotalActivations = totalActivations,
                    ActiveSessions = activeSessions,
                    TotalCertificates = totalCertificates,
                    ExpiringThisWeek = expiringThisWeek
                },

                RecentActivity = new DashboardRecentActivity
                {
                    ActivationsLast7Days = recentActivations,
                    CertificatesLast7Days = recentCertificates
                },

                ServerHealth = new ServerHealthSummary
                {
                    Status = health.ServerStatus,
                    UptimeHours = Math.Round(health.UptimeHours, 2),
                    AvgResponseTimeMs = Math.Round(health.ResponseTimeMs.Average, 2),
                    ErrorsLastHour = health.Errors.LastHour,
                    AlertCount = health.Alerts.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics dashboard");
            return StatusCode(500, new { error = "Failed to retrieve dashboard", message = ex.Message });
        }
    }

    // Helper methods
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];

        var maskedName = name.Length <= 2 ? "**" : $"{name[0]}***{name[^1]}";
        return $"{maskedName}@{domain}";
    }

    private static string MaskFingerprint(string fingerprint)
    {
        if (string.IsNullOrEmpty(fingerprint) || fingerprint.Length < 8)
            return "***";
        return $"{fingerprint[..4]}...{fingerprint[^4..]}";
    }

    private static string ExtractOsFamily(string? osVersion)
    {
        if (string.IsNullOrEmpty(osVersion))
            return "Unknown";

        osVersion = osVersion.ToLowerInvariant();

        if (osVersion.Contains("windows"))
            return "Windows";
        if (osVersion.Contains("mac") || osVersion.Contains("darwin"))
            return "macOS";
        if (osVersion.Contains("linux"))
            return "Linux";

        return "Other";
    }
}

// ============================================================================
// DTOs for Analytics Controller
// ============================================================================

public class LicenseStatsResponse
{
    public DateTime Timestamp { get; set; }
    public LicenseSummary Summary { get; set; } = new();
    public LicenseStatusBreakdown Status { get; set; } = new();
    public ExpiringLicenses Expiring { get; set; } = new();
    public RecentActivity RecentActivity { get; set; } = new();
    public LicenseTypeBreakdown LicenseTypes { get; set; } = new();
    public List<EditionStats> ByEdition { get; set; } = new();
    public List<SubscriptionStats> BySubscription { get; set; } = new();
    public List<MonthlyTrend> MonthlyTrend { get; set; } = new();
}

public class LicenseSummary
{
    public int TotalLicenses { get; set; }
    public int SyncedLicenses { get; set; }
    public int TotalActivations { get; set; }
    public int TotalCertificates { get; set; }
}

public class LicenseStatusBreakdown
{
    public int Active { get; set; }
    public int Expired { get; set; }
    public int Revoked { get; set; }
}

public class ExpiringLicenses
{
    public int ThisWeek { get; set; }
    public int ThisMonth { get; set; }
}

public class RecentActivity
{
    public int ActivationsLast7Days { get; set; }
    public int ActivationsLast30Days { get; set; }
}

public class LicenseTypeBreakdown
{
    public int Online { get; set; }
    public int Offline { get; set; }
}

public class EditionStats
{
    public string Edition { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Active { get; set; }
}

public class SubscriptionStats
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}

public class LicenseUsageResponse
{
    public string LicenseId { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public UsageMetrics Usage { get; set; } = new();
    public List<LicenseActivationInfo> Activations { get; set; } = new();
    public List<RecentCertificate> RecentCertificates { get; set; } = new();
}

public class UsageMetrics
{
    public int TotalActivations { get; set; }
    public int ActiveActivations { get; set; }
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public double TotalActiveHours { get; set; }
    public int CertificatesIssued { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastActiveSession { get; set; }
}

public class LicenseActivationInfo
{
    public string HardwareFingerprint { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsDeactivated { get; set; }
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
}

public class RecentCertificate
{
    public string CertificateId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
}

public class ActivationTrendsResponse
{
    public string Period { get; set; } = string.Empty;
    public int TotalActivations { get; set; }
    public int UniqueDevices { get; set; }
    public List<DailyActivations> DailyTrend { get; set; } = new();
    public List<VersionStats> ByVersion { get; set; } = new();
    public List<OsStats> ByOs { get; set; } = new();
}

public class DailyActivations
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int UniqueDevices { get; set; }
}

public class VersionStats
{
    public string Version { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OsStats
{
    public string Os { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ExpiringLicensesResponse
{
    public string Period { get; set; } = string.Empty;
    public int TotalExpiring { get; set; }
    public ExpiringCategory Critical { get; set; } = new();
    public ExpiringCategory Warning { get; set; } = new();
    public ExpiringCategory Upcoming { get; set; } = new();
}

public class ExpiringCategory
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<ExpiringLicenseInfo> Licenses { get; set; } = new();
}

public class ExpiringLicenseInfo
{
    public string LicenseId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string Edition { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int DaysRemaining { get; set; }
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
}

public class CertificateAnalyticsResponse
{
    public string Period { get; set; } = string.Empty;
    public int PeriodCertificates { get; set; }
    public int PeriodVerified { get; set; }
    public int TotalCertificates { get; set; }
    public int TotalVerified { get; set; }
    public List<ModuleCertStats> ByModule { get; set; } = new();
    public List<LicenseeCertStats> ByLicensee { get; set; } = new();
    public List<DailyCertificates> DailyTrend { get; set; } = new();
}

public class ModuleCertStats
{
    public string ModuleId { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Verified { get; set; }
}

public class LicenseeCertStats
{
    public string LicenseeCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyCertificates
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class AnalyticsDashboardResponse
{
    public DateTime Timestamp { get; set; }
    public QuickStats QuickStats { get; set; } = new();
    public DashboardRecentActivity RecentActivity { get; set; } = new();
    public ServerHealthSummary ServerHealth { get; set; } = new();
}

public class QuickStats
{
    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int TotalActivations { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalCertificates { get; set; }
    public int ExpiringThisWeek { get; set; }
}

public class DashboardRecentActivity
{
    public int ActivationsLast7Days { get; set; }
    public int CertificatesLast7Days { get; set; }
}

public class ServerHealthSummary
{
    public string Status { get; set; } = "Unknown";
    public double UptimeHours { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public int ErrorsLastHour { get; set; }
    public int AlertCount { get; set; }
}
