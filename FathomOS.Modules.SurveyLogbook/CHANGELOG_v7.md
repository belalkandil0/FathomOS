# Survey Electronic Logbook Module - v7 Changelog

## Version 7.6.0 - TCP/UDP Protocol Support (December 30, 2025)

### Overview
Major enhancement adding dual protocol support for NaviPac communication. Users can now choose between TCP (outbound connection) and UDP (inbound listener) based on their NaviPac configuration. Includes comprehensive bug fixes to the TCP implementation and Windows Firewall management for UDP.

### New Features

#### 1. Unified NaviPacClient with TCP/UDP Support
- **New file**: `Services/NaviPacClient.cs` - unified client supporting both protocols
- **Protocol selection**: TCP connects to NaviPac server, UDP listens for broadcasts
- **Configuration**: Settings UI allows protocol selection with descriptive guidance

#### 2. Windows Firewall Management
- **New file**: `Services/FirewallService.cs` - manages firewall rules for UDP
- **Admin detection**: Automatically detects if running with administrator privileges
- **Rule creation**: Can auto-create firewall rules when running as admin
- **Manual commands**: Provides netsh command for manual rule creation
- **Rule checking**: Verifies if required firewall rules exist

#### 3. Enhanced Settings UI
- **Protocol selection**: TCP/UDP radio buttons with protocol description
- **Firewall status panel**: Shows firewall rule status for UDP mode
- **Create Rule button**: One-click firewall rule creation (requires admin)
- **Copy Command button**: Copies netsh command to clipboard for manual execution

### TCP Implementation Fixes

The original TCP implementation had several issues that have been corrected:

| Issue | Problem | Fix |
|-------|---------|-----|
| Inefficient polling | `DataAvailable` + 10ms delays caused high CPU | Replaced with blocking `ReadAsync` |
| Hardcoded timeouts | 30s/10s fixed values | Made configurable via `ConnectionTimeoutMs` |
| ReadAsync without timeout | Could hang indefinitely | Added proper cancellation token support |
| `async void` methods | Exception handling failures | Changed `ScheduleReconnectAsync()` to `async Task` |
| No connection health monitoring | No way to detect stale connections | Added `LastDataReceived` tracking and `IsReceivingData` property |
| No socket optimization | Default socket settings | Added `NoDelay=true`, configurable buffer sizes |
| Small buffers | 1KB buffer | Increased to 8KB default |

### Connection Health Monitoring
- `LastDataReceived`: Timestamp of last received data
- `IsReceivingData`: True if data received within 30 seconds
- `ConnectionStatistics`: Tracks bytes, messages, connection count, source endpoints
- Auto-reconnect with exponential backoff (max 60 seconds)

### Files Added

| File | Description |
|------|-------------|
| `Services/NaviPacClient.cs` | Unified TCP/UDP client (1000+ lines) |
| `Services/FirewallService.cs` | Windows Firewall rule management (400+ lines) |

### Files Modified

| File | Changes |
|------|---------|
| `Services/LogEntryService.cs` | Updated to use new NaviPacClient |
| `Models/ConnectionSettings.cs` | Added `NaviPacProtocol`, `ConnectionTimeoutMs`, `AutoCreateFirewallRule` |
| `Models/ApplicationSettings.cs` | Added new connection properties |
| `ViewModels/SettingsViewModel.cs` | Added protocol selection, firewall commands, status properties |
| `Views/SettingsWindow.xaml` | Added protocol UI, firewall panel |
| `ModuleInfo.json` | Updated version and features |
| `FathomOS.Modules.SurveyLogbook.csproj` | Version bump to 7.6.0 |

### Files Removed

| File | Reason |
|------|--------|
| `Services/NaviPacTcpClient.cs` | Replaced by unified `NaviPacClient.cs` |

### New Settings Properties

```csharp
// ConnectionSettings / ApplicationSettings
public bool EnableNaviPacConnection { get; set; }      // Master enable switch
public NaviPacProtocol NaviPacProtocol { get; set; }  // TCP or UDP
public int ConnectionTimeoutMs { get; set; }           // 1000-60000ms (default: 10000)
public bool AutoCreateFirewallRule { get; set; }       // Auto-create firewall rules if admin
```

### Protocol Selection Guidance

| Protocol | Use Case | Firewall |
|----------|----------|----------|
| **TCP** | NaviPac configured with TCP User Defined Output sending to this machine | No rule needed (outbound) |
| **UDP** | NaviPac broadcasting UDO data to network | Rule required (inbound listener) |

