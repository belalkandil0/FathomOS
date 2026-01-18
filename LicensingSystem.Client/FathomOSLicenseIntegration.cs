// LicensingSystem.Client/FathomOSLicenseIntegration.cs
// Complete FathomOS license integration with offline validation support
// v3.5.0 - Added ECDSA-signed offline license support

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Timers;
using LicensingSystem.Shared;
using LicensingSystem.Shared.Models;
using LicensingSystem.Shared.Services;
using LicensingSystem.Client.Services;
using Timer = System.Timers.Timer;

namespace LicensingSystem.Client;

/// <summary>
/// Complete FathomOS License Integration with support for:
/// - Online license activation and validation
/// - Offline ECDSA-signed license files (.lic)
/// - Compact license key strings
/// - Hardware fingerprint binding
/// - Grace period handling with warnings at 7, 3, 1 days
/// - Automatic expiration warnings
/// - Enhanced feature and module checking
/// - Multi-seat license support
/// - License usage tracking
/// - License transfer capabilities
///
/// Usage:
/// 1. Create instance with server URL (or null for offline-only)
/// 2. Call InitializeAsync() on startup
/// 3. Use LoadOfflineLicense() or ActivateLicenseAsync() to activate
/// 4. Check HasModule() / HasFeature() for feature gating
/// 5. Use ValidateWithServerAsync() for online validation
/// 6. Use ValidateOffline() for offline validation
/// 7. Use GetEnabledFeatures() to get all features
/// 8. Call ShutdownAsync() on app exit
/// </summary>
/// <example>
/// // Example usage in WPF App.xaml.cs:
/// public partial class App : Application
/// {
///     private FathomOSLicenseIntegration _license;
///
///     protected override async void OnStartup(StartupEventArgs e)
///     {
///         _license = new FathomOSLicenseIntegration("https://license.fathomos.com");
///         _license.LicenseStatusChanged += OnLicenseStatusChanged;
///         await _license.InitializeAsync();
///
///         if (!_license.IsLicensed)
///         {
///             // Show activation dialog
///             new ActivationWindow(_license).ShowDialog();
///         }
///     }
/// }
/// </example>
public class FathomOSLicenseIntegration : IDisposable
{
    private readonly LicenseManager? _onlineManager;
    private readonly LicenseVerifier _offlineVerifier;
    private readonly Timer _heartbeatTimer;
    private readonly Timer _expirationCheckTimer;
    private readonly string _productName;
    private readonly string _storagePath;
    private bool _disposed;

    // Cached offline license
    private OfflineLicense? _currentOfflineLicense;
    private OfflineLicenseValidationResult? _cachedOfflineResult;
    private DateTime _lastOfflineValidation = DateTime.MinValue;
    private readonly TimeSpan _offlineCacheTimeout = TimeSpan.FromMinutes(5);

    // Events for UI integration
    /// <summary>
    /// Fired when license is expiring soon (30, 7, or 0 days)
    /// </summary>
    public event EventHandler<LicenseExpirationEventArgs>? LicenseExpiringSoon;

    /// <summary>
    /// Fired when license status changes (valid/invalid/expired)
    /// </summary>
    public event EventHandler<LicenseStatusChangedEventArgs>? LicenseStatusChanged;

    /// <summary>
    /// Fired when heartbeat to server fails
    /// </summary>
    public event EventHandler<string>? HeartbeatFailed;

    /// <summary>
    /// Fired when an offline license is loaded
    /// </summary>
    public event EventHandler<OfflineLicenseLoadedEventArgs>? OfflineLicenseLoaded;

    /// <summary>
    /// Fired when grace period warning should be shown
    /// </summary>
    public event EventHandler<GracePeriodWarningEventArgs>? OnGracePeriodWarning;

    /// <summary>
    /// Access to the underlying online LicenseManager for backward compatibility.
    /// May be null if created in offline-only mode.
    /// </summary>
    public LicenseManager? InternalManager => _onlineManager;

    /// <summary>
    /// Current online license validation result (null if using offline license)
    /// </summary>
    public LicenseValidationResult? CurrentOnlineLicense => _onlineManager?.GetCachedResult();

    /// <summary>
    /// Current offline license (null if not loaded)
    /// </summary>
    public OfflineLicense? CurrentOfflineLicense => _currentOfflineLicense;

