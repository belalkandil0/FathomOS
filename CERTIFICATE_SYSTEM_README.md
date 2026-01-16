# Fathom OS v1.0.44 - Complete Update Package

## 📦 Package Contents

```
FathomOS_Complete_Update/
├── LicensingSystem.Client/           ← Updated license manager with certificates
│   ├── HardwareFingerprint.cs
│   ├── LicenseClient.cs
│   ├── LicenseManager.cs            ← Main entry point (has CreateCertificateAsync)
│   ├── LicenseStorage.cs
│   ├── LicenseValidator.cs
│   └── LicensingSystem.Client.csproj
│
├── LicensingSystem.Shared/          ← Shared models
│   ├── LicenseModels.cs             ← ProcessingCertificate + branding classes
│   └── LicensingSystem.Shared.csproj
│
├── FathomOS.Core/Certificates/      ← Certificate UI helpers
│   ├── CertificatePdfGenerator.cs   ← HTML/PDF generation
│   └── CertificateHelper.cs         ← Simplified API for modules
│
├── FathomOS.Shell/Views/            ← Certificate UI windows
│   ├── SignatoryDialog.xaml/.cs     ← Collects signatory info
│   ├── CertificateViewerWindow.xaml/.cs  ← Views certificates
│   └── CertificateListWindow.xaml/.cs    ← Lists all certificates
│
├── SurveyListingGenerator/Updates/  ← Example module integration
│   └── CertificateIntegration.cs
│
└── README.md                        ← This file
```

---

## 🚀 Integration Steps

### Step 1: Add NuGet Package

Add to `FathomOS.Core.csproj`:

```xml
<ItemGroup>
  <!-- Required for certificate local storage (already in LicensingSystem.Client) -->
  <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
</ItemGroup>
```

### Step 2: Replace/Copy Files

1. **Replace entire `LicensingSystem.Client/` folder** with the one from this package
2. **Replace entire `LicensingSystem.Shared/` folder** with the one from this package
3. **Copy `FathomOS.Core/Certificates/` folder** to your project
4. **Copy `FathomOS.Shell/Views/` files** to your project (4 files: 2 XAML + 2 code-behind)

### Step 3: Add Project References (if not already present)

In `FathomOS.Shell.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\LicensingSystem.Shared\LicensingSystem.Shared.csproj" />
  <ProjectReference Include="..\LicensingSystem.Client\LicensingSystem.Client.csproj" />
</ItemGroup>
```

In `FathomOS.Core.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\LicensingSystem.Shared\LicensingSystem.Shared.csproj" />
  <ProjectReference Include="..\LicensingSystem.Client\LicensingSystem.Client.csproj" />
</ItemGroup>
```

### Step 4: Update App.xaml.cs

```csharp
using LicensingSystem.Client;
using LicensingSystem.Shared;

public partial class App : Application
{
    // Make LicenseManager available globally
    public static LicenseManager? LicenseManager { get; private set; }
    
    // Brand logo for certificates (cached)
    public static string? BrandLogo { get; private set; }
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize License Manager
        LicenseManager = new LicenseManager(
            productName: LicenseConstants.ProductName,
            serverUrl: "https://your-license-server.com"  // Your server URL
        );
        
        // Check license
        var result = await LicenseManager.CheckLicenseAsync();
        
        if (!result.IsValid)
        {
            // Show activation window
            var activationWindow = new ActivationWindow();
            activationWindow.ShowDialog();
            
            // Re-check after activation
            result = await LicenseManager.CheckLicenseAsync(forceRefresh: true);
        }
        
        if (result.IsValid)
        {
            // Cache brand logo for certificates
            try
            {
                var (logoUrl, logoBase64, _) = await LicenseManager.GetBrandLogoAsync();
                BrandLogo = logoBase64 ?? logoUrl;
            }
            catch { /* Ignore logo errors */ }
            
            // Show main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        else
        {
            // No valid license - exit
            MessageBox.Show(
                "A valid license is required to use Fathom OS.",
                "License Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
        }
    }
}
```

### Step 5: Add Certificate Menu to MainWindow

In your main window XAML, add a menu item:

```xml
<Menu>
    <MenuItem Header="_File">
        <!-- ... existing items ... -->
    </MenuItem>
    <MenuItem Header="_Tools">
        <MenuItem Header="Certificate Manager..." Click="menuCertificateManager_Click"/>
        <Separator/>
        <!-- ... other tools ... -->
    </MenuItem>
</Menu>
```

In your MainWindow.xaml.cs:

```csharp
using FathomOS.Core.Certificates;
using LicensingSystem.Client;

private void menuCertificateManager_Click(object sender, RoutedEventArgs e)
{
    if (App.LicenseManager == null)
    {
        MessageBox.Show("License manager not available.", "Error",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    
    CertificateHelper.OpenCertificateManager(App.LicenseManager, this, App.BrandLogo);
}
```

### Step 6 (Optional): Update Window Titles for Branding

```csharp
// In any window that should show branding:
private void UpdateWindowTitle()
{
    var brandingInfo = App.LicenseManager?.GetBrandingInfo();
    if (brandingInfo != null && !string.IsNullOrEmpty(brandingInfo.Brand))
    {
        Title = $"Survey Listing Generator — {brandingInfo.DisplayEdition}";
    }
    else
    {
        Title = "Survey Listing Generator — Fathom OS";
    }
}
```

---

## 📋 Module Integration (Survey Listing Example)

After processing completes in your module, create a certificate:

