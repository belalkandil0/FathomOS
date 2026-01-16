// LicensingSystem.Client/FathomOSIntegration.cs
// Ready-to-use integration helper for Fathom OS application
// v3.4.7 - Simplified version without session API requirements

using System.Timers;
using LicensingSystem.Shared;
using Timer = System.Timers.Timer;

namespace LicensingSystem.Client;

/// <summary>
/// Fathom OS Integration Helper - Drop-in license management for your application
/// </summary>
public class FathomOSLicenseIntegration : IDisposable
{
    private readonly LicenseManager _manager;
    private readonly Timer _heartbeatTimer;
    private readonly Timer _expirationCheckTimer;
    private bool _disposed;

    // Events for UI integration
    public event EventHandler<LicenseExpirationEventArgs>? LicenseExpiringSoon;
    public event EventHandler<LicenseStatusChangedEventArgs>? LicenseStatusChanged;
    public event EventHandler<string>? HeartbeatFailed;

    /// <summary>
    /// Access to the underlying LicenseManager for backward compatibility.
    /// Use this when existing code requires direct LicenseManager access.
    /// </summary>
    public LicenseManager InternalManager => _manager;

    /// <summary>
    /// Current license status - always synced with internal manager
    /// </summary>
    public LicenseValidationResult? CurrentLicense => _manager.GetCachedResult();

    /// <summary>
    /// Whether the application is currently licensed
    /// </summary>
    public bool IsLicensed
    {
        get
        {
            var result = _manager.GetCachedResult() ?? _manager.CheckLicense(forceRefresh: false);
            return result?.IsValid == true || result?.Status == LicenseStatus.GracePeriod;
        }
    }

    /// <summary>
    /// Days until license expiration (null if not licensed or lifetime)
    /// </summary>
    public int? DaysUntilExpiration
    {
        get
        {
            var license = CurrentLicense?.License;
            if (license?.ExpiresAt == null) return null;
            var days = (license.ExpiresAt - DateTime.UtcNow).Days;
            return days < 0 ? 0 : days;
        }
    }

    /// <summary>
    /// Creates a new Fathom OS license integration instance
    /// </summary>
    /// <param name="serverUrl">License server URL (e.g., https://license.fathomos.com)</param>
    /// <param name="productName">Product name (default: FathomOS)</param>
    public FathomOSLicenseIntegration(string serverUrl, string productName = "FathomOS")
    {
        _manager = new LicenseManager(productName, serverUrl);

        // Heartbeat timer - every 5 minutes (check server status)
        _heartbeatTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _heartbeatTimer.Elapsed += OnHeartbeatTimer;
        _heartbeatTimer.AutoReset = true;

        // Expiration check timer - every hour
        _expirationCheckTimer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        _expirationCheckTimer.Elapsed += OnExpirationCheckTimer;
        _expirationCheckTimer.AutoReset = true;
    }

