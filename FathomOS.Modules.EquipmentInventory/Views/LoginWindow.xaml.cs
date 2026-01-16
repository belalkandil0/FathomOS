using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;


namespace FathomOS.Modules.EquipmentInventory.Views;

/// <summary>
/// Modern login window with username/password and PIN authentication
/// Supports both offline (local SQLite) and online (API) authentication
/// </summary>
public partial class LoginWindow : MetroWindow
{
    private readonly AuthenticationService _authService;
    private readonly LocalDatabaseService _dbService;
    
    public bool IsAuthenticated { get; private set; }
    public User? AuthenticatedUser { get; private set; }
    
    public LoginWindow(AuthenticationService authService, LocalDatabaseService dbService)
    {
        _authService = authService;
        _dbService = dbService;
        
        InitializeComponent();
        
        // Load saved username if remember me was checked
        var settings = ModuleSettings.Load();
        if (!string.IsNullOrEmpty(settings.SavedUsername))
        {
            UsernameTextBox.Text = settings.SavedUsername;
            RememberMeCheckBox.IsChecked = true;
            PasswordBox.Focus();
        }
        else
        {
            UsernameTextBox.Focus();
        }
    }
    
    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        IsAuthenticated = false;
        DialogResult = false;
        Close();
    }
    
    private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            LoginButton_Click(sender, e);
        }
    }
    
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
        {
            ShowError("Please enter your username or email.");
            UsernameTextBox.Focus();
            return;
        }
        
        if (string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            ShowError("Please enter your password.");
            PasswordBox.Focus();
            return;
        }
        
        // Show loading
        LoadingOverlay.Visibility = Visibility.Visible;
        LoginButton.IsEnabled = false;
        HideError();
        
        try
        {
            // Use LOCAL DATABASE authentication (offline mode)
            // This works without any API server deployed
            var (success, user, error) = await _dbService.AuthenticateUserAsync(
                UsernameTextBox.Text.Trim(), 
                PasswordBox.Password);
            
            if (success && user != null)
            {
                // Check if user must change password
                if (user.MustChangePassword || user.IsPasswordExpired)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    LoginButton.IsEnabled = true;
                    
                    var changeDialog = new Dialogs.ChangePasswordDialog(_dbService, user, user.MustChangePassword);
                    changeDialog.Owner = this;
                    
                    var changeResult = changeDialog.ShowDialog();
                    
                    if (changeResult != true || !changeDialog.PasswordChanged)
                    {
                        // User cancelled or failed - cannot proceed
                        ShowError("You must change your password to continue.");
                        return;
                    }
                    
                    // Reload user after password change
                    user = await _dbService.GetUserByIdAsync(user.UserId);
                }
                
                IsAuthenticated = true;
                AuthenticatedUser = user;
                
                // Update auth service with current user for the session
                _authService.SetOfflineUser(user);
                
                // Save username if remember me is checked
                var settings = ModuleSettings.Load();
                if (RememberMeCheckBox.IsChecked == true)
                {
                    settings.SavedUsername = UsernameTextBox.Text.Trim();
                }
                else
                {
                    settings.SavedUsername = null;
                }
                settings.Save();
                
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError(error ?? "Invalid username or password.");
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Login failed: {ex.Message}");
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoginButton.IsEnabled = true;
        }
    }
    
    private async void PinLogin_Click(object sender, RoutedEventArgs e)
    {
        // Show PIN login dialog (uses local database)
        var pinDialog = new PinLoginDialog(_authService, _dbService);
        pinDialog.Owner = this;
        
        if (pinDialog.ShowDialog() == true && pinDialog.IsAuthenticated)
        {
            IsAuthenticated = true;
            AuthenticatedUser = pinDialog.AuthenticatedUser;
            DialogResult = true;
            Close();
        }
    }
    
    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }
    
    private void HideError()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
    }
}
