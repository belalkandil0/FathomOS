# S7 FATHOM EQUIPMENT & INVENTORY MODULE
# COMPREHENSIVE REVIEW & BUG FIX PLAN

## Document Version: 2.0 | January 2026

---

# EXECUTIVE SUMMARY

## Current Codebase Metrics
| Metric | Count |
|--------|-------|
| Total Lines of Code | 45,082 |
| C# Source Files | 112 |
| XAML View Files | 54 |
| Dialog Windows | 26 |
| Main Views | 26 |
| Services | 19 |
| ViewModels | 18 |
| Models | 7 |

## Issues Identified
| Category | Count | Priority |
|----------|-------|----------|
| Missing Commands (XAML → ViewModel) | 20 | CRITICAL |
| Missing Theme Resources | 17 | HIGH |
| Async Void Methods (potential bugs) | 10 | MEDIUM |
| Views with minimal code-behind | 9 | LOW |

---

# PHASE 1: CRITICAL COMMAND BINDINGS
**Priority:** CRITICAL | **Estimated Time:** 6-8 hours

## 1.1 Missing Commands - Must Implement

These commands are referenced in XAML but DO NOT EXIST in any ViewModel:

### User Management Commands (AdminView)
| Command | Target View | Implementation Location |
|---------|-------------|------------------------|
| `AddUserCommand` | AdminView.xaml | MainViewModel.cs |
| `EditUserCommand` | AdminView.xaml | MainViewModel.cs |
| `DeactivateUserCommand` | AdminView.xaml | MainViewModel.cs |
| `UnlockUserCommand` | AdminView.xaml | MainViewModel.cs |
| `ResetPasswordCommand` | AdminView.xaml | MainViewModel.cs |
| `SetPinCommand` | AdminView.xaml | MainViewModel.cs |
| `AddRoleCommand` | AdminView.xaml | MainViewModel.cs |
| `GeneratePasswordCommand` | UserEditorDialog.xaml | UserEditorViewModel (NEW) |

### Location Management Commands
| Command | Target View | Implementation Location |
|---------|-------------|------------------------|
| `AddLocationCommand` | LocationListView.xaml | MainViewModel.cs |
| `EditLocationCommand` | LocationListView.xaml | MainViewModel.cs |
| `DeleteLocationCommand` | LocationListView.xaml | MainViewModel.cs |

### Supplier Management Commands
| Command | Target View | Implementation Location |
|---------|-------------|------------------------|
| `AddSupplierCommand` | SupplierListView.xaml | MainViewModel.cs |
| `EditSupplierCommand` | SupplierListView.xaml | MainViewModel.cs |
| `DeleteSupplierCommand` | SupplierListView.xaml | MainViewModel.cs |

### Category/Certification Commands
| Command | Target View | Implementation Location |
|---------|-------------|------------------------|
| `AddCategoryCommand` | AdminView.xaml | MainViewModel.cs |
| `AddCertificationCommand` | CertificationView.xaml | MainViewModel.cs |

### Settings/Backup Commands
| Command | Target View | Implementation Location |
|---------|-------------|------------------------|
| `SaveSettingsCommand` | SettingsView.xaml | SettingsViewModel.cs ✅ (verify wiring) |
| `BrowseBackupLocationCommand` | BackupRestoreView.xaml | BackupRestoreViewModel.cs |

### Maintenance Commands
| Command | Target View | Implementation Location |
|---------|-------------|------------------------|
| `NewMaintenanceRecordCommand` | MaintenanceView.xaml | MainViewModel.cs |

### UI Commands
| Command | Target View | Implementation Location |
|---------|-------------|------------------------|
| `DismissInfoCommand` | Various | MainViewModel.cs |

---

## 1.2 Implementation Code Templates

### Add to MainViewModel.cs - User Management Section:

