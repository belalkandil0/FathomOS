# MRU Calibration Module - User Manual

## Version 3.0.0 | Fathom OS Suite

---

## Overview

The MRU Calibration Module performs **Motion Reference Unit (MRU) calibration and verification** by inter-system comparison. This module implements the Subsea7 standard procedure for verifying Pitch and Roll sensors using 3-sigma outlier rejection.

### Key Features

- **Dual Sensor Support**: Process both Pitch and Roll data simultaneously
- **Inter-System Comparison**: Compare reference MRU against system under test
- **3-Sigma Outlier Rejection**: Automatic detection and rejection of statistical outliers
- **Smart Column Auto-Detection**: Automatically finds Pitch/Roll columns (P, R, Motion, MRU patterns)
- **Data Preview & Time Filter**: View loaded data and select specific time ranges
- **Dark/Light Theme Toggle**: Switch between Subsea7 branded dark and light themes
- **Modern Help Dialog**: Tabbed help with complete step-by-step guides
- **Interactive Charts**: OxyPlot-based visualization with 15+ chart types
- **Chart Export**: Export charts to PNG or PDF individually
- **Color Customization**: Choose from preset color schemes or customize colors
- **Enhanced Statistics**: RMSE, 95% CI, Shapiro-Wilk normality test, Skewness, Kurtosis
- **Professional Reports**: Subsea7-branded PDF reports with embedded charts
- **Session Persistence**: Save and resume calibration sessions

---

## What's New in v3.0.0

### Modern Help Dialog
- **Tabbed Interface**: Dedicated tabs for Overview and each of the 7 workflow steps
- **Rich Content**: Detailed explanations with tips, warnings, and best practices
- **Contextual Access**: Press F1 or click Help to open directly to current step

### Enhanced Data Preview
- **All Columns Visible**: Time, Reference/Verified values, C-O, Z-Score, Status
- **Color-Coded Status**: Green for accepted, red for rejected points
- **Full Calculated Values**: See all processing results in the grid

### New Chart Types
- **Q-Q Plot**: Quantile-Quantile plot to verify normal distribution of residuals
- **Autocorrelation Plot**: Detect patterns and systematic errors in residuals
- **Before/After Chart**: Visual comparison showing effect of outlier rejection

### Chart Export Feature
- **Export to PNG**: Save all charts as high-resolution PNG images
- **Export to PDF**: Save individual charts as PDF files
- **Light Theme Export**: Automatically uses light background for printing

### Enhanced Statistics
- **RMSE**: Root Mean Square Error for overall accuracy
- **95% Confidence Interval**: Uncertainty bounds on the mean C-O value
- **Shapiro-Wilk Test**: Statistical test for normality (p-value displayed)
- **Skewness & Kurtosis**: Distribution shape indicators
- **Median & Quartiles**: Robust statistics (Q1, Median, Q3, IQR)

### Theme & UI Improvements
- **Fixed ComboBox Colors**: Dropdown menus now display correctly in dark theme
- **Modern Styling**: Improved dropdown appearance with shadows and rounded corners

---

## What's New in v2.2.0

### Bug Fixes
- **Fixed C-O Axis Jumping**: Secondary axis now uses fixed range to prevent jumping during scrolling
- **Fixed Axis Key Error**: Resolved "cannot find axes with key 'Secondary'" error in Step 5 charts

### New Chart Types
- **Correlation Scatter Plot**: Reference vs Verified values to identify systematic bias
- **Residual Trend Plot**: C-O over time with linear trend line to detect drift
- **Box Plot Summary**: Side-by-side comparison of Pitch and Roll distributions
- **Cumulative Distribution (CDF)**: Probability distribution of C-O values
- **Moving Average Chart**: Identifies gradual drift trends
- **Pitch vs Roll Correlation**: Detects coupled errors between sensors

### Color Customization
- **Color Scheme Selector**: Choose from Default, High Contrast, Color Blind Friendly, or Monochrome
- **Customizable Colors**: Reference line, Verified line, C-O line, Accepted/Rejected points, Mean line, Sigma boundaries
- **Reset to Defaults**: One-click reset to Subsea7 brand colors

---

## What's New in v2.1.0

- **Theme Toggle**: Click the sun/moon icon in the header to switch themes
- **Step Help**: Click the help icon (?) for guidance on the current step
- **Improved Auto-Detect**: Better column detection for various naming conventions
- **Data Preview Grid**: View loaded data in Step 3 before processing
- **Time Range Filter**: Select specific time ranges to process (Step 3)
- **Fixed ComboBox Colors**: Dropdown menus now display correctly in dark theme

---

## The Fathom 7 Process

The module guides you through **7 structured steps** to complete a calibration or verification:

