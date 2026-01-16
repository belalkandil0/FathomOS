// LicensingSystem.Client/LicenseManager.cs
// Main entry point for license management in your application

using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using LicensingSystem.Shared;

namespace LicensingSystem.Client;

/// <summary>
/// Main license manager - use this class in your application
/// Combines offline and online validation with automatic fallback
/// </summary>
public class LicenseManager : IDisposable
{
    private readonly LicenseValidator _validator;
    private readonly LicenseClient? _client;
    private readonly LicenseStorage _storage;
    private readonly string _productName;
    private readonly string? _serverUrl;
    
    private LicenseValidationResult? _cachedResult;
    private DateTime _lastValidation = DateTime.MinValue;
    private DateTime _lastHeartbeat = DateTime.MinValue;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(1); // Check every 1 minute for revocation
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(30); // Heartbeat every 30 minutes
    private readonly TimeSpan _offlineGracePeriod = TimeSpan.FromHours(24); // Allow 24 hours offline before requiring server check
    private bool _disposed;
    private bool _offlineSyncPending = false;
    private const string SYNC_PENDING_KEY = "offline_sync_pending";
    private DateTime _lastSuccessfulServerCheck = DateTime.MinValue;

    /// <summary>
    /// Creates a license manager with optional online capabilities
    /// </summary>
    /// <param name="productName">Your product name</param>
    /// <param name="serverUrl">License server URL (null for offline-only)</param>
    public LicenseManager(string productName, string? serverUrl = null)
    {
        _productName = productName;
        _serverUrl = serverUrl;
        _validator = new LicenseValidator(productName);
        _storage = new LicenseStorage(productName);

        // Load persisted sync pending flag
        _offlineSyncPending = _storage.IsOfflineSyncPending();

        if (!string.IsNullOrEmpty(serverUrl))
        {
            _client = new LicenseClient(serverUrl, productName);
        }
    }

