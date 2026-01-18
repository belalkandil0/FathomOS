# Fathom OS User Manual

**Version:** 1.0.45
**Last Updated:** January 2026

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Dashboard Overview](#dashboard-overview)
3. [Module Descriptions](#module-descriptions)
4. [License Activation](#license-activation)
5. [Settings Configuration](#settings-configuration)
6. [Troubleshooting](#troubleshooting)

---

## Getting Started

### System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| Operating System | Windows 10 (64-bit) | Windows 11 (64-bit) |
| .NET Runtime | .NET 8.0 | .NET 8.0 |
| RAM | 4 GB | 8 GB or more |
| Storage | 500 MB | 1 GB or more |
| Display | 1280x720 | 1920x1080 or higher |

### Installation

#### Standard Installation

1. Download the FathomOS installer from your vendor
2. Run `FathomOS-Setup.exe` as Administrator
3. Follow the installation wizard
4. Launch Fathom OS from the Start Menu or desktop shortcut

#### Portable Installation

1. Download the portable ZIP archive
2. Extract to your preferred location (e.g., `C:\FathomOS`)
3. Run `FathomOS.Shell.exe` directly

### First Launch

When you first launch Fathom OS, you will see:

1. **License Activation**: If no valid license is found, you'll be prompted to activate
2. **Dashboard**: The main screen showing all available modules
3. **Module Tiles**: Greyed-out tiles for unlicensed modules, active tiles for licensed ones

---

## Dashboard Overview

### Main Dashboard Layout

```
+------------------------------------------------------------------+
|  [Logo]  FATHOM OS                    [Theme] [Settings] [Help]  |
+------------------------------------------------------------------+
|                                                                   |
|  DATA PROCESSING                                                  |
|  +------------------+  +------------------+  +------------------+ |
|  | Survey Listing   |  | Sound Velocity   |  | Survey Logbook   | |
|  | Generator        |  | Profile          |  |                  | |
|  +------------------+  +------------------+  +------------------+ |
|                                                                   |
|  CALIBRATIONS                                                     |
|  +------------------+  +------------------+  +------------------+ |
|  | USBL             |  | GNSS             |  | MRU              | |
|  | Verification     |  | Calibration      |  | Calibration      | |
|  +------------------+  +------------------+  +------------------+ |
|                                                                   |
|  OPERATIONS                                                       |
|  +------------------+  +------------------+  +------------------+ |
|  | Equipment        |  | Project          |  | Personnel        | |
|  | Inventory        |  | Management       |  | Management       | |
|  +------------------+  +------------------+  +------------------+ |
|                                                                   |
+------------------------------------------------------------------+
|  [Certificates]  License: Active | User: Survey Admin            |
+------------------------------------------------------------------+
```

### Navigation

| Element | Function |
|---------|----------|
| **Module Tiles** | Click to launch the corresponding module |
| **Theme Button** | Toggle between Light, Dark, Modern, and Gradient themes |
| **Settings** | Access application settings |
| **Help (F1)** | Open help documentation |
| **Certificates** | View and manage processing certificates |

### Module States

| State | Appearance | Meaning |
|-------|------------|---------|
| Active | Full color with icon | Licensed and ready to use |
| Greyed Out | Dimmed, no icon | Not licensed |
| Group | Expandable tile | Contains multiple modules (click to expand) |

---

## Module Descriptions

### Data Processing Modules

#### Survey Listing Generator

**Purpose:** Process survey data from NPD files to generate professional survey listings with route alignment and tide corrections.

**Key Features:**
- 7-step guided workflow
- Column auto-detection for NPD files
- Route alignment with RLX files
- Tide corrections with multiple interpolation methods
- Position and depth smoothing algorithms
- KP (Kilometer Post) and DCC (Distance to Centerline) calculations
- Multiple export formats: Excel, DXF, PDF, Text, CAD Script

**Workflow:**
1. **Load Data** - Import NPD survey files
2. **Map Columns** - Configure column mapping
3. **Load Route** - Import route alignment (optional)
4. **Load Tide** - Import tide data (optional)
5. **Process** - Apply smoothing and corrections
6. **Review** - Inspect processed data
7. **Export** - Generate output files

---

#### Sound Velocity Profile

**Purpose:** Process CTD cast data and calculate sound velocity profiles using industry-standard formulas.

**Key Features:**
- Multiple input formats: .000, .svp, .ctd, .bp3, .csv
- Chen-Millero and Del Grosso formula support
- Sound speed calculations at depth
- Profile visualization
- Export to USR, VEL, PRO formats

---

#### Survey Electronic Logbook

**Purpose:** Comprehensive electronic logging system for survey operations with NaviPac integration.

**Key Features:**
- Real-time event logging
- NaviPac waypoint file import
- DVR monitoring status
- Position fix management
- Daily Progress Report (DPR) generation
- Auto-save functionality
- Seabed sampling tracking

---

### Calibration Modules

#### USBL Verification

**Purpose:** Automated verification of USBL (Ultra-Short BaseLine) acoustic positioning systems.

**Key Features:**
- 7-step verification workflow
- Batch import with column mapping
- Spin test verification (0, 90, 180, 270)
- Transit test verification (reciprocal lines)
- Quality dashboard with gauges
- IQR outlier detection
- Statistical analysis
- Certificate generation

---

#### GNSS Calibration

**Purpose:** Calibration and verification of GNSS (Global Navigation Satellite System) receivers.

**Key Features:**
- Multi-station comparison
- Baseline measurements
- Statistical analysis
- Accuracy reporting

---

#### MRU Calibration

**Purpose:** Calibration of Motion Reference Units.

**Key Features:**
- Roll, pitch, heave calibration
- Motion data analysis
- Calibration certificates

---

#### Gyro Calibration (Vessel & ROV)

**Purpose:** Verification of vessel and ROV gyrocompass systems.

**Key Features:**
- Heading comparison
- Error analysis
- Calibration tracking

---

#### Tree Inclination

**Purpose:** Analysis of Christmas tree (subsea structure) verticality.

**Key Features:**
- Inclination calculations
- Visualization
- Report generation

---

### Operations Modules

#### Equipment Inventory

**Purpose:** Track survey equipment with QR code support and manifest generation.

**Key Features:**
- Equipment database management
- QR code generation and scanning
- Inward/Outward manifests
- Calibration tracking
- Multi-location synchronization
- Equipment history
- Export to Excel

---

#### Project Management

**Purpose:** Track survey projects, milestones, and deliverables.

**Key Features:**
- Project creation and tracking
- Milestone management
- Deliverable tracking
- Client coordination
- Progress reporting

---

#### Personnel Management

**Purpose:** Manage crew members and track certifications.

**Key Features:**
- Crew database
- Certification tracking
- Expiry alerts
- Competency matrix

---

### Utility Modules

#### Network Time Sync

**Purpose:** Synchronize time across survey network with multiple sources.

**Key Features:**
- NTP server synchronization
- GPS time source support
- Reference computer mode
- Network client management
- Time offset monitoring
- Sync status dashboard

---

## License Activation

### Obtaining a License

Contact your FathomOS vendor to obtain a license file (`.lic`) or license key.

### Activation Methods

#### Method 1: License File

1. Launch FathomOS
2. Click "Activate License" on the dashboard (or press Enter)
3. Click "Browse for License File"
4. Select your `.lic` file
5. Click "Activate"

#### Method 2: License Key

1. Launch FathomOS
2. Click "Activate License"
3. Enter your license key in the text field
4. Click "Activate"

### License Types

| Type | Description | Modules Included |
|------|-------------|------------------|
| Standard | Core survey tools | Survey Listing, Survey Logbook, Sound Velocity |
| Professional | Extended calibration | Standard + All calibration modules |
| Enterprise | Full suite with branding | All modules + white-label support |

### License Information

View license details:
1. Click the license status in the dashboard footer
2. License window shows:
   - Licensee name and company
   - License type
   - Expiration date
   - Licensed modules
   - Hardware fingerprint

### Offline Operation

FathomOS licenses work offline with these features:
- Local validation using ECDSA signatures
- Grace period for license expiration
- No internet required for operation
- Periodic server sync (when online) for certificate backup

---

## Settings Configuration

### Accessing Settings

1. Click the **Settings** button (gear icon) in the dashboard header
2. Or use keyboard shortcut: **Ctrl + ,**

### General Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Theme | Application color theme | System |
| Language | UI language | English |
| Auto-save interval | Minutes between auto-saves | 5 |
| Recent projects | Number of recent projects to show | 10 |

### Theme Options

| Theme | Description |
|-------|-------------|
| Light | White background, dark text |
| Dark | Dark background, light text |
| Modern | Blue accent, clean design |
| Gradient | Gradient backgrounds |

### Export Settings

| Setting | Description |
|---------|-------------|
| Default export path | Where to save exported files |
| Excel template | Custom Excel template |
| DXF units | Drawing units (meters/feet) |
| PDF branding | Company logo for reports |

### Module-Specific Settings

Each module may have its own settings accessible from within the module.

---

## Troubleshooting

### Common Issues

#### Module Not Appearing

**Problem:** A module tile is not showing on the dashboard.

**Solutions:**
1. Check license includes the module
2. Verify module files are in `Modules/` folder
3. Restart FathomOS
4. Check for module compatibility with Shell version

---

#### License Activation Failed

**Problem:** License won't activate.

**Solutions:**
1. Verify license file is valid and not expired
2. Ensure hardware fingerprint matches (for hardware-bound licenses)
3. Check system date is correct
4. Contact vendor for reissued license

---

#### File Won't Open

**Problem:** Cannot open a data file.

**Solutions:**
1. Verify file format is supported
2. Check file is not corrupted
3. Ensure no other application has file locked
4. Try opening manually from File menu

---

#### Export Fails

**Problem:** Export operation fails or produces empty file.

**Solutions:**
1. Verify output path is writable
2. Close any open export files (Excel, etc.)
3. Check disk space
4. Try different export format

---

#### Theme Not Applying

**Problem:** Theme changes don't take effect.

**Solutions:**
1. Restart the module
2. Clear application cache
3. Reset settings to default

---

### Error Messages

| Error | Meaning | Solution |
|-------|---------|----------|
| "License expired" | License validity period ended | Renew license |
| "Hardware mismatch" | Running on different machine | Reactivate license |
| "Module not found" | Module DLL missing | Reinstall FathomOS |
| "Parse error" | Data file format issue | Check file format |
| "Export failed" | Cannot write output | Check permissions |

### Getting Help

#### In-Application Help

1. Press **F1** from any module
2. Click **Help** in the menu bar
3. Use context menus for tooltips

#### Support Contacts

- **Technical Support:** support@fathomos.com
- **Sales Inquiries:** sales@fathomos.com
- **Documentation:** docs.fathomos.com

#### Providing Information for Support

When contacting support, include:
1. FathomOS version (found in Help > About)
2. Module affected
3. Error message (exact text)
4. Steps to reproduce
5. Screenshots if applicable
6. Sample data files (if not confidential)

---

## Keyboard Shortcuts

### Global Shortcuts

| Shortcut | Action |
|----------|--------|
| F1 | Open Help |
| Ctrl + N | New project |
| Ctrl + O | Open project |
| Ctrl + S | Save project |
| Ctrl + Shift + S | Save As |
| Ctrl + , | Settings |
| Alt + F4 | Close application |

### Module-Specific Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl + E | Export |
| Ctrl + P | Print |
| Ctrl + Z | Undo |
| Ctrl + Y | Redo |
| F5 | Refresh/Recalculate |

---

## Glossary

| Term | Definition |
|------|------------|
| **DCC** | Distance to Centerline - perpendicular distance from survey point to route |
| **DXF** | Drawing Exchange Format - CAD file format |
| **KP** | Kilometer Post - distance along route |
| **MRU** | Motion Reference Unit - sensor measuring vessel motion |
| **NPD** | NaviPac Data format - survey data file |
| **RLX** | Route alignment file format |
| **SVP** | Sound Velocity Profile |
| **USBL** | Ultra-Short BaseLine - acoustic positioning system |

---

*Copyright 2026 Fathom OS. All rights reserved.*
