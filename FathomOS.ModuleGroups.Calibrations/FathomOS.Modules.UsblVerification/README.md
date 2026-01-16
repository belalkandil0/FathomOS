# USBL Verification Module

## Fathom OS Module for USBL Position Verification

A WPF module for verifying USBL (Ultra-Short BaseLine) acoustic positioning system accuracy through spin and transit tests.

---

## Overview

This module replaces the Excel-based USBL verification workflow (FOGLLOFS080) with a modern, guided 7-step process. It validates USBL position accuracy against defined tolerances for offshore survey operations.

---

## Features

### 7-Step Guided Workflow
1. **Project Setup** - Enter project info and configure unit settings
2. **Load Spin Data** - Import NPD files for 4 vessel headings (with batch import)
3. **Preview Spin Data** - Review and validate data, exclude outliers
4. **Load Transit Data** - Import NPD files for transit lines (optional)
5. **Preview Transit Data** - Review transit data
6. **Process** - Calculate verification results
7. **Results & Export** - View results, charts, quality dashboard, and export reports

### 🆕 Batch Import
- **Folder Selection** - Select a folder and auto-load all 4 spin files at once
- **Smart File Matching** - Automatically matches files to headings by filename patterns
- **Gyro-Based Fallback** - If filename doesn't match, uses average gyro to detect heading
- **Live Preview** - Watch scatter plot update as files are loaded

### 🆕 Auto-Detect Units
- Automatically guesses coordinate units based on magnitude
- Large coordinates (>1M) → likely feet (State Plane)
- Medium coordinates (100K-1M) → likely meters (UTM)
- Shows confidence level and reasoning
- Enable/disable via checkbox in Step 1

### Unit Conversion Support
- **Meters** - Base calculation unit
- **International Feet** - 1 ft = 0.3048 m (exact)
- **US Survey Feet** - 1 sft = 1200/3937 m (≈0.3048006 m)

Critical warning system alerts when unit selection may affect results at large coordinates.

### 🆕 Quality Dashboard
Visual gauges showing data quality per dataset:
- **Overall Score** (0-100) with grade (Excellent/Good/Fair/Poor/Critical)
- **Point Count Score** - Adequate sample size?
- **Outlier Score** - How clean is the data?
- **Spread Score** - Position scatter within expected bounds?
- **Consistency Score** - Low standard deviation?
- Color-coded progress bars for each metric

### 🆕 Point Selection on Charts
- Click chart points to see details
- **Point Info Panel** - Shows index, time, coordinates, and dataset
- **Toggle Exclude** - Exclude/include points directly from chart
- Highlights selected point on scatter plot

### Data Validation
- **IQR Outlier Detection** - Automatic detection using Interquartile Range method
- **Time Gap Analysis** - Warns if data gaps exceed expected intervals
- **Gyro Consistency Check** - Validates heading stability during spin tests
- **Quality Scoring** - 0-100 score for data quality assessment
- **Auto-Exclude** - Option to automatically exclude detected outliers

### Data Preview
- View loaded data before processing
- Exclude individual points manually
- See real-time statistics (count, averages, excluded points)
- Switch between individual datasets or view all combined
- **Live scatter plot** updates as data is loaded

### Interactive Charts
- **Scatter plots** with tolerance circles
- **6 chart themes**: Professional, Vibrant, Ocean, Earth, Monochrome, High Contrast
- **Legends** showing each series
- **Export to PNG** for reports

### 🆕 Report Templates
Customize PDF report branding:
- **Company Info** - Name, address, phone, email
- **Logo Upload** - Import company logo image
- **Color Scheme** - Primary, accent, pass/fail colors
- **Certificate Settings** - Number format, signatory, signature
- **Save Templates** - Create multiple templates for different clients

### 🆕 Advanced Analytics (v1.5.0)
Comprehensive statistical analysis:
- **Radial Statistics** - CEP (50%), R95, R99, DRMS, 2DRMS, 2σ radius
- **Confidence Ellipse** - 95% confidence ellipse visualization
- **Monte Carlo Simulation** - Bootstrap confidence bounds (10,000 iterations)
- **Trend Analysis** - Linear regression for drift detection with R² values
- **Time Series Charts** - Position drift over time, gyro stability
- **Histogram** - Error distribution with normal curve overlay
- **Polar Plot** - Position offsets in polar coordinates
- **Box Plot** - Outlier visualization for multiple datasets

