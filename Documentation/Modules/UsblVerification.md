# USBL Verification Module

**Module ID:** UsblVerification
**Version:** 1.7.0
**Category:** Quality Control
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Test Types](#test-types)
6. [Statistical Analysis](#statistical-analysis)
7. [Export and Reporting](#export-and-reporting)
8. [Configuration Options](#configuration-options)

---

## Overview

The USBL Verification module verifies USBL (Ultra-Short BaseLine) positioning accuracy through spin and transit tests with automated reporting. It calculates positioning errors and generates calibration certificates for quality documentation.

### Primary Use Cases

- Spin test analysis for USBL accuracy
- Transit test comparison with GNSS
- Calculate 2DRMS positioning error
- Generate USBL verification certificates
- Document acoustic positioning performance

---

## Features

### Test Support
- Spin test analysis
- Transit test analysis
- Comparison with reference positions
- Depth verification

### Data Analysis
- Position error calculation
- Statistical analysis
- Depth difference analysis
- Time-based correlation

### Visualization
- Plan view plot
- Depth profile chart
- Error histogram
- Time series display

### Reporting
- PDF verification certificates
- Excel data export
- Statistical summary reports

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.npd` | NaviPac survey data |
| `.csv` | CSV position data |
| `.txt` | Text position file |
| `.usbl` | USBL data file |

### Output Files

| Extension | Description |
|-----------|-------------|
| `.xlsx` | Excel analysis report |
| `.pdf` | PDF verification certificate |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Test] [Analysis] [View] [Export] [Help]          [Theme] |
+------------------------------------------------------------------+
| Test Type: ( ) Spin Test  (x) Transit Test                        |
+------------------------------------------------------------------+
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Data Columns      |  | Plan View                            | |
|  |                   |  |                                      | |
|  | USBL Easting:     |  |      o  o  o  o  o  o                | |
|  | [Column 4]        |  |     o              o                 | |
|  | USBL Northing:    |  |    o    +----+      o                | |
|  | [Column 5]        |  |     o   |USBL|     o                 | |
|  | USBL Depth:       |  |      o  +----+    o                  | |
|  | [Column 6]        |  |       o  o  o  o                     | |
|  | Reference E:      |  |                                      | |
|  | [Column 2]        |  | Scale: 1:500  Grid: 1m               | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Statistics        |  | Error Histogram                      | |
|  |                   |  |                                      | |
|  | Mean Error: 0.8m  |  |    |   ____                          | |
|  | Std Dev: 0.35m    |  |    |  |    |___                      | |
|  | 2DRMS: 1.51m      |  |    |__|    |   |__                   | |
|  | Points: 256       |  |    0.5m  1.0m  1.5m  2.0m            | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  [Load File]  [Calculate]  [Generate Certificate]                 |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: USBL Verification module showing spin test plan view with error ellipse and histogram]*

---

## Test Types

### Spin Test

Used to verify USBL accuracy by rotating the beacon around a fixed reference point.

**Procedure:**
1. Deploy beacon at known position
2. Rotate vessel around beacon
3. Record USBL positions throughout spin
4. Compare to known beacon position
5. Calculate accuracy statistics

**Analysis:**
- Mean position error from reference
- Standard deviation of positions
- 2DRMS error calculation
- Depth accuracy verification

### Transit Test

Compares USBL tracking of a moving beacon against GNSS reference.

**Procedure:**
1. Deploy beacon on ROV/vessel with GNSS
2. Transit at constant heading
3. Record both USBL and GNSS positions
4. Compare positions at each epoch

**Analysis:**
- Position differences (E, N)
- Depth comparison
- Distance errors over time
- 2DRMS calculation

---

## Statistical Analysis

### Spin Test Metrics

| Metric | Description |
|--------|-------------|
| Mean E Error | Average Easting error |
| Mean N Error | Average Northing error |
| Mean Radial | Mean 2D error from center |
| StdDev E | Easting standard deviation |
| StdDev N | Northing standard deviation |
| 2DRMS | 2D RMS error (95%) |
| Mean Depth Error | Depth accuracy |

### Transit Test Metrics

| Metric | Description |
|--------|-------------|
| Mean dE | Average Easting difference |
| Mean dN | Average Northing difference |
| StdDev dE | Easting difference std dev |
| StdDev dN | Northing difference std dev |
| 2DRMS | 2D RMS positioning error |
| Mean dZ | Average depth difference |

### 2DRMS Calculation

```
2DRMS = 2 * sqrt(StdDev_E^2 + StdDev_N^2)
```

Represents the radius of a circle that contains 95% of position fixes.

### Acceptance Criteria

Typical USBL specifications by depth:

| Depth Range | Typical 2DRMS |
|-------------|---------------|
| 0-100m | 0.5% of slant range |
| 100-500m | 0.3% of slant range |
| 500m+ | 0.2% of slant range |

---

## Export and Reporting

### PDF Certificate

The verification certificate includes:
- Certificate header with unique ID
- Project and vessel information
- USBL system details
- Test configuration
- Statistical results table
- Plan view plot
- Error histogram
- Pass/Fail determination
- Signatory information

### Excel Export

Detailed export containing:
- All position data
- Calculated errors
- Statistical summary
- Charts

### Certificate Example

```
USBL VERIFICATION CERTIFICATE
Certificate ID: USBL-2026-000008

Test Type: Spin Test
Project: Pipeline Survey
Vessel: MV Survey One

Equipment:
  USBL System: Kongsberg HiPAP 501
  Beacon: HiPAP Responder
  Water Depth: 125m

Test Configuration:
  Reference Position: E 425000.00, N 6500000.00
  Test Duration: 45 minutes
  Number of Fixes: 256

Results:
  Mean Radial Error:  0.82m
  Standard Deviation: 0.35m
  2DRMS:              1.51m
  Depth Error:        0.45m

Specification: 1.0% of slant range (1.25m at 125m)
Determination: PASS

Signed: [Signatory]
Date: 2026-01-15
```

---

## Configuration Options

### Analysis Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Outlier Threshold | Sigma multiplier | 3.0 |
| Min Points | Minimum data points | 50 |
| Reference Type | Fixed/Calculated | Fixed |

### Test Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Test Type | Spin/Transit | Spin |
| Depth Source | Column for depth | Auto |
| Time Sync | Time tolerance (s) | 1.0 |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Plan Scale | Auto or fixed | Auto |
| Show Ellipse | 2DRMS ellipse | Yes |
| Grid Size | Plan view grid | 1m |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | UsblVerification |
| Certificate Code | USBL |
| Test Type | Spin or Transit |
| USBL System | System description |
| Water Depth | Test depth |
| 2DRMS | Calculated error |
| Points | Number of positions |
| Pass/Fail | Determination |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open File |
| Ctrl+C | Calculate |
| Ctrl+G | Generate Certificate |
| F5 | Refresh |
| Ctrl+1 | Spin Test Mode |
| Ctrl+2 | Transit Test Mode |

---

## Troubleshooting

### Data Issues

**Large Position Scatter**
- Check USBL calibration
- Verify sound velocity profile
- Check for multipath interference

**Depth Errors**
- Verify pressure sensor calibration
- Check tide corrections
- Verify sound velocity

### Test Issues

**Insufficient Data**
- Extend test duration
- Improve USBL signal quality
- Check for dropouts

---

## Related Documentation

- [GNSS Calibration](GnssCalibration.md)
- [Getting Started](../GettingStarted.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
