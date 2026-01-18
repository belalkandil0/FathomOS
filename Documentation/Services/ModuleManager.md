# Module Manager Documentation

**Version:** 1.0.0
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Discovery Mechanism](#discovery-mechanism)
3. [Module Loading](#module-loading)
4. [Module Groups](#module-groups)
5. [Dependency Injection](#dependency-injection)
6. [Dashboard Integration](#dashboard-integration)
7. [File Associations](#file-associations)

---

## Overview

The Module Manager is responsible for discovering, loading, initializing, and managing the lifecycle of all FathomOS modules. It provides the infrastructure for the plugin architecture that allows modules to be developed and deployed independently.

### Key Responsibilities

- Discover modules from assemblies
- Instantiate module classes with DI
- Manage module lifecycle (Initialize, Launch, Shutdown)
- Create dashboard tiles for modules
- Route file opens to appropriate modules
- Handle module groups

---

## Discovery Mechanism

### Build-Time Discovery

The Shell project automatically references all modules using MSBuild wildcards:

```xml
<!-- In FathomOS.Shell.csproj -->

<!-- Regular modules at solution root -->
<ItemGroup>
  <DiscoveredModules Include="..\FathomOS.Modules.*\FathomOS.Modules.*.csproj" />
</ItemGroup>

<!-- Grouped modules inside ModuleGroups folders -->
<ItemGroup>
  <DiscoveredModules Include="..\FathomOS.ModuleGroups.*\FathomOS.Modules.*\FathomOS.Modules.*.csproj" />
</ItemGroup>

<!-- Reference all discovered modules -->
<ItemGroup>
  <ProjectReference Include="@(DiscoveredModules)" />
</ItemGroup>
```

### Post-Build Deployment

A post-build script copies module assemblies to the Modules folder:

```powershell
# deploy-modules.ps1
$modulesDir = "$PSScriptRoot\bin\$Configuration\net8.0-windows\Modules"

# Copy regular modules
Get-ChildItem -Path "$PSScriptRoot\.." -Filter "FathomOS.Modules.*" -Directory |
    ForEach-Object { Copy-ModuleFiles $_.FullName $modulesDir }

# Copy grouped modules
Get-ChildItem -Path "$PSScriptRoot\..\FathomOS.ModuleGroups.*" -Directory |
    Get-ChildItem -Filter "FathomOS.Modules.*" -Directory |
    ForEach-Object { Copy-ModuleFiles $_.FullName $modulesDir }
```

### Runtime Discovery

At startup, the Module Manager:

1. Scans loaded assemblies for IModule implementations
2. Filters to types matching naming convention
3. Creates instances of each module
4. Calls Initialize() on each module
5. Registers modules for dashboard display

```csharp
// Simplified discovery logic
var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .Where(t => typeof(IModule).IsAssignableFrom(t))
    .Where(t => !t.IsInterface && !t.IsAbstract)
    .Where(t => t.Namespace?.StartsWith("FathomOS.Modules.") == true);

foreach (var moduleType in moduleTypes)
{
    var module = CreateModuleInstance(moduleType);
    module.Initialize();
    _loadedModules.Add(module);
}
```

---

## Module Loading

### Instantiation Process

1. **Try DI Constructor** - Attempt to create with all services
2. **Fallback to Default** - Use parameterless constructor if DI fails
3. **Initialize** - Call Initialize() method
4. **Register** - Add to loaded modules collection

```csharp
private IModule CreateModuleInstance(Type moduleType)
{
    // Try DI constructor first
    var diConstructor = moduleType.GetConstructors()
        .FirstOrDefault(c => c.GetParameters().Length > 0 &&
            c.GetParameters().All(p => IsServiceType(p.ParameterType)));

    if (diConstructor != null)
    {
        var parameters = diConstructor.GetParameters()
            .Select(p => ResolveService(p.ParameterType))
            .ToArray();

        return (IModule)diConstructor.Invoke(parameters);
    }

    // Fallback to default constructor
    return (IModule)Activator.CreateInstance(moduleType)!;
}
```

### Service Resolution

Services resolved for DI constructors:

| Service Type | Instance |
|--------------|----------|
| IAuthenticationService | AuthenticationService |
| ICertificationService | CertificationService |
| IEventAggregator | EventAggregator |
| IThemeService | ThemeService |
| IErrorReporter | ErrorReporter |

---

## Module Groups

### Group Structure

Module groups organize related modules under a single expandable tile:

```
FathomOS.ModuleGroups.Calibrations/
├── FathomOS.Modules.GnssCalibration/
├── FathomOS.Modules.MruCalibration/
├── FathomOS.Modules.UsblVerification/
├── FathomOS.Modules.TreeInclination/
├── FathomOS.Modules.RovGyroCalibration/
└── FathomOS.Modules.VesselGyroCalibration/
```

### Group Detection

Groups are detected by assembly metadata:

```csharp
private string? GetModuleGroup(Assembly assembly)
{
    var location = assembly.Location;

    // Check if in a ModuleGroups folder
    var groupMatch = Regex.Match(location,
        @"FathomOS\.ModuleGroups\.(\w+)");

    return groupMatch.Success ? groupMatch.Groups[1].Value : null;
}
```

### Dashboard Behavior

- Group tile shows group name (e.g., "Calibrations")
- Click expands to show contained modules
- Each module has its own sub-tile
- Group icon shows count of modules

---

## Dependency Injection

### Module Constructor Pattern

Modules should provide two constructors:

```csharp
public class MyModule : IModule
{
    private readonly IThemeService? _themeService;
    // ... other services

    // Required: Default constructor for discovery
    public MyModule()
    {
    }

    // Optional: DI constructor for full functionality
    public MyModule(
        IAuthenticationService authService,
        ICertificationService certService,
        IEventAggregator eventAggregator,
        IThemeService themeService,
        IErrorReporter errorReporter)
    {
        _authService = authService;
        _certService = certService;
        _eventAggregator = eventAggregator;
        _themeService = themeService;
        _errorReporter = errorReporter;
    }
}
```

### Service Availability

- Services may be null if DI constructor wasn't used
- Always use null-conditional operators
- Provide fallback behavior when services unavailable

```csharp
// Safe service usage
_themeService?.ApplyTheme(AppTheme.Dark);
_errorReporter?.Report(ModuleId, "Error occurred", ex);
```

---

## Dashboard Integration

### Tile Creation

For each loaded module, a dashboard tile is created:

```csharp
private void CreateModuleTile(IModule module)
{
    var tile = new ModuleTile
    {
        Title = module.DisplayName,
        Description = module.Description,
        Icon = LoadIcon(module.IconResource),
        Category = module.Category,
        DisplayOrder = module.DisplayOrder
    };

    tile.Click += (s, e) => module.Launch(MainWindow);

    _dashboardTiles.Add(tile);
}
```

### Tile Properties

| Property | Source | Description |
|----------|--------|-------------|
| Title | DisplayName | Shown on tile |
| Description | Description | Tooltip text |
| Icon | IconResource | Tile icon |
| Category | Category | Grouping header |
| Order | DisplayOrder | Sort position |

### Category Grouping

Tiles are grouped by category on the dashboard:

```
DATA PROCESSING
  [Survey Listing] [Survey Logbook] [Sound Velocity]

CALIBRATIONS
  [GNSS] [MRU] [USBL] [Tree] [ROV Gyro] [Vessel Gyro]

UTILITIES
  [Time Sync] [Equipment Inventory]

OPERATIONS
  [Personnel] [Projects]
```

---

## File Associations

### Opening Files

When a file is opened (drag-drop, double-click, etc.):

1. Query each module with CanHandleFile()
2. First module returning true handles the file
3. Call OpenFile() on that module

```csharp
public void OpenFile(string filePath)
{
    foreach (var module in _loadedModules)
    {
        if (module.CanHandleFile(filePath))
        {
            module.OpenFile(filePath);
            return;
        }
    }

    // No module can handle this file type
    ShowUnsupportedFileDialog(filePath);
}
```

### File Type Registration

Modules register their supported file types:

```csharp
// In module implementation
public bool CanHandleFile(string filePath)
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    return ext is ".npd" or ".rlx" or ".s7p" or ".tide";
}
```

### Priority Handling

If multiple modules can handle a file, the first match wins. Order is determined by module load order (generally alphabetical by ModuleId).

---

## Lifecycle Management

### Initialization Order

1. Shell starts
2. Services registered
3. Modules discovered
4. Modules instantiated (with DI)
5. Initialize() called on each
6. Dashboard populated
7. User interaction begins

### Shutdown Order

1. User closes application
2. Shutdown() called on each module (reverse order)
3. Modules save state
4. Windows closed
5. Services disposed
6. Application exits

### Example Lifecycle

```csharp
// Module Manager calls these in sequence:

// Startup
foreach (var module in _loadedModules)
{
    module.Initialize();
}

// User clicks tile
selectedModule.Launch(MainWindow);

// Application closing
foreach (var module in _loadedModules.Reverse())
{
    module.Shutdown();
}
```

---

## Troubleshooting

### Module Not Appearing

1. Verify project name matches pattern: `FathomOS.Modules.*`
2. Check module class implements `IModule`
3. Verify namespace starts with `FathomOS.Modules.`
4. Check build output for errors
5. Ensure assembly is in Modules folder

### Module Load Failure

1. Check Output window for exceptions
2. Verify all dependencies are present
3. Check constructor doesn't throw
4. Verify Initialize() completes

### DI Constructor Not Used

1. Verify parameter types match service interfaces exactly
2. Check all required services are registered
3. Add parameterless constructor as fallback

---

## Related Documentation

- [Module API](../API/Module-API.md)
- [Developer Guide](../DeveloperGuide.md)
- [Architecture](../Architecture.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
