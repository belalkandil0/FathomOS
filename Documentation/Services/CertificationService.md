# Certification Service Documentation

**Version:** 1.0.0
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Certificate Structure](#certificate-structure)
4. [Creating Certificates](#creating-certificates)
5. [Certificate Storage](#certificate-storage)
6. [Cloud Synchronization](#cloud-synchronization)
7. [API Reference](#api-reference)

---

## Overview

The Certification Service provides processing certificate generation, storage, and management for FathomOS. Certificates provide an auditable record of data processing operations, documenting inputs, outputs, parameters, and signatories.

### Key Features

- Unique certificate ID generation
- Digital signature and hashing
- Local SQLite storage
- Cloud synchronization
- PDF certificate export
- Signatory dialog
- Certificate viewer

---

## Architecture

### Service Components

```
FathomOS.Shell
└── Services
    └── CertificationService.cs

FathomOS.Core
├── Interfaces
│   └── ICertificationService.cs
└── Certificates
    ├── CertificateHelper.cs
    ├── ProcessingCertificate.cs
    └── SignatoryInfo.cs
```

### Database Schema

Certificates are stored in a local SQLite database:

```sql
CREATE TABLE Certificates (
    Id TEXT PRIMARY KEY,
    ModuleId TEXT NOT NULL,
    CertificateCode TEXT NOT NULL,
    ProjectName TEXT,
    CreatedAt TEXT NOT NULL,
    SignatoryName TEXT,
    SignatoryTitle TEXT,
    CompanyName TEXT,
    DataJson TEXT,
    InputFilesJson TEXT,
    OutputFilesJson TEXT,
    Hash TEXT,
    SyncedToCloud INTEGER DEFAULT 0
);
```

---

## Certificate Structure

### ProcessingCertificate Class

```csharp
public class ProcessingCertificate
{
    // Identity
    public string CertificateId { get; set; }        // e.g., "SL-2026-000042"
    public string ModuleId { get; set; }              // e.g., "SurveyListing"
    public string ModuleCertificateCode { get; set; } // e.g., "SL"
    public string ModuleVersion { get; set; }

    // Project Info
    public string ProjectName { get; set; }
    public string ProjectLocation { get; set; }
    public string Vessel { get; set; }
    public string Client { get; set; }

    // Processing Info
    public Dictionary<string, string> ProcessingData { get; set; }
    public List<string> InputFiles { get; set; }
    public List<string> OutputFiles { get; set; }

    // Signatory
    public string SignatoryName { get; set; }
    public string SignatoryTitle { get; set; }
    public string CompanyName { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public string Hash { get; set; }
    public bool SyncedToCloud { get; set; }
}
```

### Certificate ID Format

Certificate IDs follow the pattern: `{Code}-{Year}-{Sequence}`

Examples:
- `SL-2026-000042` (Survey Listing)
- `GC-2026-000015` (GNSS Calibration)
- `MRU-2026-000008` (MRU Calibration)

---

## Creating Certificates

### Using CertificateHelper

The `CertificateHelper` class provides convenient methods for certificate creation:

#### Quick Creation with Dialog

```csharp
var certificate = await CertificateHelper.QuickCreate(licenseManager)
    .ForModule("SurveyListing", "SL", "1.0.45")
    .WithProject("Pipeline Survey", "North Sea")
    .WithVessel("MV Survey One")
    .WithClient("Example Oil Co")
    .AddData("Total Points", "15,432")
    .AddData("Survey Length", "45.7 km")
    .AddData("Processing Method", "Cubic Spline")
    .AddInputFile("survey.npd")
    .AddInputFile("route.rlx")
    .AddOutputFile("listing.xlsx")
    .AddOutputFile("track.dxf")
    .CreateWithDialogAsync(this);

if (certificate != null)
{
    // Certificate created successfully
    Console.WriteLine($"Certificate ID: {certificate.CertificateId}");
}
```

#### Direct API Method

```csharp
var processingData = new Dictionary<string, string>
{
    { "Total Points", "15,432" },
    { "Survey Length", "45.7 km" }
};

var inputFiles = new List<string> { "survey.npd", "route.rlx" };
var outputFiles = new List<string> { "listing.xlsx", "track.dxf" };

var certificate = await _certService.CreateCertificateAsync(
    moduleId: "SurveyListing",
    moduleCertificateCode: "SL",
    moduleVersion: "1.0.45",
    projectName: "Pipeline Survey",
    processingData: processingData,
    inputFiles: inputFiles,
    outputFiles: outputFiles,
    projectLocation: "North Sea",
    vessel: "MV Survey One",
    client: "Example Oil Co",
    owner: this);
```

### Signatory Dialog

When using `CreateWithDialogAsync` or `CreateCertificateAsync`, a signatory dialog is shown:

```
+------------------------------------+
|       Certificate Signatory        |
+------------------------------------+
|                                    |
|  Name:    [________________]       |
|  Title:   [________________]       |
|  Company: [________________]       |
|                                    |
|  [ ] Save as default signatory     |
|                                    |
|        [Cancel]   [Create]         |
+------------------------------------+
```

### Silent Creation (No Dialog)

For batch processing without user interaction:

```csharp
var certificate = await CertificateHelper.QuickCreate(licenseManager)
    .ForModule("BatchProcessor", "BP", "1.0.0")
    .WithProject("Batch Job")
    .AddData("Files Processed", "100")
    .CreateAsync("John Smith", "Survey Corp", "Survey Engineer");
```

---

## Certificate Storage

### Local Database

Certificates are stored locally in:
```
%AppData%\FathomOS\Certificates\certificates.db
```

### Retrieving Certificates

```csharp
// Get all certificates for a module
var certificates = _certService.GetCertificates("SurveyListing");

// Get specific certificate
var cert = licenseManager.GetLocalCertificate("SL-2026-000042");
```

### Viewing Certificates

```csharp
// Show certificate viewer
_certService.ViewCertificate(certificate, this);

// Open certificate manager
_certService.ShowCertificateManager(this);
```

---

## Cloud Synchronization

### Automatic Sync

When online, certificates are automatically synchronized to the cloud:

1. Certificate created locally
2. Hash computed for integrity
3. Certificate queued for sync
4. Background sync uploads to server
5. `SyncedToCloud` flag updated

### Manual Sync

```csharp
await licenseManager.SyncCertificatesAsync();
```

### Sync Status

Check if certificate has been synced:

```csharp
if (certificate.SyncedToCloud)
{
    Console.WriteLine("Certificate backed up to cloud");
}
else
{
    Console.WriteLine("Certificate pending sync");
}
```

---

## API Reference

### ICertificationService Interface

```csharp
public interface ICertificationService
{
    /// <summary>
    /// Creates a new processing certificate with UI dialog.
    /// </summary>
    Task<ProcessingCertificate?> CreateCertificateAsync(
        string moduleId,
        string moduleCertificateCode,
        string moduleVersion,
        string projectName,
        Dictionary<string, string>? processingData = null,
        List<string>? inputFiles = null,
        List<string>? outputFiles = null,
        string? projectLocation = null,
        string? vessel = null,
        string? client = null,
        Window? owner = null);

    /// <summary>
    /// Shows the certificate viewer for a certificate.
    /// </summary>
    void ViewCertificate(ProcessingCertificate certificate, Window? owner = null);

    /// <summary>
    /// Opens the certificate list/manager window.
    /// </summary>
    void ShowCertificateManager(Window? owner = null);

    /// <summary>
    /// Gets all local certificates for a module.
    /// </summary>
    IEnumerable<ProcessingCertificate> GetCertificates(string moduleId);
}
```

### CertificateHelper Static Methods

```csharp
public static class CertificateHelper
{
    /// <summary>
    /// Shows signatory dialog (set by Shell).
    /// </summary>
    public static Func<string?, Window?, SignatoryInfo?> ShowSignatoryDialog { get; set; }

    /// <summary>
    /// Shows certificate viewer (set by Shell).
    /// </summary>
    public static Action<ProcessingCertificate, bool, string?, Window?> ShowCertificateViewer { get; set; }

    /// <summary>
    /// Shows certificate manager (set by Shell).
    /// </summary>
    public static Action<LicenseManager, string?, Window?> ShowCertificateManager { get; set; }

    /// <summary>
    /// Creates a new certificate with dialog.
    /// </summary>
    public static Task<ProcessingCertificate?> CreateWithDialogAsync(...);

    /// <summary>
    /// Creates a certificate silently.
    /// </summary>
    public static Task<ProcessingCertificate> CreateSilentAsync(...);

    /// <summary>
    /// Computes SHA-256 hash for a file.
    /// </summary>
    public static string ComputeFileHash(string filePath);

    /// <summary>
    /// Creates a fluent builder for certificates.
    /// </summary>
    public static QuickCertificateBuilder QuickCreate(LicenseManager licenseManager);
}
```

### QuickCertificateBuilder

```csharp
public class QuickCertificateBuilder
{
    public QuickCertificateBuilder ForModule(string moduleId, string certificateCode, string version);
    public QuickCertificateBuilder WithProject(string name, string? location = null);
    public QuickCertificateBuilder WithVessel(string vessel);
    public QuickCertificateBuilder WithClient(string client);
    public QuickCertificateBuilder AddData(string key, string value);
    public QuickCertificateBuilder WithData(Dictionary<string, string> data);
    public QuickCertificateBuilder AddInputFile(string filePath);
    public QuickCertificateBuilder AddInputFiles(IEnumerable<string> filePaths);
    public QuickCertificateBuilder AddOutputFile(string filePath);
    public QuickCertificateBuilder AddOutputFiles(IEnumerable<string> filePaths);

    public Task<ProcessingCertificate?> CreateWithDialogAsync(Window? owner = null);
    public Task<ProcessingCertificate> CreateAsync(string signatoryName, string companyName, string? title = null);
}
```

---

## Best Practices

### 1. Include Meaningful Processing Data

Document key parameters used in processing:

```csharp
.AddData("Smoothing Method", "Moving Average")
.AddData("Window Size", "5 points")
.AddData("Outliers Removed", "23")
.AddData("Total Points", "15,432")
```

### 2. Record All Input Files

Include all files that contributed to the output:

```csharp
.AddInputFile("survey_data.npd")
.AddInputFile("route_alignment.rlx")
.AddInputFile("tide_corrections.tide")
```

### 3. Record All Output Files

Document all generated outputs:

```csharp
.AddOutputFile("survey_listing.xlsx")
.AddOutputFile("survey_track.dxf")
.AddOutputFile("survey_report.pdf")
```

### 4. Use Consistent Module Codes

Use short, unique codes for each module:
- SL = Survey Listing
- GC = GNSS Calibration
- MRU = MRU Calibration
- USBL = USBL Verification

---

## Related Documentation

- [Core API](../API/Core-API.md#certificates)
- [Shell API](../API/Shell-API.md#certification-service)
- [Module Development](../DeveloperGuide.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
