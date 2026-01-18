# Project Management Module

**Module ID:** ProjectManagement
**Version:** 1.0.0
**Category:** Operations
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Project Records](#project-records)
6. [Client Management](#client-management)
7. [Milestones and Deliverables](#milestones-and-deliverables)
8. [Resource Assignment](#resource-assignment)
9. [Configuration Options](#configuration-options)

---

## Overview

The Project Management module manages survey projects, client relationships, milestones, deliverables, and resource assignments. It provides a centralized view of project status and facilitates coordination across survey operations.

### Primary Use Cases

- Manage survey project lifecycle
- Track client relationships
- Monitor project milestones
- Manage deliverable schedules
- Assign resources to projects

---

## Features

### Project Management
- Project creation and tracking
- Status monitoring
- Timeline management
- Budget tracking
- Document management

### Client Management
- Client database
- Contact management
- Contract tracking
- Communication history

### Milestone Tracking
- Define project milestones
- Track completion status
- Set due dates
- Generate milestone reports

### Deliverable Management
- Define project deliverables
- Track submission status
- Version control
- Client approval tracking

### Resource Assignment
- Assign personnel to projects
- Assign equipment
- Vessel assignment coordination
- Capacity planning

### Dashboard
- Project status overview
- Timeline visualization
- KPI tracking
- Alert notifications

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.sproj` | FathomOS project file |
| `.csv` | CSV import |
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
| [File] [Projects] [Clients] [Resources] [Reports] [Help] [Theme] |
+------------------------------------------------------------------+
| Tabs: [Dashboard] [Projects] [Clients] [Milestones] [Deliverables]|
+------------------------------------------------------------------+
|                                                                   |
|  Dashboard Overview                                               |
|  +-------------------+  +-------------------+  +----------------+ |
|  | Active Projects   |  | Upcoming          |  | Overdue        | |
|  |       12          |  | Milestones        |  | Deliverables   | |
|  |                   |  |        5          |  |       2        | |
|  +-------------------+  +-------------------+  +----------------+ |
|                                                                   |
|  Project Timeline                                                 |
|  +------------------------------------------------------------+ |
|  | Jan     | Feb     | Mar     | Apr     | May     | Jun      | |
|  | ========> Project A                                          | |
|  |     =============> Project B                                 | |
|  |             ===========> Project C                           | |
|  +------------------------------------------------------------+ |
|                                                                   |
+------------------------------------------------------------------+
| Status: 12 projects | 5 milestones this week | 2 overdue items   |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: Project Management dashboard showing active projects, timeline view, and upcoming milestones]*

---

## Project Records

### Project Fields

| Field | Required | Description |
|-------|----------|-------------|
| Project ID | Yes | Unique identifier |
| Project Name | Yes | Project title |
| Client | Yes | Associated client |
| Status | Yes | Project status |
| Start Date | Yes | Project start |
| End Date | No | Planned end |
| Project Manager | No | Assigned PM |
| Budget | No | Project budget |
| Description | No | Project details |

### Project Status Values

| Status | Description |
|--------|-------------|
| Planned | Not yet started |
| Active | Currently in progress |
| On Hold | Temporarily paused |
| Completed | Successfully finished |
| Cancelled | Project cancelled |

### Project Types

- Pipeline Survey
- Seabed Survey
- ROV Inspection
- Cable Route Survey
- Rig Move Survey
- Site Survey
- As-Built Survey

---

## Client Management

### Client Record

| Field | Description |
|-------|-------------|
| Client ID | Unique identifier |
| Company Name | Client company |
| Industry | Business sector |
| Address | Physical address |
| Website | Company website |
| Notes | Additional info |

### Client Contacts

| Field | Description |
|-------|-------------|
| Name | Contact person name |
| Title | Job title |
| Email | Email address |
| Phone | Phone number |
| Primary | Primary contact flag |

### Communication Log

Track client communications:
- Emails
- Meetings
- Phone calls
- Site visits

---

## Milestones and Deliverables

### Milestone Record

| Field | Description |
|-------|-------------|
| Milestone ID | Unique identifier |
| Project | Parent project |
| Name | Milestone name |
| Description | Milestone details |
| Due Date | Target date |
| Status | Not Started/In Progress/Complete |
| Completion Date | Actual completion |

### Deliverable Record

| Field | Description |
|-------|-------------|
| Deliverable ID | Unique identifier |
| Project | Parent project |
| Name | Deliverable name |
| Type | Report/Data/Chart/etc. |
| Due Date | Submission due |
| Status | Draft/Review/Submitted/Approved |
| Version | Current version |
| File | Attached document |

### Status Workflow

```
Deliverable Workflow:
Draft -> Internal Review -> Client Review -> Approved
                       |                  |
                       v                  v
                   Revision          Revision
```

---

## Resource Assignment

### Personnel Assignment

| Field | Description |
|-------|-------------|
| Project | Target project |
| Person | Assigned personnel |
| Role | Project role |
| Start Date | Assignment start |
| End Date | Assignment end |
| Allocation | % time allocated |

### Equipment Assignment

| Field | Description |
|-------|-------------|
| Project | Target project |
| Equipment | Assigned equipment |
| Start Date | Assignment start |
| End Date | Assignment end |
| Notes | Special requirements |

### Vessel Assignment

| Field | Description |
|-------|-------------|
| Project | Target project |
| Vessel | Assigned vessel |
| Start Date | Mobilization date |
| End Date | Demobilization date |
| Days | Total vessel days |

---

## Configuration Options

### General Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Date Format | Display format | yyyy-MM-dd |
| Currency | Budget currency | USD |
| Fiscal Year Start | Budget period | January |

### Notification Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Milestone Warning | Days before due | 7 |
| Deliverable Warning | Days before due | 3 |
| Email Notifications | Send email alerts | No |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Timeline Range | Default view range | 6 months |
| Show Completed | Show finished items | Yes |
| Group By | Default grouping | Client |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | ProjectManagement |
| Certificate Code | PROJ |
| Report Type | Project/Status/Resource |
| Project Count | Number of projects |
| Date Range | Report period |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Project |
| Ctrl+F | Find |
| Ctrl+S | Save |
| F5 | Refresh |
| Ctrl+M | New Milestone |
| Ctrl+D | New Deliverable |

---

## Troubleshooting

### Data Issues

**Project Not Saving**
- Verify required fields
- Check database connection
- Review validation errors

**Resource Conflicts**
- Check assignment dates
- Verify availability
- Review allocation percentages

---

## Related Documentation

- [Personnel Management](PersonnelManagement.md)
- [Getting Started](../GettingStarted.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
