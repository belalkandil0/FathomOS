using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.Views.Dialogs;

using RelayCommand = FathomOS.Modules.EquipmentInventory.ViewModels.RelayCommand;

namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class AdminView : System.Windows.Controls.UserControl
{
    private readonly AdminViewModel _viewModel;
    
    public AdminView(LocalDatabaseService dbService, AuthenticationService authService)
    {
        InitializeComponent();
        _viewModel = new AdminViewModel(dbService, authService);
        DataContext = _viewModel;
    }
    
    // Event handlers for Pending Items tab
    private void PendingItemFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && _viewModel != null)
        {
            var filter = combo.SelectedIndex switch
            {
                0 => UnregisteredItemStatus.PendingReview,
                2 => UnregisteredItemStatus.ConvertedToEquipment,
                3 => UnregisteredItemStatus.KeptAsConsumable,
                4 => UnregisteredItemStatus.Rejected,
                _ => (UnregisteredItemStatus?)null // All items
            };
            _viewModel.FilterUnregisteredItems(filter);
        }
    }
    
    private void RefreshPendingItems_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.LoadUnregisteredItems();
    }
    
    private void ConvertToEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UnregisteredItem item)
        {
            _viewModel?.ConvertToEquipment(item);
        }
    }
    
    private void KeepAsConsumable_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UnregisteredItem item)
        {
            _viewModel?.KeepAsConsumable(item);
        }
    }
    
    private void RejectItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UnregisteredItem item)
        {
            _viewModel?.RejectItem(item);
        }
    }
}