### Technical Notes

#### NaviPac UDO Configuration
NaviPac User Defined Outputs can be configured to send data via:
- **TCP**: Requires NaviPac to connect to our listening port (or us to connect to NaviPac)
- **UDP**: NaviPac broadcasts datagrams to specified IP/port

#### Firewall Requirements
- TCP mode: No firewall rule needed (Windows allows outbound connections by default)
- UDP mode: Requires inbound firewall rule to receive datagrams
- Rule naming convention: `FathomOS_SurveyLogbook_UDP_{Port}`

#### Message Parsing
Both protocols use the same message parser supporting:
- Position data (Easting, Northing, KP, DCC, Depth, Heading)
- Event markers with event numbers and text
- Logging status (start/stop with runline names)
- Flexible field parsing for various NaviPac UDO formats

### Migration Notes
- Existing TCP configurations will continue to work
- Default protocol is TCP (backward compatible)
- No changes required unless switching to UDP mode

---

## Version 7.5.0 - Bug Fix Release (December 28, 2025)

### Overview
Comprehensive code review identified and fixed several binding and namespace issues.

### Bugs Fixed

#### 1. CurrentReport Property Change Notifications
- **Issue**: When switching between DPR reports, the text fields (Client, Vessel, etc.) didn't update
- **Root Cause**: `CurrentReport` setter didn't notify dependent properties
- **Fix**: Added `OnPropertyChanged()` calls for all 10 dependent properties in `CurrentReport` setter

#### 2. Shift ComboBox Binding Failure
- **Issue**: Shift selection in DPR and CrewMemberDialog didn't work - value was always null
- **Root Cause**: ComboBox used `<ComboBoxItem Content="..."/>` which returns `ComboBoxItem` object, not string
- **Fix**: Changed to `<sys:String>...</sys:String>` items in both:
  - `DprView.xaml` (lines 176-181)
  - `CrewMemberDialog.xaml` (lines 57-64)

#### 3. CrewMemberDialog Name Property Conflict
- **Issue**: Using `new string Name` hid the base `Window.Name` property (compiler warning)
- **Fix**: Renamed property to `CrewMemberName` and updated XAML binding

#### 4. Double Namespace Issue (System.Windows.System.Windows)
- **Issue**: Previous batch fix created malformed namespace `System.Windows.System.Windows.MessageBoxButton`
- **Affected Files**: 
  - MainWindow.xaml.cs
  - ManualEntryDialog.xaml.cs
  - SettingsViewModel.cs
  - MainViewModel.cs
  - DvrRecordingsViewModel.cs
  - PositionFixesViewModel.cs
  - SurveyLogViewModel.cs
- **Fix**: Global sed replacement to correct namespace

### Files Modified

| File | Changes |
|------|---------|
| `ViewModels/DprViewModel.cs` | Added 10 `OnPropertyChanged()` calls in `CurrentReport` setter |
| `Views/DprView.xaml` | Changed Shift ComboBox to use string items |
| `Views/CrewMemberDialog.xaml` | Changed Shift ComboBox to use string items, updated Name binding |
| `Views/CrewMemberDialog.xaml.cs` | Renamed `Name` property to `CrewMemberName` |
| `Views/MainWindow.xaml.cs` | Fixed double namespace |
| `Views/ManualEntryDialog.xaml.cs` | Fixed double namespace |
| `ViewModels/SettingsViewModel.cs` | Fixed double namespace |
| `ViewModels/MainViewModel.cs` | Fixed double namespace |
| `ViewModels/DvrRecordingsViewModel.cs` | Fixed double namespace |
| `ViewModels/PositionFixesViewModel.cs` | Fixed double namespace |
| `ViewModels/SurveyLogViewModel.cs` | Fixed double namespace |

### Technical Notes

#### Property Change Notification Pattern for Wrapper Properties
When a ViewModel wraps properties from a model object, changing the model requires notifying all wrapper properties:

```csharp
public DprReport? CurrentReport
{
    get => _currentReport;
    set
    {
        if (SetProperty(ref _currentReport, value))
        {
            // Notify all dependent wrapper properties
            OnPropertyChanged(nameof(Client));
            OnPropertyChanged(nameof(Vessel));
            // ... etc
        }
    }
}
```

#### ComboBox String Binding Pattern
For string properties, use string items directly instead of ComboBoxItem:

