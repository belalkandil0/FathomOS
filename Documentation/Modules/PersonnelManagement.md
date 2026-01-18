# Personnel Management Module

**Module ID:** PersonnelManagement
**Version:** 1.0.0
**Category:** Operations
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Personnel Records](#personnel-records)
6. [Certification Tracking](#certification-tracking)
7. [Vessel Assignments](#vessel-assignments)
8. [Timesheet Management](#timesheet-management)
9. [Configuration Options](#configuration-options)
10. [Authentication](#authentication)

---

## Overview

The Personnel Management module manages survey crew information, certifications, training records, vessel assignments, and timesheets. It provides comprehensive tracking of personnel competencies and deployment for offshore operations.

### Primary Use Cases

- Maintain personnel database
- Track certifications and expiry dates
- Manage vessel assignments
- Generate crew timesheets
- Verify competency compliance

---

## Features

### Personnel Database
- Comprehensive personnel records
- Contact information management
- Emergency contact details
- Photo/thumbnail support
- Document attachments

### Certification Management
- Certificate tracking
- Expiry notifications
- Renewal reminders
- Compliance reporting
- Document storage

### Vessel Assignments
- Assign personnel to vessels
- Rotation pattern management
- Deployment history
- Availability tracking

### Timesheet Management
- Daily timesheet entry
- Project/task tracking
- Offshore/onshore separation
- Export to payroll formats

### Audit Trail
- All changes logged
- User attribution
- Historical tracking
- Compliance reporting

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.csv` | Personnel import |
| `.xlsx` | Excel import |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel reports |
| `.pdf` | PDF reports |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Personnel] [Certifications] [Assignments] [Reports] [Help]|
+------------------------------------------------------------------+
| Tabs: [Personnel] [Certifications] [Vessel Assignments] [Timesheets]
+------------------------------------------------------------------+
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Quick Filters     |  | Personnel List                       | |
|  |                   |  |                                      | |
|  | Position: [All]   |  | [Photo] Name         | Position     | |
|  | Status: [Active]  |  | [img]   John Smith   | Survey Eng   | |
|  | Vessel: [All]     |  | [img]   Jane Doe     | Party Chief  | |
|  |                   |  | [img]   Bob Wilson   | CAD Tech     | |
|  | [Search...]       |  |                                      | |
|  |                   |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  [+ Add Person]  [Edit]  [Certifications]  [Assignments]         |
+------------------------------------------------------------------+
| Personnel: 45 active | 5 offshore | 3 certs expiring             |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: Personnel Management main window showing personnel list with certification status indicators and quick filters]*

---

## Personnel Records

### Personnel Fields

| Field | Required | Description |
|-------|----------|-------------|
| Employee ID | Yes | Unique identifier |
| First Name | Yes | Given name |
| Last Name | Yes | Family name |
| Position | Yes | Job role |
| Department | No | Department/team |
| Email | Yes | Work email |
| Phone | No | Contact number |
| Start Date | Yes | Employment start |
| Status | Yes | Active/Inactive |
| Photo | No | Profile picture |

### Position Types

- Survey Party Chief
- Senior Survey Engineer
- Survey Engineer
- Junior Survey Engineer
- CAD Technician
- Survey Operator
- ROV Pilot
- Data Processor

### Status Values

| Status | Description |
|--------|-------------|
| Active | Currently employed |
| On Leave | Temporary leave |
| Offshore | Currently deployed |
| Training | In training period |
| Inactive | Not currently working |

---

## Certification Tracking

### Certification Types

| Category | Examples |
|----------|----------|
| Safety | BOSIET, HUET, Sea Survival |
| Medical | Offshore Medical, ENG1 |
| Professional | IMCA Certified, Survey Qual |
| Equipment | ROV Pilot, DP Operator |
| Company | Internal certifications |

### Certification Fields

| Field | Description |
|-------|-------------|
| Certification Type | Type of certificate |
| Certificate Number | Unique cert ID |
| Issue Date | Date issued |
| Expiry Date | Date expires |
| Issuing Body | Authority/organization |
| Document | Attached certificate |
| Status | Valid/Expiring/Expired |

### Expiry Alerts

| Alert Level | Days Before |
|-------------|-------------|
| Information | 90 days |
| Warning | 30 days |
| Critical | 14 days |
| Expired | 0 days |

---

## Vessel Assignments

### Assignment Record

| Field | Description |
|-------|-------------|
| Vessel | Assigned vessel |
| Start Date | Assignment start |
| End Date | Assignment end |
| Role | Position on vessel |
| Rotation | Rotation pattern |
| Status | Planned/Active/Completed |

### Rotation Patterns

| Pattern | Description |
|---------|-------------|
| 4/4 | 4 weeks on, 4 weeks off |
| 3/3 | 3 weeks on, 3 weeks off |
| 2/2 | 2 weeks on, 2 weeks off |
| Custom | User-defined pattern |

### Assignment Validation

Before confirming an assignment:
- Check certification validity
- Verify medical fitness
- Confirm availability
- Check for conflicts

---

## Timesheet Management

### Timesheet Entry

| Field | Description |
|-------|-------------|
| Date | Work date |
| Personnel | Employee |
| Project | Project/job |
| Hours | Hours worked |
| Type | Regular/Overtime/Travel |
| Location | Offshore/Onshore |
| Notes | Additional details |

### Time Categories

| Category | Description |
|----------|-------------|
| Survey | Survey work hours |
| Processing | Data processing |
| Travel | Transit time |
| Standby | Waiting time |
| Training | Training hours |
| Leave | Vacation/sick |

### Export Formats

- Excel timesheet reports
- CSV for payroll import
- PDF summaries

---

## Configuration Options

### General Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Date Format | Display format | yyyy-MM-dd |
| Photo Path | Storage location | AppData |
| Auto Backup | Automatic backup | Yes |

### Certification Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Warning Days | Days before warning | 30 |
| Critical Days | Days before critical | 14 |
| Require All | Require all certs | Yes |

### Timesheet Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Week Start | First day of week | Monday |
| Day Hours | Standard day hours | 12 |
| Overtime After | Hours before OT | 8 |

---

## Authentication

### Access Control

This module requires authentication:

1. User must log in before accessing
2. Role-based permissions apply
3. All changes are audited

### Permissions

| Permission | Description |
|------------|-------------|
| personnel.view | View personnel records |
| personnel.edit | Edit personnel records |
| personnel.delete | Delete records |
| certs.manage | Manage certifications |
| timesheets.approve | Approve timesheets |

### Roles

| Role | Permissions |
|------|-------------|
| Viewer | View only |
| User | View and edit own |
| Manager | Full department access |
| Admin | Full system access |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | PersonnelManagement |
| Certificate Code | PM |
| Report Type | Personnel/Cert/Timesheet |
| Record Count | Number of records |
| Generated By | User who generated |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Personnel |
| Ctrl+F | Find |
| Ctrl+S | Save |
| F5 | Refresh |
| Del | Delete (with confirm) |

---

## Troubleshooting

### Login Issues

**Authentication Failed**
- Verify username and password
- Check network connection
- Contact administrator

### Data Issues

**Records Not Saving**
- Check permissions
- Verify required fields
- Check database connection

---

## Related Documentation

- [Project Management](ProjectManagement.md)
- [Getting Started](../GettingStarted.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