    /// <summary>
    /// Whether the application is currently licensed (online OR offline)
    /// </summary>
    public bool IsLicensed
    {
        get
        {
            // Check offline license first
            if (_currentOfflineLicense != null)
            {
                var result = ValidateOfflineLicense();
                return result.IsValid;
            }

            // Fall back to online license
            if (_onlineManager != null)
            {
                var result = _onlineManager.GetCachedResult() ?? _onlineManager.CheckLicense(forceRefresh: false);
                return result?.IsValid == true || result?.Status == LicenseStatus.GracePeriod;
            }

            return false;
        }
    }

    /// <summary>
    /// Days until license expiration (null if not licensed or lifetime)
    /// </summary>
    public int? DaysUntilExpiration
    {
        get
        {
            if (_currentOfflineLicense != null)
            {
                if (_currentOfflineLicense.IsLifetime) return null;
                return _currentOfflineLicense.DaysUntilExpiry;
            }

            var license = CurrentOnlineLicense?.License;
            if (license?.ExpiresAt == null) return null;
            var days = (license.ExpiresAt - DateTime.UtcNow).Days;
            return days < 0 ? 0 : days;
        }
    }

    /// <summary>
    /// Creates a new FathomOS license integration instance.
    /// </summary>
    /// <param name="serverUrl">License server URL (null for offline-only mode)</param>
    /// <param name="productName">Product name for validation (default: FathomOS)</param>
    /// <param name="customPublicKey">Custom public key for offline verification (null uses default)</param>
    public FathomOSLicenseIntegration(
        string? serverUrl = null,
        string productName = "FathomOS",
        string? customPublicKey = null)
    {
        _productName = productName;

        // Set up storage path
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            productName,
            "License");
        Directory.CreateDirectory(_storagePath);

        // Create online manager if server URL provided
        if (!string.IsNullOrEmpty(serverUrl))
        {
            _onlineManager = new LicenseManager(productName, serverUrl);
        }

        // Create offline verifier
        _offlineVerifier = new LicenseVerifier(customPublicKey);

        // Heartbeat timer - every 5 minutes
        _heartbeatTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _heartbeatTimer.Elapsed += OnHeartbeatTimer;
        _heartbeatTimer.AutoReset = true;

        // Expiration check timer - every hour
        _expirationCheckTimer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        _expirationCheckTimer.Elapsed += OnExpirationCheckTimer;
        _expirationCheckTimer.AutoReset = true;

