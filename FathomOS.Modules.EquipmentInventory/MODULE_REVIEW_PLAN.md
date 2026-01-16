# S7 Fathom Equipment & Inventory Module
# COMPREHENSIVE REVIEW PLAN

## Version: 1.0 | Created: January 2026

---

## EXECUTIVE SUMMARY

**Current State:**
- **Total Lines:** 45,082
- **C# Files:** 112
- **XAML Files:** 54
- **Dialogs:** 26
- **Views:** 26
- **Services:** 19
- **ViewModels:** 18

**Identified Issue Categories:**
1. Missing ViewModel Commands (64 commands referenced but not implemented)
2. Missing XAML Resources (several brushes/styles not defined)
3. Incomplete Code-Behind Handlers
4. Views with minimal implementation
5. TODO items remaining

---

## PHASE 1: CRITICAL BINDING FIXES
**Priority:** HIGH | **Estimated Time:** 4-6 hours

### 1.1 Missing Commands in MainViewModel

The following commands are referenced in XAML but do NOT exist in MainViewModel:

#### Equipment-Related (8 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `AddEquipmentCommand` | EquipmentEditorDialog | Implement or map to NewEquipmentCommand |
| `GenerateUniqueIdCommand` | EquipmentEditorDialog | Implement unique ID generation |
| `GenerateLabelCommand` | EquipmentEditorDialog | Implement label generation |
| `ViewLabelCommand` | EquipmentEditorDialog | Implement label preview |
| `SaveLabelCommand` | EquipmentEditorDialog | Implement label save |
| `PrintLabelCommand` | EquipmentEditorDialog | Implement label print |
| `AddPhotoCommand` | EquipmentEditorDialog | Implement photo upload |
| `RemoveEquipmentCommand` | ManifestWizard | Implement item removal |

#### Admin/User-Related (8 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `AddUserCommand` | AdminView | Implement user creation |
| `EditUserCommand` | AdminView | Implement user editing |
| `DeactivateUserCommand` | AdminView | Implement user deactivation |
| `UnlockUserCommand` | AdminView | Implement account unlock |
| `ResetPasswordCommand` | AdminView | Implement password reset |
| `SetPinCommand` | AdminView | Implement PIN setting |
| `AddRoleCommand` | AdminView | Implement role creation |
| `GeneratePasswordCommand` | UserEditorDialog | Implement password generation |

#### Location/Supplier-Related (6 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `AddLocationCommand` | LocationListView | Implement location creation |
| `EditLocationCommand` | LocationListView | Implement location editing |
| `DeleteLocationCommand` | LocationListView | Implement location deletion |
| `AddSupplierCommand` | SupplierListView | Implement supplier creation |
| `EditSupplierCommand` | SupplierListView | Implement supplier editing |
| `DeleteSupplierCommand` | SupplierListView | Implement supplier deletion |

#### Category-Related (2 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `AddCategoryCommand` | AdminView | Implement category creation |
| `AddCertificationCommand` | CertificationView | Implement certification addition |

#### Import/Export-Related (6 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `ImportCommand` | EquipmentListView | Map to ImportFromFileCommand |
| `ExportCommand` | Various | Map to appropriate export command |
| `ExportExcelCommand` | Reports | Implement Excel export |
| `ExportPdfCommand` | Reports | Implement PDF export |
| `DownloadTemplateCommand` | ImportDialog | Implement template download |
| `PreviewCommand` | ImportDialog | Implement import preview |

#### Manifest/Verification-Related (10 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `LoadShipmentCommand` | ShipmentVerification | Implement shipment loading |
| `ScanItemCommand` | ShipmentVerification | Implement QR scanning |
| `SearchExpectedItemCommand` | ShipmentVerification | Implement item search |
| `CompleteVerificationCommand` | ShipmentVerification | Implement verification complete |
| `AddUnregisteredItemCommand` | ShipmentVerification | Implement unregistered item add |
| `AddManualItemCommand` | ShipmentVerification | Implement manual item add |
| `MarkAllMissingCommand` | ShipmentVerification | Implement mark all missing |
| `RefreshPendingManifestsCommand` | ManifestManagement | Implement refresh |
| `SelectPendingManifestCommand` | ShipmentVerification | Implement manifest selection |
| `SaveProgressCommand` | ShipmentVerification | Implement progress save |

#### Settings/Backup-Related (8 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `SaveSettingsCommand` | SettingsView | Implement settings save |
| `ResetToDefaultsCommand` | SettingsView | Implement reset defaults |
| `TestConnectionCommand` | SettingsView | Implement API test |
| `CreateBackupCommand` | BackupRestoreView | Implement backup creation |
| `RestoreBackupCommand` | BackupRestoreView | Implement backup restore |
| `BrowseBackupLocationCommand` | BackupRestoreView | Implement folder browse |
| `DeleteBackupCommand` | BackupRestoreView | Implement backup deletion |
| `BrowseCommand` | Various | Implement file browser |

