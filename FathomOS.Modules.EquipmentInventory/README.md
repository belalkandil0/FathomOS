# Equipment Inventory Module

**Module ID:** EquipmentInventory
**Version:** 1.0.0
**Category:** Operations
**Author:** Fathom OS Team

---

## Overview

Enterprise-grade equipment and inventory management for offshore/onshore operations. This module provides comprehensive tracking of survey equipment with QR code support, manifest generation, and multi-location synchronization.

**Scale:** 175,000-250,000 equipment items, 100 bases, 150 vessels

## Key Features

- ✅ Equipment registry with auto-generated asset numbers
- ✅ QR code generation and scanning (format: `foseq:EQ-2024-00001`)
- ✅ Inward/Outward manifest management with approval workflows
- ✅ Defect Reporting (EFN - Equipment Failure Notification)
- ✅ Certification and calibration tracking with alerts
- ✅ Offline-first architecture with delta sync
- ✅ Excel/PDF export capabilities
- ✅ Dark/Light theme support
- ✅ Certificate generation for verification

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    DESKTOP MODULE                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   Views     │  │ ViewModels  │  │  Services   │         │
│  │   (XAML)    │◄─┤   (MVVM)    │◄─┤   (Logic)   │         │
│  └─────────────┘  └─────────────┘  └──────┬──────┘         │
│                                           │                 │
│  ┌────────────────────────────────────────┼───────────────┐│
│  │              LOCAL DATABASE             │               ││
│  │              (SQLite + EF Core)         ▼               ││
│  │  ┌─────────┐  ┌─────────┐  ┌─────────────────────┐     ││
│  │  │Equipment│  │Manifests│  │   Offline Queue     │     ││
│  │  └─────────┘  └─────────┘  └─────────────────────┘     ││
│  └─────────────────────────────────────────────────────────┘│
│                           │                                 │
│                           │ Sync                            │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                    API CLIENT                           ││
│  │  • JWT Authentication (15 min access, 7 day refresh)    ││
│  │  • Delta Sync (SyncVersion tracking)                    ││
│  │  • Conflict Resolution                                  ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                 CENTRAL API SERVER                          │
│                 (ASP.NET Core 8.0)                          │
│                                                             │
│  Endpoints:                                                 │
│  • POST /api/auth/login         - Authentication            │
│  • GET  /api/equipment          - List equipment            │
│  • GET  /api/equipment/qr/{qr}  - Lookup by QR              │
│  • POST /api/manifests          - Create manifest           │
│  • POST /api/sync/pull          - Pull changes              │
│  • POST /api/sync/push          - Push changes              │
└─────────────────────────────────────────────────────────────┘
```

## Certificate System

This module supports Fathom OS certificate generation with:

| Field | Value |
|-------|-------|
| Certificate Code | EI |
| Certificate Title | Equipment & Inventory Management Verification Certificate |

### Processing Data for Certificates

```csharp
var processingData = new Dictionary<string, string>
{
    ["Total Equipment Items"] = "15,234",
    ["Manifest Number"] = "MN-2024-00125",
    ["Equipment Transfers"] = "47 items",
    ["Certification Status"] = "All items verified",
    ["Location"] = "Offshore Platform Alpha",
    ["Inventory Date"] = "31 Dec 2024"
};
```

## API Integration

### Authentication
```csharp
var apiClient = new ApiClient("https://api.yourcompany.com/api");
var (success, error) = await apiClient.LoginAsync("username", "password");
```

### QR Code Format (CRITICAL - Must Match Mobile App)
```
Equipment: foseq:{assetNumber}  → foseq:EQ-2024-00001
Manifest:  fosman:{manifestNumber} → fosman:MN-2024-00125
Defect:    fosefn:{reportNumber} → fosefn:EFN-2024-00001
```

### Sync Protocol
```csharp
// Pull changes since last sync
var response = await apiClient.PullChangesAsync(lastSyncVersion);

// Push local changes
var result = await apiClient.PushChangesAsync(changes);
```

## Project Structure

```
FathomOS.Modules.EquipmentInventory/
├── Api/
│   ├── ApiClient.cs              # HTTP client with JWT auth
│   └── DTOs/                     # API request/response models
│       ├── ApiDTOs.cs
│       ├── ManifestDTOs.cs
│       └── SyncDTOs.cs
├── Data/
│   ├── LocalDatabaseContext.cs   # EF Core DbContext
│   └── LocalDatabaseService.cs   # CRUD operations
├── Models/
│   ├── Equipment.cs              # Equipment entity
│   ├── Manifest.cs               # Manifest entity
│   ├── DefectReport.cs           # EFN entity
│   ├── Enums.cs                  # Status/condition enums
│   └── ...
├── Services/
│   ├── SyncService.cs            # Delta sync logic
│   ├── AuthenticationService.cs  # Auth handling
│   ├── ThemeService.cs           # Theme management
│   └── ...
├── ViewModels/
│   ├── MainViewModel.cs
│   └── Dialogs/                  # 6 dialog ViewModels
├── Views/
│   ├── MainWindow.xaml
│   ├── DefectReportListView.xaml
│   └── Dialogs/                  # 7 dialog windows
├── Themes/
│   ├── DarkTheme.xaml
│   └── LightTheme.xaml
└── Documentation/
    ├── DESKTOP_INTEGRATION_HANDOFF.md
    ├── API_DOCUMENTATION.md
    ├── SYSTEM_ARCHITECTURE.md
    └── ...
```

## Statistics

| Category | Count |
|----------|-------|
| C# Files | 50+ |
| XAML Files | 12 |
| Documentation | 10 files |
| Total Lines | ~12,000+ |

## Configuration

### API Settings (ModuleSettings.json)
```json
{
  "ApiBaseUrl": "https://api.yourcompany.com/api",
  "AutoSyncEnabled": true,
  "SyncIntervalMinutes": 5,
  "UseDarkTheme": true
}
```

## Dependencies

- .NET 8.0 Windows
- FathomOS.Core (MahApps, ClosedXML, QuestPDF)
- Entity Framework Core 8.0 (SQLite)
- QRCoder 1.5.1

## Documentation

See `/Documentation/` folder for:
- `DESKTOP_INTEGRATION_HANDOFF.md` - Complete integration guide
- `API_DOCUMENTATION.md` - All API endpoints
- `SYSTEM_ARCHITECTURE.md` - Database schema & system design
- `MOBILE_APP_REQUIREMENTS.md` - Mobile app specification

## License

Proprietary - Fathom OS
