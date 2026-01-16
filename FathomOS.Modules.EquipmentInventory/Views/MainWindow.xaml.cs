using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels;


namespace FathomOS.Modules.EquipmentInventory.Views;

/// <summary>
/// Modern MainWindow with sidebar navigation and theme toggle
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly LocalDatabaseService _dbService;
    private readonly SyncService _syncService;
    private readonly AuthenticationService _authService;
    private readonly ThemeService _themeService;
    private readonly MainViewModel _viewModel;
    private bool _isInitialized;
    
    // Content views (lazy loaded)
    private DashboardView? _dashboardView;
    private EquipmentListView? _equipmentView;
    private ManifestManagementView? _manifestView;
    private LocationListView? _locationView;
    private SupplierListView? _suppliersView;
    private MaintenanceView? _maintenanceView;
    private DefectReportListView? _defectsView;
    private CertificationView? _certificationView;
    private ReportsView? _reportsView;
    private AdminView? _adminView;
    private SettingsView? _settingsView;
    private UnregisteredItemsView? _unregisteredView;
    private NotificationsView? _notificationsView;
    
    public MainWindow(LocalDatabaseService dbService, SyncService syncService, AuthenticationService authService)
    {
        _dbService = dbService;
        _syncService = syncService;
        _authService = authService;
        _themeService = new ThemeService();
        
        // Load theme before InitializeComponent
        var settings = ModuleSettings.Load();
        _themeService.ApplyTheme(settings.UseDarkTheme ? "Dark" : "Light");
        
        InitializeComponent();
        
        // Set theme toggle state
        ThemeToggleButton.IsChecked = settings.UseDarkTheme;
        
        // Create and set ViewModel
        _viewModel = new MainViewModel(_dbService, _syncService, _authService);
        DataContext = _viewModel;
        
        // Subscribe to events
        StateChanged += MainWindow_StateChanged;
        Loaded += MainWindow_Loaded;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Now it's safe to load the dashboard
        _isInitialized = true;
        LoadDashboard();
    }
    
    #region Title Bar
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
        }
        else
        {
            DragMove();
        }
    }
    
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        MaximizeIcon.Kind = WindowState == WindowState.Maximized 
            ? MahApps.Metro.IconPacks.PackIconMaterialKind.WindowRestore 
            : MahApps.Metro.IconPacks.PackIconMaterialKind.WindowMaximize;
    }
    
    #endregion
    
    #region Theme Toggle
    
    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        var isDark = ThemeToggleButton.IsChecked == true;
        _themeService.ApplyTheme(isDark ? "Dark" : "Light");
        
        // Save preference
        var settings = ModuleSettings.Load();
        settings.UseDarkTheme = isDark;
        settings.Save();
    }
    
    #endregion
    
    #region Navigation
    
    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        // Don't process navigation until window is fully initialized
        if (!_isInitialized || PageContent == null) return;
        
        if (sender is RadioButton rb)
        {
            var tag = rb.Tag?.ToString();
            switch (tag)
            {
                case "Dashboard":
                    LoadDashboard();
                    break;
                case "Equipment":
                    LoadEquipment();
                    break;
                case "Manifests":
                    LoadManifests();
                    break;
                case "Locations":
                    LoadLocations();
                    break;
                case "Suppliers":
                    LoadSuppliers();
                    break;
                case "Maintenance":
                    LoadMaintenance();
                    break;
                case "Defects":
                    LoadDefects();
                    break;
                case "Certifications":
                    LoadCertifications();
                    break;
                case "Reports":
                    LoadReports();
                    break;
                case "Unregistered":
                    LoadUnregistered();
                    break;
                case "Notifications":
                    LoadNotifications();
                    break;
                case "Admin":
                    LoadAdmin();
                    break;
                case "Settings":
                    LoadSettings();
                    break;
            }
        }
    }
    
    private void LoadDashboard()
    {
        if (PageContent == null) return;
        _dashboardView ??= new DashboardView(_dbService);
        PageContent.Content = _dashboardView;
    }
    
    private void LoadEquipment()
    {
        if (PageContent == null) return;
        _equipmentView ??= new EquipmentListView(_dbService, _viewModel);
        PageContent.Content = _equipmentView;
    }
    
    private void LoadManifests()
    {
        if (PageContent == null) return;
        _manifestView ??= new ManifestManagementView(_dbService, _viewModel);
        PageContent.Content = _manifestView;
    }
    
    private void LoadLocations()
    {
        if (PageContent == null) return;
        _locationView ??= new LocationListView(_dbService);
        PageContent.Content = _locationView;
    }
    
    private void LoadSuppliers()
    {
        if (PageContent == null) return;
        _suppliersView ??= new SupplierListView(_dbService);
        PageContent.Content = _suppliersView;
    }
    
    private void LoadMaintenance()
    {
        if (PageContent == null) return;
        _maintenanceView ??= new MaintenanceView(_dbService);
        PageContent.Content = _maintenanceView;
    }
    
    private void LoadDefects()
    {
        if (PageContent == null) return;
        _defectsView ??= new DefectReportListView();
        _defectsView.DataContext = _viewModel;
        PageContent.Content = _defectsView;
    }
    
    private void LoadCertifications()
    {
        if (PageContent == null) return;
        _certificationView ??= new CertificationView(_dbService);
        PageContent.Content = _certificationView;
    }
    
    private void LoadReports()
    {
        if (PageContent == null) return;
        _reportsView ??= new ReportsView(_viewModel);
        PageContent.Content = _reportsView;
    }
    
    private void LoadUnregistered()
    {
        if (PageContent == null) return;
        _unregisteredView ??= new UnregisteredItemsView(_dbService);
        PageContent.Content = _unregisteredView;
    }
    
    private void LoadNotifications()
    {
        if (PageContent == null) return;
        _notificationsView ??= new NotificationsView(_dbService);
        PageContent.Content = _notificationsView;
    }
    
    private void LoadAdmin()
    {
        if (PageContent == null) return;
        _adminView ??= new AdminView(_dbService, _authService);
        PageContent.Content = _adminView;
    }
    
    private void LoadSettings()
    {
        if (PageContent == null) return;
        _settingsView ??= new SettingsView(_dbService, _syncService);
        PageContent.Content = _settingsView;
    }
    
    #endregion
    
    #region User Menu
    
    private void UserMenu_Click(object sender, RoutedEventArgs e)
    {
        var contextMenu = new ContextMenu();
        
        var profileItem = new MenuItem { Header = "My Profile" };
        profileItem.Click += (s, args) => ShowProfile();
        contextMenu.Items.Add(profileItem);
        
        contextMenu.Items.Add(new Separator());
        
        var logoutItem = new MenuItem { Header = "Sign Out" };
        logoutItem.Click += (s, args) => SignOut();
        contextMenu.Items.Add(logoutItem);
        
        contextMenu.PlacementTarget = sender as Button;
        contextMenu.IsOpen = true;
    }
    
    private void ShowProfile()
    {
        var currentUser = _authService.CurrentUser;
        if (currentUser == null)
        {
            System.Windows.MessageBox.Show("No user logged in.", "Profile", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var profileInfo = $"Username: {currentUser.Username}\n" +
                          $"Full Name: {currentUser.FullName}\n" +
                          $"Email: {currentUser.Email ?? "Not set"}\n" +
                          $"Role: {currentUser.Role?.Name ?? "User"}\n" +
                          $"Last Login: {currentUser.LastLoginAt?.ToString("g") ?? "Never"}";
        
        var result = System.Windows.MessageBox.Show(
            profileInfo + "\n\nWould you like to change your password?", 
            "My Profile", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Information);
            
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var changePasswordDialog = new Dialogs.ChangePasswordDialog(_dbService, currentUser);
                changePasswordDialog.Owner = this;
                changePasswordDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening password dialog: {ex.Message}");
            }
        }
    }
    
    private void SignOut()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to sign out?", 
            "Sign Out", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question);
            
        if (result == MessageBoxResult.Yes)
        {
            _authService.Logout();
            Close();
        }
    }
    
    #endregion
    
    #region Help
    
    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        var helpDialog = new HelpDialog();
        helpDialog.Owner = this;
        helpDialog.ShowDialog();
    }
    
    #endregion
    
    #region Notifications
    
    private async void NotificationButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadNotificationsAsync();
        NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
    }
    
    private async Task LoadNotificationsAsync()
    {
        try
        {
            var notifications = await _dbService.GetManifestNotificationsAsync(isResolved: false);
            
            NotificationList.ItemsSource = notifications.Take(10).ToList();
            
            // Show/hide empty state
            NoNotificationsPanel.Visibility = notifications.Any() ? Visibility.Collapsed : Visibility.Visible;
            
            // Update badge
            _viewModel.HasNotifications = notifications.Any();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
        }
    }
    
    private async void MarkAllNotificationsRead_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var notifications = await _dbService.GetManifestNotificationsAsync(isResolved: false);
            foreach (var notification in notifications)
            {
                await _dbService.ResolveNotificationAsync(notification.NotificationId, 
                    _authService.CurrentUser?.UserId, "Marked as read");
            }
            
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking notifications read: {ex.Message}");
        }
    }
    
    private async void DismissNotification_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is Models.ManifestNotification notification)
        {
            try
            {
                await _dbService.ResolveNotificationAsync(notification.NotificationId,
                    _authService.CurrentUser?.UserId, "Dismissed");
                await LoadNotificationsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error dismissing notification: {ex.Message}");
            }
        }
    }
    
    private void Notification_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Models.ManifestNotification notification)
        {
            NotificationPopup.IsOpen = false;
            
            // Navigate to relevant content based on notification type
            if (notification.ManifestId != Guid.Empty)
            {
                // Navigate to manifests and select the relevant one
                NavManifests.IsChecked = true;
            }
        }
    }
    
    private void ViewAllNotifications_Click(object sender, RoutedEventArgs e)
    {
        NotificationPopup.IsOpen = false;
        
        // Navigate to a notifications view (or defects which includes similar info)
        NavDefects.IsChecked = true;
    }
    
    #endregion
}
