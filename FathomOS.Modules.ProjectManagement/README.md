# Project Management Module

**Module ID:** ProjectManagement
**Version:** 1.0.0
**Category:** Operations
**Author:** Fathom OS Team

---

## Overview

The Project Management module provides comprehensive project tracking capabilities for survey operations. It enables teams to manage milestones, deliverables, client coordination, and project progress reporting within the Fathom OS ecosystem.

## Features

### Project Tracking
- Create and manage survey projects
- Track project status and phases
- Assign project managers and team members
- Project timeline visualization

### Milestone Management
- Define project milestones with target dates
- Track milestone completion status
- Milestone dependency tracking
- Automated alerts for upcoming milestones

### Deliverable Management
- Define project deliverables
- Track deliverable status (Draft, Review, Approved, Delivered)
- Link deliverables to milestones
- Document version tracking

### Client Coordination
- Client contact management
- Communication logging
- Document sharing tracking
- Approval workflow integration

### Progress Reporting
- Project progress dashboards
- Gantt chart visualization
- Status reports generation
- Export to PDF and Excel

### Integration
- Links to Survey Listing projects
- Equipment allocation tracking
- Personnel assignment
- Certificate generation for project completion

## Supported File Types

| Extension | Type | Description |
|-----------|------|-------------|
| `.fproj` | Project | Fathom OS project file |
| `.xlsx` | Export | Excel progress report |
| `.pdf` | Export | PDF status report |

## Project Structure

```
FathomOS.Modules.ProjectManagement/
├── ProjectManagementModule.cs   # IModule implementation
├── Views/
│   ├── MainWindow.xaml          # Main module window
│   ├── ProjectListView.xaml     # Project list panel
│   ├── ProjectDetailsView.xaml  # Project details panel
│   ├── MilestoneView.xaml       # Milestone management
│   └── Dialogs/
│       ├── NewProjectDialog.xaml
│       ├── MilestoneDialog.xaml
│       └── DeliverableDialog.xaml
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── ProjectViewModel.cs
│   ├── MilestoneViewModel.cs
│   ├── DeliverableViewModel.cs
│   └── ViewModelBase.cs
├── Models/
│   ├── Project.cs
│   ├── Milestone.cs
│   ├── Deliverable.cs
│   └── ProjectContact.cs
├── Services/
│   ├── ProjectService.cs
│   ├── ReportService.cs
│   └── ThemeService.cs
├── Assets/
│   └── icon.png                 # Module icon
└── Themes/
    ├── DarkTheme.xaml
    ├── LightTheme.xaml
    ├── ModernTheme.xaml
    └── GradientTheme.xaml
```

## Module Metadata

```json
{
    "moduleId": "ProjectManagement",
    "displayName": "Project Management",
    "description": "Track survey projects, milestones, deliverables, and client coordination.",
    "version": "1.0.0",
    "category": "Operations",
    "displayOrder": 50,
    "certificateCode": "PM",
    "certificateTitle": "Project Management Certificate"
}
```

## Data Models

### Project

```csharp
public class Project
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ClientName { get; set; }
    public string ProjectManager { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public List<Milestone> Milestones { get; set; }
    public List<Deliverable> Deliverables { get; set; }
    public List<ProjectContact> Contacts { get; set; }
}

public enum ProjectStatus
{
    Planning,
    InProgress,
    OnHold,
    Completed,
    Cancelled
}
```

### Milestone

```csharp
public class Milestone
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime TargetDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public MilestoneStatus Status { get; set; }
    public List<string> DependsOn { get; set; }
}
```

### Deliverable

```csharp
public class Deliverable
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string MilestoneId { get; set; }
    public DeliverableStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string FilePath { get; set; }
}
```

## Usage

### Creating a Project

1. Launch the Project Management module
2. Click "New Project" button
3. Enter project details (name, client, dates)
4. Define milestones with target dates
5. Add deliverables linked to milestones
6. Save the project

### Tracking Progress

1. Select a project from the list
2. Update milestone status as work progresses
3. Mark deliverables as complete
4. View progress in the dashboard
5. Generate status reports

### Certificate Generation

```csharp
var certificate = await CertificateHelper.QuickCreate(licenseManager)
    .ForModule("ProjectManagement", "PM", "1.0.0")
    .WithProject(project.Name, project.Location)
    .WithClient(project.ClientName)
    .AddData("Project Status", project.Status.ToString())
    .AddData("Milestones Completed", completedCount.ToString())
    .AddData("Total Milestones", totalCount.ToString())
    .AddData("Deliverables", deliverableCount.ToString())
    .CreateWithDialogAsync(owner);
```

## Dependencies

- FathomOS.Core
- ClosedXML (Excel export)
- QuestPDF (PDF reports)
- OxyPlot.Wpf (Gantt charts)

## Configuration

### Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Default View | Initial view on launch | Dashboard |
| Auto-save Interval | Minutes between saves | 5 |
| Alert Days | Days before milestone alert | 7 |
| Export Path | Default export location | Documents |

## Documentation

- [Full Module Documentation](../Documentation/Modules/ProjectManagement.md)
- [Developer Guide](../Documentation/DeveloperGuide.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
