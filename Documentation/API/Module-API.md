# FathomOS Module API Reference

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [IModule Interface](#imodule-interface)
3. [Module Properties](#module-properties)
4. [Module Methods](#module-methods)
5. [Module Discovery](#module-discovery)
6. [Module Groups](#module-groups)
7. [Best Practices](#best-practices)

---

## Overview

The `IModule` interface is the contract that all FathomOS modules must implement. It defines how the Shell discovers, loads, initializes, and interacts with modules.

### Namespace

```csharp
namespace FathomOS.Core.Interfaces;
```

### Assembly

```
FathomOS.Core.dll
```

---

## IModule Interface

### Complete Interface Definition

```csharp
using System.Windows;

namespace FathomOS.Core.Interfaces;

/// <summary>
/// Contract that all Fathom OS modules must implement.
/// This interface defines how the Shell discovers and interacts with modules.
/// </summary>
public interface IModule
{
    #region Identity Properties

    /// <summary>
    /// Unique identifier for the module (e.g., "SurveyListing", "Calibration").
    /// Used internally for module management and project files.
    /// Must be PascalCase without spaces.
    /// </summary>
    string ModuleId { get; }

    /// <summary>
    /// Display name shown on dashboard (e.g., "Survey Listing Generator").
    /// This is what users see on the module tile.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Short description for tooltip/card display.
    /// Should be 1-2 sentences describing the module's purpose.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Module version following semantic versioning.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Icon resource path for dashboard tile.
    /// Format: "/FathomOS.Modules.ModuleName;component/Assets/icon.png"
    /// </summary>
    string IconResource { get; }

    /// <summary>
    /// Category for grouping on dashboard.
    /// Examples: "Data Processing", "Quality Control", "Calibrations"
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Display order on dashboard (lower numbers appear first).
    /// Use increments of 10 to allow insertion: 10, 20, 30, etc.
    /// </summary>
    int DisplayOrder { get; }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Called when module is first loaded by the Shell.
    /// Use for initialization, service registration, settings loading.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Launch the module's main window.
    /// Called when user clicks the module tile on dashboard.
    /// </summary>
    /// <param name="owner">Optional parent window for modal behavior</param>
    void Launch(Window? owner = null);

    /// <summary>
    /// Called when application is closing.
    /// Use for cleanup, saving state, releasing resources.
    /// </summary>
    void Shutdown();

    #endregion

    #region File Handling

    /// <summary>
    /// Check if this module can handle a specific file type.
    /// Used for file associations and drag-drop handling.
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <returns>True if module can open this file type</returns>
    bool CanHandleFile(string filePath);

    /// <summary>
    /// Open a file directly in this module.
    /// Called after CanHandleFile returns true.
    /// </summary>
    /// <param name="filePath">Full path to the file to open</param>
    void OpenFile(string filePath);

    #endregion
}
```

---

## Module Properties

### ModuleId

The unique identifier for the module.

| Requirement | Description |
|-------------|-------------|
| Format | PascalCase, no spaces |
| Uniqueness | Must be unique across all modules |
| Convention | Should match DLL name suffix |

**Examples:**
```csharp
public string ModuleId => "SurveyListing";
public string ModuleId => "GnssCalibration";
public string ModuleId => "NetworkTimeSync";
```

### DisplayName

The human-readable name shown on the dashboard.

| Requirement | Description |
|-------------|-------------|
| Format | Title Case with spaces |
| Length | Recommended under 30 characters |
| Content | Descriptive but concise |

**Examples:**
```csharp
public string DisplayName => "Survey Listing Generator";
public string DisplayName => "GNSS Calibration";
public string DisplayName => "Network Time Sync";
```

### Description

A brief description of the module's purpose.

| Requirement | Description |
|-------------|-------------|
| Length | 1-2 sentences |
| Content | Describes what the module does |
| Usage | Shown in tooltips |

**Examples:**
```csharp
public string Description =>
    "Generate survey listings from NPD data with route alignment and tide corrections.";

public string Description =>
    "Compare and validate GNSS positioning systems with statistical analysis.";
```

### Version

The module version using .NET `System.Version`.

```csharp
public Version Version => new Version(1, 0, 0);
public Version Version => new Version(2, 5, 3);
```

Or reference a shared version:
```csharp
public Version Version => AppInfo.Version;
```

### IconResource

Pack URI to the module icon resource.

| Requirement | Description |
|-------------|-------------|
| Format | Pack URI syntax |
| Size | 32x32 or 64x64 PNG recommended |
| Build Action | Must be set to "Resource" |

**Format:**
```csharp
public string IconResource =>
    "/FathomOS.Modules.{ModuleName};component/Assets/icon.png";
```

**Examples:**
```csharp
public string IconResource =>
    "/FathomOS.Modules.SurveyListing;component/Assets/icon.png";
```

### Category

The category for dashboard grouping.

| Standard Category | Description |
|-------------------|-------------|
| Data Processing | Data manipulation and export |
| Quality Control | Verification and validation |
| Calibration & Verification | Sensor calibration |
| Calibrations | Grouped calibrations |
| Utilities | General tools |
| Operations | Business operations |

### DisplayOrder

Controls the order of modules on the dashboard.

| Value Range | Description |
|-------------|-------------|
| 1-20 | Primary/featured modules |
| 21-40 | Secondary modules |
| 41-60 | Utility modules |
| 61+ | Administrative modules |

Use increments of 5-10 to allow insertion of new modules.

---

## Module Methods

### Initialize()

Called once when the module is first loaded.

**Purpose:**
- Load saved settings
- Register services
- Subscribe to events
- Pre-load resources

**Implementation:**
```csharp
public void Initialize()
{
    System.Diagnostics.Debug.WriteLine($"{DisplayName} v{Version} initialized");

    // Subscribe to theme changes
    if (_themeService != null)
    {
        _themeService.ThemeChanged += OnThemeChanged;
    }

    // Load settings
    LoadSettings();
}
```

### Launch(Window? owner)

Called when the user clicks the module tile.

**Parameters:**
- `owner`: Optional parent window for positioning/modality

**Implementation:**
```csharp
public void Launch(Window? owner = null)
{
    try
    {
        // Create or reuse window
        if (_mainWindow == null || !_mainWindow.IsLoaded)
        {
            _mainWindow = new MainWindow();

            if (owner != null)
            {
                _mainWindow.Owner = owner;
            }
        }

        // Show and activate
        _mainWindow.Show();
        _mainWindow.Activate();

        // Restore from minimized
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Failed to launch {DisplayName}: {ex.Message}",
            "Module Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Shutdown()

Called when the application is closing.

**Purpose:**
- Unsubscribe from events
- Save state
- Close windows
- Release resources

**Implementation:**
```csharp
public void Shutdown()
{
    try
    {
        // Unsubscribe from events
        if (_themeService != null)
        {
            _themeService.ThemeChanged -= OnThemeChanged;
        }

        // Save settings
        SaveSettings();

        // Close windows
        _mainWindow?.Close();
        _mainWindow = null;

        // Dispose resources
        _disposableResource?.Dispose();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
    }
}
```

### CanHandleFile(string filePath)

Determines if the module can open a specific file.

**Implementation:**
```csharp
public bool CanHandleFile(string filePath)
{
    if (string.IsNullOrEmpty(filePath))
        return false;

    var ext = Path.GetExtension(filePath).ToLowerInvariant();

    return ext switch
    {
        ".npd" => true,
        ".rlx" => true,
        ".s7p" => true,
        ".tide" => true,
        _ => false
    };
}
```

Or using pattern matching:
```csharp
public bool CanHandleFile(string filePath)
{
    if (string.IsNullOrEmpty(filePath)) return false;
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    return ext is ".npd" or ".csv" or ".txt";
}
```

### OpenFile(string filePath)

Opens a file in the module.

**Implementation:**
```csharp
public void OpenFile(string filePath)
{
    // Ensure module is launched
    Launch();

    // Load the file based on type
    if (_mainWindow != null && File.Exists(filePath))
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        switch (ext)
        {
            case ".s7p":
                _mainWindow.LoadProject(filePath);
                break;
            case ".npd":
                _mainWindow.LoadSurveyFile(filePath);
                break;
            case ".rlx":
                _mainWindow.LoadRouteFile(filePath);
                break;
        }
    }
}
```

---

## Module Discovery

### Discovery Process

The Shell automatically discovers modules through MSBuild wildcards:

```xml
<!-- Regular modules -->
<DiscoveredModules Include="..\FathomOS.Modules.*\FathomOS.Modules.*.csproj" />

<!-- Grouped modules -->
<DiscoveredModules Include="..\FathomOS.ModuleGroups.*\FathomOS.Modules.*\FathomOS.Modules.*.csproj" />
```

### Naming Requirements

| Requirement | Pattern |
|-------------|---------|
| Project folder | `FathomOS.Modules.{Name}` |
| Assembly name | `FathomOS.Modules.{Name}` |
| Namespace | `FathomOS.Modules.{Name}` |
| Module class | `{Name}Module` |

### Runtime Discovery

At startup, the Shell:

1. Scans all loaded assemblies
2. Finds types implementing `IModule`
3. Creates instances using DI or default constructor
4. Calls `Initialize()` on each module
5. Creates dashboard tiles based on metadata

---

## Module Groups

### Structure

Grouped modules are organized under a shared parent folder:

```
FathomOS.ModuleGroups.Calibrations/
├── FathomOS.Modules.GnssCalibration/
├── FathomOS.Modules.MruCalibration/
├── FathomOS.Modules.UsblVerification/
├── FathomOS.Modules.TreeInclination/
├── FathomOS.Modules.RovGyroCalibration/
└── FathomOS.Modules.VesselGyroCalibration/
```

### Dashboard Behavior

- Group appears as a single expandable tile
- Group name derived from folder (e.g., "Calibrations")
- Clicking expands to show contained modules
- Each module within has its own tile

---

## Best Practices

### 1. Constructor Pattern

Always provide both constructors:

```csharp
// Default for discovery (required)
public MyModule() { }

// DI constructor for full functionality
public MyModule(
    IAuthenticationService authService,
    ICertificationService certService,
    IEventAggregator eventAggregator,
    IThemeService themeService,
    IErrorReporter errorReporter)
{
    // Store references
}
```

### 2. Null Safety

Services may be null if DI constructor wasn't used:

```csharp
// Always check for null
if (_themeService != null)
{
    _themeService.ThemeChanged += OnThemeChanged;
}

// Use null-conditional operator
_errorReporter?.Report(ModuleId, "Error", ex);
```

### 3. Window Management

Reuse windows instead of recreating:

```csharp
if (_mainWindow == null || !_mainWindow.IsLoaded)
{
    _mainWindow = new MainWindow();
}
```

### 4. Event Cleanup

Always unsubscribe in Shutdown:

```csharp
public void Shutdown()
{
    if (_themeService != null)
    {
        _themeService.ThemeChanged -= OnThemeChanged;
    }
}
```

### 5. Error Handling

Wrap Launch in try-catch:

```csharp
public void Launch(Window? owner = null)
{
    try
    {
        // Launch code
    }
    catch (Exception ex)
    {
        _errorReporter?.Report(ModuleId, "Launch failed", ex);
        MessageBox.Show($"Failed to launch: {ex.Message}");
    }
}
```

---

## Related Documentation

- [Developer Guide](../DeveloperGuide.md) - Creating modules
- [Core API](Core-API.md) - Core library reference
- [Shell API](Shell-API.md) - Shell services
- [Architecture](../Architecture.md) - System architecture

---

*Copyright 2026 Fathom OS. All rights reserved.*
