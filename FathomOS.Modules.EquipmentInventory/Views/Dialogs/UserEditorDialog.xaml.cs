using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

using RelayCommand = FathomOS.Modules.EquipmentInventory.ViewModels.RelayCommand;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class UserEditorDialog : MetroWindow
{
    private readonly UserEditorViewModel _viewModel;
    
    public UserEditorDialog(LocalDatabaseService dbService, User? existingUser = null)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new UserEditorViewModel(dbService, existingUser);
        DataContext = _viewModel;
        
        _viewModel.RequestClose += (success) =>
        {
            DialogResult = success;
            Close();
        };
    }
    
    public User? GetUser() => _viewModel.GetUser();
    public string? GetTemporaryPassword() => _viewModel.TemporaryPassword;
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class UserEditorViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private readonly User? _existingUser;
    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _phone = string.Empty;
    private string _temporaryPassword = string.Empty;
    private Role? _selectedRole;
    private Location? _selectedLocation;
    private bool _isActive = true;
    private bool _mustChangePassword = true;
    private int _passwordExpiryDays = 90;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<bool>? RequestClose;
    
    public UserEditorViewModel(LocalDatabaseService dbService, User? existingUser = null)
    {
        _dbService = dbService;
        _existingUser = existingUser;
        
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
        GeneratePasswordCommand = new RelayCommand(_ => GeneratePassword());
        
        LoadData();
        
        if (existingUser != null)
        {
            // Edit mode
            _username = existingUser.Username;
            _email = existingUser.Email;
            _firstName = existingUser.FirstName ?? "";
            _lastName = existingUser.LastName ?? "";
            _phone = existingUser.Phone ?? "";
            _isActive = existingUser.IsActive;
            _mustChangePassword = existingUser.MustChangePassword;
            _passwordExpiryDays = existingUser.PasswordExpiryDays;
        }
        else
        {
            // New user - generate temporary password
            GeneratePassword();
        }
    }
    
    public bool IsNewUser => _existingUser == null;
    public string DialogTitle => IsNewUser ? "Create New User" : "Edit User";
    public string SaveButtonText => IsNewUser ? "Create User" : "Save Changes";
    
    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); ClearError(); }
    }
    
    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); ClearError(); }
    }
    
    public string FirstName
    {
        get => _firstName;
        set { _firstName = value; OnPropertyChanged(); }
    }
    
    public string LastName
    {
        get => _lastName;
        set { _lastName = value; OnPropertyChanged(); }
    }
    
    public string Phone
    {
        get => _phone;
        set { _phone = value; OnPropertyChanged(); }
    }
    
    public string TemporaryPassword
    {
        get => _temporaryPassword;
        set { _temporaryPassword = value; OnPropertyChanged(); }
    }
    
    public Role? SelectedRole
    {
        get => _selectedRole;
        set { _selectedRole = value; OnPropertyChanged(); ClearError(); }
    }
    
    public Location? SelectedLocation
    {
        get => _selectedLocation;
        set { _selectedLocation = value; OnPropertyChanged(); }
    }
    
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }
    
    public bool MustChangePassword
    {
        get => _mustChangePassword;
        set { _mustChangePassword = value; OnPropertyChanged(); }
    }
    
    public int PasswordExpiryDays
    {
        get => _passwordExpiryDays;
        set { _passwordExpiryDays = value; OnPropertyChanged(); }
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }
    
    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; OnPropertyChanged(); }
    }
    
    public ObservableCollection<Role> AvailableRoles { get; } = new();
    public ObservableCollection<Location> AvailableLocations { get; } = new();
    
    public ICommand SaveCommand { get; }
    public ICommand GeneratePasswordCommand { get; }
    
    private async void LoadData()
    {
        var roles = await _dbService.GetAllRolesAsync();
        var locations = await _dbService.GetAllLocationsAsync();
        
        AvailableRoles.Clear();
        foreach (var role in roles.Where(r => r.IsActive))
            AvailableRoles.Add(role);
        
        AvailableLocations.Clear();
        foreach (var location in locations.Where(l => l.IsActive))
            AvailableLocations.Add(location);
        
        // Set defaults for edit mode
        if (_existingUser != null)
        {
            SelectedRole = AvailableRoles.FirstOrDefault(r => 
                _existingUser.UserRoles.Any(ur => ur.RoleId == r.RoleId));
            SelectedLocation = AvailableLocations.FirstOrDefault(l => 
                l.LocationId == _existingUser.DefaultLocationId);
        }
        else
        {
            // Default to Store Keeper role for new users
            SelectedRole = AvailableRoles.FirstOrDefault(r => r.Name == "Store Keeper") 
                          ?? AvailableRoles.FirstOrDefault();
        }
    }
    
    private void GeneratePassword()
    {
        TemporaryPassword = LocalDatabaseService.GenerateTemporaryPassword();
    }
    
    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Email) &&
               SelectedRole != null &&
               (IsNewUser ? !string.IsNullOrWhiteSpace(TemporaryPassword) : true);
    }
    
    private async void Save()
    {
        ClearError();
        
        // Validate
        if (string.IsNullOrWhiteSpace(Username))
        {
            ShowError("Username is required");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(Email))
        {
            ShowError("Email is required");
            return;
        }
        
        if (!Email.Contains("@"))
        {
            ShowError("Please enter a valid email address");
            return;
        }
        
        if (SelectedRole == null)
        {
            ShowError("Please select a role");
            return;
        }
        
        try
        {
            if (IsNewUser)
            {
                // Create new user
                var newUser = new User
                {
                    Username = Username.Trim(),
                    Email = Email.Trim().ToLower(),
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Phone = Phone.Trim(),
                    IsActive = IsActive,
                    MustChangePassword = MustChangePassword,
                    PasswordExpiryDays = PasswordExpiryDays,
                    DefaultLocationId = SelectedLocation?.LocationId
                };
                
                var (success, error) = await _dbService.CreateUserAsync(newUser, TemporaryPassword);
                
                if (!success)
                {
                    ShowError(error ?? "Failed to create user");
                    return;
                }
                
                // Assign role
                await _dbService.AssignRoleToUserAsync(newUser.UserId, SelectedRole.RoleId);
                
                RequestClose?.Invoke(true);
            }
            else
            {
                // Update existing user
                _existingUser!.Username = Username.Trim();
                _existingUser.Email = Email.Trim().ToLower();
                _existingUser.FirstName = FirstName.Trim();
                _existingUser.LastName = LastName.Trim();
                _existingUser.Phone = Phone.Trim();
                _existingUser.IsActive = IsActive;
                _existingUser.MustChangePassword = MustChangePassword;
                _existingUser.PasswordExpiryDays = PasswordExpiryDays;
                _existingUser.DefaultLocationId = SelectedLocation?.LocationId;
                
                var (success, error) = await _dbService.UpdateUserAsync(_existingUser);
                
                if (!success)
                {
                    ShowError(error ?? "Failed to update user");
                    return;
                }
                
                // Update role assignment
                var existingRoleIds = _existingUser.UserRoles.Select(ur => ur.RoleId).ToList();
                foreach (var roleId in existingRoleIds)
                {
                    await _dbService.RemoveRoleFromUserAsync(_existingUser.UserId, roleId);
                }
                await _dbService.AssignRoleToUserAsync(_existingUser.UserId, SelectedRole.RoleId);
                
                RequestClose?.Invoke(true);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }
    
    public User? GetUser()
    {
        return _existingUser;
    }
    
    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
    
    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
