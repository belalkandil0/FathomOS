# FathomOS Certificate System

**Version**: 1.0.45
**Last Updated**: January 2026

## Table of Contents

- [Overview](#overview)
- [Certificate ID Format](#certificate-id-format)
- [Certificate Components](#certificate-components)
- [Export Formats](#export-formats)
- [Module Integration](#module-integration)
- [Online Verification](#online-verification)
- [QR Code Functionality](#qr-code-functionality)
- [API Reference](#api-reference)
- [Security](#security)
- [Troubleshooting](#troubleshooting)

---

## Overview

The FathomOS Certificate System provides tamper-evident, verifiable processing certificates for marine survey operations. Each certificate documents the processing performed by a FathomOS module, including input data, processing parameters, output files, and signatory information.

### Key Features

- **Unique Identification**: Every certificate has a globally unique ID with built-in validation
- **Cryptographic Signing**: RSA-SHA256 digital signatures ensure authenticity
- **Offline Generation**: Certificates can be created without internet connectivity
- **Online Verification**: QR codes and URLs enable instant verification
- **Multi-Format Export**: Professional HTML and PDF export options
- **White-Label Support**: Certificates display licensee branding

### Certificate Workflow

```
Module Processing Complete
         |
         v
+-------------------+
|  Create Certificate|  <-- SignatoryDialog (name, title, company)
+-------------------+
         |
         v
+-------------------+
|  Sign Certificate |  <-- RSA-SHA256 signature
+-------------------+
         |
         v
+-------------------+
|  Store Locally    |  <-- SQLite database
+-------------------+
         |
         v
+-------------------+
|  Sync to Server   |  <-- Background sync when online
+-------------------+
         |
         v
+-------------------+
|  Online Verify    |  <-- Public verification page
+-------------------+
```

---

## Certificate ID Format

Each certificate has a unique identifier following the format:

```
FOS-{LicenseeCode}-{YYMM}-{Sequence}-{CheckDigit}
```

### Components

| Component | Description | Example |
|-----------|-------------|---------|
| `FOS` | Fixed prefix (Fathom OS) | `FOS` |
| `LicenseeCode` | 2-3 letter licensee code | `OCS`, `S7` |
| `YYMM` | Year and month of issue | `2601` (Jan 2026) |
| `Sequence` | 4-digit sequential number | `0001` |
| `CheckDigit` | Validation character (0-9, A-Z) | `X` |

### Examples

```
FOS-OCS-2601-0001-X    # Ocean Calibration Services, Jan 2026, Certificate #1
FOS-S7-2601-0042-3     # S7 Fathom, Jan 2026, Certificate #42
FOS-ABC-2512-0001-K    # ABC Company, Dec 2025, Certificate #1
```

### Check Digit Algorithm

The check digit uses a modulo-36 weighted checksum:

```csharp
private static string CalculateCheckDigit(string licenseeCode, string yearMonth, string sequence)
{
    var input = $"{licenseeCode}{yearMonth}{sequence}";
    var sum = 0;
    for (var i = 0; i < input.Length; i++)
    {
        var c = input[i];
        var value = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
        sum += value * (i + 1);
    }

    var checkValue = sum % 36;
    return checkValue < 10 ? checkValue.ToString() : ((char)('A' + checkValue - 10)).ToString();
}
```

### Validation

Certificate IDs must match the regex pattern:

```
^FOS-[A-Z]{2,3}-\d{4}-\d{4}-[A-Z0-9]$
```

---

## Certificate Components

### Certificate Model

```csharp
public class Certificate
{
    // Primary Identity
    public string CertificateId { get; set; }        // FOS-OCS-2601-0001-X

    // License Information
    public string LicenseId { get; set; }            // License that issued this
    public string LicenseeCode { get; set; }         // OCS, S7, etc.

    // Module Information
    public string ModuleId { get; set; }             // SurveyListing, UsblVerification
    public string ModuleCertificateCode { get; set; }// SLG, USV
    public string ModuleVersion { get; set; }        // 1.0.45

    // Certificate Metadata
    public DateTime IssuedAt { get; set; }           // UTC timestamp
    public string ProjectName { get; set; }          // Project identifier
    public string? ProjectLocation { get; set; }     // Optional location
    public string? Vessel { get; set; }              // Optional vessel name
    public string? Client { get; set; }              // Optional client name

    // Signatory Information
    public string SignatoryName { get; set; }        // John Smith
    public string? SignatoryTitle { get; set; }      // Senior Surveyor
    public string CompanyName { get; set; }          // Ocean Calibration Services

    // Processing Data
    public string? ProcessingDataJson { get; set; }  // Module-specific key-value pairs
    public string? InputFilesJson { get; set; }      // Array of input file names
    public string? OutputFilesJson { get; set; }     // Array of output file names

    // Cryptographic Signature
    public string Signature { get; set; }            // Base64-encoded RSA signature
    public string SignatureAlgorithm { get; set; }   // RSA-SHA256
    public string? DataHash { get; set; }            // SHA256 hash of signed data

    // Sync Status
    public string SyncStatus { get; set; }           // pending, synced, failed
    public DateTime? SyncedAt { get; set; }          // When synced to server

    // Verification
    public string? QrCodeUrl { get; set; }           // Verification URL
    public string? EditionName { get; set; }         // License edition
}
```

### Processing Data

Each module can include custom key-value pairs in `ProcessingDataJson`:

```json
{
    "Input Files": "12",
    "Output Files": "3",
    "Total Points": "45,678",
    "Route Length": "12.5 km",
    "Processing Date": "2026-01-15",
    "Quality Check": "Passed"
}
```

### Standard Data Keys

The `ModuleCertificateHelper.DataKeys` class provides standardized keys:

```csharp
public static class DataKeys
{
    // Project information
    public const string ProjectName = "Project Name";
    public const string ClientName = "Client Name";
    public const string VesselName = "Vessel Name";

    // Processing information
    public const string ProcessingDate = "Processing Date";
    public const string SoftwareVersion = "Software Version";

    // Quality metrics
    public const string QualityCheck = "Quality Check";
    public const string ValidationStatus = "Validation Status";

    // Survey-specific
    public const string StartKp = "Start KP";
    public const string EndKp = "End KP";
    public const string RouteLength = "Route Length";
}
```

---

## Export Formats

### HTML Export

The `CertificatePdfGenerator` class generates professional HTML certificates:

```csharp
var generator = new CertificatePdfGenerator();
var html = generator.GenerateHtml(certificate, brandLogo);
await generator.SaveToFileAsync(certificate, "certificate.html", brandLogo);
```

**HTML Features:**
- A4 page format (210mm x 297mm)
- Professional styling with company branding
- QR code for verification
- Print-optimized CSS
- Watermark background

### PDF Export

PDF generation is achieved by printing the HTML to PDF:

1. Generate HTML certificate
2. Open in browser or WebBrowser control
3. Print to PDF using system print dialog

```csharp
// Generate HTML
var html = generator.GenerateHtml(certificate, brandLogo);

// Save to temp file
var tempPath = Path.Combine(Path.GetTempPath(), $"{certificate.CertificateId}.html");
await File.WriteAllTextAsync(tempPath, html);

// Open in default browser for printing
Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
```

### Certificate Appearance

```
+----------------------------------------------------------+
|  [LOGO]    CERTIFICATE OF PROCESSING                     |
|            No: FOS-OCS-2601-0001-X                       |
+----------------------------------------------------------+
|                                                          |
|  This is to certify that the data processing for:        |
|                                                          |
|  +--------------------------------------------+          |
|  |  North Sea Pipeline Survey - Route A       |          |
|  |  North Sea, Block 22/5                     |          |
|  +--------------------------------------------+          |
|                                                          |
|  has been performed using validated algorithms and       |
|  quality-controlled procedures.                          |
|                                                          |
+----------------------------------------------------------+
|  PROCESSING DETAILS                                      |
|  Module: Survey Listing Generator v1.0.45                |
|  Processing Date: 15 January 2026, 14:30 UTC             |
|  Vessel: MV Survey Excellence                            |
+----------------------------------------------------------+
|  DATA SUMMARY                                            |
|  Input Files: 12                                         |
|  Output Files: 3                                         |
|  Total Points: 45,678                                    |
|  Route Length: 12.5 km                                   |
+----------------------------------------------------------+
|                                                          |
|  AUTHORIZED BY                      [QR CODE]            |
|  _________________________          Scan to verify       |
|  John Smith                                              |
|  Senior Surveyor                                         |
|  Ocean Calibration Services                              |
|  Date: 15 January 2026                                   |
|                                                          |
+----------------------------------------------------------+
|  DIGITAL VERIFICATION                                    |
|  Certificate ID: FOS-OCS-2601-0001-X                     |
|  Verify online: https://verify.fathom.io/c/...           |
+----------------------------------------------------------+
```

---

## Module Integration

### Quick Start

Modules can generate certificates using the `CertificateHelper` class:

```csharp
using FathomOS.Core.Certificates;

// Option 1: Fluent API (recommended)
var certificate = await CertificateHelper
    .QuickCreate(licenseManager)
    .ForModule("SurveyListing", "SLG", "1.0.45")
    .WithProject("North Sea Pipeline", "North Sea, Block 22/5")
    .WithVessel("MV Survey Excellence")
    .WithClient("Ocean Energy Ltd")
    .AddData("Total Points", "45,678")
    .AddData("Route Length", "12.5 km")
    .AddInputFiles(inputFilePaths)
    .AddOutputFiles(outputFilePaths)
    .CreateWithDialogAsync(ownerWindow);

// Option 2: Direct method call
var certificate = await CertificateHelper.CreateWithDialogAsync(
    licenseManager,
    moduleId: "SurveyListing",
    moduleCertificateCode: "SLG",
    moduleVersion: "1.0.45",
    projectName: "North Sea Pipeline",
    processingData: new Dictionary<string, string>
    {
        { "Total Points", "45,678" },
        { "Route Length", "12.5 km" }
    },
    inputFiles: inputFileNames,
    outputFiles: outputFileNames,
    projectLocation: "North Sea, Block 22/5",
    vessel: "MV Survey Excellence",
    client: "Ocean Energy Ltd",
    owner: ownerWindow
);
```

### Using ICertificationService (DI)

For dependency injection scenarios:

```csharp
public class MyModule
{
    private readonly ICertificationService _certService;

    public MyModule(ICertificationService certService)
    {
        _certService = certService;
    }

    public async Task ProcessDataAsync()
    {
        // ... perform processing ...

        // Create certificate request
        var request = ModuleCertificateHelper.CreateRequest(
            moduleId: "MyModule",
            moduleCertificateCode: "MYM",
            moduleVersion: "1.0.0",
            projectName: "Test Project",
            processingData: new Dictionary<string, string>
            {
                { "Records Processed", "1,234" }
            }
        );

        // Generate certificate with dialog
        var certificateId = await ModuleCertificateHelper.GenerateCertificateAsync(
            _certService, request, ownerWindow);

        if (certificateId != null)
        {
            // Certificate created successfully
            Console.WriteLine($"Certificate: {certificateId}");
        }
    }
}
```

### Silent Certificate Creation

For automated/batch processing:

```csharp
// Create certificate without UI
var certificate = await CertificateHelper.CreateSilentAsync(
    licenseManager,
    moduleId: "BatchProcessor",
    moduleCertificateCode: "BAT",
    moduleVersion: "1.0.0",
    projectName: "Batch Job #123",
    signatoryName: "System Administrator",
    companyName: "Auto Processing Inc",
    processingData: processingData,
    signatoryTitle: "Automated System"
);
```

### Module Certificate Codes

Each module should use a unique 3-letter code:

| Module | Code | Description |
|--------|------|-------------|
| Survey Listing Generator | `SLG` | Survey data processing |
| USBL Verification | `USV` | Acoustic positioning verification |
| Sound Velocity Profile | `SVP` | CTD/sound velocity processing |
| GNSS Calibration | `GNS` | GNSS receiver calibration |
| MRU Calibration | `MRU` | Motion reference unit calibration |
| Equipment Inventory | `EQI` | Equipment tracking |
| Tree Inclination | `TRI` | Christmas tree analysis |

---

## Online Verification

### Verification URL

Certificates can be verified online at:

```
https://s7fathom-license-server.onrender.com/verify.html?id={CertificateId}
```

Example:
```
https://s7fathom-license-server.onrender.com/verify.html?id=FOS-OCS-2601-0001-X
```

### Verification Process

1. User scans QR code or visits verification URL
2. `verify.html` page loads and extracts certificate ID
3. JavaScript calls `/api/certificates/verify/{id}`
4. Server returns verification result
5. Page displays certificate status and details

### Verification Response

```json
{
    "isValid": true,
    "certificateId": "FOS-OCS-2601-0001-X",
    "issuedAt": "2026-01-15T14:30:00Z",
    "companyName": "Ocean Calibration Services",
    "moduleId": "SurveyListing",
    "moduleName": "Survey Listing Generator",
    "projectName": "North Sea Pipeline",
    "isSignatureVerified": true,
    "message": "This certificate is authentic and cryptographically verified."
}
```

### Full Certificate View

For complete certificate details, users can view:

```
https://s7fathom-license-server.onrender.com/processing-certificate.html?id={CertificateId}
```

This page displays:
- All certificate metadata
- Processing data table
- Input/output file lists
- Signatory information
- Verification status badge
- Print functionality

---

## QR Code Functionality

### QR Code Generation

QR codes are generated dynamically using the QR Server API:

```csharp
var verificationUrl = $"https://s7fathom-license-server.onrender.com/verify.html?id={certificateId}";
var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=120x120&data={Uri.EscapeDataString(verificationUrl)}";
```

### QR Code Usage

- Embedded in HTML/PDF certificates
- Displayed in certificate viewer window
- Scannable by any QR code reader
- Links directly to verification page

### QR Code Sizes

| Context | Size | URL Parameter |
|---------|------|---------------|
| PDF Certificate | 120x120 | `size=120x120` |
| Verification Page | 200x200 | `size=200x200` |
| Full Certificate | 150x150 | `size=150x150` |

---

## API Reference

### Server Endpoints

Base URL: `https://s7fathom-license-server.onrender.com`

#### Public Endpoints (No Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/certificates/verify/{certificateId}` | Verify a certificate |
| POST | `/api/certificates/verify/batch` | Verify multiple certificates |

#### Protected Endpoints (Require X-API-Key)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/certificates/sync` | Sync certificates from client |
| GET | `/api/certificates/list` | List certificates with pagination |
| GET | `/api/certificates/{certificateId}` | Get full certificate details |
| POST | `/api/certificates/sequence` | Get next sequence number |
| GET | `/api/certificates/search` | Search certificates with filters |
| GET | `/api/certificates/stats/{licenseeCode}` | Get certificate statistics |
| GET | `/api/certificates/recent/{licenseId}` | Get recent certificates |
| POST | `/api/certificates/reverify/{certificateId}` | Re-verify signature |

### Verify Certificate

```http
GET /api/certificates/verify/FOS-OCS-2601-0001-X

Response 200:
{
    "isValid": true,
    "certificateId": "FOS-OCS-2601-0001-X",
    "issuedAt": "2026-01-15T14:30:00Z",
    "companyName": "Ocean Calibration Services",
    "moduleId": "SurveyListing",
    "moduleName": "Survey Listing Generator",
    "projectName": "North Sea Pipeline",
    "isSignatureVerified": true,
    "message": "Certificate is authentic and verified"
}
```

### Sync Certificates

```http
POST /api/certificates/sync
Content-Type: application/json
X-API-Key: {api-key}

{
    "licenseId": "LIC-12345",
    "certificates": [
        {
            "certificateId": "FOS-OCS-2601-0001-X",
            "licenseeCode": "OCS",
            "moduleId": "SurveyListing",
            // ... full certificate data
        }
    ]
}

Response 200:
{
    "success": true,
    "message": "Successfully synced 1 certificates",
    "syncedCount": 1,
    "failedIds": []
}
```

### Search Certificates

```http
GET /api/certificates/search?licenseeCode=OCS&moduleId=SurveyListing&fromDate=2026-01-01&page=1&pageSize=50

Response 200:
{
    "totalCount": 42,
    "page": 1,
    "pageSize": 50,
    "totalPages": 1,
    "certificates": [
        {
            "certificateId": "FOS-OCS-2601-0001-X",
            "moduleId": "SurveyListing",
            "projectName": "North Sea Pipeline",
            "issuedAt": "2026-01-15T14:30:00Z",
            "companyName": "Ocean Calibration Services",
            "signatoryName": "John Smith"
        }
    ]
}
```

### Get Statistics

```http
GET /api/certificates/stats/OCS

Response 200:
{
    "licenseeCode": "OCS",
    "totalCertificates": 156,
    "verifiedCount": 148,
    "firstCertificateDate": "2025-06-01T00:00:00Z",
    "lastCertificateDate": "2026-01-15T14:30:00Z",
    "byModule": [
        { "moduleId": "SurveyListing", "count": 89 },
        { "moduleId": "UsblVerification", "count": 45 }
    ],
    "byMonth": [
        { "year": 2026, "month": 1, "count": 12 },
        { "year": 2025, "month": 12, "count": 18 }
    ]
}
```

---

## Security

### Cryptographic Signing

Certificates are signed using RSA-SHA256:

1. **Key Generation**: RSA-2048 key pairs generated on first use
2. **Key Storage**: Private keys protected with Windows DPAPI
3. **Canonical Data**: Deterministic JSON representation for signing
4. **Signature**: RSA-SHA256 signature stored as Base64

```csharp
// Signing process
var canonicalData = CreateCanonicalCertificateData(certificate);
var dataBytes = Encoding.UTF8.GetBytes(canonicalData);

using var sha256 = SHA256.Create();
var hash = sha256.ComputeHash(dataBytes);

var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
certificate.Signature = Convert.ToBase64String(signature);
```

### Signature Verification

Server-side verification:

```csharp
// Verification process
var canonicalData = BuildSignatureData(cert);
var dataBytes = Encoding.UTF8.GetBytes(canonicalData);

using var ecdsa = ECDsa.Create();
ecdsa.ImportFromPem(publicKeyPem);

var signatureBytes = Convert.FromBase64String(cert.Signature);
var isValid = ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);
```

### Key Storage Locations

```
%LocalAppData%\FathomOS\Certificates\Keys\
    certificate_signing_key.pem     # Private key (PEM format)
    certificate_signing_key.pub     # Public key (PEM format)
    certificate_key.dpapi           # DPAPI-protected private key
```

### Data Hash

Each certificate includes a SHA256 hash of the processing data:

```csharp
public static string ComputeHashFromData(Dictionary<string, string> processingData)
{
    var sb = new StringBuilder();
    foreach (var kvp in processingData.OrderBy(k => k.Key))
    {
        sb.Append(kvp.Key);
        sb.Append(':');
        sb.Append(kvp.Value);
        sb.Append('|');
    }

    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
    var hash = sha256.ComputeHash(bytes);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}
```

---

## Troubleshooting

### Certificate Not Found Online

**Symptoms**: Verification page shows "Certificate Not Found"

**Causes**:
1. Certificate hasn't been synced to server
2. Incorrect certificate ID
3. Server connectivity issues

**Solutions**:
1. Check sync status in Certificate Manager
2. Verify certificate ID matches exactly
3. Wait for background sync or manually trigger sync

### Signature Verification Failed

**Symptoms**: Certificate shows "Signature Not Verified"

**Causes**:
1. Certificate was modified after signing
2. Public key mismatch between client and server
3. Key rotation occurred

**Solutions**:
1. Re-issue certificate from original source
2. Verify public keys match
3. Contact support for key issues

### Sync Failures

**Symptoms**: Certificates stuck in "pending" sync status

**Causes**:
1. No internet connectivity
2. Server unavailable
3. API key issues
4. Certificate data validation failures

**Solutions**:
1. Check network connectivity
2. Verify server status
3. Check license validity
4. Review certificate data for issues

### QR Code Not Scanning

**Symptoms**: QR code reader fails to detect code

**Causes**:
1. QR code too small
2. Poor print quality
3. Screen glare

**Solutions**:
1. Print at higher resolution
2. Increase QR code size
3. Adjust screen brightness

### Certificate Manager Not Opening

**Symptoms**: "Certificates" button doesn't respond

**Causes**:
1. License not activated
2. Module dependencies not loaded
3. UI initialization error

**Solutions**:
1. Activate a valid license
2. Restart application
3. Check logs for errors

---

## Related Documentation

- [Licensing Overview](../Documentation/Licensing/README.md)
- [Developer Guide](../Documentation/DEVELOPER_GUIDE.md)
- [API Reference](../Documentation/API_REFERENCE.md)

---

**Copyright 2026 Fathom OS. All rights reserved.**
