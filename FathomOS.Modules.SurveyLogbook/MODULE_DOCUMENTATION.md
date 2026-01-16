# Fathom OS - Survey Electronic Logbook Module

## Version 1.0.0 | December 2024 | Complete Technical Documentation

---

## 📋 Module Overview

The **Survey Electronic Logbook** module provides comprehensive survey data logging, monitoring, and reporting capabilities for offshore survey operations. It integrates with NaviPac navigation software and VisualWorks DVR systems to capture real-time survey events.

### Key Features

| Feature | Description |
|---------|-------------|
| **Real-time Logging** | Captures NaviPac events, position fixes, and DVR recordings |
| **DPR Management** | Daily Progress Report and Shift Handover documentation |
| **Multi-format Export** | Excel, PDF, Word, and custom .slog format |
| **Network Integration** | Online TCP/IP connection with NaviPac |
| **File Monitoring** | Automatic detection of .npc, .wp2, and video files |
| **Custom Paths** | User-configurable paths for all data sources |

---

## 🏗️ Architecture

### Module Structure

```
FathomOS.Modules.SurveyLogbook/
├── SurveyLogbookModule.cs          # IModule implementation
├── ModuleInfo.json                 # Module metadata
│
├── Assets/
│   └── icon.png                    # Module icon (128×128)
│
├── Models/
│   ├── SurveyLogEntry.cs           # Core log entry model
│   ├── PositionFix.cs              # Position fix data model
│   ├── DvrRecording.cs             # DVR recording session model
│   ├── EivaDataLog.cs              # EIVA NaviPac/NaviScan log model
│   ├── DprReport.cs                # Daily Progress Report model
│   ├── ShiftHandover.cs            # Shift handover data model
│   ├── CrewMember.cs               # Survey crew information
│   ├── TransponderInfo.cs          # Transponder management data
│   ├── SubseaEquipment.cs          # Subsea equipment tracking
│   ├── OperationalStatus.cs        # Operational hours tracking
│   ├── ProjectInfo.cs              # Project configuration
│   └── SurveyLogFile.cs            # .slog file format model
│
├── Services/
│   ├── NaviPacClient.cs              # Unified TCP/UDP client for NaviPac
│   ├── FirewallService.cs            # Windows Firewall rule management
│   ├── NpcFileMonitor.cs           # Position fix file monitor
│   ├── WaypointFileMonitor.cs      # Waypoint file monitor
│   ├── DvrFolderMonitor.cs         # VisualWorks folder monitor
│   ├── SurveyLogManager.cs         # Central log management
│   ├── ProjectInfoService.cs       # Project info auto-detection
│   └── ThemeService.cs             # Theme management
│
├── Parsers/
│   ├── NpcFileParser.cs            # .npc file parser
│   ├── WaypointFileParser.cs       # .wp2 file parser
│   ├── DvrFolderParser.cs          # DVR folder structure parser
│   └── SlogFileHandler.cs          # .slog file read/write
│
├── Export/
│   ├── ExcelExporter.cs            # Excel workbook export
│   ├── PdfReportGenerator.cs       # PDF report generation
│   ├── WordDprExporter.cs          # Word DPR export
│   └── SlogExporter.cs             # .slog format export
│
├── ViewModels/
│   ├── MainViewModel.cs            # Main window view model
│   ├── SurveyLogViewModel.cs       # Survey log tab view model
│   ├── DprViewModel.cs             # DPR tab view model
│   ├── ConnectionSettingsViewModel.cs  # Settings dialog view model
│   ├── ViewModelBase.cs            # Base view model class
│   └── RelayCommand.cs             # ICommand implementation
│
├── Views/
│   ├── MainWindow.xaml             # Main window with tabs
│   ├── SurveyLogView.xaml          # Survey log tab
│   ├── DprView.xaml                # DPR tab
│   └── ConnectionSettingsDialog.xaml   # Settings dialog
│
├── Converters/
│   └── Converters.cs               # Value converters
│
├── Themes/
│   ├── DarkTheme.xaml
│   ├── LightTheme.xaml
│   ├── ModernTheme.xaml
│   └── GradientTheme.xaml
│
└── Data/
    └── DefaultSettings.json        # Default configuration
```

---

## 📊 Data Models

### Tab 1: Survey Log Data

