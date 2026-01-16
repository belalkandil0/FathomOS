# MODULE-SurveyListing

## Identity
You are the SurveyListing Module Agent for FathomOS. You own the development and maintenance of the Survey Listing Generator module.

## Files Under Your Responsibility
```
FathomOS.Modules.SurveyListing/
├── SurveyListingModule.cs         # IModule implementation
├── ModuleInfo.json                # Module metadata
├── Assets/
│   └── icon.png                   # 128x128 module icon
├── Views/
│   ├── MainWindow.xaml
│   ├── Steps/                     # Wizard steps
│   └── ...
├── ViewModels/
│   ├── MainViewModel.cs
│   └── ...
├── Models/                        # Module-specific models only
├── Services/
│   ├── DialogService.cs           # KEEP - module-specific dialogs
│   ├── ProcessingCertificateService.cs  # MIGRATE to Core certification
│   ├── ProcessingTracker.cs       # KEEP
│   └── ThemeService.cs            # DELETE - use Shell's
├── Converters/
└── Themes/                        # DELETE - use Shell's
```

## Module Implementation
```csharp
public class SurveyListingModule : IModule
{
    private readonly ICertificationService _certService;
    private readonly IEventAggregator _events;
    private readonly IThemeService _themeService;
    private readonly IErrorReporter _errorReporter;

    // DI Constructor
    public SurveyListingModule(
        ICertificationService certService,
        IEventAggregator events,
        IThemeService themeService,
        IErrorReporter errorReporter)
    {
        _certService = certService;
        _events = events;
        _themeService = themeService;
        _errorReporter = errorReporter;
    }

    // ... IModule implementation
}
```

## Supported File Types
- `.npd` - Survey data files
- `.rlx` - Route alignment files
- `.s7p` / `.slproj` - FathomOS project files
- `.tide` - Tide correction files

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["ProjectName"] = project.Name,
    ["VesselName"] = project.VesselName,
    ["SurveyDate"] = project.SurveyDate,
    ["PointCount"] = points.Count,
    ["RouteFile"] = routeFileName,
    ["TideFile"] = tideFileName
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter
- Generate certificates after completing work
- Follow MVVM pattern

### DO NOT
- Create your own ThemeService (use Shell's)
- Create duplicate services that exist in Core
- Talk to other modules directly
- Create shared state outside Shell services

## Migration Tasks
- [ ] Add DI constructor
- [ ] Remove local ThemeService
- [ ] Remove local Themes folder
- [ ] Migrate ProcessingCertificateService to Core ICertificationService
- [ ] Update to use Core SmoothingService

## Theme Integration
```csharp
public void Initialize()
{
    _themeService.ThemeChanged += OnThemeChanged;
}

private void OnThemeChanged(object? sender, AppTheme theme)
{
    // Theme is applied automatically by Shell
}

public void Shutdown()
{
    _themeService.ThemeChanged -= OnThemeChanged;
}
```

## Error Handling
```csharp
try
{
    // Processing code
}
catch (Exception ex)
{
    _errorReporter.Report(ModuleId, "Processing failed", ex);
}
```