        // Try to load stored offline license
        TryLoadStoredOfflineLicense();
    }

    #region Initialization and Lifecycle

    /// <summary>
    /// Initialize and validate the license on application startup.
    /// Checks both stored offline license and online license.
    /// </summary>
    /// <returns>License validation result (online or offline)</returns>
    public async Task<LicenseValidationResult> InitializeAsync()
    {
        try
        {
            // First check offline license
            if (_currentOfflineLicense != null)
            {
                var offlineResult = ValidateOfflineLicense();
                if (offlineResult.IsValid)
                {
                    _expirationCheckTimer.Start();
                    CheckExpirationWarning();

                    // Convert to LicenseValidationResult for API compatibility
                    var result = ConvertToLicenseValidationResult(offlineResult);

                    LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
                    {
                        IsValid = result.IsValid,
                        Status = result.Status,
                        Message = result.Message
                    });

                    return result;
                }
            }

            // Fall back to online validation
            if (_onlineManager != null)
            {
                var result = await _onlineManager.CheckLicenseAsync(forceRefresh: true);

                if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
                {
                    _heartbeatTimer.Start();
                    _expirationCheckTimer.Start();
                    CheckExpirationWarning();
                }

                LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
                {
                    IsValid = result.IsValid,
                    Status = result.Status,
                    Message = result.Message
                });

                return result;
            }

            // No license found
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "No license found. Please activate your copy."
            };
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = $"License initialization failed: {ex.Message}"
            };
        }
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

    #endregion

    #region Enhanced Validation Methods

    /// <summary>
    /// Validates the license with the server, optionally forcing online validation.
    /// Falls back to offline validation if server is unreachable.
    /// </summary>
    /// <param name="forceOnline">If true, requires online validation (will fail if offline)</param>
    /// <returns>License validation result with full details</returns>
    public async Task<LicenseValidationResult> ValidateWithServerAsync(bool forceOnline = false)
    {
        // First check offline license
        if (_currentOfflineLicense != null)
        {
            var offlineResult = ValidateOfflineLicense();

            // If forceOnline is true and we have an online manager, try server validation
            if (forceOnline && _onlineManager != null)
            {
                try
                {
                    var onlineResult = await _onlineManager.ForceServerCheckAsync();
                    if (onlineResult.Status != LicenseStatus.NotFound)
                    {
                        // Server responded, trust its result
                        return onlineResult;
                    }
                    // Server didn't recognize license, fall back to offline
                }
                catch
                {
                    if (forceOnline)
                    {
                        return new LicenseValidationResult
                        {
                            IsValid = false,
                            Status = LicenseStatus.Error,
                            Message = "Online validation required but server is unreachable."
                        };
                    }
                }
            }

            return ConvertToLicenseValidationResult(offlineResult);
        }

        // No offline license, try online validation
        if (_onlineManager != null)
        {
            try
            {
                var result = await _onlineManager.CheckLicenseAsync(forceRefresh: forceOnline);
                return result;
            }
            catch
            {
                if (forceOnline)
                {
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        Status = LicenseStatus.Error,
                        Message = "Online validation required but server is unreachable."
                    };
                }

                // Try cached result
                var cached = _onlineManager.GetCachedResult();
                if (cached != null)
                    return cached;
            }
        }

        return new LicenseValidationResult
        {
            IsValid = false,
            Status = LicenseStatus.NotFound,
            Message = "No license found. Please activate your copy."
        };
    }

    /// <summary>
    /// Validates the license offline only, without contacting the server.
    /// Uses cached offline license or stored online license.
    /// </summary>
    /// <returns>True if license is valid offline</returns>
    public bool ValidateOffline()
    {
        // Check offline license first
        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            return result.IsValid;
        }

        // Check if we have a stored online license that's still valid
        if (_onlineManager != null)
        {
            var cached = _onlineManager.GetCachedResult();
            if (cached != null && (cached.IsValid || cached.Status == LicenseStatus.GracePeriod))
            {
                // Check offline period hasn't expired
                // This is handled by the LicenseValidator internally
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all enabled features as a structured LicenseFeatures object.
    /// Includes modules, feature flags, tier information, and limits.
    /// </summary>
    /// <returns>LicenseFeatures object with all enabled features</returns>
    public LicenseFeatures GetEnabledFeatures()
    {
        var features = new LicenseFeatures();

        // Get raw feature list
        var featureList = new List<string>();

        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            if (result.IsValid)
            {
                featureList.AddRange(result.EnabledFeatures);
                featureList.AddRange(result.LicensedModules.Select(m => $"Module:{m}"));

                // Add tier from edition
                if (!string.IsNullOrEmpty(_currentOfflineLicense.Product.Edition))
                {
                    features.Tier = _currentOfflineLicense.Product.Edition;
                }

                // Add seats
                features.MaxSeats = _currentOfflineLicense.Product.Seats;

                // Add offline settings
                features.AllowOffline = _currentOfflineLicense.Terms.AllowOffline;
                features.MaxOfflineDays = _currentOfflineLicense.Terms.OfflineMaxDays;
            }
        }
        else if (_onlineManager != null)
        {
            var cached = _onlineManager.GetCachedResult();
            if (cached != null && (cached.IsValid || cached.Status == LicenseStatus.GracePeriod))
            {
                featureList.AddRange(cached.EnabledFeatures);
                if (cached.License?.Modules != null)
                {
                    featureList.AddRange(cached.License.Modules.Select(m => $"Module:{m}"));
                }

                if (!string.IsNullOrEmpty(cached.Edition))
                {
                    features.Tier = cached.Edition;
                }
            }
        }

        // Parse feature list into structured format
        foreach (var feature in featureList)
        {
            if (feature.StartsWith("Module:", StringComparison.OrdinalIgnoreCase))
            {
                var module = feature.Substring(7);
                if (!features.Modules.Contains(module, StringComparer.OrdinalIgnoreCase))
                    features.Modules.Add(module);
            }
            else if (feature.StartsWith("Tier:", StringComparison.OrdinalIgnoreCase))
            {
                features.Tier = feature.Substring(5);
            }
            else if (feature.StartsWith("Seats:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(feature.Substring(6), out var seats))
                    features.MaxSeats = seats;
            }
            else if (feature.StartsWith("Limit:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = feature.Substring(6).Split('=', 2);
                if (parts.Length == 2)
                    features.Limits[parts[0]] = parts[1];
            }
            else if (feature.Equals("AllowTransfer", StringComparison.OrdinalIgnoreCase))
            {
                features.AllowTransfer = true;
            }
            else
            {
                if (!features.Features.Contains(feature, StringComparer.OrdinalIgnoreCase))
                    features.Features.Add(feature);
            }
        }

        return features;
    }

    /// <summary>
    /// Checks if a specific feature is enabled in the current license.
    /// Performs case-insensitive comparison.
    /// </summary>
    /// <param name="featureName">Feature name to check (e.g., "Export:PDF", "AdvancedCharts")</param>
    /// <returns>True if the feature is enabled</returns>
    public bool IsFeatureEnabled(string featureName)
    {
        return HasFeature(featureName);
    }

    /// <summary>
    /// Gets the current grace period status if applicable.
    /// Returns null if not in grace period.
    /// </summary>
    /// <returns>Grace period warning info, or null if not in grace period</returns>
    public GracePeriodWarning? GetGracePeriodStatus()
    {
        int? graceDays = null;

        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            if (result.IsInGracePeriod)
            {
                graceDays = result.GraceDaysRemaining;
            }
        }
        else if (_onlineManager != null)
        {
            var cached = _onlineManager.GetCachedResult();
            if (cached?.Status == LicenseStatus.GracePeriod)
            {
                graceDays = cached.GraceDaysRemaining;
            }
        }

        if (graceDays.HasValue)
        {
            return GracePeriodWarning.Create(graceDays.Value);
        }

        return null;
    }

    /// <summary>
    /// Checks if the license is currently in grace period and fires warning events.
    /// Call this periodically to ensure users are notified of impending expiration.
    /// </summary>
    public void CheckGracePeriodWarnings()
    {
        var warning = GetGracePeriodStatus();
        if (warning != null)
        {
            OnGracePeriodWarning?.Invoke(this, new GracePeriodWarningEventArgs
            {
                Warning = warning
            });
        }
    }

    #endregion

    #region Offline License Operations

    /// <summary>
    /// Loads and validates an offline license from a file or license key string.
    /// Automatically detects the format (JSON .lic file or compact license key).
    /// </summary>
    /// <param name="pathOrKey">File path to .lic file, or license key string</param>
    /// <returns>Validation result</returns>
    /// <example>
    /// // Load from file:
    /// var result = integration.LoadOfflineLicense(@"C:\licenses\company.lic");
    ///
    /// // Load from key string:
    /// var result = integration.LoadOfflineLicense("FATHOM-PRO-H4sIAAA...-A7B2");
    /// </example>
    public OfflineLicenseValidationResult LoadOfflineLicense(string pathOrKey)
    {
        try
        {
            OfflineLicense license;

            // Check if it's a file path or license key
            if (File.Exists(pathOrKey))
            {
                // Load from file
                license = LicenseSerializer.LoadFromFile(pathOrKey);
            }
            else if (pathOrKey.TrimStart().StartsWith("{"))
            {
                // JSON string
                license = LicenseSerializer.FromJson(pathOrKey);
            }
            else
            {
                // Assume license key format
                license = LicenseSerializer.FromLicenseKey(pathOrKey);
            }

            // Validate the license
            var fingerprints = MachineFingerprint.Generate();
            var result = _offlineVerifier.Validate(license, fingerprints, _productName);

            if (result.IsValid)
            {
                // Store the license
                _currentOfflineLicense = license;
                _cachedOfflineResult = result;
                _lastOfflineValidation = DateTime.UtcNow;

                // Persist to storage
                StoreOfflineLicense(license);

                // Start expiration timer
                _expirationCheckTimer.Start();

                // Fire events
                OfflineLicenseLoaded?.Invoke(this, new OfflineLicenseLoadedEventArgs
                {
                    License = license,
                    Result = result
                });

                LicenseStatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs
                {
                    IsValid = true,
                    Status = result.IsInGracePeriod ? LicenseStatus.GracePeriod : LicenseStatus.Valid,
                    Message = result.IsInGracePeriod
                        ? $"License expired! {result.GraceDaysRemaining} grace days remaining."
                        : "License activated successfully."
                });
            }

            return result;
        }
        catch (LicenseSerializationException ex)
        {
            return OfflineLicenseValidationResult.Failure(
                $"Invalid license format: {ex.Message}",
                OfflineLicenseStatus.Corrupted);
        }
        catch (Exception ex)
        {
            return OfflineLicenseValidationResult.Failure(
                $"Failed to load license: {ex.Message}",
                OfflineLicenseStatus.Unknown);
        }
    }

    /// <summary>
    /// Validates the currently loaded offline license.
    /// Uses cached result if recently validated.
    /// </summary>
    /// <returns>Validation result</returns>
    public OfflineLicenseValidationResult ValidateOfflineLicense()
    {
        if (_currentOfflineLicense == null)
        {
            return OfflineLicenseValidationResult.Failure(
                "No offline license loaded",
                OfflineLicenseStatus.NotFound);
        }

        // Return cached result if recent
        if (_cachedOfflineResult != null &&
            DateTime.UtcNow - _lastOfflineValidation < _offlineCacheTimeout)
        {
            return _cachedOfflineResult;
        }

        // Re-validate
        var fingerprints = MachineFingerprint.Generate();
        var result = _offlineVerifier.Validate(_currentOfflineLicense, fingerprints, _productName);

        _cachedOfflineResult = result;
        _lastOfflineValidation = DateTime.UtcNow;

        return result;
    }

    /// <summary>
    /// Stores an offline license locally for persistence across sessions.
    /// The license is encrypted using Windows DPAPI.
    /// </summary>
    /// <param name="license">The license to store</param>
    public void StoreOfflineLicense(OfflineLicense license)
    {
        try
        {
            var json = LicenseSerializer.ToJson(license);
            var encrypted = ProtectData(json);
            var filePath = Path.Combine(_storagePath, "offline.dat");
            File.WriteAllText(filePath, Convert.ToBase64String(encrypted));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to store offline license: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a previously stored offline license.
    /// </summary>
    /// <returns>The stored license, or null if not found</returns>
    public OfflineLicense? LoadStoredOfflineLicense()
    {
        try
        {
            var filePath = Path.Combine(_storagePath, "offline.dat");
            if (!File.Exists(filePath))
                return null;

            var base64 = File.ReadAllText(filePath);
            var encrypted = Convert.FromBase64String(base64);
            var json = UnprotectData(encrypted);

            if (string.IsNullOrEmpty(json))
                return null;

            return LicenseSerializer.FromJson(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Clears the stored offline license.
    /// </summary>
    public void ClearStoredOfflineLicense()
    {
        try
        {
            var filePath = Path.Combine(_storagePath, "offline.dat");
            if (File.Exists(filePath))
                File.Delete(filePath);

            _currentOfflineLicense = null;
            _cachedOfflineResult = null;
        }
        catch { }
    }

    private void TryLoadStoredOfflineLicense()
    {
        var license = LoadStoredOfflineLicense();
        if (license != null)
        {
            _currentOfflineLicense = license;

            // Validate to populate cache
            ValidateOfflineLicense();
        }
    }

    #endregion

    #region Online License Operations

    /// <summary>
    /// Activate a license key online.
    /// </summary>
    /// <param name="licenseKey">The license key</param>
    /// <param name="email">Customer email</param>
    /// <returns>Validation result</returns>
    public async Task<LicenseValidationResult> ActivateLicenseAsync(string licenseKey, string email)
    {
        if (_onlineManager == null)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "Online activation not available. Use offline license file."
            };
        }

        try
        {
            var result = await _onlineManager.ActivateOnlineAsync(licenseKey, email);

            if (result.IsValid)
            {
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
    /// Activate an offline license file (.lic file) using online manager.
    /// For backward compatibility with existing code.
    /// </summary>
    /// <param name="licenseFilePath">Path to the .lic file</param>
    /// <returns>Validation result (as LicenseValidationResult)</returns>
    public LicenseValidationResult ActivateOffline(string licenseFilePath)
    {
        // Try new offline license format first
        var offlineResult = LoadOfflineLicense(licenseFilePath);
        if (offlineResult.IsValid || offlineResult.Status == OfflineLicenseStatus.GracePeriod)
        {
            return ConvertToLicenseValidationResult(offlineResult);
        }

        // Fall back to legacy online manager format
        if (_onlineManager != null)
        {
            try
            {
                var result = _onlineManager.ActivateFromFile(licenseFilePath);

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

        // Convert offline result to LicenseValidationResult
        return ConvertToLicenseValidationResult(offlineResult);
    }

    /// <summary>
    /// Refresh license status (call after external activation)
    /// </summary>
    public LicenseValidationResult RefreshLicenseStatus()
    {
        // Check offline first
        if (_currentOfflineLicense != null)
        {
            _cachedOfflineResult = null; // Force re-validation
            var offlineResult = ValidateOfflineLicense();
            return ConvertToLicenseValidationResult(offlineResult);
        }

        // Check online
        if (_onlineManager != null)
        {
            var result = _onlineManager.CheckLicense(forceRefresh: false);

            if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
            {
                if (!_heartbeatTimer.Enabled) _heartbeatTimer.Start();
                if (!_expirationCheckTimer.Enabled) _expirationCheckTimer.Start();
            }

            return result;
        }

        return new LicenseValidationResult
        {
            IsValid = false,
            Status = LicenseStatus.NotFound,
            Message = "No license found"
        };
    }

    #endregion

    #region Module and Feature Checking

    /// <summary>
    /// Check if a specific module is licensed.
    /// Works with both online and offline licenses.
    /// </summary>
    /// <param name="moduleId">Module ID (e.g., "SurveyListing")</param>
    /// <returns>True if module is licensed</returns>
    public bool HasModule(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            return false;

        // Check offline license first
        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            if (result.IsValid)
            {
                return result.LicensedModules.Contains(moduleId, StringComparer.OrdinalIgnoreCase);
            }
        }

        // Fall back to online license
        if (_onlineManager != null)
        {
            return _onlineManager.IsModuleLicensed(moduleId);
        }

        return false;
    }

    /// <summary>
    /// Check if a specific feature is enabled.
    /// Works with both online and offline licenses.
    /// </summary>
    /// <param name="featureName">Feature name (e.g., "Export:PDF")</param>
    /// <returns>True if feature is enabled</returns>
    public bool HasFeature(string featureName)
    {
        if (string.IsNullOrEmpty(featureName))
            return false;

        // Check offline license first
        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            if (result.IsValid)
            {
                return result.EnabledFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase);
            }
        }

        // Fall back to online license
        if (_onlineManager != null)
        {
            return _onlineManager.IsFeatureEnabled(featureName);
        }

        return false;
    }

    /// <summary>
    /// Get all licensed module IDs.
    /// </summary>
    public List<string> GetLicensedModules()
    {
        // Check offline license first
        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            if (result.IsValid)
            {
                return result.LicensedModules;
            }
        }

        // Fall back to online license
        if (_onlineManager != null)
        {
            return _onlineManager.GetLicensedModules();
        }

        return new List<string>();
    }

    /// <summary>
    /// Get all enabled features as a simple list of strings.
    /// For structured feature info, use GetEnabledFeatures() instead.
    /// </summary>
    public List<string> GetEnabledFeaturesList()
    {
        // Check offline license first
        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            if (result.IsValid)
            {
                return result.EnabledFeatures;
            }
        }

        // Fall back to online license
        if (_onlineManager != null)
        {
            var cached = _onlineManager.GetCachedResult();
            return cached?.EnabledFeatures ?? new List<string>();
        }

        return new List<string>();
    }

    #endregion

    #region Hardware and License Info

    /// <summary>
    /// Get current hardware ID for display purposes.
    /// Format: XXXX-XXXX-XXXX-XXXX
    /// </summary>
    public string GetHardwareId()
    {
        return MachineFingerprint.GenerateDisplayId();
    }

    /// <summary>
    /// Get hardware fingerprints for offline license generation.
    /// These are the full 32-character hashes needed in the License Generator.
    /// </summary>
    public List<string> GetHardwareFingerprints()
    {
        return MachineFingerprint.Generate();
    }

    /// <summary>
    /// Get license info for display in Help > License Info.
    /// Works with both online and offline licenses.
    /// </summary>
    public LicenseDisplayInfo GetLicenseDisplayInfo()
    {
        // Check offline license first
        if (_currentOfflineLicense != null)
        {
            var result = ValidateOfflineLicense();
            return new LicenseDisplayInfo
            {
                IsValid = result.IsValid,
                LicenseId = _currentOfflineLicense.Id,
                Edition = _currentOfflineLicense.Product.Edition,
                CustomerName = _currentOfflineLicense.Client.Name,
                CustomerEmail = _currentOfflineLicense.Client.Email,
                ExpiresAt = _currentOfflineLicense.IsLifetime ? null : _currentOfflineLicense.Terms.ExpiresAt,
                DaysRemaining = _currentOfflineLicense.IsLifetime ? null : result.DaysUntilExpiry,
                HardwareId = GetHardwareId(),
                Brand = _currentOfflineLicense.Brand,
                SupportCode = _currentOfflineLicense.SupportCode,
                Features = result.EnabledFeatures,
                Modules = result.LicensedModules,
                IsOfflineLicense = true
            };
        }

        // Fall back to online license
        if (_onlineManager != null)
        {
            var onlineResult = CurrentOnlineLicense ?? _onlineManager.CheckLicense(forceRefresh: false);
            var license = onlineResult?.License;

            return new LicenseDisplayInfo
            {
                IsValid = onlineResult?.IsValid ?? false,
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
                Modules = license?.Modules ?? new List<string>(),
                IsOfflineLicense = false
            };
        }

        return new LicenseDisplayInfo
        {
            IsValid = false,
            LicenseId = "Not Licensed",
            Edition = "Unknown",
            HardwareId = GetHardwareId(),
            IsOfflineLicense = false
        };
    }

    /// <summary>
    /// Get license info specific to offline licenses.
    /// </summary>
    public OfflineLicenseInfo? GetOfflineLicenseInfo()
    {
        if (_currentOfflineLicense == null)
            return null;

        var result = ValidateOfflineLicense();
        return new OfflineLicenseInfo
        {
            License = _currentOfflineLicense,
            ValidationResult = result,
            HardwareFingerprints = GetHardwareFingerprints(),
            DisplayId = GetHardwareId()
        };
    }

    #endregion

    #region Timer Handlers

    private async void OnHeartbeatTimer(object? sender, ElapsedEventArgs e)
    {
        if (_onlineManager == null) return;

        try
        {
            var result = await _onlineManager.CheckLicenseAsync(forceRefresh: true);

            if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            {
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
        DateTime? expiresAt = null;
        int daysRemaining = 999;

        // Get expiration from offline or online license
        if (_currentOfflineLicense != null)
        {
            if (_currentOfflineLicense.IsLifetime) return;
            expiresAt = _currentOfflineLicense.Terms.ExpiresAt;
            daysRemaining = _currentOfflineLicense.DaysUntilExpiry;
        }
        else if (CurrentOnlineLicense?.License != null)
        {
            expiresAt = CurrentOnlineLicense.License.ExpiresAt;
            daysRemaining = DaysUntilExpiration ?? 999;
        }

        if (expiresAt == null) return;

        if (daysRemaining <= 0)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = 0,
                ExpiresAt = expiresAt.Value,
                Severity = ExpirationSeverity.Expired
            });
        }
        else if (daysRemaining <= 7)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = daysRemaining,
                ExpiresAt = expiresAt.Value,
                Severity = ExpirationSeverity.Critical
            });
        }
        else if (daysRemaining <= 30)
        {
            LicenseExpiringSoon?.Invoke(this, new LicenseExpirationEventArgs
            {
                DaysRemaining = daysRemaining,
                ExpiresAt = expiresAt.Value,
                Severity = ExpirationSeverity.Warning
            });
        }
    }

    #endregion

    #region Helper Methods

    private LicenseValidationResult ConvertToLicenseValidationResult(OfflineLicenseValidationResult offlineResult)
    {
        return new LicenseValidationResult
        {
            IsValid = offlineResult.IsValid,
            Status = offlineResult.Status switch
            {
                OfflineLicenseStatus.Valid => LicenseStatus.Valid,
                OfflineLicenseStatus.Expired => LicenseStatus.Expired,
                OfflineLicenseStatus.GracePeriod => LicenseStatus.GracePeriod,
                OfflineLicenseStatus.InvalidSignature => LicenseStatus.InvalidSignature,
                OfflineLicenseStatus.HardwareMismatch => LicenseStatus.HardwareMismatch,
                OfflineLicenseStatus.NotFound => LicenseStatus.NotFound,
                OfflineLicenseStatus.Corrupted => LicenseStatus.Corrupted,
                OfflineLicenseStatus.Revoked => LicenseStatus.Revoked,
                _ => LicenseStatus.Error
            },
            Message = offlineResult.Error,
            DaysRemaining = offlineResult.DaysUntilExpiry,
            GraceDaysRemaining = offlineResult.GraceDaysRemaining,
            EnabledFeatures = offlineResult.EnabledFeatures
        };
    }

    private static readonly byte[] Entropy =
    {
        0x46, 0x41, 0x54, 0x48, 0x4F, 0x4D, 0x4F, 0x53,
        0x4C, 0x49, 0x43, 0x45, 0x4E, 0x53, 0x45, 0x21
    };

    private byte[] ProtectData(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
    }

    private string UnprotectData(byte[] encryptedData)
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

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _heartbeatTimer.Stop();
        _heartbeatTimer.Dispose();
        _expirationCheckTimer.Stop();
        _expirationCheckTimer.Dispose();
        _onlineManager?.Dispose();
    }

    #endregion
}

