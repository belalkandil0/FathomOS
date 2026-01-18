# Survey Listing Generator Module

**Module ID:** SurveyListing
**Version:** 1.0.45
**Category:** Data Processing
**Author:** Fathom OS Team

---

## Overview

The Survey Listing Generator is the flagship data processing module for Fathom OS. It processes NaviPac NPD survey data files to generate professional survey listings with route alignment, tide corrections, and multiple export formats.

## Features

### Data Import
- Parse NaviPac NPD files with automatic column detection
- Support for multiple depth sources (Bathy, CTD, Altimeter)
- Configurable date/time format handling
- Batch file import

### Route Alignment
- Import RLX route definition files
- Calculate KP (Kilometer Point) along route
- Calculate offset from route centerline
- Support for straight and curved segments

### Tide Corrections
- Import tide data files (.tide)
- Linear and spline interpolation methods
- Preview tide correction graph
- Apply to depth measurements

### Data Smoothing
- Moving average filter
- Savitzky-Golay smoothing
- Median filter
- Gaussian smoothing
- Kalman filter
- Configurable window sizes

### Visualization
- Interactive 3D track viewer
- Color-coded depth display
- Depth vs distance profile charts
- Statistical dashboards

### Export Formats
- Excel spreadsheet (.xlsx)
- AutoCAD DXF (.dxf)
- PDF report (.pdf)
- Text/CSV files (.txt, .csv)
- AutoCAD script (.scr)

## Supported File Types

| Extension | Type | Description |
|-----------|------|-------------|
| `.npd` | Input | NaviPac survey data |
| `.rlx` | Input | Route alignment file |
| `.tide` | Input | Tide correction data |
| `.s7p` | Project | FathomOS project file |
| `.xlsx` | Output | Excel spreadsheet |
| `.dxf` | Output | CAD drawing |
| `.pdf` | Output | PDF report |

## Project Structure

```
FathomOS.Modules.SurveyListing/
├── SurveyListingModule.cs      # IModule implementation
├── Views/
│   ├── MainWindow.xaml         # Main module window
│   └── Steps/                  # Step wizard panels
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── ViewModelBase.cs
│   └── RelayCommand.cs
├── Services/
│   └── ThemeService.cs
├── Assets/
│   └── icon.png               # Module icon
└── Themes/
    ├── DarkTheme.xaml
    ├── LightTheme.xaml
    ├── ModernTheme.xaml
    └── GradientTheme.xaml
```

## Module Metadata

```json
{
    "moduleId": "SurveyListing",
    "displayName": "Survey Listing Generator",
    "description": "Generate survey listings from NPD files with route alignment, tide corrections, and multiple export formats.",
    "version": "1.0.45",
    "category": "Data Processing",
    "displayOrder": 10,
    "certificateCode": "SL",
    "certificateTitle": "Survey Listing Processing Certificate"
}
```

## Usage

### Basic Workflow

1. **Create Project** - Enter project metadata (name, client, vessel)
2. **Load Route** (Optional) - Import RLX route alignment file
3. **Import Data** - Load NPD survey files and configure column mapping
4. **Review Data** - Preview data in table and charts
5. **Apply Corrections** - Load tide data and apply corrections
6. **Process & Export** - Run processing and export to desired formats

### Certificate Generation

```csharp
var certificate = await CertificateHelper.QuickCreate(licenseManager)
    .ForModule("SurveyListing", "SL", "1.0.45")
    .WithProject(projectName, projectLocation)
    .WithVessel(vesselName)
    .WithClient(clientName)
    .AddData("Total Points", pointCount.ToString("N0"))
    .AddData("Survey Length", $"{surveyLength:F2} km")
    .AddInputFile(inputFilePath)
    .AddOutputFile(outputFilePath)
    .CreateWithDialogAsync(owner);
```

## Dependencies

- FathomOS.Core
- MathNet.Numerics (smoothing algorithms)
- ClosedXML (Excel export)
- netDxf (CAD export)
- QuestPDF (PDF reports)
- HelixToolkit.Wpf (3D visualization)
- OxyPlot.Wpf (charting)

## Configuration

### Processing Options

| Option | Description | Default |
|--------|-------------|---------|
| Apply Vertical Offsets | Apply depth offsets | Yes |
| Depth Exaggeration | CAD scale factor | 10.0 |
| Smoothing Method | Algorithm to use | Moving Average |
| Window Size | Smoothing window | 5 |

### Export Options

| Option | Description | Default |
|--------|-------------|---------|
| Include Route | Add route to DXF | Yes |
| Include KP Labels | Add KP markers | Yes |
| KP Label Interval | Spacing in km | 1.0 |
| Include 3D Track | Export 3D polyline | No |

## Documentation

- [Full Module Documentation](../Documentation/Modules/SurveyListing.md)
- [Core API Reference](../Documentation/API/Core-API.md)
- [Developer Guide](../Documentation/DeveloperGuide.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