#### SurveyLogEntry
```csharp
public class SurveyLogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public LogEntryType EntryType { get; set; }
    public string Source { get; set; }           // NaviPac, DVR, Manual
    public string Description { get; set; }
    public string Vehicle { get; set; }          // HD11, HD12, Ross Candies
    public string Comments { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

#### PositionFix (from Excel: "Pos Fixes" sheet)
```csharp
public class PositionFix
{
    public int FixNumber { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public double Easting { get; set; }          // US Survey Feet
    public double Northing { get; set; }
    public double? SdEasting { get; set; }       // Standard Deviation
    public double? SdNorthing { get; set; }
    public int NumberOfFixes { get; set; }
    public string PositioningAid { get; set; }   // e.g., "USBL"
    public string Vehicle { get; set; }
    public double? Kp { get; set; }
    public double? Dcc { get; set; }
    public string Comments { get; set; }
}
```

#### DvrRecording (from Excel: "DVR Reg" sheet)
```csharp
public class DvrRecording
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Vehicle { get; set; }          // HD11, HD12, Ross Candies
    public string FolderPath { get; set; }       // Full hierarchical path
    public string ProjectTask { get; set; }      // Parsed from folder
    public string SubTask { get; set; }
    public string Operation { get; set; }
    public string Comment { get; set; }
    public List<string> VideoFiles { get; set; }
}
```

#### EivaDataLog (from Excel: "EIVA Data Log" sheet)
```csharp
public class EivaDataLog
{
    public Guid Id { get; set; }
    public string NaviPacStartFile { get; set; }
    public string NaviScanStartFile { get; set; }
    public string RovinsFile { get; set; }
    public string Runline { get; set; }
    public DateTime Date { get; set; }
    public string Vehicle { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public double? StartKp { get; set; }
    public double? EndKp { get; set; }
    public string Comments { get; set; }
}
```

### Tab 2: DPR & Shift Handover Data

#### DprReport (from Word document structure)
```csharp
public class DprReport
{
    // Header Information (Auto-populated from Project)
    public string Client { get; set; }
    public string Vessel { get; set; }
    public string ProjectNumber { get; set; }
    public string LocationDepth { get; set; }
    public string OffshoreManager { get; set; }
    public string ProjectSurveyor { get; set; }
    public string PartyChief { get; set; }
    public DateTime ReportDate { get; set; }
    
    // Daily Log (Time-based entries)
    public ObservableCollection<DailyLogEntry> DailyLog { get; set; }
    
    // Text Sections
    public string Last24HrsHighlights { get; set; }
    public string KnownIssues { get; set; }
    public string GeneralSurveyComments { get; set; }
    public string SurveyTasksToComplete { get; set; }
    public string ProjectInformation { get; set; }
    public string SpeedOfSoundInfo { get; set; }
    public string MocsIssued { get; set; }
    
    // Crew Management
    public ObservableCollection<CrewMember> SurveyCrew { get; set; }
    public string CrewComments { get; set; }
    
    // Equipment Tracking
    public string SurveyEquipmentIssues { get; set; }
    public string ThirdPartyEquipmentIssues { get; set; }
    public ObservableCollection<TransponderInfo> Transponders { get; set; }
    public ObservableCollection<SubseaEquipment> SubseaEquipment { get; set; }
    public string ItemsWetStored { get; set; }
    
