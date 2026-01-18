# FathomOS Installation Guide

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Pre-Installation](#pre-installation)
3. [Installation Methods](#installation-methods)
4. [License Activation](#license-activation)
5. [First Run](#first-run)
6. [Updating FathomOS](#updating-fathomos)
7. [Uninstallation](#uninstallation)
8. [Troubleshooting](#troubleshooting)

---

## System Requirements

### Minimum Requirements

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows 10 (64-bit) version 1903 or later |
| Processor | x64 processor, 2 GHz or faster |
| Memory | 4 GB RAM |
| Storage | 500 MB available disk space |
| Display | 1366 x 768 screen resolution |
| Graphics | DirectX 10 compatible graphics |
| Network | Internet connection for activation |

### Recommended Requirements

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows 11 (64-bit) |
| Processor | x64 processor, 3 GHz or faster, 4+ cores |
| Memory | 8 GB RAM or more |
| Storage | 1 GB available disk space (SSD preferred) |
| Display | 1920 x 1080 screen resolution or higher |
| Graphics | DirectX 12 compatible with 2GB VRAM |
| Network | Stable broadband connection |

### Software Prerequisites

1. **.NET 8.0 Desktop Runtime**
   - Required for application execution
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Select "Windows Desktop Runtime (x64)"

2. **Visual C++ Redistributable 2015-2022**
   - Usually installed with Windows updates
   - Download if needed: https://aka.ms/vs/17/release/vc_redist.x64.exe

---

## Pre-Installation

### Check .NET Installation

Open Command Prompt and run:
```cmd
dotnet --list-runtimes
```

Look for `Microsoft.WindowsDesktop.App 8.0.x` in the output.

### Install .NET Runtime

If not installed:

1. Visit https://dotnet.microsoft.com/download/dotnet/8.0
2. Download ".NET Desktop Runtime 8.0.x (x64)"
3. Run the installer
4. Restart if prompted

### Verify Prerequisites

```cmd
:: Check .NET version
dotnet --info

:: Check system architecture
echo %PROCESSOR_ARCHITECTURE%
```

---

## Installation Methods

### Standard Installation (Recommended)

1. **Download** the FathomOS installer
   - File: `FathomOS-Setup-1.0.45.exe`
   - Size: Approximately 150 MB

2. **Run the installer**
   - Right-click and select "Run as Administrator" if prompted
   - Accept the UAC prompt

3. **Accept License Agreement**
   - Read the End User License Agreement
   - Click "I Agree" to continue

4. **Choose Installation Location**
   - Default: `C:\Program Files\FathomOS`
   - Requires approximately 500 MB

5. **Select Components**
   - All modules selected by default
   - Deselect any modules not needed

6. **Complete Installation**
   - Click "Install"
   - Wait for completion
   - Click "Finish"

### Silent Installation

For enterprise/unattended deployment:

```cmd
:: Basic silent install
FathomOS-Setup.exe /S

:: Silent install with custom location
FathomOS-Setup.exe /S /D=D:\FathomOS

:: Silent install with log
FathomOS-Setup.exe /S /LOG="C:\Temp\fathom_install.log"
```

### Portable Installation

For running from USB or network drives:

1. Download the portable ZIP package
2. Extract to desired location
3. Ensure .NET Runtime is installed on target machine
4. Run `FathomOS.exe` directly

### Network Installation

For multi-user deployments:

1. Install to network share
2. Create shortcuts on client machines
3. Ensure .NET Runtime on all clients
4. Configure shared license file (contact support)

---

## License Activation

### Online Activation

1. Launch FathomOS
2. Click "Activate License" when prompted
3. Enter your license key:
   ```
   XXXXX-XXXXX-XXXXX-XXXXX-XXXXX
   ```
4. Click "Activate"
5. Wait for server verification
6. Activation complete

### Offline Activation

For computers without internet access:

1. Launch FathomOS
2. Click "Offline Activation"
3. Note the **Hardware Fingerprint** displayed
4. On a computer with internet:
   - Visit: https://activate.fathomos.com
   - Enter your license key
   - Enter the hardware fingerprint
   - Receive activation response
5. Back on the offline computer:
   - Paste the activation response
   - Click "Activate"

### Trial Mode

FathomOS can run in trial mode without activation:
- 14-day evaluation period
- Full functionality available
- Watermark on exported documents
- Certificate generation limited

### License Troubleshooting

**"Invalid License Key"**
- Verify key is entered correctly
- Check for extra spaces
- Contact support if key was purchased

**"Hardware Changed"**
- License is tied to hardware fingerprint
- Contact support for license transfer
- May need reactivation

**"Activation Server Unavailable"**
- Check internet connection
- Try again later
- Use offline activation

---

## First Run

### Initial Setup

1. **Launch FathomOS**
   - From Start Menu: FathomOS
   - Or: `C:\Program Files\FathomOS\FathomOS.exe`

2. **Select Theme**
   - Choose from Light, Dark, Modern, or Gradient
   - Can be changed later in Settings

3. **Configure Default Paths**
   - Set default project folder
   - Set default export folder

4. **Dashboard Overview**
   - View available modules
   - Click any module to launch

### Verify Installation

1. Launch FathomOS
2. Open Survey Listing Generator
3. Load a sample NPD file
4. Verify data displays correctly
5. Close module

### Network Time Sync Agent (Optional)

For network time synchronization:

1. Navigate to `C:\Program Files\FathomOS\Agent`
2. Run `FathomOS.TimeSyncAgent.exe` as Administrator
3. Configure as Windows Service if desired

---

## Updating FathomOS

### Automatic Updates

FathomOS checks for updates on startup:

1. Notification appears when update available
2. Click "Download Update"
3. Close application when prompted
4. Installer runs automatically
5. Application restarts

### Manual Update

1. Download latest installer
2. Run installer
3. Existing settings are preserved
4. Modules are updated automatically

### Update Settings

Configure update behavior in Settings:
- Check for updates automatically
- Download updates automatically
- Notify before installing

---

## Uninstallation

### Using Windows Settings

1. Open Windows Settings
2. Go to Apps > Installed apps
3. Find "FathomOS"
4. Click "Uninstall"
5. Confirm removal

### Using Control Panel

1. Open Control Panel
2. Go to Programs > Uninstall a program
3. Find "FathomOS"
4. Double-click to uninstall

### Complete Removal

After uninstallation, remove user data:

```cmd
:: Remove application data
rmdir /s /q "%APPDATA%\FathomOS"

:: Remove local data
rmdir /s /q "%LOCALAPPDATA%\FathomOS"
```

---

## Troubleshooting

### Application Won't Start

**Symptom:** Double-click does nothing or error appears

**Solutions:**
1. Verify .NET 8.0 Desktop Runtime is installed
2. Run as Administrator
3. Check Windows Event Viewer for errors
4. Reinstall application

### Missing Dependencies

**Symptom:** Error about missing DLL

**Solutions:**
1. Install Visual C++ Redistributable
2. Repair .NET installation
3. Reinstall FathomOS

### Permission Errors

**Symptom:** Cannot write to installation folder

**Solutions:**
1. Run as Administrator
2. Install to user-writable location
3. Check folder permissions

### License Issues

**Symptom:** Activation fails repeatedly

**Solutions:**
1. Check internet connection
2. Disable VPN temporarily
3. Try offline activation
4. Contact support

### Module Not Loading

**Symptom:** Module tile missing or error on launch

**Solutions:**
1. Check module DLL exists in Modules folder
2. Verify module dependencies
3. Check application log
4. Reinstall specific module

### Log File Location

Application logs are stored at:
```
%APPDATA%\FathomOS\Logs\
```

Include log files when contacting support.

---

## Support

### Contact Information

- **Email:** support@fathomos.com
- **Website:** https://fathomos.com/support
- **Documentation:** https://docs.fathomos.com

### When Contacting Support

Please include:
- FathomOS version number
- Windows version
- License key (last 4 characters only)
- Description of issue
- Steps to reproduce
- Log files if applicable

---

## Related Documentation

- [Configuration Guide](Configuration.md)
- [Getting Started](../GettingStarted.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
