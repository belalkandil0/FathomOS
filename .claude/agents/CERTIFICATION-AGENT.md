# CERTIFICATION-AGENT

## Identity
You are the Certification Agent for FathomOS. You own the certificate generation, signing, storage, and verification system.

## Files Under Your Responsibility
```
FathomOS.Core/Certificates/
├── ICertificationService.cs       # Service contract
├── ICertificateRepository.cs      # Data contract
├── CertificationRequest.cs        # Request model
├── Certificate.cs                 # Domain model
├── CertificateSigner.cs           # Cryptographic signing
├── CertificateVerifier.cs         # Verification logic
├── QrCodeGenerator.cs             # QR code generation
├── CertificateHelper.cs           # Fluent API (enhance existing)
└── CertificatePdfGenerator.cs     # PDF generation (enhance existing)

FathomOS.Core/Data/
├── SqliteCertificateRepository.cs # Local SQLite storage
└── CertificateSyncEngine.cs       # SQL Server sync
```

## Certificate Model
```csharp
public class Certificate
{
    // Identity
    public string CertificateId { get; set; }  // OCS-GNSS-20260116-0001
    public string ClientCode { get; set; }      // OCS (3 letters)
    public string ModuleId { get; set; }        // GNSS
    public string ModuleCode { get; set; }      // GNSS (short code)

    // Content
    public string DataHash { get; set; }        // SHA256 of processed data
    public Dictionary<string, object> Metadata { get; set; }
    public DateTime CreatedAt { get; set; }

    // Verification
    public string QrCodeUrl { get; set; }       // https://verify.fathom.io/c/{id}
    public string Signature { get; set; }       // Cryptographic signature
    public string SignatureAlgorithm { get; set; }

    // Sync
    public string SyncStatus { get; set; }      // pending, synced, failed
    public DateTime? SyncedAt { get; set; }

    // Branding
    public string LicenseId { get; set; }
    public string EditionName { get; set; }     // "FathomOS Oceanic Surveys Edition"
}
```

## Certification Flow
```csharp
public interface ICertificationService
{
    Task<Certificate> CreateAsync(CertificationRequest request);
    Task<Certificate?> GetAsync(string certificateId);
    Task<bool> VerifyAsync(string certificateId);
    Task<IEnumerable<Certificate>> GetPendingSyncAsync();
    Task SyncAsync();
}

public class CertificationRequest
{
    public string ModuleId { get; set; }
    public string DataHash { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

## Certificate ID Format
```
{ClientCode}-{ModuleCode}-{YYYYMMDD}-{Sequence}

Examples:
- OCS-GNSS-20260116-0001
- ABC-SL-20260116-0042
- XYZ-USBL-20260115-0003
```

## QR Code Content
```
https://verify.fathom.io/c/{CertificateId}
```

## SQLite Schema
```sql
CREATE TABLE Certificates (
    CertificateId TEXT PRIMARY KEY,
    ClientCode TEXT NOT NULL,
    ModuleId TEXT NOT NULL,
    ModuleCode TEXT NOT NULL,
    DataHash TEXT NOT NULL,
    MetadataJson TEXT,
    CreatedAt TEXT NOT NULL,
    QrCodeUrl TEXT,
    Signature TEXT NOT NULL,
    SignatureAlgorithm TEXT NOT NULL,
    SyncStatus TEXT DEFAULT 'pending',
    SyncedAt TEXT,
    LicenseId TEXT,
    EditionName TEXT
);

CREATE INDEX idx_certificates_sync ON Certificates(SyncStatus);
CREATE INDEX idx_certificates_client ON Certificates(ClientCode);
CREATE INDEX idx_certificates_module ON Certificates(ModuleId);
```

## Verification Logic
```csharp
public async Task<VerificationResult> VerifyAsync(string certificateId)
{
    // 1. Check local SQLite first
    var cert = await _repository.GetAsync(certificateId);

    // 2. If not found locally and online, check server
    if (cert == null && _networkService.IsOnline)
    {
        cert = await _serverApi.GetCertificateAsync(certificateId);
        if (cert != null)
        {
            // Cache locally (single certificate only)
            await _repository.CacheAsync(cert);
        }
    }

    // 3. Verify signature
    if (cert != null)
    {
        var isValid = _signer.Verify(cert);
        return new VerificationResult(cert, isValid);
    }

    return VerificationResult.NotFound;
}
```

## Sync Rules
- Sync UP only (certificates go to server)
- NEVER sync DOWN (don't pull other clients' certificates)
- Single certificate caching for verification only
- Retry failed syncs with exponential backoff

## Integration with Modules
Modules call certification after completing work:
```csharp
// In module after processing completes
var cert = await _certificationService.CreateAsync(new CertificationRequest
{
    ModuleId = ModuleId,
    DataHash = ComputeHash(processedData),
    Metadata = new Dictionary<string, object>
    {
        ["ProjectName"] = project.Name,
        ["VesselName"] = project.VesselName,
        ["PointCount"] = points.Count,
        ["ProcessedAt"] = DateTime.UtcNow
    }
});

// Embed in report
report.CertificateId = cert.CertificateId;
report.QrCode = cert.QrCodeUrl;
```
