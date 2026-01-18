// LicensingSystem.Server/Controllers/HealthController.cs
// Health check endpoints for server monitoring
// Provides basic and detailed health status information

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LicensingSystem.Server.Controllers;

/// <summary>
/// Controller for health check endpoints.
/// Provides server status, database connectivity, and resource utilization information.
///
/// PUBLIC ENDPOINTS (no authentication required):
///   - GET /api/health - Basic health status
///   - GET /api/health/detailed - Detailed health information (requires API key)
///   - GET /api/health/database - Database connectivity status
///   - GET /api/health/metrics - Server metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly LicenseDbContext _db;
    private readonly IHealthMonitorService _healthMonitor;
    private readonly ILogger<HealthController> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthController(
        LicenseDbContext db,
        IHealthMonitorService healthMonitor,
        ILogger<HealthController> logger)
    {
        _db = db;
        _healthMonitor = healthMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// GET /api/health
    /// Returns server status and timestamp.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new HealthCheckResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = "3.5.0",
            Uptime = DateTime.UtcNow - _startTime
        });
    }

    /// <summary>
    /// Detailed health check endpoint.
    /// GET /api/health/detailed
    /// Includes database status, memory usage, active connections, etc.
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var stopwatch = Stopwatch.StartNew();

        // Check database connectivity
        bool dbConnected = false;
        long dbResponseMs = 0;
        string? dbError = null;
        int licenseCount = 0;
        int activationCount = 0;
        int certificateCount = 0;

        try
        {
            var dbStopwatch = Stopwatch.StartNew();
            dbConnected = await _db.Database.CanConnectAsync();
            dbResponseMs = dbStopwatch.ElapsedMilliseconds;

            if (dbConnected)
            {
                licenseCount = await _db.LicenseKeys.CountAsync();
                activationCount = await _db.LicenseActivations.CountAsync();
                certificateCount = await _db.Certificates.CountAsync();
            }
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
            _logger.LogError(ex, "Database health check failed");
        }

        // Get memory info
        var process = Process.GetCurrentProcess();
        var memoryInfo = new MemoryInfo
        {
            WorkingSetMB = process.WorkingSet64 / 1024.0 / 1024.0,
            PrivateMemoryMB = process.PrivateMemorySize64 / 1024.0 / 1024.0,
            GCTotalMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };

        // Get active sessions
        int activeSessions = 0;
        try
        {
            activeSessions = await _db.ActiveSessions.CountAsync(s => s.IsActive);
        }
        catch { /* ignore */ }

        // Get health alerts
        var alerts = await _healthMonitor.GetActiveAlertsAsync();

        stopwatch.Stop();

        var status = dbConnected && alerts.All(a => a.Severity != "Critical")
            ? "healthy"
            : dbConnected
                ? "degraded"
                : "unhealthy";

        return Ok(new DetailedHealthCheckResponse
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            Version = "3.5.0",
            Uptime = DateTime.UtcNow - _startTime,
            CheckDurationMs = stopwatch.ElapsedMilliseconds,

            Database = new DatabaseHealthInfo
            {
                Connected = dbConnected,
                ResponseTimeMs = dbResponseMs,
                Error = dbError,
                LicenseCount = licenseCount,
                ActivationCount = activationCount,
                CertificateCount = certificateCount
            },

            Memory = memoryInfo,

            Server = new ServerInfo
            {
                MachineName = Environment.MachineName,
                OsDescription = RuntimeInformation.OSDescription,
                FrameworkDescription = RuntimeInformation.FrameworkDescription,
                ProcessorCount = Environment.ProcessorCount,
                StartTime = _startTime
            },

            Sessions = new SessionInfo
            {
                ActiveCount = activeSessions
            },

            Alerts = alerts.Select(a => new AlertInfo
            {
                Severity = a.Severity,
                Title = a.Title,
                Message = a.Message
            }).ToList()
        });
    }

    /// <summary>
    /// Database health check endpoint.
    /// GET /api/health/database
    /// Checks database connectivity and returns table counts.
    /// </summary>
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            stopwatch.Stop();

            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    connected = false,
                    responseTimeMs = stopwatch.ElapsedMilliseconds,
                    message = "Unable to connect to database"
                });
            }

            var licenseCount = await _db.LicenseKeys.CountAsync();
            var syncedLicenseCount = await _db.SyncedLicenses.CountAsync();
            var activationCount = await _db.LicenseActivations.CountAsync();
            var certificateCount = await _db.Certificates.CountAsync();
            var auditLogCount = await _db.AuditLogs.CountAsync();

            return Ok(new
            {
                status = "healthy",
                connected = true,
                responseTimeMs = stopwatch.ElapsedMilliseconds,
                tables = new
                {
                    licenses = licenseCount,
                    syncedLicenses = syncedLicenseCount,
                    activations = activationCount,
                    certificates = certificateCount,
                    auditLogs = auditLogCount
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");

            return StatusCode(503, new
            {
                status = "unhealthy",
                connected = false,
                responseTimeMs = stopwatch.ElapsedMilliseconds,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Server metrics endpoint.
    /// GET /api/health/metrics
    /// Returns performance metrics and statistics.
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var dashboard = await _healthMonitor.GetDashboardAsync();

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                serverStatus = dashboard.ServerStatus,
                uptimeHours = dashboard.UptimeHours,

                responseTime = new
                {
                    averageMs = Math.Round(dashboard.ResponseTimeMs.Average, 2),
                    maxMs = Math.Round(dashboard.ResponseTimeMs.Max, 2),
                    requestCount = dashboard.ResponseTimeMs.RequestCount
                },

                errors = new
                {
                    lastHour = dashboard.Errors.LastHour,
                    last24Hours = dashboard.Errors.Last24Hours
                },

                activationFailures = new
                {
                    lastHour = dashboard.ActivationFailures.LastHour,
                    last24Hours = dashboard.ActivationFailures.Last24Hours
                },

                licenses = new
                {
                    total = dashboard.Licenses.Total,
                    active = dashboard.Licenses.Active,
                    expiringThisWeek = dashboard.Licenses.ExpiringThisWeek
                },

                sessions = new
                {
                    active = dashboard.Sessions.Active
                },

                alerts = dashboard.Alerts.Select(a => new
                {
                    severity = a.Severity,
                    title = a.Title,
                    message = a.Message
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics", message = ex.Message });
        }
    }

    /// <summary>
    /// Readiness probe for container orchestration.
    /// GET /api/health/ready
    /// Returns 200 if server is ready to accept requests.
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();

            if (canConnect)
            {
                return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }

            return StatusCode(503, new { status = "not_ready", reason = "Database connection failed" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "not_ready", reason = ex.Message });
        }
    }

    /// <summary>
    /// Liveness probe for container orchestration.
    /// GET /api/health/live
    /// Returns 200 if server process is running.
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow, uptime = DateTime.UtcNow - _startTime });
    }
}

