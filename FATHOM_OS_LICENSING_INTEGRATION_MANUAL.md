# Fathom OS Licensing System - Integration Manual

## Version 2.0 | December 2024

---

## Table of Contents

1. [Overview](#1-overview)
2. [Project Setup](#2-project-setup)
3. [Basic Integration](#3-basic-integration)
4. [Tier System](#4-tier-system)
5. [Module Licensing](#5-module-licensing)
6. [Feature Checking API](#6-feature-checking-api)
7. [License Activation](#7-license-activation)
8. [Offline Support](#8-offline-support)
9. [Diagnostics & Troubleshooting](#9-diagnostics--troubleshooting)
10. [Common Issues & Fixes](#10-common-issues--fixes)
11. [Complete Code Examples](#11-complete-code-examples)

---

## 1. Overview

### 1.1 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         S7 FATHOM APP                           │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              LicensingSystem.Client Library               │  │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐ │  │
│  │  │ LicenseManager │ │ LicenseValidator │ │ LicenseStorage │ │  │
│  │  └─────────────┘ └─────────────┘ └─────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
└──────────────────────────────┼──────────────────────────────────┘
                               │ HTTPS
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    LICENSE SERVER (Render)                      │
│         https://s7aborern-license-server.onrender.com           │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 License Types

| Type | Description | Activation |
|------|-------------|------------|
| **Online License** | Activated with a license key, validated against server | Enter key + email in app |
| **Offline License** | Pre-generated .json file, works without internet | Import .json file |

### 1.3 Tier System

| Tier | Feature Name | Description |
|------|--------------|-------------|
| **Basic** | `Tier:Basic` | Entry level, limited modules |
| **Professional** | `Tier:Professional` | Most modules included |
| **Enterprise** | `Tier:Enterprise` | All modules + priority support |
| **Custom** | `Tier:Custom` | Hand-picked modules |

### 1.4 Module Licensing

Modules are licensed using the feature format: `Module:{ModuleId}`

Examples:
- `Module:SurveyListing`
- `Module:SoundVelocity`
- `Module:TideAnalysis`
- `Module:NetworkTimeSync`

---

## 2. Project Setup

### 2.1 Required Project Reference

Add the `LicensingSystem.Client` project to your Fathom OS solution:

```xml
<!-- In FathomOS.Shell.csproj or your main project -->
<ItemGroup>
  <ProjectReference Include="..\LicensingSystem.Client\LicensingSystem.Client.csproj" />
</ItemGroup>
```

### 2.2 Required Dependencies

The Client library requires these NuGet packages (already in .csproj):

```xml
<PackageReference Include="System.Management" Version="8.0.0" />
<PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
```

### 2.3 Project Structure

```
FathomOS/
├── LicensingSystem.Client/          ← License client library
│   ├── LicenseManager.cs            ← Main API class
│   ├── LicenseValidator.cs          ← Offline validation
│   ├── LicenseStorage.cs            ← Secure storage (DPAPI)
│   ├── LicenseClient.cs             ← Server communication
│   ├── HardwareFingerprint.cs       ← Hardware ID generation
│   └── LicensingSystem.Client.csproj
│
├── LicensingSystem.Shared/          ← Shared models
│   ├── LicenseModels.cs
│   └── LicensingSystem.Shared.csproj
│
├── FathomOS.Shell/                  ← Your main application
└── FathomOS.Core/
```

---

## 3. Basic Integration

### 3.1 Initialize LicenseManager in App.xaml.cs

```csharp
using LicensingSystem.Client;

namespace FathomOS.Shell
{
    public partial class App : Application
    {
        // Global license manager instance
        public static LicenseManager LicenseManager { get; private set; } = null!;
        
        // Server URL for online validation
        private const string LICENSE_SERVER_URL = "https://s7aborern-license-server.onrender.com";
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize license manager with server URL
            LicenseManager = new LicenseManager("FathomOS", LICENSE_SERVER_URL);
            
            // Check license at startup
            CheckLicenseAsync();
        }
        
        private async void CheckLicenseAsync()
        {
            var result = await LicenseManager.CheckLicenseAsync();
            
            if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            {
                // Show activation dialog
                ShowActivationWindow();
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            LicenseManager?.Dispose();
            base.OnExit(e);
        }
    }
}
```

### 3.2 Quick License Check

```csharp
// Synchronous check (for simple cases)
var result = App.LicenseManager.CheckLicense();

if (result.IsValid)
{
    // Licensed - proceed normally
}
else if (result.Status == LicenseStatus.GracePeriod)
{
    // Expired but in grace period - show warning
    MessageBox.Show($"License expired! {result.GraceDaysRemaining} grace days remaining.");
}
else
{
    // Not licensed - show activation dialog
    ShowActivationDialog();
}
```

---

## 4. Tier System

### 4.1 Tier Feature Names

**IMPORTANT:** Tiers are stored as features with the prefix `Tier:`

| Tier Name | Feature String | Check Method |
|-----------|----------------|--------------|
| Basic | `Tier:Basic` | `HasTier("Basic")` |
| Professional | `Tier:Professional` | `HasTier("Professional")` |
| Enterprise | `Tier:Enterprise` | `HasTier("Enterprise")` |
| Custom | `Tier:Custom` | `HasTier("Custom")` |

### 4.2 Checking Tier

```csharp
// Method 1: Get current tier name
string? currentTier = App.LicenseManager.GetLicenseTier();
// Returns: "Basic", "Professional", "Enterprise", or null

// Method 2: Check for specific tier
if (App.LicenseManager.HasTier("Professional"))
{
    // User has Professional tier
}

// Method 3: Check tier level (for >= comparisons)
if (App.LicenseManager.HasTierOrHigher("Professional"))
{
    // User has Professional OR Enterprise
}

// Method 4: Check raw feature
if (App.LicenseManager.HasFeature("Tier:Professional"))
{
    // User has Professional tier
}
```

### 4.3 Tier Hierarchy

```
Enterprise (level 3) > Professional (level 2) > Basic (level 1)
```

`HasTierOrHigher("Professional")` returns true for both Professional AND Enterprise.

### 4.4 ⚠️ IMPORTANT: Don't Use "PRO"

**WRONG:**
```csharp
// ❌ This will NOT work
if (App.LicenseManager.HasFeature("PRO"))
if (App.LicenseManager.HasTier("PRO"))
```

**CORRECT:**
```csharp
// ✅ Use full tier name
if (App.LicenseManager.HasTier("Professional"))
if (App.LicenseManager.HasFeature("Tier:Professional"))
```

---

## 5. Module Licensing

### 5.1 Module Feature Names

Modules are stored as features with the prefix `Module:`

| Module | Feature String |
|--------|----------------|
| Survey Listing | `Module:SurveyListing` |
| Sound Velocity | `Module:SoundVelocity` |
| Tide Analysis | `Module:TideAnalysis` |
| Network Time Sync | `Module:NetworkTimeSync` |
| Sensor Calibration | `Module:SensorCalibration` |

### 5.2 Checking Module Access

```csharp
// Check if specific module is licensed
if (App.LicenseManager.IsModuleLicensed("SurveyListing"))
{
    // User can access Survey Listing module
}

// Get all licensed modules
List<string> licensedModules = App.LicenseManager.GetLicensedModules();
// Returns: ["SurveyListing", "SoundVelocity", ...]
```

### 5.3 Module Launch Gate

```csharp
public void LaunchModule(string moduleId)
{
    // Check license first
    if (!App.LicenseManager.IsModuleLicensed(moduleId))
    {
        MessageBox.Show(
            $"The '{moduleId}' module requires a license upgrade.\n\n" +
            "Please contact sales to upgrade your license.",
            "Module Not Licensed",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
    }
    
    // Launch the module
    var module = ModuleManager.GetModule(moduleId);
    module?.Launch();
}
```

### 5.4 Dashboard Tile Visibility

```csharp
// In your Dashboard/ModuleManager
foreach (var module in allModules)
{
    var tile = CreateModuleTile(module);
    
    if (!App.LicenseManager.IsModuleLicensed(module.ModuleId))
    {
        // Gray out or hide unlicensed modules
        tile.IsEnabled = false;
        tile.Opacity = 0.5;
        tile.ToolTip = "This module requires a license upgrade";
    }
    
    dashboard.AddTile(tile);
}
```

---

## 6. Feature Checking API

### 6.1 Complete API Reference

```csharp
// === LICENSE STATUS ===
var result = await App.LicenseManager.CheckLicenseAsync();
// or synchronous:
var result = App.LicenseManager.CheckLicense();

result.IsValid          // bool - Is license valid?
result.Status           // LicenseStatus enum
result.Message          // string - Human-readable message
result.DaysRemaining    // int - Days until expiration
result.License          // LicenseFile - Full license details
result.EnabledFeatures  // List<string> - All enabled features

// === TIER CHECKING ===
string? tier = App.LicenseManager.GetLicenseTier();
bool hasTier = App.LicenseManager.HasTier("Professional");
bool hasTierOrHigher = App.LicenseManager.HasTierOrHigher("Professional");

// === MODULE CHECKING ===
bool hasModule = App.LicenseManager.IsModuleLicensed("SurveyListing");
List<string> modules = App.LicenseManager.GetLicensedModules();

// === RAW FEATURE CHECKING ===
bool hasFeature = App.LicenseManager.HasFeature("Tier:Professional");
bool hasFeature = App.LicenseManager.HasFeature("Module:SurveyListing");
bool hasFeature = App.LicenseManager.HasFeature("CustomFeature");

// === LICENSE INFO ===
string hwid = App.LicenseManager.GetHardwareId();
var info = App.LicenseManager.GetLicenseInfo();
```

### 6.2 LicenseStatus Enum

```csharp
public enum LicenseStatus
{
    Valid,              // License is valid and active
    GracePeriod,        // Expired but within 7-day grace period
    Expired,            // License has expired
    Revoked,            // License was revoked by admin
    HardwareMismatch,   // Running on different hardware
    InvalidSignature,   // License file is corrupted/forged
    NotFound,           // No license installed
    Corrupted           // License file is damaged
}
```

### 6.3 Status Handling Pattern

```csharp
var result = await App.LicenseManager.CheckLicenseAsync();

switch (result.Status)
{
    case LicenseStatus.Valid:
        // All good - normal operation
        break;
        
    case LicenseStatus.GracePeriod:
        // Show warning banner
        ShowExpirationWarning(result.GraceDaysRemaining);
        // Still allow normal operation
        break;
        
    case LicenseStatus.Expired:
        // Block access, show renewal dialog
        ShowRenewalDialog();
        return;
        
    case LicenseStatus.Revoked:
        // License was revoked - contact support
        ShowRevokedMessage(result.Message);
        return;
        
    case LicenseStatus.HardwareMismatch:
        // Hardware changed - need reactivation
        ShowReactivationDialog();
        return;
        
    case LicenseStatus.NotFound:
        // No license - show activation
        ShowActivationDialog();
        return;
        
    default:
        // Invalid/corrupted - show error
        ShowLicenseError(result.Message);
        return;
}
```

---

## 7. License Activation

### 7.1 Online Activation (License Key)

```csharp
public async Task<bool> ActivateOnline(string licenseKey, string email)
{
    try
    {
        var result = await App.LicenseManager.ActivateOnlineAsync(licenseKey, email);
        
        if (result.IsValid)
        {
            MessageBox.Show(
                $"License activated successfully!\n\n" +
                $"Edition: {result.License?.Edition}\n" +
                $"Expires: {result.License?.ExpiresAt:yyyy-MM-dd}\n" +
                $"Days Remaining: {result.DaysRemaining}",
                "Activation Successful",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return true;
        }
        else
        {
            MessageBox.Show(
                $"Activation failed: {result.Message}",
                "Activation Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"Network error: {ex.Message}\n\nPlease check your internet connection.",
            "Connection Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
    }
}
```

### 7.2 Offline Activation (License File)

```csharp
public bool ActivateFromFile()
{
    var dialog = new OpenFileDialog
    {
        Title = "Select License File",
        Filter = "License Files (*.json;*.lic)|*.json;*.lic|All Files (*.*)|*.*",
        DefaultExt = ".json"
    };
    
    if (dialog.ShowDialog() == true)
    {
        var result = App.LicenseManager.ActivateFromFile(dialog.FileName);
        
        if (result.IsValid)
        {
            MessageBox.Show(
                $"License imported successfully!\n\n" +
                $"Edition: {result.License?.Edition}\n" +
                $"Expires: {result.License?.ExpiresAt:yyyy-MM-dd}",
                "Import Successful",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return true;
        }
        else
        {
            MessageBox.Show(
                $"Import failed: {result.Message}",
                "Import Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }
    
    return false;
}
```

### 7.3 Complete Activation Dialog

```csharp
// ActivationWindow.xaml.cs
public partial class ActivationWindow : Window
{
    public ActivationWindow()
    {
        InitializeComponent();
        
        // Display hardware ID for support
        HardwareIdTextBlock.Text = App.LicenseManager.GetHardwareId();
    }
    
    private async void ActivateOnlineButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text) ||
            string.IsNullOrWhiteSpace(EmailTextBox.Text))
        {
            MessageBox.Show("Please enter your license key and email.");
            return;
        }
        
        IsEnabled = false;
        StatusText.Text = "Activating...";
        
        try
        {
            var result = await App.LicenseManager.ActivateOnlineAsync(
                LicenseKeyTextBox.Text.Trim(),
                EmailTextBox.Text.Trim());
            
            if (result.IsValid)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                StatusText.Text = result.Message;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            IsEnabled = true;
        }
    }
    
    private void ImportFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "License Files (*.json)|*.json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var result = App.LicenseManager.ActivateFromFile(dialog.FileName);
            
            if (result.IsValid)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                StatusText.Text = result.Message;
            }
        }
    }
}
```

---

## 8. Offline Support

### 8.1 How Offline Mode Works

1. **First activation** requires internet (online key) or a pre-generated license file (offline)
2. **After activation**, the license is stored locally in encrypted form
3. **Periodic online checks** happen every 30 minutes when internet is available
4. **If no internet**, the app continues to work using the stored license
5. **Grace period**: 7 days after expiration, license still works

### 8.2 Storage Locations

```
Primary:   %LOCALAPPDATA%\FathomOS\License\license.dat
Backup:    HKEY_CURRENT_USER\SOFTWARE\FathomOS\License
Metadata:  %LOCALAPPDATA%\FathomOS\License\metadata.dat
Revocation: %LOCALAPPDATA%\FathomOS\License\revoked.dat
```

### 8.3 Force Online Check

```csharp
// Force immediate server validation
var result = await App.LicenseManager.ForceServerCheckAsync();

if (result.Status == LicenseStatus.Revoked)
{
    // License was revoked on server
    ShowRevokedMessage();
}
```

---

## 9. Diagnostics & Troubleshooting

### 9.1 Get Diagnostics

```csharp
// Get comprehensive diagnostic information
var diagnostics = App.LicenseManager.GetDiagnostics();

// Display or log the diagnostics
Debug.WriteLine(diagnostics.ToString());
// or
MessageBox.Show(diagnostics.ToString(), "License Diagnostics");
```

### 9.2 Diagnostic Output Example

```
=== LICENSE DIAGNOSTICS ===
Has Stored License: True
Stored License ID: LIC-ABC123
Stored Expiry: 2025-12-31
Features: Tier:Professional, Module:SurveyListing, Module:SoundVelocity

In Local Revocation List: False
Local Revocation Reason: N/A
All Locally Revoked IDs: None

Last Online Check: 2024-12-27 15:30:00
Time Since Online Check: 00:45:00
Offline Sync Pending: False

Has Cached Result: True
Cached Status: Valid
Cached IsValid: True

License File: C:\Users\...\AppData\Local\FathomOS\License\license.dat
License File Exists: True
Revocation File: C:\Users\...\AppData\Local\FathomOS\License\revoked.dat
Revocation File Exists: False

Server URL: https://s7aborern-license-server.onrender.com
Has Online Capability: True
```

### 9.3 Check Local Revocation List

```csharp
// Check if a specific license is locally revoked
bool isRevoked = App.LicenseManager.IsInLocalRevocationList("LIC-ABC123");

// Get all locally revoked license IDs
List<string> revokedIds = App.LicenseManager.GetLocallyRevokedLicenseIds();

// Get revocation reason
string? reason = App.LicenseManager.GetLocalRevocationReason("LIC-ABC123");
```

### 9.4 Clear Local Revocation (Emergency Fix)

```csharp
// Clear revocation for specific license
App.LicenseManager.ClearLocalRevocationForLicense("LIC-ABC123");

// Clear ALL local revocations (use with caution!)
App.LicenseManager.ClearAllLocalRevocations();
```

---

## 10. Common Issues & Fixes

### 10.1 "License Revoked" But Server Shows Valid

**Symptom:** App shows "License Revoked" but the server dashboard shows the license is active.

**Cause:** License was added to local revocation list due to a temporary server error or network issue.

**Fix:**
```csharp
// Option 1: Clear specific license from revocation list
App.LicenseManager.ClearLocalRevocationForLicense("LIC-XXXXX");

// Option 2: Delete the revocation file manually
// Delete: %LOCALAPPDATA%\FathomOS\License\revoked.dat

// Option 3: Re-activate the license
await App.LicenseManager.ActivateOnlineAsync(licenseKey, email);
```

### 10.2 "License Not Found" After Reinstall

**Symptom:** License was working, but after reinstalling Windows or moving to new PC, license shows "Not Found".

**Cause:** License is bound to hardware. New hardware = different hardware fingerprint.

**Fix:**
1. Go to License Manager UI → Dashboard
2. Find the license → Click "Reset Activations"
3. Re-activate on the new computer

### 10.3 "Hardware Mismatch" Error

**Symptom:** App shows "Hardware Mismatch" after minor hardware changes.

**Cause:** Too many hardware components changed (motherboard, CPU, etc.)

**Note:** The system uses "fuzzy matching" - minor changes (RAM, disk) are tolerated. Major changes (motherboard + CPU) trigger mismatch.

**Fix:** Same as 10.2 - reset activations and re-activate.

### 10.4 License Works But Module Shows "Not Licensed"

**Symptom:** License is valid but specific module shows "Not Licensed".

**Cause:** The module is not included in the license tier/features.

**Check:**
```csharp
// Check what features are actually in the license
var result = App.LicenseManager.CheckLicense();
foreach (var feature in result.EnabledFeatures)
{
    Debug.WriteLine(feature);
}
```

**Fix:** Upgrade the license tier or add the specific module.

### 10.5 App Doesn't Work Without Internet

**Symptom:** App refuses to start when offline, even though it worked before.

**Cause:** Either:
- License was never activated (needs first activation online)
- Local license file was deleted/corrupted
- License is in local revocation list

**Fix:**
```csharp
var diag = App.LicenseManager.GetDiagnostics();
if (!diag.LicenseFileExists)
{
    // License file missing - need to re-activate
}
if (diag.IsInLocalRevocationList)
{
    // Clear revocation list
    App.LicenseManager.ClearAllLocalRevocations();
}
```

---

## 11. Complete Code Examples

### 11.1 App.xaml.cs - Full Implementation

```csharp
using System.Windows;
using LicensingSystem.Client;

namespace FathomOS.Shell
{
    public partial class App : Application
    {
        public static LicenseManager LicenseManager { get; private set; } = null!;
        
        private const string LICENSE_SERVER = "https://s7aborern-license-server.onrender.com";
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize license manager
            LicenseManager = new LicenseManager("FathomOS", LICENSE_SERVER);
            
            // Check license
            var result = await LicenseManager.CheckLicenseAsync();
            
            if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            {
                // Show activation window
                var activationWindow = new ActivationWindow();
                if (activationWindow.ShowDialog() != true)
                {
                    // User cancelled - exit app
                    Shutdown();
                    return;
                }
            }
            else if (result.Status == LicenseStatus.GracePeriod)
            {
                // Show expiration warning
                MessageBox.Show(
                    $"Your license has expired!\n\n" +
                    $"You have {result.GraceDaysRemaining} days remaining in the grace period.\n" +
                    $"Please renew your license to continue using Fathom OS.",
                    "License Expiring",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            
            // Show main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            LicenseManager?.Dispose();
            base.OnExit(e);
        }
    }
}
```

### 11.2 ModuleManager - License-Aware Module Loading

```csharp
using LicensingSystem.Client;

public class ModuleManager
{
    private readonly List<IModule> _modules = new();
    
    public void LoadModules()
    {
        // Discover all modules
        var allModules = DiscoverModules();
        
        foreach (var module in allModules)
        {
            // Check if module is licensed
            bool isLicensed = App.LicenseManager.IsModuleLicensed(module.ModuleId);
            
            module.IsLicensed = isLicensed;
            module.IsEnabled = isLicensed;
            
            _modules.Add(module);
        }
    }
    
    public void LaunchModule(string moduleId)
    {
        var module = _modules.FirstOrDefault(m => m.ModuleId == moduleId);
        if (module == null) return;
        
        if (!App.LicenseManager.IsModuleLicensed(moduleId))
        {
            ShowModuleNotLicensedDialog(moduleId);
            return;
        }
        
        module.Launch();
    }
    
    private void ShowModuleNotLicensedDialog(string moduleId)
    {
        var currentTier = App.LicenseManager.GetLicenseTier() ?? "None";
        
        MessageBox.Show(
            $"The '{moduleId}' module is not included in your current license.\n\n" +
            $"Current Tier: {currentTier}\n\n" +
            $"Please upgrade your license to access this module.",
            "Module Not Licensed",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
```

### 11.3 License Info Display

```csharp
public void DisplayLicenseInfo()
{
    var info = App.LicenseManager.GetLicenseInfo();
    
    // Update UI
    LicenseStatusText.Text = info.GetDisplayStatus();
    CustomerNameText.Text = info.CustomerName ?? "N/A";
    CustomerEmailText.Text = info.CustomerEmail ?? "N/A";
    EditionText.Text = info.Edition ?? "N/A";
    ExpiryText.Text = info.ExpiresAt?.ToString("yyyy-MM-dd") ?? "N/A";
    DaysRemainingText.Text = info.DaysRemaining.ToString();
    HardwareIdText.Text = info.HardwareId;
    
    // Show features
    FeaturesListBox.Items.Clear();
    foreach (var feature in info.EnabledFeatures)
    {
        FeaturesListBox.Items.Add(feature);
    }
    
    // Color code status
    StatusBorder.Background = info.Status switch
    {
        LicenseStatus.Valid => Brushes.Green,
        LicenseStatus.GracePeriod => Brushes.Orange,
        _ => Brushes.Red
    };
}
```

### 11.4 Periodic License Check (Background)

```csharp
public class LicenseCheckService
{
    private DispatcherTimer? _timer;
    
    public void Start()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5) // Check every 5 minutes
        };
        _timer.Tick += async (s, e) => await CheckLicenseAsync();
        _timer.Start();
    }
    
    private async Task CheckLicenseAsync()
    {
        var result = await App.LicenseManager.CheckLicenseAsync();
        
        if (result.Status == LicenseStatus.Revoked)
        {
            // License was revoked - notify user and exit
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "Your license has been revoked.\n\n" +
                    "Please contact support for assistance.",
                    "License Revoked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Application.Current.Shutdown();
            });
        }
        else if (result.Status == LicenseStatus.Expired)
        {
            // License expired - notify user
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "Your license has expired.\n\n" +
                    "Please renew to continue using Fathom OS.",
                    "License Expired",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }
    }
    
    public void Stop()
    {
        _timer?.Stop();
    }
}
```

---

## Quick Reference Card

### Feature Names

| Type | Format | Example |
|------|--------|---------|
| Tier | `Tier:{TierName}` | `Tier:Professional` |
| Module | `Module:{ModuleId}` | `Module:SurveyListing` |
| Custom | `{FeatureName}` | `AdvancedExport` |

### API Quick Reference

```csharp
// Check license
var result = await App.LicenseManager.CheckLicenseAsync();

// Check tier
App.LicenseManager.HasTier("Professional")
App.LicenseManager.HasTierOrHigher("Professional")
App.LicenseManager.GetLicenseTier()

// Check module
App.LicenseManager.IsModuleLicensed("SurveyListing")
App.LicenseManager.GetLicensedModules()

// Check any feature
App.LicenseManager.HasFeature("Tier:Professional")
App.LicenseManager.HasFeature("Module:SurveyListing")

// Activate
await App.LicenseManager.ActivateOnlineAsync(key, email)
App.LicenseManager.ActivateFromFile(path)

// Diagnostics
App.LicenseManager.GetDiagnostics()
App.LicenseManager.ClearLocalRevocationForLicense(licenseId)
```

### File Locations

```
License:    %LOCALAPPDATA%\FathomOS\License\license.dat
Revocation: %LOCALAPPDATA%\FathomOS\License\revoked.dat
Metadata:   %LOCALAPPDATA%\FathomOS\License\metadata.dat
Registry:   HKCU\SOFTWARE\FathomOS\License
```

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Dec 2024 | Initial integration guide |
| 2.0 | Dec 2024 | Added tier system, module licensing, diagnostics, troubleshooting, complete examples |

---

**END OF INTEGRATION MANUAL**
