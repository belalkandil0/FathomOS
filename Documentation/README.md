# FathomOS Documentation

**Version:** 1.0.45
**Last Updated:** January 2026

## Overview

FathomOS is a modular hydrographic survey processing platform designed for offshore survey operations. The platform provides a unified environment for data processing, equipment calibration, quality control, and project management.

## Documentation Structure

This documentation is organized into the following sections:

### Core Documentation
- [FathomOS Overview](FathomOS-Overview.md) - Complete system documentation
- [Architecture](Architecture.md) - System architecture and design
- [Getting Started](GettingStarted.md) - Quick start guide
- [Developer Guide](DeveloperGuide.md) - Development setup and guidelines

### API Reference
- [Core API](API/Core-API.md) - FathomOS.Core library reference
- [Shell API](API/Shell-API.md) - FathomOS.Shell application reference
- [Module API](API/Module-API.md) - IModule interface documentation

### Module Documentation
- **Data Processing**
  - [Survey Listing](Modules/SurveyListing.md) - Survey listing generator
  - [Survey Logbook](Modules/SurveyLogbook.md) - Electronic survey logbook
  - [Sound Velocity](Modules/SoundVelocity.md) - CTD/SVP processing

- **Calibration & Verification**
  - [GNSS Calibration](Modules/GnssCalibration.md) - GNSS positioning verification
  - [MRU Calibration](Modules/MruCalibration.md) - Motion reference unit calibration
  - [USBL Verification](Modules/UsblVerification.md) - USBL accuracy testing
  - [Tree Inclination](Modules/TreeInclination.md) - Structure inclination calculator
  - [ROV Gyro Calibration](Modules/RovGyroCalibration.md) - ROV gyro calibration
  - [Vessel Gyro Calibration](Modules/VesselGyroCalibration.md) - Vessel gyro calibration

- **Utilities**
  - [Network Time Sync](Modules/NetworkTimeSync.md) - Network time synchronization
  - [Equipment Inventory](Modules/EquipmentInventory.md) - Equipment tracking

- **Operations**
  - [Personnel Management](Modules/PersonnelManagement.md) - Crew management
  - [Project Management](Modules/ProjectManagement.md) - Project coordination

### Services Documentation
- [Theme Service](Services/ThemeService.md) - Application theming
- [Certification Service](Services/CertificationService.md) - Processing certificates
- [Event Aggregator](Services/EventAggregator.md) - Cross-module messaging
- [Module Manager](Services/ModuleManager.md) - Module discovery and loading

### Deployment
- [Installation Guide](Deployment/Installation.md) - Installation instructions
- [Configuration Guide](Deployment/Configuration.md) - Configuration options

## Quick Links

| Topic | Description |
|-------|-------------|
| [Getting Started](GettingStarted.md) | First-time setup and basic usage |
| [Module Development](DeveloperGuide.md#creating-a-new-module) | Creating custom modules |
| [API Reference](API/Module-API.md) | IModule interface documentation |
| [Troubleshooting](Deployment/Configuration.md#troubleshooting) | Common issues and solutions |

## Technology Stack

- **.NET 8.0** - Windows desktop framework
- **WPF** - Windows Presentation Foundation for UI
- **MahApps.Metro** - Modern UI framework
- **OxyPlot** - Charting and visualization
- **HelixToolkit** - 3D visualization
- **QuestPDF** - PDF report generation
- **ClosedXML** - Excel file handling
- **netDxf** - CAD file export

## Support

For support inquiries, please contact the FathomOS development team.

---

*Copyright 2026 Fathom OS. All rights reserved.*
