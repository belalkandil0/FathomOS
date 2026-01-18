# Tree Inclination Module

**Module ID:** TreeInclination
**Version:** 1.0.0
**Category:** Quality Control
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Calculation Method](#calculation-method)
6. [Configuration Options](#configuration-options)
7. [Export and Reporting](#export-and-reporting)

---

## Overview

The Tree Inclination module calculates the inclination of subsea structures using DigiQuartz depth sensor measurements. It determines the tilt angle of vertical structures (such as wellhead trees, risers, or jackets) based on depth readings at multiple points.

### Primary Use Cases

- Calculate wellhead tree inclination
- Verify riser verticality
- Assess jacket leg plumbness
- Document structure inclination for surveys

---

## Features

### Data Import
- Parse NaviPac NPD files
- Support DigiQuartz depth columns
- Multiple measurement point support
- Automatic unit detection

### Inclination Calculation
- Geometric angle calculation
- Tilt direction determination
- Statistical averaging
- Uncertainty estimation

### Visualization
- Structure diagram
- Inclination vector display
- Depth profile view
- Results summary

### Reporting
- PDF inclination certificate
- Excel data export
- Detailed calculation report

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.fosi` | FathomOS Inclination project |
| `.npd` | NaviPac survey data |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.fosi` | Project save file |
| `.xlsx` | Excel report |
| `.pdf` | PDF certificate |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Calculate] [View] [Export] [Help]                [Theme] |
+------------------------------------------------------------------+
|                                                                   |
|  Measurement Points                                               |
|  +------------------------------------------------------------+ |
|  | Point | Easting    | Northing   | Depth   | Description     | |
|  +------------------------------------------------------------+ |
|  | A     | 425123.45  | 6500234.56 | 125.234 | Reference       | |
|  | B     | 425124.12  | 6500235.01 | 125.298 | Top of Tree     | |
|  | C     | 425123.89  | 6500234.78 | 127.456 | Base of Tree    | |
|  +------------------------------------------------------------+ |
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Calculation       |  | Structure Diagram                    | |
|  |                   |  |                                      | |
|  | Inclination:      |  |           B                          | |
|  |   0.85 degrees    |  |          /|                          | |
|  |                   |  |         / |                          | |
|  | Direction:        |  |        /  | 2.22m                    | |
|  |   045 degrees     |  |       /   |                          | |
|  |   (NE)            |  |      A----C                          | |
|  |                   |  |      0.033m offset                   | |
|  | Height: 2.222m    |  |                                      | |
|  | Offset: 0.033m    |  | Inclination: 0.85 deg                | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  [Add Point]  [Calculate]  [Generate Certificate]                 |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: Tree Inclination module showing measurement points, calculated inclination angle, and structure diagram]*

---

## Calculation Method

### Basic Geometry

The inclination is calculated from the horizontal offset and vertical height:

```
Inclination (degrees) = arctan(Horizontal_Offset / Vertical_Height) * (180/pi)

Where:
  Horizontal_Offset = sqrt((E_top - E_base)^2 + (N_top - N_base)^2)
  Vertical_Height = |Depth_base - Depth_top|
```

### Direction Calculation

The tilt direction is the bearing from the base to the top:

```
Direction = arctan2(E_top - E_base, N_top - N_base) * (180/pi)
```

### Multiple Measurement Averaging

When multiple measurements are available:
1. Calculate inclination for each measurement set
2. Compute mean inclination
3. Calculate standard deviation
4. Report mean with uncertainty

### Depth Correction

DigiQuartz depth values may need correction for:
- Sensor offset
- Tide variations
- Sound velocity

---

## Configuration Options

### Measurement Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Depth Units | Meters/Feet | Meters |
| Sensor Offset | DigiQuartz offset | 0.0 |
| Apply Tide | Tide correction | No |

### Calculation Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Averaging | Use multiple readings | Yes |
| Min Readings | Minimum per point | 5 |
| Outlier Rejection | Remove outliers | Yes |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Direction Format | Degrees/Cardinal | Degrees |
| Precision | Decimal places | 2 |
| Show Diagram | Structure diagram | Yes |

---

## Export and Reporting

### PDF Certificate

The inclination certificate includes:
- Certificate header
- Structure identification
- Measurement point coordinates
- Calculated inclination
- Direction of tilt
- Acceptance criteria
- Signatory information

### Excel Export

Detailed export containing:
- Raw measurement data
- Calculated values
- Statistical summary
- Structure diagram

### Certificate Example

```
TREE INCLINATION CERTIFICATE
Certificate ID: TI-2026-000005

Project: Wellhead Inspection
Structure: Well XYZ-01 Christmas Tree

Measurement Points:
  Reference (A): E 425123.45, N 6500234.56, D 125.234m
  Top (B):       E 425124.12, N 6500235.01, D 125.298m
  Base (C):      E 425123.89, N 6500234.78, D 127.456m

Results:
  Inclination:    0.85 degrees
  Direction:      045 degrees (NE)
  Vertical Height: 2.222m
  Horizontal Offset: 0.033m

Specification: < 1.0 degree
Determination: PASS

Signed: [Signatory]
Date: 2026-01-15
```

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | TreeInclination |
| Certificate Code | TI |
| Structure ID | Structure identifier |
| Inclination | Calculated angle |
| Direction | Tilt direction |
| Pass/Fail | Determination |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Project |
| Ctrl+O | Open Project |
| Ctrl+S | Save Project |
| Ctrl+C | Calculate |
| Ctrl+G | Generate Certificate |
| Insert | Add Point |
| Delete | Remove Point |

---

## Troubleshooting

### Calculation Issues

**Unrealistic Inclination**
- Verify point coordinates are correct
- Check depth values are from same tide state
- Verify sensor offsets

**Direction Incorrect**
- Check point labels (top vs base)
- Verify coordinate system orientation

---

## Related Documentation

- [Survey Listing](SurveyListing.md)
- [Getting Started](../GettingStarted.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
