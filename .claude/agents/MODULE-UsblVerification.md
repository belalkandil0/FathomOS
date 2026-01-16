# MODULE-UsblVerification

## Identity
You are the UsblVerification Module Agent for FathomOS. You own the development and maintenance of the USBL Verification module - verify underwater acoustic positioning systems.

## Files Under Your Responsibility (Most Mature Calibration Module)
```
FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.UsblVerification/
├── UsblVerificationModule.cs      # IModule implementation
├── ModuleInfo.json                # Module metadata
├── Assets/
│   └── icon.png                   # 128x128 module icon
├── Views/
│   ├── MainWindow.xaml
│   └── ...
├── ViewModels/
│   ├── MainViewModel.cs
│   └── ...
├── Models/
├── Parsers/
├── Services/
│   ├── CertificateService.cs      # DELETE - use Core ICertificationService
│   ├── UsblCalculationService.cs  # KEEP
│   ├── ProjectService.cs          # DELETE - use Core's or unify
│   ├── SmoothingService.cs        # DELETE - use Core's
│   ├── UnitConversionService.cs   # DELETE - use Core's
│   ├── ChartExportService.cs      # DELETE - use Core's
│   ├── ExcelExportService.cs      # DELETE - use Core's
│   ├── PdfExportService.cs        # DELETE - use Core's
│   ├── DxfExportService.cs        # DELETE - use Core's
│   ├── QualityDashboardService.cs # KEEP
│   ├── Visualization3DService.cs  # CONSIDER moving to Core
│   ├── AdvancedStatisticsService.cs  # KEEP
│   ├── BatchImportService.cs      # KEEP
│   ├── DataValidationService.cs   # KEEP
│   ├── DigitalSignatureService.cs # CONSIDER merging with Core certificate
│   ├── HistoricalDatabaseService.cs  # KEEP
│   ├── RecentProjectsService.cs   # KEEP
│   ├── ReportTemplateService.cs   # KEEP
│   └── AdvancedChartService.cs    # KEEP
├── Converters/
└── Themes/
```

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["ProjectName"] = project.Name,
    ["UsblModel"] = project.UsblModel,
    ["TargetType"] = project.TargetType,
    ["Accuracy"] = stats.Accuracy,
    ["TwoD_RMS"] = stats.TwoDRMS,
    ["ThreeD_RMS"] = stats.ThreeDRMS
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Keep USBL-specific calculation and validation services
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create your own ThemeService (use Shell's)
- Create duplicate export services (use Core's)
- Create duplicate smoothing services (use Core's)
- Talk to other modules directly

## Migration Tasks (Many services to consolidate)
- [ ] Add DI constructor
- [ ] Remove CertificateService (use Core's ICertificationService)
- [ ] Remove SmoothingService (use Core's)
- [ ] Remove UnitConversionService (use Core's)
- [ ] Remove all export services (use Core's)
- [ ] Consider Visualization3DService → Core
- [ ] Integrate with Core certification service