```xml
<!-- WRONG - Returns ComboBoxItem object -->
<ComboBox SelectedItem="{Binding Shift}">
    <ComboBoxItem Content="Day"/>
</ComboBox>

<!-- CORRECT - Returns string value -->
<ComboBox SelectedItem="{Binding Shift}">
    <sys:String xmlns:sys="clr-namespace:System;assembly=mscorlib">Day</sys:String>
</ComboBox>
```

---

## Version 7.4.0 - DPR Crew Button Fix & UX Improvements (December 28, 2025)

### Overview
Fixed DPR crew member button not functioning and improved the DPR user experience.

### Bugs Fixed

#### 1. DPR "Add Crew Member" Button Not Working
- **Issue**: Clicking "Add Crew Member" button in DPR tab had no visible effect
- **Root Cause**: Multiple binding issues in DprView.xaml:
  - Crew count bound to `CurrentReport.CrewOnDuty.Count` (wrong collection - `CrewOnDuty` is `List<string>`)
  - ItemsSource bound to `CurrentReport.CrewOnDuty` instead of `CurrentReport.SurveyCrew`
  - Display template used `{Binding Role}` but property is `Rank`
- **Fix**: 
  - Changed crew section header from "Crew On Duty" to "Survey Crew"
  - Updated crew count binding to `CurrentReport.SurveyCrew.Count` with FallbackValue
  - Changed ItemsSource to `CurrentReport.SurveyCrew`
  - Fixed display binding from `{Binding Role}` to `{Binding Rank}`

#### 2. DPR Commands Not Working Correctly
- **Issue**: Commands had parameter handling issues
- **Fix**: Updated command signatures:
  - `RemoveCrewMemberCommand` now accepts `CrewMember` parameter from `CommandParameter="{Binding}"`
  - `RemoveTransponderCommand` now accepts `TransponderInfo` parameter
  - Added proper CanExecute checks: `p => CurrentReport != null && p is Type`

### UX Improvements

#### 1. Added "No Report Selected" State
- **Change**: Added visual feedback when no DPR report is selected
- **Details**: 
  - Right panel now shows centered message with document icon
  - Text: "No Report Selected - Click 'New Report' to create a new DPR or select an existing report from the list"
  - Report editor only visible when `HasCurrentReport` is true

#### 2. Added CrewMemberDialog
- **New File**: `Views/CrewMemberDialog.xaml` and `Views/CrewMemberDialog.xaml.cs`
- **Features**:
  - Proper form with Name, Rank/Role (combo with presets), Shift, Employer, Date On Board
  - Input validation (name required)
  - Dark theme support
  - Can be used for both adding and editing crew members

### Files Modified

| File | Changes |
|------|---------|
| `Views/DprView.xaml` | Added InverseBoolToVisibility converter, "No Report Selected" panel, Grid wrapper for right panel, fixed crew section bindings |
| `ViewModels/DprViewModel.cs` | Updated AddCrewMember to use dialog, fixed command parameter handling |
| `Views/CrewMemberDialog.xaml` | NEW - Crew member add/edit dialog |
| `Views/CrewMemberDialog.xaml.cs` | NEW - Dialog code-behind with validation |
| `ModuleInfo.json` | Version 7.4.0 |
| `FathomOS.Modules.SurveyLogbook.csproj` | Version 7.4.0 |
| `SurveyLogbookModule.cs` | Version 7.4.0 |

### Technical Notes

#### CrewMember Model Properties (DprSupportModels.cs)
```csharp
public class CrewMember
{
    public string Name { get; set; }
    public string Rank { get; set; }      // NOT "Role"
    public string Shift { get; set; }
    public string Employer { get; set; }
    public DateTime DateOnBoard { get; set; }  // NOT nullable
}
```

#### Command Parameter Pattern for List Items
```csharp
// XAML - Pass current item as parameter
<Button Command="{Binding DataContext.RemoveCrewMemberCommand, 
                          RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}"/>

// ViewModel - Accept parameter in command
RemoveCrewMemberCommand = new RelayCommand(
    p => RemoveCrewMember(p as CrewMember), 
    p => CurrentReport != null && p is CrewMember);
```

---

## Version 7.3.0 - Build Error Fixes (December 28, 2025)

### Overview
Fixed remaining build errors discovered after v7.2.0 release.

### Bugs Fixed

