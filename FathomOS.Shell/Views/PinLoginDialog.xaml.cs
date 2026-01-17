using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FathomOS.Core.Interfaces;

namespace FathomOS.Shell.Views;

/// <summary>
/// Quick PIN login dialog for returning users.
/// Shows user avatar/initials and requests PIN only.
/// </summary>
public partial class PinLoginDialog : Window
{
    private readonly IAuthenticationService _authService;
    private readonly string _username;

    /// <summary>
    /// Indicates if login was successful.
    /// </summary>
    public bool LoginSuccessful { get; private set; }

    /// <summary>
    /// Indicates if the user wants to switch to a different account.
    /// </summary>
    public bool SwitchUserRequested { get; private set; }

    /// <summary>
    /// Creates a new PinLoginDialog for the specified user.
    /// </summary>
    /// <param name="username">The username to authenticate</param>
    /// <param name="displayName">The display name to show (optional, defaults to username)</param>
    public PinLoginDialog(string username, string? displayName = null)
    {
        InitializeComponent();

        // Get auth service from DI
        _authService = App.Services.GetService(typeof(IAuthenticationService)) as IAuthenticationService
            ?? throw new InvalidOperationException("IAuthenticationService not registered in DI container");

        _username = username ?? throw new ArgumentNullException(nameof(username));

        // Set display info
        UsernameText.Text = displayName ?? username;
        UserInitials.Text = GetInitials(displayName ?? username);

        // Focus PIN box on load
        Loaded += (s, e) => PinBox.Focus();
    }

    /// <summary>
    /// Creates a new PinLoginDialog with an explicitly provided authentication service.
    /// </summary>
    public PinLoginDialog(IAuthenticationService authService, string username, string? displayName = null)
    {
        InitializeComponent();

        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _username = username ?? throw new ArgumentNullException(nameof(username));

        // Set display info
        UsernameText.Text = displayName ?? username;
        UserInitials.Text = GetInitials(displayName ?? username);

        // Focus PIN box on load
        Loaded += (s, e) => PinBox.Focus();
    }

    /// <summary>
    /// Gets initials from a name (e.g., "John Doe" -> "JD").
    /// </summary>
    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        return name.Length >= 2
            ? name[..2].ToUpperInvariant()
            : name.ToUpperInvariant();
    }

    private async void Unlock_Click(object sender, RoutedEventArgs e)
    {
        await PerformPinLoginAsync();
    }

    private async Task PerformPinLoginAsync()
    {
        var pin = PinBox.Password;

        if (string.IsNullOrEmpty(pin))
        {
            ShowStatus("Please enter your PIN.", isError: true);
            PinBox.Focus();
            return;
        }

        ShowStatus("Verifying...", isError: false);
        SetControlsEnabled(false);

        try
        {
            var result = await _authService.LoginWithPinAsync(_username, pin);

            if (result.Success)
            {
                LoginSuccessful = true;
                DialogResult = true;
                Close();
            }
            else
            {
                ShowStatus(result.Error ?? "Invalid PIN. Please try again.", isError: true);
                PinBox.Clear();
                PinBox.Focus();
                SetControlsEnabled(true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Authentication failed: {ex.Message}", isError: true);
            SetControlsEnabled(true);
        }
    }

    private void SwitchUser_Click(object sender, RoutedEventArgs e)
    {
        SwitchUserRequested = true;
        LoginSuccessful = false;
        DialogResult = false;
        Close();
    }

    private async void PinBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await PerformPinLoginAsync();
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void PinBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        // Hide any previous error when user starts typing
        if (StatusBorder.Visibility == Visibility.Visible)
        {
            StatusBorder.Visibility = Visibility.Collapsed;
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
        PinBox.IsEnabled = enabled;
        UnlockButton.IsEnabled = enabled;
        SwitchUserButton.IsEnabled = enabled;
    }
}
