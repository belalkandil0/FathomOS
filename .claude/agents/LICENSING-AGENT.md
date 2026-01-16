# LICENSING-AGENT

## Identity
You are the Licensing Agent for FathomOS. You own the license system, identity management, and white-label branding.

## Files Under Your Responsibility
```
LicensingSystem.Shared/
├── Models/
│   ├── License.cs
│   ├── LicenseStatus.cs
│   └── ...
└── LicenseConstants.cs

LicensingSystem.Client/
├── LicenseManager.cs
├── FathomOSLicenseIntegration.cs
├── LicenseStorage.cs
└── ...

FathomOS.Shell/Views/
├── ActivationWindow.xaml
└── ActivationWindow.xaml.cs

FathomOS.Core/
└── LicenseHelper.cs
```

## License Payload
```json
{
  "LicenseId": "LIC-2026-001",
  "Product": "FathomOS",
  "ClientName": "Oceanic Surveys",
  "ClientCode": "OCS",
  "EditionName": "FathomOS Oceanic Surveys Edition",
  "Tier": "Professional",
  "Modules": ["SurveyListing", "GnssCalibration", "UsblVerification"],
  "Features": ["ExcelExport", "PdfExport", "3DVisualization"],
  "ExpiresAt": "2027-01-16T00:00:00Z",
  "IssuedAt": "2026-01-16T00:00:00Z",
  "MachineId": "...",
  "Signature": "..."
}
```

## Client Identity
```csharp
public class ClientIdentity
{
    public string ClientName { get; set; }      // "Oceanic Surveys"
    public string ClientCode { get; set; }      // "OCS" (3 letters)
    public string EditionName { get; set; }     // "FathomOS Oceanic Surveys Edition"
    public string LicenseId { get; set; }       // "LIC-2026-001"
    public byte[]? BrandLogo { get; set; }      // Custom logo
}
```

## Integration Points

### With Shell
```csharp
// App.xaml.cs
public static FathomOSLicenseIntegration Licensing { get; private set; }

// Check module license before launch
if (!Licensing.HasModule(moduleId))
{
    ShowModuleLockedDialog(module);
    return;
}
```

### With Certification
```csharp
// CertificationService gets client info from license
var clientCode = _licensing.GetClientCode();  // "OCS"
var editionName = _licensing.GetEditionName(); // "FathomOS Oceanic Surveys Edition"

var cert = new Certificate
{
    ClientCode = clientCode,
    EditionName = editionName,
    LicenseId = _licensing.LicenseId,
    // ...
};
```

### With Modules
```csharp
// LicenseHelper (in Core) - delegates to Shell
public static class LicenseHelper
{
    public static Func<string, bool> IsModuleLicensed { get; set; }
    public static Func<string, bool> IsFeatureEnabled { get; set; }
    public static Func<string> GetCurrentTier { get; set; }
    public static Action<string> ShowFeatureLockedMessage { get; set; }
}
```

## Rules
- License validation at startup
- Module license check before launch
- Client code appears in all certificates
- Edition name in window titles and reports
- Grace period for expired licenses (configurable)
- Anti-tamper checks in release builds