#region Event Args and Info Classes

/// <summary>
/// Event args for license expiration warnings
/// </summary>
public class LicenseExpirationEventArgs : EventArgs
{
    public int DaysRemaining { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ExpirationSeverity Severity { get; set; }
}

/// <summary>
/// Severity level for expiration warnings
/// </summary>
public enum ExpirationSeverity
{
    Warning,    // 30 days or less
    Critical,   // 7 days or less
    Expired     // 0 days
}

/// <summary>
/// Event args for license status changes
/// </summary>
public class LicenseStatusChangedEventArgs : EventArgs
{
    public bool IsValid { get; set; }
    public LicenseStatus Status { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Event args for offline license loaded
/// </summary>
public class OfflineLicenseLoadedEventArgs : EventArgs
{
    public OfflineLicense License { get; set; } = new();
    public OfflineLicenseValidationResult Result { get; set; } = new();
}

/// <summary>
/// Display information about the current license
/// </summary>
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
    public bool IsOfflineLicense { get; set; }

    /// <summary>
    /// Gets the display edition formatted with brand if available.
    /// </summary>
    public string DisplayEdition
    {
        get
        {
            if (!IsValid || string.IsNullOrEmpty(Brand))
                return "Fathom OS";
            return $"Fathom OS - {Brand} Edition";
        }
    }

    /// <summary>
    /// Gets a status text for display.
    /// </summary>
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

    /// <summary>
    /// Gets the license type description.
    /// </summary>
    public string LicenseType => IsOfflineLicense ? "Offline (Signed)" : "Online";
}

/// <summary>
/// Detailed information about an offline license
/// </summary>
public class OfflineLicenseInfo
{
    public OfflineLicense License { get; set; } = new();
    public OfflineLicenseValidationResult ValidationResult { get; set; } = new();
    public List<string> HardwareFingerprints { get; set; } = new();
    public string DisplayId { get; set; } = "";
}

/// <summary>
/// Event args for grace period warnings
/// </summary>
public class GracePeriodWarningEventArgs : EventArgs
{
    public GracePeriodWarning Warning { get; set; } = new();
}

/// <summary>
/// Grace period warning information
/// </summary>
public class GracePeriodWarning
{
    /// <summary>
    /// Days remaining in grace period
    /// </summary>
    public int DaysRemaining { get; set; }