### 🆕 Advanced Charts (v1.6.0)
Seven new specialized chart types in Step 6:
- **Error Ellipse** - Multi-level confidence ellipses (50%, 90%, 95%, 99%)
- **Heading Radar** - Polar comparison of performance metrics across headings
- **Residual Time Series** - Quality control with ±1σ/±2σ limits and outlier detection
- **Control Chart (X-bar)** - Statistical process control monitoring
- **Depth vs Slant Range** - Acoustic path analysis with regression
- **Rose Diagram** - Directional error distribution (polar histogram)
- **Smoothing Comparison** - Before/after overlay with scatter reduction %

### 🆕 Data Smoothing (v1.5.0)
Multiple smoothing algorithms:
- **Moving Average** - Simple smoothing with configurable window
- **Gaussian Filter** - Weighted smoothing with sigma parameter
- **Savitzky-Golay** - Polynomial smoothing preserving peaks
- **Median Filter** - Spike removal for noisy data
- **Exponential Moving Average** - Recent-weighted smoothing

### 🆕 Historical Database (v1.5.0)
Track all verifications:
- **Local Database** - JSON-based storage in AppData
- **Search & Filter** - By project, vessel, client, date range, pass/fail
- **Export/Import** - Backup and restore functionality
- **Quick Access** - Recent verification history in Step 7

### 🆕 Digital Signatures (v1.5.0)
Cryptographically sign certificates:
- **RSA-SHA256** - Industry-standard cryptographic signing
- **Key Management** - Generate, store, and export key pairs
- **Password Protection** - Optional encryption for private keys
- **Verification** - Validate certificate authenticity

### 🆕 Drag & Drop Support (v1.5.0)
Enhanced file handling:
- **Drop NPD Files** - Drag files directly onto the window
- **Auto-Heading Detection** - Detects heading from filename patterns
- **Recent Projects** - Quick access to recent verifications
- **Remember Directories** - Last used folder persistence

### 🆕 Verification Certificate
Auto-generated PDF certificate on successful verification:
- **Professional Layout** - Framed certificate design
- **Pass Confirmation** - Large checkmark and "VERIFICATION SUCCESSFUL"
- **Verified Position** - Official transponder coordinates
- **Results Summary** - Spin, transit, and alignment pass criteria
- **Digital Signature** - Optional signature image
- **Certificate Number** - Auto-incrementing with year

### Export Options
- **Excel (.xlsx)** - Detailed spreadsheet with all data and calculations
- **PDF** - Professional verification report with charts
- **PNG** - Chart images for presentations
- **TXT** - Plain text summary
- **🆕 Certificate** - PDF verification certificate (only when passed)

### Project Management
- **Save/Load Projects** - .usblproj JSON format
- **Auto-backup** - Keeps last 10 backups
- **Settings Persistence** - Remembers unit settings and chart theme

### Help Guide
- Built-in help flyout (button in title bar)
- Step-by-step instructions
- Tolerance criteria explanation
- Unit conversion reference

---

## Verification Tests

### Spin Test
Static positioning at 4 vessel headings (0°, 90°, 180°, 270°) to verify position repeatability regardless of vessel orientation.

**Pass Criteria:**
- Max difference between heading averages ≤ tolerance
- Tolerance = MAX(0.5m, 0.2% × slant range)

### Transit Test
Two reciprocal transit lines over the transponder to verify:
- Position consistency between lines
- Alignment (no systematic offset)
- Scale factor (distance measurement accuracy)

**Pass Criteria:**
- Max difference from spin average ≤ tolerance
- Tolerance = MAX(1.0m, 0.5% × slant range)
- Alignment ≤ ±0.1°
- Scale factor = 1.0 ±0.005

---

## Technical Details

### Outlier Detection (IQR Method)
```
Q1 = 25th percentile
Q3 = 75th percentile
IQR = Q3 - Q1
Lower bound = Q1 - 1.5 × IQR
Upper bound = Q3 + 1.5 × IQR

Outlier if: value < lower bound OR value > upper bound
```

### Quality Score Calculation
```
Score = 100
Score -= 20 × (excluded points / total points)
Score -= 2 × max(0, 10 - valid point count)
Score -= 5 × max(0, radial spread - 2.0)
```

### Unit Conversion Warning
At large coordinates (>100,000 ft), the difference between International and US Survey feet becomes significant (~0.03m per 100,000 ft).

---

## File Structure

