# Changelog - S7 Fathom Sound Velocity Profile Module

## Version 1.2.0 (December 2024)

### V2.1 Integration Guide Compliance

Updated module to comply with S7 Fathom Module Integration Guide Version 2.1:

- **Package Management**: Removed all separate package references (MahApps, OxyPlot, etc.)
  - All packages now come from S7Fathom.Core reference
  - Module only references Core project
- **Theme System**: Uses standard resource keys from V2.1 guide
- **IModule Implementation**: Verified all 7 properties and 5 methods
- **File Extensions**: Updated CanHandleFile to support all file types in ModuleInfo.json

### Additional Fixes

- **EnumDescriptionConverter**: Added "Auto" option description for SV formula dropdown
- **File Structure**: Verified all required files present per V2.1 checklist

### Bug Fixes (from v1.1.0)

#### 1. Fixed Valeport MIDAS SVX2 File Parsing
- **Issue**: Data was not being assigned correctly when parsing Valeport files
- **Fix**: Completely rewrote `FileParserService.cs` to correctly handle the Valeport format:
  - Lines 1-27: Metadata header with "Key :\tValue" format
  - Line 28: Column headers with units (e.g., "SOUND VELOCITY;M/SEC")
  - Lines 29+: Tab-separated data with DateTime in first column
- Automatically detects and extracts:
  - Model Name (e.g., "MIDAS SVX2 3000")
  - Serial Number
  - Site Information
  - Timestamp

#### 2. Fixed Column Auto-Detection for Calculated Sound Velocity
- **Issue**: Parser was selecting measured SV column (often 0.000 at surface) instead of calculated SV
- **Fix**: Parser now prefers "Calc. Sound Velocity" column over measured when both are present
- Correctly maps all 8 columns from Valeport files:
  - Date/Time
  - Measured Sound Velocity
  - Pressure/Depth
  - Temperature
  - Conductivity
  - Calculated Salinity
  - Calculated Density
  - Calculated Sound Velocity

#### 3. Implemented Depth-Based SV Formula Selection (Auto Mode)
- **Issue**: No automatic selection between Chen & Millero vs Del Grosso formulas
- **Fix**: Added "Auto" option to SV formula selection that:
  - Uses Chen & Millero for depth ≤ 1000m
  - Uses Del Grosso for depth > 1000m
  - Applies per-point selection, so mixed-depth profiles are handled correctly
- Based on VBA logic from Formula.bas

#### 4. Depth/Pressure Input Unit Handling
- **Issue**: No way to specify if input data is depth (m) or pressure (dBar)
- **Fix**: Added `SelectedInputType` property with options:
  - Depth (meters) - applies DepthToPressure conversion using latitude-corrected gravity
  - Pressure (dBar) - uses values directly
- UI ComboBox added in Step 3 (Processing Settings)

#### 5. Removed 100-Row Display Limit
- **Issue**: Data preview showed only first 100 rows
- **Fix**: `UpdatePreviewData()` now loads all rows
- DataGrid uses virtualization for performance with large datasets

#### 6. Added Equipment Metadata Extraction
- **Issue**: Equipment info from file header not displayed
- **Fix**: Extracts "Model Name" and "Serial No." from Valeport files
- Auto-fills Equipment field in Project Info

### Technical Improvements

- Added `Timestamp` property to `CtdDataPoint` model
- Added `Clone()` method to `ColumnMapping` class
- Added `Auto` option to `SoundVelocityFormula` enum
- Removed duplicate class definitions between files
- Updated ModuleInfo.json with new features

### File Changes

| File | Changes |
|------|---------|
| `S7Fathom.Modules.SoundVelocity.csproj` | Removed package refs, Core only |
| `SoundVelocityModule.cs` | Updated version, extended file types |
| `Services/FileParserService.cs` | Complete rewrite for Valeport parsing |
| `Services/DataProcessingService.cs` | Added Auto SV formula selection |
| `Models/DataModels.cs` | Added Timestamp, Clone(), removed duplicates |
| `Models/Enums.cs` | Added SoundVelocityFormula.Auto |
| `ViewModels/MainViewModel.cs` | Removed 100-row limit, added Equipment extraction |
| `ModuleInfo.json` | Updated version and features |

### Testing

Tested with sample Valeport MIDAS SVX2 file:
- File: `sample.000`
- Rows: 4,988 data points
- Depth range: 0.003m to 1744.584m
- Successfully parses all columns
- Correctly identifies calculated SV column (column 7)
- Extracts metadata: "MIDAS SVX2 3000", "81015", "BOE SHENANDOAH"

---

## Version 1.1.0 (December 2024)

- Bug fixes for Valeport parsing
- Auto SV formula selection
- Depth/pressure unit handling
- Removed display row limit

---

## Version 1.0.0 (December 2024)

- Initial release
- Basic file parsing for multiple formats
- Chen-Millero and Del Grosso SV calculations
- UNESCO EOS-80 density calculation
- Export to USR, VEL, PRO, Excel formats
- Real-time data smoothing with 4 algorithms