#### 1. SurveyLogViewModel.cs (Line 436)
- **Error**: CS0234 - The type or namespace name 'System' does not exist in the namespace 'System.Windows'
- **Cause**: Typo: `System.Windows.System.Windows.Application` instead of `System.Windows.Application`
- **Fix**: Corrected to `System.Windows.Application.Current?.Dispatcher.Invoke(...)`

#### 2. DprViewModel.cs (Lines 349, 484)
- **Error**: CS0023 - Operator '?' cannot be applied to operand of type 'DateTime'
- **Cause**: `DateOnBoard` is a non-nullable `DateTime`, not `DateTime?`
- **Fix**: Changed `crew.DateOnBoard?.ToString("yyyy-MM-dd") ?? ""` to `crew.DateOnBoard.ToString("yyyy-MM-dd")`
- Affected both Excel export (line 349) and PDF export (line 484)

### Technical Notes
- All SaveFileDialog/OpenFileDialog references were already properly qualified with `Microsoft.Win32.` in v7.2.0
- The CS0006 metadata file error was caused by cascading failures from the above errors

---

## Version 7.2.0 - Namespace Ambiguity Resolution (December 28, 2025)

### Overview
Resolved all 90+ CS0104 namespace ambiguity errors caused by conflicts between:
- `System.Windows` (WPF)
- `System.Windows.Forms` (WinForms - used for FolderBrowserDialog)
- `QuestPDF.Infrastructure` (PDF library)

### Error Categories Fixed

| Category | Count | Files Affected |
|----------|-------|----------------|
| MessageBox conflicts | 25+ | All ViewModels, Module, Views |
| Color/Brush conflicts | 15+ | Converters.cs, PdfReportGenerator.cs |
| SaveFileDialog/OpenFileDialog | 8 | ViewModels |
| Application conflicts | 3 | MainViewModel, SurveyLogViewModel |
| Missing model properties | 15+ | Multiple models |

### Files Modified

#### 1. Models/LogEntryType.cs
- Added missing enum values: Comment (902), EquipmentSetup (903), EquipmentFailure (904), WeatherCondition (905), VesselMovement (906), PersonnelChange (907), SafetyIncident (908), OperationStart (909), OperationEnd (910)
- Updated GetDisplayName() extension method for new values

#### 2. Converters/Converters.cs
- All `Color`, `Brushes`, `SolidColorBrush` usages replaced with fully qualified `System.Windows.Media.*` types
- 15+ occurrences fixed

#### 3. Export/PdfReportGenerator.cs
- Added alias: `using QuestColor = QuestPDF.Infrastructure.Color;`
- Replaced all 10+ Color references with `QuestColor`
- Prevents conflict with System.Drawing.Color and System.Windows.Media.Color

#### 4. Models/PositionFix.cs
- Added `PositionFixType.Waypoint` and `PositionFixType.Manual` enum values
- Made `ComputedEasting`/`ComputedNorthing` settable (aliases for Easting/Northing)
- Added `PositionFixType` property (alias for FixType)
- Added `Name` property (alias for ObjectMonitored)
- Added `Source` property (alias for SourceFile)

#### 5. Models/SurveyLogEntry.cs
- Added `RawData` property (string?)
- Fixed `IsManualEntry` - remains computed property based on EntryType

#### 6. Models/DprReport.cs
- Added `CrewMembers` property (alias for SurveyCrew)

#### 7. Views/PositionFixDialog.xaml.cs
- Fixed Time property assignment (TimeSpan, not DateTime)

#### 8. Views/ManualEntryDialog.xaml.cs
- Fixed: Removed invalid `IsManualEntry = true` assignment (computed property)
- Fully qualified `System.Windows.MessageBoxButton` and `System.Windows.MessageBoxImage`

#### 9. ViewModels/MainViewModel.cs (11 occurrences)
- All `MessageBox.Show` → `System.Windows.MessageBox.Show`
- All `MessageBoxResult` → `System.Windows.MessageBoxResult`
- All `Application.Current` → `System.Windows.Application.Current`

#### 10. ViewModels/SurveyLogViewModel.cs
- All `MessageBox.Show` → `System.Windows.MessageBox.Show`
- All `Application.Current` → `System.Windows.Application.Current`

#### 11. ViewModels/DprViewModel.cs (6 occurrences)
- All `MessageBox.Show` → `System.Windows.MessageBox.Show`
- All `SaveFileDialog` → `Microsoft.Win32.SaveFileDialog`