```
FathomOS.Modules.UsblVerification/
├── Assets/icon.png                    # Module icon (128×128)
├── Converters/Converters.cs           # XAML value converters
├── Models/UsblModels.cs               # Data models
├── Parsers/UsblNpdParser.cs           # NPD file parser (uses Core NpdParser)
├── Services/
│   ├── BatchImportService.cs          # Batch import handling
│   ├── CertificateService.cs          # Verification certificate generation
│   ├── ChartExportService.cs          # PNG chart export
│   ├── DataValidationService.cs       # Outlier detection & validation
│   ├── DxfExportService.cs            # DXF CAD export
│   ├── ExcelExportService.cs          # Excel report generation
│   ├── PdfExportService.cs            # PDF report generation
│   ├── ProjectService.cs              # Save/load projects
│   ├── QualityDashboardService.cs     # Quality metrics calculations
│   ├── ReportTemplateService.cs       # Report template management
│   ├── UnitConversionService.cs       # Length unit conversions
│   └── UsblCalculationService.cs      # Verification calculations
├── Themes/
│   ├── DarkTheme.xaml                 # Dark color scheme
│   └── LightTheme.xaml                # Light color scheme
├── ViewModels/
│   ├── EventArgs.cs                   # Event args + helper classes
│   ├── MainViewModel.cs               # Main application logic
│   ├── RelayCommand.cs                # ICommand implementation
│   └── ViewModelBase.cs               # INPC base class
├── Views/
│   ├── BatchColumnMappingDialog.xaml  # Multi-file column mapping
│   ├── ColumnMappingDialog.xaml       # Single file column mapping
│   ├── MainWindow.xaml                # Main UI (7 steps)
│   └── MainWindow.xaml.cs             # Theme & dialog handling
├── ModuleInfo.json                    # Module metadata
├── INTEGRATION.md                     # Integration instructions
├── README.md                          # This file
├── FathomOS.Modules.UsblVerification.csproj
└── UsblVerificationModule.cs          # IModule implementation
```

---

## Integration with Fathom OS

### Standalone Usage
The module can run standalone with its own NuGet packages.

### Fathom OS Integration
1. Place folder at Fathom OS solution root
2. Update .csproj to reference FathomOS.Core:
```xml
<ItemGroup>
  <ProjectReference Include="..\FathomOS.Core\FathomOS.Core.csproj" />
</ItemGroup>
```
3. Remove individual package references (MahApps, OxyPlot, ClosedXML, QuestPDF)
4. Module will be auto-discovered via ModuleInfo.json

---

## NPD File Format

Expected columns (tab-separated):
- Date, Time (or combined DateTime)
- Vessel Gyro/Heading
- Vessel Easting, Vessel Northing
- Transponder Easting, Transponder Northing
- Transponder Depth

The parser automatically detects column positions based on headers.

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Ctrl+N | New Project |
| Ctrl+O | Open Project |
| Ctrl+S | Save Project |

---

## Version History

### v1.7.0 (Current)
- **🔄 Rebranding to Fathom OS**:
  - **Namespace Change**: All `S7Fathom.*` namespaces updated to `FathomOS.*`
  - **Project References**: Updated to reference `FathomOS.Core`
  - **UI Text**: All "S7 Fathom" references changed to "Fathom OS"
  - **Window Title**: Now shows "USBL Verification - Fathom OS"
  - **Documentation**: Fully updated for Fathom OS branding

- **📜 Certificate System Integration**:
  - **Certificate Code**: "UV" (USBL Verification)
  - **Certificate Title**: "USBL System Verification Certificate"
  - **Certificate Statement**: Professional certification of USBL verification completion
  - **Processing Data Dictionary**: Module provides:
    - Equipment information (USBL model, serial, transponder)
    - Spin test results (4 headings with statistics)
    - Transit test results (reciprocal lines)
    - Overall pass/fail status
    - Tolerance values and compliance
  - **Signatory Integration**: Professional title dropdown for certificate signing
  - **Branding Support**: Ready for company logo and edition name integration

- **📋 ModuleInfo.json Updates**:
  - Added `certificateCode`: "UV"
  - Added `certificateTitle`: USBL System Verification Certificate
  - Added `certificateStatement`: Full certification text
  - Updated features list with new capabilities

### v1.6.1
- **UI/UX Improvements**:
  - **Modern Window Styling**: Updated MetroWindow with modern title bar colors and theming
  - **Progress Tracking**: Visual progress bar showing overall verification completion (0-100%)
  - **Step 7 Complete Button**: Changed from "Next" to "Complete" with verification registration
  - **Project Save/Load in Step 7**: Quick access buttons to save current or load new project
  - **Modern ScrollBar Style**: Replaced default Windows scrollbars with sleek modern design
  - **DataGrid Dark Mode Fix**: Fixed white background issue - now properly themed

