using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FathomOS.Core.Interfaces;
using FathomOS.Shell.Services;

namespace FathomOS.Shell.Views;

/// <summary>
/// Login window for FathomOS that authenticates against local users.
/// Used for subsequent launches after first-time setup.
/// </summary>
public partial class LocalLoginWindow : Window
{
    private readonly ILocalUserService _userService;
    private readonly ISettingsService? _settingsService;

    /// <summary>
    /// Indicates if login was successful.
    /// </summary>
    public bool LoginSuccessful { get; private set; }

    /// <summary>
    /// The authenticated local user.
    /// </summary>
    public LocalUser? AuthenticatedUser { get; private set; }

    /// <summary>
    /// Creates a new LocalLoginWindow.
    /// </summary>
    public LocalLoginWindow()
    {
        InitializeComponent();

        // Get services from DI
        _userService = App.Services?.GetService(typeof(ILocalUserService)) as ILocalUserService
            ?? new LocalUserService();

        _settingsService = App.Services?.GetService(typeof(ISettingsService)) as ISettingsService;

        // Update license display
        var licenseInfo = App.Licensing?.GetLicenseDisplayInfo();
        if (licenseInfo != null)
        {
            EditionText.Text = licenseInfo.DisplayEdition;
            LicenseStatusText.Text = licenseInfo.IsValid ? "Licensed" : "Unlicensed";
        }

        // Load remembered username
        LoadRememberedUsername();

        Loaded += (s, e) =>
        {
            if (string.IsNullOrEmpty(UsernameTextBox.Text))
            {
                UsernameTextBox.Focus();
            }
            else
            {
                PasswordBox.Focus();
            }
        };
    }

    /// <summary>
    /// Creates a new LocalLoginWindow with specific services.
    /// </summary>
    /// <param name="userService">The local user service</param>
    /// <param name="settingsService">Optional settings service</param>
    public LocalLoginWindow(ILocalUserService userService, ISettingsService? settingsService = null)
    {
        InitializeComponent();

        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _settingsService = settingsService;

        // Update license display
        var licenseInfo = App.Licensing?.GetLicenseDisplayInfo();
        if (licenseInfo != null)
        {
            EditionText.Text = licenseInfo.DisplayEdition;
            LicenseStatusText.Text = licenseInfo.IsValid ? "Licensed" : "Unlicensed";
        }

        LoadRememberedUsername();

        Loaded += (s, e) =>
        {
            if (string.IsNullOrEmpty(UsernameTextBox.Text))
            {
                UsernameTextBox.Focus();
            }
            else
            {
                PasswordBox.Focus();
            }
        };
    }

    private void LoadRememberedUsername()
    {
        try
        {
            if (_settingsService != null)
            {
                var rememberedUsername = _settingsService.Get<string?>("LocalAuth.RememberedUsername", null);
                if (!string.IsNullOrEmpty(rememberedUsername))
                {
                    UsernameTextBox.Text = rememberedUsername;
                    RememberMeCheckBox.IsChecked = true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocalLoginWindow.LoadRememberedUsername error: {ex.Message}");
        }
    }

    private void SaveRememberedUsername(string? username)
    {
        try
        {
            if (_settingsService != null)
            {
                if (RememberMeCheckBox.IsChecked == true && !string.IsNullOrEmpty(username))
                {
                    _settingsService.Set("LocalAuth.RememberedUsername", username);
                }
                else
                {
                    _settingsService.Remove("LocalAuth.RememberedUsername");
                }
                _settingsService.Save();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocalLoginWindow.SaveRememberedUsername error: {ex.Message}");
        }
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        PerformLogin();
    }

    private void PerformLogin()
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username))
        {
            ShowStatus("Please enter your username or email.", isError: true);
            UsernameTextBox.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("Please enter your password.", isError: true);
            PasswordBox.Focus();
            return;
        }

        ShowStatus("Signing in...", isError: false);
        SetControlsEnabled(false);

        try
        {
            var user = _userService.Authenticate(username, password);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    ShowStatus("This account has been deactivated. Please contact an administrator.", isError: true);
                    SetControlsEnabled(true);
                    return;
                }

                // Save remembered username
                SaveRememberedUsername(user.Username);

                AuthenticatedUser = user;
                LoginSuccessful = true;

                System.Diagnostics.Debug.WriteLine($"LocalLoginWindow: User '{user.Username}' logged in successfully");

                DialogResult = true;
                Close();
            }
            else
            {
                ShowStatus("Invalid username or password. Please try again.", isError: true);
                PasswordBox.Clear();
                PasswordBox.Focus();
                SetControlsEnabled(true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocalLoginWindow.PerformLogin error: {ex.Message}");
            ShowStatus($"Login failed: {ex.Message}", isError: true);
            SetControlsEnabled(true);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        LoginSuccessful = false;
        DialogResult = false;
        Close();
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PasswordBox.Focus();
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PerformLogin();
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusBorder.Visibility = Visibility.Visible;
        StatusText.Text = message;

        if (isError)
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(45, 25, 25));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(248, 81, 73));
        }
        else
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(25, 45, 35));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(63, 185, 80));
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        UsernameTextBox.IsEnabled = enabled;
        PasswordBox.IsEnabled = enabled;
        RememberMeCheckBox.IsEnabled = enabled;
        LoginButton.IsEnabled = enabled;
    }
}
