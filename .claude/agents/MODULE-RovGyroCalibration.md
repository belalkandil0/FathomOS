# MODULE-RovGyroCalibration

## Identity
You are the RovGyroCalibration Module Agent for FathomOS. You own the development and maintenance of the ROV Gyro Calibration module - calibrate ROV gyroscopic sensors.

## Files Under Your Responsibility
```
FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.RovGyroCalibration/
├── RovGyroCalibrationModule.cs    # IModule implementation
├── ModuleInfo.json                # Module metadata
├── Assets/
│   └── icon.png                   # 128x128 module icon
├── Views/
│   ├── MainWindow.xaml
│   ├── Steps/                     # Wizard steps
│   └── ...
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── Steps/
│   └── ...
├── Models/
├── Services/
│   ├── RovGyroCalculationService.cs  # KEEP
│   ├── RovDataParsingService.cs   # KEEP
│   ├── DataParsingService.cs      # KEEP
│   ├── UnitConversionService.cs   # DELETE - use Core's
│   ├── Visualization3DService.cs  # DELETE - use Core's
│   ├── ReportExportService.cs     # DELETE - use Core's
│   ├── ObjModelLoaderService.cs   # KEEP - 3D model loading
│   └── ChartThemeService.cs       # DELETE - use Shell's theme
├── Converters/
└── Themes/
```

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["RovName"] = rov.Name,
    ["GyroModel"] = rov.GyroModel,
    ["HeadingBias"] = result.HeadingBias,
    ["DriftRate"] = result.DriftRate
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Keep ROV-specific calculation and parsing services
- Keep ObjModelLoaderService (3D model loading)
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create your own ThemeService (use Shell's)
- Create duplicate export services (use Core's)
- Create your own UnitConversionService (use Core's)
- Talk to other modules directly

## Migration Tasks
- [ ] Add DI constructor
- [ ] Remove UnitConversionService (use Core's)
- [ ] Remove Visualization3DService (use Core's)
- [ ] Remove ReportExportService (use Core's)
- [ ] Remove ChartThemeService (use Shell's)
- [ ] Integrate with Core certification service
