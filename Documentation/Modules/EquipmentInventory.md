# Equipment Inventory Module

**Module ID:** EquipmentInventory
**Version:** 1.0.0
**Category:** Utilities
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Equipment Management](#equipment-management)
6. [Certification Tracking](#certification-tracking)
7. [Maintenance Scheduling](#maintenance-scheduling)
8. [Manifests and Shipping](#manifests-and-shipping)
9. [Configuration Options](#configuration-options)
10. [API Integration](#api-integration)

---

## Overview

The Equipment Inventory module provides comprehensive tracking of survey equipment including calibration certificates, maintenance schedules, location tracking, and shipping manifests. It supports offline operation with cloud synchronization when connected.

### Primary Use Cases

- Track survey equipment across multiple vessels/locations
- Monitor calibration certificate expiry dates
- Schedule and record maintenance activities
- Generate shipping manifests
- QR code label generation for equipment tagging

---

## Features

### Equipment Tracking
- Complete equipment database
- Custom equipment categories
- Serial number tracking
- Location hierarchy (Company > Vessel > Room > Shelf)

### Certification Management
- Certificate tracking with expiry alerts
- Document attachment storage
- Calibration history
- Automatic expiry notifications

### Maintenance Management
- Scheduled maintenance tasks
- Maintenance history logging
- Preventive maintenance alerts
- Work order generation

### Location Management
- Hierarchical location structure
- Equipment transfer logging
- Location-based reporting
- Barcode/QR scanning support

### Shipping and Manifests
- Create shipping manifests
- Track equipment movements
- Delivery verification
- Manifest history

### Reporting
- Equipment inventory reports
- Certification status reports
- Maintenance due reports
- Custom report builder

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel import for bulk data |
| `.csv` | CSV import |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel inventory export |
| `.pdf` | PDF reports and manifests |
| `.csv` | CSV data export |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Edit] [Equipment] [Locations] [Reports] [Help]  [Theme]  |
+------------------------------------------------------------------+
| Sidebar:            | Main Content:                               |
| +------------------+ +-------------------------------------------+|
| | Navigation       | | Equipment List / Details                   ||
| |                  | |                                           ||
| | - Dashboard      | | [Search: _______________] [+ Add]         ||
| | - Equipment      | |                                           ||
| | - Locations      | | ID     | Name        | Location | Status  ||
| | - Certifications | | E-001  | CTD Sensor  | Vessel A | OK      ||
| | - Maintenance    | | E-002  | USBL Head   | Vessel A | Alert   ||
| | - Manifests      | | E-003  | ROV #1      | Vessel B | OK      ||
| | - Reports        | |                                           ||
| | - Settings       | |                                           ||
| +------------------+ +-------------------------------------------+|
+------------------------------------------------------------------+
| Status: 156 items | 3 alerts | Last sync: 5 min ago               |
+------------------------------------------------------------------+
```

### Dashboard View

- Equipment by location chart
- Certification expiry timeline
- Maintenance due list
- Recent activity feed

### Screenshot Placeholder

*[Screenshot: Equipment Inventory dashboard showing equipment counts, certification alerts, and upcoming maintenance]*

---

## Equipment Management

### Equipment Record Fields

| Field | Required | Description |
|-------|----------|-------------|
| Equipment ID | Yes | Unique identifier |
| Name | Yes | Equipment name |
| Serial Number | No | Manufacturer serial |
| Category | Yes | Equipment type |
| Manufacturer | No | Manufacturer name |
| Model | No | Model number |
| Location | Yes | Current location |
| Status | Yes | Operational status |
| Notes | No | Additional notes |

### Equipment Status Values

| Status | Description |
|--------|-------------|
| Operational | Ready for use |
| In Maintenance | Currently being serviced |
| Needs Calibration | Calibration due or overdue |
| Defective | Not operational |
| In Transit | Being shipped |
| Retired | No longer in service |

### Equipment Categories

- Navigation Equipment
- Acoustic Systems
- ROV/AUV Systems
- Survey Sensors
- Communication Equipment
- Safety Equipment
- General Tools
- Spare Parts

---

## Certification Tracking

### Certificate Fields

| Field | Description |
|-------|-------------|
| Certificate Number | Unique certificate ID |
| Type | Calibration, Safety, etc. |
| Issue Date | Date certificate issued |
| Expiry Date | Date certificate expires |
| Issuing Authority | Calibration lab/authority |
| Document | Attached certificate file |
| Notes | Additional information |

### Expiry Alerts

| Alert Level | Days Before Expiry |
|-------------|-------------------|
| Info | 60 days |
| Warning | 30 days |
| Critical | 7 days |
| Expired | 0 or negative |

### Certificate Types

- Calibration Certificate
- Annual Inspection
- Safety Certification
- Class Approval
- Type Approval
- Insurance Certificate

---

## Maintenance Scheduling

### Maintenance Record

| Field | Description |
|-------|-------------|
| Maintenance ID | Unique identifier |
| Equipment | Equipment being maintained |
| Type | Preventive, Corrective, etc. |
| Scheduled Date | Planned maintenance date |
| Completed Date | Actual completion date |
| Performed By | Technician name |
| Description | Work performed |
| Cost | Maintenance cost |
| Next Due | Next scheduled maintenance |

### Maintenance Types

| Type | Description |
|------|-------------|
| Preventive | Scheduled routine maintenance |
| Corrective | Repair after failure |
| Predictive | Based on condition monitoring |
| Upgrade | Enhancement or modification |
| Inspection | Periodic inspection |

### Scheduling Rules

- Interval-based (e.g., every 6 months)
- Usage-based (e.g., every 1000 hours)
- Event-based (e.g., after mobilization)

---

## Manifests and Shipping

### Manifest Creation

1. Create new manifest
2. Add equipment items
3. Set source and destination
4. Add shipping details
5. Generate manifest PDF
6. Track delivery status

### Manifest Fields

| Field | Description |
|-------|-------------|
| Manifest Number | Unique identifier |
| Date Created | Creation date |
| Source Location | Shipping from |
| Destination | Shipping to |
| Carrier | Shipping company |
| Tracking Number | Carrier tracking ID |
| Status | Pending, Shipped, Delivered |
| Items | List of equipment |

### Shipment Verification

- Scan items at dispatch
- Verify against manifest
- Confirm delivery receipt
- Update equipment locations

---

## Configuration Options

### General Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Date Format | Display date format | yyyy-MM-dd |
| Currency | Cost currency | USD |
| Auto Sync | Automatic cloud sync | Yes |
| Sync Interval | Minutes between syncs | 15 |

### Alert Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Cert Warning Days | Days before expiry warning | 30 |
| Maintenance Reminder | Days before due reminder | 14 |
| Email Notifications | Send email alerts | No |

### Barcode/QR Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Label Format | Label size | Standard |
| Include QR | Add QR code to labels | Yes |
| Include Barcode | Add 1D barcode | Yes |

---

## API Integration

### Cloud Synchronization

The module synchronizes with a cloud backend for:
- Multi-user access
- Backup and restore
- Cross-location sharing
- Mobile app integration

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| /api/equipment | GET | List equipment |
| /api/equipment/{id} | GET | Get equipment details |
| /api/equipment | POST | Create equipment |
| /api/equipment/{id} | PUT | Update equipment |
| /api/certifications | GET | List certifications |
| /api/manifests | GET | List manifests |

### Offline Mode

When offline:
- Full read/write access to local database
- Changes queued for sync
- Conflict resolution on reconnect
- Last sync timestamp displayed

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | EquipmentInventory |
| Certificate Code | EI |
| Equipment Count | Number of items in inventory |
| Expiring Certs | Certificates expiring soon |
| Report Type | Inventory/Certification/Maintenance |
| Report Date | Date of report generation |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Equipment |
| Ctrl+F | Find Equipment |
| Ctrl+S | Sync |
| F5 | Refresh |
| Ctrl+P | Print Report |
| Del | Delete Selected |

---

## Troubleshooting

### Sync Issues

**Sync Fails**
- Check internet connection
- Verify API credentials
- Check server status
- Try manual sync

**Conflict Errors**
- Review conflicting changes
- Choose correct version
- Contact admin for help

### Import Issues

**Import Fails**
- Verify file format
- Check required columns
- Review import log
- Fix data errors

---

## Related Documentation

- [Getting Started](../GettingStarted.md)
- [API Reference](../API/Core-API.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
