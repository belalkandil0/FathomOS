# MODULE REVIEW IMPLEMENTATION CHECKLIST

## Quick Reference for Bug Fixing Sessions

---

## PHASE 1A: EQUIPMENT EDITOR DIALOG COMMANDS
**File:** `ViewModels/Dialogs/EquipmentEditorViewModel.cs`

### Commands to Implement:
- [ ] `GenerateUniqueIdCommand` - Generate unique ID based on org/category
- [ ] `GenerateLabelCommand` - Create QR code image
- [ ] `ViewLabelCommand` - Show label preview dialog
- [ ] `SaveLabelCommand` - Save QR label to PNG/PDF
- [ ] `PrintLabelCommand` - Send to printer
- [ ] `AddPhotoCommand` - Open file dialog, add to Photos collection
- [ ] `SaveCommand` - Validate and save equipment
- [ ] `CancelCommand` - Close dialog without saving

### Properties to Verify:
- [ ] `UniqueIdGenerated` - Flag for UI state
- [ ] `LabelGenerated` - Flag for UI state
- [ ] `Photos` - ObservableCollection<string>

---

## PHASE 1B: SHIPMENT VERIFICATION DIALOG COMMANDS
**File:** `ViewModels/Dialogs/ShipmentVerificationViewModel.cs`

### Commands to Implement:
- [ ] `LoadShipmentCommand` - Load selected manifest items
- [ ] `ScanItemCommand` - Process QR scan input
- [ ] `SearchExpectedItemCommand` - Open search dialog
- [ ] `CompleteVerificationCommand` - Finalize verification
- [ ] `AddUnregisteredItemCommand` - Add new unregistered item
- [ ] `AddManualItemCommand` - Manual item entry
- [ ] `MarkAllMissingCommand` - Mark remaining as missing
- [ ] `MarkVerifiedCommand` - Mark single item verified
- [ ] `MarkDamagedCommand` - Mark single item damaged
- [ ] `MarkMissingCommand` - Mark single item missing
- [ ] `SelectPendingManifestCommand` - Select manifest from list
- [ ] `SaveProgressCommand` - Save current progress

---

## PHASE 1C: MANIFEST WIZARD DIALOG COMMANDS
**File:** `ViewModels/Dialogs/ManifestWizardViewModel.cs`

### Commands to Implement:
- [ ] `BackCommand` - Navigate to previous step
- [ ] `NextCommand` - Navigate to next step
- [ ] `AddEquipmentCommand` - Add single item to manifest
- [ ] `RemoveEquipmentCommand` - Remove item from manifest
- [ ] `AddAllEquipmentCommand` - Add all filtered equipment
- [ ] `SearchCommand` - Filter available equipment
- [ ] `AddAllCommand` - Add all visible to selection
- [ ] `SaveCommand` - Create manifest
- [ ] `CancelCommand` - Close without saving

---

## PHASE 1D: ADMIN/USER COMMANDS
**File:** `ViewModels/MainViewModel.cs` (or separate AdminViewModel)

### User Management:
- [ ] `AddUserCommand` - Open UserEditorDialog in create mode
- [ ] `EditUserCommand` - Open UserEditorDialog in edit mode
- [ ] `DeactivateUserCommand` - Set user IsActive = false
- [ ] `UnlockUserCommand` - Reset LockedOut status
- [ ] `ResetPasswordCommand` - Generate temp password
- [ ] `SetPinCommand` - Open PIN setup dialog
- [ ] `AddRoleCommand` - Open role creation dialog

### Location Management:
- [ ] `AddLocationCommand` - Open LocationEditorDialog
- [ ] `EditLocationCommand` - Open LocationEditorDialog
- [ ] `DeleteLocationCommand` - Confirm and delete

### Supplier Management:
- [ ] `AddSupplierCommand` - Open SupplierEditorDialog
- [ ] `EditSupplierCommand` - Open SupplierEditorDialog
- [ ] `DeleteSupplierCommand` - Confirm and delete

