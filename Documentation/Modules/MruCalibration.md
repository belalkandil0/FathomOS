# MRU Calibration Module

**Module ID:** MruCalibration
**Version:** 3.0.0
**Category:** Calibrations
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Calibration Process](#calibration-process)
6. [Statistical Analysis](#statistical-analysis)
7. [Export and Reporting](#export-and-reporting)
8. [Configuration Options](#configuration-options)

---

## Overview

The MRU Calibration module performs inter-system comparison for Motion Reference Units (MRUs), specifically for Pitch and Roll sensor calibration. It implements 3-sigma outlier rejection, statistical analysis, and generates professional PDF calibration reports.

### Primary Use Cases

- Compare primary and secondary MRU systems
- Validate pitch and roll measurement accuracy
- Calculate mean offsets and standard deviations
- Generate MRU calibration certificates
- Document motion sensor performance

---

## Features

### Data Import
- Parse NaviPac NPD files
- Support pitch and roll columns
- Automatic unit detection (degrees/radians)
- Time synchronization

### Comparison Analysis
- Pitch difference calculation
- Roll difference calculation
- Combined motion analysis
- Time-correlated comparison

### Statistical Processing
- Mean and standard deviation
- 3-sigma outlier rejection
- Iterative filtering
- Confidence intervals

### Visualization
- Time series plots
- Histogram distributions
- Difference scatter plots
- Statistical summary charts

### Reporting
- PDF calibration certificates
- Excel data export
- Detailed statistics reports

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.npd` | NaviPac survey data |
| `.csv` | CSV motion data |
| `.mru` | MRU data file |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel analysis report |
| `.pdf` | PDF calibration certificate |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Analysis] [View] [Export] [Help]                 [Theme] |
+------------------------------------------------------------------+
|                                                                   |
|  MRU Column Mapping                                               |
|  +------------------------------------------------------------+ |
|  | System A (Primary):    | System B (Secondary):              | |
|  | Pitch:    [Column 5]   | Pitch:    [Column 7]               | |
|  | Roll:     [Column 6]   | Roll:     [Column 8]               | |
|  +------------------------------------------------------------+ |
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Pitch Statistics  |  | Pitch Time Series                    | |
|  |                   |  |                                      | |
|  | Mean:   0.05 deg  |  |    +2 _____/\____/\______           | |
|  | StdDev: 0.12 deg  |  |     0 ____/  \__/  \_____           | |
|  | Points: 5,230     |  |    -2 _______________                | |
|  | Outliers: 15      |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Roll Statistics   |  | Roll Time Series                     | |
|  |                   |  |                                      | |
|  | Mean:  -0.03 deg  |  |    +3 ____/\__/\__/\____             | |
|  | StdDev: 0.18 deg  |  |     0 ___/  \/  \/  \___             | |
|  | Points: 5,230     |  |    -3 _______________                | |
|  | Outliers: 22      |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  [Load File]  [Calculate]  [Remove Outliers]  [Generate Cert]    |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: MRU Calibration module showing pitch and roll time series with difference statistics]*

---

## Calibration Process

### Step 1: Load Data

1. Open NPD file with MRU data from both systems
2. Verify pitch and roll data from both MRUs
3. Check time coverage and sampling rate

### Step 2: Map Columns

1. Select MRU A (primary) pitch column
2. Select MRU A (primary) roll column
3. Select MRU B (secondary) pitch column
4. Select MRU B (secondary) roll column
5. Set angle units (degrees or radians)

### Step 3: Calculate Differences

For each epoch:
- dPitch = Pitch_A - Pitch_B
- dRoll = Roll_A - Roll_B

### Step 4: Statistical Analysis

1. Calculate mean differences
2. Calculate standard deviations
3. Identify 3-sigma outliers
4. Remove outliers and recalculate
5. Compute final statistics

### Step 5: Review Results

- Check mean offset is within specification
- Verify standard deviation is acceptable
- Review outlier percentage
- Check for systematic trends

### Step 6: Generate Certificate

1. Enter calibration details
2. Specify equipment information
3. Add signatory information
4. Generate PDF certificate

---

## Statistical Analysis

### Metrics Calculated

| Metric | Description |
|--------|-------------|
| Mean dPitch | Average pitch difference |
| Mean dRoll | Average roll difference |
| StdDev dPitch | Pitch standard deviation |
| StdDev dRoll | Roll standard deviation |
| Max dPitch | Maximum pitch difference |
| Max dRoll | Maximum roll difference |
| Points | Number of valid comparisons |
| Outliers | Number removed |

### Outlier Detection

Using 3-sigma rule:
```
Outlier if:
  |dPitch - Mean_dPitch| > 3 * StdDev_dPitch
  OR
  |dRoll - Mean_dRoll| > 3 * StdDev_dRoll
```

### Acceptance Criteria

Typical MRU specifications:
| Parameter | Typical Limit |
|-----------|---------------|
| Mean Offset | < 0.1 deg |
| Std Deviation | < 0.2 deg |
| Max Difference | < 0.5 deg |

---

## Export and Reporting

### PDF Certificate

The calibration certificate includes:
- Certificate header with unique ID
- Project and vessel information
- MRU equipment details
- Pitch statistics table
- Roll statistics table
- Time series chart
- Pass/Fail determination
- Signatory block

### Excel Export

Contains worksheets:
1. **Summary** - Overall statistics
2. **Pitch Data** - Pitch comparison data
3. **Roll Data** - Roll comparison data
4. **Charts** - Embedded visualizations

### Certificate Content

```
MRU CALIBRATION CERTIFICATE
Certificate ID: MRU-2026-000015

Project: Pipeline Survey
Vessel: MV Survey One

Equipment:
  MRU A (Primary): Kongsberg Seatex MRU5
  MRU B (Secondary): iXBlue Octans

Results:
                    Pitch       Roll
  Mean Offset:      0.05 deg    -0.03 deg
  Std Deviation:    0.12 deg     0.18 deg
  Valid Points:     5,230        5,230

Determination: PASS (within specification)

Signed: [Signatory]
Date: 2026-01-15
```

---

## Configuration Options

### Analysis Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Outlier Threshold | Sigma multiplier | 3.0 |
| Max Iterations | Outlier passes | 5 |
| Angle Units | Degrees/Radians | Degrees |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Chart Y-Scale | Auto or fixed | Auto |
| Show Grid | Display chart grid | Yes |
| Color Scheme | Chart colors | Standard |

### Report Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Include Charts | Add charts to PDF | Yes |
| Show Outliers | Mark outliers | Yes |
| Decimal Places | Number precision | 3 |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | MruCalibration |
| Certificate Code | MRU |
| MRU A | Primary MRU description |
| MRU B | Secondary MRU description |
| Mean Pitch Offset | Calculated mean |
| Mean Roll Offset | Calculated mean |
| Valid Points | Number used in analysis |
| Pass/Fail | Determination result |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open File |
| Ctrl+C | Calculate |
| Ctrl+R | Remove Outliers |
| Ctrl+G | Generate Certificate |
| F5 | Refresh |

---

## Troubleshooting

### Data Issues

**Angular Data Not Found**
- Verify column contains angular values
- Check for unit consistency
- Look for label variations (pitch/roll/heel)

**Large Systematic Offset**
- Check MRU mounting alignment
- Verify correct data columns
- Review installation offsets

### Analysis Issues

**High Outlier Count**
- Check data quality
- Verify MRUs were operational
- Look for time gaps

---

## Related Documentation

- [GNSS Calibration](GnssCalibration.md)
- [Vessel Gyro Calibration](VesselGyroCalibration.md)
- [Getting Started](../GettingStarted.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
