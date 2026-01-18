# Getting Started with FathomOS

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Installation](#installation)
3. [Activation](#activation)
4. [First Launch](#first-launch)
5. [Quick Tutorial](#quick-tutorial)
6. [Common Tasks](#common-tasks)

---

## System Requirements

### Minimum Requirements

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows 10 (64-bit) or Windows 11 |
| Runtime | .NET 8.0 Desktop Runtime |
| Memory | 4 GB RAM |
| Storage | 500 MB available space |
| Display | 1366 x 768 resolution |
| Network | Not required (offline-first licensing) |

### Recommended Requirements

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows 11 (64-bit) |
| Runtime | .NET 8.0 Desktop Runtime |
| Memory | 8 GB RAM or more |
| Storage | 1 GB available space (SSD preferred) |
| Display | 1920 x 1080 resolution or higher |
| Network | Optional (for certificate sync) |

### Software Prerequisites

1. **.NET 8.0 Desktop Runtime**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Select "Windows Desktop Runtime" (x64)

2. **Visual C++ Redistributable** (if not already installed)
   - Typically installed with Windows updates

---

## Installation

### Standard Installation

1. **Download** the FathomOS installer package
2. **Run** the installer (`FathomOS-Setup.exe`)
3. **Accept** the license agreement
4. **Choose** installation location (default: `C:\Program Files\FathomOS`)
5. **Select** modules to install (all selected by default)
6. **Complete** installation

### Silent Installation

For enterprise deployment:

```cmd
FathomOS-Setup.exe /S /D=C:\FathomOS
```

### Portable Installation

FathomOS can run from a portable location:

1. Extract the ZIP package to any folder
2. Ensure .NET 8.0 Runtime is installed
3. Run `FathomOS.exe` directly

---

## Activation

FathomOS uses an **offline-first licensing system**. No internet connection is required for license activation or validation.

### Getting Your License

Contact your vendor to obtain one of the following:
- **License file** (`.lic` extension) - JSON format containing license data
- **License key string** - Format: `FATHOM-XXX-XXX-XXX-XXX`

### Activating with a License File

1. Launch FathomOS
2. When prompted, click "Load License File"
3. Browse to and select your `.lic` file
4. FathomOS verifies the license signature automatically
5. Proceed to create your local admin account

### Activating with a License Key

1. Launch FathomOS
2. When prompted, click "Enter License Key"
3. Enter your license key (e.g., `FATHOM-PRO-A1B2-C3D4-E5F6`)
4. Click "Activate"
5. FathomOS verifies the license signature automatically
6. Proceed to create your local admin account

### Creating Your Local Admin Account

After license activation, create your local administrator account:

1. Enter your desired username
2. Enter a strong password (minimum 8 characters)
3. Confirm your password
4. Click "Create Account"

This account is stored locally and does not require any server connection.

### Trial Mode

FathomOS can run in trial mode for evaluation:
- 14-day trial period
- Full functionality available
- Watermark on exports
- No license file required

---

## First Launch

### Dashboard Overview

When FathomOS starts, you see the main dashboard:

```
+------------------------------------------------------------------+
|  FATHOM OS                                    [User] [Settings]   |
+------------------------------------------------------------------+
|                                                                   |
|  DATA PROCESSING                                                  |
|  +------------------+  +------------------+  +------------------+ |
|  | Survey Listing   |  | Survey Logbook   |  | Sound Velocity   | |
|  | Generator        |  |                  |  | Profile          | |
|  +------------------+  +------------------+  +------------------+ |
|                                                                   |
|  CALIBRATIONS                                                     |
|  +------------------+  +------------------+  +------------------+ |
|  | GNSS Calibration |  | MRU Calibration  |  | USBL Verification| |
|  +------------------+  +------------------+  +------------------+ |
|                                                                   |
|  UTILITIES                                                        |
|  +------------------+  +------------------+                       |
|  | Network Time     |  | Equipment        |                       |
|  | Sync             |  | Inventory        |                       |
|  +------------------+  +------------------+                       |
|                                                                   |
+------------------------------------------------------------------+
```

### Navigation

- **Click** a module tile to launch it
- **Right-click** for module options
- **Drag and drop** files to open them in the appropriate module
- Use **keyboard shortcuts** (Ctrl+1 through Ctrl+9) to launch modules

### Theme Selection

1. Click the **Settings** icon (gear) in the top-right
2. Select **Theme**
3. Choose from:
   - Light
   - Dark (default)
   - Modern
   - Gradient

---

## Quick Tutorial

### Creating a Survey Listing

This tutorial walks through creating a basic survey listing.

#### Step 1: Launch Survey Listing Generator

Click the "Survey Listing Generator" tile on the dashboard.

#### Step 2: Create a New Project

1. Click **File > New Project** or press **Ctrl+N**
2. Enter project details:
   - Project Name: "Test Survey"
   - Client: "Example Client"
   - Vessel: "MV Survey Vessel"
3. Click **Next**

#### Step 3: Load Route File (Optional)

1. Click **Browse** to select an RLX route file
2. Or skip this step if no route alignment is needed
3. Click **Next**

#### Step 4: Load Survey Data

1. Click **Add Files** to select NPD files
2. Preview the data in the table
3. Verify column mapping is correct
4. Click **Next**

#### Step 5: Configure Tide Corrections (Optional)

1. Click **Browse** to load tide data
2. Set tide application options
3. Or skip if no tide correction needed
4. Click **Next**

#### Step 6: Process Data

1. Review processing options
2. Click **Process**
3. Wait for processing to complete

#### Step 7: Review Results

1. Check the data in the results table
2. View charts and statistics
3. Identify any issues

#### Step 8: Export

1. Click **Export**
2. Select output formats:
   - Excel
   - DXF/CAD
   - PDF Report
3. Choose output location
4. Click **Export**

#### Step 9: Generate Certificate

1. Click **Create Certificate**
2. Enter signatory information
3. Review certificate preview
4. Click **Generate**

---

## Common Tasks

### Opening Files

**Method 1: Drag and Drop**
- Drag NPD, RLX, or project files onto the dashboard
- FathomOS opens the appropriate module automatically

**Method 2: File Associations**
- Double-click FathomOS file types in Explorer
- Files open in the associated module

**Method 3: From Module**
- Launch the module
- Use File > Open or drag files to the module window

### Managing Certificates

1. Click **View > Certificates** from any module
2. Browse generated certificates
3. Double-click to view details
4. Export as PDF if needed

### Changing Settings

**Application Settings**
1. Click Settings icon on dashboard
2. Configure:
   - Theme
   - Default paths
   - Certificate options
   - Network settings

**Module Settings**
- Each module has its own settings
- Access via the module's Settings menu or gear icon

### Getting Help

- Press **F1** in any module for context-sensitive help
- Click **Help > Documentation** to open this documentation
- Click **Help > About** for version information

---

## Next Steps

- Read the [Module Documentation](Modules/) for specific module guides
- Review the [Developer Guide](DeveloperGuide.md) if creating custom modules
- Check [Configuration](Deployment/Configuration.md) for advanced settings

---

## Troubleshooting

### Application Won't Start

1. Verify .NET 8.0 Desktop Runtime is installed
2. Run as Administrator if permission errors occur
3. Check Windows Event Viewer for errors

### License Activation Fails

1. Verify the license file is not corrupted (open in text editor to check JSON)
2. Verify license key is entered correctly
3. Ensure system clock is accurate (affects expiration check)
4. If hardware-bound, contact vendor for new license if hardware changed
5. Check that the license has not expired

### Module Not Appearing

1. Check module is included in installation
2. Verify module DLL exists in Modules folder
3. Check for error messages in application log

### Export Errors

1. Ensure output path is writable
2. Close any open output files
3. Check disk space availability

---

*Copyright 2026 Fathom OS. All rights reserved.*