#### General Navigation (10 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `SaveCommand` | Multiple Dialogs | Implement save logic |
| `CancelCommand` | Multiple Dialogs | Implement cancel logic |
| `CloseCommand` | Multiple Dialogs | Implement close logic |
| `BackCommand` | Wizard dialogs | Implement back navigation |
| `NextCommand` | Wizard dialogs | Implement next navigation |
| `ClearCommand` | Various | Implement clear logic |
| `SearchCommand` | Various | Implement search |
| `SelectCommand` | Various | Implement selection |
| `RemoveCommand` | Various | Implement removal |
| `RefreshCommand` | Already exists | ✅ OK |

#### Maintenance-Related (2 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `NewMaintenanceRecordCommand` | MaintenanceView | Implement record creation |
| `CompleteMaintenanceCommand` | MaintenanceScheduleView | Implement completion |

#### Report/Print-Related (6 commands)
| Command | Used In | Action Required |
|---------|---------|-----------------|
| `PrintCommand` | ReportsView | Implement print |
| `RefreshPreviewCommand` | ReportBuilderView | Implement preview refresh |
| `SaveTemplateCommand` | ReportBuilderView | Implement template save |
| `AddFieldCommand` | ReportBuilderView | Implement field addition |
| `RemoveColumnCommand` | ReportBuilderView | Implement column removal |
| `RefreshPrintersCommand` | QrLabelPrintDialog | Implement printer refresh |
| `TestPrintCommand` | QrLabelPrintDialog | Implement test print |

---

## PHASE 2: MISSING XAML RESOURCES
**Priority:** HIGH | **Estimated Time:** 2-3 hours

### 2.1 Missing Brush Resources

| Missing Resource | Used In | Action Required |
|------------------|---------|-----------------|
| `InfoTextBrush` | AdminView | Add to DarkTheme/LightTheme |
| `WarningTextBrush` | AdminView | Add to DarkTheme/LightTheme |
| `HeaderBrush` | Multiple | Add to DarkTheme/LightTheme |

### 2.2 Missing Style Resources

| Missing Style | Used In | Action Required |
|---------------|---------|-----------------|
| `SearchTextBox` | AdminView | Add to themes or replace |
| `ModernInput` | LoginWindow | Add to themes |
| `ModernPasswordInput` | LoginWindow | Add to themes |
| `GradientButton` | LoginWindow | Add to themes |
| `StatCard` | DashboardView | Add to themes |
| `SmallSuccessButton` | AdminView | Add to themes |
| `SmallDangerButton` | AdminView | Add to themes |
| `FieldLabel` | DefectReportListView | Already exists (verify) |

---

## PHASE 3: VIEW IMPLEMENTATION GAPS
**Priority:** MEDIUM | **Estimated Time:** 6-8 hours

### 3.1 Views with Minimal Code-Behind (Need ViewModel Integration)

| View | Lines | Issue | Action Required |
|------|-------|-------|-----------------|
| BatchActionsToolbar.xaml.cs | 11 | No functionality | Already uses binding (OK) |
| DefectReportListView.xaml.cs | 11 | No handlers | Uses MainViewModel binding (OK) |
| ReportsView.xaml.cs | 13 | Minimal | Uses MainViewModel binding (OK) |
| EquipmentListView.xaml.cs | 14 | Minimal | Uses MainViewModel binding (OK) |
| ManifestListView.xaml.cs | 14 | Minimal | Uses MainViewModel binding (OK) |
| BackupRestoreView.xaml.cs | 17 | No handlers | Needs ViewModel |
| EquipmentHistoryView.xaml.cs | 17 | No handlers | Uses ViewModel (OK) |
| MaintenanceScheduleView.xaml.cs | 17 | No handlers | Uses ViewModel (OK) |
| ReportBuilderView.xaml.cs | 17 | No handlers | Uses ViewModel (OK) |

### 3.2 Missing ViewModels

| View | Has ViewModel | Action Required |
|------|---------------|-----------------|
| BackupRestoreView | ✅ BackupRestoreViewModel | Wire up properly |
| CertificationView | ❌ | Create or use MainViewModel |
| LocationListView | ❌ | Create or use MainViewModel |
| SupplierListView | ❌ | Create or use MainViewModel |
| DefectReportListView | ❌ | Uses MainViewModel (OK) |
| AdminView | ❌ | Has _viewModel but needs commands |

---

## PHASE 4: DIALOG IMPLEMENTATION REVIEW
**Priority:** MEDIUM | **Estimated Time:** 4-6 hours

