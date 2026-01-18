# Network Time Sync Module

**Module ID:** NetworkTimeSync
**Version:** 2.0.0
**Category:** Utilities
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Network Discovery](#network-discovery)
6. [Time Synchronization](#time-synchronization)
7. [GPS Time Source](#gps-time-source)
8. [Configuration Options](#configuration-options)
9. [Reporting](#reporting)

---

## Overview

The Network Time Sync module discovers computers on the network, monitors their time offsets, and synchronizes time across all machines. This is critical for survey operations where multiple computers must maintain synchronized time for data correlation.

### Primary Use Cases

- Discover all computers on survey network
- Monitor time drift across network
- Synchronize time to a reference source
- Generate time sync reports for QC
- GPS time source integration

---

## Features

### Network Discovery
- Automatic network scanning
- Subnet-based discovery
- Agent-based status monitoring
- Real-time connection status

### Time Monitoring
- Continuous offset monitoring
- Historical offset tracking
- Drift rate calculation
- Threshold alerting

### Synchronization
- One-click sync to all computers
- Selective computer sync
- GPS time source option
- NTP server support

### Reporting
- Time sync status report
- Historical offset charts
- Compliance verification
- PDF export

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.nts` | Time sync configuration |
| `.timesync` | Time sync session file |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel time report |
| `.pdf` | PDF status report |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Network] [Sync] [Reports] [Help]               [Theme]   |
+------------------------------------------------------------------+
|                                                                   |
|  Network Computers                                                |
|  +------------------------------------------------------------+ |
|  | Name          | IP Address    | Status    | Offset   | Last | |
|  +------------------------------------------------------------+ |
|  | SURVEY-PC-01  | 192.168.1.10  | Online    | +0.002s  | 5s   | |
|  | NAV-PC        | 192.168.1.11  | Online    | +0.125s  | 5s   | |
|  | LOGGING-PC    | 192.168.1.12  | Online    | -0.045s  | 5s   | |
|  | OFFLINE-PC    | 192.168.1.20  | Offline   | N/A      | 2m   | |
|  +------------------------------------------------------------+ |
|                                                                   |
|  Time Source: [GPS Receiver on COM3]  Reference: 14:35:22.456   |
|                                                                   |
|  [Discover Network]  [Sync All]  [Generate Report]               |
+------------------------------------------------------------------+
| Status: 3 computers online | Max offset: 0.125s | GPS: Locked    |
+------------------------------------------------------------------+
```

### Dashboard Cards

- **Computers Online** - Count of reachable computers
- **Max Offset** - Largest time difference
- **GPS Status** - Lock status and satellite count
- **Last Sync** - Time of last synchronization

### Screenshot Placeholder

*[Screenshot: Network Time Sync dashboard showing discovered computers with time offsets and status indicators]*

---

## Network Discovery

### Discovery Methods

1. **Broadcast Discovery** - Sends broadcast packets to discover agents
2. **Subnet Scan** - Scans IP range for responsive agents
3. **Manual Add** - Manually specify computer IP/hostname

### Agent Protocol

The module communicates with agents installed on network computers:

```
Agent Commands:
- GET_TIME     - Request current system time
- GET_STATUS   - Request computer status
- SYNC_TIME    - Synchronize to provided time
- PING         - Check agent availability
```

### Discovery Process

1. Select network interface or subnet
2. Click "Discover Network"
3. Wait for scan completion (10-30 seconds)
4. Review discovered computers
5. Optionally add missing computers manually

---

## Time Synchronization

### Synchronization Process

1. **Get Reference Time** - From GPS or NTP server
2. **Calculate Offsets** - For each computer
3. **Send Sync Command** - To all selected computers
4. **Verify Sync** - Confirm time updated
5. **Log Results** - Record sync event

### Sync Options

| Option | Description |
|--------|-------------|
| Sync All | Synchronize all online computers |
| Sync Selected | Synchronize selected computers only |
| Scheduled Sync | Automatic sync at intervals |
| Threshold Sync | Sync only if offset exceeds threshold |

### Offset Calculation

```
Offset = (T_remote - T_reference) - (RTT / 2)

Where:
- T_remote = Time reported by remote computer
- T_reference = Reference time source
- RTT = Round-trip time for communication
```

---

## GPS Time Source

### Supported Devices

- Serial GPS receivers (NMEA 0183)
- USB GPS receivers
- Network GPS time servers

### GPS Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| COM Port | Serial port for GPS | Auto-detect |
| Baud Rate | Serial baud rate | 4800 |
| NMEA Sentences | Sentences to parse | $GPRMC, $GPZDA |
| PPS Enable | Use pulse-per-second | If available |

### GPS Status Indicators

| Status | Description |
|--------|-------------|
| Searching | Looking for satellites |
| 2D Fix | Position only, time may be inaccurate |
| 3D Fix | Full fix, reliable time |
| DGPS | Differential correction applied |

---

## Configuration Options

### Network Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Discovery Subnet | Network to scan | Auto-detect |
| Agent Port | Communication port | 5555 |
| Discovery Timeout | Scan timeout (ms) | 5000 |
| Status Interval | Update interval (s) | 5 |

### Synchronization Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Time Source | GPS / NTP / Local | GPS |
| NTP Server | NTP server address | time.google.com |
| Offset Threshold | Acceptable offset (s) | 0.1 |
| Sync Interval | Auto-sync interval (min) | 0 (disabled) |

### Alerting Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Enable Alerts | Show offset warnings | Yes |
| Warning Threshold | Warning level (s) | 0.5 |
| Critical Threshold | Critical level (s) | 1.0 |
| Alert Sound | Play sound on alert | No |

---

## Reporting

### Status Report

Generates a comprehensive report including:

- Report header (date, reference, operator)
- Computer list with final offsets
- Sync history summary
- Compliance status (Pass/Fail)
- Recommendations

### Historical Data

Track time offsets over time:

- Hourly offset averages
- Drift rate trends
- Sync event log
- Offline periods

### Export Options

| Format | Content |
|--------|---------|
| PDF | Formatted status report |
| Excel | Raw data with charts |
| CSV | Data for external analysis |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | NetworkTimeSync |
| Certificate Code | NTS |
| Computers Synced | Count of synced machines |
| Reference Source | GPS/NTP/Local |
| Max Offset Before | Largest offset before sync |
| Max Offset After | Largest offset after sync |
| Sync Time | Timestamp of synchronization |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| F5 | Refresh Status |
| F9 | Start/Stop Discovery |
| Ctrl+S | Sync All |
| Ctrl+R | Generate Report |
| Del | Remove Computer |

---

## Troubleshooting

### Discovery Issues

**No Computers Found**
- Verify network connectivity
- Check firewall settings
- Ensure agents are installed and running
- Try manual IP addition

**Agent Connection Failed**
- Verify agent is running on remote computer
- Check agent port (default 5555)
- Verify network route exists

### GPS Issues

**No GPS Lock**
- Check antenna connection
- Move antenna to clear sky view
- Verify COM port settings
- Try different baud rate

**GPS Time Incorrect**
- Ensure GPS has 3D fix
- Check for date/time format issues
- Verify time zone settings

---

## Related Documentation

- [Survey Logbook](SurveyLogbook.md)
- [Installation Guide](../Deployment/Installation.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
