// LicensingSystem.Server/Controllers/CertificateController.cs
// Certificate management endpoints for Fathom OS

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Configuration;
using LicensingSystem.Server.Services;
using LicensingSystem.Shared;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace LicensingSystem.Server.Controllers;

/// <summary>
/// Controller for certificate management operations.
/// Handles syncing, verification, and listing of processing certificates.
///
/// PUBLIC ENDPOINTS (no authentication required):
///   - GET /api/certificates/verify/{certificateId} - Verify a certificate
///
/// PROTECTED ENDPOINTS (require X-API-Key header):
///   - POST /api/certificates/sync - Sync certificates from client
///   - GET /api/certificates/list - List certificates
///   - GET /api/certificates/{certificateId} - Get certificate details
///   - POST /api/certificates/sequence - Get next sequence number
/// </summary>
[ApiController]
[Route("api/certificates")]
public class CertificateController : ControllerBase
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<CertificateController> _logger;
    private readonly IConfiguration _config;
    private readonly IWebhookService? _webhookService;

    public CertificateController(
        LicenseDbContext db,
        ILogger<CertificateController> logger,
        IConfiguration config,
        IWebhookService? webhookService = null)
    {
        _db = db;
        _logger = logger;
        _config = config;
        _webhookService = webhookService;
    }

    /// <summary>
    /// Syncs certificates from client to server.
    /// POST /api/certificates/sync
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<CertificateSyncResponse>> SyncCertificates([FromBody] CertificateSyncRequest request)
    {
        if (request == null || !request.Certificates.Any())
        {
            return BadRequest(new CertificateSyncResponse
            {
                Success = false,
                Message = "No certificates provided"
            });
        }

        _logger.LogInformation("Syncing {Count} certificates for license {LicenseId}", 
            request.Certificates.Count, request.LicenseId);

        var syncedCount = 0;
        var failedIds = new List<string>();

        foreach (var cert in request.Certificates)
        {
            try
            {
                // Check if certificate already exists
                var existing = await _db.Certificates
                    .FirstOrDefaultAsync(c => c.CertificateId == cert.CertificateId);

                if (existing != null)
                {
                    _logger.LogDebug("Certificate {CertificateId} already exists, skipping", cert.CertificateId);
                    syncedCount++; // Count as success since it's already there
                    continue;
                }

                // Verify signature if present
                bool signatureVerified = false;
                if (!string.IsNullOrEmpty(cert.Signature))
                {
                    signatureVerified = VerifyCertificateSignature(cert);
                    if (!signatureVerified)
                    {
                        _logger.LogWarning("Certificate {CertificateId} signature verification failed", cert.CertificateId);
                    }
                }

                // Create new certificate record
                var record = new CertificateRecord
                {
                    CertificateId = cert.CertificateId,
                    LicenseId = request.LicenseId,
                    LicenseeCode = cert.LicenseeCode,
                    ModuleId = cert.ModuleId,
                    ModuleCertificateCode = cert.ModuleCertificateCode,
                    ModuleVersion = cert.ModuleVersion,
                    IssuedAt = cert.IssuedAt,
                    SyncedAt = DateTime.UtcNow,
                    ProjectName = cert.ProjectName,
                    ProjectLocation = cert.ProjectLocation,
                    Vessel = cert.Vessel,
                    Client = cert.Client,
                    SignatoryName = cert.SignatoryName,
                    SignatoryTitle = cert.SignatoryTitle,
                    CompanyName = cert.CompanyName,
                    ProcessingDataJson = JsonSerializer.Serialize(cert.ProcessingData),
                    InputFilesJson = JsonSerializer.Serialize(cert.InputFiles),
                    OutputFilesJson = JsonSerializer.Serialize(cert.OutputFiles),
                    Signature = cert.Signature,
                    SignatureAlgorithm = cert.SignatureAlgorithm,
                    IsSignatureVerified = signatureVerified
                };

                _db.Certificates.Add(record);
                syncedCount++;
                
                _logger.LogInformation("Synced certificate {CertificateId} from {Module} (signature verified: {Verified})", 
                    cert.CertificateId, cert.ModuleId, signatureVerified);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync certificate {CertificateId}", cert.CertificateId);
                failedIds.Add(cert.CertificateId);
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new CertificateSyncResponse
        {
            Success = failedIds.Count == 0,
            Message = failedIds.Count == 0 
                ? $"Successfully synced {syncedCount} certificates" 
                : $"Synced {syncedCount} certificates, {failedIds.Count} failed",
            SyncedCount = syncedCount,
            FailedIds = failedIds
        });
    }

    /// <summary>
    /// Verifies a certificate signature using the certificate public key.
    /// </summary>
    private bool VerifyCertificateSignature(ProcessingCertificate cert)
    {
        try
        {
            if (string.IsNullOrEmpty(cert.Signature))
                return false;

            // Get the public key
            string publicKeyPem;
            try
            {
                publicKeyPem = LicensingConfiguration.GetCertificatePublicKey(_config);
            }
            catch
            {
                _logger.LogWarning("Certificate public key not configured, skipping verification");
                return false;
            }

            // Build the data that was signed (same as client-side)
            var dataToSign = BuildSignatureData(cert);
            var dataBytes = Encoding.UTF8.GetBytes(dataToSign);

            // Parse the public key
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(publicKeyPem);

            // Decode the signature from base64
            var signatureBytes = Convert.FromBase64String(cert.Signature);

            // Verify
            return ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate signature for {CertificateId}", cert.CertificateId);
            return false;
        }
    }

    /// <summary>
    /// Builds the canonical string representation for signing/verification.
    /// Must match exactly what the client uses to sign.
    /// </summary>
    private static string BuildSignatureData(ProcessingCertificate cert)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CertificateId:{cert.CertificateId}");
        sb.AppendLine($"LicenseeCode:{cert.LicenseeCode}");
        sb.AppendLine($"ModuleId:{cert.ModuleId}");
        sb.AppendLine($"ModuleCertificateCode:{cert.ModuleCertificateCode}");
        sb.AppendLine($"ModuleVersion:{cert.ModuleVersion}");
        sb.AppendLine($"IssuedAt:{cert.IssuedAt:O}");
        sb.AppendLine($"ProjectName:{cert.ProjectName}");
        sb.AppendLine($"SignatoryName:{cert.SignatoryName}");
        sb.AppendLine($"CompanyName:{cert.CompanyName}");
        
        // Include processing data in deterministic order
        if (cert.ProcessingData != null && cert.ProcessingData.Count > 0)
        {
            foreach (var kvp in cert.ProcessingData.OrderBy(x => x.Key))
            {
                sb.AppendLine($"Data:{kvp.Key}={kvp.Value}");
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Verifies a certificate by ID.
    /// GET /api/certificates/verify/{certificateId}
    /// This is a PUBLIC endpoint - no authentication required.
    /// </summary>
    [HttpGet("verify/{certificateId}")]
    public async Task<ActionResult<CertificateVerificationResult>> VerifyCertificate(string certificateId)
    {
        _logger.LogInformation("Verifying certificate {CertificateId}", certificateId);

        var cert = await _db.Certificates
            .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

        if (cert == null)
        {
            _logger.LogWarning("Certificate not found: {CertificateId}", certificateId);
            return Ok(new CertificateVerificationResult
            {
                IsValid = false,
                CertificateId = certificateId,
                Message = "Certificate not found. This certificate ID is not in our records."
            });
        }

        // Get module display name
        var module = await _db.Modules.FirstOrDefaultAsync(m => m.ModuleId == cert.ModuleId);
        var moduleName = module?.DisplayName ?? cert.ModuleId;

        var message = cert.IsSignatureVerified
            ? "✓ This certificate is authentic and cryptographically verified."
            : "✓ This certificate is in our records (signature not verified).";

        return Ok(new CertificateVerificationResult
        {
            IsValid = true,
            CertificateId = cert.CertificateId,
            IssuedAt = cert.IssuedAt,
            CompanyName = cert.CompanyName,
            ModuleId = cert.ModuleId,
            ModuleName = moduleName,
            ProjectName = cert.ProjectName,
            IsSignatureVerified = cert.IsSignatureVerified,
            Message = message
        });
    }

    /// <summary>
    /// Lists certificates for a license.
    /// GET /api/certificates/list?licenseId={id}
    /// </summary>
    [HttpGet("list")]
    public async Task<ActionResult<List<CertificateListItem>>> ListCertificates(
        [FromQuery] string? licenseId,
        [FromQuery] string? licenseeCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Certificates.AsQueryable();

        if (!string.IsNullOrEmpty(licenseId))
        {
            query = query.Where(c => c.LicenseId == licenseId);
        }

        if (!string.IsNullOrEmpty(licenseeCode))
        {
            query = query.Where(c => c.LicenseeCode == licenseeCode);
        }

        var certificates = await query
            .OrderByDescending(c => c.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CertificateListItem
            {
                CertificateId = c.CertificateId,
                ModuleId = c.ModuleId,
                ProjectName = c.ProjectName,
                IssuedAt = c.IssuedAt,
                CompanyName = c.CompanyName,
                SignatoryName = c.SignatoryName
            })
            .ToListAsync();

        return Ok(certificates);
    }

    /// <summary>
    /// Gets the next certificate sequence number for a licensee.
    /// POST /api/certificates/sequence
    /// </summary>
    [HttpPost("sequence")]
    public async Task<ActionResult<CertificateSequenceResponse>> GetNextSequence([FromBody] CertificateSequenceRequest request)
    {
        if (string.IsNullOrEmpty(request.LicenseeCode) || request.LicenseeCode.Length != 2)
        {
            return BadRequest(new { message = "Invalid licensee code. Must be 2 uppercase letters." });
        }

        var yearMonth = DateTime.UtcNow.ToString("yyMM");
        
        // Find or create sequence record
        var sequence = await _db.CertificateSequences
            .FirstOrDefaultAsync(s => s.LicenseeCode == request.LicenseeCode && s.YearMonth == yearMonth);

        if (sequence == null)
        {
            sequence = new CertificateSequenceRecord
            {
                LicenseeCode = request.LicenseeCode,
                YearMonth = yearMonth,
                LastSequence = 0
            };
            _db.CertificateSequences.Add(sequence);
        }

        // Increment sequence
        sequence.LastSequence++;
        sequence.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Generate certificate ID
        var certificateId = LicenseConstants.GenerateCertificateId(
            request.LicenseeCode, 
            sequence.LastSequence, 
            DateTime.UtcNow);

        return Ok(new CertificateSequenceResponse
        {
            CertificateId = certificateId,
            SequenceNumber = sequence.LastSequence,
            YearMonth = yearMonth
        });
    }

    /// <summary>
    /// Gets full certificate details.
    /// GET /api/certificates/{certificateId}
    /// </summary>
    [HttpGet("{certificateId}")]
    public async Task<ActionResult<CertificateDetailResponse>> GetCertificateDetails(string certificateId)
    {
        var cert = await _db.Certificates
            .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

        if (cert == null)
        {
            return NotFound(new { message = "Certificate not found" });
        }

        // Get module info
        var module = await _db.Modules.FirstOrDefaultAsync(m => m.ModuleId == cert.ModuleId);

        return Ok(new CertificateDetailResponse
        {
            CertificateId = cert.CertificateId,
            LicenseId = cert.LicenseId,
            LicenseeCode = cert.LicenseeCode,
            ModuleId = cert.ModuleId,
            ModuleName = module?.DisplayName ?? cert.ModuleId,
            ModuleVersion = cert.ModuleVersion,
            IssuedAt = cert.IssuedAt,
            SyncedAt = cert.SyncedAt,
            ProjectName = cert.ProjectName,
            ProjectLocation = cert.ProjectLocation,
            Vessel = cert.Vessel,
            Client = cert.Client,
            SignatoryName = cert.SignatoryName,
            SignatoryTitle = cert.SignatoryTitle,
            CompanyName = cert.CompanyName,
            ProcessingData = string.IsNullOrEmpty(cert.ProcessingDataJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(cert.ProcessingDataJson) ?? new(),
            InputFiles = string.IsNullOrEmpty(cert.InputFilesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(cert.InputFilesJson) ?? new(),
            OutputFiles = string.IsNullOrEmpty(cert.OutputFilesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(cert.OutputFilesJson) ?? new(),
            IsSignatureVerified = cert.IsSignatureVerified
        });
    }

    /// <summary>
    /// Batch verify multiple certificates.
    /// POST /api/certificates/verify/batch
    /// Verifies multiple certificate IDs in a single request.
    /// </summary>
    [HttpPost("verify/batch")]
    public async Task<ActionResult<BatchVerificationResponse>> VerifyBatch([FromBody] BatchVerificationRequest request)
    {
        if (request == null || !request.CertificateIds.Any())
        {
            return BadRequest(new { message = "No certificate IDs provided" });
        }

        _logger.LogInformation("Batch verification request for {Count} certificates", request.CertificateIds.Count);

        var results = new List<CertificateVerificationResult>();

        foreach (var certId in request.CertificateIds.Distinct().Take(100)) // Limit to 100
        {
            var cert = await _db.Certificates
                .FirstOrDefaultAsync(c => c.CertificateId == certId);

            if (cert == null)
            {
                results.Add(new CertificateVerificationResult
                {
                    IsValid = false,
                    CertificateId = certId,
                    Message = "Certificate not found"
                });
                continue;
            }

            var module = await _db.Modules.FirstOrDefaultAsync(m => m.ModuleId == cert.ModuleId);

            results.Add(new CertificateVerificationResult
            {
                IsValid = true,
                CertificateId = cert.CertificateId,
                IssuedAt = cert.IssuedAt,
                CompanyName = cert.CompanyName,
                ModuleId = cert.ModuleId,
                ModuleName = module?.DisplayName ?? cert.ModuleId,
                ProjectName = cert.ProjectName,
                IsSignatureVerified = cert.IsSignatureVerified,
                Message = cert.IsSignatureVerified
                    ? "Certificate is authentic and verified"
                    : "Certificate is in records (signature not verified)"
            });
        }

        return Ok(new BatchVerificationResponse
        {
            TotalRequested = request.CertificateIds.Count,
            Verified = results.Count(r => r.IsValid),
            NotFound = results.Count(r => !r.IsValid),
            Results = results
        });
    }

    /// <summary>
    /// Get certificate statistics for a licensee.
    /// GET /api/certificates/stats/{licenseeCode}
    /// Returns aggregate statistics about certificates issued.
    /// </summary>
    [HttpGet("stats/{licenseeCode}")]
    public async Task<ActionResult<CertificateStatsResponse>> GetCertificateStats(string licenseeCode)
    {
        if (string.IsNullOrEmpty(licenseeCode) || licenseeCode.Length != 2)
        {
            return BadRequest(new { message = "Invalid licensee code. Must be 2 uppercase letters." });
        }

        var certificates = await _db.Certificates
            .Where(c => c.LicenseeCode == licenseeCode.ToUpperInvariant())
            .ToListAsync();

        if (!certificates.Any())
        {
            return Ok(new CertificateStatsResponse
            {
                LicenseeCode = licenseeCode.ToUpperInvariant(),
                TotalCertificates = 0,
                Message = "No certificates found for this licensee"
            });
        }

        // Group by module
        var byModule = certificates
            .GroupBy(c => c.ModuleId)
            .Select(g => new ModuleCertificateStats
            {
                ModuleId = g.Key,
                Count = g.Count(),
                FirstIssued = g.Min(c => c.IssuedAt),
                LastIssued = g.Max(c => c.IssuedAt)
            })
            .OrderByDescending(m => m.Count)
            .ToList();

        // Group by month
        var byMonth = certificates
            .GroupBy(c => new { c.IssuedAt.Year, c.IssuedAt.Month })
            .Select(g => new MonthlyCertificateStats
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .Take(12)
            .ToList();

        return Ok(new CertificateStatsResponse
        {
            LicenseeCode = licenseeCode.ToUpperInvariant(),
            TotalCertificates = certificates.Count,
            VerifiedCount = certificates.Count(c => c.IsSignatureVerified),
            FirstCertificateDate = certificates.Min(c => c.IssuedAt),
            LastCertificateDate = certificates.Max(c => c.IssuedAt),
            ByModule = byModule,
            ByMonth = byMonth
        });
    }

    /// <summary>
    /// Search certificates with filtering.
    /// GET /api/certificates/search
    /// Supports filtering by date range, module, project, etc.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<CertificateSearchResponse>> SearchCertificates(
        [FromQuery] string? licenseeCode,
        [FromQuery] string? licenseId,
        [FromQuery] string? moduleId,
        [FromQuery] string? projectName,
        [FromQuery] string? companyName,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _db.Certificates.AsQueryable();

        if (!string.IsNullOrEmpty(licenseeCode))
            query = query.Where(c => c.LicenseeCode == licenseeCode.ToUpperInvariant());

        if (!string.IsNullOrEmpty(licenseId))
            query = query.Where(c => c.LicenseId == licenseId);

        if (!string.IsNullOrEmpty(moduleId))
            query = query.Where(c => c.ModuleId == moduleId);

        if (!string.IsNullOrEmpty(projectName))
            query = query.Where(c => c.ProjectName.Contains(projectName));

        if (!string.IsNullOrEmpty(companyName))
            query = query.Where(c => c.CompanyName.Contains(companyName));

        if (fromDate.HasValue)
            query = query.Where(c => c.IssuedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(c => c.IssuedAt <= toDate.Value);

        var totalCount = await query.CountAsync();

        var certificates = await query
            .OrderByDescending(c => c.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CertificateListItem
            {
                CertificateId = c.CertificateId,
                ModuleId = c.ModuleId,
                ProjectName = c.ProjectName,
                IssuedAt = c.IssuedAt,
                CompanyName = c.CompanyName,
                SignatoryName = c.SignatoryName
            })
            .ToListAsync();

        return Ok(new CertificateSearchResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Certificates = certificates
        });
    }

    /// <summary>
    /// Re-verify certificate signature.
    /// POST /api/certificates/reverify/{certificateId}
    /// Re-validates the signature of a certificate.
    /// </summary>
    [HttpPost("reverify/{certificateId}")]
    public async Task<ActionResult> ReverifyCertificate(string certificateId)
    {
        var cert = await _db.Certificates
            .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

        if (cert == null)
        {
            return NotFound(new { message = "Certificate not found" });
        }

        if (string.IsNullOrEmpty(cert.Signature))
        {
            return Ok(new
            {
                certificateId = cert.CertificateId,
                previousStatus = cert.IsSignatureVerified,
                newStatus = false,
                message = "Certificate has no signature to verify"
            });
        }

        // Rebuild certificate for verification
        var mockCert = new ProcessingCertificate
        {
            CertificateId = cert.CertificateId,
            LicenseeCode = cert.LicenseeCode,
            ModuleId = cert.ModuleId,
            ModuleCertificateCode = cert.ModuleCertificateCode,
            ModuleVersion = cert.ModuleVersion ?? string.Empty,
            IssuedAt = cert.IssuedAt,
            ProjectName = cert.ProjectName,
            SignatoryName = cert.SignatoryName,
            CompanyName = cert.CompanyName,
            Signature = cert.Signature,
            ProcessingData = string.IsNullOrEmpty(cert.ProcessingDataJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(cert.ProcessingDataJson) ?? new()
        };

        var previousStatus = cert.IsSignatureVerified;
        var newStatus = VerifyCertificateSignature(mockCert);

        cert.IsSignatureVerified = newStatus;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Re-verified certificate {CertificateId}: {PreviousStatus} -> {NewStatus}",
            certificateId, previousStatus, newStatus);

        return Ok(new
        {
            certificateId = cert.CertificateId,
            previousStatus,
            newStatus,
            message = newStatus
                ? "Signature verified successfully"
                : "Signature verification failed"
        });
    }

    /// <summary>
    /// Get recent certificates for a license.
    /// GET /api/certificates/recent/{licenseId}
    /// Returns the most recent certificates for a specific license.
    /// </summary>
    [HttpGet("recent/{licenseId}")]
    public async Task<ActionResult<List<CertificateListItem>>> GetRecentCertificates(
        string licenseId,
        [FromQuery] int count = 10)
    {
        if (count > 100) count = 100;

        var certificates = await _db.Certificates
            .Where(c => c.LicenseId == licenseId)
            .OrderByDescending(c => c.IssuedAt)
            .Take(count)
            .Select(c => new CertificateListItem
            {
                CertificateId = c.CertificateId,
                ModuleId = c.ModuleId,
                ProjectName = c.ProjectName,
                IssuedAt = c.IssuedAt,
                CompanyName = c.CompanyName,
                SignatoryName = c.SignatoryName
            })
            .ToListAsync();

        return Ok(certificates);
    }
}

// ============================================================================
// Additional DTOs for Certificate Controller
// ============================================================================

public class BatchVerificationRequest
{
    public List<string> CertificateIds { get; set; } = new();
}

public class BatchVerificationResponse
{
    public int TotalRequested { get; set; }
    public int Verified { get; set; }
    public int NotFound { get; set; }
    public List<CertificateVerificationResult> Results { get; set; } = new();
}

public class CertificateStatsResponse
{
    public string LicenseeCode { get; set; } = string.Empty;
    public int TotalCertificates { get; set; }
    public int VerifiedCount { get; set; }
    public DateTime? FirstCertificateDate { get; set; }
    public DateTime? LastCertificateDate { get; set; }
    public string? Message { get; set; }
    public List<ModuleCertificateStats> ByModule { get; set; } = new();
    public List<MonthlyCertificateStats> ByMonth { get; set; } = new();
}

public class ModuleCertificateStats
{
    public string ModuleId { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime FirstIssued { get; set; }
    public DateTime LastIssued { get; set; }
}

public class MonthlyCertificateStats
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}

public class CertificateSearchResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<CertificateListItem> Certificates { get; set; } = new();
}

// ============================================================================
// DTOs for Certificate Controller
// ============================================================================

public class CertificateListItem
{
    public string CertificateId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string SignatoryName { get; set; } = string.Empty;
}

public class CertificateSequenceRequest
{
    public string LicenseeCode { get; set; } = string.Empty;
}

public class CertificateSequenceResponse
{
    public string CertificateId { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string YearMonth { get; set; } = string.Empty;
}

public class CertificateDetailResponse
{
    public string CertificateId { get; set; } = string.Empty;
    public string LicenseId { get; set; } = string.Empty;
    public string LicenseeCode { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string? ModuleVersion { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime SyncedAt { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectLocation { get; set; }
    public string? Vessel { get; set; }
    public string? Client { get; set; }
    public string SignatoryName { get; set; } = string.Empty;
    public string? SignatoryTitle { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Dictionary<string, string> ProcessingData { get; set; } = new();
    public List<string> InputFiles { get; set; } = new();
    public List<string> OutputFiles { get; set; } = new();
    public bool IsSignatureVerified { get; set; }
}