public class AdminViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private readonly AuthenticationService _authService;
    private User? _selectedUser;
    private string _userSearchText = string.Empty;
    private bool _showDefaultCredentialsInfo = true;
    private int _pendingItemCount;
    private UnregisteredItemStatus? _currentFilter = UnregisteredItemStatus.PendingReview;
    private List<UnregisteredItem> _allUnregisteredItems = new();
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public AdminViewModel(LocalDatabaseService dbService, AuthenticationService authService)
    {
        _dbService = dbService;
        _authService = authService;
        
        // User commands
        AddUserCommand = new RelayCommand(_ => AddUser());
        EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null);
        ResetPasswordCommand = new RelayCommand(_ => ResetPassword(), _ => SelectedUser != null);
        SetPinCommand = new RelayCommand(_ => SetPin(), _ => SelectedUser != null);
        UnlockUserCommand = new RelayCommand(_ => UnlockUser(), _ => SelectedUser?.IsLocked == true);
        DeactivateUserCommand = new RelayCommand(_ => DeactivateUser(), _ => SelectedUser != null && !SelectedUser.IsSuperAdmin);
        DismissInfoCommand = new RelayCommand(_ => ShowDefaultCredentialsInfo = false);
        
        // Other commands
        AddRoleCommand = new RelayCommand(_ => AddRole());
        AddCategoryCommand = new RelayCommand(_ => AddCategory());
        
        LoadData();
        LoadUnregisteredItems();
    }
    
    public ObservableCollection<User> Users { get; } = new();
    public ObservableCollection<Role> Roles { get; } = new();
    public ObservableCollection<EquipmentCategory> Categories { get; } = new();
    public ObservableCollection<UnregisteredItem> UnregisteredItems { get; } = new();
    
    public IEnumerable<User> FilteredUsers => string.IsNullOrWhiteSpace(UserSearchText)
        ? Users
        : Users.Where(u => 
            u.Username.Contains(UserSearchText, StringComparison.OrdinalIgnoreCase) ||
            u.DisplayName.Contains(UserSearchText, StringComparison.OrdinalIgnoreCase) ||
            u.Email.Contains(UserSearchText, StringComparison.OrdinalIgnoreCase));
    
    public User? SelectedUser
    {
        get => _selectedUser;
        set { _selectedUser = value; OnPropertyChanged(); }
    }
    
    public string UserSearchText
    {
        get => _userSearchText;
        set { _userSearchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredUsers)); }
    }
    
    public bool ShowDefaultCredentialsInfo
    {
        get => _showDefaultCredentialsInfo;
        set { _showDefaultCredentialsInfo = value; OnPropertyChanged(); }
    }
    
    public int PendingItemCount
    {
        get => _pendingItemCount;
        set { _pendingItemCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingItems)); OnPropertyChanged(nameof(HasNoPendingItems)); }
    }
    
    public bool HasPendingItems => PendingItemCount > 0;
    public bool HasNoPendingItems => UnregisteredItems.Count == 0;
    
    // Commands
    public ICommand AddUserCommand { get; }
    public ICommand EditUserCommand { get; }
    public ICommand ResetPasswordCommand { get; }
    public ICommand SetPinCommand { get; }
    public ICommand UnlockUserCommand { get; }
    public ICommand DeactivateUserCommand { get; }
    public ICommand DismissInfoCommand { get; }
    public ICommand AddRoleCommand { get; }
    public ICommand AddCategoryCommand { get; }
    
    private async void LoadData()
    {
        try
        {
            var users = await _dbService.GetAllUsersAsync();
            var roles = await _dbService.GetAllRolesAsync();
            var categories = await _dbService.GetAllCategoriesAsync();
            
            Users.Clear();
            foreach (var u in users) Users.Add(u);
            
            Roles.Clear();
            foreach (var r in roles) Roles.Add(r);
            
            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);
            
            OnPropertyChanged(nameof(FilteredUsers));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading admin data: {ex.Message}");
        }
    }
    
    public async void LoadUnregisteredItems()
    {
        try
        {
            _allUnregisteredItems = await _dbService.GetUnregisteredItemsAsync(null);
            PendingItemCount = _allUnregisteredItems.Count(i => i.Status == UnregisteredItemStatus.PendingReview);
            FilterUnregisteredItems(_currentFilter);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading unregistered items: {ex.Message}");
        }
    }
    
    public void FilterUnregisteredItems(UnregisteredItemStatus? filter)
    {
        _currentFilter = filter;
        UnregisteredItems.Clear();
        
        var items = filter.HasValue
            ? _allUnregisteredItems.Where(i => i.Status == filter.Value)
            : _allUnregisteredItems;
        
        foreach (var item in items.OrderByDescending(i => i.CreatedDate))
        {
            UnregisteredItems.Add(item);
        }
        
        OnPropertyChanged(nameof(HasNoPendingItems));
    }
    
    public async void ConvertToEquipment(UnregisteredItem item)
    {
        var result = MessageBox.Show(
            $"Convert '{item.Name}' to tracked equipment?\n\n" +
            $"This will create a new equipment record in the inventory system.",
            "Convert to Equipment",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            // Create new equipment from unregistered item
            var equipment = new Equipment
            {
                Name = item.Name,
                Description = item.Description,
                SerialNumber = item.SerialNumber,
                Manufacturer = item.Manufacturer,
                Model = item.Model,
                LocationId = item.DestinationLocationId,
                Status = EquipmentStatus.Available,
                Condition = EquipmentCondition.Good,
                IsConsumable = false
            };
            
            var savedEquipment = await _dbService.SaveEquipmentAsync(equipment);
            
            // Update unregistered item status
            item.Status = UnregisteredItemStatus.ConvertedToEquipment;
            item.ConvertedEquipmentId = savedEquipment.EquipmentId;
            item.ReviewedDate = DateTime.UtcNow;
            await _dbService.SaveUnregisteredItemAsync(item);
            
            MessageBox.Show(
                $"Equipment created successfully!\n\n" +
                $"Asset Number: {savedEquipment.AssetNumber}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            LoadUnregisteredItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to convert item: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    public async void KeepAsConsumable(UnregisteredItem item)
    {
        var result = MessageBox.Show(
            $"Mark '{item.Name}' as consumable?\n\n" +
            $"This item will be recorded but not tracked in the inventory system.",
            "Keep as Consumable",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            item.Status = UnregisteredItemStatus.KeptAsConsumable;
            item.ReviewedDate = DateTime.UtcNow;
            await _dbService.SaveUnregisteredItemAsync(item);
            
            MessageBox.Show("Item marked as consumable.", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            
            LoadUnregisteredItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update item: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    public async void RejectItem(UnregisteredItem item)
    {
        var result = MessageBox.Show(
            $"Reject '{item.Name}'?\n\n" +
            $"This item will be marked as rejected and removed from the pending list.",
            "Reject Item",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            item.Status = UnregisteredItemStatus.Rejected;
            item.ReviewedDate = DateTime.UtcNow;
            await _dbService.SaveUnregisteredItemAsync(item);
            
            MessageBox.Show("Item rejected.", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            
            LoadUnregisteredItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to reject item: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void AddUser()
    {
        var dialog = new UserEditorDialog(_dbService);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            var tempPassword = dialog.GetTemporaryPassword();
            
            // Show temporary password to admin
            MessageBox.Show(
                $"User created successfully!\n\n" +
                $"Temporary Password: {tempPassword}\n\n" +
                $"Please provide this password to the user.\n" +
                $"They will be required to change it on first login.",
                "User Created",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            LoadData();
        }
    }
    
    private void EditUser()
    {
        if (SelectedUser == null) return;
        
        var dialog = new UserEditorDialog(_dbService, SelectedUser);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            LoadData();
        }
    }
    
    private async void ResetPassword()
    {
        if (SelectedUser == null) return;
        
        var result = MessageBox.Show(
            $"Reset password for {SelectedUser.Username}?\n\n" +
            $"A new temporary password will be generated and the user will be required to change it on next login.",
            "Reset Password",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        var tempPassword = LocalDatabaseService.GenerateTemporaryPassword();
        var (success, error) = await _dbService.ResetPasswordAsync(SelectedUser.UserId, tempPassword);
        
        if (success)
        {
            MessageBox.Show(
                $"Password reset successfully!\n\n" +
                $"New Temporary Password: {tempPassword}\n\n" +
                $"Please provide this password to the user.",
                "Password Reset",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            LoadData();
        }
        else
        {
            MessageBox.Show($"Failed to reset password: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void SetPin()
    {
        if (SelectedUser == null) return;
        
        // Generate a new PIN
        var random = new Random();
        var pin = random.Next(1000, 9999).ToString();
        
        var result = MessageBox.Show(
            $"Set PIN for {SelectedUser.Username}?\n\n" +
            $"New PIN will be: {pin}\n\n" +
            $"Click Yes to confirm, or No to cancel.",
            "Set PIN",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        var (success, error) = await _dbService.SetUserPinAsync(SelectedUser.UserId, pin);
        
        if (success)
        {
            MessageBox.Show(
                $"PIN set successfully for {SelectedUser.Username}\n\n" +
                $"PIN: {pin}\n\n" +
                $"Please provide this PIN to the user.",
                "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Failed to set PIN: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void UnlockUser()
    {
        if (SelectedUser == null || !SelectedUser.IsLocked) return;
        
        var success = await _dbService.UnlockUserAsync(SelectedUser.UserId);
        
        if (success)
        {
            MessageBox.Show($"User {SelectedUser.Username} has been unlocked.", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData();
        }
        else
        {
            MessageBox.Show("Failed to unlock user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void DeactivateUser()
    {
        if (SelectedUser == null || SelectedUser.IsSuperAdmin) return;
        
        var result = MessageBox.Show(
            $"Deactivate user {SelectedUser.Username}?\n\n" +
            $"The user will no longer be able to log in.",
            "Deactivate User",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;
        
        var success = await _dbService.DeactivateUserAsync(SelectedUser.UserId);
        
        if (success)
        {
            MessageBox.Show($"User {SelectedUser.Username} has been deactivated.", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData();
        }
        else
        {
            MessageBox.Show("Failed to deactivate user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void AddRole()
    {
        MessageBox.Show("Role editor will be implemented. For now, use database directly.", "Add Role", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void AddCategory()
    {
        MessageBox.Show("Category editor will be implemented. For now, use database directly.", "Add Category", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
