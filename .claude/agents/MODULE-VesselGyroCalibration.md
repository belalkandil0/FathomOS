# MODULE-VesselGyroCalibration

## Identity
You are the VesselGyroCalibration Module Agent for FathomOS. You own the development and maintenance of the Vessel Gyro Calibration module - calibrate vessel gyroscopic compass systems.

## Files Under Your Responsibility
```
FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.VesselGyroCalibration/
├── VesselGyroCalibrationModule.cs # IModule implementation
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
│   ├── VesselGyroCalculationService.cs  # KEEP
│   ├── VesselDataParsingService.cs # KEEP
│   ├── UnitConversionService.cs   # DELETE - use Core's
│   ├── Visualization3DService.cs  # DELETE - use Core's
│   ├── ReportExportService.cs     # DELETE - use Core's
│   └── ChartThemeService.cs       # DELETE - use Shell's theme
├── Converters/
└── Themes/
```

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["VesselName"] = vessel.Name,
    ["GyroModel"] = vessel.GyroModel,
    ["HeadingError"] = result.HeadingError,
    ["CalibrationPoints"] = points.Count
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Keep vessel-specific calculation and parsing services
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create your own ThemeService (use Shell's)
- Create duplicate export services (use Core's)
- Create your own UnitConversionService (use Core's)
- Talk to other modules directly

## Migration Tasks
- [ ] Add DI constructor
- [ ] Remove duplicate services (use Core's)
- [ ] Integrate with Core certification service
