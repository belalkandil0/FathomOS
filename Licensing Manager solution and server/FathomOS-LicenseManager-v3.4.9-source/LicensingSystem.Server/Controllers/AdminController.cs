// LicensingSystem.Server/Controllers/AdminController.cs
// Admin endpoints for license management

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;
using LicensingSystem.Shared;

namespace LicensingSystem.Server.Controllers;

/// <summary>
/// Admin endpoints for license management
/// All endpoints require X-API-Key header (except public endpoints)
/// This server is OPTIONAL - license validation happens offline in FathomOS app
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly LicenseDbContext _db;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        LicenseDbContext db,
        ILicenseService licenseService,
        ILogger<AdminController> logger)
    {
        _db = db;
        _licenseService = licenseService;
        _logger = logger;
    }

    #region License Sync (for License Generator UI)

    /// <summary>
    /// Sync a license record from the License Generator UI.
    /// This stores a copy of offline-generated licenses for tracking.
    /// POST /api/admin/licenses/sync
    /// </summary>
    [HttpPost("licenses/sync")]
    public async Task<ActionResult<LicenseSyncResponse>> SyncLicense([FromBody] LicenseSyncRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.LicenseId))
        {
            return BadRequest(new LicenseSyncResponse
            {
                Success = false,
                Message = "LicenseId is required"
            });
        }

        try
        {
            // Check if this license already exists
            var existing = await _db.SyncedLicenses
                .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);

            if (existing != null)
            {
                // Update existing record
                existing.ClientName = request.ClientName ?? existing.ClientName;
                existing.ClientCode = request.ClientCode ?? existing.ClientCode;
                existing.Edition = request.Edition ?? existing.Edition;
                existing.LicenseJson = request.LicenseJson ?? existing.LicenseJson;
                existing.ExpiresAt = request.ExpiresAt != default ? request.ExpiresAt : existing.ExpiresAt;
                existing.CustomerEmail = request.CustomerEmail ?? existing.CustomerEmail;
                existing.Features = request.Features ?? existing.Features;
                existing.Brand = request.Brand ?? existing.Brand;
                existing.SyncedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Updated synced license record: {LicenseId}", request.LicenseId);

                return Ok(new LicenseSyncResponse
                {
                    Success = true,
                    Message = "License record updated",
                    LicenseId = request.LicenseId,
                    IsNew = false
                });
            }

            // Create new synced license record
            var syncedLicense = new SyncedLicenseRecord
            {
                LicenseId = request.LicenseId,
                ClientName = request.ClientName,
                ClientCode = request.ClientCode,
                Edition = request.Edition,
                LicenseJson = request.LicenseJson,
                IssuedAt = request.IssuedAt != default ? request.IssuedAt : DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                SyncedAt = DateTime.UtcNow,
                IsRevoked = false,
                CustomerEmail = request.CustomerEmail,
                Features = request.Features,
                Brand = request.Brand
            };

            _db.SyncedLicenses.Add(syncedLicense);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Synced new license record: {LicenseId} for {ClientName}",
                request.LicenseId, request.ClientName);

            return Ok(new LicenseSyncResponse
            {
                Success = true,
                Message = "License record synced successfully",
                LicenseId = request.LicenseId,
                IsNew = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing license: {LicenseId}", request.LicenseId);
            return StatusCode(500, new LicenseSyncResponse
            {
                Success = false,
                Message = $"Error syncing license: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Bulk sync multiple license records.
    /// POST /api/admin/licenses/sync-bulk
    /// </summary>
    [HttpPost("licenses/sync-bulk")]
    public async Task<ActionResult<BulkLicenseSyncResponse>> SyncLicensesBulk([FromBody] BulkLicenseSyncRequest request)
    {
        if (request?.Licenses == null || !request.Licenses.Any())
        {
            return BadRequest(new BulkLicenseSyncResponse
            {
                Success = false,
                Message = "No licenses provided"
            });
        }

        var syncedCount = 0;
        var updatedCount = 0;
        var failedIds = new List<string>();

        foreach (var license in request.Licenses)
        {
            try
            {
                var existing = await _db.SyncedLicenses
                    .FirstOrDefaultAsync(l => l.LicenseId == license.LicenseId);

                if (existing != null)
                {
                    existing.ClientName = license.ClientName ?? existing.ClientName;
                    existing.ClientCode = license.ClientCode ?? existing.ClientCode;
                    existing.Edition = license.Edition ?? existing.Edition;
                    existing.LicenseJson = license.LicenseJson ?? existing.LicenseJson;
                    existing.ExpiresAt = license.ExpiresAt != default ? license.ExpiresAt : existing.ExpiresAt;
                    existing.SyncedAt = DateTime.UtcNow;
                    updatedCount++;
                }
                else
                {
                    _db.SyncedLicenses.Add(new SyncedLicenseRecord
                    {
                        LicenseId = license.LicenseId,
                        ClientName = license.ClientName,
                        ClientCode = license.ClientCode,
                        Edition = license.Edition,
                        LicenseJson = license.LicenseJson,
                        IssuedAt = license.IssuedAt != default ? license.IssuedAt : DateTime.UtcNow,
                        ExpiresAt = license.ExpiresAt,
                        SyncedAt = DateTime.UtcNow,
                        CustomerEmail = license.CustomerEmail,
                        Features = license.Features,
                        Brand = license.Brand
                    });
                    syncedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync license: {LicenseId}", license.LicenseId);
                failedIds.Add(license.LicenseId);
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Bulk sync completed: {SyncedCount} new, {UpdatedCount} updated, {FailedCount} failed",
            syncedCount, updatedCount, failedIds.Count);

        return Ok(new BulkLicenseSyncResponse
        {
            Success = failedIds.Count == 0,
            Message = $"Synced {syncedCount} new, updated {updatedCount} existing" +
                      (failedIds.Count > 0 ? $", {failedIds.Count} failed" : ""),
            SyncedCount = syncedCount,
            UpdatedCount = updatedCount,
            FailedIds = failedIds
        });
    }

    /// <summary>
    /// Get all synced license records (for tracking dashboard).
    /// GET /api/admin/licenses/synced
    /// </summary>
    [HttpGet("licenses/synced")]
    public async Task<ActionResult<List<SyncedLicenseInfo>>> GetSyncedLicenses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var licenses = await _db.SyncedLicenses
            .OrderByDescending(l => l.SyncedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new SyncedLicenseInfo
            {
                Id = l.Id,
                LicenseId = l.LicenseId,
                ClientName = l.ClientName,
                ClientCode = l.ClientCode,
                Edition = l.Edition,
                IssuedAt = l.IssuedAt,
                ExpiresAt = l.ExpiresAt,
                SyncedAt = l.SyncedAt,
                IsRevoked = l.IsRevoked,
                CustomerEmail = l.CustomerEmail,
                Brand = l.Brand,
                Status = l.IsRevoked ? "Revoked" :
                         (l.ExpiresAt < DateTime.UtcNow ? "Expired" : "Active")
            })
            .ToListAsync();

        return Ok(licenses);
    }

    /// <summary>
    /// Revoke a synced license (mark as revoked for tracking).
    /// POST /api/admin/licenses/synced/{licenseId}/revoke
    /// </summary>
    [HttpPost("licenses/synced/{licenseId}/revoke")]
    public async Task<ActionResult> RevokeSyncedLicense(string licenseId, [FromBody] RevokeLicenseRequest? request = null)
    {
        var license = await _db.SyncedLicenses
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        if (license == null)
            return NotFound(new { message = "Synced license not found" });

        license.IsRevoked = true;
        license.RevokedAt = DateTime.UtcNow;
        license.RevokedReason = request?.Reason ?? "Revoked by admin";

        await _db.SaveChangesAsync();

        _logger.LogInformation("Revoked synced license: {LicenseId}", licenseId);

        return Ok(new { message = "License marked as revoked" });
    }

    #endregion

    /// <summary>
    /// Lists all license keys
    /// </summary>
    [HttpGet("licenses")]
    public async Task<ActionResult<List<LicenseKeyInfo>>> GetAllLicenses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            // Fetch data first, then project (more SQLite-friendly)
            var licenseRecords = await _db.LicenseKeys
                .Include(l => l.Activations)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var licenses = licenseRecords.Select(l => new LicenseKeyInfo
            {
                Id = l.Id,
                LicenseKey = l.Key,
                LicenseId = l.LicenseId,
                CustomerEmail = l.CustomerEmail,
                CustomerName = l.CustomerName,
                Edition = l.Edition,
                CreatedAt = l.CreatedAt,
                ExpiresAt = l.ExpiresAt,
                SubscriptionType = l.SubscriptionType.ToString(),
                IsRevoked = l.IsRevoked,
                IsActivated = l.Activations.Any(a => !a.IsDeactivated),
                ActivationCount = l.Activations.Count,
                LastActivationDate = l.Activations
                    .Where(a => !a.IsDeactivated)
                    .OrderByDescending(a => a.ActivatedAt)
                    .Select(a => (DateTime?)a.ActivatedAt)
                    .FirstOrDefault(),
                HardwareFingerprints = l.Activations
                    .Where(a => !a.IsDeactivated && !string.IsNullOrEmpty(a.HardwareFingerprint))
                    .Select(a => a.HardwareFingerprint!)
                    .Distinct()
                    .ToList(),
                // White-Label Branding
                Brand = l.Brand,
                LicenseeCode = l.LicenseeCode,
                SupportCode = l.SupportCode,
                // License Type (Online/Offline)
                LicenseType = l.LicenseType ?? "Online",
                IsOffline = l.LicenseType == "Offline",
                // Computed Status
                Status = l.IsRevoked ? "Revoked" : (l.ExpiresAt < DateTime.UtcNow ? "Expired" : "Active")
            }).ToList();

            return Ok(licenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching licenses");
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Gets license details by ID
    /// </summary>
    [HttpGet("licenses/{id}")]
    public async Task<ActionResult<LicenseDetailsInfo>> GetLicense(int id)
    {
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (license == null)
            return NotFound();

        return Ok(new LicenseDetailsInfo
        {
            Id = license.Id,
            LicenseKey = license.Key,
            LicenseId = license.LicenseId,
            CustomerEmail = license.CustomerEmail,
            CustomerName = license.CustomerName,
            ProductName = license.ProductName,
            Edition = license.Edition,
            Features = license.Features,
            CreatedAt = license.CreatedAt,
            ExpiresAt = license.ExpiresAt,
            SubscriptionType = license.SubscriptionType.ToString(),
            IsRevoked = license.IsRevoked,
            RevokedAt = license.RevokedAt,
            RevocationReason = license.RevocationReason,
            // White-Label Branding
            Brand = license.Brand,
            LicenseeCode = license.LicenseeCode,
            SupportCode = license.SupportCode,
            Activations = license.Activations.Select(a => new ActivationInfo
            {
                Id = a.Id,
                ActivatedAt = a.ActivatedAt,
                LastSeenAt = a.LastSeenAt,
                IsDeactivated = a.IsDeactivated,
                HardwareFingerprint = a.HardwareFingerprint,
                MachineName = a.MachineName,
                AppVersion = a.AppVersion,
                OsVersion = a.OsVersion,
                IpAddress = a.IpAddress
            }).ToList()
        });
    }

    /// <summary>
    /// Creates a new license key (Online or Offline)
    /// </summary>
    [HttpPost("licenses")]
    public async Task<ActionResult<CreateLicenseResponse>> CreateLicense([FromBody] CreateLicenseRequest request)
    {
        var isOffline = request.LicenseType?.Equals("Offline", StringComparison.OrdinalIgnoreCase) == true;
        
        // Validate offline license requirements
        if (isOffline && string.IsNullOrWhiteSpace(request.HardwareId))
        {
            return BadRequest("Hardware ID is required for offline licenses");
        }
        
        var licenseKey = GenerateLicenseKey();
        var licenseId = $"LIC-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        // Parse subscription type from string
        var subscriptionType = request.SubscriptionType?.ToLower() switch
        {
            "monthly" => SubscriptionType.Monthly,
            "yearly" => SubscriptionType.Yearly,
            "lifetime" => SubscriptionType.Lifetime,
            _ => SubscriptionType.Monthly
        };

        var expiresAt = subscriptionType switch
        {
            SubscriptionType.Monthly => DateTime.UtcNow.AddMonths(request.DurationMonths > 0 ? request.DurationMonths : 1),
            SubscriptionType.Yearly => DateTime.UtcNow.AddYears(request.DurationMonths > 0 ? request.DurationMonths / 12 : 1),
            SubscriptionType.Lifetime => DateTime.UtcNow.AddYears(100),
            _ => DateTime.UtcNow.AddMonths(request.DurationMonths > 0 ? request.DurationMonths : 1)
        };

        // Generate support code if not provided
        var supportCode = request.SupportCode;
        if (string.IsNullOrEmpty(supportCode))
        {
            var licenseeCode = request.LicenseeCode?.ToUpperInvariant() ?? "00";
            if (licenseeCode.Length != 2) licenseeCode = "00";
            supportCode = LicenseConstants.GenerateSupportCode(licenseeCode);
        }

        // Generate QR verification token
        var qrToken = GenerateQrVerificationToken(licenseId);

        var license = new LicenseKeyRecord
        {
            Key = isOffline ? $"OFFLINE-{licenseKey}" : licenseKey,
            LicenseId = licenseId,
            ProductName = request.ProductName ?? LicenseConstants.ProductName,
            Edition = request.Edition ?? "Professional",
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName ?? "",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            SubscriptionType = subscriptionType,
            Features = string.Join(",", request.Features ?? new List<string> { "Tier:Professional" }),
            // White-Label Branding
            Brand = request.Brand,
            LicenseeCode = request.LicenseeCode?.ToUpperInvariant(),
            SupportCode = supportCode,
            BrandLogo = request.BrandLogo,
            BrandLogoUrl = request.BrandLogoUrl,
            // Offline license tracking
            HardwareId = isOffline ? request.HardwareId : null,
            LicenseType = isOffline ? "Offline" : "Online",
            // QR verification
            QrVerificationToken = qrToken,
            // Business tracking (v3.3)
            Notes = request.Notes,
            PurchaseOrderNumber = request.PurchaseOrderNumber,
            PurchasePrice = request.PurchasePrice,
            Currency = request.Currency,
            SalesRep = request.SalesRep,
            ReferralSource = request.ReferralSource
        };

        _db.LicenseKeys.Add(license);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created {LicenseType} license {LicenseId} for {Email} with LicenseeCode {LicenseeCode}", 
            isOffline ? "offline" : "online", licenseId, request.CustomerEmail, license.LicenseeCode);

        // For offline licenses, generate the signed license file content
        string? licenseFileContent = null;
        if (isOffline)
        {
            licenseFileContent = await _licenseService.GenerateOfflineLicenseFileAsync(
                licenseId,
                licenseKey,
                request.CustomerEmail,
                request.CustomerName ?? "",
                request.Edition ?? "Professional",
                expiresAt,
                request.Features ?? new List<string> { "Tier:Professional" },
                request.HardwareId!,
                request.Brand,
                request.LicenseeCode
            );
        }

        // Generate QR code URL
        var qrVerificationUrl = $"https://license.fathomos.com/verify?id={licenseId}&token={qrToken}";
        var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(qrVerificationUrl)}";

        return Ok(new CreateLicenseResponse
        {
            LicenseKey = licenseKey,
            LicenseId = licenseId,
            CustomerEmail = license.CustomerEmail,
            CustomerName = license.CustomerName,
            Edition = license.Edition,
            CreatedAt = license.CreatedAt,
            ExpiresAt = license.ExpiresAt,
            SubscriptionType = subscriptionType.ToString(),
            // White-Label Branding
            Brand = license.Brand,
            LicenseeCode = license.LicenseeCode,
            SupportCode = license.SupportCode,
            // Offline license
            LicenseType = isOffline ? "Offline" : "Online",
            LicenseFileContent = licenseFileContent,
            // QR Code (v3.3)
            QrCodeUrl = qrCodeUrl
        });
    }

    /// <summary>
    /// Registers an offline-generated license on the server for tracking/management
    /// </summary>
    [HttpPost("licenses/register-offline")]
    public async Task<ActionResult<LicenseKeyInfo>> RegisterOfflineLicense([FromBody] RegisterOfflineLicenseRequest request)
    {
        // Check if this license ID already exists
        var existing = await _db.LicenseKeys.FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);
        if (existing != null)
        {
            // Already registered, return existing info
            return Ok(new LicenseKeyInfo
            {
                Id = existing.Id,
                LicenseKey = MaskKey(existing.Key),
                LicenseId = existing.LicenseId,
                CustomerEmail = existing.CustomerEmail,
                CustomerName = existing.CustomerName,
                Edition = existing.Edition,
                CreatedAt = existing.CreatedAt,
                ExpiresAt = existing.ExpiresAt,
                SubscriptionType = existing.SubscriptionType.ToString()
            });
        }

        // Parse subscription type
        var subscriptionType = request.SubscriptionType?.ToLower() switch
        {
            "monthly" => SubscriptionType.Monthly,
            "yearly" => SubscriptionType.Yearly,
            "lifetime" => SubscriptionType.Lifetime,
            _ => SubscriptionType.Monthly
        };

        var expiresAt = subscriptionType switch
        {
            SubscriptionType.Monthly => DateTime.UtcNow.AddMonths(request.DurationMonths > 0 ? request.DurationMonths : 1),
            SubscriptionType.Yearly => DateTime.UtcNow.AddYears(request.DurationMonths > 0 ? request.DurationMonths / 12 : 1),
            SubscriptionType.Lifetime => DateTime.UtcNow.AddYears(100),
            _ => DateTime.UtcNow.AddMonths(request.DurationMonths > 0 ? request.DurationMonths : 1)
        };

        // Create a license record (offline licenses don't have activation keys)
        var license = new LicenseKeyRecord
        {
            Key = $"OFFLINE-{request.LicenseId}", // Mark as offline license
            LicenseId = request.LicenseId ?? $"LIC-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            ProductName = request.ProductName ?? LicenseConstants.ProductName,
            Edition = request.Edition ?? "Professional",
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName ?? "",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            SubscriptionType = subscriptionType,
            Features = string.Join(",", request.Features ?? new List<string> { "Tier:Professional" }),
            IsOfflineGenerated = true,
            // White-Label Branding
            Brand = request.Brand,
            LicenseeCode = request.LicenseeCode?.ToUpperInvariant(),
            SupportCode = request.SupportCode ?? (!string.IsNullOrEmpty(request.LicenseeCode) 
                ? LicenseConstants.GenerateSupportCode(request.LicenseeCode.ToUpperInvariant()) 
                : null),
            BrandLogo = request.BrandLogo,
            BrandLogoUrl = request.BrandLogoUrl
        };

        _db.LicenseKeys.Add(license);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Registered offline license {LicenseId} for {Email}", license.LicenseId, request.CustomerEmail);

        return Ok(new LicenseKeyInfo
        {
            Id = license.Id,
            LicenseKey = "(Offline License)",
            LicenseId = license.LicenseId,
            CustomerEmail = license.CustomerEmail,
            CustomerName = license.CustomerName,
            Edition = license.Edition,
            CreatedAt = license.CreatedAt,
            ExpiresAt = license.ExpiresAt,
            SubscriptionType = subscriptionType.ToString()
        });
    }

    /// <summary>
    /// Extends a license expiration
    /// </summary>
    [HttpPost("licenses/{id}/extend")]
    public async Task<ActionResult> ExtendLicense(int id, [FromBody] ExtendLicenseRequest request)
    {
        var license = await _db.LicenseKeys.FindAsync(id);
        if (license == null)
            return NotFound();

        var baseDate = license.ExpiresAt > DateTime.UtcNow ? license.ExpiresAt : DateTime.UtcNow;
        license.ExpiresAt = baseDate.AddMonths(request.Months);
        
        await _db.SaveChangesAsync();

        _logger.LogInformation("Extended license {LicenseId} by {Months} months", 
            license.LicenseId, request.Months);

        return Ok(new { NewExpiresAt = license.ExpiresAt });
    }

    /// <summary>
    /// Revokes a license
    /// </summary>
    [HttpPost("licenses/{id}/revoke")]
    public async Task<ActionResult> RevokeLicense(int id, [FromBody] RevokeLicenseRequest? request = null)
    {
        var license = await _db.LicenseKeys.FindAsync(id);
        if (license == null)
            return NotFound();

        license.IsRevoked = true;
        license.RevokedAt = DateTime.UtcNow;
        license.RevocationReason = request?.Reason ?? "Revoked by admin";

        await _db.SaveChangesAsync();

        _logger.LogInformation("Revoked license {LicenseId}: {Reason}", 
            license.LicenseId, license.RevocationReason);

        return Ok();
    }

    /// <summary>
    /// Reinstates a revoked license
    /// </summary>
    [HttpPost("licenses/{id}/reinstate")]
    public async Task<ActionResult> ReinstateLicense(int id)
    {
        var license = await _db.LicenseKeys.FindAsync(id);
        if (license == null)
            return NotFound();

        license.IsRevoked = false;
        license.RevokedAt = null;
        license.RevocationReason = null;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Reinstated license {LicenseId}", license.LicenseId);

        return Ok();
    }

    /// <summary>
    /// Updates license tier and modules
    /// </summary>
    [HttpPut("licenses/{id}/features")]
    public async Task<ActionResult> UpdateLicenseFeatures(int id, [FromBody] UpdateLicenseFeaturesRequest request)
    {
        var license = await _db.LicenseKeys.FindAsync(id);
        if (license == null)
            return NotFound(new { error = "License not found" });

        if (license.IsRevoked)
            return BadRequest(new { error = "Cannot modify a revoked license" });

        // Update edition/tier
        if (!string.IsNullOrEmpty(request.Edition))
        {
            license.Edition = request.Edition;
        }

        // Update features
        if (request.Features != null && request.Features.Any())
        {
            license.Features = string.Join(",", request.Features);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated license {LicenseId} features: Edition={Edition}, Features={Features}", 
            license.LicenseId, license.Edition, license.Features);

        return Ok(new { 
            message = "License features updated successfully",
            edition = license.Edition,
            features = license.Features
        });
    }

    /// <summary>
    /// Gets license details including current features for editing
    /// </summary>
    [HttpGet("licenses/{id}/details")]
    public async Task<ActionResult<LicenseEditDetails>> GetLicenseDetails(int id)
    {
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id);
            
        if (license == null)
            return NotFound(new { error = "License not found" });

        return Ok(new LicenseEditDetails
        {
            Id = license.Id,
            LicenseId = license.LicenseId,
            LicenseKey = license.Key,
            CustomerEmail = license.CustomerEmail,
            CustomerName = license.CustomerName,
            Edition = license.Edition,
            Features = license.Features?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new(),
            ExpiresAt = license.ExpiresAt,
            IsRevoked = license.IsRevoked,
            SubscriptionType = license.SubscriptionType.ToString(),
            // White-Label Branding
            Brand = license.Brand,
            LicenseeCode = license.LicenseeCode,
            SupportCode = license.SupportCode
        });
    }

    /// <summary>
    /// Deactivates all activations for a license (allows reactivation on new device)
    /// </summary>
    [HttpPost("licenses/{id}/reset-activations")]
    public async Task<ActionResult> ResetActivations(int id)
    {
        var activations = await _db.LicenseActivations
            .Where(a => a.LicenseKeyId == id && !a.IsDeactivated)
            .ToListAsync();

        foreach (var activation in activations)
        {
            activation.IsDeactivated = true;
            activation.DeactivatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new { DeactivatedCount = activations.Count });
    }

    /// <summary>
    /// Gets dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetStats()
    {
        var now = DateTime.UtcNow;

        var stats = new DashboardStats
        {
            TotalLicenses = await _db.LicenseKeys.CountAsync(),
            ActiveLicenses = await _db.LicenseKeys.CountAsync(l => 
                !l.IsRevoked && l.ExpiresAt > now),
            ExpiredLicenses = await _db.LicenseKeys.CountAsync(l => 
                !l.IsRevoked && l.ExpiresAt <= now),
            RevokedLicenses = await _db.LicenseKeys.CountAsync(l => l.IsRevoked),
            TotalActivations = await _db.LicenseActivations.CountAsync(a => !a.IsDeactivated),
            RecentActivations = await _db.LicenseActivations
                .Where(a => a.ActivatedAt > now.AddDays(-7))
                .CountAsync(),
            ExpiringThisMonth = await _db.LicenseKeys.CountAsync(l => 
                l.ExpiresAt > now && l.ExpiresAt <= now.AddMonths(1))
        };

        return Ok(stats);
    }

    private static string GenerateLicenseKey()
    {
        // Format: XXXX-XXXX-XXXX-XXXX
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude confusing chars
        var random = new Random();
        var segments = new List<string>();
        
        for (int s = 0; s < 4; s++)
        {
            var segment = new char[4];
            for (int i = 0; i < 4; i++)
            {
                segment[i] = chars[random.Next(chars.Length)];
            }
            segments.Add(new string(segment));
        }

        return string.Join("-", segments);
    }

    private static string GenerateQrVerificationToken(string licenseId)
    {
        // Generate a cryptographic verification token for QR codes
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var data = $"{licenseId}:{timestamp}:FathomOS-QR-2024";
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        var token = Convert.ToBase64String(hash[..16])
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return $"{timestamp}-{token}";
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 8)
            return "****-****-****-****";
        return $"{key[..4]}-****-****-{key[^4..]}";
    }

    #region Module Management

    /// <summary>
    /// Gets all modules (for License Generator and Admin Panel)
    /// </summary>
    [HttpGet("modules")]
    public async Task<ActionResult<List<ModuleInfo>>> GetModules()
    {
        try
        {
            var modules = await _db.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new ModuleInfo
                {
                    Id = m.Id,
                    ModuleId = m.ModuleId,
                    DisplayName = m.DisplayName,
                    Description = m.Description,
                    Category = m.Category,
                    DefaultTier = m.DefaultTier,
                    DisplayOrder = m.DisplayOrder,
                    Icon = m.Icon,
                    CertificateCode = m.CertificateCode,
                    Version = m.Version,
                    IsActive = m.IsActive
                })
                .ToListAsync();

            return Ok(modules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching modules");
            return StatusCode(500, new { error = "Failed to fetch modules" });
        }
    }

    /// <summary>
    /// Adds a new module
    /// </summary>
    [HttpPost("modules")]
    public async Task<ActionResult<ModuleInfo>> AddModule([FromBody] AddModuleRequest request)
    {
        try
        {
            // Check if module already exists
            if (await _db.Modules.AnyAsync(m => m.ModuleId == request.ModuleId))
            {
                return BadRequest(new { error = $"Module '{request.ModuleId}' already exists" });
            }

            // Validate CertificateCode if provided
            if (!string.IsNullOrEmpty(request.CertificateCode))
            {
                if (request.CertificateCode.Length != 2 || !request.CertificateCode.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                {
                    return BadRequest(new { error = "CertificateCode must be exactly 2 characters (A-Z or 0-9)" });
                }

                if (await _db.Modules.AnyAsync(m => m.CertificateCode == request.CertificateCode))
                {
                    return BadRequest(new { error = $"CertificateCode '{request.CertificateCode}' already in use" });
                }
            }

            var module = new ModuleRecord
            {
                ModuleId = request.ModuleId,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Category = request.Category ?? "General",
                DefaultTier = request.DefaultTier ?? "Professional",
                DisplayOrder = request.DisplayOrder ?? 100,
                Icon = request.Icon,
                CertificateCode = request.CertificateCode?.ToUpperInvariant(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Modules.Add(module);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Added new module: {ModuleId} with CertificateCode: {CertificateCode}", 
                request.ModuleId, module.CertificateCode);

            return Ok(new ModuleInfo
            {
                Id = module.Id,
                ModuleId = module.ModuleId,
                DisplayName = module.DisplayName,
                Description = module.Description,
                Category = module.Category,
                DefaultTier = module.DefaultTier,
                DisplayOrder = module.DisplayOrder,
                Icon = module.Icon,
                CertificateCode = module.CertificateCode,
                Version = module.Version,
                IsActive = module.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding module: {ModuleId}", request.ModuleId);
            return StatusCode(500, new { error = "Failed to add module" });
        }
    }

    /// <summary>
    /// Updates a module
    /// </summary>
    [HttpPut("modules/{id}")]
    public async Task<ActionResult> UpdateModule(int id, [FromBody] UpdateModuleRequest request)
    {
        try
        {
            var module = await _db.Modules.FindAsync(id);
            if (module == null)
                return NotFound(new { error = "Module not found" });

            if (!string.IsNullOrEmpty(request.DisplayName))
                module.DisplayName = request.DisplayName;
            if (request.Description != null)
                module.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Category))
                module.Category = request.Category;
            if (!string.IsNullOrEmpty(request.DefaultTier))
                module.DefaultTier = request.DefaultTier;
            if (request.DisplayOrder.HasValue)
                module.DisplayOrder = request.DisplayOrder.Value;
            if (request.Icon != null)
                module.Icon = request.Icon;
            if (request.Version != null)
                module.Version = request.Version;
            if (request.IsActive.HasValue)
                module.IsActive = request.IsActive.Value;

            // Handle CertificateCode update with validation
            if (!string.IsNullOrEmpty(request.CertificateCode))
            {
                var code = request.CertificateCode.ToUpperInvariant();
                if (code.Length != 2 || !code.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                {
                    return BadRequest(new { error = "CertificateCode must be exactly 2 characters (A-Z or 0-9)" });
                }

                // Check if code is already used by another module
                if (await _db.Modules.AnyAsync(m => m.CertificateCode == code && m.Id != id))
                {
                    return BadRequest(new { error = $"CertificateCode '{code}' already in use by another module" });
                }

                module.CertificateCode = code;
            }

            module.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated module: {ModuleId}", module.ModuleId);
            return Ok(new { message = "Module updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module: {Id}", id);
            return StatusCode(500, new { error = "Failed to update module" });
        }
    }

    /// <summary>
    /// Deletes a module (soft delete)
    /// </summary>
    [HttpDelete("modules/{id}")]
    public async Task<ActionResult> DeleteModule(int id)
    {
        try
        {
            var module = await _db.Modules.FindAsync(id);
            if (module == null)
                return NotFound(new { error = "Module not found" });

            module.IsActive = false;
            module.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted (deactivated) module: {ModuleId}", module.ModuleId);
            return Ok(new { message = "Module deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting module: {Id}", id);
            return StatusCode(500, new { error = "Failed to delete module" });
        }
    }

    #endregion

    #region Module Registration (Public - for Fathom OS)

    /// <summary>
    /// Registers or updates a module from Fathom OS client.
    /// This is a PUBLIC endpoint - modules self-register when they load.
    /// POST /api/modules/register
    /// </summary>
    [HttpPost("/api/modules/register")]
    public async Task<ActionResult<ModuleRegistrationResponse>> RegisterModule([FromBody] ModuleRegistrationRequest request)
    {
        if (string.IsNullOrEmpty(request.ModuleId) || string.IsNullOrEmpty(request.CertificateCode))
        {
            return BadRequest(new ModuleRegistrationResponse
            {
                Success = false,
                Message = "ModuleId and CertificateCode are required"
            });
        }

        if (request.CertificateCode.Length != 2 || !request.CertificateCode.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
        {
            return BadRequest(new ModuleRegistrationResponse
            {
                Success = false,
                Message = "CertificateCode must be exactly 2 characters (A-Z or 0-9)"
            });
        }

        try
        {
            // Check if module already exists
            var existingModule = await _db.Modules
                .FirstOrDefaultAsync(m => m.ModuleId == request.ModuleId);

            if (existingModule != null)
            {
                // Check if certificate code conflicts
                if (existingModule.CertificateCode != request.CertificateCode)
                {
                    return BadRequest(new ModuleRegistrationResponse
                    {
                        Success = false,
                        Message = $"Module '{request.ModuleId}' already registered with different certificate code '{existingModule.CertificateCode}'"
                    });
                }

                // Update version
                existingModule.Version = request.Version;
                existingModule.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Updated module: {ModuleId} to version {Version}", 
                    request.ModuleId, request.Version);

                return Ok(new ModuleRegistrationResponse
                {
                    Success = true,
                    Message = $"Module '{request.ModuleId}' updated to version {request.Version}",
                    ModuleId = existingModule.ModuleId,
                    CertificateCode = existingModule.CertificateCode
                });
            }

            // Check if certificate code is already used by another module
            var codeConflict = await _db.Modules
                .FirstOrDefaultAsync(m => m.CertificateCode == request.CertificateCode);

            if (codeConflict != null)
            {
                return BadRequest(new ModuleRegistrationResponse
                {
                    Success = false,
                    Message = $"Certificate code '{request.CertificateCode}' already used by module '{codeConflict.ModuleId}'"
                });
            }

            // Create new module
            var newModule = new ModuleRecord
            {
                ModuleId = request.ModuleId,
                DisplayName = request.DisplayName ?? request.ModuleId,
                Description = $"Auto-registered module {request.ModuleId}",
                Category = "Auto-Registered",
                DefaultTier = "Professional",
                CertificateCode = request.CertificateCode,
                Version = request.Version,
                DisplayOrder = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Modules.Add(newModule);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Registered new module: {ModuleId} with certificate code {CertificateCode}", 
                request.ModuleId, request.CertificateCode);

            return Ok(new ModuleRegistrationResponse
            {
                Success = true,
                Message = $"Module '{request.ModuleId}' registered successfully",
                ModuleId = newModule.ModuleId,
                CertificateCode = newModule.CertificateCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering module: {ModuleId}", request.ModuleId);
            return StatusCode(500, new ModuleRegistrationResponse
            {
                Success = false,
                Message = "Failed to register module"
            });
        }
    }

    #endregion

    #region Tier Management

    /// <summary>
    /// Gets all tiers with their modules (for License Generator and Admin Panel)
    /// </summary>
    [HttpGet("tiers")]
    public async Task<ActionResult<List<TierInfo>>> GetTiers()
    {
        try
        {
            var tiers = await _db.LicenseTiers
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var tierModules = await _db.TierModules.ToListAsync();
            var modules = await _db.Modules.Where(m => m.IsActive).ToListAsync();

            var result = tiers.Select(t => new TierInfo
            {
                Id = t.Id,
                TierId = t.TierId,
                DisplayName = t.DisplayName,
                Description = t.Description,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                Modules = tierModules
                    .Where(tm => tm.TierId == t.TierId)
                    .Join(modules, tm => tm.ModuleId, m => m.ModuleId, (tm, m) => new ModuleInfo
                    {
                        Id = m.Id,
                        ModuleId = m.ModuleId,
                        DisplayName = m.DisplayName,
                        Description = m.Description,
                        Category = m.Category,
                        Icon = m.Icon,
                        CertificateCode = m.CertificateCode,
                        Version = m.Version,
                        IsActive = m.IsActive
                    })
                    .ToList()
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tiers");
            return StatusCode(500, new { error = "Failed to fetch tiers" });
        }
    }

    /// <summary>
    /// Updates tier modules
    /// </summary>
    [HttpPut("tiers/{tierId}/modules")]
    public async Task<ActionResult> UpdateTierModules(string tierId, [FromBody] UpdateTierModulesRequest request)
    {
        try
        {
            var tier = await _db.LicenseTiers.FirstOrDefaultAsync(t => t.TierId == tierId);
            if (tier == null)
                return NotFound(new { error = "Tier not found" });

            // Remove existing tier modules
            var existingMappings = await _db.TierModules.Where(tm => tm.TierId == tierId).ToListAsync();
            _db.TierModules.RemoveRange(existingMappings);

            // Add new tier modules
            foreach (var moduleId in request.ModuleIds)
            {
                _db.TierModules.Add(new TierModuleRecord
                {
                    TierId = tierId,
                    ModuleId = moduleId
                });
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated modules for tier: {TierId}", tierId);
            return Ok(new { message = "Tier modules updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tier modules: {TierId}", tierId);
            return StatusCode(500, new { error = "Failed to update tier modules" });
        }
    }

    #endregion

    #region Branding Endpoints (New for Fathom OS)

    /// <summary>
    /// Checks if a licensee code is available (not already in use).
    /// GET /api/admin/branding/check-code/{code}
    /// </summary>
    [HttpGet("branding/check-code/{code}")]
    public async Task<ActionResult<LicenseeCodeCheckResult>> CheckLicenseeCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 2)
        {
            return BadRequest(new LicenseeCodeCheckResult
            {
                IsValid = false,
                IsAvailable = false,
                Message = "Licensee code must be exactly 2 characters (A-Z or 0-9)"
            });
        }

        code = code.ToUpperInvariant();
        
        if (!LicenseConstants.IsValidLicenseeCode(code))
        {
            return Ok(new LicenseeCodeCheckResult
            {
                IsValid = false,
                IsAvailable = false,
                Message = "Licensee code must be exactly 2 characters (A-Z or 0-9)"
            });
        }

        var exists = await _db.LicenseKeys.AnyAsync(l => l.LicenseeCode == code);
        
        return Ok(new LicenseeCodeCheckResult
        {
            IsValid = true,
            IsAvailable = !exists,
            Code = code,
            Message = exists 
                ? $"Code '{code}' is already in use by another license" 
                : $"Code '{code}' is available"
        });
    }

    /// <summary>
    /// Gets all used licensee codes.
    /// GET /api/admin/branding/used-codes
    /// </summary>
    [HttpGet("branding/used-codes")]
    public async Task<ActionResult<List<UsedLicenseeCode>>> GetUsedLicenseeCodes()
    {
        var codes = await _db.LicenseKeys
            .Where(l => l.LicenseeCode != null)
            .Select(l => new UsedLicenseeCode
            {
                Code = l.LicenseeCode!,
                Brand = l.Brand,
                CustomerEmail = l.CustomerEmail,
                LicenseId = l.LicenseId
            })
            .Distinct()
            .ToListAsync();

        return Ok(codes);
    }

    /// <summary>
    /// Gets the logo for a licensee by code.
    /// GET /api/branding/logo/{licenseeCode}
    /// This is a PUBLIC endpoint - no authentication required.
    /// </summary>
    [HttpGet("/api/branding/logo/{licenseeCode}")]
    public async Task<ActionResult> GetBrandLogo(string licenseeCode)
    {
        if (string.IsNullOrEmpty(licenseeCode))
        {
            return BadRequest("Licensee code is required");
        }

        var license = await _db.LicenseKeys
            .Where(l => l.LicenseeCode == licenseeCode.ToUpperInvariant())
            .Select(l => new { l.BrandLogo, l.BrandLogoUrl })
            .FirstOrDefaultAsync();

        if (license == null)
        {
            return NotFound(new { message = "Licensee not found" });
        }

        // Return logo URL if available
        if (!string.IsNullOrEmpty(license.BrandLogoUrl))
        {
            return Ok(new { logoUrl = license.BrandLogoUrl });
        }

        // Return base64 logo if available
        if (!string.IsNullOrEmpty(license.BrandLogo))
        {
            return Ok(new { logoBase64 = license.BrandLogo });
        }

        return NotFound(new { message = "No logo found for this licensee" });
    }

    /// <summary>
    /// Updates branding for a license.
    /// PUT /api/admin/licenses/{id}/branding
    /// </summary>
    [HttpPut("licenses/{id}/branding")]
    public async Task<ActionResult> UpdateLicenseBranding(int id, [FromBody] UpdateBrandingRequest request)
    {
        var license = await _db.LicenseKeys.FindAsync(id);
        if (license == null)
        {
            return NotFound(new { message = "License not found" });
        }

        // If changing licensee code, validate it
        if (!string.IsNullOrEmpty(request.LicenseeCode) && request.LicenseeCode != license.LicenseeCode)
        {
            var code = request.LicenseeCode.ToUpperInvariant();
            if (!LicenseConstants.IsValidLicenseeCode(code))
            {
                return BadRequest(new { message = "Licensee code must be exactly 2 uppercase letters (A-Z)" });
            }

            var exists = await _db.LicenseKeys.AnyAsync(l => l.LicenseeCode == code && l.Id != id);
            if (exists)
            {
                return BadRequest(new { message = $"Code '{code}' is already in use by another license" });
            }

            license.LicenseeCode = code;
            // Regenerate support code with new licensee code
            license.SupportCode = LicenseConstants.GenerateSupportCode(code);
        }

        if (request.Brand != null)
        {
            license.Brand = request.Brand;
        }

        if (request.BrandLogo != null)
        {
            // Validate logo size (≤20KB)
            if (request.BrandLogo.Length > LicenseConstants.MaxLogoSizeBytes * 1.4) // Base64 is ~1.33x larger
            {
                return BadRequest(new { message = "Logo must be ≤20KB" });
            }
            license.BrandLogo = request.BrandLogo;
        }

        if (request.BrandLogoUrl != null)
        {
            license.BrandLogoUrl = request.BrandLogoUrl;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated branding for license {LicenseId}: Brand={Brand}, Code={Code}", 
            license.LicenseId, license.Brand, license.LicenseeCode);

        return Ok(new
        {
            message = "Branding updated successfully",
            brand = license.Brand,
            licenseeCode = license.LicenseeCode,
            supportCode = license.SupportCode
        });
    }

    /// <summary>
    /// Regenerates support code for a license.
    /// POST /api/admin/licenses/{id}/regenerate-support-code
    /// </summary>
    [HttpPost("licenses/{id}/regenerate-support-code")]
    public async Task<ActionResult> RegenerateSupportCode(int id)
    {
        var license = await _db.LicenseKeys.FindAsync(id);
        if (license == null)
        {
            return NotFound(new { message = "License not found" });
        }

        if (string.IsNullOrEmpty(license.LicenseeCode))
        {
            return BadRequest(new { message = "License does not have a licensee code" });
        }

        license.SupportCode = LicenseConstants.GenerateSupportCode(license.LicenseeCode);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Regenerated support code for license {LicenseId}", license.LicenseId);

        return Ok(new
        {
            message = "Support code regenerated",
            supportCode = license.SupportCode
        });
    }

    #endregion

    #region Analytics Endpoints

    /// <summary>
    /// Get comprehensive usage analytics
    /// GET /api/admin/analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<AdminAnalyticsResponse>> GetAnalytics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            // License statistics
            var totalLicenses = await _db.LicenseKeys.CountAsync();
            var activeLicenses = await _db.LicenseKeys.CountAsync(l => !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow);
            var expiredLicenses = await _db.LicenseKeys.CountAsync(l => !l.IsRevoked && l.ExpiresAt <= DateTime.UtcNow);
            var revokedLicenses = await _db.LicenseKeys.CountAsync(l => l.IsRevoked);
            var offlineLicenses = await _db.LicenseKeys.CountAsync(l => l.LicenseType == "Offline");

            // Recent activity
            var newLicensesThisMonth = await _db.LicenseKeys
                .CountAsync(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-30));
            var activationsThisMonth = await _db.LicenseActivations
                .CountAsync(a => a.ActivatedAt >= DateTime.UtcNow.AddDays(-30));

            // Edition breakdown
            var editionBreakdown = await _db.LicenseKeys
                .Where(l => !l.IsRevoked)
                .GroupBy(l => l.Edition ?? "Unknown")
                .Select(g => new EditionStatistic { Edition = g.Key, Count = g.Count() })
                .ToListAsync();

            // Daily activity
            var dailyActivity = await _db.LicenseActivations
                .Where(a => a.ActivatedAt >= fromDate && a.ActivatedAt <= toDate)
                .GroupBy(a => a.ActivatedAt.Date)
                .Select(g => new DailyActivityStat
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Activations = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            // Usage analytics from UsageAnalytics table
            var usageEvents = await _db.UsageAnalytics
                .Where(u => u.Timestamp >= fromDate && u.Timestamp <= toDate)
                .GroupBy(u => u.EventType)
                .Select(g => new UsageEventStat { EventType = g.Key, Count = g.Count() })
                .ToListAsync();

            // Module usage
            var moduleUsage = await _db.UsageAnalytics
                .Where(u => u.EventType == "ModuleLaunch" && u.EntityId != null && u.Timestamp >= fromDate && u.Timestamp <= toDate)
                .GroupBy(u => u.EntityId!)
                .Select(g => new ModuleUsageStat { ModuleId = g.Key, LaunchCount = g.Count() })
                .OrderByDescending(m => m.LaunchCount)
                .Take(20)
                .ToListAsync();

            // Floating pool stats
            var activeFloatingCheckouts = await _db.FloatingPoolCheckouts
                .CountAsync(c => c.IsActive);
            var totalFloatingCheckouts = await _db.FloatingPoolCheckouts.CountAsync();

            // Transfer stats
            var completedTransfers = await _db.LicenseTransfers
                .CountAsync(t => t.Status == "Completed" && t.TransferredAt >= fromDate);
            var pendingTransfers = await _db.LicenseTransfers
                .CountAsync(t => t.Status == "Pending");

            return Ok(new AdminAnalyticsResponse
            {
                GeneratedAt = DateTime.UtcNow,
                PeriodFrom = fromDate,
                PeriodTo = toDate,
                LicenseStats = new AdminLicenseStatistics
                {
                    Total = totalLicenses,
                    Active = activeLicenses,
                    Expired = expiredLicenses,
                    Revoked = revokedLicenses,
                    Offline = offlineLicenses,
                    NewThisMonth = newLicensesThisMonth
                },
                ActivationStats = new AdminActivationStatistics
                {
                    TotalActivations = await _db.LicenseActivations.CountAsync(),
                    ActiveActivations = await _db.LicenseActivations.CountAsync(a => !a.IsDeactivated),
                    ActivationsThisMonth = activationsThisMonth
                },
                EditionBreakdown = editionBreakdown,
                DailyActivity = dailyActivity,
                UsageEvents = usageEvents,
                ModuleUsage = moduleUsage,
                FloatingPoolStats = new FloatingPoolStatistics
                {
                    ActiveCheckouts = activeFloatingCheckouts,
                    TotalCheckouts = totalFloatingCheckouts
                },
                TransferStats = new AdminTransferStatistics
                {
                    CompletedTransfers = completedTransfers,
                    PendingTransfers = pendingTransfers
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics");
            return StatusCode(500, new { error = "Failed to generate analytics" });
        }
    }

    /// <summary>
    /// Get usage analytics for a specific license
    /// GET /api/admin/analytics/license/{licenseId}
    /// </summary>
    [HttpGet("analytics/license/{licenseId}")]
    public async Task<ActionResult<LicenseAnalyticsResponse>> GetLicenseAnalytics(
        string licenseId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var license = await _db.LicenseKeys
                .Include(l => l.Activations)
                .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

            if (license == null)
            {
                return NotFound(new { error = "License not found" });
            }

            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var usageEvents = await _db.UsageAnalytics
                .Where(u => u.LicenseId == licenseId && u.Timestamp >= fromDate && u.Timestamp <= toDate)
                .ToListAsync();

            var moduleUsage = usageEvents
                .Where(e => e.EventType == "ModuleLaunch" && e.EntityId != null)
                .GroupBy(e => e.EntityId!)
                .ToDictionary(g => g.Key, g => g.Count());

            var featureUsage = usageEvents
                .Where(e => e.EventType == "FeatureUsage" && e.EntityId != null)
                .GroupBy(e => e.EntityId!)
                .ToDictionary(g => g.Key, g => g.Count());

            var sessionCount = usageEvents.Count(e => e.EventType == "SessionStart");
            var totalUsageMinutes = usageEvents
                .Where(e => e.EventType == "SessionEnd" && e.Properties != null)
                .Sum(e => {
                    try
                    {
                        var props = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(e.Properties!);
                        if (props != null && props.TryGetValue("DurationMinutes", out var duration) && int.TryParse(duration, out var mins))
                            return mins;
                    }
                    catch { }
                    return 0;
                });

            return Ok(new LicenseAnalyticsResponse
            {
                LicenseId = licenseId,
                CustomerName = license.CustomerName,
                Edition = license.Edition,
                PeriodFrom = fromDate,
                PeriodTo = toDate,
                TotalSessions = sessionCount,
                TotalUsageMinutes = totalUsageMinutes,
                ModuleUsage = moduleUsage,
                FeatureUsage = featureUsage,
                ActivationCount = license.Activations.Count(a => !a.IsDeactivated),
                LastSeen = license.Activations.Where(a => !a.IsDeactivated).Max(a => a.LastSeenAt)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license analytics for {LicenseId}", licenseId);
            return StatusCode(500, new { error = "Failed to get license analytics" });
        }
    }

    /// <summary>
    /// Export analytics data as CSV
    /// GET /api/admin/analytics/export
    /// </summary>
    [HttpGet("analytics/export")]
    public async Task<ActionResult> ExportAnalytics([FromQuery] string type = "licenses")
    {
        try
        {
            string csv;
            string filename;

            switch (type.ToLower())
            {
                case "licenses":
                    var licenses = await _db.LicenseKeys
                        .OrderByDescending(l => l.CreatedAt)
                        .Take(1000)
                        .ToListAsync();

                    csv = "LicenseId,CustomerEmail,CustomerName,Edition,CreatedAt,ExpiresAt,Status,Brand,LicenseeCode\n" +
                        string.Join("\n", licenses.Select(l =>
                            $"\"{l.LicenseId}\",\"{l.CustomerEmail}\",\"{l.CustomerName}\",\"{l.Edition}\"," +
                            $"\"{l.CreatedAt:yyyy-MM-dd}\",\"{l.ExpiresAt:yyyy-MM-dd}\"," +
                            $"\"{(l.IsRevoked ? "Revoked" : l.ExpiresAt < DateTime.UtcNow ? "Expired" : "Active")}\"," +
                            $"\"{l.Brand}\",\"{l.LicenseeCode}\""));
                    filename = $"licenses_export_{DateTime.Now:yyyyMMdd}.csv";
                    break;

                case "activations":
                    var activations = await _db.LicenseActivations
                        .Include(a => a.LicenseKey)
                        .OrderByDescending(a => a.ActivatedAt)
                        .Take(1000)
                        .ToListAsync();

                    csv = "LicenseId,MachineName,ActivatedAt,LastSeenAt,IpAddress,AppVersion\n" +
                        string.Join("\n", activations.Select(a =>
                            $"\"{a.LicenseKey?.LicenseId}\",\"{a.MachineName}\",\"{a.ActivatedAt:yyyy-MM-dd HH:mm}\"," +
                            $"\"{a.LastSeenAt:yyyy-MM-dd HH:mm}\",\"{a.IpAddress}\",\"{a.AppVersion}\""));
                    filename = $"activations_export_{DateTime.Now:yyyyMMdd}.csv";
                    break;

                case "usage":
                    var usage = await _db.UsageAnalytics
                        .OrderByDescending(u => u.Timestamp)
                        .Take(5000)
                        .ToListAsync();

                    csv = "LicenseId,EventType,EntityId,Timestamp,MachineName\n" +
                        string.Join("\n", usage.Select(u =>
                            $"\"{u.LicenseId}\",\"{u.EventType}\",\"{u.EntityId}\",\"{u.Timestamp:yyyy-MM-dd HH:mm}\",\"{u.MachineName}\""));
                    filename = $"usage_export_{DateTime.Now:yyyyMMdd}.csv";
                    break;

                default:
                    return BadRequest(new { error = "Invalid export type. Use: licenses, activations, or usage" });
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics");
            return StatusCode(500, new { error = "Export failed" });
        }
    }

    #endregion

    #region Revocation Endpoints

    /// <summary>
    /// Revoke a license by license ID (string)
    /// POST /api/admin/revoke/{licenseId}
    /// </summary>
    [HttpPost("revoke/{licenseId}")]
    public async Task<ActionResult> RevokeLicenseById(string licenseId, [FromBody] RevokeLicenseRequest? request = null)
    {
        var license = await _db.LicenseKeys.FirstOrDefaultAsync(l => l.LicenseId == licenseId);
        if (license == null)
        {
            return NotFound(new { error = "License not found" });
        }

        if (license.IsRevoked)
        {
            return BadRequest(new { error = "License is already revoked" });
        }

        license.IsRevoked = true;
        license.RevokedAt = DateTime.UtcNow;
        license.RevocationReason = request?.Reason ?? "Revoked by admin";

        // Add revocation record
        _db.Revocations.Add(new RevocationRecord
        {
            LicenseId = licenseId,
            RevokedAt = DateTime.UtcNow,
            Reason = license.RevocationReason,
            RevokedBy = "Admin API"
        });

        // Terminate any active sessions
        var activeSessions = await _db.ActiveSessions
            .Where(s => s.LicenseId == licenseId && s.IsActive)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "License revoked";
        }

        // Terminate floating checkouts
        var floatingCheckouts = await _db.FloatingPoolCheckouts
            .Where(c => c.LicenseId == licenseId && c.IsActive)
            .ToListAsync();

        foreach (var checkout in floatingCheckouts)
        {
            checkout.IsActive = false;
            checkout.CheckedInAt = DateTime.UtcNow;
            checkout.CheckInReason = "License revoked";
        }

        await _db.SaveChangesAsync();

        _logger.LogWarning("License revoked: {LicenseId}, Reason: {Reason}", licenseId, license.RevocationReason);

        return Ok(new
        {
            message = "License revoked successfully",
            licenseId,
            revokedAt = license.RevokedAt,
            reason = license.RevocationReason,
            terminatedSessions = activeSessions.Count,
            terminatedCheckouts = floatingCheckouts.Count
        });
    }

    /// <summary>
    /// Bulk revoke multiple licenses
    /// POST /api/admin/revoke/bulk
    /// </summary>
    [HttpPost("revoke/bulk")]
    public async Task<ActionResult> BulkRevokeLicenses([FromBody] BulkRevokeRequest request)
    {
        if (request.LicenseIds == null || !request.LicenseIds.Any())
        {
            return BadRequest(new { error = "No license IDs provided" });
        }

        var revokedCount = 0;
        var failedIds = new List<string>();

        foreach (var licenseId in request.LicenseIds)
        {
            var license = await _db.LicenseKeys.FirstOrDefaultAsync(l => l.LicenseId == licenseId);
            if (license == null || license.IsRevoked)
            {
                failedIds.Add(licenseId);
                continue;
            }

            license.IsRevoked = true;
            license.RevokedAt = DateTime.UtcNow;
            license.RevocationReason = request.Reason ?? "Bulk revocation by admin";

            _db.Revocations.Add(new RevocationRecord
            {
                LicenseId = licenseId,
                RevokedAt = DateTime.UtcNow,
                Reason = license.RevocationReason,
                RevokedBy = "Admin API (Bulk)"
            });

            revokedCount++;
        }

        await _db.SaveChangesAsync();

        _logger.LogWarning("Bulk revocation: {Count} licenses revoked, {Failed} failed",
            revokedCount, failedIds.Count);

        return Ok(new
        {
            message = $"Revoked {revokedCount} licenses",
            revokedCount,
            failedIds,
            failedCount = failedIds.Count
        });
    }

    /// <summary>
    /// Get revocation history
    /// GET /api/admin/revocations
    /// </summary>
    [HttpGet("revocations")]
    public async Task<ActionResult> GetRevocations([FromQuery] int limit = 50)
    {
        var revocations = await _db.Revocations
            .OrderByDescending(r => r.RevokedAt)
            .Take(limit)
            .Select(r => new
            {
                r.Id,
                r.LicenseId,
                r.RevokedAt,
                r.Reason,
                r.RevokedBy
            })
            .ToListAsync();

        return Ok(revocations);
    }

    #endregion

    #region Server Admin Endpoints

    /// <summary>
    /// Get server health status
    /// GET /api/admin/health
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> GetHealth()
    {
        var dbSize = 0L;
        try
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "licenses.db");
            if (System.IO.File.Exists(dbPath))
                dbSize = new FileInfo(dbPath).Length / (1024 * 1024); // MB
        }
        catch { }

        var totalLicenses = await _db.LicenseKeys.CountAsync();
        var activeLicenses = await _db.LicenseKeys.CountAsync(l => !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow);
        
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        var uptimeStr = uptime.TotalDays >= 1 
            ? $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m"
            : uptime.TotalHours >= 1 
                ? $"{(int)uptime.TotalHours}h {uptime.Minutes}m"
                : $"{uptime.Minutes}m {uptime.Seconds}s";

        return Ok(new
        {
            status = "Healthy",
            uptime = uptimeStr,
            memoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            databaseSizeMb = (double)dbSize,
            totalLicenses,
            activeLicenses,
            lastBackup = (DateTime?)null,
            activeSessions = 0
        });
    }

    /// <summary>
    /// Create backup
    /// POST /api/admin/backup
    /// </summary>
    [HttpPost("backup")]
    public async Task<ActionResult> CreateBackup()
    {
        try
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "licenses.db");
            var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            Directory.CreateDirectory(backupDir);
            
            var backupFile = Path.Combine(backupDir, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db");
            System.IO.File.Copy(dbPath, backupFile, true);
            
            return Ok(new { message = "Backup created", path = backupFile });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Backup failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Download latest backup
    /// GET /api/admin/backup/download
    /// </summary>
    [HttpGet("backup/download")]
    public ActionResult DownloadBackup()
    {
        var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
        if (!Directory.Exists(backupDir))
            return NotFound(new { message = "No backups found" });

        var latestBackup = Directory.GetFiles(backupDir, "*.db")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (latestBackup == null)
            return NotFound(new { message = "No backups found" });

        var bytes = System.IO.File.ReadAllBytes(latestBackup);
        return File(bytes, "application/octet-stream", Path.GetFileName(latestBackup));
    }

    /// <summary>
    /// Restore from latest backup
    /// POST /api/admin/restore
    /// </summary>
    [HttpPost("restore")]
    public ActionResult RestoreBackup()
    {
        try
        {
            var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            if (!Directory.Exists(backupDir))
                return NotFound(new { message = "No backups found" });

            var latestBackup = Directory.GetFiles(backupDir, "*.db")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (latestBackup == null)
                return NotFound(new { message = "No backups found to restore" });

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "licenses.db");
            
            // Create a backup of current before restoring
            var preRestoreBackup = Path.Combine(backupDir, $"pre_restore_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db");
            if (System.IO.File.Exists(dbPath))
                System.IO.File.Copy(dbPath, preRestoreBackup, true);

            // Restore
            System.IO.File.Copy(latestBackup, dbPath, true);

            _logger.LogWarning("Database restored from backup: {BackupFile}", latestBackup);
            
            return Ok(new { message = $"Database restored from {Path.GetFileName(latestBackup)}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Restore failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// List active sessions
    /// GET /api/admin/sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult> GetSessions()
    {
        // Get recent activations as "sessions"
        var sessions = await _db.LicenseActivations
            .Include(a => a.LicenseKey)
            .OrderByDescending(a => a.LastSeenAt)
            .Take(50)
            .Select(a => new
            {
                sessionToken = a.Id.ToString(),
                licenseId = a.LicenseKey != null ? a.LicenseKey.LicenseId : "Unknown",
                machineName = a.MachineName,
                ipAddress = a.IpAddress,
                startedAt = a.ActivatedAt,
                lastHeartbeat = a.LastSeenAt
            })
            .ToListAsync();

        return Ok(sessions);
    }

    /// <summary>
    /// Get recent license activations with full details
    /// GET /api/admin/activations
    /// </summary>
    [HttpGet("activations")]
    public async Task<ActionResult> GetRecentActivations([FromQuery] int limit = 50, [FromQuery] bool includeOffline = true)
    {
        var query = _db.LicenseActivations
            .Include(a => a.LicenseKey)
            .Where(a => !a.IsDeactivated)
            .OrderByDescending(a => a.ActivatedAt);

        var activations = await query
            .Take(limit)
            .Select(a => new
            {
                id = a.Id,
                licenseId = a.LicenseKey != null ? a.LicenseKey.LicenseId : null,
                customerName = a.LicenseKey != null ? a.LicenseKey.CustomerName : null,
                customerEmail = a.LicenseKey != null ? a.LicenseKey.CustomerEmail : null,
                edition = a.LicenseKey != null ? a.LicenseKey.Edition : null,
                brand = a.LicenseKey != null ? a.LicenseKey.Brand : null,
                hardwareFingerprint = a.HardwareFingerprint,
                machineName = a.MachineName,
                ipAddress = a.IpAddress,
                appVersion = a.AppVersion,
                osVersion = a.OsVersion,
                activatedAt = a.ActivatedAt,
                lastSeenAt = a.LastSeenAt,
                isOffline = a.LicenseKey != null && a.LicenseKey.IsOfflineGenerated
            })
            .ToListAsync();

        return Ok(activations);
    }

    /// <summary>
    /// Get activation status for a specific license
    /// GET /api/admin/licenses/{id}/activation-status
    /// </summary>
    [HttpGet("licenses/{id}/activation-status")]
    public async Task<ActionResult> GetLicenseActivationStatus(int id)
    {
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (license == null)
            return NotFound(new { error = "License not found" });

        var activeActivation = license.Activations.FirstOrDefault(a => !a.IsDeactivated);

        return Ok(new
        {
            licenseId = license.LicenseId,
            isActivated = activeActivation != null,
            activationDetails = activeActivation == null ? null : new
            {
                hardwareFingerprint = activeActivation.HardwareFingerprint,
                machineName = activeActivation.MachineName,
                ipAddress = activeActivation.IpAddress,
                appVersion = activeActivation.AppVersion,
                osVersion = activeActivation.OsVersion,
                activatedAt = activeActivation.ActivatedAt,
                lastSeenAt = activeActivation.LastSeenAt
            }
        });
    }

    /// <summary>
    /// Terminate a session
    /// DELETE /api/admin/sessions/{token}
    /// </summary>
    [HttpDelete("sessions/{token}")]
    public async Task<ActionResult> TerminateSession(string token)
    {
        if (int.TryParse(token, out var activationId))
        {
            var activation = await _db.LicenseActivations.FindAsync(activationId);
            if (activation != null)
            {
                _db.LicenseActivations.Remove(activation);
                await _db.SaveChangesAsync();
                return Ok(new { message = "Session terminated" });
            }
        }
        return NotFound(new { message = "Session not found" });
    }

    /// <summary>
    /// Terminate all sessions
    /// DELETE /api/admin/sessions
    /// </summary>
    [HttpDelete("sessions")]
    public async Task<ActionResult> TerminateAllSessions()
    {
        var count = await _db.LicenseActivations.CountAsync();
        _db.LicenseActivations.RemoveRange(_db.LicenseActivations);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Terminated {count} sessions" });
    }

    /// <summary>
    /// Get blocked IPs
    /// GET /api/admin/blocked-ips
    /// </summary>
    [HttpGet("blocked-ips")]
    public ActionResult GetBlockedIps()
    {
        // Return empty list for now - can be expanded with a blocked IPs table
        return Ok(new List<object>());
    }

    /// <summary>
    /// Block an IP
    /// POST /api/admin/blocked-ips
    /// </summary>
    [HttpPost("blocked-ips")]
    public ActionResult BlockIp([FromBody] BlockIpRequest request)
    {
        // Placeholder - would add to blocked IPs table
        _logger.LogWarning("IP blocked: {IP} - Reason: {Reason}", request.IpAddress, request.Reason);
        return Ok(new { message = $"IP {request.IpAddress} blocked" });
    }

    /// <summary>
    /// Unblock an IP
    /// DELETE /api/admin/blocked-ips/{ip}
    /// </summary>
    [HttpDelete("blocked-ips/{ip}")]
    public ActionResult UnblockIp(string ip)
    {
        _logger.LogInformation("IP unblocked: {IP}", ip);
        return Ok(new { message = $"IP {ip} unblocked" });
    }

    /// <summary>
    /// Get audit logs
    /// GET /api/admin/audit
    /// </summary>
    [HttpGet("audit")]
    public async Task<ActionResult> GetAuditLogs([FromQuery] int limit = 50, [FromQuery] string? eventType = null, [FromQuery] string? licenseId = null, [FromQuery] string? ip = null)
    {
        var query = _db.AuditLogs.AsQueryable();
        
        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(a => a.Action == eventType);
        if (!string.IsNullOrEmpty(licenseId))
            query = query.Where(a => a.EntityId == licenseId);
        if (!string.IsNullOrEmpty(ip))
            query = query.Where(a => a.IpAddress != null && a.IpAddress.Contains(ip));

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new
            {
                id = a.Id,
                eventType = a.Action,
                message = $"{a.Action} {a.EntityType ?? ""} {a.EntityId ?? ""}".Trim(),
                licenseId = a.EntityId,
                ipAddress = a.IpAddress,
                timestamp = a.Timestamp,
                details = a.NewValues
            })
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// Export audit logs as CSV
    /// GET /api/admin/audit/export
    /// </summary>
    [HttpGet("audit/export")]
    public async Task<ActionResult> ExportAuditLogs()
    {
        var logs = await _db.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(1000)
            .ToListAsync();

        var csv = "Timestamp,Action,EntityType,EntityId,IpAddress,UserEmail\n" +
            string.Join("\n", logs.Select(l => 
                $"\"{l.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{l.Action}\",\"{l.EntityType}\",\"{l.EntityId}\",\"{l.IpAddress}\",\"{l.UserEmail}\""));

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"audit_logs_{DateTime.Now:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Get customer statistics
    /// GET /api/admin/customers/stats
    /// </summary>
    [HttpGet("customers/stats")]
    public async Task<ActionResult> GetCustomerStats()
    {
        var total = await _db.LicenseKeys.Select(l => l.CustomerEmail).Distinct().CountAsync();
        var active = await _db.LicenseKeys.Where(l => !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow).Select(l => l.CustomerEmail).Distinct().CountAsync();
        var expiring = await _db.LicenseKeys.Where(l => !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow && l.ExpiresAt < DateTime.UtcNow.AddDays(30)).CountAsync();

        return Ok(new
        {
            totalCustomers = total,
            activeSubscriptions = active,
            expiringSoon = expiring
        });
    }

    /// <summary>
    /// List customers
    /// GET /api/admin/customers
    /// </summary>
    [HttpGet("customers")]
    public async Task<ActionResult> GetCustomers([FromQuery] int limit = 50, [FromQuery] string? email = null, [FromQuery] string? name = null)
    {
        var query = _db.LicenseKeys.AsQueryable();

        if (!string.IsNullOrEmpty(email))
            query = query.Where(l => l.CustomerEmail != null && l.CustomerEmail.Contains(email));
        if (!string.IsNullOrEmpty(name))
            query = query.Where(l => l.CustomerName != null && l.CustomerName.Contains(name));

        var customers = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .Select(l => new
            {
                customerName = l.CustomerName,
                customerEmail = l.CustomerEmail,
                edition = l.Edition,
                expiresAt = l.ExpiresAt,
                isActive = !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow,
                licenseKey = l.Key
            })
            .ToListAsync();

        return Ok(customers);
    }

    #endregion
}

public class BlockIpRequest
{
    public string IpAddress { get; set; } = "";
    public string? Reason { get; set; }
}

// DTOs for Module Management

public class ModuleInfo
{
    public int Id { get; set; }
    public string ModuleId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public string? DefaultTier { get; set; }
    public int DisplayOrder { get; set; }
    public string? Icon { get; set; }
    public string? CertificateCode { get; set; }
    public string? Version { get; set; }
    public bool IsActive { get; set; }
}

public class AddModuleRequest
{
    public string ModuleId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? DefaultTier { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Icon { get; set; }
    public string? CertificateCode { get; set; }
}

public class UpdateModuleRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? DefaultTier { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Icon { get; set; }
    public string? CertificateCode { get; set; }
    public string? Version { get; set; }
    public bool? IsActive { get; set; }
}

public class TierInfo
{
    public int Id { get; set; }
    public string TierId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<ModuleInfo> Modules { get; set; } = new();
}

public class UpdateTierModulesRequest
{
    public List<string> ModuleIds { get; set; } = new();
}

// DTOs for Admin API

public class LicenseKeyInfo
{
    public int Id { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string LicenseId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Edition { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string SubscriptionType { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public bool IsActivated { get; set; }
    public int ActivationCount { get; set; }
    public DateTime? LastActivationDate { get; set; }
    public List<string> HardwareFingerprints { get; set; } = new();
    
    // White-Label Branding (New for Fathom OS)
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    
    // License Type & Status
    public string LicenseType { get; set; } = "Online";
    public bool IsOffline { get; set; }
    public string Status { get; set; } = "Active";
}

public class LicenseDetailsInfo : LicenseKeyInfo
{
    public string ProductName { get; set; } = string.Empty;
    public string Features { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public List<ActivationInfo> Activations { get; set; } = new();
}

public class ActivationInfo
{
    public int Id { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsDeactivated { get; set; }
    public string? HardwareFingerprint { get; set; }
    public string? MachineName { get; set; }
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
    public string? IpAddress { get; set; }
}

public class CreateLicenseRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }
    public string? Edition { get; set; }
    public string SubscriptionType { get; set; } = "Monthly"; // Accept as string
    public int DurationMonths { get; set; } = 1;
    public List<string>? Features { get; set; }
    
    // White-Label Branding (New for Fathom OS)
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? BrandLogo { get; set; }
    public string? BrandLogoUrl { get; set; }
    
    // License Type (Online/Offline)
    public string LicenseType { get; set; } = "Online";
    public string? HardwareId { get; set; } // Required for offline licenses
    public string? SupportCode { get; set; } // Custom support code (auto-generated if not provided)
    
    // Business Tracking (v3.3)
    public string? Notes { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Currency { get; set; }
    public string? SalesRep { get; set; }
    public string? ReferralSource { get; set; }
}

/// <summary>
/// Response for license creation - includes offline license file content if applicable
/// </summary>
public class CreateLicenseResponse
{
    public string? LicenseKey { get; set; }
    public string? LicenseId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? Edition { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? SubscriptionType { get; set; }
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    public string LicenseType { get; set; } = "Online";
    public string? LicenseFileContent { get; set; } // The signed license file content for offline licenses
    public string? QrCodeUrl { get; set; } // QR code for license verification (v3.3)
}

public class ExtendLicenseRequest
{
    public int Months { get; set; } = 1;
}

public class RevokeLicenseRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateLicenseFeaturesRequest
{
    public string? Edition { get; set; }
    public List<string>? Features { get; set; }
}

public class LicenseEditDetails
{
    public int Id { get; set; }
    public string LicenseId { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Edition { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string SubscriptionType { get; set; } = string.Empty;
    
    // White-Label Branding
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
}

public class DashboardStats
{
    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int ExpiredLicenses { get; set; }
    public int RevokedLicenses { get; set; }
    public int TotalActivations { get; set; }
    public int RecentActivations { get; set; }
    public int ExpiringThisMonth { get; set; }
}

public class RegisterOfflineLicenseRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }
    public string? Edition { get; set; }
    public string SubscriptionType { get; set; } = "Monthly";
    public int DurationMonths { get; set; } = 1;
    public List<string>? Features { get; set; }
    public string? LicenseId { get; set; } // The LicenseId from the offline file
    public bool IsOfflineGenerated { get; set; } = true;
    
    // White-Label Branding (New for Fathom OS)
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    public string? BrandLogo { get; set; }
    public string? BrandLogoUrl { get; set; }
}

// ============================================================================
// Branding DTOs (New for Fathom OS)
// ============================================================================

// LicenseeCodeCheckResult is defined in LicensingSystem.Shared

public class UsedLicenseeCode
{
    public string Code { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string LicenseId { get; set; } = string.Empty;
}

public class UpdateBrandingRequest
{
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? BrandLogo { get; set; }
    public string? BrandLogoUrl { get; set; }
}

// ============================================================================
// License Sync DTOs (for License Generator UI sync)
// ============================================================================

/// <summary>
/// Request to sync a single license record from the License Generator UI
/// </summary>
public class LicenseSyncRequest
{
    public string LicenseId { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? ClientCode { get; set; }
    public string? Edition { get; set; }
    public string? LicenseJson { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Features { get; set; }
    public string? Brand { get; set; }
}

/// <summary>
/// Response from license sync
/// </summary>
public class LicenseSyncResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? LicenseId { get; set; }
    public bool IsNew { get; set; }
}

/// <summary>
/// Request for bulk license sync
/// </summary>
public class BulkLicenseSyncRequest
{
    public List<LicenseSyncRequest> Licenses { get; set; } = new();
}

/// <summary>
/// Response from bulk license sync
/// </summary>
public class BulkLicenseSyncResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int SyncedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<string> FailedIds { get; set; } = new();
}

/// <summary>
/// Synced license information for display
/// </summary>
public class SyncedLicenseInfo
{
    public int Id { get; set; }
    public string LicenseId { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? ClientCode { get; set; }
    public string? Edition { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime SyncedAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Brand { get; set; }
    public string Status { get; set; } = "Active";
}

// ============================================================================
// Analytics DTOs
// ============================================================================

public class AdminAnalyticsResponse
{
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public AdminLicenseStatistics LicenseStats { get; set; } = new();
    public AdminActivationStatistics ActivationStats { get; set; } = new();
    public List<EditionStatistic> EditionBreakdown { get; set; } = new();
    public List<DailyActivityStat> DailyActivity { get; set; } = new();
    public List<UsageEventStat> UsageEvents { get; set; } = new();
    public List<ModuleUsageStat> ModuleUsage { get; set; } = new();
    public FloatingPoolStatistics FloatingPoolStats { get; set; } = new();
    public AdminTransferStatistics TransferStats { get; set; } = new();
}

public class AdminLicenseStatistics
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Expired { get; set; }
    public int Revoked { get; set; }
    public int Offline { get; set; }
    public int NewThisMonth { get; set; }
}

public class AdminActivationStatistics
{
    public int TotalActivations { get; set; }
    public int ActiveActivations { get; set; }
    public int ActivationsThisMonth { get; set; }
}

public class EditionStatistic
{
    public string Edition { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyActivityStat
{
    public string Date { get; set; } = string.Empty;
    public int Activations { get; set; }
}

public class UsageEventStat
{
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ModuleUsageStat
{
    public string ModuleId { get; set; } = string.Empty;
    public int LaunchCount { get; set; }
}

public class FloatingPoolStatistics
{
    public int ActiveCheckouts { get; set; }
    public int TotalCheckouts { get; set; }
}

public class AdminTransferStatistics
{
    public int CompletedTransfers { get; set; }
    public int PendingTransfers { get; set; }
}

public class LicenseAnalyticsResponse
{
    public string LicenseId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Edition { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int TotalSessions { get; set; }
    public int TotalUsageMinutes { get; set; }
    public Dictionary<string, int> ModuleUsage { get; set; } = new();
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
    public int ActivationCount { get; set; }
    public DateTime? LastSeen { get; set; }
}

public class BulkRevokeRequest
{
    public List<string> LicenseIds { get; set; } = new();
    public string? Reason { get; set; }
}

// ============================================================================
// Module Registration DTOs
// ============================================================================

public class ModuleRegistrationRequest
{
    public string ModuleId { get; set; } = string.Empty;
    public string CertificateCode { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Version { get; set; }
}

public class ModuleRegistrationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ModuleId { get; set; }
    public string? CertificateCode { get; set; }
}
