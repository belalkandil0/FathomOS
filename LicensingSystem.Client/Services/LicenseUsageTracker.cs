// LicensingSystem.Client/Services/LicenseUsageTracker.cs
// Tracks license usage for analytics and compliance reporting
// Supports module launches, feature usage, and server sync

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace LicensingSystem.Client.Services;

/// <summary>
/// Interface for license usage tracking
/// </summary>
public interface ILicenseUsageTracker
{
    /// <summary>
    /// Tracks a module launch event
    /// </summary>
    void TrackModuleLaunch(string moduleId);

    /// <summary>
    /// Tracks a feature usage event
    /// </summary>
    void TrackFeatureUsage(string featureId);

    /// <summary>
    /// Tracks a generic event
    /// </summary>
    void TrackEvent(string eventName, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Gets a usage report for a date range
    /// </summary>
    Task<UsageReport> GetUsageReportAsync(DateTime from, DateTime to);

    /// <summary>
    /// Syncs usage data to the server
    /// </summary>
    Task SyncUsageToServerAsync();

    /// <summary>
    /// Gets the current session duration in minutes
    /// </summary>
    int GetCurrentSessionDuration();

    /// <summary>
    /// Starts a new session
    /// </summary>
    void StartSession();

    /// <summary>
    /// Ends the current session
    /// </summary>
    void EndSession();
}

/// <summary>
/// Tracks license usage for analytics and compliance reporting.
/// Stores usage locally and syncs to server when online.
/// </summary>
public class LicenseUsageTracker : ILicenseUsageTracker, IDisposable
{
    private readonly string _storagePath;
    private readonly string _registryPath;
    private readonly string? _serverUrl;
    private readonly string _licenseId;
    private readonly HttpClient? _httpClient;
    private UsageData _usageData;
    private DateTime _sessionStartTime;
    private bool _sessionActive;
    private readonly object _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new LicenseUsageTracker instance
    /// </summary>
    /// <param name="licenseId">License ID to track usage for</param>
    /// <param name="serverUrl">Optional server URL for syncing</param>
    /// <param name="productName">Product name for storage paths</param>
    public LicenseUsageTracker(
        string licenseId,
        string? serverUrl = null,
        string productName = "FathomOS")
    {
        _licenseId = licenseId;
        _serverUrl = serverUrl;

        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            productName,
            "License",
            "Usage");
        _registryPath = $@"SOFTWARE\{productName}\License\Usage";

        Directory.CreateDirectory(_storagePath);

        if (!string.IsNullOrEmpty(serverUrl))
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        _usageData = LoadUsageData();
    }

    /// <summary>
    /// Tracks a module launch event
    /// </summary>
    public void TrackModuleLaunch(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            return;

        lock (_lockObject)
        {
            // Increment module usage count
            if (!_usageData.ModuleUsageCounts.ContainsKey(moduleId))
                _usageData.ModuleUsageCounts[moduleId] = 0;
            _usageData.ModuleUsageCounts[moduleId]++;

            // Record to daily usage
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            EnsureDailyRecord(today);
            if (!_usageData.DailyUsage[today].ModulesLaunched.Contains(moduleId))
                _usageData.DailyUsage[today].ModulesLaunched.Add(moduleId);
            _usageData.DailyUsage[today].ModuleLaunchCount++;

            // Add to pending sync
            _usageData.PendingEvents.Add(new UsageEvent
            {
                EventType = "ModuleLaunch",
                EntityId = moduleId,
                Timestamp = DateTime.UtcNow
            });

            SaveUsageData();
        }
    }