- **🆕 Outlier/Spike Filtering**:
  - **Statistical Detection**: Detect outliers based on sigma threshold (1.5σ - 4.0σ)
  - **Interactive Slider**: Adjust threshold and see outliers detected in real-time
  - **Outlier List Panel**: Shows all detected outliers with radial distance from mean
  - **Individual Exclude**: Click to exclude single outliers with supervision
  - **Exclude All Button**: Remove all detected outliers at once
  - **Restore Function**: Bring back all excluded points if needed
  - **Highlight in Charts**: Outliers shown as orange triangles in scatter plot
  - **Works with Smoothing**: Outlier detection independent of smoothing option

- **Smoothing Improvements**:
  - **Affects Spin Visualization**: Main scatter plot now shows smoothed data when enabled
  - **Smoothing Comparison Chart**: Fixed to show before/after comparison properly
  - **Removed Small Preview**: Cleaned up Step 3 layout by removing redundant mini preview

- **Chart Fixes**:
  - **Radar Chart**: Fixed to display data correctly using proper property names
  - **Rose Diagram**: User can now interact with the polar view
  - **Smoothing Effect Tab**: Shows original vs smoothed with movement vectors

- **Bug Fixes**:
  - Fixed footer navigation button styling (modern look instead of Windows XP style)
  - Fixed NullToVisibility converter to handle integer values (for outlier count)
  - Fixed BoolToIcon converter to support custom icon parameters (Check|ChevronRight format)
  - Added RadialFromMean property to UsblObservation model for outlier display
  - Added IsOutlier property to UsblObservation for outlier tracking
  - Fixed outlier series display in UpdateSpinChart method
  - Fixed OverallProgress property binding for progress bar
  - Added IsVerificationComplete tracking property
  - Added CompleteVerification() method for Step 7 finalization
  - Fixed PercentToWidth converter MaxWidth from 150 to 300 for progress bar

### v1.6.0
- **3D Scatter Plot Visualization**:
  - Interactive WPF 3D viewport with mouse rotation and zoom
  - Points colored by heading (Red/Green/Blue/Magenta for 0°/90°/180°/270°)
  - Semi-transparent tolerance sphere around mean position
  - Optional CEP and R95 statistical spheres
  - 3D coordinate axes (X=Easting, Y=Northing, Z=Depth)
  - Optional grid plane and vessel track visualization
  - Legend and info overlay panels
  - Point size adjustment slider
  - Display toggles: Axes, Grid, Tolerance, Statistics, Transit data
  
- **DXF Plan View Export**:
  - Professional CAD-ready plan view with all elements
  - Multiple tolerance rings (0.5x, 1x, 1.5x, 2x tolerance)
  - Statistical circles: CEP, R95, 2DRMS with labels
  - 95% Confidence ellipse
  - Different symbols for each heading direction
  - Per-heading mean position markers
  - Coordinate grid with labels
  - Transit lines connecting sequential points
  - Legend with all symbology explained
  - Scale bar and north arrow
  - Title block with project info and pass/fail result
  - Separate layers for easy CAD manipulation

- **🆕 Advanced Analytics Charts** (Step 6 - Quality Dashboard):
  - **Error Ellipse Chart** - Position confidence ellipses at 50%, 90%, 95%, 99% levels
    - Shows major/minor axes with lengths
    - Displays scatter points with mean center
    - Calculates ellipse rotation angle from covariance
  - **Heading Comparison Radar** - Polar radar comparing performance across headings
    - Std Dev (Radial) metric per heading
    - Max Offset per heading
    - Visual symmetry check for directional bias
  - **Residual Time Series** - Quality control chart over time
    - Mean residual line
    - ±1σ and ±2σ control limits
    - Outlier detection (points > 2σ marked as triangles)
  - **Statistical Control Chart (X-bar)** - SPC process monitoring
    - Subgroup means with configurable subgroup size
    - Center Line (CL), Upper/Lower Control Limits (UCL/LCL)
    - Out-of-control point highlighting
  - **Depth vs Slant Range** - Acoustic propagation analysis
    - Scatter plot of depth vs calculated slant range
    - Theoretical vertical line comparison
    - Linear regression with R² value
  - **Rose Diagram** - Directional error distribution
    - Polar histogram of error bearings (36 bins)
    - Petal color intensity by error magnitude
    - Cardinal direction labels
  - **Before/After Smoothing Overlay** - Smoothing effect visualization
    - Original points (red circles) vs Smoothed (green diamonds)
    - Movement vectors showing point displacement
    - Scatter reduction percentage calculation

