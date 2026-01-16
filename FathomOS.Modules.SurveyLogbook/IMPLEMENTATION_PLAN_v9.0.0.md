# Survey Electronic Logbook - v9.0.0 Implementation Plan

## Overview
Major enhancement release focusing on professional NaviPac integration with real-time data monitoring and dynamic field configuration.

---

## Phase 1: Real-Time Data Monitor Window ✅ COMPLETED
**Status:** Implementation complete - Code review passed
**Estimated Effort:** 2 days
**Priority:** HIGH

### Goals:
- [x] Create DataMonitorWindow.xaml - Real-time data viewer
- [x] Create DataMonitorViewModel.cs - ViewModel for data monitor
- [x] Wire up RawDataReceived event from NaviPacClient
- [x] Add "Data Monitor" button to MainWindow toolbar
- [x] Display connection status, message count, rate
- [x] Show raw data messages (last 500)
- [x] Show parsed field breakdown with type detection
- [x] Add Clear, Pause, Export, Copy functions
- [x] Add NaviPacClient property to LogEntryService

### Files Created:
- Views/DataMonitorWindow.xaml ✅
- Views/DataMonitorWindow.xaml.cs ✅
- ViewModels/DataMonitorViewModel.cs ✅

### Files Modified:
- Views/MainWindow.xaml (added toolbar button) ✅
- ViewModels/MainViewModel.cs (added command to open monitor) ✅
- Services/LogEntryService.cs (added NaviPacClient property) ✅

### Features Implemented:
- Real-time message display with timestamps
- Auto-scroll option
- Pause/Resume functionality
- Message rate calculation (msg/sec)
- Total bytes tracking
- Connection status indicator (Green/Orange/Red/Gray)
- Field parsing with separator selection (comma, space, semicolon, colon, tab)
- Auto field type detection (Integer, Decimal, DateTime, Coordinates, etc.)
- Field name guessing based on value patterns
- Export log to file
- Copy message to clipboard
- Test button for debugging without NaviPac connection

---

## Phase 2: Dynamic Field Configuration ✅ COMPLETED
**Status:** Implementation complete
**Estimated Effort:** 3 days
**Priority:** HIGH

### Goals:
- [x] Create FieldConfigurationWindow.xaml
- [x] Create FieldConfigurationViewModel.cs
- [x] Create UserFieldDefinition model class
- [x] Replace hardcoded checkboxes with DataGrid
- [x] Support custom field names
- [x] Position index mapping to UDO order
- [x] "Show in Log" toggle per field
- [x] Add/Remove/Reorder fields
- [x] Save/Load field templates (.json)
- [x] Import from NaviPac .out2 file

### Files Created:
- Views/FieldConfigurationWindow.xaml ✅
- Views/FieldConfigurationWindow.xaml.cs ✅
- ViewModels/FieldConfigurationViewModel.cs ✅
- Models/UserFieldDefinition.cs ✅ (includes FieldTemplate class)

### Files Modified:
- Views/MainWindow.xaml (added Field Configuration toolbar button) ✅
- ViewModels/MainViewModel.cs (added OpenFieldConfigurationCommand) ✅
- Models/ApplicationSettings.cs (added NaviPacFields property) ✅

### Features Implemented:
- Dynamic field list with position ordering
- Field property editor (name, type, decimals, unit, scale, offset)
- Show/Hide in Survey Log toggle per field
- Field reordering (move up/down)
- Duplicate and delete fields
- Template system with built-in defaults
- Save/load custom templates to AppData
- Import from NaviPac .out2 XML files
- Import/Export to JSON
- Reset to default configuration
- Display-friendly separator selection
- Changes indicator with save/cancel workflow

---

## Phase 3: Survey Log Dynamic Columns ✅ COMPLETED
**Status:** Implementation complete
**Estimated Effort:** 2 days
**Priority:** HIGH

### Goals:
- [x] Modify SurveyLogView to generate columns dynamically
- [x] Columns based on "Show in Log" from field config
- [x] Update SurveyLogEntry model for dynamic data
- [x] Update Excel/PDF export for dynamic columns
- [x] Maintain backward compatibility

### Files Created:
- Services/DynamicColumnService.cs ✅

### Files Modified:
- Views/SurveyLogView.xaml.cs (RegenerateColumns method) ✅
- ViewModels/SurveyLogViewModel.cs (FieldConfiguration property, GetColumnDefinitions) ✅
- ViewModels/MainViewModel.cs (ApplySettingsToViewModels propagation) ✅
- Models/SurveyLogEntry.cs (extended navigation properties) ✅

### Features Implemented:
- DynamicColumnService with core + dynamic column separation
- Automatic binding path resolution for field types
- String format based on data type and decimal places
- FieldConfigurationChanged event for column regeneration
- GetColumnValue/GetFormattedColumnValue for exports
- GetColumnDefinitions public method for view access

---

## Phase 4: UDP/TCP Enhancements & All Field Types ✅ COMPLETED
**Status:** Implementation complete
**Estimated Effort:** 2.5 days
**Priority:** MEDIUM

### Goals:
- [x] UDP: Option to bind to specific interface
- [x] UDP: Source IP filtering option
- [x] UDP: Multicast group support
- [x] Support all NaviPac UDO field types:
  - [x] DOL, DAL
  - [x] Course (CMG), Speed (SMG)
  - [x] Roll, Pitch, Heave
  - [x] Age (Position Age)
  - [x] Height/Depth separation
  - [x] Latitude/Longitude
  - [x] Event Number

### Files Created:
- Services/NaviPacDataParser.cs ✅

### Files Modified:
- Services/NaviPacClient.cs (UDP enhancement options) ✅
- Services/LogEntryService.cs (NaviPacDataParser integration) ✅
- Models/ApplicationSettings.cs (UDP settings) ✅
- Models/UserFieldDefinition.cs (extended FieldDataType enum) ✅
- Models/SurveyLogEntry.cs (15+ new navigation properties) ✅
- Models/LogEntryType.cs (PositionUpdate entry type) ✅

### Features Implemented:
- NaviPacDataParser with full field type support (25 types)
- UDP interface binding option
- UDP source IP filtering
- UDP multicast group support
- Extended SurveyLogEntry with all navigation fields
- LogEntryService creates entries from parsed NaviPac data
- PositionUpdate vs NaviPacEvent entry type distinction

---

## Bug Tracking

### Phase 1 Bugs Found:
- None reported

### Phase 2 Bugs Found:
- None reported

### Phase 3 Bugs Found:
- None reported

### Phase 4 Bugs Found:
- None reported

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 8.0.0 | 2026-01-02 | NaviPac Data Config UI, TimePicker fix |
| 9.0.0 | 2026-01-02 | Data Monitor, Dynamic Fields, Dynamic Columns, UDP Enhancements |

---

## Notes
- All four phases completed
- Extended SurveyLogEntry with 15+ navigation properties
- DynamicColumnService handles core + dynamic column generation
- NaviPacDataParser supports 25 field data types
- FieldConfigurationChanged event ensures UI updates
- Backward compatible with existing configurations