    /// <summary>
    /// Tracks a feature usage event
    /// </summary>
    public void TrackFeatureUsage(string featureId)
    {
        if (string.IsNullOrEmpty(featureId))
            return;

        lock (_lockObject)
        {
            // Increment feature usage count
            if (!_usageData.FeatureUsageCounts.ContainsKey(featureId))
                _usageData.FeatureUsageCounts[featureId] = 0;
            _usageData.FeatureUsageCounts[featureId]++;

            // Record to daily usage
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            EnsureDailyRecord(today);
            if (!_usageData.DailyUsage[today].FeaturesUsed.Contains(featureId))
                _usageData.DailyUsage[today].FeaturesUsed.Add(featureId);
            _usageData.DailyUsage[today].FeatureUsageCount++;

            // Add to pending sync
            _usageData.PendingEvents.Add(new UsageEvent
            {
                EventType = "FeatureUsage",
                EntityId = featureId,
                Timestamp = DateTime.UtcNow
            });

            SaveUsageData();
        }
    }

    /// <summary>
    /// Tracks a generic event
    /// </summary>
    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null)
    {
        if (string.IsNullOrEmpty(eventName))
            return;

        lock (_lockObject)
        {
            _usageData.PendingEvents.Add(new UsageEvent
            {
                EventType = eventName,
                Timestamp = DateTime.UtcNow,
                Properties = properties
            });

            SaveUsageData();
        }
    }

    /// <summary>
    /// Starts a new session
    /// </summary>
    public void StartSession()
    {
        lock (_lockObject)
        {
            _sessionStartTime = DateTime.UtcNow;
            _sessionActive = true;
            _usageData.TotalSessions++;

            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            EnsureDailyRecord(today);
            _usageData.DailyUsage[today].SessionCount++;

            _usageData.PendingEvents.Add(new UsageEvent
            {
                EventType = "SessionStart",
                Timestamp = DateTime.UtcNow
            });

            SaveUsageData();
        }
    }

    /// <summary>
    /// Ends the current session
    /// </summary>
    public void EndSession()
    {
        lock (_lockObject)
        {
            if (!_sessionActive)
                return;

            var sessionDuration = (int)(DateTime.UtcNow - _sessionStartTime).TotalMinutes;
            _usageData.TotalUsageMinutes += sessionDuration;

            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            EnsureDailyRecord(today);
            _usageData.DailyUsage[today].UsageMinutes += sessionDuration;

            _usageData.PendingEvents.Add(new UsageEvent
            {
                EventType = "SessionEnd",
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, string>
                {
                    { "DurationMinutes", sessionDuration.ToString() }
                }
            });

            _sessionActive = false;
            SaveUsageData();
        }
    }

    /// <summary>
    /// Gets the current session duration in minutes
    /// </summary>
    public int GetCurrentSessionDuration()
    {
        if (!_sessionActive)
            return 0;

        return (int)(DateTime.UtcNow - _sessionStartTime).TotalMinutes;
    }

    /// <summary>
    /// Gets a usage report for a date range
    /// </summary>
    public Task<UsageReport> GetUsageReportAsync(DateTime from, DateTime to)
    {
        lock (_lockObject)
        {
            var report = new UsageReport
            {
                LicenseId = _licenseId,
                FromDate = from,
                ToDate = to,
                GeneratedAt = DateTime.UtcNow
            };

            var fromStr = from.ToString("yyyy-MM-dd");
            var toStr = to.ToString("yyyy-MM-dd");

            foreach (var kvp in _usageData.DailyUsage)
            {
                if (string.Compare(kvp.Key, fromStr) >= 0 && string.Compare(kvp.Key, toStr) <= 0)
                {
                    report.TotalSessions += kvp.Value.SessionCount;
                    report.TotalUsageMinutes += kvp.Value.UsageMinutes;
                    report.TotalModuleLaunches += kvp.Value.ModuleLaunchCount;
                    report.TotalFeatureUsages += kvp.Value.FeatureUsageCount;

                    foreach (var module in kvp.Value.ModulesLaunched)
                    {
                        if (!report.ModuleUsage.ContainsKey(module))
                            report.ModuleUsage[module] = 0;
                        report.ModuleUsage[module]++;
                    }

                    foreach (var feature in kvp.Value.FeaturesUsed)
                    {
                        if (!report.FeatureUsage.ContainsKey(feature))
                            report.FeatureUsage[feature] = 0;
                        report.FeatureUsage[feature]++;
                    }

                    report.DailyRecords.Add(new DailyUsageRecord
                    {
                        Date = kvp.Key,
                        Sessions = kvp.Value.SessionCount,
                        UsageMinutes = kvp.Value.UsageMinutes,
                        ModuleLaunches = kvp.Value.ModuleLaunchCount,
                        FeatureUsages = kvp.Value.FeatureUsageCount,
                        ModulesUsed = kvp.Value.ModulesLaunched,
                        FeaturesUsed = kvp.Value.FeaturesUsed
                    });
                }
            }

            report.DailyRecords = report.DailyRecords.OrderBy(r => r.Date).ToList();
            return Task.FromResult(report);
        }
    }

    /// <summary>
    /// Syncs usage data to the server
    /// </summary>
    public async Task SyncUsageToServerAsync()
    {
        if (_httpClient == null || string.IsNullOrEmpty(_serverUrl))
            return;

        List<UsageEvent> eventsToSync;
        lock (_lockObject)
        {
            if (_usageData.PendingEvents.Count == 0)
                return;

            eventsToSync = _usageData.PendingEvents.ToList();
        }

        try
        {
            var request = new UsageSyncRequest
            {
                LicenseId = _licenseId,
                Events = eventsToSync,
                MachineName = Environment.MachineName,
                SyncedAt = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/usage/sync",
                request);

            if (response.IsSuccessStatusCode)
            {
                lock (_lockObject)
                {
                    // Remove synced events
                    foreach (var evt in eventsToSync)
                    {
                        _usageData.PendingEvents.Remove(evt);
                    }
                    _usageData.LastSyncedAt = DateTime.UtcNow;
                    SaveUsageData();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Usage sync failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all-time usage statistics
    /// </summary>
    public UsageStatistics GetAllTimeStatistics()
    {
        lock (_lockObject)
        {
            return new UsageStatistics
            {
                LicenseId = _licenseId,
                TotalSessions = _usageData.TotalSessions,
                TotalUsageMinutes = _usageData.TotalUsageMinutes,
                ModuleUsageCounts = new Dictionary<string, int>(_usageData.ModuleUsageCounts),
                FeatureUsageCounts = new Dictionary<string, int>(_usageData.FeatureUsageCounts),
                FirstUsageDate = _usageData.FirstUsageDate,
                LastUsageDate = _usageData.LastUsageDate
            };
        }
    }

    private void EnsureDailyRecord(string date)
    {
        if (!_usageData.DailyUsage.ContainsKey(date))
        {
            _usageData.DailyUsage[date] = new DailyUsageData
            {
                Date = date
            };
        }

        // Update first/last usage dates
        if (_usageData.FirstUsageDate == DateTime.MinValue)
            _usageData.FirstUsageDate = DateTime.UtcNow;
        _usageData.LastUsageDate = DateTime.UtcNow;
    }

    private UsageData LoadUsageData()
    {
        try
        {
            var filePath = Path.Combine(_storagePath, $"usage_{_licenseId}.dat");
            if (File.Exists(filePath))
            {
                var encrypted = Convert.FromBase64String(File.ReadAllText(filePath));
                var json = Unprotect(encrypted);
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<UsageData>(json) ?? new UsageData();
                }
            }
        }
        catch
        {
            // Failed to load, return default
        }

        return new UsageData();
    }

    private void SaveUsageData()
    {
        try
        {
            var json = JsonSerializer.Serialize(_usageData);
            var encrypted = Protect(json);
            var filePath = Path.Combine(_storagePath, $"usage_{_licenseId}.dat");
            File.WriteAllText(filePath, Convert.ToBase64String(encrypted));
        }
        catch { }
    }

    private static readonly byte[] Entropy =
    {
        0x55, 0x53, 0x41, 0x47, 0x45, 0x54, 0x52, 0x41,
        0x43, 0x4B, 0x45, 0x52, 0x44, 0x41, 0x54, 0x41
    };

    private byte[] Protect(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
    }

    private string Unprotect(byte[] encryptedData)
    {
        try
        {
            var bytes = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_sessionActive)
                EndSession();

            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Internal storage for usage data
/// </summary>
internal class UsageData
{
    public int TotalSessions { get; set; }
    public long TotalUsageMinutes { get; set; }
    public Dictionary<string, int> ModuleUsageCounts { get; set; } = new();
    public Dictionary<string, int> FeatureUsageCounts { get; set; } = new();
    public Dictionary<string, DailyUsageData> DailyUsage { get; set; } = new();
    public List<UsageEvent> PendingEvents { get; set; } = new();
    public DateTime FirstUsageDate { get; set; }
    public DateTime LastUsageDate { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

/// <summary>
/// Daily usage record
/// </summary>
internal class DailyUsageData
{
    public string Date { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int UsageMinutes { get; set; }
    public int ModuleLaunchCount { get; set; }
    public int FeatureUsageCount { get; set; }
    public List<string> ModulesLaunched { get; set; } = new();
    public List<string> FeaturesUsed { get; set; } = new();
}

/// <summary>
/// Usage event for tracking
/// </summary>
public class UsageEvent
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("entityId")]
    public string? EntityId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, string>? Properties { get; set; }
}

/// <summary>
/// Request to sync usage data to server
/// </summary>
public class UsageSyncRequest
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<UsageEvent> Events { get; set; } = new();

    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = string.Empty;

    [JsonPropertyName("syncedAt")]
    public DateTime SyncedAt { get; set; }
}

/// <summary>
/// Usage report for a date range
/// </summary>
public class UsageReport
{
    public string LicenseId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalSessions { get; set; }
    public long TotalUsageMinutes { get; set; }
    public int TotalModuleLaunches { get; set; }
    public int TotalFeatureUsages { get; set; }
    public Dictionary<string, int> ModuleUsage { get; set; } = new();
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
    public List<DailyUsageRecord> DailyRecords { get; set; } = new();

    /// <summary>
    /// Average usage minutes per day
    /// </summary>
    public double AverageUsageMinutesPerDay
    {
        get
        {
            var days = DailyRecords.Count;
            return days > 0 ? (double)TotalUsageMinutes / days : 0;
        }
    }

    /// <summary>
    /// Most used module
    /// </summary>
    public string? MostUsedModule => ModuleUsage
        .OrderByDescending(kvp => kvp.Value)
        .FirstOrDefault().Key;

    /// <summary>
    /// Most used feature
    /// </summary>
    public string? MostUsedFeature => FeatureUsage
        .OrderByDescending(kvp => kvp.Value)
        .FirstOrDefault().Key;
}

/// <summary>
/// Daily usage record for reports
/// </summary>
public class DailyUsageRecord
{
    public string Date { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int UsageMinutes { get; set; }
    public int ModuleLaunches { get; set; }
    public int FeatureUsages { get; set; }
    public List<string> ModulesUsed { get; set; } = new();
    public List<string> FeaturesUsed { get; set; } = new();
}

/// <summary>
/// All-time usage statistics
/// </summary>
public class UsageStatistics
{
    public string LicenseId { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public long TotalUsageMinutes { get; set; }
    public Dictionary<string, int> ModuleUsageCounts { get; set; } = new();
    public Dictionary<string, int> FeatureUsageCounts { get; set; } = new();
    public DateTime FirstUsageDate { get; set; }
    public DateTime LastUsageDate { get; set; }

    /// <summary>
    /// Total usage hours
    /// </summary>
    public double TotalUsageHours => TotalUsageMinutes / 60.0;

    /// <summary>
    /// Total days since first use
    /// </summary>
    public int DaysSinceFirstUse => FirstUsageDate == DateTime.MinValue
        ? 0
        : (int)(DateTime.UtcNow - FirstUsageDate).TotalDays;
}
