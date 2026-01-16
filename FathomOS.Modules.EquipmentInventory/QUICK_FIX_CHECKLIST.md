# QUICK-FIX CHECKLIST
## Equipment Inventory Module - Bug Fix Tracker

---

## STATUS LEGEND
- ⬜ Not Started
- 🔄 In Progress  
- ✅ Complete
- ❌ Blocked

---

# PHASE 1: CRITICAL FIXES (Must Fix)

## 1.1 Missing Commands in MainViewModel.cs

### User Management Commands
| Status | Command | Implementation |
|--------|---------|----------------|
| ⬜ | `AddUserCommand` | Open UserEditorDialog in create mode |
| ⬜ | `EditUserCommand` | Open UserEditorDialog with selected user |
| ⬜ | `DeactivateUserCommand` | Set user.IsActive = false |
| ⬜ | `UnlockUserCommand` | Reset user.LockedOut |
| ⬜ | `ResetPasswordCommand` | Generate temp password |
| ⬜ | `SetPinCommand` | Open PIN dialog |
| ⬜ | `AddRoleCommand` | Open role dialog |

### Location Commands
| Status | Command | Implementation |
|--------|---------|----------------|
| ⬜ | `AddLocationCommand` | Open LocationEditorDialog |
| ⬜ | `EditLocationCommand` | Open LocationEditorDialog with data |
| ⬜ | `DeleteLocationCommand` | Confirm + delete |

### Supplier Commands
| Status | Command | Implementation |
|--------|---------|----------------|
| ⬜ | `AddSupplierCommand` | Open SupplierEditorDialog |
| ⬜ | `EditSupplierCommand` | Open SupplierEditorDialog with data |
| ⬜ | `DeleteSupplierCommand` | Confirm + delete |

### Other Commands
| Status | Command | Implementation |
|--------|---------|----------------|
| ⬜ | `AddCategoryCommand` | Open category dialog |
| ⬜ | `AddCertificationCommand` | Open CertificationDialog |
| ⬜ | `NewMaintenanceRecordCommand` | Open MaintenanceRecordDialog |
| ⬜ | `SaveSettingsCommand` | Save ModuleSettings |
| ⬜ | `BrowseBackupLocationCommand` | Open folder browser |
| ⬜ | `DismissInfoCommand` | Hide info panel |
| ⬜ | `GeneratePasswordCommand` | Generate random password |

**Subtotal:** 0/20 Complete

---

## 1.2 Add Command Initializations

Add to `InitializeCommands()` in MainViewModel.cs:

```csharp
// User Management
AddUserCommand = new RelayCommand(_ => AddUser());
EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null);
DeactivateUserCommand = new RelayCommand(async _ => await DeactivateUserAsync(), _ => SelectedUser != null);
UnlockUserCommand = new RelayCommand(async _ => await UnlockUserAsync(), _ => SelectedUser?.LockedOut == true);
ResetPasswordCommand = new RelayCommand(async _ => await ResetPasswordAsync(), _ => SelectedUser != null);
SetPinCommand = new RelayCommand(_ => SetPin(), _ => SelectedUser != null);
AddRoleCommand = new RelayCommand(_ => AddRole());

// Location Management  
AddLocationCommand = new RelayCommand(_ => AddLocation());
EditLocationCommand = new RelayCommand(_ => EditLocation(), _ => SelectedLocation != null);
DeleteLocationCommand = new RelayCommand(async _ => await DeleteLocationAsync(), _ => SelectedLocation != null);

// Supplier Management
AddSupplierCommand = new RelayCommand(_ => AddSupplier());
EditSupplierCommand = new RelayCommand(_ => EditSupplier(), _ => SelectedSupplier != null);
DeleteSupplierCommand = new RelayCommand(async _ => await DeleteSupplierAsync(), _ => SelectedSupplier != null);

// Other
AddCategoryCommand = new RelayCommand(_ => AddCategory());
AddCertificationCommand = new RelayCommand(_ => AddCertification());
NewMaintenanceRecordCommand = new RelayCommand(_ => AddMaintenanceRecord());
SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
BrowseBackupLocationCommand = new RelayCommand(_ => BrowseBackupLocation());
DismissInfoCommand = new RelayCommand(_ => ShowInfoPanel = false);
```

---

# PHASE 2: HIGH PRIORITY (Theme Resources)

## 2.1 Add Converter Instances to DarkTheme.xaml

| Status | Converter Key | Class |
|--------|---------------|-------|
| ⬜ | `BoolToVisibility` | BoolToVisibilityConverter |
| ⬜ | `InverseBoolToVisibility` | InverseBoolToVisibilityConverter |
| ⬜ | `InverseBoolConverter` | InverseBoolConverter |
| ⬜ | `NullToVisibility` | NullToVisibilityConverter |
| ⬜ | `InverseNullToVisibility` | InverseNullToVisibilityConverter |
| ⬜ | `IntToVisibility` | IntToVisibilityConverter |
| ⬜ | `StatusToColor` | StatusToColorConverter |
| ⬜ | `ManifestStatusToColor` | ManifestStatusToColorConverter |
| ⬜ | `NotificationTypeToColor` | NotificationTypeToColorConverter |
| ⬜ | `NotificationTypeToIcon` | NotificationTypeToIconConverter |
| ⬜ | `StringToBrush` | StringToSolidColorBrushConverter |
| ⬜ | `BytesToImageConverter` | BytesToImageConverter |

