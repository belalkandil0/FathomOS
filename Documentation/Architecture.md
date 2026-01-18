# FathomOS Architecture

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Clean Architecture Layers](#clean-architecture-layers)
3. [Solution Structure](#solution-structure)
4. [Core Components](#core-components)
5. [Module Architecture](#module-architecture)
6. [Service Layer](#service-layer)
7. [CQRS Pattern](#cqrs-pattern)
8. [Data Flow](#data-flow)
9. [Dependency Injection](#dependency-injection)
10. [Module Discovery](#module-discovery)
11. [Technology Stack](#technology-stack)

---

## Overview

FathomOS follows a modular, plugin-based architecture that allows independent development and deployment of functional modules while sharing common infrastructure services.

### Architectural Principles

1. **Modularity**: Each functional area is implemented as an independent module
2. **Separation of Concerns**: Clear boundaries between Shell, Core, and Modules
3. **Dependency Inversion**: Modules depend on abstractions, not concrete implementations
4. **Single Responsibility**: Each component has a well-defined purpose
5. **Open/Closed**: System is extensible without modifying existing code

### High-Level Architecture

```
+----------------------------------------------------------+
|                    FathomOS.Shell                         |
|  +------------------+  +------------------+               |
|  | Module Manager   |  | License Manager  |               |
|  +------------------+  +------------------+               |
|  +------------------+  +------------------+               |
|  | Theme Service    |  | Certificate Svc  |               |
|  +------------------+  +------------------+               |
+----------------------------------------------------------+
                            |
                            v
+----------------------------------------------------------+
|                    FathomOS.Core                          |
|  +------------+  +------------+  +------------+          |
|  | Models     |  | Parsers    |  | Exporters  |          |
|  +------------+  +------------+  +------------+          |
|  +------------+  +------------+  +------------+          |
|  | Services   |  | Interfaces |  | Utilities  |          |
|  +------------+  +------------+  +------------+          |
+----------------------------------------------------------+
                            |
        +-------------------+-------------------+
        |                   |                   |
        v                   v                   v
+----------------+  +----------------+  +----------------+
| Module A       |  | Module B       |  | Module C       |
| (Survey List)  |  | (Calibration)  |  | (Utilities)    |
+----------------+  +----------------+  +----------------+
```

---

## Clean Architecture Layers

FathomOS follows Uncle Bob's Clean Architecture principles with dependency rules flowing inward from external layers to core business logic.

### Layer Diagram

```
+=========================================================================+
|                        PRESENTATION LAYER                                |
|  +-------------------------------------------------------------------+  |
|  |              WPF Views, XAML, ViewModels (MVVM)                   |  |
|  |     FathomOS.Shell/Views   |   FathomOS.Modules.*/Views          |  |
|  +-------------------------------------------------------------------+  |
+=========================================================================+
                                    |
                                    | depends on
                                    v
+=========================================================================+
|                        APPLICATION LAYER                                 |
|  +-------------------------------------------------------------------+  |
|  |            Services, Commands, Queries, DTOs                      |  |
|  |   FathomOS.Core/Services   |   Module-specific Services          |  |
|  +-------------------------------------------------------------------+  |
+=========================================================================+
                                    |
                                    | depends on
                                    v
+=========================================================================+
|                          DOMAIN LAYER                                    |
|  +-------------------------------------------------------------------+  |
|  |      Entities, Value Objects, Domain Services, Interfaces         |  |
|  |  FathomOS.Core/Models  |  FathomOS.Core/Interfaces               |  |
|  +-------------------------------------------------------------------+  |
+=========================================================================+
                                    |
                                    | depends on
                                    v
+=========================================================================+
|                       INFRASTRUCTURE LAYER                               |
|  +-------------------------------------------------------------------+  |
|  |    Parsers, Exporters, Data Access, External Services             |  |
|  |  FathomOS.Core/Parsers  |  FathomOS.Core/Export  |  Licensing    |  |
|  +-------------------------------------------------------------------+  |
+=========================================================================+
```

### Layer Responsibilities

| Layer | Responsibility | FathomOS Components |
|-------|---------------|---------------------|
| **Presentation** | User interface, input handling, data binding | WPF Views, ViewModels, Themes |
| **Application** | Use case orchestration, service coordination | Services (Smoothing, Cache, AutoSave) |
| **Domain** | Business entities, rules, interfaces | Models (SurveyPoint, RouteData, TideData) |
| **Infrastructure** | External concerns, I/O, data persistence | Parsers, Exporters, LicenseManager |

### Dependency Rules

1. Inner layers know nothing about outer layers
2. All dependencies point inward
3. Domain layer has no external dependencies
4. Interfaces defined in Domain, implemented in Infrastructure

---

## Solution Structure

```
FathomOS/
├── FathomOS.Shell/                    # Main application host
│   ├── Views/                         # Shell UI components
│   ├── Security/                      # Anti-debug, protection
│   └── deploy-modules.ps1             # Module deployment script
│
├── FathomOS.Core/                     # Shared core library
│   ├── Interfaces/                    # IModule, service contracts
│   ├── Models/                        # Common data models
│   ├── Parsers/                       # File format parsers
│   ├── Calculations/                  # Mathematical operations
│   ├── Export/                        # Export functionality
│   ├── Services/                      # Shared services
│   └── Certificates/                  # Certificate helpers
│
├── FathomOS.Modules.SurveyListing/    # Survey listing module
├── FathomOS.Modules.SurveyLogbook/    # Survey logbook module
├── FathomOS.Modules.SoundVelocity/    # Sound velocity module
├── FathomOS.Modules.NetworkTimeSync/  # Time sync module
├── FathomOS.Modules.EquipmentInventory/
├── FathomOS.Modules.PersonnelManagement/
├── FathomOS.Modules.ProjectManagement/
│
├── FathomOS.ModuleGroups.Calibrations/ # Grouped calibration modules
│   ├── FathomOS.Modules.GnssCalibration/
│   ├── FathomOS.Modules.MruCalibration/
│   ├── FathomOS.Modules.UsblVerification/
│   ├── FathomOS.Modules.TreeInclination/
│   ├── FathomOS.Modules.RovGyroCalibration/
│   └── FathomOS.Modules.VesselGyroCalibration/
│
├── LicensingSystem.Client/            # License validation
├── LicensingSystem.Shared/            # License models
└── FathomOS.Tests/                    # Unit tests
```

---

## Core Components

### FathomOS.Shell

The Shell is the main executable application that provides:

| Component | Responsibility |
|-----------|----------------|
| Main Window | Dashboard with module tiles |
| Module Manager | Discovery, loading, lifecycle |
| License Manager | Activation, validation, branding |
| Theme Service | Theme switching, persistence |
| Certificate UI | Viewer, list, signatory dialog |
| Event Aggregator | Cross-module messaging |

### FathomOS.Core

The Core library provides shared functionality:

#### Models

```csharp
// Key model classes
Project           // Project metadata and settings
SurveyPoint       // Individual survey data point
RouteData         // Route alignment information
TideData          // Tide correction data
ColumnMapping     // Data column configuration
```

#### Parsers

```csharp
NpdParser         // NaviPac NPD file parser
RlxParser         // Route alignment parser
TideParser        // Tide file parser
DxfLayoutParser   // DXF layout extraction
```

#### Calculators

```csharp
DepthCalculator   // Seabed depth calculations
KpCalculator      // Kilometer point calculations
UnitConverter     // Unit conversions
TideCorrector     // Tide correction application
```

#### Exporters

```csharp
DxfExporter       // CAD DXF export
ExcelExporter     // Excel spreadsheet export
TextExporter      // Text/CSV export
PdfReportGenerator // PDF report generation
CadScriptExporter  // AutoCAD script export
```

---

## Module Architecture

### IModule Interface

All modules implement the `IModule` interface:

```csharp
public interface IModule
{
    // Identity
    string ModuleId { get; }
    string DisplayName { get; }
    string Description { get; }
    Version Version { get; }
    string IconResource { get; }
    string Category { get; }
    int DisplayOrder { get; }

    // Lifecycle
    void Initialize();
    void Launch(Window? owner = null);
    void Shutdown();

    // File Handling
    bool CanHandleFile(string filePath);
    void OpenFile(string filePath);
}
```

### Module Structure

Each module follows a consistent structure:

```
FathomOS.Modules.ModuleName/
├── ModuleNameModule.cs          # IModule implementation
├── Views/
│   ├── MainWindow.xaml(.cs)     # Main module window
│   └── [Other windows/dialogs]
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── ViewModelBase.cs
│   └── RelayCommand.cs
├── Models/
│   └── [Module-specific models]
├── Services/
│   ├── ThemeService.cs
│   └── [Module-specific services]
├── Converters/
│   └── Converters.cs
├── Parsers/                      # Optional
├── Export/                       # Optional
├── Assets/
│   └── icon.png
└── Themes/
    ├── LightTheme.xaml
    ├── DarkTheme.xaml
    ├── ModernTheme.xaml
    └── GradientTheme.xaml
```

### Module Categories

Modules are organized by category for dashboard grouping:

| Category | Description |
|----------|-------------|
| Data Processing | Survey data manipulation |
| Quality Control | Verification and validation |
| Calibration & Verification | Sensor calibration |
| Calibrations | Grouped calibration modules |
| Utilities | General tools |
| Operations | Business operations |

---

## Service Layer

### Core Services

Services provided by the Shell to modules:

```csharp
// Authentication
IAuthenticationService
    bool IsAuthenticated { get; }
    IUser? CurrentUser { get; }
    Task<bool> ShowLoginDialogAsync(Window? owner);
    bool HasPermission(string permission);
    bool HasRole(params string[] roles);
    event EventHandler<IUser?> AuthenticationChanged;

// Theme Management
IThemeService
    AppTheme CurrentTheme { get; set; }
    void ApplyTheme(AppTheme theme);
    event EventHandler<AppTheme> ThemeChanged;

// Certification
ICertificationService
    Task<ProcessingCertificate> CreateCertificateAsync(...);
    void ViewCertificate(ProcessingCertificate cert);

// Event Messaging
IEventAggregator
    void Publish<TEvent>(TEvent eventData);
    void Subscribe<TEvent>(Action<TEvent> handler);

// Error Reporting
IErrorReporter
    void Report(string module, string message, Exception ex);
```

### Dependency Injection

Modules receive services through constructor injection:

```csharp
public class MyModule : IModule
{
    private readonly IAuthenticationService? _authService;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;

    // Default constructor for discovery
    public MyModule() { }

    // DI constructor for full functionality
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

---

## CQRS Pattern

FathomOS uses a lightweight CQRS (Command Query Responsibility Segregation) pattern for complex operations, separating read and write operations for better scalability and maintainability.

### Pattern Overview

```
+------------------+                      +------------------+
|     Commands     |                      |     Queries      |
|  (Write Ops)     |                      |   (Read Ops)     |
+--------+---------+                      +--------+---------+
         |                                         |
         v                                         v
+--------+---------+                      +--------+---------+
| Command Handler  |                      |  Query Handler   |
|  - Validation    |                      |  - Data Access   |
|  - Business Logic|                      |  - Projection    |
|  - State Change  |                      |  - Caching       |
+--------+---------+                      +--------+---------+
         |                                         |
         v                                         v
+--------+---------+                      +--------+---------+
|  Command Result  |                      |   Query Result   |
|  - Success/Fail  |                      |  - Data DTOs     |
|  - Validation    |                      |  - Collections   |
+------------------+                      +------------------+
```

### Commands (Write Operations)

Commands modify state and return results indicating success or failure.

```csharp
// Command Definition
public class ProcessSurveyCommand
{
    public List<SurveyPoint> Points { get; set; }
    public SmoothingOptions SmoothingOptions { get; set; }
    public TideCorrectionOptions TideOptions { get; set; }
    public RouteAlignmentOptions RouteOptions { get; set; }
}

// Command Result
public class ProcessSurveyResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<SurveyPoint> ProcessedPoints { get; set; }
    public SmoothingResult SmoothingStats { get; set; }
    public int PointsModified { get; set; }
}
```

### Queries (Read Operations)

Queries retrieve data without causing side effects.

```csharp
// Query Interface
public interface IProjectQuery
{
    Task<Project?> GetByIdAsync(string projectId);
    Task<IEnumerable<Project>> GetRecentAsync(int count);
    Task<IEnumerable<Project>> SearchAsync(string searchTerm);
}

// Query Implementation
public class ProjectQuery : IProjectQuery
{
    private readonly ICacheService _cache;

    public async Task<Project?> GetByIdAsync(string projectId)
    {
        return await _cache.GetOrCreateAsync(
            $"project:{projectId}",
            () => LoadProjectFromDisk(projectId),
            TimeSpan.FromMinutes(5));
    }
}
```

### Command vs Query Comparison

| Aspect | Commands | Queries |
|--------|----------|---------|
| **Purpose** | Modify state | Read data |
| **Returns** | Result/Status | Data |
| **Caching** | No (invalidates cache) | Yes (uses cache) |
| **Validation** | Full validation required | Minimal validation |
| **Side Effects** | Yes | No |
| **Idempotency** | Often non-idempotent | Always idempotent |

### Benefits in FathomOS

1. **Separation of Concerns**: Read and write paths are optimized independently
2. **Testability**: Commands and queries can be unit tested in isolation
3. **Scalability**: Read-heavy operations (viewing data) don't block write operations (processing)
4. **Caching**: Query results can be aggressively cached without worrying about stale data

---

## Data Flow

### Survey Processing Pipeline

```
[Input Files] --> [Parsers] --> [Models] --> [Calculators] --> [Exporters] --> [Output]

NPD File ──────┐
               ├──> NpdParser ──> SurveyPoints ──┐
               │                                  │
RLX File ──────┼──> RlxParser ──> RouteData ─────┼──> KpCalculator
               │                                  │       │
Tide File ─────┼──> TideParser ──> TideData ─────┼──> TideCorrector
               │                                  │       │
               └──────────────────────────────────┼──> DepthCalculator
                                                  │       │
                                                  v       v
                                           ProcessedPoints
                                                  │
                    ┌─────────────────────────────┼─────────────────┐
                    │                             │                 │
                    v                             v                 v
              DxfExporter               ExcelExporter        PdfReportGenerator
                    │                             │                 │
                    v                             v                 v
              [DXF File]                   [Excel File]       [PDF Report]
```

### Event Flow

```
[Module A] ──publish──> [Event Aggregator] ──notify──> [Module B]
                                │
                                └──notify──> [Module C]
```

---

## Dependency Injection

### Service Registration

The Shell registers services at startup:

```csharp
services.AddSingleton<IThemeService, ThemeService>();
services.AddSingleton<IEventAggregator, EventAggregator>();
services.AddSingleton<ICertificationService, CertificationService>();
services.AddSingleton<IAuthenticationService, AuthenticationService>();
services.AddSingleton<IErrorReporter, ErrorReporter>();
services.AddSingleton<LicenseManager>();
```

### Module Resolution

When a module is instantiated:

1. Try DI constructor with all services
2. Fall back to default constructor if services unavailable
3. Call `Initialize()` after construction

---

## Module Discovery

### Discovery Mechanism

The Shell discovers modules automatically using MSBuild wildcards:

```xml
<!-- In FathomOS.Shell.csproj -->

<!-- Regular modules at solution root -->
<DiscoveredModules Include="..\FathomOS.Modules.*\FathomOS.Modules.*.csproj" />

<!-- Grouped modules inside ModuleGroups folders -->
<DiscoveredModules Include="..\FathomOS.ModuleGroups.*\FathomOS.Modules.*\FathomOS.Modules.*.csproj" />

<!-- Reference all discovered modules -->
<ProjectReference Include="@(DiscoveredModules)" />
```

### Discovery Process

1. Shell builds with module references
2. Post-build script copies modules to `bin/Modules/`
3. At runtime, Shell scans assemblies for `IModule` implementations
4. Modules are instantiated and initialized
5. Dashboard tiles are created based on module metadata

### Module Groups

Grouped modules appear under a single expandable tile:

```
FathomOS.ModuleGroups.Calibrations/
├── FathomOS.Modules.GnssCalibration/
├── FathomOS.Modules.MruCalibration/
└── ...
```

The group name is derived from the folder name (e.g., "Calibrations").

---

## Technology Stack

### Core Technologies

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 LTS | Runtime framework |
| WPF | 8.0 | Windows UI framework |
| C# | 12.0 | Programming language |

### NuGet Packages by Category

#### UI & Presentation
| Package | Version | Purpose |
|---------|---------|---------|
| MahApps.Metro | 2.x | Modern Metro-style UI |
| ControlzEx | 5.x | UI behaviors and controls |
| HelixToolkit.Wpf | 2.x | 3D visualization |
| OxyPlot.Wpf | 2.x | Charting and graphs |

#### Data Processing
| Package | Version | Purpose |
|---------|---------|---------|
| MathNet.Numerics | 5.x | Statistics and numerics |
| Microsoft.Data.Sqlite | 8.x | Local database storage |

#### Export & Reporting
| Package | Version | Purpose |
|---------|---------|---------|
| ClosedXML | 0.102+ | Excel file generation |
| netDxf | 3.x | DXF/CAD file export |
| QuestPDF | 2024.x | PDF report generation |

#### Security & Licensing
| Package | Version | Purpose |
|---------|---------|---------|
| System.Security.Cryptography | built-in | ECDSA license signing |

### Development Tools

| Tool | Purpose |
|------|---------|
| Visual Studio 2022 | Primary IDE |
| .NET 8.0 SDK | Build toolchain |
| MSBuild | Build automation |

---

## Related Documentation

- [Module API](API/Module-API.md) - IModule interface details
- [Core API](API/Core-API.md) - Core library reference
- [Developer Guide](DeveloperGuide.md) - Creating modules

---

*Copyright 2026 Fathom OS. All rights reserved.*
