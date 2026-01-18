using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FathomOS.Shell.Services;
using LicensingSystem.Shared;

namespace FathomOS.Shell.Views;

/// <summary>
/// Window for creating the local administrator account during first-launch setup.
/// </summary>
public partial class CreateAccountWindow : Window
{
    private readonly ILocalUserService _userService;
    private readonly LicenseFile? _license;

    /// <summary>
    /// The created user after successful account creation.
    /// </summary>
    public LocalUser? CreatedUser { get; private set; }

    /// <summary>
    /// Indicates if account was created successfully.
    /// </summary>
    public bool AccountCreated => CreatedUser != null;

    /// <summary>
    /// Creates a new CreateAccountWindow.
    /// </summary>
    public CreateAccountWindow()
    {
        InitializeComponent();

        // Get user service from DI or create one
        _userService = App.Services?.GetService(typeof(ILocalUserService)) as ILocalUserService
            ?? new LocalUserService();

        // Get license info from App
        var licenseInfo = App.Licensing?.GetLicenseDisplayInfo();
        if (licenseInfo != null)
        {
            ClientText.Text = licenseInfo.CustomerName ?? licenseInfo.CustomerEmail ?? "Licensed";
            EditionText.Text = licenseInfo.DisplayEdition;
        }

        Loaded += (s, e) => FullNameInput.Focus();
    }

    /// <summary>
    /// Creates a new CreateAccountWindow with specific services.
    /// </summary>
    /// <param name="userService">The local user service</param>
    /// <param name="license">Optional license information</param>
    public CreateAccountWindow(ILocalUserService userService, LicenseFile? license = null)
    {
        InitializeComponent();

        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _license = license;

        // Update license display
        if (license != null)
        {
            ClientText.Text = !string.IsNullOrEmpty(license.CustomerName)
                ? license.CustomerName
                : license.CustomerEmail;
            EditionText.Text = license.DisplayEdition;
        }

        Loaded += (s, e) => FullNameInput.Focus();
    }

    private void Input_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateForm();
    }

    private void Password_Changed(object sender, RoutedEventArgs e)
    {
        ValidatePasswordRequirements();
        ValidateForm();
    }

    private void ValidatePasswordRequirements()
    {
        var password = PasswordInput.Password;

        // Check length
        var hasLength = password.Length >= 8;
        UpdateRequirement(ReqLength, hasLength, "At least 8 characters");

        // Check uppercase
        var hasUppercase = password.Any(char.IsUpper);
        UpdateRequirement(ReqUppercase, hasUppercase, "At least one uppercase letter");

        // Check lowercase
        var hasLowercase = password.Any(char.IsLower);
        UpdateRequirement(ReqLowercase, hasLowercase, "At least one lowercase letter");

        // Check number
        var hasNumber = password.Any(char.IsDigit);
        UpdateRequirement(ReqNumber, hasNumber, "At least one number");
    }

    private void UpdateRequirement(TextBlock textBlock, bool isMet, string text)
    {
        if (isMet)
        {
            textBlock.Text = $"\u2713 {text}"; // Checkmark character
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(63, 185, 80)); // Green
        }
        else
        {
            textBlock.Text = $"\u2022 {text}"; // Bullet character
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)); // Gray
        }
    }

    private void ValidateForm()
    {
        var isValid = true;
        HideStatus();

        // Check full name
        if (string.IsNullOrWhiteSpace(FullNameInput.Text))
        {
            isValid = false;
        }

        // Check email
        if (string.IsNullOrWhiteSpace(EmailInput.Text) || !IsValidEmail(EmailInput.Text))
        {
            isValid = false;
        }

        // Check username
        if (string.IsNullOrWhiteSpace(UsernameInput.Text) || UsernameInput.Text.Length < 3)
        {
            isValid = false;
        }

        // Check password requirements
        var password = PasswordInput.Password;
        if (password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            isValid = false;
        }

        // Check password match
        if (PasswordInput.Password != ConfirmPasswordInput.Password)
        {
            isValid = false;
        }

        CreateAccountButton.IsEnabled = isValid;
    }

    private void CreateAccount_Click(object sender, RoutedEventArgs e)
    {
        // Final validation
        if (string.IsNullOrWhiteSpace(FullNameInput.Text))
        {
            ShowStatus("Please enter your full name.", isError: true);
            FullNameInput.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(EmailInput.Text) || !IsValidEmail(EmailInput.Text))
        {
            ShowStatus("Please enter a valid email address.", isError: true);
            EmailInput.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(UsernameInput.Text) || UsernameInput.Text.Length < 3)
        {
            ShowStatus("Username must be at least 3 characters.", isError: true);
            UsernameInput.Focus();
            return;
        }

        var password = PasswordInput.Password;
        if (password.Length < 8)
        {
            ShowStatus("Password must be at least 8 characters.", isError: true);
            PasswordInput.Focus();
            return;
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
        {
            ShowStatus("Password does not meet requirements.", isError: true);
            PasswordInput.Focus();
            return;
        }

        if (password != ConfirmPasswordInput.Password)
        {
            ShowStatus("Passwords do not match.", isError: true);
            ConfirmPasswordInput.Focus();
            return;
        }

        // Check if username already exists
        if (_userService.GetUserByUsername(UsernameInput.Text.Trim()) != null)
        {
            ShowStatus("Username already exists. Please choose a different username.", isError: true);
            UsernameInput.Focus();
            return;
        }

        // Check if email already exists
        if (_userService.GetUserByEmail(EmailInput.Text.Trim()) != null)
        {
            ShowStatus("An account with this email already exists.", isError: true);
            EmailInput.Focus();
            return;
        }

        ShowStatus("Creating account...", isError: false);
        SetControlsEnabled(false);

        try
        {
            var request = new CreateUserRequest
            {
                FullName = FullNameInput.Text.Trim(),
                Email = EmailInput.Text.Trim(),
                Username = UsernameInput.Text.Trim(),
                Password = password,
                Role = "Administrator"
            };

            CreatedUser = _userService.CreateUser(request);

            if (CreatedUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"CreateAccountWindow: Created admin user '{CreatedUser.Username}'");
                DialogResult = true;
                Close();
            }
            else
            {
                ShowStatus("Failed to create account. Please try again.", isError: true);
                SetControlsEnabled(true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error creating account: {ex.Message}", isError: true);
            SetControlsEnabled(true);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
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

    private void HideStatus()
    {
        StatusBorder.Visibility = Visibility.Collapsed;
    }

    private void SetControlsEnabled(bool enabled)
    {
        FullNameInput.IsEnabled = enabled;
        EmailInput.IsEnabled = enabled;
        UsernameInput.IsEnabled = enabled;
        PasswordInput.IsEnabled = enabled;
        ConfirmPasswordInput.IsEnabled = enabled;
        CreateAccountButton.IsEnabled = enabled;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        try
        {
            // Simple email validation regex
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
