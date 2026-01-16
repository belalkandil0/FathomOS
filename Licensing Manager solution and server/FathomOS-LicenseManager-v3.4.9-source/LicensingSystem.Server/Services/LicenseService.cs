// LicensingSystem.Server/Services/LicenseService.cs
// Server-side license management service

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicensingSystem.Shared;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LicensingSystem.Server.Services;

public interface ILicenseService
{
    Task<ActivationResponse> ActivateLicenseAsync(ActivationRequest request);
    Task<ValidationResponse> ValidateLicenseAsync(ValidationRequest request);
    Task<bool> IsLicenseRevokedAsync(string licenseId);
    Task RevokeLicenseAsync(string licenseId, string reason);
    Task<SignedLicense?> GetLicenseByKeyAsync(string licenseKey);
    
    /// <summary>
    /// Generates a signed offline license file content
    /// </summary>
    Task<string> GenerateOfflineLicenseFileAsync(
        string licenseId,
        string licenseKey,
        string customerEmail,
        string customerName,
        string edition,
        DateTime expiresAt,
        List<string> features,
        string hardwareId,
        string? brand,
        string? licenseeCode);
}

public class LicenseService : ILicenseService
{
    private readonly LicenseDbContext _db;
    private readonly ECDsa _ecdsa;
    private readonly ILogger<LicenseService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;

    public LicenseService(
        LicenseDbContext db, 
        IConfiguration config,
        ILogger<LicenseService> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditService auditService)
    {
        _db = db;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;

        // Load private key using helper (supports environment variables for production)
        var privateKeyPem = LicensingConfiguration.GetPrivateKey(config);
        
        _ecdsa = ECDsa.Create();
        _ecdsa.ImportFromPem(privateKeyPem);
    }

    public async Task<ActivationResponse> ActivateLicenseAsync(ActivationRequest request)
    {
        // Find the license key in database
        var licenseRecord = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Key == request.LicenseKey);

        if (licenseRecord == null)
        {
            return new ActivationResponse
            {
                Success = false,
                Message = "Invalid license key.",
                ServerTime = DateTime.UtcNow
            };
        }

        // Create a combined fingerprint from the hardware fingerprints
        var combinedFingerprint = string.Join("-", request.HardwareFingerprints.OrderBy(x => x).Take(4));
        
        // Check if key is already used (single device)
        if (licenseRecord.Activations.Any(a => !a.IsDeactivated))
        {
            var existingActivation = licenseRecord.Activations.First(a => !a.IsDeactivated);
            
            // Check if it's the same hardware (allow same device re-activation)
            if (existingActivation.HardwareFingerprint != combinedFingerprint)
            {
                // Calculate similarity between fingerprints
                var existingParts = existingActivation.HardwareFingerprint.Split('-');
                var newParts = combinedFingerprint.Split('-');
                var matchCount = existingParts.Count(ep => newParts.Contains(ep));

                if (matchCount < 2) // Require at least 2 matches
                {
                    return new ActivationResponse
                    {
                        Success = false,
                        Message = "This license key is already activated on another device.",
                        ServerTime = DateTime.UtcNow
                    };
                }
            }

            // Same device, re-issue license - update existing activation
            existingActivation.LastSeenAt = DateTime.UtcNow;
            existingActivation.AppVersion = request.AppVersion;
            existingActivation.OsVersion = request.OsVersion;
            existingActivation.IpAddress = GetClientIpAddress();
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Re-activating license on same device");
            
            // Log re-activation
            await _auditService.LogAsync(
                "LICENSE_REACTIVATED",
                "License",
                licenseRecord.LicenseId,
                null,
                request.Email,
                GetClientIpAddress(),
                $"License re-activated on {request.MachineName ?? "Unknown"}",
                true
            );
            
            // Create signed license
            var reissuedLicense = CreateSignedLicense(licenseRecord, request.HardwareFingerprints);
            
            return new ActivationResponse
            {
                Success = true,
                Message = "License re-activated successfully!",
                SignedLicense = reissuedLicense,
                ServerTime = DateTime.UtcNow
            };
        }

        // Check subscription status
        if (licenseRecord.ExpiresAt < DateTime.UtcNow)
        {
            return new ActivationResponse
            {
                Success = false,
                Message = "This subscription has expired. Please renew.",
                ServerTime = DateTime.UtcNow
            };
        }

        // Check if revoked
        if (licenseRecord.IsRevoked)
        {
            return new ActivationResponse
            {
                Success = false,
                Message = "This license has been revoked.",
                ServerTime = DateTime.UtcNow
            };
        }

        // Create signed license
        var signedLicense = CreateSignedLicense(licenseRecord, request.HardwareFingerprints);

