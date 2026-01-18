# FathomOS Configuration Guide

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Application Settings](#application-settings)
3. [User Preferences](#user-preferences)
4. [Module Configuration](#module-configuration)
5. [Network Configuration](#network-configuration)
6. [Certificate Configuration](#certificate-configuration)
7. [Advanced Settings](#advanced-settings)
8. [Configuration Files](#configuration-files)
9. [Troubleshooting](#troubleshooting)

---

## Overview

FathomOS configuration is managed at multiple levels:
- **Application Settings** - Global application behavior
- **User Preferences** - Per-user customizations
- **Module Configuration** - Module-specific settings

### Settings Storage Locations

| Type | Location |
|------|----------|
| Application | `%ProgramData%\FathomOS\` |
| User | `%AppData%\FathomOS\` |
| Local Cache | `%LocalAppData%\FathomOS\` |

---

## Application Settings

### Accessing Application Settings

1. Launch FathomOS
2. Click the Settings icon (gear) in top-right
3. Or use keyboard shortcut: `Ctrl+,`

### General Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Theme | UI color theme | Dark |
| Language | Interface language | English |
| Check for Updates | Auto-check on startup | Yes |
| Send Analytics | Anonymous usage data | No |
| Startup Behavior | What to show on launch | Dashboard |

### Path Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Default Project Folder | Where to save projects | Documents\FathomOS\Projects |
| Default Export Folder | Where to save exports | Documents\FathomOS\Exports |
| Default Import Folder | Initial folder for file open | Documents |
| Template Folder | Custom templates location | AppData\FathomOS\Templates |

### Performance Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Hardware Acceleration | Use GPU rendering | Yes |
| Max Memory Usage | Memory limit (MB) | Auto |
| Cache Size | Disk cache limit (MB) | 500 |
| Background Threads | Worker thread count | Auto |

---

## User Preferences

### Theme Configuration

Select from four themes:

```
Settings > Appearance > Theme
  - Light
  - Dark (default)
  - Modern
  - Gradient
```

Each theme affects:
- Window backgrounds
- Text colors
- Control styles
- Chart colors

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Font Size | UI text size | Medium |
| High Contrast | Enhanced contrast mode | Off |
| Animation | UI animations | On |
| Dashboard Layout | Tile arrangement | Auto |

### Default Signatory

Configure default values for certificates:

```
Settings > Certificates > Default Signatory
  - Name: [Your Name]
  - Title: [Your Title]
  - Company: [Company Name]
  - [ ] Save as default
```

### File Associations

Configure which files open in FathomOS:

```
Settings > File Associations
  [x] .npd - NaviPac Data
  [x] .rlx - Route Alignment
  [x] .s7p - FathomOS Project
  [x] .slog - Survey Log
  [ ] .csv - CSV Files
```

---

## Module Configuration

### Survey Listing Settings

```
Module Settings > Survey Listing
  - Default Column Mapping
    - Time Pattern: "Time"
    - Easting Pattern: "East"
    - Northing Pattern: "North"
  - Export Options
    - Include Charts: Yes
    - Default Format: Excel
  - Processing
    - Default Smoothing: None
    - Outlier Threshold: 3-sigma
```

### Survey Logbook Settings

```
Module Settings > Survey Logbook
  - Connection
    - NaviPac Host: localhost
    - NaviPac Port: 6000
    - Auto Reconnect: Yes
  - Monitoring
    - NPC Folder: [path]
    - DVR Folder: [path]
    - Monitor Interval: 1000ms
  - Auto-Save
    - Enabled: Yes
    - Interval: 5 minutes
```

### Network Time Sync Settings

```
Module Settings > Network Time Sync
  - Discovery
    - Subnet: Auto-detect
    - Agent Port: 5555
    - Timeout: 5000ms
  - GPS
    - COM Port: Auto
    - Baud Rate: 4800
  - Sync
    - Source: GPS
    - Threshold: 0.1s
```

### Equipment Inventory Settings

```
Module Settings > Equipment Inventory
  - Sync
    - Auto Sync: Yes
    - Interval: 15 minutes
    - API Endpoint: [configured]
  - Alerts
    - Cert Warning Days: 30
    - Maintenance Reminder: 14
```

---

## Network Configuration

### Proxy Settings

If behind a corporate proxy:

```
Settings > Network > Proxy
  - Use System Proxy: [Yes/No]
  - Manual Configuration:
    - Host: proxy.company.com
    - Port: 8080
    - Username: [optional]
    - Password: [optional]
```

### Firewall Configuration

FathomOS requires the following ports:

| Port | Protocol | Purpose |
|------|----------|---------|
| 443 | HTTPS | License activation, cloud sync |
| 6000 | TCP | NaviPac connection |
| 5555 | TCP/UDP | Time sync agent |

### Offline Mode

Configure behavior when offline:

```
Settings > Network > Offline Mode
  - Queue certificate sync: Yes
  - Cache equipment data: Yes
  - Show offline indicator: Yes
```

---

## Certificate Configuration

### Certificate Settings

```
Settings > Certificates
  - Storage
    - Local Database: %AppData%\FathomOS\Certificates\
    - Backup Location: [optional path]
  - Sync
    - Auto Sync to Cloud: Yes
    - Sync Interval: On creation
  - Display
    - Show Certificate After Creation: Yes
    - Include in Export: Yes
```

### Branding

License-based branding configuration:

```
Settings > Certificates > Branding
  - Company Logo: [from license]
  - Company Name: [from license]
  - Custom Footer: [optional]
```

### Certificate Numbering

Certificate IDs follow the pattern: `{Module}-{Year}-{Sequence}`

Example sequences:
- SL-2026-000001
- GC-2026-000001
- MRU-2026-000001

---

## Advanced Settings

### Logging

Configure application logging:

```
Settings > Advanced > Logging
  - Log Level: [Debug/Info/Warning/Error]
  - Log Location: %AppData%\FathomOS\Logs\
  - Max Log Size: 10 MB
  - Log Retention: 30 days
```

### Diagnostics

```
Settings > Advanced > Diagnostics
  - Enable Debug Mode: No
  - Show Developer Tools: No
  - Performance Monitoring: No
```

### Reset Settings

```
Settings > Advanced > Reset
  - Reset All Settings: [Restore defaults]
  - Clear Cache: [Remove cached data]
  - Reset Window Positions: [Restore layout]
```

---

## Configuration Files

### Settings File

Main settings stored in JSON:

```
%AppData%\FathomOS\settings.json
```

Example structure:
```json
{
  "theme": "Dark",
  "language": "en-US",
  "checkForUpdates": true,
  "defaultProjectPath": "C:\\Users\\User\\Documents\\FathomOS\\Projects",
  "defaultExportPath": "C:\\Users\\User\\Documents\\FathomOS\\Exports",
  "recentProjects": [
    "C:\\Projects\\Survey1.s7p",
    "C:\\Projects\\Survey2.s7p"
  ],
  "defaultSignatory": {
    "name": "John Smith",
    "title": "Survey Engineer",
    "company": "Survey Corp"
  }
}
```

### Module Settings

Per-module settings:

```
%AppData%\FathomOS\{ModuleId}\settings.json
```

### License File

```
%ProgramData%\FathomOS\license.dat
```

### Certificate Database

```
%AppData%\FathomOS\Certificates\certificates.db
```

---

## Environment Variables

### Available Variables

| Variable | Description |
|----------|-------------|
| FATHOMOS_HOME | Installation directory |
| FATHOMOS_DATA | User data directory |
| FATHOMOS_LOG_LEVEL | Override log level |
| FATHOMOS_OFFLINE | Force offline mode |

### Setting Variables

```cmd
:: Temporary (current session)
set FATHOMOS_LOG_LEVEL=Debug

:: Permanent (system)
setx FATHOMOS_LOG_LEVEL Debug /M
```

---

## Command Line Options

### Available Options

```
FathomOS.exe [options]

Options:
  --project <path>     Open specified project
  --module <id>        Launch specified module
  --theme <name>       Override theme (Light/Dark/Modern/Gradient)
  --offline            Start in offline mode
  --reset              Reset to default settings
  --log-level <level>  Set logging level
  --help               Show help information
```

### Examples

```cmd
:: Open with specific project
FathomOS.exe --project "C:\Projects\Survey.s7p"

:: Launch specific module
FathomOS.exe --module SurveyListing

:: Debug mode with verbose logging
FathomOS.exe --log-level Debug
```

---

## Troubleshooting

### Settings Not Saving

**Symptom:** Changes don't persist after restart

**Solutions:**
1. Check write permissions to AppData folder
2. Run as Administrator once
3. Check disk space
4. Reset settings and reconfigure

### Performance Issues

**Symptom:** Application runs slowly

**Solutions:**
1. Disable hardware acceleration (if GPU issues)
2. Reduce max memory setting
3. Clear cache
4. Disable animations

### Module Configuration Lost

**Symptom:** Module settings reset

**Solutions:**
1. Check module settings file exists
2. Verify JSON file is not corrupted
3. Restore from backup
4. Reconfigure module

### Connection Problems

**Symptom:** Cannot connect to services

**Solutions:**
1. Check firewall settings
2. Verify proxy configuration
3. Test network connectivity
4. Try offline mode

### Reset All Configuration

To completely reset FathomOS:

```cmd
:: Close FathomOS first

:: Remove user settings
rmdir /s /q "%APPDATA%\FathomOS"

:: Remove local cache
rmdir /s /q "%LOCALAPPDATA%\FathomOS"

:: Keep license - only remove settings
:: Do NOT delete %ProgramData%\FathomOS unless reinstalling
```

---

## Related Documentation

- [Installation Guide](Installation.md)
- [Getting Started](../GettingStarted.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