    // Status Sections
    public string HseNotes { get; set; }
    public string WeatherConditions { get; set; }
    public ObservableCollection<FieldReport> FieldReports { get; set; }
    public string DataManagement { get; set; }
    public OperationalStatus OperationalStatus { get; set; }
    public string MaterialRequests { get; set; }
}
```

---

## 🔌 Data Sources & Integration

### 1. NaviPac TCP/IP Connection

**Purpose**: Real-time navigation events and position updates

```
┌─────────────────┐         TCP/IP          ┌─────────────────┐
│    NaviPac      │ ─────────────────────▶  │  Survey Logbook │
│  (Port 4001)    │   User Defined Output   │    Module       │
└─────────────────┘                         └─────────────────┘
```

**Configuration**:
- Host: User-configurable (default: localhost)
- Port: User-configurable (default: 4001)
- Protocol: TCP with auto-reconnect
- Data Format: Custom NMEA-style or free ASCII

### 2. NaviPac File Monitoring

#### .npc Files (Position Fix Reports)
**Purpose**: Detailed position fix statistics and observations

**Sample Format**:
```
EIVA XYZ calibration report: 19.06.2025 20:26:53 | Object: 0000 Island Performer
------------------------------------------------------------------------------------------
     Error    Average     Median    Minimum    Maximum  Std. dev. 95%err.range      Count 
------------------------------------------------------------------------------------------
Easting     -1658.169  -1658.190  -1658.290  -1658.020      0.082        0.160   10 of 10
Northing   -20281.733 -20281.710 -20281.920 -20281.530      0.151        0.296   10 of 10
...
Date       Time         Observed X Observed Y Observed Z
------------------------------------------------------------------------------------------
20/06/2025 01:26:43.107 2129798.80 9765368.16     -87.24
```

**Monitored Path**: User-configurable (e.g., `C:\EIVA\NaviPac\Data\`)

#### .wp2 Files (Waypoints)
**Purpose**: Waypoint definitions and coordinates

**Sample Format**:
```
"WP001"; 2520289.710; 10130994.900; 0.000; 7.1; 3.1; 7.1; ""; 0.00; -8.1; ""; 0.00; ""; 1; 0.000; 0.000; 0.000; 0; 0.05
```

**Monitored Path**: User-configurable (e.g., `C:\EIVA\NaviPac\Waypoints\`)

### 3. VisualWorks DVR Monitoring

**Purpose**: Automatic detection of video recording sessions

**Folder Structure Example**:
```
Z:\VisualWorks\Projects\07.TC1127_Bracon_Various_Ops\
├── A.SFL_Type3_Flying_Leads_Installation\
│   └── B.SFL11_SFL-1411-001\
│       └── B.Landing_Laying_Operations\
│           └── B.HD12\
│               ├── 2025-07-01_13-33-00.wmv
│               └── 2025-07-01_14-15-00.wmv
└── C.Special_Tasks\
    └── A.HD11\
        └── A.Beacon_Deployment\
```

**Parsing Logic**:
- Extracts project task hierarchy from folder names
- Removes prefixes (A., B., C., etc.)
- Identifies vehicle from folder structure
- Captures video file timestamps

---

## 📁 .slog File Format

### Purpose
Custom export format for managers to load and review survey log files offline.

### Format Specification

```json
{
  "fileVersion": "1.0",
  "formatType": "FathomOS.SurveyLog",
  "exportDate": "2025-07-01T18:30:00Z",
  "exportedBy": "George Venable",
  
  "projectInfo": {
    "client": "Beacon",
    "vessel": "Ross Candies",
    "projectNumber": "TC1127",
    "projectName": "Bracon Various Operations",
    "location": "WR51",
    "startDate": "2025-06-03",
    "coordinateSystem": "BLM zone 15N (US survey feet)"
  },
  
  "surveyLog": {
    "dvrRecordings": [...],
    "positionFixes": [...],
    "eivaDataLogs": [...],
    "manualEntries": [...]
  },
  
  "dprReports": [...],
  
  "metadata": {
    "totalEntries": 1250,
    "dateRange": {
      "start": "2025-06-03",
      "end": "2025-07-01"
    },
    "checksum": "SHA256:..."
  }
}
```

### File Extension
- `.slog` - Survey Log file
- `.slogz` - Compressed Survey Log file (gzip)

---

## 🖥️ User Interface

### Main Window Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Survey Electronic Logbook - Fathom OS                              _ □ ✕  │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌───────────────┐  ┌────────────────────┐                                  │
│  │ 📋 Survey Log │  │ 📄 DPR & Handover  │                    ⚙️ Settings  │
│  └───────────────┘  └────────────────────┘                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                         [Tab Content Area]                                  │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  Status: Connected to NaviPac (192.168.1.100:4001) │ Entries: 1,250 │ ●    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Tab 1: Survey Log

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PROJECT INFO                                                               │
│  Client: Beacon    Vessel: Ross Candies    Project: TC1127    Date: 01 Jul │
├──────────────────────────────────────────────────┬──────────────────────────┤
│  SUB-TABS                                        │  FILTERS                 │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐        │  Date: [01/07/2025  ▼]  │
│  │DVR Reg   │ │Pos Fixes │ │EIVA Data  │        │  Vehicle: [All       ▼]  │
│  └──────────┘ └──────────┘ └───────────┘        │  Type: [All          ▼]  │
├──────────────────────────────────────────────────┴──────────────────────────┤
│  DATA GRID                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Date       │ Start   │ End     │ Vehicle │ Folder/Comment             ││
│  ├─────────────────────────────────────────────────────────────────────────┤│
│  │ 01/07/2025 │ 13:33   │ 13:44   │ HD11    │ Landing Operations...      ││
│  │ 01/07/2025 │ 13:38   │ 13:38   │ HD11    │ Position Fix #1            ││
│  │ 01/07/2025 │ 14:15   │ 14:30   │ HD12    │ Beacon Deployment          ││
│  └─────────────────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────────────────┤
│  [+ Add Entry]  [🗑️ Delete]  [📤 Export Excel]  [📤 Export PDF]  [💾 Save] │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Tab 2: DPR & Shift Handover

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  HEADER (Auto-populated)                                         [📅 Date] │
│  Client: Beacon          │ Offshore Mgr: Dave Marshal                       │
│  Vessel: Ross Candies    │ Project Surveyor: Rafael Avila                   │
│  Project: TC1127         │ Party Chief: George Venable                      │
│  Location: WR51          │                                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  LAST 24HRS DAILY LOG KEY TIMES                                             │
│  ┌───────┬──────────────────────────────────────────────────────────────┐  │
│  │ Time  │ Activity                                                      │  │
│  ├───────┼──────────────────────────────────────────────────────────────┤  │
│  │ 0000  │ Midnight position: 29°08'13.30" N  90°12'15.85" W            │  │
│  │ 0600  │ Vessel moored at dock                                        │  │
│  │ 1722  │ Began HD11 Sprint Cal & Vessel sensor verification           │  │
│  └───────┴──────────────────────────────────────────────────────────────┘  │
│  [+ Add Time Entry]                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  SECTIONS (Collapsible)                                                     │
│  ▼ Last 24hrs Highlights                                                    │
│    ┌────────────────────────────────────────────────────────────────────┐  │
│    │ Vessel at dock at Intermoor for equipment transfers                │  │
│    └────────────────────────────────────────────────────────────────────┘  │
│  ► Known Issues                                                             │
│  ► General Survey Comments                                                  │
│  ► Survey Tasks to Complete                                                 │
│  ► Crew Status                                                              │
│  ► Transponder Management                                                   │
│  ► Equipment Status                                                         │
│  ► Operational Status                                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│  [📤 Export Word]  [📤 Export PDF]  [📤 Export Excel]  [💾 Save Report]    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Configuration

### Connection Settings Dialog

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Connection Settings                                                _ □ ✕  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  NAVIPAC TCP CONNECTION                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Host:     [192.168.1.100        ]   Port: [4001    ]                │   │
│  │ [✓] Auto-reconnect on disconnect                                    │   │
│  │ Status: ● Connected                          [Test] [Connect]       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  NAVIPAC FILE MONITORING                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [✓] Enable Position Fix Monitoring                                  │   │
│  │ Fix Output Folder: [C:\Survey\NaviPac\Fixes           ] [Browse...] │   │
│  │                                                                     │   │
│  │ [✓] Enable Waypoint Monitoring                                      │   │
│  │ Waypoint Folder:   [C:\Survey\NaviPac\Waypoints       ] [Browse...] │   │
│  │                                                                     │   │
│  │ [✓] Monitor subdirectories                                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  VISUALWORKS DVR MONITORING                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [✓] Enable DVR Folder Monitoring                                    │   │
│  │ Project Folder:    [Z:\VisualWorks\Projects\TC1127    ] [Browse...] │   │
│  │                                                                     │   │
│  │ [✓] Parse folder hierarchy for project structure                    │   │
│  │ [✓] Monitor subdirectories                                          │   │
│  │                                                                     │   │
│  │ Vehicle Folder Mappings:                                            │   │
│  │ ┌──────────────────────────────────────────────────────────────┐   │   │
│  │ │ Folder Pattern      │ Vehicle Name                           │   │   │
│  │ ├──────────────────────────────────────────────────────────────┤   │   │
│  │ │ HD11                │ HD11                                   │   │   │
│  │ │ HD12                │ HD12                                   │   │   │
│  │ │ Ross*               │ Ross Candies                           │   │   │
│  │ └──────────────────────────────────────────────────────────────┘   │   │
│  │ [+ Add] [Edit] [Remove]                                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  OUTPUT SETTINGS                                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Default Export Folder: [C:\Survey\Exports             ] [Browse...] │   │
│  │ Auto-save interval:    [5        ] minutes                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│                                              [Save Settings]  [Cancel]      │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📤 Export Formats

### 1. Excel Export (.xlsx)
- **Survey Log**: Separate sheets for DVR Register, Position Fixes, EIVA Data Log
- **DPR**: Complete DPR with all sections and tables
- Uses ClosedXML (from FathomOS.Core)

### 2. PDF Export (.pdf)
- Professional formatted reports
- Company branding support
- Uses QuestPDF (from FathomOS.Core)

### 3. Word Export (.docx) - DPR Only
- Matches original DPR template structure
- Uses docx library (JavaScript) or python-docx

### 4. Survey Log Export (.slog)
- Complete survey log data in JSON format
- Includes all entries and project info
- Compressed option (.slogz)
- Managers can load for offline review

---

## 🔄 Data Flow

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   NaviPac TCP   │────▶│                 │     │                 │
│   (Real-time)   │     │                 │     │                 │
└─────────────────┘     │                 │     │                 │
                        │  SurveyLog      │────▶│   UI Display    │
┌─────────────────┐     │  Manager        │     │   (DataGrid)    │
│  .npc Files     │────▶│                 │     │                 │
│  (Fix Reports)  │     │                 │     │                 │
└─────────────────┘     │                 │     │                 │
                        │                 │     └─────────────────┘
┌─────────────────┐     │                 │              │
│  .wp2 Files     │────▶│                 │              │
│  (Waypoints)    │     │                 │              ▼
└─────────────────┘     │                 │     ┌─────────────────┐
                        │                 │     │   Export        │
┌─────────────────┐     │                 │────▶│   - Excel       │
│  VisualWorks    │────▶│                 │     │   - PDF         │
│  (DVR Folders)  │     └─────────────────┘     │   - Word        │
└─────────────────┘                             │   - .slog       │
                                                └─────────────────┘
```

---

## 🚀 Implementation Phases

### Phase 1: Core Infrastructure (Week 1-2)
- [ ] Module structure and IModule implementation
- [ ] Data models (SurveyLogEntry, PositionFix, DvrRecording, etc.)
- [ ] Basic UI with tabs
- [ ] Settings dialog

### Phase 2: Data Acquisition (Week 3-4)
- [ ] NaviPac TCP client
- [ ] .npc file parser and monitor
- [ ] .wp2 file parser and monitor
- [ ] DVR folder monitor and parser

### Phase 3: Survey Log Tab (Week 5-6)
- [ ] DataGrid with filtering
- [ ] Manual entry support
- [ ] Real-time updates
- [ ] Sub-tabs (DVR Reg, Pos Fixes, EIVA Data)

### Phase 4: DPR Tab (Week 7-8)
- [ ] DPR form layout
- [ ] Auto-population from project info
- [ ] Crew management grid
- [ ] Equipment tracking grids

### Phase 5: Export (Week 9-10)
- [ ] Excel export (ClosedXML)
- [ ] PDF export (QuestPDF)
- [ ] Word export (docx)
- [ ] .slog format implementation

### Phase 6: Polish (Week 11-12)
- [ ] Error handling
- [ ] Performance optimization
- [ ] Testing
- [ ] Documentation

---

## 📝 Notes

### Auto-Population Logic
Project information in DPR is automatically populated from:
1. NaviPac connection data (when available)
2. Module settings/configuration
3. Previous session data
4. .slog file when loaded

### Network Requirements
- Must be on same network as NaviPac
- TCP port 4001 (default) must be accessible
- File shares for NaviPac data folder
- File shares for VisualWorks folder

### Supported File Types
- **NaviPac**: .npc, .wp2, .npd
- **VisualWorks**: .wmv, .mpg, .mp4, .mpeg, .m2t, .ts
- **Export**: .xlsx, .pdf, .docx, .slog, .slogz

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | Dec 2024 | Initial specification |

---

**END OF MODULE DOCUMENTATION**