### Category Management:
- [ ] `AddCategoryCommand` - Add new category

---

## PHASE 1E: SETTINGS/BACKUP COMMANDS
**File:** `ViewModels/SettingsViewModel.cs` or `BackupRestoreViewModel.cs`

### Settings:
- [ ] `SaveSettingsCommand` - Persist settings to ModuleSettings
- [ ] `ResetToDefaultsCommand` - Reset all settings
- [ ] `TestConnectionCommand` - Test API endpoint

### Backup:
- [ ] `CreateBackupCommand` - Create database backup
- [ ] `RestoreBackupCommand` - Restore from backup
- [ ] `BrowseBackupLocationCommand` - Folder browser dialog
- [ ] `DeleteBackupCommand` - Delete selected backup

---

## PHASE 1F: IMPORT/EXPORT COMMANDS
**File:** `ViewModels/Dialogs/ImportViewModel.cs`

- [ ] `BrowseCommand` - Open file dialog
- [ ] `PreviewCommand` - Parse and preview data
- [ ] `DownloadTemplateCommand` - Save template Excel
- [ ] `ImportCommand` - Execute import
- [ ] `CancelCommand` - Close dialog

---

## PHASE 1G: QR LABEL PRINT COMMANDS
**File:** `ViewModels/Dialogs/QrLabelPrintViewModel.cs`

- [ ] `RefreshPrintersCommand` - Enumerate printers
- [ ] `TestPrintCommand` - Print test label
- [ ] `PrintCommand` - Print all labels
- [ ] `CloseCommand` - Close dialog

---

## PHASE 1H: REPORT BUILDER COMMANDS
**File:** `ViewModels/ReportBuilderViewModel.cs`

- [ ] `AddFieldCommand` - Add field to columns
- [ ] `RemoveColumnCommand` - Remove from columns
- [ ] `RefreshPreviewCommand` - Regenerate preview
- [ ] `SaveTemplateCommand` - Save report definition
- [ ] `ExportExcelCommand` - Export to Excel
- [ ] `ExportPdfCommand` - Export to PDF
- [ ] `PrintCommand` - Print report

---

## PHASE 1I: MAINTENANCE COMMANDS
**File:** `ViewModels/MaintenanceScheduleViewModel.cs`

- [ ] `NewMaintenanceRecordCommand` - Open record dialog
- [ ] `CompleteMaintenanceCommand` - Mark complete
- [ ] `ViewEquipmentCommand` - Navigate to equipment

---

## PHASE 2: THEME RESOURCES

### Add to DarkTheme.xaml:
```xml
<!-- Missing Brushes -->
<SolidColorBrush x:Key="InfoTextBrush" Color="#90CAF9"/>
<SolidColorBrush x:Key="WarningTextBrush" Color="#FFB74D"/>
<SolidColorBrush x:Key="HeaderBrush" Color="#1A1D21"/>

<!-- Missing Styles -->
<Style x:Key="SearchTextBox" TargetType="TextBox" BasedOn="{StaticResource MahApps.Styles.TextBox}">
    <Setter Property="mah:TextBoxHelper.Watermark" Value="Search..."/>
    <Setter Property="mah:TextBoxHelper.ClearTextButton" Value="True"/>
</Style>

<Style x:Key="StatCard" TargetType="Border">
    <Setter Property="Background" Value="{DynamicResource CardBrush}"/>
    <Setter Property="CornerRadius" Value="12"/>
    <Setter Property="Padding" Value="20"/>
</Style>

<Style x:Key="SmallSuccessButton" TargetType="Button" BasedOn="{StaticResource SecondaryButton}">
    <Setter Property="Background" Value="{DynamicResource SuccessBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="10,5"/>
    <Setter Property="FontSize" Value="11"/>
</Style>

<Style x:Key="SmallDangerButton" TargetType="Button" BasedOn="{StaticResource SecondaryButton}">
    <Setter Property="Background" Value="{DynamicResource ErrorBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="10,5"/>
    <Setter Property="FontSize" Value="11"/>
</Style>

<Style x:Key="ModernInput" TargetType="TextBox" BasedOn="{StaticResource MahApps.Styles.TextBox}">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Padding" Value="12,10"/>
</Style>

<Style x:Key="ModernPasswordInput" TargetType="PasswordBox" BasedOn="{StaticResource MahApps.Styles.PasswordBox}">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Padding" Value="12,10"/>
</Style>

<Style x:Key="GradientButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="Bold"/>
    <Setter Property="Padding" Value="30,12"/>
</Style>
```

