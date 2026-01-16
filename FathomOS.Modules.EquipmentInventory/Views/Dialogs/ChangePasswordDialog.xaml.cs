using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class ChangePasswordDialog : MetroWindow
{
    private readonly ChangePasswordViewModel _viewModel;
    
    public ChangePasswordDialog(LocalDatabaseService dbService, User user, bool isFirstLogin = false)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new ChangePasswordViewModel(dbService, user, isFirstLogin);
        DataContext = _viewModel;
    }
    
    public bool PasswordChanged => _viewModel.PasswordChanged;
    
    private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.CurrentPassword = ((PasswordBox)sender).Password;
    }
    
    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.NewPassword = ((PasswordBox)sender).Password;
    }
    
    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
    }
    
    private async void ChangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.ChangePasswordAsync())
        {
            DialogResult = true;
            Close();
        }
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class ChangePasswordViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private readonly User _user;
    private readonly bool _isFirstLogin;
    
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    
    // Validation colors
    private static readonly SolidColorBrush GrayBrush = new(Color.FromRgb(128, 128, 128));
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(76, 175, 80));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(244, 67, 54));
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ChangePasswordViewModel(LocalDatabaseService dbService, User user, bool isFirstLogin)
    {
        _dbService = dbService;
        _user = user;
        _isFirstLogin = isFirstLogin;
    }
    
    public bool ShowCurrentPassword => !_isFirstLogin;
    public bool AllowCancel => !_isFirstLogin;
    public bool PasswordChanged { get; private set; }
    
    public string HeaderMessage => _isFirstLogin
        ? "You must change your password before continuing. This is required for first-time login."
        : "Enter your current password and choose a new password.";
    
    public string CurrentPassword
    {
        get => _currentPassword;
        set { _currentPassword = value; OnPropertyChanged(); ClearError(); }
    }
    
    public string NewPassword
    {
        get => _newPassword;
        set 
        { 
            _newPassword = value; 
            OnPropertyChanged(); 
            ClearError();
            ValidatePassword();
        }
    }
    
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set 
        { 
            _confirmPassword = value; 
            OnPropertyChanged(); 
            ClearError();
            ValidatePassword();
        }
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }
    
    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; OnPropertyChanged(); }
    }
    
    // Validation properties
    public bool HasLength => _newPassword.Length >= 8;
    public bool HasUppercase => _newPassword.Any(char.IsUpper);
    public bool HasLowercase => _newPassword.Any(char.IsLower);
    public bool HasDigit => _newPassword.Any(char.IsDigit);
    public bool HasSpecial => _newPassword.Any(c => !char.IsLetterOrDigit(c));
    public bool PasswordsMatch => !string.IsNullOrEmpty(_newPassword) && _newPassword == _confirmPassword;
    
    public bool CanChange => HasLength && HasUppercase && HasLowercase && HasDigit && HasSpecial && PasswordsMatch;
    
    // Icon properties
    public string LengthIcon => HasLength ? "CheckCircle" : "CircleOutline";
    public string UppercaseIcon => HasUppercase ? "CheckCircle" : "CircleOutline";
    public string LowercaseIcon => HasLowercase ? "CheckCircle" : "CircleOutline";
    public string DigitIcon => HasDigit ? "CheckCircle" : "CircleOutline";
    public string SpecialIcon => HasSpecial ? "CheckCircle" : "CircleOutline";
    public string MatchIcon => PasswordsMatch ? "CheckCircle" : "CircleOutline";
    
    // Color properties
    public Brush LengthColor => string.IsNullOrEmpty(_newPassword) ? GrayBrush : (HasLength ? GreenBrush : RedBrush);
    public Brush UppercaseColor => string.IsNullOrEmpty(_newPassword) ? GrayBrush : (HasUppercase ? GreenBrush : RedBrush);
    public Brush LowercaseColor => string.IsNullOrEmpty(_newPassword) ? GrayBrush : (HasLowercase ? GreenBrush : RedBrush);
    public Brush DigitColor => string.IsNullOrEmpty(_newPassword) ? GrayBrush : (HasDigit ? GreenBrush : RedBrush);
    public Brush SpecialColor => string.IsNullOrEmpty(_newPassword) ? GrayBrush : (HasSpecial ? GreenBrush : RedBrush);
    public Brush MatchColor => string.IsNullOrEmpty(_confirmPassword) ? GrayBrush : (PasswordsMatch ? GreenBrush : RedBrush);
    
    private void ValidatePassword()
    {
        OnPropertyChanged(nameof(HasLength));
        OnPropertyChanged(nameof(HasUppercase));
        OnPropertyChanged(nameof(HasLowercase));
        OnPropertyChanged(nameof(HasDigit));
        OnPropertyChanged(nameof(HasSpecial));
        OnPropertyChanged(nameof(PasswordsMatch));
        OnPropertyChanged(nameof(CanChange));
        OnPropertyChanged(nameof(LengthIcon));
        OnPropertyChanged(nameof(UppercaseIcon));
        OnPropertyChanged(nameof(LowercaseIcon));
        OnPropertyChanged(nameof(DigitIcon));
        OnPropertyChanged(nameof(SpecialIcon));
        OnPropertyChanged(nameof(MatchIcon));
        OnPropertyChanged(nameof(LengthColor));
        OnPropertyChanged(nameof(UppercaseColor));
        OnPropertyChanged(nameof(LowercaseColor));
        OnPropertyChanged(nameof(DigitColor));
        OnPropertyChanged(nameof(SpecialColor));
        OnPropertyChanged(nameof(MatchColor));
    }
    
    public async Task<bool> ChangePasswordAsync()
    {
        if (!CanChange)
        {
            ShowError("Please meet all password requirements");
            return false;
        }
        
        try
        {
            var (success, error) = await _dbService.ChangePasswordAsync(
                _user.UserId,
                _isFirstLogin ? "" : _currentPassword,
                _newPassword);
            
            if (success)
            {
                PasswordChanged = true;
                return true;
            }
            else
            {
                ShowError(error ?? "Failed to change password");
                return false;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
            return false;
        }
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