```csharp
#region User Management Commands
public ICommand AddUserCommand { get; private set; } = null!;
public ICommand EditUserCommand { get; private set; } = null!;
public ICommand DeactivateUserCommand { get; private set; } = null!;
public ICommand UnlockUserCommand { get; private set; } = null!;
public ICommand ResetPasswordCommand { get; private set; } = null!;
public ICommand SetPinCommand { get; private set; } = null!;
public ICommand AddRoleCommand { get; private set; } = null!;

private void InitializeUserManagementCommands()
{
    AddUserCommand = new RelayCommand(async _ => await AddUserAsync());
    EditUserCommand = new RelayCommand(async _ => await EditUserAsync(), _ => SelectedUser != null);
    DeactivateUserCommand = new RelayCommand(async _ => await DeactivateUserAsync(), _ => SelectedUser != null);
    UnlockUserCommand = new RelayCommand(async _ => await UnlockUserAsync(), _ => SelectedUser?.LockedOut == true);
    ResetPasswordCommand = new RelayCommand(async _ => await ResetPasswordAsync(), _ => SelectedUser != null);
    SetPinCommand = new RelayCommand(async _ => await SetPinAsync(), _ => SelectedUser != null);
    AddRoleCommand = new RelayCommand(async _ => await AddRoleAsync());
}
#endregion
```

### Add to MainViewModel.cs - Location Management Section:

```csharp
#region Location Management Commands
public ICommand AddLocationCommand { get; private set; } = null!;
public ICommand EditLocationCommand { get; private set; } = null!;
public ICommand DeleteLocationCommand { get; private set; } = null!;

private void InitializeLocationCommands()
{
    AddLocationCommand = new RelayCommand(_ => AddLocation());
    EditLocationCommand = new RelayCommand(_ => EditLocation(), _ => SelectedLocation != null);
    DeleteLocationCommand = new RelayCommand(async _ => await DeleteLocationAsync(), _ => SelectedLocation != null);
}

private void AddLocation()
{
    var dialog = new LocationEditorDialog(_dbService);
    dialog.Owner = Application.Current.MainWindow;
    if (dialog.ShowDialog() == true)
    {
        _ = LoadLocationsAsync();
    }
}

private void EditLocation()
{
    if (SelectedLocation == null) return;
    var dialog = new LocationEditorDialog(_dbService, SelectedLocation);
    dialog.Owner = Application.Current.MainWindow;
    if (dialog.ShowDialog() == true)
    {
        _ = LoadLocationsAsync();
    }
}

private async Task DeleteLocationAsync()
{
    if (SelectedLocation == null) return;
    
    var result = MessageBox.Show(
        $"Delete location '{SelectedLocation.Name}'?\n\nThis cannot be undone.",
        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
    
    if (result != MessageBoxResult.Yes) return;
    
    await _dbService.DeleteLocationByIdAsync(SelectedLocation.LocationId);
    await LoadLocationsAsync();
}
#endregion
```

### Add to MainViewModel.cs - Supplier Management Section:

```csharp
#region Supplier Management Commands
public ICommand AddSupplierCommand { get; private set; } = null!;
public ICommand EditSupplierCommand { get; private set; } = null!;
public ICommand DeleteSupplierCommand { get; private set; } = null!;

private void InitializeSupplierCommands()
{
    AddSupplierCommand = new RelayCommand(_ => AddSupplier());
    EditSupplierCommand = new RelayCommand(_ => EditSupplier(), _ => SelectedSupplier != null);
    DeleteSupplierCommand = new RelayCommand(async _ => await DeleteSupplierAsync(), _ => SelectedSupplier != null);
}
#endregion
```

---

# PHASE 2: MISSING THEME RESOURCES
**Priority:** HIGH | **Estimated Time:** 3-4 hours

## 2.1 Missing Brushes

Add to both `Themes/DarkTheme.xaml` and `Themes/LightTheme.xaml`:

```xml
<!-- Header Brush -->
<SolidColorBrush x:Key="HeaderBrush" Color="#1A1D21"/>  <!-- Dark -->
<SolidColorBrush x:Key="HeaderBrush" Color="#F5F5F5"/>  <!-- Light -->
```

## 2.2 Missing Styles

### ActionButton Style
```xml
<Style x:Key="ActionButton" TargetType="Button" BasedOn="{StaticResource SecondaryButton}">
    <Setter Property="Padding" Value="12,8"/>
    <Setter Property="MinWidth" Value="80"/>
</Style>
```

