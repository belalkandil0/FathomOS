# ROV Gyro Calibration Module

**Module ID:** RovGyroCalibration
**Version:** 1.0.0
**Category:** Calibrations
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Calibration Process](#calibration-process)
6. [Geometric Corrections](#geometric-corrections)
7. [Export and Reporting](#export-and-reporting)

---

## Overview

The ROV Gyro Calibration module calibrates ROV gyro (heading) systems against a vessel reference with geometric corrections for the ROV position relative to the vessel. It accounts for the geometry of the ROV tracking system when comparing ROV heading to vessel heading.

### Primary Use Cases

- Calibrate ROV gyro against vessel reference
- Apply geometric corrections for ROV position
- Calculate heading offsets
- Generate ROV gyro calibration certificates

---

## Features

### Data Import
- Parse NaviPac NPD files
- ROV heading data
- Vessel heading reference
- Position data for geometry

### Geometric Correction
- Account for ROV position relative to vessel
- Bearing correction from vessel to ROV
- Dynamic geometry during movement

### Statistical Analysis
- Heading difference calculation
- 3-sigma outlier rejection
- Mean offset determination
- Standard deviation calculation

### Reporting
- PDF calibration certificate
- Excel data export
- Detailed analysis report

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.npd` | NaviPac survey data |
| `.csv` | CSV heading data |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel analysis |
| `.pdf` | PDF certificate |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Analysis] [View] [Export] [Help]                 [Theme] |
+------------------------------------------------------------------+
|                                                                   |
|  Data Columns                                                     |
|  +------------------------------------------------------------+ |
|  | ROV Heading:     [Column 8]                                 | |
|  | Vessel Heading:  [Column 5]                                 | |
|  | ROV Easting:     [Column 6]   Vessel Easting:  [Column 2]  | |
|  | ROV Northing:    [Column 7]   Vessel Northing: [Column 3]  | |
|  +------------------------------------------------------------+ |
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Statistics        |  | Heading Difference Time Series       | |
|  |                   |  |                                      | |
|  | Mean Offset:      |  |    +5 ______________________         | |
|  |   2.45 degrees    |  |     0 ------ mean --------           | |
|  |                   |  |    -5 ______________________         | |
|  | Std Deviation:    |  |                                      | |
|  |   0.85 degrees    |  |                                      | |
|  |                   |  |                                      | |
|  | Points: 3,450     |  |                                      | |
|  | Outliers: 28      |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  [x] Apply Geometric Correction                                   |
|                                                                   |
|  [Load File]  [Calculate]  [Generate Certificate]                 |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: ROV Gyro Calibration showing heading differences with geometric correction applied]*

---

## Calibration Process

### Step 1: Load Data

1. Open NPD file with ROV and vessel data
2. Verify both ROV and vessel headings present
3. Check position data available for geometry

### Step 2: Map Columns

1. Select ROV heading column
2. Select vessel heading column
3. Select position columns (for geometric correction)

### Step 3: Configure Corrections

1. Enable/disable geometric correction
2. Set any known offsets
3. Configure outlier threshold

### Step 4: Calculate

The calculation process:
1. Compute raw heading differences
2. Apply geometric correction if enabled
3. Calculate statistics
4. Identify and remove outliers
5. Recalculate final statistics

### Step 5: Review and Export

1. Review mean offset value
2. Verify standard deviation is acceptable
3. Generate calibration certificate

---

## Geometric Corrections

### Geometry Concept

When the ROV is not directly beneath the vessel, the bearing from vessel to ROV affects the apparent heading difference.

```
                Vessel
                  |
                  | Bearing to ROV
                  |
                  v
                 ROV

Correction = Bearing(Vessel -> ROV)
```

### Correction Formula

```
Corrected_Difference = Raw_Difference - Bearing_Correction

Where:
  Raw_Difference = ROV_Heading - Vessel_Heading
  Bearing_Correction = arctan2(ROV_E - Vessel_E, ROV_N - Vessel_N)
```

### When to Apply

Apply geometric correction when:
- ROV is tracking away from vessel
- Significant horizontal offset exists
- High accuracy calibration required

Do not apply when:
- ROV directly beneath vessel
- Vessel stationary, ROV rotating
- Dedicated spin test conditions

---

## Export and Reporting

### PDF Certificate

Certificate includes:
- Certificate header
- Equipment identification
- Geometric correction status
- Statistical results
- Time series chart
- Signatory information

### Excel Export

Contains:
- Raw data
- Corrected differences
- Statistics summary
- Charts

### Certificate Example

```
ROV GYRO CALIBRATION CERTIFICATE
Certificate ID: RGC-2026-000003

Project: Pipeline Survey
Vessel: MV Survey One

Equipment:
  ROV System: Work Class ROV
  ROV Gyro: IXSEA Phins
  Reference: Vessel Gyro (Anschutz)

Configuration:
  Geometric Correction: Applied

Results:
  Mean Heading Offset: 2.45 degrees
  Standard Deviation: 0.85 degrees
  Valid Points: 3,422
  Outliers Removed: 28

Recommended Offset: 2.45 degrees
(Apply +2.45 deg to ROV heading)

Signed: [Signatory]
Date: 2026-01-15
```

---

## Configuration Options

### Analysis Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Geometric Correction | Apply correction | Yes |
| Outlier Threshold | Sigma multiplier | 3.0 |
| Max Iterations | Outlier passes | 5 |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Angle Range | -180/180 or 0/360 | -180/180 |
| Chart Scale | Y-axis range | Auto |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | RovGyroCalibration |
| Certificate Code | RGC |
| ROV System | ROV identifier |
| Vessel Gyro | Reference system |
| Mean Offset | Calibration result |
| Geometric Corr | Yes/No |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open File |
| Ctrl+C | Calculate |
| Ctrl+G | Generate Certificate |
| F5 | Refresh |

---

## Related Documentation

- [Vessel Gyro Calibration](VesselGyroCalibration.md)
- [MRU Calibration](MruCalibration.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
