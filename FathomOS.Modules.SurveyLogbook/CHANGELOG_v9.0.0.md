# Survey Electronic Logbook - v9.0.0 Changelog

## Release Date: January 2, 2026

## Overview
Major feature release introducing real-time NaviPac data monitoring, dynamic field configuration system, dynamic survey log columns, and enhanced UDP/TCP connectivity for professional survey operations.

---

## New Features

### 🔍 Phase 1: Real-Time Data Monitor Window
A new dedicated window for monitoring NaviPac data in real-time:

- **Live Message Display**: View incoming NaviPac UDO messages as they arrive
- **Connection Status**: Visual indicator (Green/Orange/Red/Gray) showing connection state
- **Message Statistics**: 
  - Total message count
  - Total bytes received
  - Messages per second rate
  - Last data timestamp
- **Field Parsing**: 
  - Automatic separator detection (comma, semicolon, colon, space, tab)
  - Type detection (Integer, Decimal, DateTime, Coordinates, String)
  - Field name guessing based on value patterns
- **Controls**:
  - Pause/Resume data capture
  - Clear message log
  - Export log to file
  - Copy message to clipboard
  - Auto-scroll toggle
  - Test button for offline debugging

### ⚙️ Phase 2: Dynamic Field Configuration System
Complete flexibility in configuring NaviPac UDO field mappings:

- **Visual Field Editor**:
  - Add, remove, reorder fields
  - Duplicate existing fields
  - Drag-and-drop positioning
- **Field Properties**:
  - Custom field names
  - Data type selection (25+ types including coordinates, KP, DCC, motion, etc.)
  - Decimal places configuration
  - Unit specification
  - Scale factor and offset for unit conversions
  - "Show in Log" toggle per field
- **Template System**:
  - Save custom configurations as templates
  - Load built-in templates (Standard NaviPac, Minimal Position)
  - Import from NaviPac .out2 XML files
  - Export/Import to JSON format
- **Separator Selection**: 
  - User-friendly dropdown with display names
  - Maps to actual separator characters

### 📊 Phase 3: Dynamic Survey Log Columns
Survey log DataGrid now dynamically generates columns based on field configuration:

- **Automatic Column Generation**: Columns generated from UserFieldDefinition list
- **Column Types**: Core columns (Time, Date, Type, Description, Source) plus dynamic fields
- **Real-time Updates**: FieldConfigurationChanged event triggers column regeneration
- **Export Integration**: Dynamic columns exported to Excel/CSV with proper formatting
- **Property Mapping**: Comprehensive mapping between field types and SurveyLogEntry properties

### 🌐 Phase 4: Enhanced UDP/TCP Connectivity
Advanced network communication options for NaviPac integration:

- **UDP Enhancements**:
  - Network interface binding (select specific NIC)
  - Source IP filtering (accept data only from specific IPs)
  - Multicast group support
  - Configurable receive buffer size
  - Reuse address option
- **NaviPacDataParser**: Full-featured parser supporting all 25+ data types
- **Extended Navigation Properties**:
  - Latitude, Longitude (WGS84 decimal degrees)
  - Height/Depth separation
  - Roll, Pitch, Heave motion data
  - DOL (Distance Off Line), DAL (Distance Along Line)
  - SMG (Speed Made Good), CMG (Course Made Good)
  - Position Age, Event Number

---

## Files Added

### Views
- `Views/DataMonitorWindow.xaml` - Real-time data monitor UI
- `Views/DataMonitorWindow.xaml.cs` - Data monitor code-behind
- `Views/FieldConfigurationWindow.xaml` - Field configuration UI
- `Views/FieldConfigurationWindow.xaml.cs` - Field configuration code-behind

### ViewModels
- `ViewModels/DataMonitorViewModel.cs` - Data monitor business logic
- `ViewModels/FieldConfigurationViewModel.cs` - Field configuration logic

### Models
- `Models/UserFieldDefinition.cs` - Field definition model with FieldTemplate class

### Services
- `Services/NaviPacDataParser.cs` - NaviPac UDO data parsing service
- `Services/DynamicColumnService.cs` - Dynamic DataGrid column generation

---

## Files Modified

### Views
- `Views/MainWindow.xaml` - Added toolbar buttons for Data Monitor and Field Configuration
- `Views/SurveyLogView.xaml.cs` - Dynamic column regeneration on configuration change

### ViewModels
- `ViewModels/MainViewModel.cs` - Added commands, field configuration propagation
- `ViewModels/SurveyLogViewModel.cs` - FieldConfiguration property with change event

### Models
- `Models/ApplicationSettings.cs` - NaviPacFields property, UDP enhancement settings
- `Models/SurveyLogEntry.cs` - Extended navigation properties (15+ new fields)
- `Models/LogEntryType.cs` - Added PositionUpdate entry type

### Services
- `Services/LogEntryService.cs` - NaviPacDataParser integration, field configuration support
- `Services/NaviPacClient.cs` - UDP enhancement options

---

## Technical Details

### Data Monitor Performance
- Maximum 500 messages retained in memory
- 5-second rolling window for rate calculation
- Non-blocking UI updates via dispatcher

### Field Configuration Storage
- Templates saved to `%AppData%\FathomOS\SurveyLogbook\Templates\`
- Field definitions stored in ApplicationSettings JSON
- NaviPac .out2 XML import support

### Supported Field Data Types (25 types)
```
Auto, String, Integer, Decimal, DateTime,
Latitude, Longitude, Easting, Northing, HeightDepth, Depth,
HeadingBearing, Course, KP, DCC, DOL, DAL,
Roll, Pitch, Heave, Speed, Age, EventNumber
```

### Extended SurveyLogEntry Properties
```csharp
// Geographic
Latitude, Longitude, Height, Depth, Easting, Northing

// Navigation
Kp, Dcc, DOL, DAL, Heading

// Motion
Roll, Pitch, Heave

// Speed/Course
SMG (Speed Made Good), CMG (Course Made Good)

// Status
Age (Position Age), EventNumber
```

### DynamicColumnService Features
- Automatic binding path resolution
- String format based on data type
- Core + dynamic column separation
- Export value retrieval methods

---

## Upgrade Notes

- Compatible with existing v8.x configuration files
- Field configuration is optional - default auto-detect mode remains available
- No database migration required
- Recommend backing up settings before upgrade
- New UDP options default to disabled (backward compatible)

---

## Dependencies
- No new package dependencies
- Utilizes existing MahApps.Metro controls

---

## Known Issues
- None reported

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 9.0.0 | 2026-01-02 | Phase 1-4: Data Monitor, Field Config, Dynamic Columns, UDP Enhancements |
| 8.0.0 | 2026-01-02 | UI modernization, export improvements |
| 7.7.1 | 2026-01-02 | Bug fixes |
| 7.7.0 | 2026-01-02 | Settings window enhancements |
| 7.6.1 | 2026-01-02 | TabControl fixes |
