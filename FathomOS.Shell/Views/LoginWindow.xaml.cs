using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FathomOS.Core.Interfaces;

namespace FathomOS.Shell.Views;

/// <summary>
/// Login window for Fathom OS user authentication.
/// Shows username/password form with Remember Me option.
/// </summary>
public partial class LoginWindow : Window
{
    private readonly IAuthenticationService _authService;

    /// <summary>
    /// Indicates if login was successful (used by caller).
    /// </summary>
    public bool LoginSuccessful { get; private set; }

    /// <summary>
    /// Creates a new LoginWindow using the authentication service from DI.
    /// </summary>
    public LoginWindow()
    {
        InitializeComponent();

        // Get auth service from DI
        _authService = App.Services.GetService(typeof(IAuthenticationService)) as IAuthenticationService
            ?? throw new InvalidOperationException("IAuthenticationService not registered in DI container");

        // Focus username field on load
        Loaded += (s, e) => UsernameTextBox.Focus();

        // Check for remembered username
        LoadRememberedUsername();
    }

    /// <summary>
    /// Creates a new LoginWindow with an explicitly provided authentication service.
    /// </summary>
    /// <param name="authService">The authentication service to use</param>
    public LoginWindow(IAuthenticationService authService)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        // Focus username field on load
        Loaded += (s, e) => UsernameTextBox.Focus();

        // Check for remembered username
        LoadRememberedUsername();
    }

    /// <summary>
    /// Loads the remembered username from settings if available.
    /// </summary>
    private void LoadRememberedUsername()
    {
        try
        {
            var settingsService = App.Services.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService != null)
            {
                var rememberedUsername = settingsService.Get<string?>("Auth.RememberedUsername", null);
                if (!string.IsNullOrEmpty(rememberedUsername))
                {
                    UsernameTextBox.Text = rememberedUsername;
                    RememberMeCheckBox.IsChecked = true;
                    PasswordBox.Focus();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoginWindow.LoadRememberedUsername error: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the username if Remember Me is checked.
    /// </summary>
    private void SaveRememberedUsername(string? username)
    {
        try
        {
            var settingsService = App.Services.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService != null)
            {
                if (RememberMeCheckBox.IsChecked == true && !string.IsNullOrEmpty(username))
                {
                    settingsService.Set("Auth.RememberedUsername", username);
                }
                else
                {
                    settingsService.Remove("Auth.RememberedUsername");
                }
                settingsService.Save();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoginWindow.SaveRememberedUsername error: {ex.Message}");
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        await PerformLoginAsync();
    }

    private async Task PerformLoginAsync()
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username))
        {
            ShowStatus("Please enter your username.", isError: true);
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
            var result = await _authService.LoginAsync(username, password);

            if (result.Success)
            {
                // Save remembered username if checked
                SaveRememberedUsername(username);

                LoginSuccessful = true;
                DialogResult = true;
                Close();
            }
            else
            {
                ShowStatus(result.Error ?? "Login failed. Please check your credentials.", isError: true);
                PasswordBox.Clear();
                PasswordBox.Focus();
                SetControlsEnabled(true);
            }
        }
        catch (Exception ex)
        {
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

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await PerformLoginAsync();
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusBorder.Visibility = Visibility.Visible;
        StatusText.Text = message;

        if (isError)
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(60, 30, 35));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 120));
        }
        else
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(30, 50, 45));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 212, 170));
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
