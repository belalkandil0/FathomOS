# Survey Logbook Module

**Module ID:** SurveyLogbook
**Version:** 11.0.0
**Category:** Data Processing
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Real-Time Integration](#real-time-integration)
6. [Log Entry Types](#log-entry-types)
7. [DPR and Shift Handover](#dpr-and-shift-handover)
8. [Configuration Options](#configuration-options)
9. [Export Formats](#export-formats)

---

## Overview

The Survey Logbook module provides an electronic survey logging solution with real-time integration capabilities. It captures survey events, position fixes, DVR recordings, and waypoint changes, enabling comprehensive documentation of survey operations.

### Primary Use Cases

- Real-time survey event logging
- Position fix documentation from calibration files
- DVR recording tracking and linking
- Daily Progress Report (DPR) generation
- Shift handover documentation
- Survey timeline management

---

## Features

### Real-Time Logging
- TCP connection to NaviPac for live data
- File monitoring for automatic log updates
- Timestamp synchronization
- Auto-save functionality

### Position Fix Tracking
- Parse NPC calibration files
- Manual position fix entry
- Position verification logging
- Fix quality indicators

### DVR Recording Management
- Monitor VisualWorks folder structure
- Automatic recording detection
- Link recordings to log entries
- Duration and timestamp tracking

### Waypoint Monitoring
- Parse WP2 waypoint files
- Detect waypoint changes
- Log waypoint events automatically
- Waypoint history tracking

### Report Generation
- Daily Progress Reports (DPR)
- Shift Handover Reports
- Custom report templates
- Excel and PDF export

---

## Supported File Types

### Input Files

| Extension | Description | Auto-Monitor |
|-----------|-------------|--------------|
| `.slog` | Survey log file (JSON) | No |
| `.slogz` | Compressed survey log | No |
| `.npc` | NaviPac calibration file | Yes |
| `.wp2` | Waypoint file | Yes |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.slog` | Survey log (uncompressed) |
| `.slogz` | Survey log (GZip compressed) |
| `.xlsx` | Excel export |
| `.pdf` | PDF report |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Edit] [View] [Connection] [Reports] [Help]    [Theme]    |
+------------------------------------------------------------------+
| Tabs: [Survey Log] [Position Fixes] [DVR Recordings] [DPR]       |
+------------------------------------------------------------------+
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Quick Actions     |  | Log Entry List                       | |
|  |                   |  |                                      | |
|  | [+ New Entry]     |  | Time     | Type    | Description     | |
|  | [+ Position Fix]  |  | 08:00:00 | Start   | Survey started  | |
|  | [+ Note]          |  | 08:15:32 | Fix     | Position fix #1 | |
|  |                   |  | 09:30:00 | Event   | Line change     | |
|  | Connection:       |  | ...                                   | |
|  | [Connected]       |  |                                      | |
|  |                   |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
+------------------------------------------------------------------+
| Status: Connected to NaviPac | Entries: 156 | Last: 09:45:22     |
+------------------------------------------------------------------+
```

### Tab Views

1. **Survey Log** - Main event log view
2. **Position Fixes** - Position fix entries
3. **DVR Recordings** - Video recording log
4. **DPR** - Daily Progress Report editor

### Screenshot Placeholder

*[Screenshot: Survey Logbook main window showing real-time log entries with NaviPac connection status]*

---

## Real-Time Integration

### NaviPac TCP Connection

The module can connect to NaviPac via TCP to receive real-time data:

```
Connection Settings:
- Host: 192.168.1.100
- Port: 6000
- Protocol: NaviPac Standard
- Reconnect: Automatic
```

### Data Received
- Current position (Easting, Northing)
- Depth values
- Heading
- Status messages
- Event notifications

### File Monitoring

The module monitors specified folders for changes:

| Monitor Type | Folder | File Pattern |
|--------------|--------|--------------|
| NPC Files | Configurable | *.npc |
| Waypoints | Configurable | *.wp2 |
| DVR Recordings | VisualWorks | Subdirectories |

### Firewall Configuration

The module includes a firewall management service to configure Windows Firewall rules for TCP connections automatically.

---

## Log Entry Types

### Standard Entry Types

| Type | Icon | Description |
|------|------|-------------|
| Start | Play | Survey/line start |
| End | Stop | Survey/line end |
| Position Fix | Pin | Position verification |
| Waypoint | Flag | Waypoint change |
| Event | Info | General event |
| Note | Note | Manual note entry |
| Recording | Video | DVR recording reference |
| Alarm | Warning | System alarm |
| Equipment | Gear | Equipment status |

### Entry Fields

Each log entry contains:
- Timestamp (auto or manual)
- Entry type
- Description/comment
- Position (optional)
- Depth (optional)
- Linked files/recordings
- User ID (if authenticated)

### Custom Fields

Users can define custom fields for entries:
- Text fields
- Numeric fields
- Dropdown selections
- Checkbox options

---

## DPR and Shift Handover

### Daily Progress Report (DPR)

Generate comprehensive daily reports including:

**Header Information:**
- Project/vessel details
- Date and weather
- Crew on shift
- Overall status

**Operations Summary:**
- Survey lines completed
- Distance covered
- Notable events
- Equipment issues

**Time Breakdown:**
- Survey time
- Transit time
- Downtime (categorized)
- Standby time

### Shift Handover

Document shift changes with:
- Outgoing shift summary
- Current operation status
- Pending tasks
- Safety notes
- Equipment status
- Crew roster

---

## Configuration Options

### Connection Settings

| Setting | Description | Default |
|---------|-------------|---------|
| NaviPac Host | TCP server IP | localhost |
| NaviPac Port | TCP port | 6000 |
| Auto Reconnect | Reconnect on disconnect | Yes |
| Reconnect Interval | Seconds between attempts | 5 |

### Monitoring Settings

| Setting | Description | Default |
|---------|-------------|---------|
| NPC Folder | Path to NPC files | (User configured) |
| Waypoint Folder | Path to WP2 files | (User configured) |
| DVR Folder | VisualWorks root | (User configured) |
| Monitor Interval | Check interval (ms) | 1000 |

### Auto-Save Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Auto Save | Enable auto-save | Yes |
| Save Interval | Minutes between saves | 5 |
| Compress Logs | Use GZIP compression | Yes |
| Backup Count | Number of backups | 5 |

---

## Export Formats

### Excel Export

Generates spreadsheets with:
- Log entries with all fields
- Position fixes summary
- DVR recordings list
- Time analysis charts
- Custom formatting

### PDF Report

Creates formatted reports with:
- Project header
- Chronological log
- Statistics summary
- Signature blocks

### Data Interchange

Export raw data for:
- Integration with other systems
- Backup and archive
- Custom processing

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | SurveyLogbook |
| Certificate Code | SLB |
| Log File | Source log file name |
| Total Entries | Number of log entries |
| Date Range | Start to end date |
| Position Fixes | Number of fixes logged |
| DVR Recordings | Number of recordings |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Log Entry |
| Ctrl+F | New Position Fix |
| Ctrl+O | Open Log File |
| Ctrl+S | Save Log |
| Ctrl+E | Export |
| F5 | Refresh |
| F9 | Toggle Connection |
| Del | Delete Entry |

---

## Troubleshooting

### Connection Issues

**NaviPac Won't Connect**
- Verify IP address and port
- Check firewall settings
- Ensure NaviPac is broadcasting
- Try manual firewall rule

**File Monitoring Not Working**
- Verify folder paths exist
- Check folder permissions
- Restart monitoring service

### Data Issues

**Missing Entries**
- Check auto-save is enabled
- Verify log file isn't corrupted
- Check available disk space

---

## Related Documentation

- [Getting Started](../GettingStarted.md)
- [Network Time Sync](NetworkTimeSync.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