### 4.1 Equipment Editor Dialog
**File:** Views/Dialogs/EquipmentEditorDialog.xaml

**Missing Commands:**
- `GenerateUniqueIdCommand` - Generate unique ID
- `GenerateLabelCommand` - Generate QR label
- `ViewLabelCommand` - Preview label
- `SaveLabelCommand` - Save label to file
- `PrintLabelCommand` - Print label
- `AddPhotoCommand` - Add photo
- `SaveCommand` - Save equipment
- `CancelCommand` - Cancel dialog

**Action:** Ensure EquipmentEditorViewModel implements all commands

### 4.2 Shipment Verification Dialog
**File:** Views/Dialogs/ShipmentVerificationDialog.xaml

**Missing Commands:**
- `LoadShipmentCommand`
- `ScanItemCommand`
- `SearchExpectedItemCommand`
- `CompleteVerificationCommand`
- `AddUnregisteredItemCommand`
- `AddManualItemCommand`
- `MarkAllMissingCommand`
- `MarkVerifiedCommand`
- `MarkDamagedCommand`
- `MarkMissingCommand`
- `SelectPendingManifestCommand`
- `SaveProgressCommand`

**Action:** Review ShipmentVerificationViewModel for completeness

### 4.3 Manifest Wizard Dialog
**File:** Views/Dialogs/ManifestWizardDialog.xaml

**Missing Commands:**
- `BackCommand`
- `NextCommand`
- `AddEquipmentCommand`
- `RemoveEquipmentCommand`
- `AddAllEquipmentCommand`
- `SearchCommand`
- `AddAllCommand`
- `SaveCommand`
- `CancelCommand`

**Action:** Review ManifestWizardViewModel for completeness

### 4.4 User Editor Dialog
**File:** Views/Dialogs/UserEditorDialog.xaml

**Missing Commands:**
- `GeneratePasswordCommand`
- `SaveCommand`
- `CancelCommand`

**Action:** Create UserEditorViewModel or add commands

### 4.5 Import Dialog
**File:** Views/Dialogs/ImportDialog.xaml

**Missing Commands:**
- `BrowseCommand`
- `PreviewCommand`
- `DownloadTemplateCommand`
- `ImportCommand`
- `CancelCommand`

**Action:** Review ImportViewModel for completeness

### 4.6 QR Label Print Dialog
**File:** Views/Dialogs/QrLabelPrintDialog.xaml

**Missing Commands:**
- `RefreshPrintersCommand`
- `TestPrintCommand`
- `PrintCommand`
- `CloseCommand`

**Action:** Review QrLabelPrintViewModel for completeness

---

## PHASE 5: SERVICE VERIFICATION
**Priority:** LOW | **Estimated Time:** 2-3 hours

### 5.1 Services to Verify

| Service | Status | Notes |
|---------|--------|-------|
| AuthenticationService | ✅ | Appears complete |
| BackupService | ⚠️ | Needs UI wiring |
| BatchOperationsService | ✅ | Just wired in Phase 3 |
| DashboardService | ✅ | Used by DashboardView |
| EquipmentTemplateService | ✅ | Just wired in Phase 3 |
| FavoritesService | ✅ | Just wired in Phase 3 |
| QRCodeService | ⚠️ | Need to verify label generation |
| ReportBuilderService | ⚠️ | Need to verify commands |
| SyncService | ⚠️ | Need to test with API |
| ThemeService | ✅ | Working |
| MaintenanceSchedulingService | ⚠️ | Need to verify UI integration |

---

## PHASE 6: THEME & STYLE COMPLETION
**Priority:** LOW | **Estimated Time:** 2 hours

### 6.1 DarkTheme.xaml Additions Needed

```xml
<!-- Missing Resources to Add -->
<SolidColorBrush x:Key="InfoTextBrush" Color="#90CAF9"/>
<SolidColorBrush x:Key="WarningTextBrush" Color="#FFB74D"/>
<SolidColorBrush x:Key="HeaderBrush" Color="#1A1D21"/>

<!-- Missing Styles -->
<Style x:Key="SearchTextBox" TargetType="TextBox" BasedOn="{StaticResource MahApps.Styles.TextBox}">
    <!-- Search box styling -->
</Style>

<Style x:Key="StatCard" TargetType="Border">
    <!-- Dashboard stat card styling -->
</Style>

<Style x:Key="SmallSuccessButton" TargetType="Button" BasedOn="{StaticResource SecondaryButton}">
    <Setter Property="Background" Value="{DynamicResource SuccessBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="10,5"/>
</Style>

<Style x:Key="SmallDangerButton" TargetType="Button" BasedOn="{StaticResource SecondaryButton}">
    <Setter Property="Background" Value="{DynamicResource ErrorBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="10,5"/>
</Style>
```