    /// <summary>
    /// Warning severity level
    /// </summary>
    public GraceWarningSeverity Severity { get; set; }

    /// <summary>
    /// User-friendly warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Recommended action
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// Whether to show a modal dialog
    /// </summary>
    public bool ShowModal { get; set; }

    /// <summary>
    /// Creates appropriate warning based on days remaining
    /// </summary>
    public static GracePeriodWarning Create(int daysRemaining)
    {
        return daysRemaining switch
        {
            <= 0 => new GracePeriodWarning
            {
                DaysRemaining = 0,
                Severity = GraceWarningSeverity.Expired,
                Message = "Your license grace period has expired. FathomOS will now operate in limited mode.",
                RecommendedAction = "Please renew your license immediately to restore full functionality.",
                ShowModal = true
            },
            1 => new GracePeriodWarning
            {
                DaysRemaining = 1,
                Severity = GraceWarningSeverity.Critical,
                Message = "URGENT: Your license grace period expires TOMORROW!",
                RecommendedAction = "Renew your license now to avoid service interruption.",
                ShowModal = true
            },
            <= 3 => new GracePeriodWarning
            {
                DaysRemaining = daysRemaining,
                Severity = GraceWarningSeverity.Critical,
                Message = $"Your license grace period expires in {daysRemaining} days.",
                RecommendedAction = "Please renew your license as soon as possible.",
                ShowModal = true
            },
            <= 7 => new GracePeriodWarning
            {
                DaysRemaining = daysRemaining,
                Severity = GraceWarningSeverity.Warning,
                Message = $"Your license has expired. You have {daysRemaining} days remaining in the grace period.",
                RecommendedAction = "Please renew your license to continue using FathomOS.",
                ShowModal = true
            },
            _ => new GracePeriodWarning
            {
                DaysRemaining = daysRemaining,
                Severity = GraceWarningSeverity.Info,
                Message = $"Your license has expired. Grace period: {daysRemaining} days remaining.",
                RecommendedAction = "Consider renewing your license.",
                ShowModal = false
            }
        };
    }
}

/// <summary>
/// Grace period warning severity levels
/// </summary>
public enum GraceWarningSeverity
{
    /// <summary>Informational - more than 7 days remaining</summary>
    Info,

    /// <summary>Warning - 7 days or less remaining</summary>
    Warning,

    /// <summary>Critical - 3 days or less remaining</summary>
    Critical,

    /// <summary>Expired - grace period has ended</summary>
    Expired
}

#endregion
