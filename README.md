# Fathom OS

**Modular Hydrographic Survey Processing Platform**

Fathom OS is a comprehensive suite of tools for processing hydrographic survey data. The application features a modular architecture where each processing function is a separate module that integrates seamlessly with the main platform.

## Architecture

```
FathomOS/
├── FathomOS.Core/                      # Shared library (models, parsers, services)
├── FathomOS.Shell/                     # Main application with dashboard
├── FathomOS.Modules.SurveyListing/     # Survey Listing Generator module
├── FathomOS.Modules.Calibration/       # Calibration module (planned)
├── FathomOS.Modules.Verification/      # Verification module (planned)
└── FathomOS.Tests/                     # Unit tests
```

## Modules

### Survey Listing Generator (v1.5.6)
Generate survey listings from NPD files with:
- Route alignment (RLX files)
- Tide corrections
- Position and depth smoothing
- KP and DCC calculations
- Interactive CAD-like editor
- Multiple export formats (Excel, DXF, PDF, Text)

### Additional Modules (Planned)
- **Calibration** - Survey calibration using control points
- **Verification** - Survey verification and QC reports

## Technology Stack

- **.NET 8.0** - Windows desktop framework
- **WPF** - Windows Presentation Foundation UI
- **C# 12** - Latest language features

### NuGet Packages
- MathNet.Numerics - Numerical computations
- ClosedXML - Excel file generation
- netDxf - DXF file export
- System.Text.Json - JSON serialization

## Building

```bash
# Build entire solution
dotnet build FathomOS.sln

# Build release
dotnet build FathomOS.sln -c Release

# Run tests
dotnet test FathomOS.Tests
```

## Module Development

To create a new module, see the [Module Integration Guide](S7_FATHOM_MODULE_INTEGRATION_GUIDE.md).

Key requirements:
1. Implement `IModule` interface from FathomOS.Core
2. Create `ModuleInfo.json` metadata file
3. Include 128x128 module icon
4. Use `SurveyPoint` model from Core for survey data
5. Reference Core parsers (don't duplicate)

## Project Files

Fathom OS uses `.s7p` project files (JSON format) that can store:
- Shared survey data
- Module-specific settings
- Processing results

## License

© 2024 S7 Solutions. All rights reserved.

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0 | 2024-12-12 | Initial modular architecture |
| 1.5.6 | 2024-12-12 | Survey Listing module (migrated from standalone) |

## v1.0.44 Changes (January 2026)

### New: Digital Certificate System
- **Processing Certificates**: Generate professional certificates for completed jobs
- **Offline Generation**: Create certificates locally, sync when online
- **White-Label Support**: Certificates include licensee branding
- **Certificate Manager**: Access via "📜 Certificates" button in dashboard footer

### Certificate API for Modules
```csharp
var cert = await CertificateHelper.QuickCreate(App.LicenseManager)
    .ForModule("SurveyListing", "SL", "1.0.0")
    .WithProject("Project Name", "Location")
    .AddData("Points", "15,432")
    .CreateWithDialogAsync(this);
```

### New Files
- `FathomOS.Core/Certificates/CertificateHelper.cs` - Fluent API
- `FathomOS.Core/Certificates/CertificatePdfGenerator.cs` - HTML generator
- `FathomOS.Shell/Views/CertificateListWindow.xaml` - Certificate manager
- `FathomOS.Shell/Views/CertificateViewerWindow.xaml` - Certificate viewer
- `FathomOS.Shell/Views/SignatoryDialog.xaml` - Signatory selection

### Updated
- LicensingSystem.Client - Certificate storage and sync
- LicensingSystem.Shared - Certificate models
- FathomOS.Core.csproj - Added Microsoft.Data.Sqlite
- FathomOS.Shell - Dashboard certificate button
