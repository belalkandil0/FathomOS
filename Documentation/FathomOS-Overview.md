# FathomOS Complete System Documentation

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [Introduction](#introduction)
2. [System Overview](#system-overview)
3. [Key Features](#key-features)
4. [Module Categories](#module-categories)
5. [Core Concepts](#core-concepts)
6. [Workflow Overview](#workflow-overview)
7. [Integration Capabilities](#integration-capabilities)
8. [Security and Licensing](#security-and-licensing)

---

## Introduction

FathomOS is a comprehensive, modular hydrographic survey processing platform designed specifically for offshore survey operations. It provides a unified environment that streamlines data processing, equipment calibration, quality control, and project management tasks commonly performed during marine survey operations.

### Target Users

- Survey Engineers and Surveyors
- Survey Party Chiefs
- QC/QA Personnel
- Offshore Survey Managers
- Calibration Specialists

### Design Philosophy

FathomOS follows a modular architecture where each functional area is implemented as an independent module. This approach provides:

- **Flexibility**: Organizations can deploy only the modules they need
- **Maintainability**: Individual modules can be updated independently
- **Extensibility**: Custom modules can be developed to meet specific requirements
- **Consistency**: All modules share a common UI framework and design language

---

## System Overview

### Platform Architecture

FathomOS consists of three main components:

1. **FathomOS.Shell** - The main application host that provides:
   - Module discovery and loading
   - User authentication and licensing
   - Theme management
   - Certificate generation and storage
   - Shared services infrastructure

2. **FathomOS.Core** - Shared library containing:
   - Common data models
   - File parsers (NPD, RLX, tide files)
   - Calculation engines
   - Export functionality (DXF, Excel, PDF)
   - Service interfaces

3. **Modules** - Independent functional units:
   - Data processing modules
   - Calibration modules
   - Utility modules
   - Operations modules

### Technology Foundation

| Component | Technology |
|-----------|------------|
| Framework | .NET 8.0 Windows |
| UI Framework | WPF with MahApps.Metro |
| Charting | OxyPlot 2.1 |
| 3D Visualization | HelixToolkit.WPF.SharpDX |
| PDF Generation | QuestPDF 2024.3 |
| Excel Operations | ClosedXML 0.102 |
| CAD Export | netDxf 3.0 |
| Database | Microsoft.Data.Sqlite |
| Mathematics | MathNet.Numerics 5.0 |

---

## Key Features

### Data Processing
- NaviPac NPD file parsing with automatic column detection
- Route alignment from RLX files
- Tide correction application
- KP (Kilometer Point) calculation
- Data smoothing and spline fitting
- Interval-based output generation

### Export Capabilities
- **Excel**: Formatted spreadsheets with charts
- **DXF/CAD**: 2D and 3D polylines with layers
- **PDF**: Professional reports with branding
- **Text/CSV**: Standard data formats

### Quality Control
- Statistical analysis (mean, std dev, 2DRMS)
- 3-sigma outlier rejection
- Data validation and verification
- Calibration certificate generation

### Professional Output
- Branded PDF certificates
- Digital signatures
- Cloud synchronization
- Audit trail maintenance

---

## Module Categories

### Data Processing

| Module | Purpose |
|--------|---------|
| Survey Listing Generator | Generate survey listings from NPD data with route alignment |
| Survey Logbook | Electronic survey log with real-time NaviPac integration |
| Sound Velocity Profile | Process CTD data and calculate sound velocity |

### Calibration & Verification

| Module | Purpose |
|--------|---------|
| GNSS Calibration | Compare and validate GNSS positioning systems |
| MRU Calibration | Calibrate pitch/roll sensors by inter-system comparison |
| USBL Verification | Verify USBL positioning through spin/transit tests |
| Vessel Gyro Calibration | Calibrate vessel gyro using C-O methodology |
| ROV Gyro Calibration | Calibrate ROV gyro against vessel reference |
| Tree Inclination | Calculate structure inclination from depth sensors |

### Utilities

| Module | Purpose |
|--------|---------|
| Network Time Sync | Synchronize time across network computers |
| Equipment Inventory | Track and manage survey equipment |

### Operations

| Module | Purpose |
|--------|---------|
| Personnel Management | Manage crew, certifications, and timesheets |
| Project Management | Manage projects, milestones, and deliverables |

---

## Core Concepts

### Projects

A Project in FathomOS represents a survey campaign or job. Projects contain:
- Project metadata (name, client, vessel)
- Configuration settings
- Input data references
- Processing parameters
- Output file tracking

### Certificates

Processing certificates provide an auditable record of data processing operations. Each certificate contains:
- Unique certificate ID (e.g., SL-2026-000001)
- Module and version information
- Input/output file lists
- Processing parameters
- Signatory information
- Timestamp and digital signature

### Module Groups

Modules can be organized into groups for dashboard organization. The Calibrations group, for example, contains all calibration-related modules under a single expandable tile.

### Themes

FathomOS supports multiple UI themes:
- **Light** - Traditional light interface
- **Dark** - Dark mode for low-light environments
- **Modern** - Contemporary design
- **Gradient** - Gradient-based styling

---

## Workflow Overview

### Typical Survey Listing Workflow

1. **Project Setup**
   - Create new project or load existing
   - Set project metadata (client, vessel, location)

2. **Data Import**
   - Load NPD survey data files
   - Import route alignment (RLX file)
   - Load tide correction data

3. **Configuration**
   - Map data columns
   - Set processing parameters
   - Configure depth offsets

4. **Processing**
   - Align survey to route
   - Apply tide corrections
   - Calculate derived values
   - Apply smoothing/filtering

5. **Review**
   - Validate results
   - Check for anomalies
   - Preview charts and statistics

6. **Export**
   - Generate Excel listings
   - Export CAD files
   - Create PDF reports
   - Generate processing certificate

### Calibration Workflow

1. **Data Collection**
   - Import calibration data files
   - Map required columns

2. **Analysis**
   - Automatic outlier detection
   - Statistical calculations
   - Comparison visualization

3. **Reporting**
   - Generate calibration certificate
   - Export detailed reports

---

## Integration Capabilities

### File Associations

FathomOS modules register to handle specific file types:

| Extension | Module | Description |
|-----------|--------|-------------|
| .npd | Multiple | NaviPac survey data |
| .rlx | Survey Listing | Route alignment |
| .s7p | Survey Listing | FathomOS project |
| .tide | Survey Listing | Tide correction data |
| .slog/.slogz | Survey Logbook | Survey log files |
| .npc | Survey Logbook | NaviPac calibration |
| .svp/.ctd | Sound Velocity | Sound velocity profiles |
| .usbl | USBL Verification | USBL test data |

### NaviPac Integration

- Real-time TCP data capture
- File monitoring for log updates
- Waypoint file parsing
- Calibration file import

### Network Integration

- Time synchronization across computers
- Equipment inventory cloud sync
- Certificate cloud backup

---

## Security and Licensing

### License Management

FathomOS uses a hardware-bound licensing system:
- Licenses are tied to hardware fingerprint
- Online activation required
- Optional offline activation
- License branding for resellers

### Certificate Security

- SHA-256 hash verification
- Digital signatures
- Server synchronization
- Tamper detection

### Data Protection

- SQLite encrypted storage
- Protected credential storage
- Audit logging

---

## Related Documentation

- [Architecture](Architecture.md) - Detailed system architecture
- [Getting Started](GettingStarted.md) - Quick start guide
- [Developer Guide](DeveloperGuide.md) - Module development
- [API Reference](API/Module-API.md) - Programming interfaces

---

*Copyright 2026 Fathom OS. All rights reserved.*
