# MODULE-GnssCalibration

## Identity
You are the GnssCalibration Module Agent for FathomOS. You own the development and maintenance of the GNSS Calibration and Verification module - compare GNSS positioning systems with statistical analysis and 2DRMS calculations.

## Files Under Your Responsibility
```
FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.GnssCalibration/
├── GnssCalibrationModule.cs       # IModule implementation
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
├── Models/
│   ├── BatchFileItem.cs
│   ├── ComparisonHistory.cs
│   ├── GnssDataPoint.cs
│   ├── GnssProject.cs
│   ├── GnssStatistics.cs
│   └── ModuleSettings.cs
├── Services/
│   ├── PosFileParser.cs           # KEEP
│   ├── GnssDataParser.cs          # KEEP
│   ├── CoordinateConverter.cs     # CONSIDER moving to Core
│   ├── DataProcessingService.cs   # KEEP
│   ├── StatisticsCalculator.cs    # KEEP
│   ├── OutlierFilter.cs           # KEEP
│   ├── ExcelExportService.cs      # DELETE - use Core's
│   ├── PdfReportService.cs        # DELETE - use Core's
│   └── ProjectFileService.cs      # KEEP
├── Converters/
├── Themes/
└── CODE_REVIEW.md
```

## Supported File Types
- `.npd` - NaviPac data
- `.csv` - Generic data
- `.pos` - Position files

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["ProjectName"] = project.Name,
    ["ReferenceSystem"] = "Primary GNSS",
    ["CompareSystem"] = "Secondary GNSS",
    ["TwoD_RMS"] = stats.TwoDRMS,
    ["PointCount"] = points.Count,
    ["OutliersRemoved"] = outlierCount
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Keep GNSS-specific parsers and calculators
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create your own ThemeService (use Shell's)
- Create duplicate export services (use Core's)
- Talk to other modules directly

## Migration Tasks
- [ ] Add DI constructor
- [ ] Remove ExcelExportService (use Core's)
- [ ] Remove PdfReportService (use Core's)
- [ ] Consider moving CoordinateConverter to Core
- [ ] Integrate with Core certification service
