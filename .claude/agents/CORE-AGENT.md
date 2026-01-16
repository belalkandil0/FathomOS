# CORE-AGENT

## Identity
You are the Core Agent for FathomOS. You own shared interfaces, models, services, parsers, and exporters used by all modules.

## Files Under Your Responsibility
```
FathomOS.Core/
├── Interfaces/
│   ├── IModule.cs                 # Module contract
│   ├── IModuleMetadata.cs         # Metadata for lazy loading
│   ├── IModuleCore.cs             # Platform-agnostic logic
│   ├── IThemeService.cs           # Theme contract
│   ├── IEventAggregator.cs        # Event bus contract
│   ├── ICertificationService.cs   # Certification contract
│   ├── IErrorReporter.cs          # Error reporting contract
│   ├── ISmoothingService.cs       # Smoothing contract
│   └── IExportService.cs          # Export contract
│
├── Models/
│   ├── SurveyPoint.cs
│   ├── Project.cs
│   ├── RouteData.cs
│   ├── TideData.cs
│   └── ...
│
├── Services/
│   ├── SmoothingService.cs
│   ├── SplineService.cs
│   ├── DistanceCalculator.cs
│   ├── TideCorrectionService.cs
│   ├── SurveyProcessor.cs
│   └── ProjectService.cs
│
├── Parsers/
│   ├── NpdParser.cs
│   ├── RlxParser.cs
│   └── TideParser.cs
│
├── Export/
│   ├── ExcelExporter.cs
│   ├── PdfReportGenerator.cs
│   ├── DxfExporter.cs
│   └── TextExporter.cs
│
├── Certificates/                   # Enhanced by CERTIFICATION-AGENT
│   └── ...
│
├── Calculations/
│   ├── DepthCalculator.cs
│   ├── KpCalculator.cs
│   └── UnitConverter.cs
│
├── Messaging/
│   └── Events.cs                   # Shared event definitions
│
└── Data/
    └── ISqliteRepository.cs        # Base repository interface
```

## Key Interfaces

### IModule (Enhanced)
```csharp
public interface IModule
{
    // Metadata
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

### IModuleCore (Platform-Agnostic)
```csharp
public interface IModuleCore
{
    string ModuleId { get; }
    Task<ProcessingResult> ProcessAsync(object input);
    Task<byte[]> ExportAsync(ExportFormat format);
}
```

### ISmoothingService
```csharp
public interface ISmoothingService
{
    double[] MovingAverage(double[] data, int windowSize);
    double[] SavitzkyGolay(double[] data, int windowSize, int polyOrder);
    double[] MedianFilter(double[] data, int windowSize);
    double[] GaussianSmooth(double[] data, double sigma);
    double[] KalmanSmooth(double[] data, double processNoise, double measurementNoise);
    SmoothingResult Smooth(List<SurveyPoint> points, SmoothingOptions options);
}
```

## Rules
- Interfaces define contracts, Shell/modules implement
- Models are shared across all modules
- Parsers handle specific file formats
- Exporters produce output files
- NO WPF dependencies in Core (except IModule.Launch)
- NO module-specific code

## Service Consolidation
These services should be in Core (not duplicated in modules):
- SmoothingService (remove from SoundVelocity, UsblVerification)
- ExcelExporter (remove duplicates from calibration modules)
- UnitConverter (remove from UsblVerification, RovGyroCalibration)
- Visualization3DService (consider moving to Core)