| Step | Name | Purpose |
|:----:|------|---------|
| 1 | **Select** | Choose sensor type and calibration purpose |
| 2 | **Load** | Import NPD file and map columns |
| 3 | **Configure** | Enter project information and MRU system details |
| 4 | **Process** | Calculate C-O values and apply statistical analysis |
| 5 | **Analyze** | Review charts, residuals, and outliers |
| 6 | **Verify** | QC checks, accept/reject decision, and sign-off |
| 7 | **Export** | Generate PDF report and Excel data package |

---

## Step-by-Step Guide

### Step 1: Select

**Purpose**: Choose the type of calibration exercise.

#### Calibration Mode
- Reference system has its C-O (Correction-Observed) applied in NaviPac
- System being calibrated has C-O set to **0**
- Determines the C-O value of the system being calibrated

#### Verification Mode
- Both systems have their known C-O values applied
- Verifies that the existing C-O is still valid
- Used for periodic verification checks

---

### Step 2: Load

**Purpose**: Import NaviPac NPD survey data file.

1. Click **"Browse for NPD File"**
2. Select a `.npd` or `.csv` file from NaviPac
3. The module will auto-detect available columns
4. Map the columns:
   - **Time Column**: Timestamp (UTC)
   - **Reference Pitch/Roll**: Reference MRU sensor values
   - **Verified Pitch/Roll**: System under test values

#### File Format Notes
- NaviPac files may have split Date/Time columns
- Maximum 31,999 observations supported
- Recommended: Log data every 15 minutes

---

### Step 3: Configure

**Purpose**: Enter project and system information.

#### Project Information
- Project Title
- Project Number
- Survey Date/Time
- Observed By (surveyor initials)
- Checked By (QC checker initials)

#### Location Details
- Vessel Name
- Survey Location
- Latitude

#### MRU System Details
- **Reference System**: Model and Serial Number
- **Verified System**: Model and Serial Number

---

### Step 4: Process

**Purpose**: Calculate calibration values and statistics.

The processing engine:

1. **Calculates C-O** (Computed - Observed):
   ```
   C-O = Reference Value - Verified Value
   ```

2. **Computes Initial Statistics** (all points):
   - Mean C-O
   - Standard Deviation

3. **Applies Iterative 3-Sigma Rejection**:
   ```
   Z-score = |C-O - Mean| / StdDev
   If Z-score > threshold → Reject point
   Recalculate statistics
   Repeat until no new rejections
   ```

4. **Calculates Final Statistics** (accepted points only)

5. **Handles Midnight Crossover** (from import):
   - Auto-detects time rollover
   - Adds 1 day to subsequent times

#### Processing Controls
- **Rejection Threshold**: Adjustable sigma value (default: 3.0)
- **Process Button**: Start initial processing
- **Reprocess Button**: Recalculate with new threshold

#### Results Display
- Mean C-O value (degrees)
- Standard Deviation
- Accepted/Rejected point counts
- Rejection percentage

#### Processing Log
Real-time log showing each processing step and iteration.

---

### Step 5: Analyze

**Purpose**: Review results through interactive OxyPlot charts.

Charts are automatically generated when entering this step.

#### Chart Tabs

| Tab | Content | Description |
|-----|---------|-------------|
| **Pitch Calibration** | Main time series | Reference, Verified, C-O with 3σ boundaries |
| **Roll Calibration** | Main time series | Reference, Verified, C-O with 3σ boundaries |
| **C-O Analysis** | Scatter plots | C-O values with mean and sigma lines |
| **Distribution** | Histograms | C-O frequency distribution |

#### Main Calibration Chart
- **X-axis**: Time (HH:mm:ss)
- **Primary Y-axis (left)**: Reference & Verified values (degrees)
- **Secondary Y-axis (right)**: C-O values (degrees)
- **Annotations**: Mean line (teal), ±3σ boundaries (red dashed)

#### Data Series Colors
| Series | Color | Description |
|--------|-------|-------------|
| Reference | Blue | Reference MRU values |
| Verified | Green | System under test values |
| C-O | Orange | Calculated differences (accepted) |
| Rejected | Red × | Outlier points beyond 3σ |

#### Statistics Panel
For each sensor (Pitch/Roll):
- **Mean C-O**: Average of accepted points (degrees)
- **Std Dev**: Standard deviation of C-O
- **Min/Max C-O**: Range of accepted values
- **Accepted**: Count of accepted observations
- **Rejected**: Count and percentage of rejected points

#### Refresh Charts
Click the **Refresh Charts** button to regenerate all charts after reprocessing data.

---

### Step 6: Verify

**Purpose**: QC decision and digital sign-off.

#### QC Checklist
All four checks must be completed before sign-off is enabled:

| Check | Description |
|-------|-------------|
| **Statistics within range** | Mean C-O and Std Dev are reasonable for sensor type |
| **Rejection reasonable** | Typically less than 5% rejection rate expected |
| **No systematic drift** | C-O values stable over time, no trending |
| **Data quality verified** | Sufficient observations, clean data |

#### Sign-off Controls
For each sensor (Pitch/Roll):

1. **Surveyor Initials**: Enter initials of calibration surveyor
2. **Witness Initials**: Enter initials of witness (if required)
3. **Comments**: Optional notes about the calibration
4. **Accept/Reject Buttons**: Record decision

#### Decision States
| Decision | Color | Effect |
|----------|-------|--------|
| Not Decided | Gray | Cannot proceed to export |
| Accepted | Green | C-O value approved for use |
| Rejected | Red | Calibration failed, re-test required |

---

### Step 7: Export

**Purpose**: Generate final deliverables.

#### Export Options

| Button | Output | Description |
|--------|--------|-------------|
| **PDF Report** | .pdf | Subsea7-branded calibration report |
| **Excel Data** | .xlsx | Complete data workbook |
| **Export All** | .pdf + .xlsx | Both files to selected folder |

#### PDF Report Contents (QuestPDF)
- Project information header with Subsea7 branding
- MRU system details (Reference and Verified)
- Results tables for each sensor
- Decision boxes with Accept/Reject status
- Sign-off section with initials and comments
- Page numbers and generation timestamp

#### Excel Workbook Sheets (ClosedXML)
| Sheet | Content |
|-------|---------|
| **Summary** | Project info, system details, results overview |
| **Pitch Data** | All data points with status (filterable) |
| **Pitch Statistics** | Detailed statistical breakdown |
| **Roll Data** | All data points with status (filterable) |
| **Roll Statistics** | Detailed statistical breakdown |

#### File Naming
Auto-generated filename format:
```
MRU_Calibration_{VesselName}_{YYYYMMDD}.pdf
MRU_Calibration_{VesselName}_{YYYYMMDD}.xlsx
```

---

## Technical Specifications

### Supported File Formats
| Format | Extension | Notes |
|--------|-----------|-------|
| NaviPac | .npd | Primary format, auto-detected columns |
| CSV | .csv | Generic comma-separated values |
| MRU Session | .mru | Saved calibration session |

### Statistical Methods
- **Outlier Detection**: Z-score with 3-sigma threshold
- **Mean**: Arithmetic mean of accepted C-O values
- **Standard Deviation**: Sample standard deviation

### Chart Library
- **OxyPlot** for 2D visualization
- Export to PNG/PDF

---

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| File won't load | Ensure file is valid NPD/CSV format |
| Columns not detected | Check column headers match NaviPac format |
| All points rejected | Check data quality, consider adjusting threshold |
| Time axis wrong | File may have midnight crossover - auto-handled |

### Contact Support
For technical assistance, contact the Fathom OS Support.

---

## Implementation Status

| Step | Status | Features |
|------|--------|----------|
| 1. Select | ✅ Complete | Purpose selection (Calibration/Verification) |
| 2. Load | ✅ Complete | NPD file loading, column mapping, auto-detect, import |
| 3. Configure | ✅ Complete | Project info, MRU system details, personnel |
| 4. Process | ✅ Complete | C-O calculation, 3-sigma rejection, iterative processing |
| 5. Analyze | ✅ Complete | OxyPlot charts, statistics, histograms |
| 6. Verify | ✅ Complete | QC checklist, Accept/Reject decisions, sign-off |
| 7. Export | ✅ Complete | PDF report (QuestPDF), Excel workbook (ClosedXML) |

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | Dec 2024 | Initial release - Phase 1 foundation |
| 1.1.0 | Dec 2024 | Phase 2 - NPD loading, column mapping, project config |
| 1.2.0 | Dec 2024 | Phase 3 - Core processing engine, C-O calculation, 3-sigma rejection |
| 1.3.0 | Dec 2024 | Phase 4 - OxyPlot charts, analysis views, statistics display |
| 2.0.0 | Dec 2024 | Phase 5 - COMPLETE: QC sign-off, PDF reports, Excel export |

---

## Appendix: Formula Reference

### C-O Calculation
```
C-O = Reference_Value - Verified_Value
```

### Z-Score (Standardized C-O)
```
Z = |C-O - Mean(C-O)| / StdDev(C-O)
```

### Rejection Criterion
```
If Z > 3.0 → Point is rejected
```

### Final Statistics (Accepted Points Only)
```
Mean_final = Average(C-O where not rejected)
SD_final = StdDev(C-O where not rejected)
```

---

**© 2024 S7 Solutions | Subsea7**