```csharp
using FathomOS.Core.Certificates;

// Simple one-liner approach:
private async Task OnProcessingComplete()
{
    if (App.LicenseManager == null) return;
    
    await CertificateHelper.QuickCreate(App.LicenseManager)
        .ForModule("SurveyListing", "SL", "1.0.43")
        .WithProject(txtProjectName.Text, txtLocation.Text)
        .WithVessel(txtVessel.Text)
        .WithClient(txtClient.Text)
        .AddData("Total Points", _stats.TotalPoints.ToString("N0"))
        .AddData("KP Range", $"{_stats.StartKp:F3} — {_stats.EndKp:F3} km")
        .AddData("Depth Range", $"{_stats.MinDepth:F1} to {_stats.MaxDepth:F1} m")
        .AddData("Coordinate System", cboCoordSystem.Text)
        .AddInputFile(_inputFilePath)
        .AddOutputFile(_outputFilePath)
        .CreateWithDialogAsync(this);
}
```

This will:
1. Show the `SignatoryDialog` to collect name, title, company
2. Create the certificate (works offline!)
3. Store it locally (auto-syncs when online)
4. Show the `CertificateViewerWindow` with the result

---

## 🔧 Module Certificate Codes

| Module | Code | Description |
|--------|------|-------------|
| SurveyListing | SL | Survey listing generator |
| TideAnalysis | TA | Tide analysis tools |
| Calibrations | CA | Equipment calibrations |
| SoundVelocity | SV | Sound velocity processing |
| NetworkTimeSync | NT | Network time synchronization |
| BatchProcessor | BP | Batch processing module |
| USBLCalibration | UC | USBL calibration |
| DepthAnalysis | DA | Depth analysis module |

---

## 📊 Certificate Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        MODULE (Offline)                          │
│  await CertificateHelper.QuickCreate(licenseManager)            │
│      .ForModule("SurveyListing", "SL", "1.0.43")               │
│      .WithProject("Pipeline Survey", "Gulf of Mexico")          │
│      .AddData("Total Points", "15,234")                         │
│      .CreateWithDialogAsync(this);                              │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    LOCAL STORAGE (SQLite)                        │
│  Certificate ID: FOS-S7-2501-00001-X3B7                         │
│  Status: Pending Sync                                            │
└───────────────────────────┬─────────────────────────────────────┘
                            │ (When online - automatic)
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     LICENSE SERVER                               │
│  POST /api/certificates/sync                                     │
│  Response: Synced + Verified                                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎨 Certificate ID Format

```
FOS-XX-YYMM-NNNNN-CCCC
│   │  │    │     └── Check digits (4 chars)
│   │  │    └── Sequence number (5 digits)
│   │  └── Year/Month (4 digits)
│   └── Licensee Code (2 chars, e.g., "S7")
└── Fathom OS prefix
```

Example: `FOS-S7-2501-00001-X3B7`

---

## ✅ What's New in v1.0.44

1. **Offline Certificate Generation** - Works fully offline, syncs when online
2. **White-Label Branding** - Certificates show company logo and name
3. **Certificate Manager UI** - View, export, and track all certificates
4. **Fluent API** - Easy one-liner certificate creation for modules
5. **Auto-Sync** - Certificates automatically sync during license validation
6. **HTML Export** - Export certificates as HTML (print to PDF from browser)

---

## ❓ Questions for License Manager Team

**All requirements appear to be met!** The uploaded `LicensingSystem_Client` package includes:

- ✅ `CreateCertificateAsync()` method
- ✅ `GetLocalCertificates()` / `GetLocalCertificate()` methods  
- ✅ `GetLocalCertificateStats()` method
- ✅ `GetBrandingInfo()` method
- ✅ `GetBrandLogoAsync()` method
- ✅ `SyncPendingCertificatesAsync()` / `VerifyPendingCertificatesAsync()` methods
- ✅ `ProcessingCertificate` model with all required fields
- ✅ `LocalCertificateEntry` class with sync/verify status
- ✅ `LicenseBrandingInfo` class with Brand, LicenseeCode, etc.

**No additional information needed from the License Manager team.**

---

## 📁 Files Changed Summary

| File | Action | Description |
|------|--------|-------------|
| `LicensingSystem.Client/*` | REPLACE | Updated license manager with certificates |
| `LicensingSystem.Shared/*` | REPLACE | Updated models |
| `FathomOS.Core/Certificates/CertificatePdfGenerator.cs` | NEW | HTML certificate generator |
| `FathomOS.Core/Certificates/CertificateHelper.cs` | NEW | Simplified API for modules |
| `FathomOS.Shell/Views/SignatoryDialog.xaml` | NEW | Signatory input dialog |
| `FathomOS.Shell/Views/SignatoryDialog.xaml.cs` | NEW | Code-behind |
| `FathomOS.Shell/Views/CertificateViewerWindow.xaml` | NEW | Certificate viewer |
| `FathomOS.Shell/Views/CertificateViewerWindow.xaml.cs` | NEW | Code-behind |
| `FathomOS.Shell/Views/CertificateListWindow.xaml` | NEW | Certificate list |
| `FathomOS.Shell/Views/CertificateListWindow.xaml.cs` | NEW | Code-behind |
| `App.xaml.cs` | UPDATE | Add initialization code |
| `MainWindow.xaml` | UPDATE | Add certificate menu |
| Each module | UPDATE | Add certificate generation |

---

## 🔒 Security Notes

- Certificates are **NOT cryptographically signed on the client** (no private key)
- Server verification returns "✓ In our records" (not "signature verified")
- Local storage uses DPAPI encryption (same as license)
- Certificate IDs include checksum for tamper detection

---

**Version**: 1.0.44  
**Date**: January 2025
