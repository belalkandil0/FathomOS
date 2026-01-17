using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views;

/// <summary>
/// PIN login dialog for quick authentication
/// Uses local database for offline PIN verification
/// </summary>
public partial class PinLoginDialog : MetroWindow
{
    private readonly AuthenticationService _authService;
    private readonly LocalDatabaseService _dbService;
    private string _enteredPin = "";
    private readonly Ellipse[] _dots;
    
    private static readonly SolidColorBrush FilledBrush = new(
        (Color)System.Windows.Media.ColorConverter.ConvertFromString("#00D4AA")!);
    private static readonly SolidColorBrush EmptyBrush = new(
        (Color)System.Windows.Media.ColorConverter.ConvertFromString("#30FFFFFF")!);
    
    public bool IsAuthenticated { get; private set; }
    public User? AuthenticatedUser { get; private set; }
    
    public PinLoginDialog(AuthenticationService authService, LocalDatabaseService dbService)
    {
        _authService = authService;
        _dbService = dbService;
        InitializeComponent();
        
        _dots = new[] { Dot1, Dot2, Dot3, Dot4 };
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void BackspaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_enteredPin.Length > 0)
        {
            _enteredPin = _enteredPin.Substring(0, _enteredPin.Length - 1);
            UpdateDots();
            HideError();
        }
    }
    
    private async void NumberButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && _enteredPin.Length < 4)
        {
            _enteredPin += button.Content?.ToString();
            UpdateDots();
            HideError();
            
            if (_enteredPin.Length == 4)
            {
                await TryAuthenticate();
            }
        }
    }
    
    private async Task TryAuthenticate()
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        
        try
        {
            // Get username from settings (remembered user)
            var settings = ModuleSettings.Load();
            var username = settings.SavedUsername ?? "";
            
            if (string.IsNullOrEmpty(username))
            {
                ShowError("No saved username. Please login with password first.");
                _enteredPin = "";
                UpdateDots();
                return;
            }
            
            // Use LOCAL DATABASE for PIN authentication (offline mode)
            var (success, user, error) = await _dbService.AuthenticateUserWithPinAsync(username, _enteredPin);
            
            if (success && user != null)
            {
                IsAuthenticated = true;
                AuthenticatedUser = user;
                _authService.SetOfflineUser(user);
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError(error ?? "Invalid PIN. Please try again.");
                _enteredPin = "";
                UpdateDots();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
            _enteredPin = "";
            UpdateDots();
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }
    
    private void UpdateDots()
    {
        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].Fill = i < _enteredPin.Length ? FilledBrush : EmptyBrush;
        }
    }
    
    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
    
    private void HideError()
    {
        ErrorText.Visibility = Visibility.Collapsed;
    }
}
