# Sound Velocity Profile Module

**Module ID:** SoundVelocity
**Version:** 1.2.0
**Category:** Data Processing
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Supported File Types](#supported-file-types)
4. [User Interface](#user-interface)
5. [Calculation Methods](#calculation-methods)
6. [Data Processing](#data-processing)
7. [Export Formats](#export-formats)
8. [Configuration Options](#configuration-options)

---

## Overview

The Sound Velocity Profile module processes CTD (Conductivity, Temperature, Depth) cast data and calculates sound velocity profiles using industry-standard formulas. It supports various input formats and exports to common SVP file formats used by survey and sonar systems.

### Primary Use Cases

- Process CTD sensor data
- Calculate sound velocity profiles
- Apply depth-based corrections
- Export to sonar system formats
- Generate SVP comparison reports

---

## Features

### Data Import
- Multiple CTD file format support
- Automatic format detection
- Column mapping for non-standard files
- Batch processing capability

### Sound Velocity Calculation
- Chen-Millero equation (UNESCO)
- Del Grosso equation
- Wilson equation
- Side-by-side comparison

### Data Processing
- Depth binning and averaging
- Spike removal
- Smoothing filters
- Depth correction for sensor offset

### Profile Visualization
- Interactive SVP plot
- Temperature vs depth plot
- Salinity vs depth plot
- Comparison overlay

### Export
- Multiple export formats
- Custom depth intervals
- Extended header options
- Batch export

---

## Supported File Types

### Input Files

| Extension | Description |
|-----------|-------------|
| `.000-.999` | Seabird CNV cast files |
| `.svp` | Generic SVP file |
| `.ctd` | CTD data file |
| `.bp3` | Valeport BP3 file |
| `.txt` | Text-based SVP |
| `.csv` | Comma-separated values |

### Output Files

| Extension | Description | Systems |
|-----------|-------------|---------|
| `.usr` | USR format | Generic |
| `.vel` | Velocity file | Kongsberg |
| `.pro` | Profile file | Reson/TELEDYNE |
| `.svp` | SVP format | Multiple |
| `.csv` | CSV export | Analysis |
| `.xlsx` | Excel export | Reporting |

---

## User Interface

### Main Window Layout

```
+------------------------------------------------------------------+
| [File] [Process] [View] [Export] [Help]                 [Theme]  |
+------------------------------------------------------------------+
|                                                                   |
|  +-------------------+  +--------------------------------------+ |
|  | Cast List         |  | Profile View                         | |
|  |                   |  |                                      | |
|  | [x] Cast 001      |  |     Depth (m)                       | |
|  | [x] Cast 002      |  |     0 ┌────────────────┐            | |
|  | [ ] Cast 003      |  |    50 │    ╲           │            | |
|  |                   |  |   100 │      ╲         │            | |
|  | Statistics:       |  |   150 │        ╲       │            | |
|  | Min SV: 1490 m/s  |  |   200 │          ─     │            | |
|  | Max SV: 1520 m/s  |  |       └────────────────┘            | |
|  | Avg SV: 1505 m/s  |  |       1490   1500   1510 m/s        | |
|  +-------------------+  +--------------------------------------+ |
|                                                                   |
|  [Load Files]  [Process]  [Export USR]  [Export VEL]             |
+------------------------------------------------------------------+
| Status: 3 casts loaded | Max depth: 250m | Formula: Chen-Millero |
+------------------------------------------------------------------+
```

### Screenshot Placeholder

*[Screenshot: Sound Velocity Profile module showing loaded CTD data with depth profile chart and calculated sound velocity curve]*

---

## Calculation Methods

### Chen-Millero Equation (UNESCO)

The UNESCO Chen-Millero equation is the default and most widely used:

```
c(S,T,P) = Cw(T,P) + A(T,P)S + B(T,P)S^(3/2) + D(T,P)S^2

Where:
- c = Sound velocity (m/s)
- S = Salinity (PSU)
- T = Temperature (C)
- P = Pressure (dbar)
- Cw, A, B, D = Polynomial coefficients
```

### Del Grosso Equation

Alternative formula often used for deep water:

```
c = 1402.392 + CT + CS + CP + CSTP

Where CT, CS, CP, CSTP are polynomial terms for
temperature, salinity, pressure, and cross-terms.
```

### Wilson Equation

Simpler equation for quick calculations:

```
c = 1449.2 + 4.6T - 0.055T^2 + 0.00029T^3 + (1.34 - 0.01T)(S - 35) + 0.016D
```

---

## Data Processing

### Processing Pipeline

1. **Load Raw Data** - Import CTD file(s)
2. **Quality Check** - Identify spikes and gaps
3. **Apply Corrections** - Sensor offsets, salinity
4. **Calculate SV** - Apply selected formula
5. **Bin/Average** - Depth-based averaging
6. **Smooth** - Optional smoothing filter
7. **Export** - Output to selected format

### Depth Binning

| Bin Size | Use Case |
|----------|----------|
| 0.5 m | High resolution shallow water |
| 1.0 m | Standard survey work |
| 2.0 m | Deep water surveys |
| 5.0 m | Quick profiles |

### Spike Removal

- Standard deviation threshold (default: 3-sigma)
- Median filter pre-processing
- Manual spike flagging

### Smoothing Options

| Method | Description |
|--------|-------------|
| None | Use raw calculated values |
| Moving Average | Simple averaging window |
| Median Filter | Median value in window |
| Savitzky-Golay | Polynomial smoothing |

---

## Export Formats

### USR Format

Standard format supported by many systems:

```
DEPTH    VELOCITY
   0.00   1520.45
   1.00   1520.12
   2.00   1519.85
   ...
```

### VEL Format (Kongsberg)

Kongsberg multibeam format:

```
[HEADER]
LATITUDE=59.1234
LONGITUDE=5.5678
DATE=2026/01/15
TIME=14:30:00
[DATA]
0.00,1520.45
1.00,1520.12
...
```

### PRO Format (Reson)

Reson/TELEDYNE format:

```
RESON SVP FORMAT
Lat: 59.1234 N
Lon: 5.5678 E
Date: 15/01/2026 14:30
0.00 1520.45
1.00 1520.12
...
```

---

## Configuration Options

### Calculation Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Formula | Calculation equation | Chen-Millero |
| Salinity | Default salinity (PSU) | 35.0 |
| Latitude | Location latitude | 0.0 |
| Sensor Offset | Depth sensor offset (m) | 0.0 |

### Processing Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Bin Size | Depth averaging interval | 1.0 m |
| Spike Threshold | Sigma for spike detection | 3.0 |
| Smoothing | Smoothing method | None |
| Window Size | Smoothing window | 5 |

### Export Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Header Type | Full, Minimal, None | Full |
| Decimal Places | Velocity precision | 2 |
| Depth Units | Meters, Feet | Meters |
| Velocity Units | m/s, ft/s | m/s |

---

## Certificate Metadata

| Field | Description |
|-------|-------------|
| Module ID | SoundVelocity |
| Certificate Code | SVP |
| Cast Count | Number of casts processed |
| Max Depth | Maximum depth in profile |
| Formula Used | Calculation equation |
| Export Format | Output file format |
| Position | Lat/Lon of cast |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Load Files |
| Ctrl+P | Process |
| Ctrl+E | Export |
| F5 | Refresh View |
| Delete | Remove Cast |

---

## Troubleshooting

### Import Issues

**File Format Not Recognized**
- Check file extension
- Verify file content format
- Try manual column mapping

**Missing Temperature/Salinity**
- Set default salinity value
- Check column mapping
- Verify sensor data quality

### Calculation Issues

**Unrealistic SV Values**
- Check input data ranges
- Verify units (C vs F, PSU vs ppt)
- Check depth values

---

## Related Documentation

- [Getting Started](../GettingStarted.md)
- [Survey Listing](SurveyListing.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
