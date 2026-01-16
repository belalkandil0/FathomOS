using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels;

namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class LocationEditorDialog : MetroWindow
{
    private readonly LocationEditorViewModel _viewModel;
    
    public LocationEditorDialog(LocalDatabaseService dbService, Location? existingLocation = null)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new LocationEditorViewModel(dbService, existingLocation);
        DataContext = _viewModel;
        
        _viewModel.RequestClose += (success) =>
        {
            DialogResult = success;
            Close();
        };
        
        Loaded += (s, e) => NameTextBox.Focus();
    }
    
    public Location? GetLocation() => _viewModel.GetLocation();
    
    /// <summary>
    /// Get the user assignments to save
    /// </summary>
    public List<(Guid UserId, string AccessLevel)> GetUserAssignments() => _viewModel.GetUserAssignments();
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// ViewModel item for assigned users with display properties
/// </summary>
public class AssignedUserViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    private string _accessLevel = "Read";
    public string AccessLevel
    {
        get => _accessLevel;
        set { _accessLevel = value; OnPropertyChanged(); }
    }
    
    public string Initials => string.IsNullOrEmpty(DisplayName) 
        ? "?" 
        : string.Concat(DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(n => char.ToUpper(n[0])));
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class LocationEditorViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private readonly Location? _existingLocation;
    
    public event Action<bool>? RequestClose;
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public LocationEditorViewModel(LocalDatabaseService dbService, Location? existingLocation = null)
    {
        _dbService = dbService;
        _existingLocation = existingLocation;
        
        // Initialize location types
        LocationTypes = new ObservableCollection<LocationType>(Enum.GetValues<LocationType>());
        
        // Initialize user collections
        AssignedUsers = new ObservableCollection<AssignedUserViewModel>();
        AvailableUsers = new ObservableCollection<User>();
        AccessLevels = new ObservableCollection<string> { "Read", "Write", "Admin" };
        
        if (existingLocation != null)
        {
            Name = existingLocation.Name;
            Code = existingLocation.Code;
            SelectedType = existingLocation.Type;
            Description = existingLocation.Description ?? "";
            Address = existingLocation.Address ?? "";
            ContactPerson = existingLocation.ContactPerson ?? "";
            IsActive = existingLocation.IsActive;
        }
        else
        {
            SelectedType = LocationType.Base;
            IsActive = true;
        }
        
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
        AddUserCommand = new RelayCommand(_ => AddUser(), _ => SelectedAvailableUser != null);
        RemoveUserCommand = new RelayCommand(user => RemoveUser(user as AssignedUserViewModel));
        
        _ = LoadDataAsync();
    }
    
    public string WindowTitle => _existingLocation == null ? "Add New Location" : "Edit Location";
    public bool IsNew => _existingLocation == null;
    
    public ObservableCollection<LocationType> LocationTypes { get; }
    public ObservableCollection<AssignedUserViewModel> AssignedUsers { get; }
    public ObservableCollection<User> AvailableUsers { get; }
    public ObservableCollection<string> AccessLevels { get; }
    
    #region Location Properties
    
    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }
    
    private string _code = "";
    public string Code
    {
        get => _code;
        set { _code = value?.ToUpperInvariant() ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }
    
    private LocationType _selectedType;
    public LocationType SelectedType
    {
        get => _selectedType;
        set { _selectedType = value; OnPropertyChanged(); }
    }
    
    private string _description = "";
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }
    
    private string _address = "";
    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); }
    }
    
    private string _contactPerson = "";
    public string ContactPerson
    {
        get => _contactPerson;
        set { _contactPerson = value; OnPropertyChanged(); }
    }
    
    private bool _isActive = true;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }
    
    #endregion
    
    #region User Assignment Properties
    
    public int AssignedUsersCount => AssignedUsers.Count;
    public bool HasNoAssignedUsers => AssignedUsers.Count == 0;
    
    private AssignedUserViewModel? _selectedAssignedUser;
    public AssignedUserViewModel? SelectedAssignedUser
    {
        get => _selectedAssignedUser;
        set { _selectedAssignedUser = value; OnPropertyChanged(); }
    }
    
    private User? _selectedAvailableUser;
    public User? SelectedAvailableUser
    {
        get => _selectedAvailableUser;
        set { _selectedAvailableUser = value; OnPropertyChanged(); }
    }
    
    #endregion
    
    #region Error Properties
    
    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; OnPropertyChanged(); }
    }
    
    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); HasError = !string.IsNullOrEmpty(value); }
    }
    
    #endregion
    
    #region Commands
    
    public ICommand SaveCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand RemoveUserCommand { get; }
    
    #endregion
    
    #region Methods
    
    private async Task LoadDataAsync()
    {
        try
        {
            // Load all users
            var allUsers = await _dbService.GetAllUsersAsync();
            
            // If editing existing location, load assigned users
            if (_existingLocation != null)
            {
                var assignments = await _dbService.GetUserLocationAssignmentsAsync(_existingLocation.LocationId);
                foreach (var assignment in assignments)
                {
                    if (assignment.User != null)
                    {
                        AssignedUsers.Add(new AssignedUserViewModel
                        {
                            UserId = assignment.UserId,
                            Username = assignment.User.Username,
                            DisplayName = assignment.User.DisplayName,
                            Email = assignment.User.Email ?? "",
                            AccessLevel = assignment.AccessLevel
                        });
                    }
                }
            }
            
            // Populate available users (exclude already assigned)
            RefreshAvailableUsers(allUsers);
            
            OnPropertyChanged(nameof(AssignedUsersCount));
            OnPropertyChanged(nameof(HasNoAssignedUsers));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load users: {ex.Message}";
        }
    }
    
    private void RefreshAvailableUsers(List<User>? allUsers = null)
    {
        if (allUsers == null)
        {
            // Keep existing available users, just filter
            var toRemove = AvailableUsers
                .Where(u => AssignedUsers.Any(a => a.UserId == u.UserId))
                .ToList();
            foreach (var u in toRemove)
                AvailableUsers.Remove(u);
            return;
        }
        
        AvailableUsers.Clear();
        var assignedIds = AssignedUsers.Select(a => a.UserId).ToHashSet();
        foreach (var user in allUsers.Where(u => u.IsActive && !assignedIds.Contains(u.UserId)))
        {
            AvailableUsers.Add(user);
        }
    }
    
    private void AddUser()
    {
        if (SelectedAvailableUser == null) return;
        
        var user = SelectedAvailableUser;
        AssignedUsers.Add(new AssignedUserViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email ?? "",
            AccessLevel = "Read"
        });
        
        AvailableUsers.Remove(user);
        SelectedAvailableUser = null;
        
        OnPropertyChanged(nameof(AssignedUsersCount));
        OnPropertyChanged(nameof(HasNoAssignedUsers));
    }
    
    private void RemoveUser(AssignedUserViewModel? user)
    {
        if (user == null) return;
        
        AssignedUsers.Remove(user);
        
        // Parse display name into first/last (best effort)
        var nameParts = (user.DisplayName ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : null;
        var lastName = nameParts.Length > 1 ? nameParts[1] : null;
        
        // Add back to available users
        AvailableUsers.Add(new User
        {
            UserId = user.UserId,
            Username = user.Username,
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email
        });
        
        OnPropertyChanged(nameof(AssignedUsersCount));
        OnPropertyChanged(nameof(HasNoAssignedUsers));
    }
    
    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Code);
    }
    
    private async void Save()
    {
        try
        {
            ErrorMessage = "";
            
            // Validate
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Location name is required.";
                return;
            }
            
            if (string.IsNullOrWhiteSpace(Code))
            {
                ErrorMessage = "Location code is required.";
                return;
            }
            
            // Check for duplicate code
            var existing = await _dbService.GetAllLocationsAsync();
            var duplicate = existing.FirstOrDefault(l => 
                l.Code.Equals(Code, StringComparison.OrdinalIgnoreCase) && 
                l.LocationId != (_existingLocation?.LocationId ?? Guid.Empty));
                
            if (duplicate != null)
            {
                ErrorMessage = $"A location with code '{Code}' already exists.";
                return;
            }
            
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving location: {ex.Message}";
        }
    }
    
    public Location? GetLocation()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Code))
            return null;
            
        var location = _existingLocation ?? new Location();
        location.Name = Name.Trim();
        location.Code = Code.Trim().ToUpperInvariant();
        location.Type = SelectedType;
        location.Description = Description?.Trim();
        location.Address = Address?.Trim();
        location.ContactPerson = ContactPerson?.Trim();
        location.IsActive = IsActive;
        location.UpdatedAt = DateTime.UtcNow;
        
        if (_existingLocation == null)
        {
            location.LocationId = Guid.NewGuid();
        }
        
        return location;
    }
    
    /// <summary>
    /// Get the current user assignments to be saved
    /// </summary>
    public List<(Guid UserId, string AccessLevel)> GetUserAssignments()
    {
        return AssignedUsers
            .Select(u => (u.UserId, u.AccessLevel))
            .ToList();
    }
    
    #endregion
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