### GradientButton Style (LoginWindow)
```xml
<Style x:Key="GradientButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="Bold"/>
    <Setter Property="Padding" Value="30,12"/>
    <Setter Property="Background">
        <Setter.Value>
            <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                <GradientStop Color="#667eea" Offset="0"/>
                <GradientStop Color="#764ba2" Offset="1"/>
            </LinearGradientBrush>
        </Setter.Value>
    </Setter>
</Style>
```

### ModernInput Style (LoginWindow)
```xml
<Style x:Key="ModernInput" TargetType="TextBox" BasedOn="{StaticResource MahApps.Styles.TextBox}">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Padding" Value="12,10"/>
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="BorderBrush" Value="{DynamicResource BorderBrush}"/>
</Style>

<Style x:Key="ModernPasswordInput" TargetType="PasswordBox" BasedOn="{StaticResource MahApps.Styles.PasswordBox}">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Padding" Value="12,10"/>
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}"/>
</Style>
```

### PinButton Style
```xml
<Style x:Key="PinButton" TargetType="Button" BasedOn="{StaticResource SecondaryButton}">
    <Setter Property="Width" Value="60"/>
    <Setter Property="Height" Value="60"/>
    <Setter Property="FontSize" Value="24"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Margin" Value="5"/>
</Style>
```

### ToolbarButton Style
```xml
<Style x:Key="ToolbarButton" TargetType="Button" BasedOn="{StaticResource SecondaryButton}">
    <Setter Property="Padding" Value="10,5"/>
    <Setter Property="MinWidth" Value="0"/>
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="0"/>
</Style>
```

### ManifestTabHeader Style
```xml
<Style x:Key="ManifestTabHeader" TargetType="TextBlock">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
</Style>
```

### ThemeToggle Style
```xml
<Style x:Key="ThemeToggle" TargetType="ToggleButton">
    <Setter Property="Width" Value="40"/>
    <Setter Property="Height" Value="40"/>
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="Cursor" Value="Hand"/>
</Style>
```

## 2.3 Missing Converters

Add to `Converters/Converters.cs`:

```csharp
public class BytesToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            using var ms = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NotificationTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string type)
        {
            return type.ToLower() switch
            {
                "error" => new SolidColorBrush(Color.FromRgb(229, 57, 53)),
                "warning" => new SolidColorBrush(Color.FromRgb(255, 183, 77)),
                "success" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                _ => new SolidColorBrush(Color.FromRgb(66, 165, 245))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NotificationTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string type)
        {
            return type.ToLower() switch
            {
                "error" => "AlertCircle",
                "warning" => "Alert",
                "success" => "CheckCircle",
                _ => "Information"
            };
        }
        return "Information";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorString);
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class WizardStepDotConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepParam && int.TryParse(stepParam, out int step))
        {
            return currentStep >= step ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class WizardStepLineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepParam && int.TryParse(stepParam, out int step))
        {
            return currentStep > step 
                ? Application.Current.FindResource("AccentBrush") 
                : Application.Current.FindResource("BorderBrush");
        }
        return Application.Current.FindResource("BorderBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

---

# PHASE 3: VIEW IMPLEMENTATION FIXES
**Priority:** MEDIUM | **Estimated Time:** 4-6 hours

## 3.1 Views Needing ViewModel Wiring

### BackupRestoreView.xaml.cs
**Current:** 17 lines, no event handlers
**Issue:** Has ViewModel but no initialization

```csharp
public partial class BackupRestoreView : UserControl
{
    private readonly BackupRestoreViewModel _viewModel;
    
    public BackupRestoreView()
    {
        _viewModel = new BackupRestoreViewModel();
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += async (s, e) => await _viewModel.LoadBackupsAsync();
    }
}
```

### MaintenanceScheduleView.xaml.cs
**Current:** 17 lines, minimal
**Fix:** Ensure ViewModel is properly initialized

### ReportBuilderView.xaml.cs
**Current:** 17 lines, minimal
**Fix:** Ensure ViewModel commands are working

## 3.2 Async Void Fixes

**Issue:** `async void` methods can cause unhandled exceptions

### AdminView.xaml.cs - Fix async void methods:

```csharp
// BEFORE (problematic):
private async void LoadData() { ... }

