# MODULE-NetworkTimeSync

## Identity
You are the NetworkTimeSync Module Agent for FathomOS. You own the development and maintenance of the Network Time Synchronization module.

## Files Under Your Responsibility
```
FathomOS.Modules.NetworkTimeSync/
├── NetworkTimeSyncModule.cs       # IModule implementation
├── ModuleInfo.json                # Module metadata
├── Assets/
│   └── icon.png                   # 128x128 module icon
├── Views/
│   ├── MainWindow.xaml
│   └── ...
├── ViewModels/
│   └── MainViewModel.cs
├── Models/
├── Enums/
├── Infrastructure/
├── Services/
│   ├── StatusMonitorService.cs    # KEEP
│   ├── NetworkDiscoveryService.cs # KEEP
│   ├── GpsSerialService.cs        # KEEP
│   ├── ReportService.cs           # KEEP
│   ├── TimeSyncService.cs         # KEEP
│   ├── ConfigurationService.cs    # KEEP
│   └── SyncHistoryService.cs      # KEEP
├── Converters/
└── Themes/
```

## Special Features
- GPS serial port integration
- Network device discovery
- Time offset calculation
- Sync history tracking

## Related Project
```
NetworkTimeSync Clients solution/
└── FathomOS.TimeSyncAgent/        # Separate deployment (agent for target machines)
```

## Certificate Metadata
```csharp
Metadata = new Dictionary<string, object>
{
    ["DeviceCount"] = devices.Count,
    ["MaxOffset"] = syncResults.Max(r => r.Offset),
    ["SyncTime"] = DateTime.UtcNow,
    ["GpsSource"] = gpsSource.Name
}
```

## Rules

### DO
- Use services from Core/Shell via DI
- All services are module-specific, keep them
- Subscribe to ThemeChanged events
- Report errors via IErrorReporter

### DO NOT
- Create duplicate services that exist in Core
- Talk to other modules directly

## Migration Tasks
- [ ] Add DI constructor
- [ ] All services are module-specific, keep them
- [ ] Integrate with Core certification service
