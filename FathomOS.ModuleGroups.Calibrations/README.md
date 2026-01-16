# Calibrations Module Group

This folder contains all calibration-related modules. Modules placed here will appear under the "Calibrations" group tile on the Fathom OS dashboard.

## How to Add a Module to This Group

1. Create your module folder inside this directory:
   ```
   FathomOS.ModuleGroups.Calibrations/
   └── FathomOS.Modules.GyroCalibration/   ← Your module here
       ├── FathomOS.Modules.GyroCalibration.csproj
       ├── ModuleInfo.json
       ├── YourModule.cs  (implements IModule)
       ├── Assets/
       │   └── icon.png
       └── Views/
           └── MainWindow.xaml
   ```

2. The module will be automatically:
   - Discovered by Shell.csproj at build time
   - Built with the solution
   - Deployed to `bin/Modules/_Groups/Calibrations/{ModuleName}/`
   - Displayed when user clicks the "Calibrations" tile

## GroupInfo.json

```json
{
  "GroupId": "Calibrations",
  "DisplayName": "Calibrations",
  "Description": "Sensor calibration and verification tools",
  "DisplayOrder": 10
}
```

## Planned Modules

- [ ] Gyro Calibration
- [ ] MRU Calibration
- [ ] USBL Calibration
- [ ] DVL Calibration
- [ ] Depth Sensor Calibration

## Notes

- Each module is developed in a separate chat/session
- All modules automatically get Core packages (MahApps, OxyPlot, HelixToolkit, etc.)
- Just reference FathomOS.Core in your module's .csproj