#### 12. ViewModels/DvrRecordingsViewModel.cs
- `SaveFileDialog` → `Microsoft.Win32.SaveFileDialog`
- `FolderBrowserDialog` uses `System.Windows.Forms.FolderBrowserDialog` (intentional)

#### 13. ViewModels/PositionFixesViewModel.cs
- `SaveFileDialog` → `Microsoft.Win32.SaveFileDialog`

#### 14. SurveyLogbookModule.cs (2 occurrences)
- All `MessageBox.Show` → `System.Windows.MessageBox.Show`

#### 15. Views/MainWindow.xaml.cs (2 occurrences)
- All `MessageBox.Show` → `System.Windows.MessageBox.Show`

### Namespace Strategy

**Solution Applied**: Fully qualify all ambiguous types instead of using aliases at the top of files.

| Type | Resolution |
|------|------------|
| `MessageBox.Show` | `System.Windows.MessageBox.Show` |
| `MessageBoxButton` | `System.Windows.MessageBoxButton` |
| `MessageBoxImage` | `System.Windows.MessageBoxImage` |
| `MessageBoxResult` | `System.Windows.MessageBoxResult` |
| `Application.Current` | `System.Windows.Application.Current` |
| `SaveFileDialog` | `Microsoft.Win32.SaveFileDialog` |
| `OpenFileDialog` | `Microsoft.Win32.OpenFileDialog` |
| `FolderBrowserDialog` | `System.Windows.Forms.FolderBrowserDialog` (WinForms) |
| `Color` (WPF) | `System.Windows.Media.Color` |
| `Brushes` (WPF) | `System.Windows.Media.Brushes` |
| `SolidColorBrush` (WPF) | `System.Windows.Media.SolidColorBrush` |
| `Color` (QuestPDF) | `QuestColor` (via `using QuestColor = QuestPDF.Infrastructure.Color;`) |

### Why These Conflicts Occurred

The project uses:
1. **WPF** (`System.Windows`) - Main UI framework
2. **WinForms** (`System.Windows.Forms`) - For `FolderBrowserDialog` (no WPF equivalent)
3. **QuestPDF** - PDF generation (has its own `Color` type)
4. **System.Drawing** - Referenced transitively by QuestPDF

This combination creates namespace collisions when types have the same name but different source assemblies.

---

## Version 7.1.0 - Build Fix (December 28, 2025)

### Build Error Fixes

Fixed 4 CS0104 ambiguous reference errors caused by conflict between `System.Windows.Forms.UserControl` and `System.Windows.Controls.UserControl`:

| File | Fix Applied |
|------|-------------|
| `Views/DprView.xaml.cs` | Changed to `System.Windows.Controls.UserControl` fully qualified |
| `Views/DvrRecordingsView.xaml.cs` | Changed to `System.Windows.Controls.UserControl` fully qualified |
| `Views/PositionFixesView.xaml.cs` | Changed to `System.Windows.Controls.UserControl` fully qualified |
| `Views/SurveyLogView.xaml.cs` | Changed to `System.Windows.Controls.UserControl` fully qualified |

**Root Cause**: The project references `System.Windows.Forms` (for `FolderBrowserDialog` in Settings) and `System.Windows.Controls` (for WPF UserControl), causing namespace ambiguity when both are in scope.

**Solution**: Removed `using System.Windows.Controls;` statements and used fully qualified type names (`System.Windows.Controls.UserControl`) in class declarations.

---

## Version 7.0.0 - Bug Fixes & Feature Completion

### Bug Fixes

#### 1. SurveyLogViewModel - AddManualEntry
- **Issue**: AddManualEntry was a placeholder with TODO comment
- **Fix**: Implemented to show ManualEntryDialog and create proper entries
- **File**: `ViewModels/SurveyLogViewModel.cs`

#### 2. SurveyLogViewModel - ExportSelection
- **Issue**: ExportSelection was a placeholder with TODO comment  
- **Fix**: Implemented full Excel/CSV export using ClosedXML with SaveFileDialog
- **File**: `ViewModels/SurveyLogViewModel.cs`

#### 3. SurveyLogViewModel - ViewDetails
- **Issue**: ViewDetails was a placeholder with TODO comment
- **Fix**: Implemented to show detailed MessageBox with all entry information
- **File**: `ViewModels/SurveyLogViewModel.cs`

#### 4. SurveyLogViewModel - Missing Usings
- **Issue**: Missing `System.Window` and `System.Linq` namespaces
- **Fix**: Added required using statements
- **File**: `ViewModels/SurveyLogViewModel.cs`

