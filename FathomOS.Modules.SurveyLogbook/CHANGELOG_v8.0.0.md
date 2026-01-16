# Survey Electronic Logbook Module - v8.0.0 Changelog

## Release Date: January 2, 2026

## Summary
Major release with critical bug fixes and comprehensive NaviPac data configuration UI. This release addresses crashes and adds user-configurable field mapping for NaviPac User Defined Output integration.

---

## ✅ Bug Fixes

### 1. **CRITICAL: TimePicker Crash Fixed**
- **Issue:** Application crashed when clicking "Add Manual Entry" button
- **Root Cause:** Invalid `PickerVisibility` enum value `"HourMinuteSecond"` in ManualEntryDialog.xaml
- **Solution:** Changed to valid value `"HourMinute"` (MahApps.Metro supported value)
- **File:** `Views/ManualEntryDialog.xaml` (line 55)

### 2. **Version Number Mismatch Fixed**
- **Issue:** Module version property showed 7.7.1 instead of 8.0.0
- **Root Cause:** `SurveyLogbookModule.cs` Version property not updated during version bump
- **Solution:** Updated Version property to `new Version(8, 0, 0)`
- **File:** `SurveyLogbookModule.cs` (line 59)

### 3. **Property Reference Error Fixed**
- **Issue:** Import methods used non-existent `ObjectName` property variant
- **Root Cause:** Inconsistent property naming between model alias and usage
- **Solution:** Changed to use primary property `ObjectMonitored` for consistency
- **File:** `ViewModels/MainViewModel.cs` (lines 592, 599)

---

## ✨ New Features

### 2. **NaviPac Data Configuration Section**
Added comprehensive UI for configuring NaviPac data capture in Settings window:

#### Debug Logging Toggle
- **Location:** Settings > NaviPac Data Configuration
- **Function:** Enable/disable verbose logging of raw NaviPac data for troubleshooting
- **Default:** Enabled (true)

#### Data Separator Selection
- **Function:** Configure the field separator used in NaviPac User Defined Output
- **Options:** Comma (,), Space, Semicolon (;), Colon (:), Tab
- **Default:** Comma (,)
- **Tip:** Match this to your NaviPac UDO configuration

#### Field Detection Mode
- **Auto-Detect (Recommended):** Intelligently parses fields based on data patterns
- **Manual Field Mapping:** For custom field ordering (use if auto-detect fails)
- **Default:** Auto-Detect enabled

#### Data Fields to Capture
Comprehensive checkbox grid for selecting which NaviPac fields to log:

**Column 1:**
- Event Number
- Date/Time
- Easting
- Northing
- Height/Depth

**Column 2:**
- KP (Kilometre Post)
- DCC (Distance Cross Course)
- Latitude
- Longitude
- Gyro/Heading

**Column 3:**
- Roll
- Pitch
- Heave
- SMG (Speed Made Good)
- CMG (Course Made Good)

#### Quick Selection Buttons
- **Select All Fields:** Enables all data field checkboxes
- **Clear All:** Disables all data field checkboxes

---

## 🔧 Technical Changes

### Files Modified

1. **Views/ManualEntryDialog.xaml**
   - Fixed TimePicker PickerVisibility from `HourMinuteSecond` to `HourMinute`

2. **Views/SettingsWindow.xaml**
   - Added NaviPac Data Configuration section (170+ lines)
   - Increased window height from 700 to 900
   - Increased window width from 650 to 700

3. **ViewModels/SettingsViewModel.cs**
   - Added 18 new properties for data configuration
   - Added `SelectAllFieldsCommand` and `ClearAllFieldsCommand`
   - Updated `LoadSettings()` and `GetSettings()` methods
   - Added `SelectAllFields()` and `ClearAllFields()` methods

4. **ViewModels/MainViewModel.cs**
   - Fixed property reference from ObjectName to ObjectMonitored in import methods

5. **Models/ApplicationSettings.cs**
   - Added 17 new settings properties for NaviPac data configuration
   - Updated `ResetToDefaults()` to include new settings
   - All new settings persist to JSON configuration file

6. **SurveyLogbookModule.cs**
   - Fixed Version property to return 8.0.0

### New Properties in ApplicationSettings

```csharp
// Debug and Separator
bool EnableDebugLogging = true
string NaviPacSeparator = ","
bool AutoDetectFields = true

// Field Capture Flags (all default to true)
bool CaptureEvent
bool CaptureDateTime
bool CaptureEasting
bool CaptureNorthing
bool CaptureHeight
bool CaptureKP
bool CaptureDCC
bool CaptureLatitude
bool CaptureLongitude
bool CaptureGyro
bool CaptureRoll
bool CapturePitch
bool CaptureHeave
bool CaptureSMG
bool CaptureCMG
```

---

## 📋 Settings Persistence

All new settings are automatically saved to:
```
%AppData%\FathomOS\SurveyLogbook\settings.json
```

---

## 🔄 Migration Notes

- No manual migration required
- Existing settings files will automatically get new defaults
- All field captures default to `true` for backward compatibility

---

## 📖 Related Documentation

- **A1_User_defined_Outputs.pdf** - EIVA NaviPac User Defined Output configuration guide
- **MODULE_DOCUMENTATION.md** - Full module documentation

---

## Version Information

- **Version:** 8.0.0
- **Previous Version:** 7.7.1
- **Target Framework:** .NET 8.0-windows
- **UI Framework:** MahApps.Metro 2.4.10