### 6.2 LightTheme.xaml Additions Needed
Same resources with light theme colors.

---

## PHASE 7: TESTING & VALIDATION
**Priority:** HIGH | **Estimated Time:** 4-6 hours

### 7.1 Navigation Testing

| Screen | Test | Status |
|--------|------|--------|
| Dashboard | Load stats | ⬜ |
| Equipment | List/Search/Filter | ⬜ |
| Equipment | Add/Edit/Delete | ⬜ |
| Equipment | Batch operations | ⬜ |
| Equipment | Templates | ⬜ |
| Equipment | Favorites | ⬜ |
| Manifests | List/Create | ⬜ |
| Manifests | Workflow | ⬜ |
| Manifests | Verification | ⬜ |
| Locations | CRUD | ⬜ |
| Suppliers | CRUD | ⬜ |
| Maintenance | Schedule/Records | ⬜ |
| Defects | CRUD/Workflow | ⬜ |
| Certifications | List/Add | ⬜ |
| Reports | Standard/Custom | ⬜ |
| Unregistered | Review/Convert | ⬜ |
| Notifications | View/Resolve | ⬜ |
| Admin | Users/Roles | ⬜ |
| Settings | All sections | ⬜ |

### 7.2 Dialog Testing

| Dialog | Test | Status |
|--------|------|--------|
| EquipmentEditorDialog | All fields save | ⬜ |
| ManifestWizardDialog | Full workflow | ⬜ |
| ShipmentVerificationDialog | Scan/Verify | ⬜ |
| DefectReportEditorDialog | Full form | ⬜ |
| UserEditorDialog | Create/Edit | ⬜ |
| LocationEditorDialog | Create/Edit | ⬜ |
| All others | Basic function | ⬜ |

### 7.3 Export Testing

| Export | Test | Status |
|--------|------|--------|
| Equipment Excel | All data | ⬜ |
| Equipment PDF | Formatting | ⬜ |
| Manifest PDF | Formatting | ⬜ |
| QR Labels PDF | Generation | ⬜ |
| Custom Reports | Builder | ⬜ |

---

## IMPLEMENTATION ORDER

### Week 1: Critical Fixes
1. **Day 1-2:** Phase 1.1 - Equipment-related commands
2. **Day 2-3:** Phase 1.1 - Admin/User commands
3. **Day 3-4:** Phase 2 - Missing XAML resources
4. **Day 4-5:** Phase 4.1-4.3 - Critical dialog fixes

### Week 2: Feature Completion
1. **Day 1-2:** Phase 1.1 - Remaining commands
2. **Day 2-3:** Phase 3 - View implementation gaps
3. **Day 3-4:** Phase 4.4-4.6 - Remaining dialogs
4. **Day 4-5:** Phase 5 - Service verification

### Week 3: Testing & Polish
1. **Day 1-2:** Phase 6 - Theme completion
2. **Day 2-4:** Phase 7 - Full testing
3. **Day 4-5:** Bug fixes and polish

---

## FILES REQUIRING IMMEDIATE ATTENTION

### Critical Priority (Breaking Issues)
1. `ViewModels/MainViewModel.cs` - Add missing commands
2. `Themes/DarkTheme.xaml` - Add missing resources
3. `Themes/LightTheme.xaml` - Add missing resources
4. `ViewModels/Dialogs/EquipmentEditorViewModel.cs` - Missing commands
5. `ViewModels/Dialogs/ShipmentVerificationViewModel.cs` - Missing commands

### High Priority (Incomplete Features)
1. `Views/AdminView.xaml.cs` - User management wiring
2. `Views/BackupRestoreView.xaml.cs` - Backup wiring
3. `Views/Dialogs/UserEditorDialog.xaml.cs` - ViewModel connection
4. `Views/SettingsView.xaml.cs` - Settings persistence

### Medium Priority (Enhancement)
1. `Views/LocationListView.xaml.cs` - CRUD operations
2. `Views/SupplierListView.xaml.cs` - CRUD operations
3. `Views/CertificationView.xaml.cs` - Certification management
4. `Views/MaintenanceView.xaml.cs` - Maintenance operations

---

## METRICS TO TRACK

| Metric | Current | Target |
|--------|---------|--------|
| Missing Commands | 64 | 0 |
| Missing Resources | 8 | 0 |
| Views <30 lines | 9 | 0* |
| TODO/FIXME items | 1 | 0 |
| Untested screens | 14 | 0 |

*Some views are OK with minimal code-behind if using ViewModel binding

---

## NEXT STEPS

1. Review this plan and confirm priorities
2. Begin Phase 1 implementation
3. Track progress against checklist
4. Update documentation as fixes are made

---

**Document Version:** 1.0
**Last Updated:** January 2026
**Author:** Claude (AI Assistant)