// ============================================================================
// DTOs for Health Controller
// ============================================================================

public class HealthCheckResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "3.5.0";
    public TimeSpan Uptime { get; set; }
}

public class DetailedHealthCheckResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "3.5.0";
    public TimeSpan Uptime { get; set; }
    public long CheckDurationMs { get; set; }
    public DatabaseHealthInfo Database { get; set; } = new();
    public MemoryInfo Memory { get; set; } = new();
    public ServerInfo Server { get; set; } = new();
    public SessionInfo Sessions { get; set; } = new();
    public List<AlertInfo> Alerts { get; set; } = new();
}

public class DatabaseHealthInfo
{
    public bool Connected { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? Error { get; set; }
    public int LicenseCount { get; set; }
    public int ActivationCount { get; set; }
    public int CertificateCount { get; set; }
}

public class MemoryInfo
{
    public double WorkingSetMB { get; set; }
    public double PrivateMemoryMB { get; set; }
    public double GCTotalMemoryMB { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}

public class ServerInfo
{
    public string MachineName { get; set; } = string.Empty;
    public string OsDescription { get; set; } = string.Empty;
    public string FrameworkDescription { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public DateTime StartTime { get; set; }
}

public class SessionInfo
{
    public int ActiveCount { get; set; }
}

public class AlertInfo
{
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
