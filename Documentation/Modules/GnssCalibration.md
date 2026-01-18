# GNSS Calibration Module

**Module ID:** GnssCalibration
**Version:** 4.5.4
**Category:** Calibration & Verification
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

The GNSS Calibration module compares and validates GNSS positioning systems through statistical analysis, outlier detection, and 2DRMS calculations. It is used to verify GNSS system accuracy and inter-system agreement during survey mobilizations.

### Primary Use Cases

- Compare primary and secondary GNSS systems
- Validate positioning accuracy
- Calculate 2DRMS error metrics
- Generate calibration certificates
- Document GNSS system performance

---

## Features

### Data Import
- Parse NaviPac NPD files
- Support multiple position columns
- Automatic column detection
- Time synchronization options

### Comparison Analysis
- System-to-system comparison
- Easting and Northing differences
- 2D distance calculation
- Time-based correlation

### Statistical Processing
- Mean and standard deviation
- 3-sigma outlier rejection
- Iterative outlier removal
- 95% confidence intervals

### Visualization
- Scatter plot of differences
- Time series charts
- Histogram distribution
- Error ellipse display

### Reporting
- Professional PDF certificates
- Excel data export
- Statistical summary reports

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.npd` | NaviPac survey data |
| `.csv` | CSV position data |
| `.pos` | Generic position file |

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
|  Column Mapping                                                   |
|  +------------------------------------------------------------+ |
|  | System A (Primary):    | System B (Secondary):              | |
|  | Time:     [Column 1]   | Time:     [Same as A]              | |
|  | Easting:  [Column 2]   | Easting:  [Column 4]               | |
|  | Northing: [Column 3]   | Northing: [Column 5]               | |
|  +------------------------------------------------------------+ |
|                                                                   |
|  Results                                                          |
|  +-------------------+  +--------------------------------------+ |
|  | Statistics        |  | Scatter Plot                         | |
|  |                   |  |                                      | |
|  | dE Mean:  0.012m  |  |         *  *                        | |
|  | dE StdDev: 0.045m |  |      *    *   *                     | |
|  | dN Mean:  -0.008m |  |    *   *  x  *   *                  | |
|  | dN StdDev: 0.038m |  |      *    *   *                     | |
|  | 2DRMS:    0.118m  |  |         *  *                        | |
|  | Points:   4,521   |  |                                      | |
|  | Outliers: 23      |  |                                      | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  [Load File]  [Calculate]  [Remove Outliers]  [Generate Cert]    |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: GNSS Calibration module showing difference scatter plot with 2DRMS ellipse and statistical summary]*

---

## Calibration Process

### Step 1: Load Data

1. Open NPD file containing both GNSS systems
2. Verify both systems have valid positions
3. Check time coverage and data density

### Step 2: Map Columns

1. Select primary system Easting column
2. Select primary system Northing column
3. Select secondary system Easting column
4. Select secondary system Northing column
5. Verify time column selection

### Step 3: Calculate Differences

The module calculates for each epoch:
- dE = Easting_A - Easting_B
- dN = Northing_A - Northing_B
- dH = sqrt(dE^2 + dN^2)

### Step 4: Statistical Analysis

1. Calculate mean and standard deviation
2. Identify outliers (|value| > 3 * sigma)
3. Optionally remove outliers
4. Recalculate statistics
5. Compute 2DRMS

### Step 5: Review Results

- Check if 2DRMS meets specification
- Review outlier count
- Examine time series for trends
- Verify distribution is normal

### Step 6: Generate Certificate

1. Enter signatory information
2. Add project details
3. Generate PDF certificate
4. Save to certificate database

---

## Statistical Analysis

### Metrics Calculated

| Metric | Formula | Description |
|--------|---------|-------------|
| Mean dE | sum(dE)/n | Average Easting difference |
| Mean dN | sum(dN)/n | Average Northing difference |
| StdDev dE | sqrt(sum((dE-mean)^2)/(n-1)) | Easting standard deviation |
| StdDev dN | sqrt(sum((dN-mean)^2)/(n-1)) | Northing standard deviation |
| RMS | sqrt(mean(dE^2 + dN^2)) | Root mean square error |
| 2DRMS | 2 * sqrt(StdDev_dE^2 + StdDev_dN^2) | 2D RMS error (95%) |

### Outlier Detection

3-Sigma Rule:
- Calculate mean and standard deviation
- Flag points where |dE| > 3*StdDev_dE OR |dN| > 3*StdDev_dN
- Optionally remove and recalculate

### Iterative Rejection

1. Calculate initial statistics
2. Remove outliers beyond threshold
3. Recalculate statistics
4. Repeat until no new outliers
5. Maximum 5 iterations

### Acceptance Criteria

Typical specifications:
| Parameter | Typical Limit |
|-----------|---------------|
| 2DRMS | < 0.15 m |
| Mean Offset | < 0.05 m |
| Outlier Rate | < 1% |

---

## Export and Reporting

### PDF Certificate

The calibration certificate includes:
- Certificate header with unique ID
- Project and vessel information
- Equipment details (GNSS makes/models)
- Statistical summary table
- Scatter plot visualization
- Pass/Fail determination
- Signatory information

### Excel Export

Detailed data export containing:
- Raw difference data
- Statistical summary
- Charts (scatter, time series, histogram)
- Outlier flagging

### Data Fields

| Column | Description |
|--------|-------------|
| Time | Epoch timestamp |
| Easting_A | Primary system Easting |
| Northing_A | Primary system Northing |
| Easting_B | Secondary system Easting |
| Northing_B | Secondary system Northing |
| dE | Easting difference |
| dN | Northing difference |
| dH | 2D difference |
| Outlier | Flag (true/false) |

---

## Configuration Options

### Analysis Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Outlier Threshold | Sigma multiplier | 3.0 |
| Max Iterations | Outlier removal passes | 5 |
| Min Points | Minimum valid points | 100 |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Chart Scale | Auto or fixed scale | Auto |
| Show Ellipse | Display 2DRMS ellipse | Yes |
| Time Format | Chart time axis format | HH:mm:ss |

### Report Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Company Name | Report header | (From license) |
| Logo | Company logo | (From license) |
| Pass/Fail | Include determination | Yes |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | GnssCalibration |
| Certificate Code | GC |
| System A | Primary GNSS description |
| System B | Secondary GNSS description |
| 2DRMS | Calculated 2DRMS value |
| Total Points | Number of comparison points |
| Outliers | Number removed |
| Pass/Fail | Determination result |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open File |
| Ctrl+C | Calculate |
| Ctrl+R | Remove Outliers |
| Ctrl+G | Generate Certificate |
| F5 | Refresh View |

---

## Troubleshooting

### Data Issues

**Columns Not Detected**
- Check file format
- Verify column headers
- Try manual mapping

**Large Differences**
- Check coordinate system
- Verify time synchronization
- Check for datum differences

### Analysis Issues

**Too Many Outliers**
- Check data quality
- Verify correct columns
- Look for systematic errors

---

## Related Documentation

- [MRU Calibration](MruCalibration.md)
- [Getting Started](../GettingStarted.md)
- [Core API](../API/Core-API.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
