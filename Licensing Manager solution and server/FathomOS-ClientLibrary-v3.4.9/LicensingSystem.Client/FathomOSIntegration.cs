// LicensingSystem.Client/FathomOSIntegration.cs
// Ready-to-use integration helper for Fathom OS application
// v3.4.9 - Complete API with module/feature checking, license info, and deactivation

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
    private readonly LicenseClient? _client;
    private readonly Timer _heartbeatTimer;
    private readonly Timer _expirationCheckTimer;
    private bool _disposed;
    private string? _currentLicenseId;

    // Events for UI integration
    public event EventHandler<LicenseExpirationEventArgs>? LicenseExpiringSoon;
    public event EventHandler<SessionConflictEventArgs>? SessionConflict;
    public event EventHandler<LicenseTransferredEventArgs>? LicenseTransferred;
    public event EventHandler<LicenseStatusChangedEventArgs>? LicenseStatusChanged;
    public event EventHandler<string>? HeartbeatFailed;

    /// <summary>
    /// Current license status
    /// </summary>
    public LicenseValidationResult? CurrentLicense { get; private set; }

    /// <summary>
    /// Whether the application is currently licensed
    /// </summary>
    public bool IsLicensed => CurrentLicense?.IsValid == true;

    /// <summary>
    /// Days until license expiration (null if not licensed or lifetime)
    /// </summary>
    public int? DaysUntilExpiration
    {
        get
        {
            if (CurrentLicense?.ExpiresAt == null) return null;
            var days = (CurrentLicense.ExpiresAt.Value - DateTime.UtcNow).Days;
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
        _client = new LicenseClient(serverUrl, productName);

        // Heartbeat timer - every 2 minutes
        _heartbeatTimer = new Timer(TimeSpan.FromMinutes(2).TotalMilliseconds);
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
            CurrentLicense = await _manager.ValidateLicenseAsync();

            if (CurrentLicense.IsValid)
            {
                _currentLicenseId = CurrentLicense.LicenseId;

                // Start session
                var sessionResult = await StartSessionAsync();
                if (!sessionResult.Success && sessionResult.IsConflict)
                {
                    SessionConflict?.Invoke(this, new SessionConflictEventArgs
                    {
                        ActiveDevice = sessionResult.ConflictDevice,
                        LastSeen = sessionResult.ConflictLastSeen,
                        CanForceTerminate = sessionResult.CanForceTerminate
                    });
                }

                // Start timers
                _heartbeatTimer.Start();
                _expirationCheckTimer.Start();

                // Check for expiration warning
                CheckExpirationWarning();
            }

            LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
            {
                IsValid = CurrentLicense.IsValid,
                Status = CurrentLicense.Status,
                Message = CurrentLicense.Message
            });

            return CurrentLicense;
        }
        catch (Exception ex)
        {
            CurrentLicense = new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Error,
                Message = $"License initialization failed: {ex.Message}"
            };
            return CurrentLicense;
        }
    }

    /// <summary>
    /// Activate a license key
    /// </summary>
    public async Task<LicenseValidationResult> ActivateLicenseAsync(string licenseKey, string email)
    {
        try
        {
            CurrentLicense = await _manager.ActivateOnlineAsync(licenseKey, email);

            if (CurrentLicense.IsValid)
            {
                _currentLicenseId = CurrentLicense.LicenseId;

                // Start session
                await StartSessionAsync();

                // Start timers
                _heartbeatTimer.Start();
                _expirationCheckTimer.Start();
            }

            LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
            {
                IsValid = CurrentLicense.IsValid,
                Status = CurrentLicense.Status,
                Message = CurrentLicense.Message
            });

            return CurrentLicense;
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Error,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Activate an offline license file (.lic file)
    /// </summary>
    /// <param name="licenseFilePath">Path to the .lic file generated by the License Manager</param>
    public Task<LicenseValidationResult> ActivateOfflineAsync(string licenseFilePath)
    {
        try
        {
            // Use synchronous ActivateFromFile (offline doesn't need network)
            CurrentLicense = _manager.ActivateFromFile(licenseFilePath);

            if (CurrentLicense.IsValid)
            {
                _currentLicenseId = CurrentLicense.LicenseId;
                _expirationCheckTimer.Start();
            }

            LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
            {
                IsValid = CurrentLicense.IsValid,
                Status = CurrentLicense.Status,
                Message = CurrentLicense.Message
            });

            return Task.FromResult(CurrentLicense);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Error,
                Message = $"Failed to activate offline license: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Force terminate another session and take over
    /// </summary>
    public async Task<bool> ForceTerminateOtherSessionAsync()
    {
        if (string.IsNullOrEmpty(_currentLicenseId) || _client == null)
            return false;

        var terminated = await _client.ForceTerminateSessionAsync(_currentLicenseId);
        if (terminated)
        {
            // Now start our session
            var result = await StartSessionAsync();
            return result.Success;
        }
        return false;
    }

    /// <summary>
    /// Get current hardware ID for transfer purposes
    /// </summary>
    public string GetHardwareId()
    {
        var fingerprint = new HardwareFingerprint();
        return fingerprint.GetDisplayHwid();
    }

    /// <summary>
    /// Get license info for display in Help > License Info
    /// </summary>
    public LicenseDisplayInfo GetLicenseDisplayInfo()
    {
        var fingerprint = new HardwareFingerprint();
        
        return new LicenseDisplayInfo
        {
            IsValid = CurrentLicense?.IsValid ?? false,
            LicenseId = CurrentLicense?.LicenseId ?? "Not Licensed",
            Edition = CurrentLicense?.Edition ?? "Unknown",
            CustomerName = CurrentLicense?.CustomerName,
            CustomerEmail = CurrentLicense?.CustomerEmail,
            ExpiresAt = CurrentLicense?.ExpiresAt,
            DaysRemaining = DaysUntilExpiration,
            HardwareId = fingerprint.GetDisplayHwid(),
            Brand = CurrentLicense?.Brand,
            SupportCode = CurrentLicense?.SupportCode,
            Features = CurrentLicense?.Features ?? new List<string>(),
            Modules = CurrentLicense?.Modules ?? new List<string>()
        };
    }

    /// <summary>
    /// Shutdown - call on application exit
    /// </summary>
    public async Task ShutdownAsync()
    {
        _heartbeatTimer.Stop();
        _expirationCheckTimer.Stop();

        if (_client != null)
        {
            await _client.EndSessionAsync();
        }
    }

    #region Module and Feature Checking

    /// <summary>
    /// Check if a specific module is licensed
    /// </summary>
    /// <param name="moduleId">The module ID to check (e.g., "SurveyListing")</param>
    public bool HasModule(string moduleId)
    {
        return _manager.IsModuleLicensed(moduleId);
    }

    /// <summary>
    /// Check if a specific feature is enabled
    /// </summary>
    /// <param name="featureName">The feature name to check (e.g., "Export:PDF")</param>
    public bool HasFeature(string featureName)
    {
        return _manager.IsFeatureEnabled(featureName);
    }

    /// <summary>
    /// Get list of all licensed module IDs
    /// </summary>
    public List<string> GetLicensedModules()
    {
        return _manager.GetLicensedModules();
    }

    /// <summary>
    /// Get the current license tier (e.g., "Professional", "Enterprise")
    /// </summary>
    public string? GetLicenseTier()
    {
        return _manager.GetLicenseTier();
    }

    /// <summary>
    /// Check if license has a specific tier
    /// </summary>
    /// <param name="tierName">Tier name to check (e.g., "Professional")</param>
    public bool HasTier(string tierName)
    {
        return _manager.HasTier(tierName);
    }

    /// <summary>
    /// Check if license tier meets minimum requirement
    /// Tier hierarchy: Basic -> Standard -> Professional -> Enterprise
    /// </summary>
    /// <param name="minimumTier">Minimum required tier</param>
    public bool HasMinimumTier(string minimumTier)
    {
        return _manager.HasMinimumTier(minimumTier);
    }

    /// <summary>
    /// Get all license features (raw feature strings)
    /// </summary>
    public List<string> GetFeatures()
    {
        return CurrentLicense?.Features ?? new List<string>();
    }

    #endregion

    #region License Information

    /// <summary>
    /// Get the license ID (e.g., "LIC-XXXX-XXXX-XXXX")
    /// </summary>
    public string? GetLicenseId()
    {
        return CurrentLicense?.LicenseId;
    }

    /// <summary>
    /// Get the support code for customer support verification
    /// </summary>
    public string? GetSupportCode()
    {
        return CurrentLicense?.SupportCode;
    }

    /// <summary>
    /// Get the brand name (for white-label editions)
    /// </summary>
    public string? GetBrand()
    {
        return CurrentLicense?.Brand;
    }

    /// <summary>
    /// Get the customer name from the license
    /// </summary>
    public string? GetCustomerName()
    {
        return CurrentLicense?.CustomerName;
    }

    /// <summary>
    /// Get the license expiration date
    /// </summary>
    public DateTime? GetExpirationDate()
    {
        return CurrentLicense?.ExpiresAt;
    }

    #endregion

    #region Deactivation

    /// <summary>
    /// Deactivate the current license (removes local license data)
    /// </summary>
    public void DeactivateLicense()
    {
        _heartbeatTimer.Stop();
        _expirationCheckTimer.Stop();
        
        _manager.Deactivate();
        CurrentLicense = null;
        _currentLicenseId = null;
        
        LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
        {
            IsValid = false,
            Status = LicenseStatus.NotActivated,
            Message = "License has been deactivated"
        });
    }

    #endregion

    private async Task<SessionStartResult> StartSessionAsync()
    {
        if (string.IsNullOrEmpty(_currentLicenseId) || _client == null)
            return new SessionStartResult { Success = false, Message = "No license or client" };

        return await _client.StartSessionAsync(_currentLicenseId);
    }

    private async void OnHeartbeatTimer(object? sender, ElapsedEventArgs e)
    {
        if (_client == null) return;

        try
        {
            var result = await _client.SessionHeartbeatAsync();
            if (!result.Success)
            {
                HeartbeatFailed?.Invoke(this, result.Message ?? "Heartbeat failed");
            }
            else if (!result.IsValid)
            {
                // License became invalid
                CurrentLicense = new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.Expired,
                    Message = "License is no longer valid"
                };

                LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
                {
                    IsValid = false,
                    Status = LicenseStatus.Expired,
                    Message = "Your license has expired or been revoked."
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
        if (CurrentLicense?.ExpiresAt == null) return;

        var daysRemaining = DaysUntilExpiration ?? 999;

        if (daysRemaining <= 0)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = 0,
                ExpiresAt = CurrentLicense.ExpiresAt.Value,
                Severity = ExpirationSeverity.Expired
            });
        }
        else if (daysRemaining <= 7)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = daysRemaining,
                ExpiresAt = CurrentLicense.ExpiresAt.Value,
                Severity = ExpirationSeverity.Critical
            });
        }
        else if (daysRemaining <= 30)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = daysRemaining,
                ExpiresAt = CurrentLicense.ExpiresAt.Value,
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
        _client?.Dispose();
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

public class SessionConflictEventArgs : EventArgs
{
    public string? ActiveDevice { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool CanForceTerminate { get; set; }
}

public class LicenseTransferredEventArgs : EventArgs
{
    public DateTime TransferredAt { get; set; }
    public string? Message { get; set; }
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
