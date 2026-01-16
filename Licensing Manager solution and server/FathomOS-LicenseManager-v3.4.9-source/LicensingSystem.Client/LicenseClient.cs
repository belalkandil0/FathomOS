// LicensingSystem.Client/LicenseClient.cs
// HTTP client for communicating with the Fathom OS license server

using System.Net.Http.Json;
using System.Text.Json;
using LicensingSystem.Shared;

namespace LicensingSystem.Client;

/// <summary>
/// Client for online license activation and validation
/// </summary>
public class LicenseClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly LicenseStorage _storage;
    private readonly HardwareFingerprint _hwFingerprint;
    private bool _disposed;

    public LicenseClient(string serverBaseUrl, string productName = "FathomOS")
    {
        _baseUrl = serverBaseUrl.TrimEnd('/');
        _storage = new LicenseStorage(productName);
        _hwFingerprint = new HardwareFingerprint();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{productName}/1.0");
    }

    /// <summary>
    /// Activates a license key online
    /// </summary>
    public async Task<LicenseValidationResult> ActivateOnlineAsync(
        string licenseKey, 
        string email,
        string appVersion = "1.0")
    {
        try
        {
            var request = new ActivationRequest
            {
                LicenseKey = licenseKey,
                Email = email,
                HardwareFingerprints = _hwFingerprint.GetFingerprints(),
                MachineName = Environment.MachineName,
                AppVersion = appVersion,
                OsVersion = Environment.OSVersion.ToString()
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/license/activate", 
                request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.NotFound,
                    Message = $"Activation failed: {response.StatusCode}. {errorContent}"
                };
            }

            var activationResponse = await response.Content.ReadFromJsonAsync<ActivationResponse>();

            if (activationResponse == null || !activationResponse.Success)
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.NotFound,
                    Message = activationResponse?.Message ?? "Activation failed. Please check your license key."
                };
            }

            if (activationResponse.SignedLicense != null)
            {
                // Store the license
                _storage.SaveLicense(activationResponse.SignedLicense);
                _storage.UpdateLastOnlineCheck();

                var license = activationResponse.SignedLicense.License;
                return new LicenseValidationResult
                {
                    IsValid = true,
                    Status = LicenseStatus.Valid,
                    Message = "License activated successfully!",
                    License = license,
                    DaysRemaining = (int)(license.ExpiresAt - DateTime.UtcNow).TotalDays,
                    EnabledFeatures = license.Features
                };
            }

            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "No license received from server."
            };
        }
        catch (HttpRequestException ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = $"Network error: {ex.Message}. Please check your internet connection."
            };
        }
        catch (TaskCanceledException)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "Request timed out. Please try again."
            };
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = $"Activation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates the current license with the server (heartbeat)
    /// </summary>
    public async Task<LicenseValidationResult> ValidateOnlineAsync(string licenseId)
    {
        try
        {
            var request = new ValidationRequest
            {
                LicenseId = licenseId,
                HardwareFingerprints = _hwFingerprint.GetFingerprints()
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/license/validate", 
                request);

            if (!response.IsSuccessStatusCode)
            {
                // Server unreachable, fall back to offline validation
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.NotFound,
                    Message = "Server validation failed. Using offline mode."
                };
            }

            var validationResponse = await response.Content.ReadFromJsonAsync<ValidationResponse>();

            if (validationResponse == null)
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.Corrupted,
                    Message = "Invalid response from server."
                };
            }

            // Update online check timestamp
            _storage.UpdateLastOnlineCheck();

            // If server sent an updated license, store it
            if (validationResponse.UpdatedLicense != null)
            {
                _storage.SaveLicense(validationResponse.UpdatedLicense);
            }

            // Get the license data - prefer server's UpdatedLicense, fall back to stored license
            var license = validationResponse.UpdatedLicense?.License;
            if (license == null && validationResponse.IsValid)
            {
                // Server said valid but didn't send license - load from local storage
                var storedLicense = _storage.LoadLicense();
                license = storedLicense?.License;
            }

            // Calculate DaysRemaining from license expiry
            int daysRemaining = 0;
            if (license != null)
            {
                daysRemaining = Math.Max(0, (int)(license.ExpiresAt - DateTime.UtcNow).TotalDays);
            }

            // Get enabled features from license
            var enabledFeatures = license?.Features ?? new List<string>();

            return new LicenseValidationResult
            {
                IsValid = validationResponse.IsValid,
                Status = validationResponse.Status,
                Message = validationResponse.Message,
                License = license,
                ServerTime = validationResponse.ServerTime,
                DaysRemaining = daysRemaining,
                EnabledFeatures = enabledFeatures
            };
        }
        catch (HttpRequestException)
        {
            // Network error - return a result indicating offline validation should be used
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "Unable to reach license server. Using offline validation."
            };
        }
        catch (TaskCanceledException)
        {
            // Timeout
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "License server timeout. Using offline validation."
            };
        }
        catch (Exception)
        {
            // Other error - return a result indicating offline validation should be used
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "Unable to reach license server. Using offline validation."
            };
        }
    }

    /// <summary>
    /// Checks if revocation list includes this license
    /// </summary>
    public async Task<bool> CheckRevocationAsync(string licenseId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/license/revoked/{licenseId}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RevocationCheckResult>();
                return result?.IsRevoked ?? false;
            }

            return false; // Assume not revoked if server unreachable
        }
        catch
        {
            return false; // Assume not revoked if error
        }
    }

    /// <summary>
    /// Gets the current hardware ID for display
    /// </summary>
    public string GetHardwareId()
    {
        return _hwFingerprint.GetDisplayHwid();
    }

    // =========================================================================
    // CERTIFICATE METHODS (New for Fathom OS)
    // =========================================================================

    /// <summary>
    /// Syncs certificates to the server.
    /// Call this periodically when online to upload pending certificates.
    /// </summary>
    public async Task<CertificateSyncResponse> SyncCertificatesAsync(string licenseId, List<ProcessingCertificate> certificates)
    {
        try
        {
            var request = new CertificateSyncRequest
            {
                LicenseId = licenseId,
                Certificates = certificates
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/certificates/sync",
                request);

            if (!response.IsSuccessStatusCode)
            {
                return new CertificateSyncResponse
                {
                    Success = false,
                    Message = $"Sync failed: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<CertificateSyncResponse>();
            return result ?? new CertificateSyncResponse
            {
                Success = false,
                Message = "Invalid response from server"
            };
        }
        catch (Exception ex)
        {
            return new CertificateSyncResponse
            {
                Success = false,
                Message = $"Sync error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Verifies a certificate by ID.
    /// </summary>
    public async Task<CertificateVerificationResult> VerifyCertificateAsync(string certificateId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/certificates/verify/{certificateId}");

            if (!response.IsSuccessStatusCode)
            {
                return new CertificateVerificationResult
                {
                    IsValid = false,
                    CertificateId = certificateId,
                    Message = $"Verification failed: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<CertificateVerificationResult>();
            return result ?? new CertificateVerificationResult
            {
                IsValid = false,
                CertificateId = certificateId,
                Message = "Invalid response from server"
            };
        }
        catch (Exception ex)
        {
            return new CertificateVerificationResult
            {
                IsValid = false,
                CertificateId = certificateId,
                Message = $"Verification error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets the next certificate sequence number from the server.
    /// Use this to generate certificate IDs when online.
    /// </summary>
    public async Task<(string? CertificateId, int SequenceNumber, string? Error)> GetNextCertificateIdAsync(string licenseeCode)
    {
        try
        {
            var request = new { licenseeCode = licenseeCode };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/certificates/sequence",
                request);

            if (!response.IsSuccessStatusCode)
            {
                return (null, 0, $"Request failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<CertificateSequenceResponse>();
            if (result == null)
            {
                return (null, 0, "Invalid response from server");
            }

            return (result.CertificateId, result.SequenceNumber, null);
        }
        catch (Exception ex)
        {
            return (null, 0, ex.Message);
        }
    }

    /// <summary>
    /// Registers a module with the server.
    /// </summary>
    public async Task<ModuleRegistrationResponse> RegisterModuleAsync(string moduleId, string certificateCode, string displayName, string version)
    {
        try
        {
            var request = new ModuleRegistrationRequest
            {
                ModuleId = moduleId,
                CertificateCode = certificateCode,
                DisplayName = displayName,
                Version = version
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/modules/register",
                request);

            if (!response.IsSuccessStatusCode)
            {
                return new ModuleRegistrationResponse
                {
                    Success = false,
                    Message = $"Registration failed: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<ModuleRegistrationResponse>();
            return result ?? new ModuleRegistrationResponse
            {
                Success = false,
                Message = "Invalid response from server"
            };
        }
        catch (Exception ex)
        {
            return new ModuleRegistrationResponse
            {
                Success = false,
                Message = $"Registration error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets the brand logo URL or base64 for a licensee.
    /// </summary>
    public async Task<(string? LogoUrl, string? LogoBase64, string? Error)> GetBrandLogoAsync(string licenseeCode)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/branding/logo/{licenseeCode}");

            if (!response.IsSuccessStatusCode)
            {
                return (null, null, $"Request failed: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            string? logoUrl = null;
            string? logoBase64 = null;

            if (doc.RootElement.TryGetProperty("logoUrl", out var urlProp))
            {
                logoUrl = urlProp.GetString();
            }
            if (doc.RootElement.TryGetProperty("logoBase64", out var base64Prop))
            {
                logoBase64 = base64Prop.GetString();
            }

            return (logoUrl, logoBase64, null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    // ==================== SESSION MANAGEMENT ====================

    private string? _currentSessionToken;

    /// <summary>
    /// Current session token (if active)
    /// </summary>
    public string? CurrentSessionToken => _currentSessionToken;

    /// <summary>
    /// Starts a license session to prevent concurrent use
    /// </summary>
    public async Task<SessionStartResult> StartSessionAsync(string licenseId)
    {
        try
        {
            var request = new
            {
                LicenseId = licenseId,
                HardwareFingerprint = _hwFingerprint.GetPrimaryFingerprint(),
                MachineName = Environment.MachineName
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/license/session/start",
                request);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // License in use on another device
                var conflictResponse = await response.Content.ReadFromJsonAsync<SessionStartResponse>();
                return new SessionStartResult
                {
                    Success = false,
                    IsConflict = true,
                    Message = conflictResponse?.Message ?? "License is already in use on another device",
                    ConflictDevice = conflictResponse?.ConflictInfo?.ActiveDevice,
                    ConflictLastSeen = conflictResponse?.ConflictInfo?.LastSeen,
                    CanForceTerminate = conflictResponse?.ConflictInfo?.CanForceTerminate ?? false
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new SessionStartResult
                {
                    Success = false,
                    Message = $"Failed to start session: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<SessionStartResponse>();
            if (result?.Success == true && !string.IsNullOrEmpty(result.SessionToken))
            {
                _currentSessionToken = result.SessionToken;
            }

            return new SessionStartResult
            {
                Success = result?.Success ?? false,
                SessionToken = result?.SessionToken,
                Message = result?.Message
            };
        }
        catch (Exception ex)
        {
            return new SessionStartResult
            {
                Success = false,
                Message = $"Session start error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Sends a heartbeat to keep the session alive (call every 2 minutes)
    /// </summary>
    public async Task<SessionHeartbeatResult> SessionHeartbeatAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionToken))
        {
            return new SessionHeartbeatResult
            {
                Success = false,
                Message = "No active session"
            };
        }

        try
        {
            var request = new { SessionToken = _currentSessionToken };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/license/session/heartbeat",
                request);

            if (!response.IsSuccessStatusCode)
            {
                return new SessionHeartbeatResult
                {
                    Success = false,
                    Message = $"Heartbeat failed: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<SessionHeartbeatResponse>();
            return new SessionHeartbeatResult
            {
                Success = result?.Success ?? false,
                IsValid = result?.IsValid ?? false,
                ExpiresAt = result?.ExpiresAt,
                ServerTime = result?.ServerTime ?? DateTime.UtcNow,
                Message = result?.Message
            };
        }
        catch (Exception ex)
        {
            return new SessionHeartbeatResult
            {
                Success = false,
                Message = $"Heartbeat error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Ends the current session (call on app close)
    /// </summary>
    public async Task<bool> EndSessionAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionToken))
        {
            return true; // No session to end
        }

        try
        {
            var request = new { SessionToken = _currentSessionToken };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/license/session/end",
                request);

            _currentSessionToken = null;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            _currentSessionToken = null;
            return false;
        }
    }

    /// <summary>
    /// Force terminates another session to take over the license
    /// </summary>
    public async Task<bool> ForceTerminateSessionAsync(string licenseId)
    {
        try
        {
            var request = new
            {
                LicenseId = licenseId,
                HardwareFingerprint = _hwFingerprint.GetPrimaryFingerprint(),
                MachineName = Environment.MachineName
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/license/session/force-terminate",
                request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Try to end session on dispose
            if (!string.IsNullOrEmpty(_currentSessionToken))
            {
                try
                {
                    EndSessionAsync().GetAwaiter().GetResult();
                }
                catch { }
            }
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

// Session Management DTOs

public class SessionStartResult
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
    public bool IsConflict { get; set; }
    public string? ConflictDevice { get; set; }
    public DateTime? ConflictLastSeen { get; set; }
    public bool CanForceTerminate { get; set; }
}

public class SessionHeartbeatResult
{
    public bool Success { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ServerTime { get; set; }
    public string? Message { get; set; }
}

internal class SessionStartResponse
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
    public SessionConflictInfo? ConflictInfo { get; set; }
}

internal class SessionConflictInfo
{
    public string? ActiveDevice { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool CanForceTerminate { get; set; }
}

internal class SessionHeartbeatResponse
{
    public bool Success { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ServerTime { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Result of revocation check
/// </summary>
internal class RevocationCheckResult
{
    public bool IsRevoked { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Response from certificate sequence endpoint
/// </summary>
internal class CertificateSequenceResponse
{
    public string CertificateId { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string YearMonth { get; set; } = string.Empty;
}
