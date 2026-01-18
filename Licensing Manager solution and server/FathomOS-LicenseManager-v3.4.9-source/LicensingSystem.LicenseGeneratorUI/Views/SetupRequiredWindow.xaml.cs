using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace LicenseGeneratorUI.Views;

/// <summary>
/// Setup wizard window that appears when the server requires first-time admin setup.
/// This window allows the Desktop UI to complete setup without needing a setup token
/// when running on the same machine (localhost).
/// </summary>
public partial class SetupRequiredWindow : Window
{
    private readonly string _serverUrl;
    private readonly HttpClient _httpClient;

    public SetupRequiredWindow(string serverUrl, HttpClient httpClient)
    {
        InitializeComponent();
        _serverUrl = serverUrl;
        _httpClient = httpClient;

        // Display the server URL in the info box
        ServerUrlText.Text = string.IsNullOrEmpty(serverUrl) ? "localhost" : new Uri(serverUrl).Host;
    }

    private void Input_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        CheckFormValidity();
    }

    private void PasswordInput_Changed(object sender, RoutedEventArgs e)
    {
        ValidatePassword();
        CheckFormValidity();
    }

    private void ConfirmPasswordInput_Changed(object sender, RoutedEventArgs e)
    {
        var password = PasswordInput.Password;
        var confirm = ConfirmPasswordInput.Password;

        if (!string.IsNullOrEmpty(confirm))
        {
            if (password != confirm)
            {
                ConfirmHint.Text = "Passwords do not match";
                ConfirmHint.Visibility = Visibility.Visible;
            }
            else
            {
                ConfirmHint.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            ConfirmHint.Visibility = Visibility.Collapsed;
        }

        CheckFormValidity();
    }

    private void ValidatePassword()
    {
        var password = PasswordInput.Password;
        var requirements = new List<string>();

        if (password.Length < 12) requirements.Add("12+ chars");
        if (!Regex.IsMatch(password, @"[A-Z]")) requirements.Add("uppercase");
        if (!Regex.IsMatch(password, @"[a-z]")) requirements.Add("lowercase");
        if (!Regex.IsMatch(password, @"[0-9]")) requirements.Add("digit");
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            requirements.Add("special char");

        if (requirements.Count > 0)
        {
            PasswordHint.Text = $"Missing: {string.Join(", ", requirements)}";
            PasswordHint.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#F85149")!);
        }
        else
        {
            PasswordHint.Text = "Password meets all requirements";
            PasswordHint.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#3FB950")!);
        }
    }

    private bool IsPasswordValid()
    {
        var password = PasswordInput.Password;
        return password.Length >= 12 &&
            Regex.IsMatch(password, @"[A-Z]") &&
            Regex.IsMatch(password, @"[a-z]") &&
            Regex.IsMatch(password, @"[0-9]") &&
            Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
    }

    private void CheckFormValidity()
    {
        var email = EmailInput.Text.Trim();
        var username = UsernameInput.Text.Trim();
        var password = PasswordInput.Password;
        var confirm = ConfirmPasswordInput.Password;

        var isValid = !string.IsNullOrEmpty(email) &&
            email.Contains("@") &&
            email.Contains(".") &&
            username.Length >= 3 &&
            IsPasswordValid() &&
            password == confirm;

        CompleteButton.IsEnabled = isValid;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void CompleteSetup_Click(object sender, RoutedEventArgs e)
    {
        CompleteButton.IsEnabled = false;
        ErrorBorder.Visibility = Visibility.Collapsed;
        SuccessBorder.Visibility = Visibility.Collapsed;

        try
        {
            var request = new
            {
                email = EmailInput.Text.Trim(),
                username = UsernameInput.Text.Trim(),
                displayName = string.IsNullOrWhiteSpace(DisplayNameInput.Text)
                    ? UsernameInput.Text.Trim()
                    : DisplayNameInput.Text.Trim(),
                password = PasswordInput.Password,
                enableTwoFactor = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/setup/ui-complete", request);

            if (response.IsSuccessStatusCode)
            {
                // Show success message
                SuccessText.Text = "Administrator account created successfully! The license server is now ready to use.";
                SuccessBorder.Visibility = Visibility.Visible;

                // Short delay to show success message
                await Task.Delay(1500);

                DialogResult = true;
                Close();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();

                // Try to extract message from JSON response
                string errorMessage = error;
                try
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(error);
                    if (jsonDoc.RootElement.TryGetProperty("message", out var msgElement))
                    {
                        errorMessage = msgElement.GetString() ?? error;
                    }
                }
                catch
                {
                    // Use raw error if JSON parsing fails
                }

                ShowError($"Setup failed: {errorMessage}");
            }
        }
        catch (HttpRequestException ex)
        {
            ShowError($"Connection error: {ex.Message}\n\nMake sure the server is running and accessible.");
        }
        catch (TaskCanceledException)
        {
            ShowError("Request timed out. The server may be unresponsive.");
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error: {ex.Message}");
        }
        finally
        {
            CheckFormValidity();  // Re-enable button if form is still valid
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
        SuccessBorder.Visibility = Visibility.Collapsed;
    }
}
