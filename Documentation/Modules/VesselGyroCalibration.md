# Vessel Gyro Calibration Module

**Module ID:** VesselGyroCalibration
**Version:** 1.0.0
**Category:** Calibrations
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Calibration Process](#calibration-process)
6. [C-O Methodology](#c-o-methodology)
7. [Export and Reporting](#export-and-reporting)

---

## Overview

The Vessel Gyro Calibration module calibrates vessel gyro systems using the C-O (Calculated minus Observed) methodology with 3-sigma outlier detection. It compares calculated headings from GNSS positions against observed gyro headings.

### Primary Use Cases

- Calibrate vessel gyrocompass
- Verify heading sensor accuracy
- Calculate gyro offset
- Generate vessel gyro certificates

---

## Features

### Data Import
- Parse NaviPac NPD files
- Gyro heading data
- GNSS position data
- Automatic column detection

### C-O Analysis
- Calculate heading from positions
- Compare to observed gyro
- Apply 3-sigma filtering
- Iterative outlier removal

### Statistical Analysis
- Mean C-O difference
- Standard deviation
- Outlier identification
- Confidence intervals

### Reporting
- PDF calibration certificate
- Excel data export
- Statistical summary

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.npd` | NaviPac survey data |
| `.csv` | CSV navigation data |

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
|  Data Configuration                                               |
|  +------------------------------------------------------------+ |
|  | Position Columns:        | Gyro Column:                     | |
|  | Easting:  [Column 2]     | Heading: [Column 5]              | |
|  | Northing: [Column 3]     |                                  | |
|  +------------------------------------------------------------+ |
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | C-O Statistics    |  | C-O Time Series                      | |
|  |                   |  |                                      | |
|  | C-O Mean:         |  |    +5 ______________________         | |
|  |   -1.25 degrees   |  |     0 ------ mean --------           | |
|  |                   |  |    -5 ______________________         | |
|  | Std Deviation:    |  |                                      | |
|  |   0.42 degrees    |  |                                      | |
|  |                   |  |                                      | |
|  | Points: 2,856     |  |                                      | |
|  | Outliers: 45      |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  Minimum Speed Filter: [0.5] knots                                |
|                                                                   |
|  [Load File]  [Calculate]  [Generate Certificate]                 |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: Vessel Gyro Calibration showing C-O analysis with time series chart and statistics]*

---

## Calibration Process

### Step 1: Load Data

1. Open NPD file with position and gyro data
2. Verify GNSS position data quality
3. Check gyro heading data available

### Step 2: Configure Analysis

1. Map position columns (Easting, Northing)
2. Map gyro heading column
3. Set minimum speed threshold
4. Configure outlier threshold

### Step 3: Calculate C-O

For each epoch (when speed > threshold):
1. Calculate bearing from position difference
2. Compare to observed gyro heading
3. Record C-O difference

### Step 4: Statistical Analysis

1. Calculate mean C-O
2. Calculate standard deviation
3. Identify 3-sigma outliers
4. Remove outliers and recalculate
5. Report final statistics

### Step 5: Generate Certificate

1. Review final offset value
2. Enter signatory information
3. Generate PDF certificate

---

## C-O Methodology

### Calculated Heading

The calculated heading is derived from consecutive GNSS positions:

```
Calculated_Heading = arctan2(E2 - E1, N2 - N1) * (180/pi)

Where:
  E1, N1 = Position at time t
  E2, N2 = Position at time t+1
```

### C-O Difference

```
C-O = Calculated_Heading - Observed_Gyro_Heading

Normalize to -180 to +180 range
```

### Speed Filtering

Only calculate C-O when vessel speed exceeds threshold:

```
Speed = Distance / Time_Interval
Distance = sqrt((E2-E1)^2 + (N2-N1)^2)

If Speed > Minimum_Speed then calculate C-O
```

Recommended minimum speed: 0.5 knots

### Outlier Rejection

Using 3-sigma rule:
```
Outlier if |C-O - Mean_CO| > 3 * StdDev_CO
```

Iterate until no new outliers found (max 5 iterations).

---

## Export and Reporting

### PDF Certificate

Certificate includes:
- Certificate header
- Vessel and equipment details
- Analysis configuration
- C-O statistics table
- Time series chart
- Recommended offset
- Signatory information

### Excel Export

Contains:
- Raw data with C-O values
- Filtered data
- Statistics summary
- Charts

### Certificate Example

```
VESSEL GYRO CALIBRATION CERTIFICATE
Certificate ID: VGC-2026-000007

Project: Pipeline Survey
Vessel: MV Survey One

Equipment:
  Primary Gyro: Anschutz Standard 22
  GNSS System: Fugro Starfix

Analysis Configuration:
  Minimum Speed: 0.5 knots
  Outlier Threshold: 3-sigma

Results:
  C-O Mean: -1.25 degrees
  Standard Deviation: 0.42 degrees
  Valid Points: 2,811
  Outliers Removed: 45

Recommended Action:
  Apply gyro offset of +1.25 degrees
  (or verify gyro calibration)

Signed: [Signatory]
Date: 2026-01-15
```

---

## Configuration Options

### Analysis Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Min Speed | Speed threshold (knots) | 0.5 |
| Outlier Threshold | Sigma multiplier | 3.0 |
| Max Iterations | Outlier passes | 5 |
| Position Interval | Time between fixes | Auto |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Angle Range | Display range | -180/180 |
| Chart Scale | Y-axis range | Auto |
| Show Outliers | Mark outliers | Yes |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | VesselGyroCalibration |
| Certificate Code | VGC |
| Gyro System | Gyro identifier |
| GNSS System | Position source |
| C-O Mean | Calibration offset |
| Valid Points | Points used |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open File |
| Ctrl+C | Calculate |
| Ctrl+G | Generate Certificate |
| F5 | Refresh |

---

## Troubleshooting

### Large C-O Values

**High Mean Offset**
- Gyro may need alignment
- Check for magnetic interference
- Verify GNSS antenna position

**High Standard Deviation**
- Increase minimum speed
- Check GNSS quality
- Look for gyro instability

### Insufficient Data

**Too Few Valid Points**
- Lower speed threshold
- Extend data period
- Check for data gaps

---

## Related Documentation

- [ROV Gyro Calibration](RovGyroCalibration.md)
- [GNSS Calibration](GnssCalibration.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
