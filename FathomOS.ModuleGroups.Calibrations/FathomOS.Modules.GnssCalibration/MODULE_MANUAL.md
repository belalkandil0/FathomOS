# GNSS Calibration & Verification Module

## Complete User Manual - Version 4.5.4

---

## Table of Contents

1. [Overview](#overview)
2. [System Requirements](#system-requirements)
3. [Installation](#installation)
4. [Quick Start](#quick-start)
5. [7-Step Workflow](#7-step-workflow)
6. [File Formats](#file-formats)
7. [Technical Reference](#technical-reference)
8. [Charts & Visualization](#charts--visualization)
9. [Export Options](#export-options)
10. [Troubleshooting](#troubleshooting)
11. [Version History](#version-history)

---

## Overview

The GNSS Calibration & Verification Module is a professional survey software tool for comparing and validating GNSS (Global Navigation Satellite System) positioning systems. It provides comprehensive statistical analysis, outlier detection, and visualization capabilities essential for quality control in offshore survey operations.

### Key Capabilities

| Feature | Description |
|---------|-------------|
| **GNSS Comparison** | Compare two positioning systems (System 1 vs System 2) |
| **C-O Calculation** | Compute Computed minus Observed differences |
| **Statistical Analysis** | 2DRMS, CEP50, CEP95, Mean, StdDev, RMS, Max Radial from Avg |
| **Tolerance Check** | Pass/Fail against user-defined 2DRMS specification |
| **Outlier Filtering** | Configurable sigma threshold (1-5σ) |
| **Time Range Slider** | Interactive slider to select start/end time for data filtering |
| **Full Data Loading** | Load ALL data rows instantly with loading indicator |
| **12 Chart Types** | Comprehensive visualization suite with interactive tooltips |
| **Premium UI** | Modern card-based design with professional styling |
| **Project Save/Load** | Save and resume work with .gnss project files |
| **Batch Processing** | Process multiple NPD files with consistent settings |
| **Comparison History** | Track all processing runs with results |
| **Keyboard Shortcuts** | F5=Process, Ctrl+S=Save, Ctrl+E=Export, etc. |
| **Data Preview** | Full DataGrid popup with CSV export for all data |
| **Multiple Exports** | PDF with charts, Full Data PDF, Excel, Chart images |
| **Multi-Unit Support** | Meters, Feet, US Survey Feet |

---

## System Requirements

| Component | Requirement |
|-----------|-------------|
| **Operating System** | Windows 10 or Windows 11 (64-bit) |
| **Runtime** | .NET 8.0 Desktop Runtime |
| **Memory** | 4 GB RAM minimum, 8 GB recommended |
| **Display** | 1366 x 768 minimum, 1920 x 1080 recommended |
| **Disk Space** | 50 MB for module |

---

## Installation

1. Ensure Fathom OS Shell application is installed
2. Copy the module folder to:
   ```
   [FathomOS]/Modules/_Groups/Calibrations/GnssCalibration/
   ```
3. Restart Fathom OS if running
4. Module appears in the **Calibrations** group on the dashboard

### Module Structure

```
FathomOS.ModuleGroups.Calibrations/
├── GroupInfo.json
├── icon.png
└── FathomOS.Modules.GnssCalibration/
    ├── FathomOS.Modules.GnssCalibration.dll
    ├── ModuleInfo.json
    └── Assets/icon.png
```

---

## Quick Start

1. **Load Data**: Step 1 → Enter project info → Browse for NPD/CSV/POS file
2. **Map Columns**: Step 2 → Select E/N/H columns for both systems
3. **Configure**: Step 3 → Set sigma threshold (default: 3σ)
4. **Process**: Step 4 → Click "Process Data"
5. **Review**: Step 5 → Accept/reject individual points
6. **Analyze**: Step 6 → View statistics tables
7. **Export**: Step 7 → Generate PDF/Excel reports

---

## 7-Step Workflow

### Step 1: Project & Files

- Enter project name, vessel, date, location, operator
- Select comparison node identifier
- **Browse** to load NPD, CSV, or POS file
- Optionally load secondary POS file

### Step 2: Columns & Preview

- **System 1 Mapping**: Select Easting, Northing, Height columns
- **System 2 Mapping**: Select corresponding columns
- **Time Filter**: Enable to focus on specific time range
- **Data Preview**: Full virtualized data table (all rows visible)
- **NaviPac Option**: Enable date/time split for NaviPac files

### Step 3: Settings

- **Outlier Filtering**: Enable/disable
- **Sigma Threshold**: Adjust 1-5σ with slider
- **Data Summary**: View file info and column counts

### Step 4: Process & Data

- **Process Data Button**: Execute C-O calculations
- **Full Data Table**: All comparison points with:
  - Index, Time, ΔE, ΔN, ΔH, Radial
  - System 1 and System 2 coordinates
  - Status (OK / Rejected)
- Rejected points highlighted in red

### Step 5: Quality Control (QC)

- Interactive point list
- Toggle individual points as accepted/rejected
- Real-time statistics update

### Step 6: Statistics

- **Raw Data Statistics**: Before filtering
- **Filtered Data Statistics**: After outlier removal
- Metrics: Mean, StdDev, RMS, 2DRMS, CEP50, CEP95, Min, Max

### Step 7: Charts & Export

- **12 Charts**: Tabbed interface
- **Theme Selector**: 6 professional themes
- **Export Buttons**:
  - Summary PDF (with embedded charts)
  - Full Data PDF (paginated table)
  - Excel Workbook
  - Chart Images (12 PNGs)
  - Export All

---

## File Formats

### NPD Files (NaviPac)

Comma-separated survey data from NaviPac software.

**NaviPac Date/Time Convention**:
```
Header: Time,East,North,Depth,Heading      (5 columns)
Data:   22/08/2025,14:30:45,2105742.10,... (6 values)
```

Enable "NaviPac date/time split" option in Step 2.

### CSV Files

Standard comma-separated values with header row. Auto-detects columns based on naming patterns.

### POS Files (Applanix)

GNSS trajectory data with automatic coordinate conversion:

| Coordinate Type | Handling |
|-----------------|----------|
| Geographic (WGS84) | Converted to UTM |
| UTM | Used directly |
| Local Grid | Used as-is |

---

## Technical Reference

### Delta Calculation (C-O)

```
ΔE = System1_Easting − System2_Easting
ΔN = System1_Northing − System2_Northing
ΔH = System1_Height − System2_Height
Radial = √(ΔE² + ΔN²)
```

### Statistical Formulas

| Metric | Formula | Description |
|--------|---------|-------------|
| **Mean** | μ = Σx / n | Arithmetic average |
| **Std Dev** | σ = √(Σ(x-μ)² / n) | Population standard deviation |
| **RMS** | √(Σx² / n) | Root Mean Square |
| **2DRMS** | 2 × √(σE² + σN²) | ~95% horizontal accuracy |
| **CEP 50%** | Median radial | 50% of points within |
| **CEP 95%** | 95th percentile | 95% of points within |

### Sigma Filtering

| Threshold | Data Retained | Use Case |
|-----------|---------------|----------|
| 1σ | ~68.3% | Very strict |
| 2σ | ~95.4% | Moderate |
| **3σ** | ~99.7% | **Standard (default)** |
| 4σ | ~99.99% | Minimal |
| 5σ | ~99.9999% | Extreme outliers only |

**Algorithm**:
1. Calculate mean (μ) and std dev (σ) of radial distances
2. For each point: Z = |radial - μ| / σ
3. If Z > threshold → reject point
4. Recalculate statistics on accepted points

---

## Charts & Visualization

### 12 Professional Chart Types

| # | Chart | Purpose |
|---|-------|---------|
| 1 | **Delta Scatter** | ΔE vs ΔN with CEP50, CEP95, 2DRMS circles |
| 2 | **Time Series** | ΔE, ΔN, Radial over time |
| 3 | **Histogram** | Radial error distribution |
| 4 | **CDF** | Cumulative Distribution Function |
| 5 | **Height Analysis** | ΔH with ±1σ bands |
| 6 | **Polar Error** | Error by bearing (N/E/S/W) |
| 7 | **Error Components** | Bar chart Mean/StdDev/RMS |
| 8 | **Q-Q Plot** | Normal probability plot |
| 9 | **Running Stats** | Moving average trend |
| 10 | **Error Ellipse** | Confidence ellipses (39%, 68%, 95%, 99%) |
| 11 | **Raw Position** | System 1 vs System 2 raw |
| 12 | **Filtered Position** | Filtered comparison |

### 6 Color Themes

| Theme | Best For |
|-------|----------|
| Subsea7 Professional | Official reports (default) |
| High Contrast | Presentations |
| Ocean Blue | Marine theme |
| Monochrome | B&W printing |
| Earth Tones | Extended use |
| Vibrant Modern | Dashboards |

---

## Export Options

### Summary PDF
- Project information
- Complete statistics (Raw & Filtered)
- All 12 charts embedded as images
- Professional Subsea7 branding

### Full Data PDF
- Complete data table (all points)
- Paginated (landscape orientation)
- Shows ΔE, ΔN, ΔH, Radial, Status

### Excel Workbook
- **Summary Sheet**: Project info, key metrics
- **Raw Data Sheet**: All points, full precision
- **Filtered Data Sheet**: Accepted points only
- **Statistics Sheet**: Complete breakdown

### Chart Images
- 12 high-resolution PNG files
- Numbered: 01_DeltaScatter.png through 12_FilteredPosition.png
- Optimized dimensions per chart type

---

## Troubleshooting

### No data after loading file
- Verify file format (NPD, CSV, or POS)
- Check column mapping in Step 2
- Enable "NaviPac date/time split" for NaviPac files

### All points rejected
- Increase sigma threshold (try 4σ or 5σ)
- Disable filtering to inspect raw data
- Check for systematic offsets

### Charts not displaying
- Ensure data is processed (Step 4)
- Try a different theme
- Resize window if truncated

### Export fails
- Check destination folder permissions
- Close any open files with same name
- Verify disk space

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| **4.3.0** | Jan 2025 | Project Save/Load (.gnss files), Settings Persistence, Keyboard Shortcuts (F5, Ctrl+S, Ctrl+E, etc.), Chart Tooltips, Data Point Highlighting, Batch Processing, Comparison History |
| 4.2.0 | Jan 2025 | Tolerance specification (Pass/Fail check), Max Radial from Average calculation, enhanced 2DRMS chart with info panel, improved error handling |
| 4.1.0 | Jan 2025 | Fixed chart errors (Error Components, Help Window), redesigned 2DRMS chart to match reference style |
| 4.0.0 | Dec 2024 | Complete workflow redesign, PDF with charts, Full Data PDF |
| 3.8.0 | Dec 2024 | 12 charts, 6 themes, CEP circles |
| 3.7.0 | Dec 2024 | Performance, async rendering |
| 3.5.0 | Dec 2024 | Tabbed layout, help window fix |
| 3.3.0 | Dec 2024 | Modern charts, Excel improvements |
| 3.0.0 | Dec 2024 | POS file support, coordinate conversion |
| 1.0.0 | Dec 2024 | Initial release |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **F5** | Process Data |
| **Ctrl+S** | Save Project |
| **Ctrl+Shift+S** | Save Project As |
| **Ctrl+O** | Open Project |
| **Ctrl+N** | New Project |
| **Ctrl+E** | Export to Excel |
| **Ctrl+P** | Export to PDF |
| **F1** | Show Help |
| **Escape** | Clear Selection |

---

## Version History

### Version 4.5.2 (January 2025) - Final Polish
**Dynamic Navigation Button:**
- "Next" button now changes based on current step:
  - Step 4 (Process): Shows "Complete Analysis" with checkmark icon
  - Step 7 (Export): Shows "Finish" with check icon
  - Other steps: Shows "Next" with arrow icon
- Uses DataTriggers for proper icon switching (binding doesn't work with PackIconMaterial)

**Chart Auto-Fit:**
- Added `model.ResetAllAxes()` to all 13 chart creation methods
- Charts now automatically fit data to view on initial display
- Ensures proper zoom level when charts are first rendered

**Chart Theme Dropdown:**
- Fixed: Changing theme now properly regenerates all charts with new colors
- 6 color presets available: Subsea7 Professional, High Contrast, Ocean Blue, Monochrome, Earth Tones, Vibrant Modern
- Charts fully support zoom/pan with mouse interaction

### Version 4.5.1 (January 2025) - UI Overhaul & Theme Fixes
**UI Space Optimization:**
- Moved Help, Theme, New buttons to window title bar (next to min/max/close)
- Removed redundant header with logo/title (window title bar already shows this)
- Replaced large step circles with compact step "pills" - saves ~80px vertical space
- Removed step descriptions from step bar
- Moved point counters to step bar (right side)
- Reduced margins and padding throughout

**Theme Fixes:**
- Fixed dark theme dropdowns showing white background
- Added comprehensive ComboBox, ListBox, Popup styling for dark theme
- Replaced all hardcoded "White" backgrounds with DynamicResource CardBrush
- Step7 and Step6 now properly theme-aware

**Chart Fixes:**
- Fixed 2DRMS circle chart not showing (was binding to wrong property name)
- Fixed Q-Q Plot not showing (was binding to QQPlotModel instead of QqPlotModel)

**Removed:**
- Debug panel from Step 2 (was added for column mapping diagnostics)

### Version 4.5.0 (January 2025) - CRITICAL FIX: Duplicate Column Names
- **ROOT CAUSE FOUND**: Files with duplicate column names (e.g., two "North" columns) caused IndexOf() to return the first match
- **BUG**: When user selected the second "North" column for System 2, `IndexOf("North")` returned index of the FIRST "North" (System 1's column)
- **FIX**: Added `MakeColumnNamesUnique()` method that appends " (N)" suffix to duplicate column names
  - First "North" stays as "North"
  - Second "North" becomes "North (2)"
- **FIX**: Added `_columnNameToIndex` dictionary to map unique display names to actual column indices
- **FIX**: Replaced all `IndexOf()` calls with `GetColumnIndex()` that uses the stored mapping
- **Result**: Dropdown now shows unique names, and selecting "North (2)" correctly maps to the second North column
- **Debug panel retained**: Yellow panel still shows current indices for verification

### Version 4.4.9 (January 2025) - VISUAL DEBUG VERSION
- **Added**: Visible debug panel on Step 2 (Load) showing:
  - Current column mapping indices for both System 1 and System 2
  - Currently selected column names from dropdowns
- **Purpose**: This yellow panel helps you verify if your dropdown selections are being applied
- **Expected behavior**: When you change a dropdown, the indices should update immediately
- **If indices stay at -1**: The selected column name isn't found in AvailableColumns
- **To remove this panel later**: Delete the "DEBUG: Column Mapping Indices" Border in Step2LoadView.xaml

### Version 4.5.2 (January 2025) - DEBUG VERSION
- **Added**: Comprehensive debug logging throughout column mapping flow
  - UpdateColumnMapping logs all selected columns and resolved indices
  - PopulateDataPreview logs mapping hash and all indices before loading
  - GetRawPreview logs incoming mapping, auto-detected, and final resolved indices
  - Parse method logs user mapping vs auto-detected vs final indices
- **Debug Output**: Run in Visual Studio Debug mode to see [GNSS] prefixed messages in Output window
- This version helps diagnose exactly where user column selections are being lost

### Version 4.5.2 (January 2025)
- **CRITICAL FIX**: User column selections were being overwritten when clicking "Next" on Step 2
  - Root cause: `DetectColumns()` was called twice - once when file loaded, and again when leaving Step 2
  - The second call overwrote any manual column selections the user made
- **Fixed**: `PerformStepAction(2)` no longer calls `DetectColumns()` - only preserves existing mapping
- **Added**: try/finally protection to ensure `_isSettingColumnsFromDetection` flag is always reset

### Version 4.5.2 (January 2025)
- **Fixed**: Column mapping issue where user-selected columns were being overridden by auto-detection
- **Added**: Validation for Height columns being the same (prevents System 1 and System 2 using same height column)
- **Improved**: Better error messages showing actual column names when duplicate columns detected
- **Added**: Debug logging for column mapping to help diagnose mapping issues
- **Improved**: User selections now take absolute priority over auto-detection

### Version 4.5.2 (January 2025)
- **Fixed**: CS0103 - `TwoDrmsPlotModel` renamed to `ErrorEllipsePlotModel` in chart export
- **Fixed**: CS1503 - PdfReportService ComposeContent call using proper Element callback pattern

### Version 4.3.8 (January 2025) - Comprehensive Bug Fix
- **Fixed**: Opacity binding error in Step 2 time filter section
- **Fixed**: Duplicate Mode=OneWay declarations in Step 7 bindings
- **Fixed**: All read-only property bindings now use Mode=OneWay:
  - TotalPointCount, AcceptedPointCount, RejectedPointCount
  - UnitAbbreviation, Project properties
  - Statistics properties (all values)
  - StatusMessage, LoadedFileName, etc.
- **Improved**: Time filter panel now shows disabled state with reduced opacity
- **Improved**: UI consistency across all step views

### Version 4.3.7 (January 2025) - Binding Fix
- **Fixed**: UnitAbbreviation read-only binding crash
- **Fixed**: Statistics bindings across Step 6 and Step 7

### Version 4.3.6 (January 2025) - Binding Fix
- **Fixed**: TotalPointCount read-only binding crash
- **Fixed**: AcceptedPointCount, RejectedPointCount bindings

### Version 4.3.5 (January 2025) - Bug Fix
- **Fixed**: DataPreviewRow.RowValues compilation error in ShowDataPreviewPopup

### Version 4.3.4 (January 2025) - Professional Workflow Update
- **Redesigned Step 2**: Removed embedded data preview, added "Preview All Data" popup button with confirmation checkbox
- **Redesigned Step 6**: Fixed table visibility with ScrollViewer, removed broken theme dropdown, added "Preview All Data" popup button
- **Redesigned Step 7**: Professional supervisor approval workflow
  - Supervisor Name and Initials required before approval
  - APPROVE button validates input and confirms
  - Export buttons disabled until approved
  - Approval info displayed after confirmation
- **Added**: Data preview popup shows all points (up to 500 in popup, full export available)
- **Improved**: Tables now fully visible with proper scrolling

### Version 4.3.3 (January 2025)
- **Improved**: Delta Scatter now shows ALL points (removed 2000 point decimation limit)
- **Added**: Chart Theme dropdown in Step 7 with 6 color presets
- **Added**: "COMPLETE" button with supervisor confirmation popup
- **Added**: Data Table tab in Step 7 showing all comparison points
- **Moved**: Color palette selector from Step 6 to Step 7

### Version 4.4.2 (January 2025)
- **Improved**: Column auto-detection patterns now more flexible (matches DGNSS, GNSS, GPS, Primary/Secondary, Ref/Obs, System1/2 naming conventions)
- **Fixed**: Recursive data preview loading during column detection
- **Improved**: Status messages now show which columns were auto-detected
- **Improved**: Better feedback when columns cannot be auto-detected

### Version 4.4.1 (January 2025)
- **Fixed**: VirtualizingPanel attached property build errors (CS0103, CS0747)
- **Fixed**: Correct syntax for setting attached properties in code-behind

### Version 4.4.0 (January 2025) - Major UI Overhaul
- **NEW**: Time Range Slider for intuitive time filtering with start/end handles
- **NEW**: Full Data Loading - loads ALL data rows (no 100 row limit)
- **NEW**: Loading overlay with progress indicator for large files
- **NEW**: DataGrid-based data preview popup with virtualization for performance
- **NEW**: CSV export from data preview popup
- **NEW**: Filter statistics panel showing total, selected, and duration
- **Improved**: Premium UI redesign with card-based layout
- **Improved**: Gradient headers for GNSS 1 and GNSS 2 cards
- **Improved**: Data preview automatically refreshes when column mapping changes
- **Fixed**: Data not loading when selecting columns from dropdowns

### Version 4.3.8 (January 2025)
- **Fixed**: Comprehensive binding error fixes (Mode=OneWay for read-only properties)
- **Fixed**: Opacity binding error in Step 2 (changed to DataTrigger Style)
- **Fixed**: Duplicate Mode=OneWay declarations in Step 7

### Version 4.3.7 (January 2025)
- **Fixed**: TotalPointCount binding error
- **Fixed**: AcceptedPointCount binding error  
- **Fixed**: RejectedPointCount binding error
- **Fixed**: All Statistics.* binding errors
- **Fixed**: UnitAbbreviation binding errors across all views

### Version 4.3.6 (January 2025)
- **Fixed**: UnitAbbreviation read-only binding error

### Version 4.3.5 (January 2025)
- **Fixed**: DataPreviewRow.RowValues compilation error

### Version 4.3.4 (January 2025)
- **Improved**: Step 2 data preview as popup window (up to 500 rows)
- **Improved**: Step 6 scrollable statistics tables
- **Improved**: Step 7 supervisor approval workflow

### Version 4.3.3 (January 2025)
- **Removed**: Chart point limits (now renders up to 50K points)
- **Added**: Theme dropdown selector
### Version 4.5.2 (January 2025)
- **Fixed**: Added missing binding alias properties (SampleCount, MeanRadial, StdDevRadial, DeltaStdDev*, DeltaRms*)
- **Fixed**: All IsChecked bindings now include Mode=TwoWay for proper checkbox binding
- **Fixed**: ExportPlotToPng default parameters for chart export calls
- **Code Review**: Comprehensive code quality review completed
  - Verified all models have required properties
  - Confirmed proper exception handling in async methods
  - Validated disposable patterns (using statements)
  - Confirmed event handler cleanup in OnClosed

### Version 4.4.3 (January 2025)
- **Fixed**: Step 6 Statistics table color contrast (white backgrounds with dark text for readability)
- **Added**: Professional "Complete Analysis" popup in Step 7 with:
  - Supervisor approval section (name, initials)
  - Export options checkboxes (PDF, Excel, Charts, Digital Certificate)
  - Project summary display
  - Modern card-based UI design
- **Added**: Digital Certificate PDF export (Pass/Fail certificate with signature line)
- **Fixed**: Validation to ensure System1 and System2 columns are different (prevents zero deltas)
- **Improved**: Error messages when columns are incorrectly mapped
- **Added**: Debug logging for column mapping to help diagnose parsing issues

### Version 4.4.2 (January 2025)
- **Fixed**: Column mapping regression - restored auto-detection and manual column selection
- **Improved**: Auto-detection patterns for GNSS column names
- **Fixed**: Recursive loading issue during column detection
- **Improved**: Status messages showing which columns were auto-detected

### Version 4.5.2 (January 2025)
- **Fixed**: Theme consistency - added subtle/tint background brushes for dark/light theme support
- **Fixed**: LightTheme missing ListBox and ListBoxItem styles
- **Fixed**: Replaced hardcoded background colors with DynamicResource for proper theme switching
- **Added**: InfoSubtleBrush, SuccessSubtleBrush, WarningSubtleBrush, ErrorSubtleBrush, PanelAltBrush, PanelHighlightBrush

### Version 4.5.4 (January 2025) - FathomOS Rebranding
- **REBRANDING**: S7 Fathom → Fathom OS throughout entire module
- **Namespace change**: S7Fathom.* → FathomOS.*
- **Certificate System**: Added certificate support for professional verification
  - Certificate Code: GC (GNSS Calibration)
  - Certificate includes: Statistics table, charts, processing data
- **CRITICAL FIX**: Restored 7 missing chart tabs that were accidentally removed in UI overhaul
- **Charts restored**: CDF, Height Analysis, Polar Error, Error Components, Running Stats, Raw Position, Filtered Position
- **Now all 12 documented charts are displayed**: Delta Scatter, 2DRMS Circle, Time Series, Histogram, Q-Q Plot, CDF, Height, Polar, Components, Running, Raw Pos, Filtered
- **UI Improvement**: Enhanced step header with prominent step number badge, title, and description
- **UI Improvement**: Added visual progress bar showing workflow completion
- **UI Improvement**: Merged Step 7 navigation - single "Complete & Export" button replaces duplicate buttons
- **UI Improvement**: Navigation bar now hidden on Step 7 (handled internally)
- **Help Guide**: Updated to version 4.5.4 with new "12 Chart Types" section

### Version 4.5.2 (January 2025)
- **UI Optimization**: Moved Help/Theme/New buttons to window title bar (RightWindowCommands)
- **UI Optimization**: Removed redundant header, compact step indicator bar
- **Fixed**: Chart bindings - ErrorEllipsePlotModel and QqPlotModel property names
- **Fixed**: Dark theme - replaced hardcoded white backgrounds with CardBrush
- **Fixed**: DarkTheme ComboBox, Popup, ListBox styling for proper dark mode
- **Removed**: Debug panel from Step 2 Load view

### Version 4.5.0 (January 2025)
- **Fixed**: Duplicate column name bug - columns with same name now properly differentiated
- **Fixed**: Dictionary-based index mapping for accurate column selection
- **Improved**: Column dropdown shows unique names with " (2)", " (3)" suffixes for duplicates

### Version 4.4.1 (January 2025)
- **Fixed**: VirtualizingPanel attached property syntax compilation errors

### Version 4.4.0 (January 2025)
- **Major UI Overhaul**: Premium card-based design throughout
- **Added**: Full data loading (no more 100-row limit)
- **Added**: Time range slider for interactive data filtering
- **Added**: Loading indicator during data operations
- **Added**: Data preview export to CSV
- **Improved**: DataGrid with virtualization for large datasets

### Version 4.3.3 (January 2025)
- **Improved**: Supervisor confirmation dialog with summary

### Version 4.3.2 (January 2025)
- **Fixed**: Q-Q Plot not rendering (binding mismatch)
- **Added**: Delta Scatter info panel with Vessel, Date, Location, 2DRMS, Max Radial
- **Added**: Data Table tab in Export step showing all comparison points
- **Fixed**: TotalCount property error in statistics

### Version 4.3.1 (January 2025)
- **Fixed**: Error Components chart using BarSeries with incorrect axis type
- **Fixed**: Delta Scatter 2DRMS circle distortion (equal axis scaling)
- **Improved**: Chart rendering stability

### Version 4.3.0 (January 2025)
- Settings Persistence
- Project File Save/Load (.gnss)
- Keyboard Shortcuts (F5, Ctrl+S, Ctrl+E, etc.)
- Chart Interactive Tooltips
- Data Point Highlighting
- Batch Processing
- Comparison History Tracking

---

## Support

**Developer**: Fathom OS - Survey Software Development  
**Platform**: Fathom OS Survey Software Suite  
**Module**: GNSS Calibration & Verification  
**Version**: 4.5.4

---

© 2024-2025 Fathom OS. All rights reserved.