**Subtotal:** 0/12 Complete

## 2.2 Add Missing Styles to DarkTheme.xaml

| Status | Style Key | Target Type |
|--------|-----------|-------------|
| ⬜ | `NavButton` | RadioButton |
| ⬜ | `StatCard` | Border |
| ⬜ | `ModernTextBox` | TextBox |
| ⬜ | `ModernInput` | TextBox |
| ⬜ | `ModernPasswordInput` | PasswordBox |
| ⬜ | `PinButton` | Button |
| ⬜ | `GradientButton` | Button |
| ⬜ | `ActionButton` | Button |
| ⬜ | `IconButton` | Button |
| ⬜ | `FilterButton` | Button |
| ⬜ | `SearchTextBox` | TextBox |
| ⬜ | `ItemCard` | Border |
| ⬜ | `ManifestCard` | Border |
| ⬜ | `NotificationCard` | Border |
| ⬜ | `ManifestTabHeader` | TextBlock |
| ⬜ | `HelpNavButton` | RadioButton |
| ⬜ | `PrimaryActionButton` | Button |

**Subtotal:** 0/17 Complete

## 2.3 Create Missing Converters

| Status | Converter | File |
|--------|-----------|------|
| ⬜ | `WizardStepDotConverter` | Converters/Converters.cs |
| ⬜ | `WizardStepLineConverter` | Converters/Converters.cs |

**Subtotal:** 0/2 Complete

## 2.4 Apply to LightTheme.xaml

| Status | Task |
|--------|------|
| ⬜ | Copy all converter instances |
| ⬜ | Copy all styles (adjust colors) |

**Subtotal:** 0/2 Complete

---

# PHASE 3: MEDIUM PRIORITY (Code Quality)

## 3.1 Fix Null Reference Risks

| Status | File | Line | Fix |
|--------|------|------|-----|
| ⬜ | ExcelImportService.cs | 29 | Add null check for First() |
| ⬜ | CreateFromTemplateDialog.cs | 108 | Add null check |

**Subtotal:** 0/2 Complete

## 3.2 Add Exception Handling

| Status | File | Methods Needing Try/Catch |
|--------|------|--------------------------|
| ⬜ | ApiClient.cs | All public async methods |
| ⬜ | EquipmentTemplateService.cs | All async methods |
| ⬜ | ReportBuilderService.cs | All async methods |

**Subtotal:** 0/3 Complete

## 3.3 Fix Empty Catch Blocks

| Status | File | Line |
|--------|------|------|
| ⬜ | ApiClient.cs | 146 |
| ⬜ | LabelPrintService.cs | 292, 298 |
| ⬜ | ReportBuilderService.cs | 321 |
| ⬜ | QRCodeService.cs | 221 |
| ⬜ | DocumentViewerService.cs | 311 |
| ⬜ | AuthenticationService.cs | 88 |

**Subtotal:** 0/6 Complete

## 3.4 Fix Memory Leaks

| Status | File | Issue |
|--------|------|-------|
| ⬜ | MainWindow.xaml.cs | Unsubscribe events on close |
| ⬜ | NotificationsView.xaml.cs | Use WeakEventManager |
| ⬜ | UnregisteredItemsView.xaml.cs | Use WeakEventManager |
| ⬜ | ManifestManagementView.xaml.cs | Use WeakEventManager |
| ⬜ | LocationEditorDialog.xaml.cs | Unsubscribe on close |

**Subtotal:** 0/5 Complete

---

# PHASE 4: LOW PRIORITY (Polish)

## 4.1 Fix Async Void Methods

| Status | File | Method |
|--------|------|--------|
| ⬜ | AdminView.xaml.cs | LoadData() |
| ⬜ | AdminView.xaml.cs | LoadUnregisteredItems() |
| ⬜ | AdminView.xaml.cs | ConvertToEquipment() |
| ⬜ | AdminView.xaml.cs | KeepAsConsumable() |
| ⬜ | AdminView.xaml.cs | RejectItem() |
| ⬜ | AdminView.xaml.cs | ResetPassword() |
| ⬜ | AdminView.xaml.cs | SetPin() |
| ⬜ | AdminView.xaml.cs | UnlockUser() |
| ⬜ | AdminView.xaml.cs | DeactivateUser() |
| ⬜ | LocationEditorDialog.xaml.cs | Save() |

**Subtotal:** 0/10 Complete

---

# OVERALL PROGRESS

| Phase | Items | Complete | Percentage |
|-------|-------|----------|------------|
| Phase 1: Commands | 20 | 0 | 0% |
| Phase 2: Resources | 33 | 0 | 0% |
| Phase 3: Code Quality | 16 | 0 | 0% |
| Phase 4: Polish | 10 | 0 | 0% |
| **TOTAL** | **79** | **0** | **0%** |

---

# NOTES

## Testing After Each Fix
After fixing each item:
1. Build the project: `dotnet build`
2. Run the application
3. Navigate to affected view
4. Test the specific functionality
5. Check Output window for binding errors

## Common Binding Error Pattern
If you see: `System.Windows.Data Error: 40`
This means a binding path is wrong or property doesn't exist.

## Quick Validation
```bash
# Check for any remaining binding issues
grep -rn "Command=\"{Binding [A-Z]" Views/ | \
  sed 's/.*Binding \([^}]*\).*/\1/' | \
  sort -u | wc -l
```

---

**Last Updated:** January 2026