    /// <summary>
    /// Checks if the application is licensed
    /// Call this at startup and periodically during use
    /// </summary>
    public async Task<LicenseValidationResult> CheckLicenseAsync(bool forceRefresh = false)
    {
        // Return cached result if recent and not forced
        if (!forceRefresh && _cachedResult != null && 
            DateTime.UtcNow - _lastValidation < _cacheTimeout)
        {
            return _cachedResult;
        }

        // Check for clock tampering
        if (_storage.DetectClockTampering())
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = "System clock appears to have been modified. Please correct your system time."
            };
        }

        // Perform offline validation first (fast)
        var offlineResult = _validator.ValidateCurrentLicense();

        // If offline validation failed completely (no license), return immediately
        if (offlineResult.Status == LicenseStatus.NotFound)
        {
            _cachedResult = offlineResult;
            _lastValidation = DateTime.UtcNow;
            return offlineResult;
        }

        // If we have a client and should check online
        if (_client != null && ShouldCheckOnline())
        {
            try
            {
                // Try to sync offline license if pending
                if (_offlineSyncPending && offlineResult.License != null)
                {
                    await TrySyncOfflineLicenseAsync(offlineResult.License);
                }

                var onlineResult = await _client.ValidateOnlineAsync(
                    offlineResult.License?.LicenseId ?? "");

                if (onlineResult.IsValid)
                {
                    // Online validation succeeded
                    _cachedResult = onlineResult;
                    _lastValidation = DateTime.UtcNow;
                    _storage.UpdateLastOnlineCheck();
                    
                    // IMPORTANT: If server says valid, remove from local revocation list
                    // This handles reinstatement or false positive revocations
                    if (offlineResult.License != null)
                    {
                        _storage.RemoveFromRevocationList(offlineResult.License.LicenseId);
                    }
                    
                    // Send heartbeat if needed
                    await SendHeartbeatIfNeededAsync(offlineResult.License);
                    
                    return onlineResult;
                }

                // Server explicitly rejected the license - TRUST THE SERVER
                // This includes: Revoked, Expired, HardwareMismatch
                if (onlineResult.Status == LicenseStatus.Revoked ||
                    onlineResult.Status == LicenseStatus.Expired ||
                    onlineResult.Status == LicenseStatus.HardwareMismatch)
                {
                    // If REVOKED, add to local revocation list to prevent re-import
                    if (onlineResult.Status == LicenseStatus.Revoked && offlineResult.License != null)
                    {
                        _storage.AddToRevocationList(
                            offlineResult.License.LicenseId, 
                            onlineResult.Message ?? "Revoked by server");
                        
                        // Also delete the local license file
                        _storage.ClearLicense();
                    }
                    
                    _cachedResult = onlineResult;
                    _lastValidation = DateTime.UtcNow;
                    return onlineResult; // Don't fall back to offline!
                }

                // Server returned NotFound - license may not be synced yet
                // Only allow offline fallback if we haven't synced before
                if (onlineResult.Status == LicenseStatus.NotFound)
                {
                    // If offline sync is still pending, allow offline use
                    if (_offlineSyncPending)
                    {
                        if (offlineResult.IsValid || offlineResult.Status == LicenseStatus.GracePeriod)
                        {
                            offlineResult.Message += " (Pending server sync)";
                        }
                    }
                    else
                    {
                        // Sync already happened but server doesn't recognize license
                        // This is suspicious - could be a forged license
                        return new LicenseValidationResult
                        {
                            IsValid = false,
                            Status = LicenseStatus.InvalidSignature,
                            Message = "License not recognized by server. Please reactivate."
                        };
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Network error - allow offline use
                if (offlineResult.IsValid)
                {
                    offlineResult.Message += " (Offline mode - server unreachable)";
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout - allow offline use
                if (offlineResult.IsValid)
                {
                    offlineResult.Message += " (Offline mode - connection timeout)";
                }
            }
            catch
            {
                // Other error - allow offline use with warning
            }
        }

        _cachedResult = offlineResult;
        _lastValidation = DateTime.UtcNow;
        return offlineResult;
    }

    /// <summary>
    /// Synchronous version of CheckLicense for simple use cases
    /// </summary>
    public LicenseValidationResult CheckLicense(bool forceRefresh = false)
    {
        return CheckLicenseAsync(forceRefresh).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Activates a license using a license key (online)
    /// </summary>
    public async Task<LicenseValidationResult> ActivateOnlineAsync(
        string licenseKey, 
        string email,
        string appVersion = "1.0")
    {
        if (_client == null)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "Online activation is not configured. Please use a license file."
            };
        }

        var result = await _client.ActivateOnlineAsync(licenseKey, email, appVersion);
        
        if (result.IsValid)
        {
            _cachedResult = result;
            _lastValidation = DateTime.UtcNow;
            _offlineSyncPending = false; // Activated online, no sync needed
            
            // IMPORTANT: Remove from local revocation list if it was there
            // This allows re-activation after a false revocation or reinstatement
            if (result.License != null)
            {
                _storage.RemoveFromRevocationList(result.License.LicenseId);
                _storage.RecordFirstActivation(result.License.LicenseId);
            }
        }

        return result;
    }

    /// <summary>
    /// Activates a license from a file (offline)
    /// </summary>
    public LicenseValidationResult ActivateFromFile(string licenseFilePath)
    {
        // IMPORTANT: Pre-read the license file to get the LicenseId
        // so we can remove it from revocation list BEFORE validation
        // This allows re-activation of previously revoked licenses
        try
        {
            var json = System.IO.File.ReadAllText(licenseFilePath);
            var signedLicense = System.Text.Json.JsonSerializer.Deserialize<SignedLicense>(json);
            if (signedLicense?.License != null)
            {
                // Remove from revocation list before validation
                // This allows re-activation after reinstatement or false positives
                _storage.RemoveFromRevocationList(signedLicense.License.LicenseId);
            }
        }
        catch
        {
            // If we can't read the file, let the validator handle the error
        }
        
        var result = _validator.ActivateFromFile(licenseFilePath);
        
        if (result.IsValid)
        {
            _cachedResult = result;
            _lastValidation = DateTime.UtcNow;
            _offlineSyncPending = true; // Mark for sync when internet is available
            
            // Persist the sync pending flag so it survives app restart
            _storage.SetOfflineSyncPending(true, result.License?.LicenseId);
            
            // Record first activation for anti-reset attack protection
            if (result.License != null)
            {
                _storage.RecordFirstActivation(result.License.LicenseId);
            }
        }

        return result;
    }

    /// <summary>
    /// Forces an immediate server check for license status.
    /// Use this to quickly detect revocation/expiration.
    /// Call this periodically (e.g., every 5-10 minutes) in your app.
    /// Returns cached result if offline - doesn't block the user.
    /// </summary>
    /// <returns>License validation result (safe for offline use)</returns>
    public async Task<LicenseValidationResult> ForceServerCheckAsync()
    {
        if (_client == null)
        {
            // No server configured, return cached result
            return _cachedResult ?? new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "No license found"
            };
        }

        try
        {
            var currentLicense = _validator.ValidateCurrentLicense();
            if (currentLicense.License == null)
            {
                return currentLicense;
            }

            // Force online validation
            var onlineResult = await _client.ValidateOnlineAsync(currentLicense.License.LicenseId);
            
            // If server returned a time, check for clock tampering
            if (onlineResult.ServerTime != default)
            {
                if (_storage.DetectClockTamperingWithServerTime(onlineResult.ServerTime))
                {
                    // Clock tampering detected - but only warn, don't block
                    // The server time comparison is informational
                    System.Diagnostics.Debug.WriteLine("Warning: System clock may be incorrect");
                }
            }
            
            // Only update cache if we got a valid response
            if (onlineResult.Status != LicenseStatus.NotFound || onlineResult.IsValid == false)
            {
                _cachedResult = onlineResult;
                _lastValidation = DateTime.UtcNow;
                _lastSuccessfulServerCheck = DateTime.UtcNow;
                _storage.UpdateLastOnlineCheck();
            }

            return onlineResult;
        }
        catch (HttpRequestException)
        {
            // Network error - user is OFFLINE
            // Return last known good state - DON'T block the user!
            return _cachedResult ?? _validator.ValidateCurrentLicense();
        }
        catch (TaskCanceledException)
        {
            // Timeout - user might be offline or server slow
            // Return last known good state
            return _cachedResult ?? _validator.ValidateCurrentLicense();
        }
        catch (Exception ex)
        {
            // Other error - still don't block offline users
            System.Diagnostics.Debug.WriteLine($"Server check error: {ex.Message}");
            return _cachedResult ?? _validator.ValidateCurrentLicense();
        }
    }

    /// <summary>
    /// Checks if the license has been revoked (quick server check)
    /// Returns true ONLY if server confirms revocation.
    /// Returns false if offline (benefit of doubt to user).
    /// </summary>
    public async Task<bool> IsRevokedAsync()
    {
        try
        {
            var result = await ForceServerCheckAsync();
            
            // Only return true if we actually confirmed revocation from server
            return result.Status == LicenseStatus.Revoked;
        }
        catch
        {
            // If we can't reach server, don't assume revoked
            return false;
        }
    }

    /// <summary>
    /// Checks if user is currently online (can reach license server)
    /// </summary>
    public async Task<bool> IsOnlineAsync()
    {
        if (_client == null) return false;
        
        try
        {
            // Simple connectivity check with short timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var currentLicense = _validator.ValidateCurrentLicense();
            if (currentLicense.License == null) return false;
            
            await _client.ValidateOnlineAsync(currentLicense.License.LicenseId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the hardware ID for display to user (short format: XXXX-XXXX-XXXX-XXXX)
    /// NOTE: This is for DISPLAY only - do NOT use for offline license creation!
    /// </summary>
    public string GetHardwareId()
    {
        return _validator.GetHardwareId();
    }

    /// <summary>
    /// Gets all hardware fingerprints for OFFLINE LICENSE creation.
    /// These are the full 32-character hashes that must be entered in the License Generator.
    /// Returns one fingerprint per line for easy copy/paste.
    /// </summary>
    public string GetHardwareFingerprintsForLicense()
    {
        var fingerprints = _validator.GetHardwareFingerprints();
        return string.Join(Environment.NewLine, fingerprints);
    }

    /// <summary>
    /// Gets all hardware fingerprints as a list.
    /// Use this for programmatic access to fingerprints.
    /// </summary>
    public List<string> GetHardwareFingerprintsList()
    {
        return _validator.GetHardwareFingerprints();
    }

    /// <summary>
    /// DIAGNOSTIC: Compares current hardware fingerprints with those in a license file.
    /// Use this to debug "hardware mismatch" errors.
    /// </summary>
    public string DiagnoseHardwareMismatch(string? licenseFilePath = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== HARDWARE FINGERPRINT DIAGNOSTIC ===");
        sb.AppendLine();
        
        // Get current fingerprints
        var currentFingerprints = _validator.GetHardwareFingerprints();
        sb.AppendLine($"CURRENT HARDWARE FINGERPRINTS ({currentFingerprints.Count} total):");
        for (int i = 0; i < currentFingerprints.Count; i++)
        {
            sb.AppendLine($"  [{i}] {currentFingerprints[i]}");
        }
        sb.AppendLine();
        
        // If license file provided, load and compare
        SignedLicense? signedLicense = null;
        
        if (!string.IsNullOrEmpty(licenseFilePath) && File.Exists(licenseFilePath))
        {
            try
            {
                var json = File.ReadAllText(licenseFilePath);
                signedLicense = System.Text.Json.JsonSerializer.Deserialize<SignedLicense>(json);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"ERROR reading license file: {ex.Message}");
            }
        }
        else
        {
            // Try to load stored license
            signedLicense = _storage.LoadLicense();
        }
        
        if (signedLicense?.License != null)
        {
            var storedFingerprints = signedLicense.License.HardwareFingerprints;
            sb.AppendLine($"LICENSE FILE FINGERPRINTS ({storedFingerprints.Count} total):");
            for (int i = 0; i < storedFingerprints.Count; i++)
            {
                sb.AppendLine($"  [{i}] {storedFingerprints[i]}");
            }
            sb.AppendLine();
            
            // Count matches
            int matchCount = 0;
            sb.AppendLine("MATCHING ANALYSIS:");
            foreach (var stored in storedFingerprints)
            {
                bool found = currentFingerprints.Any(c => 
                    string.Equals(c, stored, StringComparison.OrdinalIgnoreCase));
                if (found)
                {
                    matchCount++;
                    sb.AppendLine($"  ✅ MATCH: {stored}");
                }
                else
                {
                    sb.AppendLine($"  ❌ NO MATCH: {stored}");
                }
            }
            sb.AppendLine();
            sb.AppendLine($"RESULT: {matchCount} matches out of {storedFingerprints.Count} stored fingerprints");
            sb.AppendLine($"THRESHOLD: {signedLicense.License.FingerprintMatchThreshold} required");
            sb.AppendLine($"VERDICT: {(matchCount >= signedLicense.License.FingerprintMatchThreshold ? "WOULD PASS ✅" : "WOULD FAIL ❌")}");
        }
        else
        {
            sb.AppendLine("No license file found to compare against.");
        }
        
        sb.AppendLine();
        sb.AppendLine("=== END DIAGNOSTIC ===");
        
        return sb.ToString();
    }

    /// <summary>
    /// Gets the current license status for UI display
    /// </summary>
    public LicenseStatusInfo GetStatusInfo()
    {
        var result = _cachedResult ?? _validator.ValidateCurrentLicense();

        // IMPORTANT: No trial mode - if no valid license, Edition is "UNLICENSED"
        string edition = "UNLICENSED";
        if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
        {
            edition = result.License?.Edition ?? "STANDARD";
        }

        return new LicenseStatusInfo
        {
            IsLicensed = result.IsValid || result.Status == LicenseStatus.GracePeriod,
            Status = result.Status,
            StatusMessage = result.Message,
            CustomerName = result.License?.CustomerName,
            CustomerEmail = result.License?.CustomerEmail,
            Edition = edition,
            ExpiresAt = result.License?.ExpiresAt,
            DaysRemaining = result.DaysRemaining,
            GraceDaysRemaining = result.GraceDaysRemaining,
            Features = result.EnabledFeatures,
            HardwareId = GetHardwareId(),
            HardwareFingerprints = GetHardwareFingerprintsList(),
            // Branding info (New for Fathom OS)
            Brand = result.License?.Brand,
            LicenseeCode = result.License?.LicenseeCode,
            SupportCode = result.License?.SupportCode
        };
    }

    /// <summary>
    /// Checks if a specific feature is enabled
    /// </summary>
    public bool IsFeatureEnabled(string featureName)
    {
        var result = _cachedResult ?? _validator.ValidateCurrentLicense();
        
        if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            return false;

        return result.EnabledFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase);
    }

    #region Module-Based Licensing
    
    /// <summary>
    /// Checks if a specific module is licensed.
    /// Module features follow the naming convention: "Module:{moduleId}"
    /// Example: IsModuleLicensed("SurveyListing") checks for "Module:SurveyListing"
    /// </summary>
    /// <param name="moduleId">The module ID (PascalCase, e.g., "SurveyListing", "TideAnalysis")</param>
    /// <returns>True if the module is licensed</returns>
    public bool IsModuleLicensed(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            return false;

        var result = _cachedResult ?? _validator.ValidateCurrentLicense();
        
        if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            return false;

        // Check for direct module feature: "Module:SurveyListing"
        var moduleFeature = $"Module:{moduleId}";
        return result.EnabledFeatures.Any(f => 
            f.Equals(moduleFeature, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all licensed module IDs from the current license.
    /// Returns empty list if not licensed or no modules found.
    /// </summary>
    /// <returns>List of module IDs (e.g., ["SurveyListing", "TideAnalysis"])</returns>
    public List<string> GetLicensedModules()
    {
        var result = _cachedResult ?? _validator.ValidateCurrentLicense();
        
        if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            return new List<string>();

        // Extract module IDs from "Module:XXX" features
        return result.EnabledFeatures
            .Where(f => f.StartsWith("Module:", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Substring(7)) // Remove "Module:" prefix
            .ToList();
    }

    /// <summary>
    /// Gets the current license tier (e.g., "Basic", "Professional", "Enterprise").
    /// Tier features follow the naming convention: "Tier:{tierName}"
    /// Returns null if no tier found or not licensed.
    /// </summary>
    /// <returns>Tier name or null</returns>
    public string? GetLicenseTier()
    {
        var result = _cachedResult ?? _validator.ValidateCurrentLicense();
        
        if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            return null;

        // Find "Tier:XXX" feature
        var tierFeature = result.EnabledFeatures
            .FirstOrDefault(f => f.StartsWith("Tier:", StringComparison.OrdinalIgnoreCase));
        
        if (tierFeature == null)
            return null;

        return tierFeature.Substring(5); // Remove "Tier:" prefix
    }

    /// <summary>
    /// Checks if the license has a specific tier.
    /// </summary>
    /// <param name="tierName">Tier name (e.g., "Professional")</param>
    /// <returns>True if the license has this tier</returns>
    public bool HasTier(string tierName)
    {
        if (string.IsNullOrEmpty(tierName))
            return false;

        var currentTier = GetLicenseTier();
        return currentTier != null && 
               currentTier.Equals(tierName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the license is at least a certain tier level.
    /// Tier hierarchy: Basic (1) < Professional (2) < Enterprise (3)
    /// Custom tier is treated as level 0.
    /// </summary>
    /// <param name="minimumTier">Minimum required tier</param>
    /// <returns>True if current tier meets or exceeds minimum</returns>
    public bool HasMinimumTier(string minimumTier)
    {
        var currentTier = GetLicenseTier();
        if (currentTier == null)
            return false;

        int currentLevel = GetTierLevel(currentTier);
        int requiredLevel = GetTierLevel(minimumTier);
        
        return currentLevel >= requiredLevel;
    }

    private static int GetTierLevel(string tierName)
    {
        return tierName?.ToLowerInvariant() switch
        {
            "basic" => 1,
            "professional" or "pro" => 2,
            "enterprise" => 3,
            _ => 0 // Custom or unknown
        };
    }

    #endregion

    /// <summary>
    /// Deactivates the current license
    /// </summary>
    public void Deactivate()
    {
        _validator.RemoveLicense();
        _cachedResult = null;
        _offlineSyncPending = false;
    }

    /// <summary>
    /// Manually trigger sync of offline license to server
    /// Call this when you detect internet connectivity
    /// </summary>
    public async Task<bool> SyncOfflineLicenseAsync()
    {
        if (_client == null || !_offlineSyncPending)
            return false;

        var result = _validator.ValidateCurrentLicense();
        if (result.License == null)
            return false;

        return await TrySyncOfflineLicenseAsync(result.License);
    }

    /// <summary>
    /// Send heartbeat to server to report status
    /// </summary>
    public async Task<bool> SendHeartbeatAsync()
    {
        if (_client == null)
            return false;

        var result = _validator.ValidateCurrentLicense();
        if (result.License == null)
            return false;

        return await SendHeartbeatIfNeededAsync(result.License, force: true);
    }

    private async Task<bool> TrySyncOfflineLicenseAsync(LicenseFile license)
    {
        if (_client == null || string.IsNullOrEmpty(_serverUrl))
            return false;

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var syncRequest = new
            {
                LicenseId = license.LicenseId,
                HardwareFingerprint = string.Join("-", license.HardwareFingerprints.Take(4)),
                ProductName = license.Product,
                Edition = license.Edition,
                Features = string.Join(",", license.Features),
                CustomerEmail = license.CustomerEmail,
                CustomerName = license.CustomerName,
                CreatedAt = license.IssuedAt,
                ExpiresAt = license.ExpiresAt,
                ActivatedAt = DateTime.UtcNow,
                AppVersion = GetAppVersion(),
                OsVersion = Environment.OSVersion.ToString(),
                MachineName = Environment.MachineName
            };

            var json = JsonSerializer.Serialize(syncRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{_serverUrl}/api/license/sync-offline", content);
            
            if (response.IsSuccessStatusCode)
            {
                _offlineSyncPending = false;
                _storage.SetOfflineSyncPending(false); // Clear persisted flag
                return true;
            }
        }
        catch
        {
            // Sync failed, will retry later
        }

        return false;
    }

    private async Task<bool> SendHeartbeatIfNeededAsync(LicenseFile? license, bool force = false)
    {
        if (_client == null || license == null || string.IsNullOrEmpty(_serverUrl))
            return false;

        // Check if heartbeat is needed
        if (!force && DateTime.UtcNow - _lastHeartbeat < _heartbeatInterval)
            return true; // Already sent recently

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var heartbeatRequest = new
            {
                LicenseId = license.LicenseId,
                HardwareFingerprint = string.Join("-", license.HardwareFingerprints.Take(4)),
                AppVersion = GetAppVersion(),
                OsVersion = Environment.OSVersion.ToString()
            };

            var json = JsonSerializer.Serialize(heartbeatRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{_serverUrl}/api/license/heartbeat", content);
            
            if (response.IsSuccessStatusCode)
            {
                _lastHeartbeat = DateTime.UtcNow;
                
                // Check if server says license is revoked
                var responseJson = await response.Content.ReadAsStringAsync();
                var heartbeatResponse = JsonSerializer.Deserialize<HeartbeatResponseDto>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (heartbeatResponse?.IsRevoked == true && license != null)
                {
                    // License was revoked on server - add to local revocation list
                    _storage.AddToRevocationList(
                        license.LicenseId, 
                        heartbeatResponse.RevokeReason ?? "Revoked by server (heartbeat)");
                    
                    // Delete local license
                    _storage.ClearLicense();
                    
                    // Update cached result
                    _cachedResult = new LicenseValidationResult
                    {
                        IsValid = false,
                        Status = LicenseStatus.Revoked,
                        Message = "This license has been revoked."
                    };
                }
                else
                {
                    // We're online! Sync and verify pending certificates in background
                    _ = SyncAndVerifyPendingCertificatesAsync();
                }
                
                return true;
            }
        }
        catch
        {
            // Heartbeat failed, will retry later
        }

        return false;
    }

    private bool ShouldCheckOnline()
    {
        var lastOnline = _storage.GetLastOnlineCheck();
        var timeSinceOnline = DateTime.UtcNow - lastOnline;
        
        // Check online frequently for revocation detection (every 30 minutes)
        // This ensures revoked licenses are detected quickly
        return timeSinceOnline.TotalMinutes >= 30;
    }

    private string GetAppVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            return assembly?.GetName().Version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    #region Troubleshooting Helpers

    /// <summary>
    /// Checks if a specific license is in the local revocation list.
    /// Use this to diagnose "stuck revoked" issues.
    /// </summary>
    public bool IsInLocalRevocationList(string licenseId)
    {
        return _storage.IsLicenseRevoked(licenseId);
    }

    /// <summary>
    /// Gets all license IDs in the local revocation list.
    /// Use for diagnostics.
    /// </summary>
    public List<string> GetLocallyRevokedLicenseIds()
    {
        return _storage.GetRevokedLicenseIds();
    }

    /// <summary>
    /// Removes a specific license from the local revocation list.
    /// Use this when a license was falsely marked as revoked.
    /// </summary>
    public void ClearLocalRevocationForLicense(string licenseId)
    {
        _storage.RemoveFromRevocationList(licenseId);
    }

    /// <summary>
    /// Clears the entire local revocation list.
    /// USE WITH CAUTION - this allows all previously revoked licenses to be re-used.
    /// </summary>
    public void ClearAllLocalRevocations()
    {
        _storage.ClearRevocationList();
    }

    /// <summary>
    /// Gets the reason why a license was locally revoked.
    /// Returns null if the license is not in the local revocation list.
    /// </summary>
    public string? GetLocalRevocationReason(string licenseId)
    {
        return _storage.GetRevocationReason(licenseId);
    }

    /// <summary>
    /// Gets comprehensive diagnostic information about the current license state.
    /// Use this to troubleshoot license issues.
    /// </summary>
    public LicenseDiagnostics GetDiagnostics()
    {
        var diag = new LicenseDiagnostics();
        
        try
        {
            // Check stored license
            var storedLicense = _storage.LoadLicense();
            diag.HasStoredLicense = storedLicense != null;
            diag.StoredLicenseId = storedLicense?.License?.LicenseId;
            diag.StoredLicenseExpiry = storedLicense?.License?.ExpiresAt;
            diag.StoredLicenseFeatures = storedLicense?.License?.Features;
            
            // Check revocation list
            if (storedLicense?.License != null)
            {
                diag.IsInLocalRevocationList = _storage.IsLicenseRevoked(storedLicense.License.LicenseId);
                diag.LocalRevocationReason = _storage.GetRevocationReason(storedLicense.License.LicenseId);
            }
            diag.AllLocallyRevokedIds = _storage.GetRevokedLicenseIds();
            
            // Check last online check
            diag.LastOnlineCheck = _storage.GetLastOnlineCheck();
            diag.TimeSinceOnlineCheck = DateTime.UtcNow - diag.LastOnlineCheck;
            diag.IsOfflineSyncPending = _offlineSyncPending;
            
            // Check cached result
            diag.HasCachedResult = _cachedResult != null;
            diag.CachedStatus = _cachedResult?.Status.ToString();
            diag.CachedIsValid = _cachedResult?.IsValid ?? false;
            
            // Storage paths
            diag.LicenseFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _productName, "License", "license.dat");
            diag.RevocationFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _productName, "License", "revoked.dat");
            
            diag.LicenseFileExists = File.Exists(diag.LicenseFilePath);
            diag.RevocationFileExists = File.Exists(diag.RevocationFilePath);
            
            // Server URL
            diag.ServerUrl = _serverUrl;
            diag.HasOnlineCapability = _client != null;
        }
        catch (Exception ex)
        {
            diag.DiagnosticError = ex.Message;
        }
        
        return diag;
    }

    #endregion

    #region Certificate Methods (New for Fathom OS)

    /// <summary>
    /// Syncs certificates to the server.
    /// Call this periodically when online to upload pending certificates.
    /// </summary>
    /// <param name="certificates">List of certificates to sync</param>
    /// <returns>Sync result with success status and synced count</returns>
    public async Task<CertificateSyncResponse> SyncCertificatesAsync(List<ProcessingCertificate> certificates)
    {
        if (_client == null)
        {
            return new CertificateSyncResponse
            {
                Success = false,
                Message = "No server URL configured - cannot sync certificates"
            };
        }

        var license = _storage.LoadLicense();
        if (license?.License == null)
        {
            return new CertificateSyncResponse
            {
                Success = false,
                Message = "No license loaded - cannot sync certificates"
            };
        }

        return await _client.SyncCertificatesAsync(license.License.LicenseId, certificates);
    }

    /// <summary>
    /// Verifies a certificate by ID with the server.
    /// </summary>
    /// <param name="certificateId">Certificate ID to verify</param>
    /// <returns>Verification result</returns>
    public async Task<CertificateVerificationResult> VerifyCertificateAsync(string certificateId)
    {
        if (_client == null)
        {
            return new CertificateVerificationResult
            {
                IsValid = false,
                CertificateId = certificateId,
                Message = "No server URL configured - cannot verify certificate"
            };
        }

        return await _client.VerifyCertificateAsync(certificateId);
    }

    /// <summary>
    /// Gets the next certificate ID from the server.
    /// Use this when online to get server-assigned certificate IDs.
    /// </summary>
    /// <returns>Tuple with CertificateId, SequenceNumber, and Error message if failed</returns>
    public async Task<(string? CertificateId, int SequenceNumber, string? Error)> GetNextCertificateIdAsync()
    {
        if (_client == null)
        {
            return (null, 0, "No server URL configured");
        }

        var license = _storage.LoadLicense();
        var licenseeCode = license?.License?.LicenseeCode;
        
        if (string.IsNullOrEmpty(licenseeCode))
        {
            return (null, 0, "No licensee code in license - cannot generate certificate ID");
        }

        return await _client.GetNextCertificateIdAsync(licenseeCode);
    }

    /// <summary>
    /// Generates a local certificate ID (for offline use).
    /// Use GetNextCertificateIdAsync when online for server-assigned IDs.
    /// </summary>
    /// <param name="localSequence">Local sequence number to use</param>
    /// <returns>Certificate ID or null if no licensee code</returns>
    public string? GenerateLocalCertificateId(int localSequence)
    {
        var license = _storage.LoadLicense();
        var licenseeCode = license?.License?.LicenseeCode;
        
        if (string.IsNullOrEmpty(licenseeCode))
        {
            return null;
        }

        return LicenseConstants.GenerateCertificateId(licenseeCode, localSequence, DateTime.UtcNow);
    }

    #region Offline Certificate Management

    /// <summary>
    /// Creates a processing certificate and stores it locally.
    /// Works offline - will sync to server when online.
    /// </summary>
    /// <param name="moduleId">Module ID (e.g., "SurveyListing")</param>
    /// <param name="moduleCertificateCode">Module certificate code (e.g., "SL")</param>
    /// <param name="moduleVersion">Module version</param>
    /// <param name="projectName">Project name</param>
    /// <param name="signatoryName">Name of person signing</param>
    /// <param name="companyName">Company name</param>
    /// <param name="processingData">Processing-specific data</param>
    /// <param name="inputFiles">List of input files</param>
    /// <param name="outputFiles">List of output files</param>
    /// <param name="projectLocation">Optional project location</param>
    /// <param name="vessel">Optional vessel name</param>
    /// <param name="client">Optional client name</param>
    /// <param name="signatoryTitle">Optional signatory title</param>
    /// <returns>The created certificate (stored locally, will sync when online)</returns>
    public async Task<ProcessingCertificate> CreateCertificateAsync(
        string moduleId,
        string moduleCertificateCode,
        string moduleVersion,
        string projectName,
        string signatoryName,
        string companyName,
        Dictionary<string, string>? processingData = null,
        List<string>? inputFiles = null,
        List<string>? outputFiles = null,
        string? projectLocation = null,
        string? vessel = null,
        string? client = null,
        string? signatoryTitle = null)
    {
        var license = _storage.LoadLicense();
        var licenseeCode = license?.License?.LicenseeCode ?? "XX";

        // Try to get certificate ID from server first (if online)
        string certificateId;
        bool gotServerSequence = false;
        
        if (_client != null)
        {
            try
            {
                var (serverId, _, error) = await _client.GetNextCertificateIdAsync(licenseeCode);
                if (!string.IsNullOrEmpty(serverId) && string.IsNullOrEmpty(error))
                {
                    certificateId = serverId;
                    gotServerSequence = true;
                }
                else
                {
                    // Fallback to local
                    var localSeq = _storage.GetNextLocalCertificateSequence();
                    certificateId = LicenseConstants.GenerateCertificateId(licenseeCode, localSeq, DateTime.UtcNow);
                }
            }
            catch
            {
                // Offline - use local sequence
                var localSeq = _storage.GetNextLocalCertificateSequence();
                certificateId = LicenseConstants.GenerateCertificateId(licenseeCode, localSeq, DateTime.UtcNow);
            }
        }
        else
        {
            // No client - use local sequence
            var localSeq = _storage.GetNextLocalCertificateSequence();
            certificateId = LicenseConstants.GenerateCertificateId(licenseeCode, localSeq, DateTime.UtcNow);
        }

        // Create the certificate (unsigned - server will verify based on record)
        var certificate = new ProcessingCertificate
        {
            CertificateId = certificateId,
            LicenseeCode = licenseeCode,
            ModuleId = moduleId,
            ModuleCertificateCode = moduleCertificateCode,
            ModuleVersion = moduleVersion,
            IssuedAt = DateTime.UtcNow,
            ProjectName = projectName,
            ProjectLocation = projectLocation,
            Vessel = vessel,
            Client = client,
            SignatoryName = signatoryName,
            SignatoryTitle = signatoryTitle,
            CompanyName = companyName,
            ProcessingData = processingData ?? new Dictionary<string, string>(),
            InputFiles = inputFiles ?? new List<string>(),
            OutputFiles = outputFiles ?? new List<string>(),
            // No signature - certificates are unsigned in Option 1
            Signature = null,
            SignatureAlgorithm = null
        };

        // Store locally
        _storage.SaveCertificateLocally(certificate);

        // If we got server sequence, try to sync immediately
        if (gotServerSequence && _client != null)
        {
            try
            {
                await SyncSingleCertificateAsync(certificate);
            }
            catch
            {
                // Will sync later
            }
        }

        System.Diagnostics.Debug.WriteLine($"Created certificate {certificateId} (server sequence: {gotServerSequence})");
        return certificate;
    }

    /// <summary>
    /// Syncs a single certificate to the server and marks it as synced locally.
    /// </summary>
    private async Task<bool> SyncSingleCertificateAsync(ProcessingCertificate certificate)
    {
        if (_client == null) return false;

        var license = _storage.LoadLicense();
        if (license?.License == null) return false;

        try
        {
            var response = await _client.SyncCertificatesAsync(
                license.License.LicenseId, 
                new List<ProcessingCertificate> { certificate });

            if (response.Success || response.SyncedCount > 0)
            {
                _storage.MarkCertificatesAsSynced(new[] { certificate.CertificateId });
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to sync certificate: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Syncs all pending (unsynced) certificates to the server.
    /// Call this when the application goes online.
    /// </summary>
    /// <returns>Number of certificates synced</returns>
    public async Task<int> SyncPendingCertificatesAsync()
    {
        if (_client == null) return 0;

        var unsyncedCertificates = _storage.GetUnsyncedCertificates();
        if (unsyncedCertificates.Count == 0) return 0;

        var license = _storage.LoadLicense();
        if (license?.License == null) return 0;

        try
        {
            System.Diagnostics.Debug.WriteLine($"Syncing {unsyncedCertificates.Count} pending certificates...");
            
            var response = await _client.SyncCertificatesAsync(
                license.License.LicenseId,
                unsyncedCertificates);

            if (response.Success || response.SyncedCount > 0)
            {
                // Mark all as synced (server accepted them)
                var syncedIds = unsyncedCertificates
                    .Where(c => !response.FailedIds.Contains(c.CertificateId))
                    .Select(c => c.CertificateId);
                
                _storage.MarkCertificatesAsSynced(syncedIds);
                
                System.Diagnostics.Debug.WriteLine($"Synced {response.SyncedCount} certificates");
                return response.SyncedCount;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to sync pending certificates: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Verifies all synced but unverified certificates with the server.
    /// Updates local storage with verification status.
    /// </summary>
    /// <returns>Number of certificates verified</returns>
    public async Task<int> VerifyPendingCertificatesAsync()
    {
        if (_client == null) return 0;

        var unverifiedCertificates = _storage.GetUnverifiedCertificates();
        if (unverifiedCertificates.Count == 0) return 0;

        var verifiedCount = 0;
        var verifiedIds = new List<string>();

        System.Diagnostics.Debug.WriteLine($"Verifying {unverifiedCertificates.Count} synced certificates...");

        foreach (var cert in unverifiedCertificates)
        {
            try
            {
                var result = await _client.VerifyCertificateAsync(cert.CertificateId);
                
                if (result.IsValid)
                {
                    verifiedIds.Add(cert.CertificateId);
                    verifiedCount++;
                    
                    // Check if signature was verified
                    if (result.IsSignatureVerified)
                    {
                        System.Diagnostics.Debug.WriteLine($"Certificate {cert.CertificateId} verified with signature");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Certificate {cert.CertificateId} verified (in records)");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to verify certificate {cert.CertificateId}: {ex.Message}");
            }
        }

        if (verifiedIds.Count > 0)
        {
            _storage.MarkCertificatesAsVerified(verifiedIds);
        }

        System.Diagnostics.Debug.WriteLine($"Verified {verifiedCount} certificates");
        return verifiedCount;
    }

    /// <summary>
    /// Syncs unsynced certificates and verifies unverified ones.
    /// Called automatically when going online.
    /// </summary>
    public async Task SyncAndVerifyPendingCertificatesAsync()
    {
        try
        {
            // First sync any unsynced certificates
            await SyncPendingCertificatesAsync();
            
            // Then verify any synced but unverified certificates
            await VerifyPendingCertificatesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in certificate sync/verify: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets certificate statistics from local storage.
    /// </summary>
    public CertificateStats GetLocalCertificateStats()
    {
        var (total, unsynced, unverified, verified) = _storage.GetCertificateStats();
        return new CertificateStats
        {
            TotalLocal = total,
            PendingSync = unsynced,
            PendingVerification = unverified,
            Verified = verified
        };
    }

    /// <summary>
    /// Gets all locally stored certificates.
    /// </summary>
    public List<LocalCertificateEntry> GetLocalCertificates()
    {
        return _storage.GetAllLocalCertificates();
    }

    /// <summary>
    /// Gets a specific certificate from local storage.
    /// </summary>
    public LocalCertificateEntry? GetLocalCertificate(string certificateId)
    {
        return _storage.GetCertificate(certificateId);
    }

    /// <summary>
    /// Cleans up old verified certificates from local storage.
    /// </summary>
    /// <param name="keepDays">Number of days to keep certificates</param>
    /// <returns>Number of certificates deleted</returns>
    public int CleanupOldCertificates(int keepDays = 365)
    {
        return _storage.CleanupOldCertificates(keepDays);
    }

    #endregion

    /// <summary>
    /// Registers a module with the server.
    /// </summary>
    public async Task<ModuleRegistrationResponse> RegisterModuleAsync(string moduleId, string certificateCode, string displayName, string version)
    {
        if (_client == null)
        {
            return new ModuleRegistrationResponse
            {
                Success = false,
                Message = "No server URL configured"
            };
        }

        return await _client.RegisterModuleAsync(moduleId, certificateCode, displayName, version);
    }

    /// <summary>
    /// Gets the brand logo for the current license.
    /// </summary>
    /// <returns>Tuple with LogoUrl, LogoBase64, and Error message if failed</returns>
    public async Task<(string? LogoUrl, string? LogoBase64, string? Error)> GetBrandLogoAsync()
    {
        // First try to get from local license
        var license = _storage.LoadLicense();
        if (license?.License != null)
        {
            // Return local logo if available
            if (!string.IsNullOrEmpty(license.License.BrandLogo))
            {
                return (null, license.License.BrandLogo, null);
            }
            if (!string.IsNullOrEmpty(license.License.BrandLogoUrl))
            {
                return (license.License.BrandLogoUrl, null, null);
            }
        }

        // Try to fetch from server if online
        if (_client != null && !string.IsNullOrEmpty(license?.License?.LicenseeCode))
        {
            return await _client.GetBrandLogoAsync(license.License.LicenseeCode);
        }

        return (null, null, "No logo available");
    }

    /// <summary>
    /// Gets the white-label branding info from the current license.
    /// </summary>
    public LicenseBrandingInfo? GetBrandingInfo()
    {
        var license = _storage.LoadLicense();
        if (license?.License == null)
        {
            return null;
        }

        return new LicenseBrandingInfo
        {
            Brand = license.License.Brand,
            LicenseeCode = license.License.LicenseeCode,
            SupportCode = license.License.SupportCode,
            BrandLogo = license.License.BrandLogo,
            BrandLogoUrl = license.License.BrandLogoUrl,
            DisplayEdition = license.License.DisplayEdition
        };
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}

// DTO for heartbeat response
internal class HeartbeatResponseDto
{
    public bool Success { get; set; }
    public bool IsValid { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ServerTime { get; set; }
    public string? Message { get; set; }
    
    // Enhanced fields
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public string? LicenseStatus { get; set; }
}

/// <summary>
/// License status information for UI display
/// </summary>
public class LicenseStatusInfo
{
    public bool IsLicensed { get; set; }
    public LicenseStatus Status { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string Edition { get; set; } = "UNLICENSED";  // NO TRIAL MODE!
    public DateTime? ExpiresAt { get; set; }
    public int DaysRemaining { get; set; }
    public int GraceDaysRemaining { get; set; }
    public List<string> Features { get; set; } = new();
    
    /// <summary>
    /// Short display format: XXXX-XXXX-XXXX-XXXX (for showing to users)
    /// </summary>
    public string HardwareId { get; set; } = string.Empty;
    
    /// <summary>
    /// Full fingerprints for OFFLINE license creation (each is 32 chars)
    /// Copy these to License Generator for offline licenses
    /// </summary>
    public List<string> HardwareFingerprints { get; set; } = new();

    /// <summary>
    /// Check if this is a trial (always false - no trial mode supported)
    /// </summary>
    public bool IsTrial => false;  // NO TRIAL MODE!

    #region Module-Based Licensing Properties

    /// <summary>
    /// Gets the current license tier (e.g., "Basic", "Professional", "Enterprise").
    /// Returns null if no tier or not licensed.
    /// </summary>
    public string? Tier => Features
        .FirstOrDefault(f => f.StartsWith("Tier:", StringComparison.OrdinalIgnoreCase))
        ?.Substring(5);

    /// <summary>
    /// Gets list of licensed module IDs (e.g., ["SurveyListing", "TideAnalysis"]).
    /// Returns empty list if not licensed.
    /// </summary>
    public List<string> LicensedModules => Features
        .Where(f => f.StartsWith("Module:", StringComparison.OrdinalIgnoreCase))
        .Select(f => f.Substring(7))
        .ToList();

    /// <summary>
    /// Checks if a specific module is licensed.
    /// </summary>
    /// <param name="moduleId">Module ID (PascalCase, e.g., "SurveyListing")</param>
    /// <returns>True if licensed</returns>
    public bool IsModuleLicensed(string moduleId)
    {
        if (!IsLicensed || string.IsNullOrEmpty(moduleId))
            return false;
        
        return Features.Any(f => 
            f.Equals($"Module:{moduleId}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the license has at least the specified tier level.
    /// Tier hierarchy: Basic (1) &lt; Professional (2) &lt; Enterprise (3)
    /// </summary>
    public bool HasMinimumTier(string minimumTier)
    {
        if (!IsLicensed || Tier == null)
            return false;

        int currentLevel = GetTierLevel(Tier);
        int requiredLevel = GetTierLevel(minimumTier);
        
        return currentLevel >= requiredLevel;
    }

    private static int GetTierLevel(string? tierName)
    {
        return tierName?.ToLowerInvariant() switch
        {
            "basic" => 1,
            "professional" or "pro" => 2,
            "enterprise" => 3,
            _ => 0
        };
    }

    #endregion

    #region Branding Properties (New for Fathom OS)

    /// <summary>
    /// Company/brand name for white-labeling.
    /// Example: "Subsea7 Survey Division"
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Unique 2-letter licensee code (A-Z).
    /// Used in Certificate IDs: FOS-{LicenseeCode}-2412-00001-X3B7
    /// </summary>
    public string? LicenseeCode { get; set; }
    
    /// <summary>
    /// Support verification code in format SUP-XX-XXXXX.
    /// Customer provides this when contacting support.
    /// </summary>
    public string? SupportCode { get; set; }
    
    /// <summary>
    /// Gets the display name for this edition.
    /// Returns "Fathom OS" if no brand, or "Fathom OS — {Brand} Edition" if branded.
    /// </summary>
    public string DisplayEdition => string.IsNullOrEmpty(Brand)
        ? LicenseConstants.ProductDisplayName
        : $"{LicenseConstants.ProductDisplayName} — {Brand} Edition";

    #endregion

    public string GetDisplayStatus()
    {
        return Status switch
        {
            LicenseStatus.Valid => $"Licensed ({DisplayEdition}) - {DaysRemaining} days remaining",
            LicenseStatus.GracePeriod => $"EXPIRED - {GraceDaysRemaining} grace days remaining!",
            LicenseStatus.Expired => "License Expired",
            LicenseStatus.HardwareMismatch => "Hardware Changed - Reactivation Required",
            LicenseStatus.InvalidSignature => "Invalid License",
            LicenseStatus.Revoked => "License Revoked",
            LicenseStatus.NotFound => "Not Activated",
            _ => "Unknown Status"
        };
    }
}

/// <summary>
/// Diagnostic information for troubleshooting license issues
/// </summary>
public class LicenseDiagnostics
{
    // Stored license info
    public bool HasStoredLicense { get; set; }
    public string? StoredLicenseId { get; set; }
    public DateTime? StoredLicenseExpiry { get; set; }
    public List<string>? StoredLicenseFeatures { get; set; }
    
    // Revocation info
    public bool IsInLocalRevocationList { get; set; }
    public string? LocalRevocationReason { get; set; }
    public List<string> AllLocallyRevokedIds { get; set; } = new();
    
    // Online check info
    public DateTime LastOnlineCheck { get; set; }
    public TimeSpan TimeSinceOnlineCheck { get; set; }
    public bool IsOfflineSyncPending { get; set; }
    
    // Cached result
    public bool HasCachedResult { get; set; }
    public string? CachedStatus { get; set; }
    public bool CachedIsValid { get; set; }
    
    // File paths
    public string? LicenseFilePath { get; set; }
    public string? RevocationFilePath { get; set; }
    public bool LicenseFileExists { get; set; }
    public bool RevocationFileExists { get; set; }
    
    // Server info
    public string? ServerUrl { get; set; }
    public bool HasOnlineCapability { get; set; }
    
    // Error
    public string? DiagnosticError { get; set; }
    
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== LICENSE DIAGNOSTICS ===");
        sb.AppendLine($"Has Stored License: {HasStoredLicense}");
        sb.AppendLine($"Stored License ID: {StoredLicenseId ?? "N/A"}");
        sb.AppendLine($"Stored Expiry: {StoredLicenseExpiry?.ToString("yyyy-MM-dd") ?? "N/A"}");
        sb.AppendLine($"Features: {(StoredLicenseFeatures != null ? string.Join(", ", StoredLicenseFeatures) : "N/A")}");
        sb.AppendLine();
        sb.AppendLine($"In Local Revocation List: {IsInLocalRevocationList}");
        sb.AppendLine($"Local Revocation Reason: {LocalRevocationReason ?? "N/A"}");
        sb.AppendLine($"All Locally Revoked IDs: {(AllLocallyRevokedIds.Count > 0 ? string.Join(", ", AllLocallyRevokedIds) : "None")}");
        sb.AppendLine();
        sb.AppendLine($"Last Online Check: {LastOnlineCheck:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Time Since Online Check: {TimeSinceOnlineCheck}");
        sb.AppendLine($"Offline Sync Pending: {IsOfflineSyncPending}");
        sb.AppendLine();
        sb.AppendLine($"Has Cached Result: {HasCachedResult}");
        sb.AppendLine($"Cached Status: {CachedStatus ?? "N/A"}");
        sb.AppendLine($"Cached IsValid: {CachedIsValid}");
        sb.AppendLine();
        sb.AppendLine($"License File: {LicenseFilePath}");
        sb.AppendLine($"License File Exists: {LicenseFileExists}");
        sb.AppendLine($"Revocation File: {RevocationFilePath}");
        sb.AppendLine($"Revocation File Exists: {RevocationFileExists}");
        sb.AppendLine();
        sb.AppendLine($"Server URL: {ServerUrl ?? "N/A"}");
        sb.AppendLine($"Has Online Capability: {HasOnlineCapability}");
        
        if (!string.IsNullOrEmpty(DiagnosticError))
        {
            sb.AppendLine();
            sb.AppendLine($"ERROR: {DiagnosticError}");
        }
        
        return sb.ToString();
    }
}

/// <summary>
/// White-label branding information from a license.
/// </summary>
public class LicenseBrandingInfo
{
    /// <summary>
    /// Company/brand name for white-labeling.
    /// Example: "Subsea7 Survey Division"
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Unique 2-letter licensee code (A-Z).
    /// Used in Certificate IDs: FOS-{LicenseeCode}-2412-00001-X3B7
    /// </summary>
    public string? LicenseeCode { get; set; }
    
    /// <summary>
    /// Support verification code in format SUP-XX-XXXXX.
    /// Customer provides this when contacting support.
    /// </summary>
    public string? SupportCode { get; set; }
    
    /// <summary>
    /// Base64-encoded company logo PNG (≤20KB).
    /// </summary>
    public string? BrandLogo { get; set; }
    
    /// <summary>
    /// HTTPS URL for high-resolution logo.
    /// </summary>
    public string? BrandLogoUrl { get; set; }
    
    /// <summary>
    /// Gets the display name for this edition.
    /// Returns "Fathom OS" if no brand, or "Fathom OS — {Brand} Edition" if branded.
    /// </summary>
    public string DisplayEdition { get; set; } = LicenseConstants.ProductDisplayName;
}

/// <summary>
/// Statistics about locally stored certificates.
/// </summary>
public class CertificateStats
{
    /// <summary>
    /// Total number of certificates stored locally.
    /// </summary>
    public int TotalLocal { get; set; }
    
    /// <summary>
    /// Number of certificates waiting to be synced to server.
    /// </summary>
    public int PendingSync { get; set; }
    
    /// <summary>
    /// Number of certificates synced but not yet verified by server.
    /// </summary>
    public int PendingVerification { get; set; }
    
    /// <summary>
    /// Number of certificates verified by server.
    /// </summary>
    public int Verified { get; set; }
    
    /// <summary>
    /// True if there are certificates waiting to sync or verify.
    /// </summary>
    public bool HasPending => PendingSync > 0 || PendingVerification > 0;
    
    public override string ToString()
    {
        return $"Total: {TotalLocal}, Pending Sync: {PendingSync}, Pending Verify: {PendingVerification}, Verified: {Verified}";
    }
}