### Add same to LightTheme.xaml with appropriate colors

---

## PHASE 3: VIEW WIRING CHECKLIST

### AdminView.xaml.cs
- [ ] Wire up user list refresh
- [ ] Wire up role management
- [ ] Wire up category management
- [ ] Wire up unregistered items actions

### LocationListView.xaml.cs
- [ ] Load locations on init
- [ ] Implement add/edit/delete
- [ ] Wire up selection

### SupplierListView.xaml.cs
- [ ] Load suppliers on init
- [ ] Implement add/edit/delete
- [ ] Wire up selection

### CertificationView.xaml.cs
- [ ] Load certifications on init
- [ ] Implement add certification
- [ ] Implement expiry filters

### BackupRestoreView.xaml.cs
- [ ] Wire up BackupRestoreViewModel
- [ ] Load backup list
- [ ] Implement create/restore/delete

---

## QUICK FIX PATTERNS

### Pattern 1: Add Simple Command to ViewModel
```csharp
// In constructor
AddItemCommand = new RelayCommand(_ => AddItem(), _ => CanAddItem);

// Method
private void AddItem()
{
    // Implementation
}

private bool CanAddItem => /* condition */;
```

### Pattern 2: Add Async Command
```csharp
// In constructor
SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => CanSave);

// Method
private async Task SaveAsync()
{
    IsBusy = true;
    try { /* implementation */ }
    finally { IsBusy = false; }
}
```

### Pattern 3: Wire View to ViewModel
```csharp
public partial class MyView : UserControl
{
    private readonly MyViewModel _viewModel;
    
    public MyView(LocalDatabaseService dbService)
    {
        _viewModel = new MyViewModel(dbService);
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += async (s, e) => await _viewModel.LoadAsync();
    }
}
```

### Pattern 4: Dialog Command Pattern
```csharp
SaveCommand = new RelayCommand(_ =>
{
    if (Validate())
    {
        DialogResult = true;
        CloseAction?.Invoke();
    }
});

CancelCommand = new RelayCommand(_ =>
{
    DialogResult = false;
    CloseAction?.Invoke();
});
```

---

## TESTING COMMANDS

### Build Test
```bash
dotnet build FathomOS.Modules.EquipmentInventory.csproj
```

### Check for Binding Errors at Runtime
1. Run application
2. Open Output window in VS
3. Filter for "BindingExpression"
4. Each binding error indicates missing property/command

### Verify XAML Resources
1. Open each theme file
2. Search for referenced resource keys
3. Ensure all StaticResource/DynamicResource keys exist

---

## PROGRESS TRACKING

| Phase | Items | Complete | Remaining |
|-------|-------|----------|-----------|
| 1A Equipment Editor | 8 | 0 | 8 |
| 1B Shipment Verify | 12 | 0 | 12 |
| 1C Manifest Wizard | 9 | 0 | 9 |
| 1D Admin/User | 10 | 0 | 10 |
| 1E Settings/Backup | 7 | 0 | 7 |
| 1F Import/Export | 5 | 0 | 5 |
| 1G QR Labels | 4 | 0 | 4 |
| 1H Report Builder | 7 | 0 | 7 |
| 1I Maintenance | 3 | 0 | 3 |
| 2 Theme Resources | 10 | 0 | 10 |
| 3 View Wiring | 5 | 0 | 5 |
| **TOTAL** | **80** | **0** | **80** |

---

**Last Updated:** January 2026
