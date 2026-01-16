# Survey Electronic Logbook - Version 7.7.1

**Release Date:** January 2, 2026  
**Type:** Bug Fix Release

## Bug Fixes

### CS1061 Compilation Errors Fixed
- **LogEntryService.cs** - Fixed property access patterns for event args classes
  - Changed `e.EventData.EventNumber` → `e.EventNumber` 
  - Changed `e.EventData.EventText` → `e.EventText`
  - Changed `e.StatusData.System` → `e.System`
  - Changed `e.StatusData.IsLogging` → `e.IsLogging`
  - Changed `e.StatusData.RunlineName` → `e.RunlineName`

### CS0029 Type Conversion Errors Fixed
- Fixed implicit conversion errors where `string` was being used instead of `int`
- All type mismatches in switch statements resolved

### Event Args Classes Updated
- **LoggingStatusEventArgs** - Added missing properties:
  - `System` (string) - Identifies the logging system (NaviPac, NaviScan)
  - `RunlineName` (string?) - Current runline name when logging starts

## Files Modified

1. **Services/NaviPacClient.cs**
   - Added `System` and `RunlineName` properties to `LoggingStatusEventArgs`

2. **Services/LogEntryService.cs**
   - Fixed `OnNaviPacEvent()` method - direct property access
   - Fixed `OnLoggingStatusChanged()` method - direct property access

## Previous Version
- v7.7.0 - TCP server mode architecture fix (NaviPac connection)

## Upgrade Notes
This is a bug fix release. No configuration changes required.
Simply replace the module files with the new version.
