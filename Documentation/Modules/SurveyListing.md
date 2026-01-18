# Survey Listing Generator Module

**Module ID:** SurveyListing
**Version:** 1.0.45
**Category:** Data Processing
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Workflow](#workflow)
6. [Configuration Options](#configuration-options)
7. [Export Formats](#export-formats)
8. [Certificate Metadata](#certificate-metadata)
9. [Keyboard Shortcuts](#keyboard-shortcuts)

---

## Overview

The Survey Listing Generator is the flagship module of FathomOS, designed to process NaviPac NPD survey data files and generate professional survey listings. It supports route alignment, tide corrections, data smoothing, and exports to multiple formats including Excel, DXF/CAD, and PDF.

### Primary Use Cases

- Generate survey listings from ROV/AUV survey data
- Align survey data to pipeline/cable routes
- Apply tide corrections to depth measurements
- Create CAD-ready polylines for deliverables
- Generate processing certificates for QA/QC

---

## Features

### Data Processing
- Parse NaviPac NPD files with automatic column detection
- Multiple depth source selection (Bathy, CTD, Altimeter)
- Configurable date/time format handling
- Support for split date/time columns

### Route Alignment
- Import RLX route definition files
- Calculate KP (Kilometer Point) along route
- Calculate offset from route centerline
- Handle both straight and curved segments

### Tide Corrections
- Import tide data files
- Linear and spline interpolation methods
- Preview tide correction graph
- Apply to depth measurements

### Data Smoothing
- Moving average filter
- Savitzky-Golay smoothing
- Median filter
- Configurable window sizes

### 3D Visualization
- Interactive 3D track viewer
- Color-coded depth display
- Orbit and pan controls
- Depth exaggeration settings

### Charting
- Depth vs distance profile
- KP vs time chart
- Statistics display
- Export charts to images

---

## Supported File Types

### Input Files

| Extension | Description | Required |
|-----------|-------------|----------|
| `.npd` | NaviPac survey data | Yes |
| `.rlx` | Route alignment file | No |
| `.tide` | Tide correction file | No |
| `.s7p` | FathomOS project file | No |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel spreadsheet |
| `.dxf` | AutoCAD DXF |
| `.csv` | Comma-separated values |
| `.txt` | Tab-delimited text |
| `.pdf` | PDF report |
| `.scr` | AutoCAD script |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Edit] [View] [Tools] [Help]                    [Theme]   |
+------------------------------------------------------------------+
|  Step Navigation                                                  |
|  [1 Project] [2 Route] [3 Data] [4 Review] [5 Tide] [6 Process]  |
+------------------------------------------------------------------+
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Navigation Panel  |  | Main Content Area                    | |
|  |                   |  |                                      | |
|  | - Project Info    |  | (Content varies by step)             | |
|  | - File List       |  |                                      | |
|  | - Statistics      |  |                                      | |
|  |                   |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
+------------------------------------------------------------------+
| Status Bar: [Progress] | [File Count] | [Point Count]            |
+------------------------------------------------------------------+
```

### Step Panels

1. **Project Setup** - Configure project metadata
2. **Route File** - Load and preview route alignment
3. **Survey Data** - Import and map NPD files
4. **Data Review** - Preview data in table/chart
5. **Tide Corrections** - Load and apply tide data
6. **Processing** - Run calculations and export

### Screenshot Placeholder

*[Screenshot: Main window showing Step 3 - Survey Data with loaded NPD file and column mapping dialog]*

---

## Workflow

### Step 1: Project Setup

1. Enter project name
2. Enter client name
3. Enter vessel name
4. Set project location
5. Configure survey type (Seabed/ROV Dynamic)

### Step 2: Route File (Optional)

1. Click "Browse" to select RLX file
2. Preview route in map view
3. View route statistics (length, segments)
4. Configure KP calculation settings

### Step 3: Survey Data

1. Click "Add Files" to select NPD files
2. Review automatic column detection
3. Adjust column mapping if needed
4. Set date/time format options
5. Preview data in table

### Step 4: Data Review

1. Review loaded points in table
2. View statistical summary
3. Identify and mark outliers
4. View depth profile chart

### Step 5: Tide Corrections (Optional)

1. Load tide data file
2. Preview tide graph
3. Select interpolation method
4. Apply corrections

### Step 6: Processing & Export

1. Configure processing options
2. Click "Process" to run calculations
3. Review results
4. Export to desired formats
5. Generate processing certificate

---

## Configuration Options

### Processing Options

| Option | Description | Default |
|--------|-------------|---------|
| Apply Vertical Offsets | Apply bathy-to-altimeter offset | Yes |
| Bathy to Altimeter Offset | Vertical offset in meters | 0.0 |
| Bathy to ROV Ref Offset | ROV reference offset | 0.0 |
| Depth Exaggeration | CAD visualization scale | 10.0 |

### Smoothing Options

| Option | Description | Default |
|--------|-------------|---------|
| Smoothing Method | Algorithm to use | Moving Average |
| Window Size | Number of points | 5 |
| Apply to Coordinates | Smooth XY positions | Yes |
| Apply to Depth | Smooth Z values | Yes |

### Export Options

| Option | Description | Default |
|--------|-------------|---------|
| Include Route in DXF | Add route centerline | Yes |
| Include KP Labels | Add KP markers | Yes |
| KP Label Interval | Spacing in km | 1.0 |
| Include 3D Track | Export 3D polyline | No |

---

## Export Formats

### Excel Export

Creates a formatted spreadsheet with:
- Project information header
- Column headers with units
- All processed survey points
- Statistics summary
- Charts (optional)

### DXF Export

Creates a CAD file with layers:
- `Survey_Track` - 2D survey track (green)
- `Survey_Track_3D` - 3D track with depth (blue)
- `Route_Centerline` - Route alignment (red)
- `KP_Labels` - Kilometer point labels (yellow)
- `Survey_Points` - Point markers (cyan)
- `Survey_Fixes` - Fix positions (magenta)

### PDF Report

Generates a professional report with:
- Project information
- Processing summary
- Data statistics
- Track charts
- Certificate information

---

## Certificate Metadata

When generating a processing certificate, the following metadata is recorded:

| Field | Description |
|-------|-------------|
| Module ID | SurveyListing |
| Certificate Code | SL |
| Input Files | List of NPD/RLX/Tide files |
| Output Files | List of exported files |
| Total Points | Number of survey points |
| Survey Length | Total track distance |
| Processing Method | Smoothing algorithm used |
| Tide Correction | Yes/No and method |
| Route Alignment | Yes/No |
| Depth Range | Min to max depth |

### Example Certificate

```
Certificate ID: SL-2026-000042
Module: Survey Listing Generator v1.0.45
Project: Pipeline Survey Alpha
Date: 2026-01-15 14:30:22 UTC

Input Files:
- survey_line_001.npd
- survey_line_002.npd
- pipeline_route.rlx
- tide_data.tide

Output Files:
- survey_listing.xlsx
- survey_track.dxf

Processing Data:
- Total Points: 15,432
- Survey Length: 45.7 km
- Depth Range: 85.2m to 142.8m
- Smoothing: Moving Average (5-point)
- Tide Correction: Linear Interpolation

Signatory: John Smith, Survey Engineer
Company: Survey Corp Ltd
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Project |
| Ctrl+O | Open Project |
| Ctrl+S | Save Project |
| Ctrl+Shift+S | Save Project As |
| Ctrl+E | Export |
| Ctrl+P | Process |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| F1 | Help |
| F5 | Refresh Data |
| F11 | Fullscreen |

---

## Troubleshooting

### Common Issues

**NPD File Won't Parse**
- Check date/time format matches file
- Verify column mapping is correct
- Check for encoding issues (use UTF-8)

**Route Alignment Fails**
- Ensure RLX file is valid format
- Check coordinate system matches survey data
- Verify route and survey data overlap

**Large File Performance**
- Use data decimation for preview
- Process in batches for very large files
- Enable hardware acceleration

---

## Related Documentation

- [Getting Started](../GettingStarted.md)
- [Core API - NpdParser](../API/Core-API.md#npdparser)
- [Core API - DxfExporter](../API/Core-API.md#dxfexporter)

---

*Copyright 2026 Fathom OS. All rights reserved.*
