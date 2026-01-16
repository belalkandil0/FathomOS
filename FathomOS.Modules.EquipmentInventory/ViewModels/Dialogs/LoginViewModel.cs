using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthenticationService _authService;
    
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _rememberMe;
    private bool? _dialogResult;
    
    public LoginViewModel(AuthenticationService authService)
    {
        _authService = authService;
        
        // Load saved username
        var settings = ModuleSettings.Load();
        _username = settings.LastUsername ?? string.Empty;
        _rememberMe = !string.IsNullOrEmpty(_username);
        
        LoginCommand = new AsyncRelayCommand(async _ => await LoginAsync(), _ => CanLogin && !IsBusy);
        CancelCommand = new RelayCommand(_ => Cancel());
    }
    
    public string Username
    {
        get => _username;
        set { SetProperty(ref _username, value); OnPropertyChanged(nameof(CanLogin)); }
    }
    
    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); OnPropertyChanged(nameof(CanLogin)); }
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }
    
    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }
    
    public bool CanLogin => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    
    public ICommand LoginCommand { get; }
    public ICommand CancelCommand { get; }
    
    private async Task LoginAsync()
    {
        if (!CanLogin) return;
        
        IsBusy = true;
        ErrorMessage = string.Empty;
        
        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            
            if (result.Success)
            {
                if (RememberMe)
                {
                    var settings = ModuleSettings.Load();
                    settings.LastUsername = Username;
                    settings.Save();
                }
                
                DialogResult = true;
            }
            else
            {
                ErrorMessage = result.Error ?? "Login failed. Please check your credentials.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void Cancel()
    {
        DialogResult = false;
    }
}