- **Bug Fixes**:
  - Fixed duplicate TransitPointCount property causing compilation error
  - Fixed duplicate HeadingResult class definition (CS0104 ambiguity error)
  - Added missing TransponderModel and TransponderFrequency properties
  - Fixed 3D camera binding conflict (now uses code-behind control)
  - Fixed 3D ModelVisual3D scene update mechanism
  - Fixed mah:Expander to standard WPF Expander (MahApps doesn't have custom Expander)
  - Fixed Viewport3D Background property error (Viewport3D doesn't support Background)
  - Fixed OxyPlot Legend API - use Legends collection instead of deprecated properties
  - Fixed double to int conversions for NominalHeading in DxfExportService and Visualization3DService
  - Added missing UsblModel and ProjectFilePath properties to UsblVerificationProject
  - Added missing SpinTestPassed, TransitTestPassed, MeanPosition, CertificateNumber, SpinStatistics to VerificationResults
  - Added QualityScore alias to QualityMetrics
  - Fixed Spin*Observations references to use Project.Spin*.Observations
  - Fixed HeadingResult.Heading double to int conversion with Math.Round
  - Fixed TwoWay binding on read-only properties (TransitFileCount, SpinFileCount, etc.) - added Mode=OneWay
  - Fixed QuestPDF container errors in CertificateService and PdfExportService (multiple children to single-child container)
  - Enhanced real-time smoothing visualization with ShowSmoothedDifference option
  - Removed incorrectly created folder artifact
  - All version references synchronized to 1.6.0
  - **Advanced Charts Bug Fixes**:
    - Fixed TextAnnotation position using calculated max extent instead of axis property
    - Fixed Rose Diagram to use LineSeries instead of AreaSeries for polar plot compatibility
    - Fixed division by zero in Rose Diagram color intensity calculation
    - Fixed division by zero in Depth vs Slant Range regression calculation
    - Fixed division by zero in histogram normal curve overlay
    - Fixed division by zero and empty collection issues in smoothing comparison chart
    - Fixed radar chart to use correct property names (StdDevEasting2Sigma instead of StdDevEasting)
    - Added MaxRadialOffset calculation from observations in radar chart
    - Added guard for empty validObs in CreateTrendChart
    - Fixed polar chart DataPoints to use degrees instead of radians for axis consistency

### v1.5.1
- **Real-Time Data Smoothing**:
  - Live Slider Control: Adjust smoothing strength 0-100% with instant chart update
  - Enable/Disable Toggle: Quick on/off without losing settings
  - Real-Time Statistics: RMS residual and points affected shown live
  - Window Size Slider: Adjust algorithm window with immediate feedback
  - One-click reset to original data

### v1.5.0
- **Advanced Statistics & Analytics**:
  - Radial Statistics: CEP, R95, R99, DRMS, 2DRMS, 2σ radius
  - Confidence Ellipse: 95% confidence ellipse calculation and visualization
  - Monte Carlo Simulation: Bootstrap confidence bounds for position estimates
  - Trend Analysis: Linear regression for position drift detection
  
- **New Chart Types**:
  - Time Series Chart: Position drift over time, gyro stability, depth variation
  - Histogram: Error distribution with normal curve overlay
  - Polar Plot: Position offsets in polar coordinates with tolerance circle
  - Box Plot: Outlier visualization for multiple datasets
  - Trend Chart: Position trend with regression lines
  
- **Real-Time Data Smoothing** (v1.5.1):
  - **Live Slider Control**: Adjust smoothing strength from 0-100% with instant chart update
  - **Enable/Disable Toggle**: Quick on/off without losing settings
  - **Real-Time Statistics**: RMS residual and points affected shown live
  - **Window Size Slider**: Adjust algorithm window size with immediate feedback
  - Moving Average, Gaussian, Savitzky-Golay, Median, EMA filters
  - One-click reset to original data
  
- **Historical Database**:
  - Save verification results to local database
  - Search by project, vessel, client, date range, pass/fail
  - Export/import database backup
  - Quick access to recent records
  
- **Digital Signatures**:
  - RSA-SHA256 cryptographic certificate signing
  - Key pair generation with optional password protection
  - Signature verification for authenticity
  - PDF signature metadata files
  
- **Recent Projects & Drag-Drop**:
  - Recent projects list with quick access
  - Pin favorite projects
  - Drag & drop NPD files directly onto window
  - Auto-detect heading from filenames
  - Remember last used directories
  
- **Step 7 Redesign**:
  - **Tab-based layout**: Export Options, Advanced Analytics, History
  - **Analytics Tab**: CEP/R95/2DRMS cards, Monte Carlo results, Drift analysis
  - **History Tab**: DataGrid with pass/fail coloring, export/import database
  - **4-chart grid**: Time series, histogram, polar plot, statistics panel
  
- **New Services Added**:
  - AdvancedStatisticsService
  - AdvancedChartService
  - SmoothingService
  - HistoricalDatabaseService
  - DigitalSignatureService
  - RecentProjectsService

### v1.4.0
- **Streamlined Data Import UI**:
  - Removed individual file loading buttons - batch import is now the primary method
  - Cleaner, more modern card-based layout for file management
  - Real-time statistics summary (files, headings, points, status)
  - Heading indicators showing which headings are loaded
  
- **Dynamic Heading Detection Improvements**:
  - ActualHeading now shown in file list items
  - Charts, tables, and all UI elements show actual headings from data
  - QualityMetrics table (Step 6) shows actual headings
  - Live preview chart uses actual headings in legend
  
- **Processing Status Fix**:
  - New ProcessingStatus property tracks: NotProcessed, Processing, Pass, Fail
  - Step 3 no longer shows "Failed" before processing
  - Clear "Click to process" message before processing
  
- **Data Preview Auto-Load**:
  - Preview data now loads automatically when entering Step 2
  - No longer need to switch tabs to see data
  - Full property notifications for immediate UI refresh
  
- **Bug Fixes**:
  - Fixed QualityMetrics not refreshing after processing
  - Fixed preview data not showing immediately on Step 2
  - Added UpdateQualityMetrics call to UpdateResultProperties

### v1.3.3
- **Dynamic Heading Detection**:
  - Headings now calculated from average gyro readings in each NPD file
  - Supports field scenarios where headings are not exactly 0°, 90°, 180°, 270°
  - Uses circular mean calculation to handle 359° to 1° wraparound correctly
  - Chart legends, results, and status messages all show actual headings
  
- **Transit Test Optional**:
  - If transit test is skipped, only spin test determines overall pass/fail
  - Overall verification passes if spin test passes (when transit skipped)
  - Added TransitWasPerformed flag to properly track test status
  
- **Enhanced Data Preview (Step 2)**:
  - New statistics panel showing file counts and data summaries
  - Per-heading breakdown with actual heading values
  - Data quality indicators (coverage %, heading spread status)
  - Modern premium UI with gradient accents
  
- **Improved UI/UX**:
  - Premium data grid styling with better contrast
  - Status badges and progress indicators
  - Heading spread validation (checks if ~90° apart)

### v1.3.2
- **Navigation Fix**:
  - Fixed step navigation not switching views after clicking Next
  - Added UpdateStepIndicators() method to update step progress display
  - CurrentStep setter now notifies all IsStepX properties
  
- **Auto-Detect Improvements**:
  - Fixed Vessel Northing incorrectly picking "Height" columns
  - Added exclusion patterns for height/depth/z in vessel position detection
  - Improved column matching logic with separate exclude patterns
  
- **Premium UI Redesign**:
  - New premium dark theme with cyan/purple gradient accents
  - Glowing buttons with hover effects
  - Glass card effects and better shadows
  - Updated color palette with better contrast
  - Improved light theme with modern look
  - Step indicators with gradient backgrounds

### v1.3.1
- **Delimiter Auto-Detection Fix**:
  - NPD files now properly auto-detect delimiter (comma, tab, semicolon)
  - Fixed issue where all data was treated as single column
  - Default delimiter changed from Tab to Comma (most common for NPD files)
  - Delimiter ComboBox now uses index-based selection for reliability
  - Auto-detection counts delimiter occurrences in header line
  
- **Bug Fixes**:
  - Fixed `XamlParseException` on startup (StaticResource → DynamicResource)
  - Fixed delimiter selection not working in BatchColumnMappingDialog

### v1.3.0
- **Enhanced Column Mapping UI**:
  - New `ColumnMappingDialog` with enhanced auto-detection algorithm
  - New `BatchColumnMappingDialog` for multi-file import with per-file mappings
  - NaviPac format auto-detection (date/time column split)
  - Real-time validation with visual feedback
  - "Apply to All" for consistent mapping across files
  - Preview panel shows mapped column values
  
- **Theme Toggle in Title Bar**:
  - Dark/Light theme toggle button in window title bar
  - Icons update to show current theme (Moon/Sun)
  - Theme applies to all dialogs
  
- **Step 1 Redesign**:
  - Split-panel layout: Project info left, file lists right
  - "Load Files" and "Batch Import" buttons per data type
  - File lists show filename, record count, heading label
  - Remove button per file for easy management
  - Empty state messages guide users
  
- **Step 2 Enhanced Preview**:
  - Full-width DataGrid (no longer "too small on side")
  - Spin/Transit radio toggle to switch data views
  - Refresh button to reload preview
  - Shows record count for current view
  
- **DXF Export**:
  - New DxfExportService for CAD export
  - Tolerance circle, mean position marker
  - Color-coded layers for each heading
  - Vessel position markers
  - Text labels with coordinates
  
- **Processing Improvements**:
  - Separate spin/transit analysis commands
  - SpinHeadingResults collection for per-heading stats
  - UpdateResultProperties for UI bindings
  - QualityPlotModel for dashboard charts

### v1.2.7
- **Improvement** (Following NPD Parsing Guide):
  - Updated `UsblNpdParser` to use Core `NpdParser` for robust NPD file parsing
  - Properly handles NaviPac date/time offset trick (`HasDateTimeSplit = true`)
  - Uses Core's pattern-based column auto-detection
  - Maintains consistency with other calibration modules (MruCalibration, GnssCalibration)
  - Reduces duplicated parsing logic

### v1.2.6
- **Bug Fix**:
  - Replaced `System.Windows.Forms.FolderBrowserDialog` with WPF-compatible `OpenFileDialog` approach
  - Removes dependency on Windows Forms (pure WPF module now)
  - BatchImportSpin and BatchImportTransit now use "select any file in folder" pattern

### v1.2.5
- **Integration Fix**:
  - Added INTEGRATION.md with step-by-step instructions for adding module to solution
  - Grouped modules require manual addition to solution (not auto-discovered)
  - Correct ProjectReference path for ModuleGroup location: `..\..\FathomOS.Core\FathomOS.Core.csproj`

### v1.2.4
- **Critical Fix**:
  - Fixed .csproj to properly reference FathomOS.Core instead of standalone packages
  - Corrected relative path for ModuleGroup location: `..\..\FathomOS.Core\FathomOS.Core.csproj`
  - Removed UseWindowsForms (not needed, was causing conflicts)
  - IModule interface now properly resolved from FathomOS.Core.Interfaces

### v1.2.3
- **Critical Fix**:
  - Removed duplicate IModule interface that conflicted with FathomOS.Core
  - Module now correctly uses IModule from FathomOS.Core.Interfaces

### v1.2.2
- **Bug Fixes**:
  - Fixed PdfExportService.Export call (removed extra arguments)
  - Fixed SaveProject parameter order (project, filePath)

### v1.2.1
- **Bug Fixes**:
  - Fixed Color/Colors/Brushes ambiguity between System.Drawing and System.Windows.Media
  - Fixed Size ambiguity in PDF export (QuestPDF.Infrastructure.Size)
  - Fixed MessageBox ambiguity between System.Windows.Forms and System.Windows
  - Fixed OpenFileDialog/SaveFileDialog ambiguity (Microsoft.Win32)
  - Fixed FontWeights ambiguity (OxyPlot.FontWeights)
  - Fixed CalculateVerification method name (now CalculateResults)
  - Removed duplicate RelayCommand definition

### v1.2.0
- **Batch Import** - Load all 4 spin files via folder selection
- **Live Chart Preview** - Scatter plot updates as data loads
- **Auto-detect Units** - Guesses units based on coordinate magnitude
- **Quality Dashboard** - Visual gauges for data quality metrics
- **Point Selection** - Click chart points to view details/exclude
- **Report Templates** - Custom company branding for PDF exports
- **Verification Certificate** - Auto-generated PDF certificate on pass
- **Template Editor** - Edit company info, colors, logo, signature
- Certificate number format with auto-increment
- Enhanced file pattern matching for batch import

### v1.1.0
- **7-step guided workflow** with visual step indicators
- **Unit conversion** (Meters, Int'l Feet, US Survey Feet)
- **Data preview** with exclude/include functionality
- **IQR outlier detection** with auto-exclude option
- **Help guide** flyout
- **6 chart themes** with legend
- **Quality scoring**
- Project save/load with auto-backup

### v1.0.0
- Initial release
- Basic spin/transit verification
- Excel/PDF export

---

## Roadmap / Future Features

### 🚀 Recommended for v1.5.0 - Enhanced Analytics

#### Additional Statistics Views
- **Time Series Analysis**
  - Position drift over time chart
  - Gyro stability chart during spin test
  - Depth variation timeline
  
- **Radial Statistics Table**
  - Per-heading: Radial mean, max, min, 2σ radius
  - 95% confidence ellipse dimensions
  - CEP (Circular Error Probable)
  
- **Comparison View**
  - Side-by-side heading comparison
  - Differential analysis between headings
  - Highlight worst-performing heading

#### New Charts
- **Polar Plot** - Show position offsets in polar coordinates relative to heading
- **3D Scatter Plot** - Visualize X/Y/Depth distribution using HelixToolkit
- **Histogram** - Position error distribution
- **Box Plot** - Outlier visualization per heading
- **Time Series Line Chart** - Position over time with trend line

### 🔧 Recommended for v1.6.0 - Advanced Features

#### Statistical Analysis
- **Monte Carlo Simulation** - Confidence bounds calculation
- **Helmert Transformation** - 7-parameter adjustment if reference position known
- **Datum Shift Detection** - Identify systematic offsets between headings
- **Trend Analysis** - Detect if position is drifting during test

#### Calibration Features
- **Lever Arm Calculation** - Estimate USBL mounting offsets from spin test
- **Roll/Pitch Sensitivity Analysis** - Calculate impact of MRU errors
- **Auto-Calibration Suggestions** - Recommend offset adjustments

#### Data Processing
- **Smoothing Options** - Apply moving average, Gaussian, or Savitzky-Golay filter
- **Decimation** - Reduce point count while preserving statistics
- **Interpolation** - Fill gaps in transit data
- **Time Synchronization Check** - Verify all systems are time-aligned

### 📊 Recommended for v1.7.0 - Reporting Enhancements

#### Report Templates
- **Client-Specific Templates** - Save custom branding per client
- **Multi-Language Support** - Generate reports in different languages
- **Comparison Reports** - Compare current test vs historical baseline

#### New Export Formats
- **DXF Plan View** - Export position scatter with tolerance circles
- **KML/KMZ** - Google Earth visualization
- **CSV Time Series** - Export for external analysis
- **Word Document** - .docx report with embedded charts

#### Quality Certification
- **Digital Signatures** - Cryptographically sign certificates
- **QR Code** - Link to verification record
- **Audit Trail** - Track all processing steps

### 🎨 Recommended for v1.8.0 - UI/UX Improvements

#### User Experience
- **Drag & Drop File Loading** - Drop NPD files directly onto application
- **Recent Projects** - Quick access to previous verifications
- **Project Templates** - Save common configurations
- **Undo/Redo** - Revert exclusion decisions

#### Visualization
- **Animation Mode** - Animate through time showing position changes
- **Heatmap Overlay** - Show point density on scatter plot
- **Custom Axis Scaling** - User-defined zoom levels
- **Annotation Tools** - Add notes/markers to charts

#### Accessibility
- **High Contrast Mode** - For outdoor laptop use
- **Keyboard Navigation** - Full keyboard support
- **Screen Reader Support** - Accessibility compliance

### 📡 Recommended for v2.0.0 - Enterprise Features

#### Multi-System Support
- **Multiple USBL Systems** - Compare System A vs System B
- **Reference Position Input** - Known position for absolute accuracy check
- **INS Integration** - Import INS data for comparison

#### Database Integration
- **Historical Database** - Store all verifications
- **Trend Reporting** - System performance over time
- **Fleet Management** - Track verification status across vessels

#### Automation
- **Scheduled Verifications** - Auto-run tests at defined intervals
- **Real-Time Mode** - Connect directly to USBL system for live testing
- **API Integration** - Trigger from external systems

---

## Requirements

- .NET 8.0 (Windows)
- Windows 10/11

---

## Dependencies (Standalone)

- MahApps.Metro 2.4.10
- MahApps.Metro.IconPacks.Material 5.0.0
- OxyPlot.Wpf 2.1.2
- ClosedXML 0.102.1
- QuestPDF 2024.3.0
- SkiaSharp 2.88.7

---

## Author

S7 Solutions

---

## License

Proprietary - S7 Solutions