    /// <summary>
    /// Initialize and validate the license on application startup
    /// </summary>
    /// <returns>License validation result</returns>
    public async Task<LicenseValidationResult> InitializeAsync()
    {
        try
        {
            var result = await _manager.CheckLicenseAsync(forceRefresh: true);

            if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
            {
                // Start timers for ongoing checks
                _heartbeatTimer.Start();
                _expirationCheckTimer.Start();

                // Check for expiration warning
                CheckExpirationWarning();
            }

            // Fire status changed event
            LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
            {
                IsValid = result.IsValid,
                Status = result.Status,
                Message = result.Message
            });

            return result;
        }
        catch (Exception ex)
        {
            var errorResult = new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = $"License initialization failed: {ex.Message}"
            };
            return errorResult;
        }
    }

    /// <summary>
    /// Activate a license key (online)
    /// </summary>
    public async Task<LicenseValidationResult> ActivateLicenseAsync(string licenseKey, string email)
    {
        try
        {
            var result = await _manager.ActivateOnlineAsync(licenseKey, email);

            if (result.IsValid)
            {
                // Start timers
                _heartbeatTimer.Start();
                _expirationCheckTimer.Start();
            }

            LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
            {
                IsValid = result.IsValid,
                Status = result.Status,
                Message = result.Message
            });

            return result;
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Activate an offline license file (.lic file)
    /// </summary>
    /// <param name="licenseFilePath">Path to the .lic file generated by the License Manager</param>
    public LicenseValidationResult ActivateOffline(string licenseFilePath)
    {
        try
        {
            var result = _manager.ActivateFromFile(licenseFilePath);

            if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
            {
                _expirationCheckTimer.Start();
            }

            LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
            {
                IsValid = result.IsValid,
                Status = result.Status,
                Message = result.Message
            });

            return result;
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = $"Failed to activate offline license: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Refresh license status (call after external activation)
    /// </summary>
    public LicenseValidationResult RefreshLicenseStatus()
    {
        var result = _manager.CheckLicense(forceRefresh: false);
        
        if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
        {
            if (!_heartbeatTimer.Enabled) _heartbeatTimer.Start();
            if (!_expirationCheckTimer.Enabled) _expirationCheckTimer.Start();
        }

        return result;
    }

    /// <summary>
    /// Get current hardware ID for transfer purposes
    /// </summary>
    public string GetHardwareId()
    {
        return _manager.GetHardwareId();
    }

    /// <summary>
    /// Check if a specific module is licensed
    /// </summary>
    public bool HasModule(string moduleId)
    {
        return _manager.IsModuleLicensed(moduleId);
    }

    /// <summary>
    /// Check if a specific feature is enabled
    /// </summary>
    public bool HasFeature(string featureName)
    {
        return _manager.IsFeatureEnabled(featureName);
    }

    /// <summary>
    /// Get all licensed module IDs
    /// </summary>
    public List<string> GetLicensedModules()
    {
        return _manager.GetLicensedModules();
    }

    /// <summary>
    /// Get license info for display in Help > License Info
    /// </summary>
    public LicenseDisplayInfo GetLicenseDisplayInfo()
    {
        var result = CurrentLicense ?? _manager.CheckLicense(forceRefresh: false);
        var license = result?.License;
        
        return new LicenseDisplayInfo
        {
            IsValid = result?.IsValid ?? false,
            LicenseId = license?.LicenseId ?? "Not Licensed",
            Edition = license?.Edition ?? "Unknown",
            CustomerName = license?.CustomerName,
            CustomerEmail = license?.CustomerEmail,
            ExpiresAt = license?.ExpiresAt,
            DaysRemaining = DaysUntilExpiration,
            HardwareId = GetHardwareId(),
            Brand = license?.Brand,
            SupportCode = license?.SupportCode,
            Features = license?.Features ?? new List<string>(),
            Modules = license?.Modules ?? new List<string>()
        };
    }

    /// <summary>
    /// Shutdown - call on application exit
    /// </summary>
    public Task ShutdownAsync()
    {
        _heartbeatTimer.Stop();
        _expirationCheckTimer.Stop();
        return Task.CompletedTask;
    }

    private async void OnHeartbeatTimer(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // Just do a periodic license check
            var result = await _manager.CheckLicenseAsync(forceRefresh: true);
            
            if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            {
                // License became invalid (expired or revoked)
                LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
                {
                    IsValid = false,
                    Status = result.Status,
                    Message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            HeartbeatFailed?.Invoke(this, ex.Message);
        }
    }

    private void OnExpirationCheckTimer(object? sender, ElapsedEventArgs e)
    {
        CheckExpirationWarning();
    }

    private void CheckExpirationWarning()
    {
        var license = CurrentLicense?.License;
        if (license?.ExpiresAt == null) return;

        var daysRemaining = DaysUntilExpiration ?? 999;

        if (daysRemaining <= 0)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = 0,
                ExpiresAt = license.ExpiresAt,
                Severity = ExpirationSeverity.Expired
            });
        }
        else if (daysRemaining <= 7)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = daysRemaining,
                ExpiresAt = license.ExpiresAt,
                Severity = ExpirationSeverity.Critical
            });
        }
        else if (daysRemaining <= 30)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = daysRemaining,
                ExpiresAt = license.ExpiresAt,
                Severity = ExpirationSeverity.Warning
            });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _heartbeatTimer.Stop();
        _heartbeatTimer.Dispose();
        _expirationCheckTimer.Stop();
        _expirationCheckTimer.Dispose();
        _manager.Dispose();
    }
}

// Event Args Classes

public class LicenseExpirationEventArgs : EventArgs
{
    public int DaysRemaining { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ExpirationSeverity Severity { get; set; }
}

public enum ExpirationSeverity
{
    Warning,    // 30 days or less
    Critical,   // 7 days or less
    Expired     // 0 days
}

public class LicenseStatusChangedEventArgs : EventArgs
{
    public bool IsValid { get; set; }
    public LicenseStatus Status { get; set; }
    public string? Message { get; set; }
}

public class LicenseDisplayInfo
{
    public bool IsValid { get; set; }
    public string LicenseId { get; set; } = "";
    public string Edition { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysRemaining { get; set; }
    public string HardwareId { get; set; } = "";
    public string? Brand { get; set; }
    public string? SupportCode { get; set; }
    public List<string> Features { get; set; } = new();
    public List<string> Modules { get; set; } = new();
    
    /// <summary>
    /// Gets the display edition formatted as:
    /// - "Fathom OS" if not licensed or no brand
    /// - "Fathom OS — {Brand} Edition" if branded
    /// Use this for window titles and about dialogs.
    /// </summary>
    public string DisplayEdition
    {
        get
        {
            if (!IsValid || string.IsNullOrEmpty(Brand))
                return "Fathom OS";
            return $"Fathom OS — {Brand} Edition";
        }
    }
    
    public string StatusText
    {
        get
        {
            if (!IsValid) return "Not Licensed";
            if (DaysRemaining == null) return "Lifetime License";
            if (DaysRemaining <= 0) return "EXPIRED";
            if (DaysRemaining <= 7) return $"Expires in {DaysRemaining} days (CRITICAL)";
            if (DaysRemaining <= 30) return $"Expires in {DaysRemaining} days";
            return $"Valid until {ExpiresAt:yyyy-MM-dd}";
        }
    }
}
