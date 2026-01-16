# Survey Electronic Logbook - Version 11.0.0 Changelog

## Release Date: January 2025

## Overview

Version 11.0.0 marks a major milestone with the transition from Fathom OS to Fathom OS branding, along with full certificate system integration for professional verification documentation.

---

## 🔄 BREAKING CHANGES

### Namespace Migration: FathomOS → FathomOS

All namespaces, assemblies, and references have been updated:

| Component | Old Name | New Name |
|-----------|----------|----------|
| Root Namespace | `FathomOS.Modules.SurveyLogbook` | `FathomOS.Modules.SurveyLogbook` |
| Assembly | `FathomOS.Modules.SurveyLogbook.dll` | `FathomOS.Modules.SurveyLogbook.dll` |
| Core Reference | `FathomOS.Core` | `FathomOS.Core` |
| Data Folder | `%APPDATA%/FathomOS/SurveyLogbook` | `%APPDATA%/FathomOS/SurveyLogbook` |
| Firewall Rules | `FathomOS_SurveyLogbook_*` | `FathomOS_SurveyLogbook_*` |
| File Format Type | `FathomOS.SurveyLog` | `FathomOS.SurveyLog` |

### Migration Notes

- Existing `.slog` files remain compatible (format unchanged)
- Settings files will need to be migrated manually or recreated
- Firewall rules will need to be recreated with new naming convention
- Templates stored in old path will need to be copied to new location

---

## ✨ NEW FEATURES

### Certificate System Integration

Added full support for Fathom OS Certificate System:

**ModuleInfo.json Updates:**
```json
{
    "CertificateCode": "SL",
    "CertificateTitle": "Survey Electronic Logbook Processing Verification Certificate",
    "CertificateStatement": "This is to certify that the survey data logging, DVR monitoring, position fix capture, and Daily Progress Report documentation has been successfully completed..."
}
```

**Certificate Data Fields:**
- Total Log Entries
- Event Categories (DVR, Position Fix, Manual, NaviPac)
- Date Range Covered
- Position Fixes Logged
- DVR Recordings Tracked
- DPR Reports Generated
- Export Formats Used

### Branding Service Support

- Window titles now use `BrandingService.EditionName`
- Export headers include company branding
- PDF reports display company logo and name
- About dialog shows license information

---

## 🐛 BUG FIXES

### From v10.0.0 (Integrated)

1. **CS0200 - Read-only Command Properties**
   - Fixed 18 command properties in `FieldConfigurationViewModel`
   - Changed from `{ get; }` to `{ get; private set; } = null!;`

2. **CS0104 - MessageBox Ambiguity**
   - Fully qualified all `MessageBox.Show()` calls with `System.Windows.MessageBox.Show()`
   - Fixed in FieldConfigurationViewModel and DataMonitorViewModel

3. **CS0117 - CreateDefaultSet Method**
   - Verified correct usage of `UserFieldDefinition.CreateDefaultFields()`

4. **CS0411 - Generic Type Inference**
   - Fixed `GetDynamicField<T>` calls in NaviPacDataParser
   - Used direct dictionary access instead of generic method

---

## 📁 FILE CHANGES

### Renamed Files
- `FathomOS.Modules.SurveyLogbook.csproj` → `FathomOS.Modules.SurveyLogbook.csproj`

### Updated Files (Namespace/Branding)
All `.cs`, `.xaml`, and configuration files updated with new namespaces:
- 10 ViewModels
- 12 Views (XAML + code-behind)
- 7 Services
- 11 Models
- 4 Parsers
- 2 Exporters
- 2 Themes
- 1 Converters file
- ModuleInfo.json
- SurveyLogbookModule.cs

### String Replacements
| Pattern | Replacement | Count |
|---------|-------------|-------|
| `FathomOS.Modules.SurveyLogbook` | `FathomOS.Modules.SurveyLogbook` | ~150 |
| `FathomOS.Core` | `FathomOS.Core` | ~30 |
| `Fathom OS` (display) | `Fathom OS` | ~50 |
| `FathomOS` (paths) | `FathomOS` | ~20 |

---

## 📋 CERTIFICATE SYSTEM DETAILS

### Certificate Code: SL

### Processing Data Dictionary
```csharp
var processingData = new Dictionary<string, string>
{
    ["Total Log Entries"] = "1,234",
    ["NaviPac Events"] = "856",
    ["Position Fixes"] = "124",
    ["DVR Recordings"] = "67",
    ["Manual Entries"] = "187",
    ["Date Range"] = "01 Jan 2025 — 10 Jan 2025",
    ["DPR Reports"] = "10",
    ["Shift Handovers"] = "20",
    ["Export Formats"] = "Excel, PDF, .slog"
};
```

### Signatory Titles (Dropdown)
- Survey Supervisor
- Senior Survey Engineer
- Survey Engineer
- Processing Engineer
- Operations Manager
- Project Manager
- Data Processing Specialist
- Field Engineer

---

## 🔧 TECHNICAL CHANGES

### Assembly Information
```xml
<Version>11.0.0</Version>
<Authors>Fathom OS Team</Authors>
<Company>Fathom OS</Company>
<Product>Fathom OS Survey Electronic Logbook</Product>
<Copyright>Copyright © 2024-2025 Fathom OS</Copyright>
```

### Dependencies
- FathomOS.Core (updated from FathomOS.Core)
- .NET 8.0 Windows
- MahApps.Metro 2.4.10
- ClosedXML 0.102.1
- QuestPDF 2024.3.0

---

## 📖 MIGRATION GUIDE

### For End Users

1. **Settings Migration**
   ```
   Copy from: %APPDATA%/FathomOS/SurveyLogbook/
   Copy to:   %APPDATA%/FathomOS/SurveyLogbook/
   ```

2. **Templates Migration**
   ```
   Copy from: %APPDATA%/FathomOS/SurveyLogbook/Templates/
   Copy to:   %APPDATA%/FathomOS/SurveyLogbook/Templates/
   ```

3. **Firewall Rules**
   - Remove old `FathomOS_SurveyLogbook_*` rules
   - Create new rules automatically via Settings

### For Developers

1. Update all `using FathomOS.*` to `using FathomOS.*`
2. Update project references
3. Rebuild solution

---

## 🔮 FUTURE ROADMAP

### v11.1.0 (Planned)
- Full BrandingService integration
- Certificate generation UI
- Signatory dialog implementation

### v12.0.0 (Planned)
- Multi-stream NaviPac support
- Enhanced DPR templates
- Cloud backup integration

---

## 📝 NOTES

- This version requires Fathom OS Shell v2.0.0 or later
- Backwards compatible with .slog files from v7.0.0+
- Windows 10/11 required for full functionality

---

**Full Changelog History:**
- v11.0.0 - Fathom OS transition, certificate system
- v10.0.0 - Bug fixes (compilation errors)
- v9.0.0 - Dynamic field configuration
- v8.0.0 - NaviPac multi-protocol support
- v7.7.1 - Position fix enhancements
- v7.7.0 - DPR improvements
- v7.6.1 - Export fixes
