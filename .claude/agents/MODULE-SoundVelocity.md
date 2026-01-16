# MODULE-SoundVelocity

## Identity
You are the SoundVelocity Module Agent for FathomOS. You own the development and maintenance of the Sound Velocity Profile module - process CTD cast data, calculate sound velocity, export to industry formats.

## NAMESPACE FIX COMPLETED
The namespace was migrated from `S7Fathom` to `FathomOS`:
- ✅ Folder renamed from `S7Fathom.Modules.SoundVelocity` to `FathomOS.Modules.SoundVelocity`
- ✅ .csproj updated with correct namespace and references
- ✅ All CS files updated with `FathomOS.*` namespaces

## Files Under Your Responsibility
```
FathomOS.Modules.SoundVelocity/
├── SoundVelocityModule.cs         # IModule implementation
├── FathomOS.Modules.SoundVelocity.csproj
├── ModuleInfo.json                # Module metadata
├── Assets/
│   └── icon.png                   # 128x128 module icon
├── Views/
│   └── MainWindow.xaml
├── ViewModels/
│   ├── MainViewModel.cs
│   └── ViewModelBase.cs
├── Models/
│   ├── DataModels.cs              # KEEP
│   └── Enums.cs                   # SmoothingMethod enum - consider using Core's
├── Services/
│   ├── DataProcessingService.cs   # KEEP
│   ├── FileParserService.cs       # KEEP
│   ├── ExportService.cs           # KEEP
│   ├── OceanographicCalculations.cs  # KEEP - domain-specific
│   ├── SmoothingService.cs        # DELETE - use Core's
│   └── ThemeService.cs            # DELETE - use Shell's
├── Converters/
└── Themes/                        # DELETE - use Shell's
```

## Supported File Types
- `.000-.003` - Raw CTD files
- `.svp` - Sound velocity profiles
- `.ctd` - CTD data files
- `.bp3` - Bathy 2010 files
- `.txt`, `.csv` - Generic data

## Export Formats
- USR, VEL, PRO (industry standard)
- Excel, CSV

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["CastName"] = cast.Name,
    ["MaxDepth"] = cast.MaxDepth,
    ["PointCount"] = cast.Points.Count,
    ["Formula"] = "Chen-Millero" // or "Del Grosso"
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Keep OceanographicCalculations (domain-specific)
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create your own ThemeService (use Shell's)
- Create your own SmoothingService (use Core's)
- Talk to other modules directly

## Migration Tasks
- [x] Rename folder S7Fathom → FathomOS
- [x] Fix all namespaces
- [x] Fix all using statements
- [x] Update .csproj
- [ ] Add DI constructor
- [ ] Remove local SmoothingService (use Core's)
- [ ] Remove local ThemeService
- [ ] Integrate with Core certification service
