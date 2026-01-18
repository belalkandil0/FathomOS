using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using LicenseGeneratorUI.Services;

namespace LicenseGeneratorUI.Views;

/// <summary>
/// Optional server connection dialog for standalone mode.
/// Allows connecting to a license server for cloud sync and analytics.
/// </summary>
public partial class ServerConnectionWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private bool _connectionTested = false;
    private bool _connectionSuccessful = false;

    /// <summary>
    /// Gets whether the user chose to work offline (skipped server connection)
    /// </summary>
    public bool WorkOffline { get; private set; } = true;

    /// <summary>
    /// Gets the configured server URL (if connected)
    /// </summary>
    public string? ServerUrl { get; private set; }

    /// <summary>
    /// Gets the configured API key (if connected)
    /// </summary>
    public string? ApiKey { get; private set; }

    /// <summary>
    /// Gets whether auto-sync is enabled
    /// </summary>
    public bool AutoSyncEnabled { get; private set; }

    public ServerConnectionWindow()
    {
        InitializeComponent();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _settings = new SettingsService();

        // Load existing settings
        LoadSettings();
    }

    public ServerConnectionWindow(SettingsService settings)
    {
        InitializeComponent();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _settings = settings;

        // Load existing settings
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (!string.IsNullOrEmpty(_settings.ServerUrl))
        {
            ServerUrlInput.Text = _settings.ServerUrl;
        }

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            ApiKeyInput.Password = _settings.ApiKey;
        }

        AutoSyncCheckbox.IsChecked = _settings.AutoSyncToServer;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
        SuccessBorder.Visibility = Visibility.Collapsed;

        var serverUrl = ServerUrlInput.Text.Trim().TrimEnd('/');

        if (string.IsNullOrEmpty(serverUrl))
        {
            ShowError("Please enter a server URL.");
            return;
        }

        // Update UI to show testing
        TestButton.IsEnabled = false;
        TestButton.Content = "Testing...";
        ConnectionStatus.Text = "Testing...";
        ConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D29922")!);

        try
        {
            // Test the connection
            var response = await _httpClient.GetAsync($"{serverUrl}/api/license/time");

            if (response.IsSuccessStatusCode)
            {
                _connectionTested = true;
                _connectionSuccessful = true;

                ConnectionStatus.Text = "Connected";
                ConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950")!);
                ShowSuccess("Connection successful! Server is reachable.");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.Contains("setup_required"))
                {
                    _connectionTested = true;
                    _connectionSuccessful = false;

                    ConnectionStatus.Text = "Setup required";
                    ConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D29922")!);
                    ShowError("Server requires initial setup. Visit the server URL in a browser to complete setup.");
                }
                else
                {
                    throw new Exception($"Server returned: {response.StatusCode}");
                }
            }
            else
            {
                throw new Exception($"Server returned: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _connectionTested = true;
            _connectionSuccessful = false;

            ConnectionStatus.Text = "Failed";
            ConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149")!);
            ShowError($"Connection failed: {ex.Message}\n\nCheck the URL and ensure the server is running.");
        }
        catch (TaskCanceledException)
        {
            _connectionTested = true;
            _connectionSuccessful = false;

            ConnectionStatus.Text = "Timeout";
            ConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149")!);
            ShowError("Connection timed out. The server may be unreachable.");
        }
        catch (Exception ex)
        {
            _connectionTested = true;
            _connectionSuccessful = false;

            ConnectionStatus.Text = "Error";
            ConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149")!);
            ShowError($"Error: {ex.Message}");
        }
        finally
        {
            TestButton.IsEnabled = true;
            TestButton.Content = "Test Connection";
        }
    }

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        var serverUrl = ServerUrlInput.Text.Trim().TrimEnd('/');
        var apiKey = ApiKeyInput.Password;

        if (string.IsNullOrEmpty(serverUrl))
        {
            ShowError("Please enter a server URL.");
            return;
        }

        if (!_connectionTested || !_connectionSuccessful)
        {
            var result = MessageBox.Show(
                "The connection has not been tested or the last test failed.\n\n" +
                "Do you want to save these settings anyway?",
                "Connection Not Verified",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        // Save settings
        _settings.ServerUrl = serverUrl;
        _settings.ApiKey = apiKey;
        _settings.AutoSyncToServer = AutoSyncCheckbox.IsChecked == true;
        _settings.HasServerConfig = true;
        _settings.Save();

        // Set result properties
        WorkOffline = false;
        ServerUrl = serverUrl;
        ApiKey = apiKey;
        AutoSyncEnabled = AutoSyncCheckbox.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        WorkOffline = true;
        _settings.HasServerConfig = false;
        _settings.Save();

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
        SuccessBorder.Visibility = Visibility.Collapsed;
    }

    private void ShowSuccess(string message)
    {
        SuccessText.Text = message;
        SuccessBorder.Visibility = Visibility.Visible;
        ErrorBorder.Visibility = Visibility.Collapsed;
    }
}