        // Record activation
        var activation = new LicenseActivationRecord
        {
            LicenseKeyId = licenseRecord.Id,
            HardwareFingerprint = combinedFingerprint,
            MachineName = request.MachineName,
            ActivatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            AppVersion = request.AppVersion,
            OsVersion = request.OsVersion,
            IpAddress = GetClientIpAddress()
        };

        _db.LicenseActivations.Add(activation);
        await _db.SaveChangesAsync();

        // Log successful activation
        await _auditService.LogAsync(
            "LICENSE_ACTIVATED",
            "License",
            licenseRecord.LicenseId,
            null,
            request.Email,
            GetClientIpAddress(),
            $"License activated on {request.MachineName ?? "Unknown"} (App: {request.AppVersion})",
            true,
            null,
            JsonSerializer.Serialize(new { 
                MachineName = request.MachineName,
                HardwareFingerprint = combinedFingerprint,
                AppVersion = request.AppVersion,
                OsVersion = request.OsVersion
            })
        );

        return new ActivationResponse
        {
            Success = true,
            Message = "License activated successfully!",
            SignedLicense = signedLicense,
            ServerTime = DateTime.UtcNow
        };
    }

    public async Task<ValidationResponse> ValidateLicenseAsync(ValidationRequest request)
    {
        _logger.LogInformation("ValidateLicenseAsync called for LicenseId: {LicenseId}", request.LicenseId);
        
        // Create a combined fingerprint from the hardware fingerprints
        var combinedFingerprint = string.Join("-", request.HardwareFingerprints.OrderBy(x => x).Take(4));
        
        // First, try to find an activation record (for online/activated licenses)
        var activation = await _db.LicenseActivations
            .Include(a => a.LicenseKey)
            .FirstOrDefaultAsync(a => a.LicenseKey.LicenseId == request.LicenseId && !a.IsDeactivated);

        LicenseKeyRecord? licenseRecord = null;
        bool isOfflineLicense = false;

        if (activation != null)
        {
            licenseRecord = activation.LicenseKey;
        }
        else
        {
            // No activation found - check if it's an offline-generated license
            licenseRecord = await _db.LicenseKeys
                .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);
            
            if (licenseRecord != null)
            {
                isOfflineLicense = licenseRecord.IsOfflineGenerated;
                _logger.LogInformation("Found offline license: {LicenseId}, IsOfflineGenerated: {IsOffline}", 
                    request.LicenseId, isOfflineLicense);
            }
        }

        if (licenseRecord == null)
        {
            _logger.LogWarning("License not found in database: {LicenseId}", request.LicenseId);
            return new ValidationResponse
            {
                IsValid = false,
                Status = LicenseStatus.NotFound,
                Message = "License not found.",
                ServerTime = DateTime.UtcNow
            };
        }

        _logger.LogInformation("Found license: {LicenseId}, IsRevoked: {IsRevoked}, ExpiresAt: {ExpiresAt}, IsOffline: {IsOffline}", 
            request.LicenseId, licenseRecord.IsRevoked, licenseRecord.ExpiresAt, isOfflineLicense);

        // For online licenses, check hardware match
        if (!isOfflineLicense && activation != null)
        {
            var existingParts = activation.HardwareFingerprint.Split('-');
            var newParts = combinedFingerprint.Split('-');
            var matchCount = existingParts.Count(ep => newParts.Contains(ep));

            if (matchCount < 2)
            {
                _logger.LogWarning("Hardware mismatch for {LicenseId}: matched {MatchCount}/2 required", 
                    request.LicenseId, matchCount);
                return new ValidationResponse
                {
                    IsValid = false,
                    Status = LicenseStatus.HardwareMismatch,
                    Message = "Hardware mismatch detected.",
                    ServerTime = DateTime.UtcNow
                };
            }
        }

        // Check revocation - THIS IS THE KEY CHECK FOR REVOCATION!
        if (licenseRecord.IsRevoked)
        {
            _logger.LogWarning("License REVOKED: {LicenseId}, Reason: {Reason}", 
                request.LicenseId, licenseRecord.RevocationReason);
            return new ValidationResponse
            {
                IsValid = false,
                Status = LicenseStatus.Revoked,
                Message = "This license has been revoked.",
                ServerTime = DateTime.UtcNow,
                RevokedAt = licenseRecord.RevokedAt,
                RevokeReason = licenseRecord.RevocationReason
            };
        }

        // Check expiration
        if (licenseRecord.ExpiresAt < DateTime.UtcNow)
        {
            var gracePeriodEnd = licenseRecord.ExpiresAt.AddDays(LicenseConstants.GracePeriodDays);
            
            if (DateTime.UtcNow <= gracePeriodEnd)
            {
                _logger.LogInformation("License in grace period: {LicenseId}", request.LicenseId);
                return new ValidationResponse
                {
                    IsValid = true,
                    Status = LicenseStatus.GracePeriod,
                    Message = "License expired. Please renew.",
                    ServerTime = DateTime.UtcNow
                };
            }

            _logger.LogWarning("License EXPIRED: {LicenseId}", request.LicenseId);
            return new ValidationResponse
            {
                IsValid = false,
                Status = LicenseStatus.Expired,
                Message = "License has expired.",
                ServerTime = DateTime.UtcNow
            };
        }

        // Update last seen (only for activated online licenses)
        if (activation != null)
        {
            activation.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Return updated license (in case expiry was extended)
        var signedLicense = CreateSignedLicense(licenseRecord, request.HardwareFingerprints);

        _logger.LogInformation("License VALID: {LicenseId}", request.LicenseId);
        return new ValidationResponse
        {
            IsValid = true,
            Status = LicenseStatus.Valid,
            Message = "License is valid.",
            ServerTime = DateTime.UtcNow,
            UpdatedLicense = signedLicense
        };
    }

    public async Task<bool> IsLicenseRevokedAsync(string licenseId)
    {
        var license = await _db.LicenseKeys
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        return license?.IsRevoked ?? false;
    }

    public async Task RevokeLicenseAsync(string licenseId, string reason)
    {
        var license = await _db.LicenseKeys
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        if (license != null)
        {
            license.IsRevoked = true;
            license.RevokedAt = DateTime.UtcNow;
            license.RevocationReason = reason;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<SignedLicense?> GetLicenseByKeyAsync(string licenseKey)
    {
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Key == licenseKey);

        if (license == null) return null;

        var activation = license.Activations.FirstOrDefault(a => !a.IsDeactivated);
        if (activation == null) return null;

        // Convert stored fingerprint back to list
        var fingerprints = activation.HardwareFingerprint.Split('-').ToList();
        return CreateSignedLicense(license, fingerprints);
    }

    private SignedLicense CreateSignedLicense(LicenseKeyRecord record, List<string> hwFingerprints)
    {
        var license = new LicenseFile
        {
            Version = 3, // Updated for Fathom OS
            LicenseId = record.LicenseId,
            Product = record.ProductName,
            Edition = record.Edition,
            CustomerEmail = record.CustomerEmail,
            CustomerName = record.CustomerName,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = record.ExpiresAt,
            SubscriptionType = record.SubscriptionType,
            HardwareFingerprints = hwFingerprints,
            FingerprintMatchThreshold = 3,
            Features = string.IsNullOrEmpty(record.Features) 
                ? new List<string>() 
                : record.Features.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            // White-Label Branding (New for Fathom OS)
            Brand = record.Brand,
            LicenseeCode = record.LicenseeCode,
            SupportCode = record.SupportCode,
            BrandLogo = record.BrandLogo,
            BrandLogoUrl = record.BrandLogoUrl
        };

        // Sign the license
        var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = false });
        var signature = _ecdsa.SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA256);

        return new SignedLicense
        {
            License = license,
            Signature = Convert.ToBase64String(signature)
        };
    }

    private string GetClientIpAddress()
    {
        // BUG FIX: Actually get the client IP address
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return "unknown";

        // Check for forwarded IP (when behind proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the list (client IP)
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to direct connection IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Generates a signed offline license file content
    /// </summary>
    public Task<string> GenerateOfflineLicenseFileAsync(
        string licenseId,
        string licenseKey,
        string customerEmail,
        string customerName,
        string edition,
        DateTime expiresAt,
        List<string> features,
        string hardwareId,
        string? brand,
        string? licenseeCode)
    {
        // Create the license file
        var license = new LicenseFile
        {
            Version = 3,
            LicenseId = licenseId,
            Product = LicenseConstants.ProductName,
            Edition = edition,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            SubscriptionType = SubscriptionType.Yearly, // Default for offline
            LicenseType = LicenseType.Offline,
            HardwareFingerprints = new List<string> { hardwareId },
            FingerprintMatchThreshold = 1, // Exact match for offline
            Features = features,
            Brand = brand,
            LicenseeCode = licenseeCode
        };

        // Sign the license
        var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = false });
        var signature = _ecdsa.SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA256);

        var signedLicense = new SignedLicense
        {
            License = license,
            Signature = Convert.ToBase64String(signature)
        };

        // Generate the .lic file content (JSON format)
        var licenseFileContent = JsonSerializer.Serialize(signedLicense, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogInformation("Generated offline license file for {LicenseId}, Hardware: {HardwareId}", 
            licenseId, hardwareId);

        return Task.FromResult(licenseFileContent);
    }
}
