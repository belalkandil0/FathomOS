# Fathom OS

**Professional Marine Survey Software Suite**

Fathom OS is a comprehensive, modular software platform designed for offshore and marine survey operations. Built with modern .NET 8 and WPF, it provides a unified dashboard for managing survey data processing, equipment calibration, project management, and quality control workflows.

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Modules](#modules)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [License](#license)
- [Contributing](#contributing)
- [Support](#support)
- [Version History](#version-history)

---

## Features

### Core Platform
- **Modular Architecture**: Plug-and-play modules loaded dynamically at runtime
- **Unified Dashboard**: Central hub for accessing all survey tools
- **Dark/Light Themes**: Full theme support across all modules
- **License Management**: Secure, hardware-bound licensing with online/offline support
- **Certificate Generation**: Automated processing verification certificates
- **White-Label Support**: Customizable branding for enterprise deployments

### Data Processing
- **Survey Listing Generator**: Process NPD survey data with route alignment, tide corrections, and smoothing
- **Sound Velocity Profiles**: CTD data processing with Chen-Millero and Del Grosso formulas
- **Multiple Export Formats**: Excel, PDF, DXF, CAD scripts, and text exports

### Quality Control
- **USBL Verification**: 7-step guided workflow for acoustic positioning verification
- **GNSS Calibration**: Geodetic receiver calibration and verification
- **MRU Calibration**: Motion Reference Unit calibration tools
- **Gyro Calibration**: Vessel and ROV gyrocompass verification
- **Tree Inclination**: Christmas tree verticality analysis

### Operations Management
- **Equipment Inventory**: QR code tracking, manifests, and multi-location sync
- **Project Management**: Milestones, deliverables, and client coordination
- **Survey Logbook**: Electronic logging with NaviPac integration
- **Personnel Management**: Crew tracking and certification management
- **Network Time Sync**: Network-wide time synchronization with NTP and GPS support

---

## Screenshots

### Dashboard
<!-- ![Dashboard Screenshot](docs/images/dashboard.png) -->
*Main dashboard with module tiles and quick access - Screenshot placeholder*

### Survey Listing Generator
<!-- ![Survey Listing Screenshot](docs/images/survey-listing.png) -->
*7-step wizard for survey data processing - Screenshot placeholder*

### USBL Verification
<!-- ![USBL Verification Screenshot](docs/images/usbl-verification.png) -->
*Quality dashboard with statistical analysis - Screenshot placeholder*

---

## System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| OS | Windows 10 (64-bit) | Windows 11 (64-bit) |
| .NET | .NET 8.0 Runtime | .NET 8.0 Runtime |
| RAM | 4 GB | 8 GB or more |
| Storage | 500 MB | 1 GB or more |
| Display | 1280x720 | 1920x1080 or higher |

---

## Installation

### Option 1: Installer (Recommended)
1. Download the latest installer from the releases page
2. Run `FathomOS-Setup.exe`
3. Follow the installation wizard
4. Launch Fathom OS from the Start Menu

### Option 2: Portable Version
1. Download the portable ZIP archive
2. Extract to your preferred location
3. Run `FathomOS.Shell.exe`

### Option 3: Build from Source
```bash
# Clone the repository
git clone https://github.com/fathomos/fathomos.git

# Navigate to the project directory
cd fathomos

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the application
dotnet run --project FathomOS.Shell
```

---

## Quick Start

### 1. Activate Your License
- Launch Fathom OS
- Click "Activate License" on the dashboard
- Enter your license key or import a license file (`.lic` format)
- Your licensed modules will appear on the dashboard

### 2. Select a Module
- Click any module tile to launch it
- Each module has its own workflow and documentation
- Use the Help menu (F1) for module-specific guidance

### 3. Process Your Data
- Follow the step-by-step wizard in each module
- Configure settings for your specific workflow
- Export results in your preferred format

### 4. Generate Certificates
- After processing, use the certificate generation feature
- Select a signatory for approval
- Export PDF certificates for documentation

---

## Modules

### Data Processing

| Module | Version | Description |
|--------|---------|-------------|
| **Survey Listing Generator** | 1.0.45 | Process NPD survey data with route alignment, tide corrections, smoothing, KP/DCC calculations, and multiple export formats (Excel, DXF, PDF) |
| **Sound Velocity Profile** | 1.2.0 | Process CTD cast data and calculate sound velocity using Chen-Millero or Del Grosso formulas. Export to USR, VEL, PRO formats |
| **Survey Electronic Logbook** | 11.0.0 | Comprehensive data logging with NaviPac integration, DVR monitoring, position fixes, and DPR generation |

### Quality Control & Calibrations

| Module | Version | Description |
|--------|---------|-------------|
| **USBL Verification** | 1.7.0 | Automated USBL system verification with 7-step workflow, batch import, quality dashboard, and certificate generation |
| **GNSS Calibration** | 1.0.0 | GNSS receiver calibration and verification workflows |
| **MRU Calibration** | 1.0.0 | Motion Reference Unit calibration tools |
| **Vessel Gyro Calibration** | 1.0.0 | Vessel gyrocompass verification |
| **ROV Gyro Calibration** | 1.0.0 | ROV gyrocompass calibration |
| **Tree Inclination** | 1.0.0 | Christmas tree verticality analysis |

### Operations Management

| Module | Version | Description |
|--------|---------|-------------|
| **Equipment Inventory** | 1.0.0 | QR code tracking, inward/outward manifests, multi-location synchronization, certification tracking |
| **Project Management** | 1.0.0 | Project tracking, milestones, deliverables, and client coordination |
| **Personnel Management** | 1.0.0 | Crew tracking and certification management |

### Utilities

| Module | Version | Description |
|--------|---------|-------------|
| **Network Time Sync** | 2.0.0 | Network-wide time synchronization with NTP, GPS, and reference computer support |

---

## Architecture

Fathom OS follows Clean Architecture principles with a modular design:

```
FathomOS/
├── FathomOS.Core/                      # Shared library (models, parsers, services, interfaces)
│   ├── Calculations/                   # Mathematical calculations (depth, KP, unit conversion)
│   ├── Export/                         # Export services (DXF, Excel, PDF, Text)
│   ├── Interfaces/                     # Core interfaces (IModule, services)
│   ├── Models/                         # Domain models (SurveyPoint, RouteData, etc.)
│   ├── Parsers/                        # File parsers (RLX, NPD, Tide, DXF)
│   └── Services/                       # Core services (Smoothing, Cache, AutoSave)
│
├── FathomOS.Domain/                    # Business logic and domain models
│
├── FathomOS.Shell/                     # Main application shell and dashboard
│   ├── Views/                          # Shell windows and dialogs
│   └── Modules/                        # Module loading directory
│
├── FathomOS.Modules.SurveyListing/     # Survey Listing Generator module
├── FathomOS.Modules.SurveyLogbook/     # Survey Electronic Logbook module
├── FathomOS.Modules.SoundVelocity/     # Sound Velocity Profile module
├── FathomOS.Modules.EquipmentInventory/# Equipment Inventory module
├── FathomOS.Modules.ProjectManagement/ # Project Management module
├── FathomOS.Modules.PersonnelManagement/# Personnel Management module
├── FathomOS.Modules.NetworkTimeSync/   # Network Time Sync module
│
├── FathomOS.ModuleGroups.Calibrations/ # Grouped calibration modules
│   ├── FathomOS.Modules.UsblVerification/
│   ├── FathomOS.Modules.GnssCalibration/
│   ├── FathomOS.Modules.MruCalibration/
│   ├── FathomOS.Modules.RovGyroCalibration/
│   ├── FathomOS.Modules.VesselGyroCalibration/
│   └── FathomOS.Modules.TreeInclination/
│
├── LicensingSystem.Client/             # License validation client
├── LicensingSystem.Shared/             # Shared licensing models
│
├── FathomOS.Tests/                     # Unit and integration tests
│
└── Documentation/                      # Project documentation
```

For detailed architecture documentation, see [Documentation/ARCHITECTURE.md](Documentation/ARCHITECTURE.md).

---

## Technology Stack

### Core Framework
- **.NET 8.0** - Latest LTS Windows desktop framework
- **WPF** - Windows Presentation Foundation UI
- **C# 12** - Latest language features

### UI Components
- **MahApps.Metro** - Modern Metro-style UI framework
- **HelixToolkit** - 3D visualization
- **OxyPlot** - Charting and graphs
- **ControlzEx** - UI behaviors and controls

### Data Processing
- **MathNet.Numerics** - Numerical computations and statistics
- **Microsoft.Data.Sqlite** - Local data storage

### Export & Reporting
- **ClosedXML** - Excel file generation
- **netDxf** - DXF/CAD file export
- **QuestPDF** - PDF report generation

### Security
- **System.Security.Cryptography** - ECDSA license signing
- **Hardware fingerprinting** - Multi-factor machine identification

---

## License

Fathom OS is proprietary software. A valid license is required for use.

### License Types

| Type | Description |
|------|-------------|
| **Standard** | Core modules for survey operations |
| **Professional** | Extended modules including calibrations |
| **Enterprise** | Full suite with white-labeling support |

### Key Features
- Hardware-bound activation (ECDSA signatures)
- Offline operation support with grace period
- Module-based licensing (per-module activation)
- Certificate generation capabilities
- White-label branding for enterprise

### Licensing Quick Start
1. Obtain license file (`.lic`) from vendor
2. Launch FathomOS
3. Load license file or enter license key
4. Licensed modules appear on dashboard

For detailed licensing documentation, see:
- [Licensing Overview](Documentation/Licensing/README.md)
- [Vendor Guide](Documentation/Licensing/VendorGuide.md)
- [Technical Reference](Documentation/Licensing/TechnicalReference.md)

Contact [sales@fathomos.com](mailto:sales@fathomos.com) for licensing inquiries.

---

## Contributing

We welcome contributions from the community. Please read our contributing guidelines before submitting pull requests.

### Development Setup
1. Install Visual Studio 2022 or later
2. Install .NET 8.0 SDK
3. Clone the repository
4. Open `FathomOS.sln` in Visual Studio
5. Build and run

### Code Standards
- Follow C# coding conventions
- Use meaningful names for classes, methods, and variables
- Include XML documentation for public APIs
- Write unit tests for new functionality

### Module Development
To create a new module:
1. Implement `IModule` interface from FathomOS.Core
2. Create `ModuleInfo.json` metadata file
3. Include 128x128 module icon
4. Use `SurveyPoint` model from Core for survey data
5. Reference Core parsers (don't duplicate)

For detailed development guidelines, see [Documentation/DEVELOPER_GUIDE.md](Documentation/DEVELOPER_GUIDE.md).

---

## Support

### Documentation
- [Architecture Guide](Documentation/ARCHITECTURE.md)
- [Developer Guide](Documentation/DEVELOPER_GUIDE.md)
- [API Reference](Documentation/API_REFERENCE.md)
- [User Manual](Documentation/USER_MANUAL.md)

### Contact
- **Technical Support**: [support@fathomos.com](mailto:support@fathomos.com)
- **Sales**: [sales@fathomos.com](mailto:sales@fathomos.com)
- **Website**: [https://fathomos.com](https://fathomos.com)

### Issue Reporting
Report bugs and feature requests through the issue tracker. Include:
- Fathom OS version
- Module affected
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.45 | January 2026 | Digital certificate system, certificate manager, white-label support |
| 1.0.44 | January 2026 | Certificate API for modules, signatory management |
| 1.0.0 | December 2024 | Initial modular architecture release |

### v1.0.45 Highlights
- **Digital Certificate System**: Generate professional certificates for completed jobs
- **Offline Generation**: Create certificates locally, sync when online
- **White-Label Support**: Certificates include licensee branding
- **Certificate Manager**: Access via Certificates button in dashboard footer

For complete changelog, see [Documentation/CHANGELOG.md](Documentation/CHANGELOG.md).

---

**Current Version**: v1.0.45
**Build Date**: January 2026
**Platform**: Windows (x64)

---

Copyright 2026 Fathom OS. All rights reserved.
