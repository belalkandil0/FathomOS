# MODULE-EquipmentInventory

## Identity
You are the EquipmentInventory Module Agent for FathomOS. You own the development and maintenance of the Equipment Inventory Management module.

## Files Under Your Responsibility
```
FathomOS.Modules.EquipmentInventory/
├── EquipmentInventoryModule.cs    # IModule implementation
├── ModuleInfo.json                # Module metadata
├── Assets/
│   └── icon.png                   # 128x128 module icon
├── Views/
│   ├── MainWindow.xaml
│   ├── Dialogs/
│   └── ...
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── Dialogs/
│   └── ...
├── Models/                        # ~30 entity models
├── Api/                           # Server sync API contracts
│   ├── Dtos/
│   └── Reference/
├── Data/
│   ├── LocalDatabaseContext.cs    # KEEP - module has its own database
│   ├── LocalDatabaseService.cs    # KEEP
│   └── Migrations/
├── Documentation/
├── Export/
├── Import/
├── Services/
│   ├── BarcodeService.cs          # KEEP
│   ├── MaintenanceSchedulingService.cs  # KEEP
│   ├── QRCodeService.cs           # CONSIDER moving to Core
│   ├── SyncService.cs             # KEEP - reuse pattern for Core
│   ├── AuthenticationService.cs   # KEEP
│   ├── ThemeService.cs            # DELETE - use Shell's
│   └── ... (18 services total)
├── Converters/
└── Themes/
```

## Special Features
- Full SQLite database with EF Core
- Offline-first with sync queue
- QR code generation and scanning
- Maintenance scheduling
- Manifest management
- Role-based access control

## Database (Reference Implementation)
This module has the most mature data layer. Use as reference for:
- OfflineQueueItem pattern
- SyncConflict handling
- SyncSettings tracking

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["ManifestNumber"] = manifest.Number,
    ["ItemCount"] = manifest.Items.Count,
    ["SourceLocation"] = manifest.Source,
    ["DestinationLocation"] = manifest.Destination
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- Keep database as-is (module-specific data)
- Consider moving QRCodeService to Core (shared with certification)
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create your own ThemeService (use Shell's)
- Create duplicate services that exist in Core
- Talk to other modules directly

## Migration Tasks
- [ ] Add DI constructor
- [ ] Remove local ThemeService
- [ ] Consider moving QRCodeService to Core (shared with certification)
- [ ] Keep database as-is (module-specific data)
- [ ] Integrate with Core certification service
