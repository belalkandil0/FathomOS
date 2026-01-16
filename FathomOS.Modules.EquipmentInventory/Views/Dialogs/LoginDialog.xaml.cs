using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class LoginDialog : MetroWindow
{
    private readonly LoginViewModel _viewModel;
    
    public LoginDialog(AuthenticationService authService)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new LoginViewModel(authService);
        DataContext = _viewModel;
        
        // Sync password box with viewmodel
        PasswordBox.PasswordChanged += (s, e) => _viewModel.Password = PasswordBox.Password;
        
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
                Close();
            }
        };
    }
}