// AFTER (proper):
private async Task LoadDataAsync() { ... }

// Call from Loaded event:
Loaded += async (s, e) => 
{
    try { await LoadDataAsync(); }
    catch (Exception ex) { Debug.WriteLine($"Load error: {ex.Message}"); }
};
```

---

# PHASE 4: DIALOG VERIFICATION
**Priority:** MEDIUM | **Estimated Time:** 4-6 hours

## 4.1 Dialogs to Verify

| Dialog | ViewModel | Commands | Status |
|--------|-----------|----------|--------|
| EquipmentEditorDialog | EquipmentEditorViewModel | 9 | ✅ Has all |
| ShipmentVerificationDialog | ShipmentVerificationViewModel | 10 | ✅ Has all |
| ManifestWizardDialog | ManifestWizardViewModel | 7 | ✅ Has all |
| ImportDialog | ImportViewModel | 4 | ✅ Has all |
| QrLabelPrintDialog | QrLabelPrintViewModel | 7 | ✅ Has all |
| SettingsDialog | SettingsViewModel | 6 | ✅ Has all |
| UserEditorDialog | ❌ MISSING | N/A | 🔴 CREATE |
| SupplierEditorDialog | ❌ Uses code-behind | N/A | ⚠️ Verify |
| LocationEditorDialog | ❌ Uses code-behind | N/A | ⚠️ Verify |

## 4.2 Create UserEditorViewModel

```csharp
public class UserEditorViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private User? _user;
    private bool _isNewUser;
    
    public User? User
    {
        get => _user;
        set => SetProperty(ref _user, value);
    }
    
    // Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand GeneratePasswordCommand { get; }
    
    public Action? CloseAction { get; set; }
    public bool DialogResult { get; private set; }
    
    public UserEditorViewModel(LocalDatabaseService dbService, User? existingUser = null)
    {
        _dbService = dbService;
        _isNewUser = existingUser == null;
        _user = existingUser ?? new User { UserId = Guid.NewGuid() };
        
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => Cancel());
        GeneratePasswordCommand = new RelayCommand(_ => GeneratePassword());
    }
    
    private bool CanSave() => !string.IsNullOrWhiteSpace(User?.Username);
    
    private void Save()
    {
        DialogResult = true;
        CloseAction?.Invoke();
    }
    
    private void Cancel()
    {
        DialogResult = false;
        CloseAction?.Invoke();
    }
    
    private void GeneratePassword()
    {
        // Generate random password
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        // Set to temporary password field
        OnPropertyChanged(nameof(GeneratedPassword));
    }
    
    public string GeneratedPassword { get; private set; } = "";
}
```

---

# PHASE 5: SERVICE INTEGRATION VERIFICATION
**Priority:** LOW | **Estimated Time:** 2-3 hours

## 5.1 Services to Test

| Service | Integration Points | Status |
|---------|-------------------|--------|
| AuthenticationService | LoginWindow, MainWindow | ✅ |
| BackupService | BackupRestoreView | ⚠️ Verify |
| BatchOperationsService | MainViewModel | ✅ Phase 3 |
| DashboardService | DashboardView | ✅ |
| EquipmentTemplateService | MainViewModel | ✅ Phase 3 |
| FavoritesService | MainViewModel | ✅ Phase 3 |
| QRCodeService | EquipmentEditorDialog | ⚠️ Verify |
| ReportBuilderService | ReportBuilderView | ⚠️ Verify |
| SyncService | MainWindow | ⚠️ Needs API |
| ExcelImportService | ImportDialog | ✅ |
| ExcelExportService | Multiple | ✅ |
| PdfExportService | Multiple | ✅ |

---

# PHASE 6: FULL TESTING
**Priority:** HIGH | **Estimated Time:** 8-10 hours

## 6.1 Navigation Testing Checklist

| Screen | Access | Data Load | Actions | Export |
|--------|--------|-----------|---------|--------|
| Dashboard | ⬜ | ⬜ | ⬜ | N/A |
| Equipment | ⬜ | ⬜ | ⬜ | ⬜ |
| Manifests | ⬜ | ⬜ | ⬜ | ⬜ |
| Locations | ⬜ | ⬜ | ⬜ | N/A |
| Suppliers | ⬜ | ⬜ | ⬜ | N/A |
| Maintenance | ⬜ | ⬜ | ⬜ | ⬜ |
| Defects | ⬜ | ⬜ | ⬜ | ⬜ |
| Certifications | ⬜ | ⬜ | ⬜ | ⬜ |
| Reports | ⬜ | ⬜ | ⬜ | ⬜ |
| Unregistered | ⬜ | ⬜ | ⬜ | N/A |
| Notifications | ⬜ | ⬜ | ⬜ | N/A |
| Admin | ⬜ | ⬜ | ⬜ | N/A |
| Settings | ⬜ | ⬜ | ⬜ | N/A |

## 6.2 Dialog Testing Checklist

| Dialog | Opens | Loads Data | Saves | Cancels |
|--------|-------|------------|-------|---------|
| EquipmentEditorDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| ManifestWizardDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| ShipmentVerificationDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| DefectReportEditorDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| UserEditorDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| LocationEditorDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| SupplierEditorDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| ImportDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| QrLabelPrintDialog | ⬜ | ⬜ | ⬜ | ⬜ |
| All Phase 3 Dialogs | ⬜ | ⬜ | ⬜ | ⬜ |

---

# IMPLEMENTATION SCHEDULE

## Week 1: Critical Fixes
| Day | Phase | Tasks |
|-----|-------|-------|
| 1 | 1.1 | User Management Commands |
| 2 | 1.1 | Location/Supplier Commands |
| 3 | 1.1 | Remaining Commands |
| 4 | 2.1-2.2 | Theme Resources |
| 5 | 2.3 | Missing Converters |

## Week 2: View & Dialog Fixes
| Day | Phase | Tasks |
|-----|-------|-------|
| 1 | 3.1 | View ViewModel Wiring |
| 2 | 3.2 | Async Void Fixes |
| 3 | 4.1-4.2 | Dialog Verification |
| 4 | 5.1 | Service Integration |
| 5 | Buffer | Catch-up |

## Week 3: Testing & Polish
| Day | Phase | Tasks |
|-----|-------|-------|
| 1-2 | 6.1 | Navigation Testing |
| 3-4 | 6.2 | Dialog Testing |
| 5 | | Bug Fixes & Polish |

---

# FILES TO MODIFY (Summary)

## High Priority Modifications
1. `ViewModels/MainViewModel.cs` - Add 20+ missing commands
2. `Themes/DarkTheme.xaml` - Add 10+ missing resources
3. `Themes/LightTheme.xaml` - Add 10+ missing resources
4. `Converters/Converters.cs` - Add 6 missing converters

## Medium Priority Modifications
1. `Views/BackupRestoreView.xaml.cs` - ViewModel wiring
2. `Views/AdminView.xaml.cs` - Fix async void methods
3. `ViewModels/Dialogs/UserEditorViewModel.cs` - CREATE NEW

## Low Priority Modifications
1. Various view code-behinds - Minor fixes

---

# QUICK REFERENCE: COMMAND COUNT BY LOCATION

| ViewModel | Commands Defined | Commands Needed |
|-----------|-----------------|-----------------|
| MainViewModel.cs | 43 | +20 |
| MainViewModel.BatchOperations.cs | 14 | 0 |
| EquipmentEditorViewModel | 9 | 0 |
| ShipmentVerificationViewModel | 10 | 0 |
| ManifestWizardViewModel | 7 | 0 |
| ImportViewModel | 4 | 0 |
| QrLabelPrintViewModel | 7 | 0 |
| SettingsViewModel | 6 | 0 |
| BackupRestoreViewModel | 4 | +1 |
| ReportBuilderViewModel | 7 | 0 |
| **TOTAL** | **111** | **+21** |

---

**Document Complete**
**Last Updated:** January 2026