#### 5. DvrRecordingsViewModel - ExportDvrList
- **Issue**: Placeholder implementation with "to be implemented" message
- **Fix**: Full Excel/CSV export with ClosedXML, SaveFileDialog, proper formatting
- **File**: `ViewModels/DvrRecordingsViewModel.cs`

#### 6. PositionFixesViewModel - ExportFixes
- **Issue**: Placeholder implementation with "to be implemented" message
- **Fix**: Full Excel/CSV export with all position fix fields
- **File**: `ViewModels/PositionFixesViewModel.cs`

#### 7. DprViewModel - ExportWordAsync
- **Issue**: TODO placeholder for Word export
- **Fix**: Implemented as Excel export using ClosedXML (more practical for survey industry)
- **File**: `ViewModels/DprViewModel.cs`

#### 8. DprViewModel - ExportPdfAsync
- **Issue**: TODO placeholder for PDF export
- **Fix**: Full PDF export using QuestPDF with professional formatting, tables, sections
- **File**: `ViewModels/DprViewModel.cs`

#### 9. DvrRecording Model - Missing File Properties
- **Issue**: Missing FilePath, FileName, FileSizeMB, RecordingDate for file browser mode
- **Fix**: Added all file-specific properties with proper backing fields
- **File**: `Models/DvrRecording.cs`

#### 10. DvrRecording Model - StartDateTime Not Settable
- **Issue**: StartDateTime was read-only but RefreshDvrRecordings needed to set it
- **Fix**: Made StartDateTime settable (sets Date and StartTime internally)
- **File**: `Models/DvrRecording.cs`

#### 11. DvrRecording Model - Missing Channel Property
- **Issue**: Channel property not defined but used in DvrRecordingsViewModel
- **Fix**: Added Channel property
- **File**: `Models/DvrRecording.cs`

#### 12. PositionFix Model - Missing Calibration Properties
- **Issue**: RequiredEasting, RequiredNorthing, ErrorEasting, ErrorNorthing not defined
- **Fix**: Added all calibration-specific properties for SetEastingNorthing data
- **File**: `Models/PositionFix.cs`

#### 13. PositionFixesView.xaml - Wrong Binding Name
- **Issue**: Used `PositionFixType` instead of `FixType`
- **Fix**: Changed binding to `FixType`
- **File**: `Views/PositionFixesView.xaml`

#### 14. DprView.xaml - Export Button Mismatch
- **Issue**: Button showed "Word" but implementation exports to Excel
- **Fix**: Changed icon to MicrosoftExcel and text to "Excel"
- **File**: `Views/DprView.xaml`

### Dependencies Added (in using statements)
- `ClosedXML.Excel` - Excel export functionality
- `QuestPDF.Fluent` - PDF generation
- `QuestPDF.Helpers` - PDF helpers (colors, page sizes)
- `QuestPDF.Infrastructure` - PDF license configuration
- `Microsoft.Win32` - SaveFileDialog

### Export Features Now Fully Functional

| Feature | Format | Status |
|---------|--------|--------|
| Survey Log Export | Excel, CSV | ✅ Complete |
| DVR List Export | Excel, CSV | ✅ Complete |
| Position Fixes Export | Excel, CSV | ✅ Complete |
| DPR Export to Excel | Excel (.xlsx) | ✅ Complete |
| DPR Export to PDF | PDF | ✅ Complete |

### Testing Checklist

- [ ] Open module, verify all 4 tabs load
- [ ] Survey Events tab:
  - [ ] Add Manual Entry button opens dialog
  - [ ] Export Selection exports filtered entries
  - [ ] View Details shows entry information
  - [ ] Copy to Clipboard works
- [ ] DVR Recordings tab:
  - [ ] Browse folder works
  - [ ] Refresh scans for video files
  - [ ] Export DVR List creates Excel/CSV
  - [ ] Open File launches video player
  - [ ] Open Folder opens Explorer
- [ ] Position Fixes tab:
  - [ ] Filter by type works
  - [ ] Export Fixes creates Excel/CSV
  - [ ] Add/Delete position fixes works
- [ ] DPR tab:
  - [ ] Create new report works
  - [ ] Export to Excel creates .xlsx file
  - [ ] Export to PDF creates .pdf file with proper formatting

---
**Version**: 7.0.0  
**Date**: December 2024  
**Previous Version**: 6.0.0 (UI Completeness)
