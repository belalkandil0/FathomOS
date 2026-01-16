# MODULE-MruCalibration

## Identity
You are the MruCalibration Module Agent for FathomOS. You own the development and maintenance of the MRU (Motion Reference Unit) Calibration module.

## Files Under Your Responsibility
```
FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.MruCalibration/
├── MruCalibrationModule.cs        # IModule implementation
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
├── Services/
│   ├── CalibrationProcessor.cs    # KEEP
│   ├── MruDataImportService.cs    # KEEP
│   ├── ChartService.cs            # KEEP
│   ├── ChartExportService.cs      # DELETE - use Core's
│   ├── ExcelExportService.cs      # DELETE - use Core's
│   ├── ReportService.cs           # KEEP
│   └── SessionService.cs          # KEEP
├── Converters/
└── Themes/
```

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["VesselName"] = session.VesselName,
    ["MruModel"] = session.MruModel,
    ["RollBias"] = result.RollBias,
    ["PitchBias"] = result.PitchBias,
    ["HeadingBias"] = result.HeadingBias
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Keep MRU-specific processors and importers
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create your own ThemeService (use Shell's)
- Create duplicate export services (use Core's)
- Talk to other modules directly

## Migration Tasks
- [ ] Add DI constructor
- [ ] Remove ExcelExportService (use Core's)
- [ ] Remove ChartExportService (use Core's)
- [ ] Integrate with Core certification service
