using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LicenseGeneratorUI.Services;
using LicensingSystem.Shared;

namespace LicenseGeneratorUI.Views;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private string _serverUrl = "";
    private bool _isConnected = false;
    private bool _isInitialized = false;  // Prevent events during initialization
    private List<LicenseInfo> _licenses = new();
    private string? _privateKey;
    private string? _publicKey;
    
    // Module/Tier data for Create License page
    private List<ModuleInfoFull> _allModules = new();
    private List<TierInfoFull> _allTiers = new();
    private Dictionary<string, CheckBox> _moduleCheckboxes = new();
    
    // Offline license data
    private string? _lastOfflineLicenseContent;
    private bool _isOfflineLicense = false;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            _settings = new SettingsService();
            
            _isInitialized = true;  // Mark as initialized after InitializeComponent
            
            LoadSettings();
            
            // Don't await - let it run in background
            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await ConnectToServerAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize main window:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    #region Navigation

    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        // Skip if called during InitializeComponent (controls not yet created)
        if (PageDashboard == null || PageCreate == null || PageManage == null || 
            PageKeys == null || PageModules == null || PageSettings == null ||
            PageServerAdmin == null || PageAuditLogs == null || PageCustomers == null)
            return;

        if (sender is not RadioButton rb) return;

        // Hide all pages
        PageDashboard.Visibility = Visibility.Collapsed;
        PageCreate.Visibility = Visibility.Collapsed;
        PageManage.Visibility = Visibility.Collapsed;
        PageKeys.Visibility = Visibility.Collapsed;
        PageModules.Visibility = Visibility.Collapsed;
        PageServerAdmin.Visibility = Visibility.Collapsed;
        PageAuditLogs.Visibility = Visibility.Collapsed;
        PageCustomers.Visibility = Visibility.Collapsed;
        PageSettings.Visibility = Visibility.Collapsed;

        // Show selected page
        if (rb == NavDashboard)
        {
            PageDashboard.Visibility = Visibility.Visible;
            _ = LoadLicensesAsync();
        }
        else if (rb == NavCreate)
        {
            PageCreate.Visibility = Visibility.Visible;
            _ = LoadModulesForCreateAsync();
        }
        else if (rb == NavManage)
        {
            PageManage.Visibility = Visibility.Visible;
        }
        else if (rb == NavKeys)
        {
            PageKeys.Visibility = Visibility.Visible;
            LoadStoredKeys();
        }
        else if (rb == NavModules)
        {
            PageModules.Visibility = Visibility.Visible;
            _ = LoadModulesAndTiersAsync();
        }
        else if (rb == NavServerAdmin)
        {
            PageServerAdmin.Visibility = Visibility.Visible;
            _ = LoadServerHealthAsync();
        }
        else if (rb == NavAuditLogs)
        {
            PageAuditLogs.Visibility = Visibility.Visible;
            _ = LoadAuditLogsAsync();
        }
        else if (rb == NavCustomers)
        {
            PageCustomers.Visibility = Visibility.Visible;
            _ = LoadCustomersAsync();
        }
        else if (rb == NavSettings)
        {
            PageSettings.Visibility = Visibility.Visible;
            ServerUrlInput.Text = _serverUrl;
            UpdateKeyStorageInfo();
        }
    }

    #endregion

    #region Settings & Connection

    private void LoadSettings()
    {
        _serverUrl = _settings.ServerUrl;
        _privateKey = _settings.PrivateKey;
        _publicKey = _settings.PublicKey;
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _serverUrl = ServerUrlInput.Text.Trim().TrimEnd('/');
        _settings.ServerUrl = _serverUrl;
        _settings.Save();
        
        await ConnectToServerAsync();
        MessageBox.Show(_isConnected ? "✅ Settings saved and connected!" : "⚠️ Settings saved but could not connect to server.", 
            "Settings", MessageBoxButton.OK, _isConnected ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        await ConnectToServerAsync();
        MessageBox.Show(_isConnected ? "✅ Connection successful!" : "❌ Connection failed. Check the URL.", 
            "Connection Test", MessageBoxButton.OK, _isConnected ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private async Task ConnectToServerAsync()
    {
        if (string.IsNullOrEmpty(_serverUrl))
        {
            _isConnected = false;
            UpdateServerStatus();
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/license/time");

            // Check if server returned 503 with setup_required
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.Contains("setup_required"))
                {
                    _isConnected = false;
                    UpdateServerStatus();

                    // Show setup dialog on UI thread
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ShowSetupRequiredDialog();
                    });
                    return;
                }
            }

            _isConnected = response.IsSuccessStatusCode;

            // If connected, fetch server info
            if (_isConnected)
            {
                await UpdateServerInfoAsync();
            }
        }
        catch
        {
            _isConnected = false;
        }

        UpdateServerStatus();

        if (_isConnected)
        {
            await LoadLicensesAsync();
        }
    }

    /// <summary>
    /// Show the setup required dialog when server needs first-time configuration
    /// </summary>
    private void ShowSetupRequiredDialog()
    {
        var setupWindow = new SetupRequiredWindow(_serverUrl, _httpClient);
        setupWindow.Owner = this;

        if (setupWindow.ShowDialog() == true)
        {
            // Setup completed successfully, try to connect again
            _ = Dispatcher.InvokeAsync(async () =>
            {
                await ConnectToServerAsync();
            });
        }
    }

    private async Task UpdateServerInfoAsync()
    {
        try
        {
            // Try to get server root info
            var response = await _httpClient.GetAsync(_serverUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var serverInfo = System.Text.Json.JsonSerializer.Deserialize<ServerRootInfo>(json, jsonOptions);
                
                Dispatcher.Invoke(() =>
                {
                    ServerVersionText.Text = serverInfo?.Version ?? "-";
                    ServerStatusText.Text = serverInfo?.Status ?? "-";
                    ServerInfoCard.Visibility = Visibility.Visible;
                });
            }
            
            // Get db-status for counts
            var dbResponse = await _httpClient.GetAsync($"{_serverUrl}/db-status");
            if (dbResponse.IsSuccessStatusCode)
            {
                var json = await dbResponse.Content.ReadAsStringAsync();
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dbInfo = System.Text.Json.JsonSerializer.Deserialize<DbStatusInfo>(json, jsonOptions);
                
                Dispatcher.Invoke(() =>
                {
                    ServerLicenseCountText.Text = dbInfo?.LicenseCount.ToString() ?? "-";
                    ServerActivationCountText.Text = dbInfo?.ActivationCount.ToString() ?? "-";
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateServerInfoAsync error: {ex.Message}");
        }
    }

    private void UpdateServerStatus()
    {
        Dispatcher.Invoke(() =>
        {
            if (_isConnected)
            {
                StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950"));
                ConnectionStatus.Text = "Connected";
                SettingsConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950"));
                SettingsConnectionStatus.Text = "Connected";
                ServerInfoCard.Visibility = Visibility.Visible;
            }
            else
            {
                StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149"));
                ConnectionStatus.Text = "Disconnected";
                SettingsConnectionDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149"));
                SettingsConnectionStatus.Text = "Not Connected";
                ServerInfoCard.Visibility = Visibility.Collapsed;
            }
            ServerUrlDisplay.Text = string.IsNullOrEmpty(_serverUrl) ? "No server" : _serverUrl;
        });
    }

    private void OpenPortal_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_serverUrl))
        {
            MessageBox.Show("Please configure a server URL first.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        OpenUrl($"{_serverUrl}/portal.html");
    }

    private void OpenSwagger_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_serverUrl))
        {
            MessageBox.Show("Please configure a server URL first.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        OpenUrl($"{_serverUrl}/swagger");
    }

    private async void RefreshServerInfo_Click(object sender, RoutedEventArgs e)
    {
        await UpdateServerInfoAsync();
        MessageBox.Show("Server information refreshed.", "Refreshed", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RegenerateKeys_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "⚠️ WARNING: Regenerating keys will invalidate ALL existing offline licenses!\n\n" +
            "Only do this if you suspect your keys have been compromised.\n\n" +
            "Are you sure you want to continue?",
            "Regenerate Keys", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                
                // Export private key
                var privateKeyBytes = ecdsa.ExportECPrivateKey();
                _privateKey = "-----BEGIN EC PRIVATE KEY-----\n" +
                             Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                             "\n-----END EC PRIVATE KEY-----";
                
                // Export public key
                var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
                _publicKey = "-----BEGIN PUBLIC KEY-----\n" +
                            Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                            "\n-----END PUBLIC KEY-----";

                // Save to settings
                _settings.PrivateKey = _privateKey;
                _settings.PublicKey = _publicKey;
                _settings.Save();
                
                UpdateKeyStorageInfo();
                MessageBox.Show("Keys regenerated successfully.\n\n⚠️ All existing offline licenses are now invalid.\n\nPlease update the public key on your license server.", 
                    "Keys Regenerated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to regenerate keys: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SaveDefaults_Click(object sender, RoutedEventArgs e)
    {
        _settings.DefaultEdition = (DefaultEditionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Professional";
        _settings.DefaultDuration = (DefaultDurationCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1 Year";
        _settings.DefaultBrand = DefaultBrandInput.Text.Trim();
        _settings.Save();
        MessageBox.Show("Default settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenDocs_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/belalkandil0/FathomOS/wiki");
    }

    private void ReportIssue_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/belalkandil0/FathomOS/issues");
    }

    private void OpenUrl(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateKeyStorageInfo()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FathomOS", "LicenseManager");
        KeyStorageLocation.Text = $"Keys stored in: {path}";
    }

    #endregion

    #region Dashboard - License List

    private async void RefreshLicenses_Click(object sender, RoutedEventArgs e)
    {
        await LoadLicensesAsync();
    }

    private async Task LoadLicensesAsync()
    {
        if (string.IsNullOrEmpty(_serverUrl) || !_isConnected) return;

        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/licenses");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                _licenses = JsonSerializer.Deserialize<List<LicenseInfo>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                
                UpdateLicenseList();
                UpdateStats();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load licenses: {ex.Message}");
        }
    }

    private void UpdateStats()
    {
        Dispatcher.Invoke(() =>
        {
            TotalLicenses.Text = _licenses.Count.ToString();
            ActiveLicenses.Text = _licenses.Count(l => l.Status == "Active").ToString();
            ExpiringSoon.Text = _licenses.Count(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.Now.AddDays(30) && l.Status == "Active").ToString();
            RevokedLicenses.Text = _licenses.Count(l => l.Status == "Revoked" || l.IsRevoked).ToString();
        });
    }

    private void UpdateLicenseList()
    {
        Dispatcher.Invoke(() =>
        {
            LicenseListPanel.Children.Clear();
            
            if (_licenses.Count == 0)
            {
                LicenseListPanel.Children.Add(new TextBlock 
                { 
                    Text = "No licenses found. Create your first license to get started.",
                    FontSize = 13,
                    Foreground = FindResource("TextMutedBrush") as Brush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(40)
                });
                return;
            }

            foreach (var license in _licenses.Take(50))
            {
                LicenseListPanel.Children.Add(CreateLicenseItem(license));
            }
        });
    }

    private Border CreateLicenseItem(LicenseInfo license)
    {
        var border = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161B22")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 14, 20, 14)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Info
        var info = new StackPanel();
        info.Children.Add(new TextBlock 
        { 
            Text = license.CustomerName ?? license.CustomerEmail ?? "Unknown",
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = FindResource("TextPrimaryBrush") as Brush
        });
        
        var detailsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        detailsPanel.Children.Add(new TextBlock { Text = license.Edition ?? "N/A", FontSize = 11, Foreground = FindResource("TextMutedBrush") as Brush });
        detailsPanel.Children.Add(new TextBlock { Text = " • ", FontSize = 11, Foreground = FindResource("TextMutedBrush") as Brush });
        detailsPanel.Children.Add(new TextBlock { Text = license.ExpiresAt?.ToString("yyyy-MM-dd") ?? "N/A", FontSize = 11, Foreground = FindResource("TextMutedBrush") as Brush });
        if (!string.IsNullOrEmpty(license.LicenseeCode))
        {
            detailsPanel.Children.Add(new TextBlock { Text = $" • Code: {license.LicenseeCode}", FontSize = 11, Foreground = FindResource("PrimaryBrush") as Brush });
        }
        info.Children.Add(detailsPanel);
        
        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        // Buttons
        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        
        var statusBadge = new Border
        {
            Background = license.IsRevoked 
                ? FindResource("ErrorBgBrush") as Brush 
                : (license.ExpiresAt < DateTime.Now ? FindResource("WarningBgBrush") as Brush : FindResource("SuccessBgBrush") as Brush),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 8, 0)
        };
        statusBadge.Child = new TextBlock 
        { 
            Text = license.IsRevoked ? "Revoked" : (license.ExpiresAt < DateTime.Now ? "Expired" : "Active"),
            FontSize = 10,
            FontWeight = FontWeights.Medium,
            Foreground = license.IsRevoked 
                ? FindResource("ErrorBrush") as Brush 
                : (license.ExpiresAt < DateTime.Now ? FindResource("WarningBrush") as Brush : FindResource("SuccessBrush") as Brush)
        };
        buttonsPanel.Children.Add(statusBadge);

        var viewBtn = CreateActionButton("👁", "View");
        viewBtn.Tag = license;
        viewBtn.Click += ViewLicense_Click;
        buttonsPanel.Children.Add(viewBtn);

        var editBtn = CreateActionButton("✏️", "Edit");
        editBtn.Tag = license;
        editBtn.Click += EditLicense_Click;
        buttonsPanel.Children.Add(editBtn);

        var pdfBtn = CreateActionButton("📄", "Export PDF");
        pdfBtn.Tag = license;
        pdfBtn.Click += ExportLicensePdf_Click;
        buttonsPanel.Children.Add(pdfBtn);

        var htmlBtn = CreateActionButton("🌐", "View Online Certificate");
        htmlBtn.Tag = license;
        htmlBtn.Click += ViewHtmlCertificate_Click;
        buttonsPanel.Children.Add(htmlBtn);

        if (!license.IsRevoked)
        {
            var revokeBtn = CreateActionButton("🚫", "Revoke");
            revokeBtn.Tag = license;
            revokeBtn.Click += RevokeLicense_Click;
            buttonsPanel.Children.Add(revokeBtn);
        }

        Grid.SetColumn(buttonsPanel, 1);
        grid.Children.Add(buttonsPanel);

        border.Child = grid;
        return border;
    }

    private Button CreateActionButton(string icon, string tooltip)
    {
        return new Button
        {
            Content = icon,
            ToolTip = tooltip,
            Style = FindResource("IconButton") as Style,
            Margin = new Thickness(4, 0, 0, 0)
        };
    }

    private void ViewLicense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseInfo license)
        {
            var details = $"License Key: {license.LicenseKey}\n" +
                         $"Customer: {license.CustomerName}\n" +
                         $"Email: {license.CustomerEmail}\n" +
                         $"Edition: {license.Edition}\n" +
                         $"Created: {license.CreatedAt:yyyy-MM-dd HH:mm}\n" +
                         $"Expires: {license.ExpiresAt:yyyy-MM-dd HH:mm}\n" +
                         $"Status: {(license.IsRevoked ? "Revoked" : "Active")}\n";
            
            if (!string.IsNullOrEmpty(license.Brand))
                details += $"Brand: {license.Brand}\n";
            if (!string.IsNullOrEmpty(license.LicenseeCode))
                details += $"Licensee Code: {license.LicenseeCode}\n";
            if (!string.IsNullOrEmpty(license.SupportCode))
                details += $"Support Code: {license.SupportCode}\n";

            MessageBox.Show(details, "License Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportLicensePdf_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseInfo license)
        {
            try
            {
                // Ask user for theme preference
                var themeResult = MessageBox.Show(
                    "Would you like to generate a Dark theme PDF?\n\nClick 'Yes' for Dark theme\nClick 'No' for Light theme",
                    "PDF Theme", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                
                if (themeResult == MessageBoxResult.Cancel) return;
                bool useDarkTheme = themeResult == MessageBoxResult.Yes;
                
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export License Certificate",
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"License_{license.CustomerName?.Replace(" ", "_") ?? "Certificate"}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var certData = new LicenseCertificateData
                    {
                        LicenseId = license.LicenseId ?? "",
                        // Only include LicenseKey for offline licenses
                        LicenseKey = license.IsOffline ? (license.LicenseKey ?? "") : "",
                        CustomerName = license.CustomerName ?? "",
                        CustomerEmail = license.CustomerEmail ?? "",
                        Edition = license.Edition ?? "Professional",
                        SubscriptionType = license.SubscriptionType ?? "Yearly",
                        LicenseType = license.IsOffline ? "Offline" : "Online",
                        IssuedAt = license.CreatedAt ?? DateTime.Now,
                        ExpiresAt = license.ExpiresAt ?? DateTime.Now.AddYears(1),
                        Brand = license.Brand,
                        LicenseeCode = license.LicenseeCode,
                        SupportCode = license.SupportCode,
                        Modules = license.Modules ?? new List<string>()
                    };
                    
                    if (useDarkTheme)
                        LicenseCertificateGenerator.GenerateDarkModeCertificate(saveDialog.FileName, certData);
                    else
                        LicenseCertificateGenerator.GenerateCertificate(saveDialog.FileName, certData);
                    
                    MessageBox.Show("Certificate exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ViewHtmlCertificate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseInfo license)
        {
            if (string.IsNullOrEmpty(_serverUrl) || !_isConnected)
            {
                MessageBox.Show("Please connect to a server first to view online certificates.", 
                    "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Open HTML certificate in default browser
                var certificateUrl = $"{_serverUrl}/license-certificate.html?id={license.LicenseId}";
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = certificateUrl,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open certificate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CopyHtmlCertificateUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseInfo license)
        {
            if (string.IsNullOrEmpty(_serverUrl))
            {
                MessageBox.Show("Please configure a server URL first.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var certificateUrl = $"{_serverUrl}/license-certificate.html?id={license.LicenseId}";
            System.Windows.Clipboard.SetText(certificateUrl);
            MessageBox.Show("Certificate URL copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void RevokeLicense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseInfo license)
        {
            var result = MessageBox.Show($"Are you sure you want to revoke the license for {license.CustomerName}?\n\nThis action cannot be undone.",
                "Confirm Revocation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _httpClient.PostAsync(
                        $"{_serverUrl}/api/admin/licenses/{license.Id}/revoke",
                        new StringContent(JsonSerializer.Serialize(new { reason = "Revoked via License Manager" }), 
                            Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("License revoked successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadLicensesAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to revoke: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void EditLicense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseInfo license)
        {
            try
            {
                // Fetch full license details from server
                var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/licenses/{license.Id}/details");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Failed to load license details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var licenseDetails = JsonSerializer.Deserialize<LicenseGeneratorUI.Models.LicenseEditDetailsDto>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (licenseDetails == null)
                {
                    MessageBox.Show("Failed to parse license details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Convert modules and tiers to the format expected by EditLicenseWindow
                var serverModules = _allModules.Select(m => new LicenseGeneratorUI.Models.ServerModuleInfo
                {
                    Id = m.Id,
                    ModuleId = m.ModuleId ?? "",
                    DisplayName = m.DisplayName ?? m.ModuleId ?? "",
                    Description = m.Description,
                    CertificateCode = m.CertificateCode ?? "",
                    DisplayOrder = m.DisplayOrder,
                    Icon = m.Icon
                }).ToList();

                var serverTiers = _allTiers.Select(t => new LicenseGeneratorUI.Models.ServerTierInfo
                {
                    Id = t.Id,
                    TierId = t.TierId ?? "",
                    DisplayName = t.DisplayName ?? t.TierId ?? "",
                    Description = t.Description,
                    DisplayOrder = t.DisplayOrder,
                    Modules = t.Modules?.Select(m => new LicenseGeneratorUI.Models.TierModuleRef { ModuleId = m.ModuleId ?? "" }).ToList() 
                              ?? new List<LicenseGeneratorUI.Models.TierModuleRef>()
                }).ToList();

                var editWindow = new EditLicenseWindow(
                    licenseDetails,
                    serverModules,
                    serverTiers,
                    _serverUrl,
                    _httpClient);

                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true)
                {
                    // Refresh the license list
                    await LoadLicensesAsync();
                    MessageBox.Show("License updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing license: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Create License

    private async Task LoadModulesForCreateAsync()
    {
        if (!_isConnected || string.IsNullOrEmpty(_serverUrl))
        {
            Dispatcher.Invoke(() =>
            {
                ModuleCheckboxPanel.Children.Clear();
                ModuleCheckboxPanel.Children.Add(new TextBlock
                {
                    Text = "Connect to server to load modules",
                    FontSize = 12,
                    Foreground = FindResource("TextMutedBrush") as Brush,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            });
            return;
        }

        try
        {
            // Load modules
            var modulesResponse = await _httpClient.GetAsync($"{_serverUrl}/api/admin/modules");
            if (modulesResponse.IsSuccessStatusCode)
            {
                var json = await modulesResponse.Content.ReadAsStringAsync();
                _allModules = JsonSerializer.Deserialize<List<ModuleInfoFull>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }

            // Load tiers
            var tiersResponse = await _httpClient.GetAsync($"{_serverUrl}/api/admin/tiers");
            if (tiersResponse.IsSuccessStatusCode)
            {
                var json = await tiersResponse.Content.ReadAsStringAsync();
                _allTiers = JsonSerializer.Deserialize<List<TierInfoFull>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }

            BuildModuleCheckboxes();
            ApplyTierSelection();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading modules for create: {ex.Message}");
        }
    }

    private void BuildModuleCheckboxes()
    {
        Dispatcher.Invoke(() =>
        {
            ModuleCheckboxPanel.Children.Clear();
            _moduleCheckboxes.Clear();

            if (_allModules.Count == 0)
            {
                ModuleCheckboxPanel.Children.Add(new TextBlock
                {
                    Text = "No modules available. Add modules in the Modules & Tiers page.",
                    FontSize = 12,
                    Foreground = FindResource("TextMutedBrush") as Brush,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            foreach (var module in _allModules.OrderBy(m => m.DisplayOrder))
            {
                var checkbox = new CheckBox
                {
                    Content = $"{module.Icon ?? "📦"} {module.DisplayName} ({module.CertificateCode ?? "??"})",
                    Tag = module.ModuleId,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    Margin = new Thickness(0, 4, 0, 4),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                checkbox.Checked += ModuleCheckbox_Changed;
                checkbox.Unchecked += ModuleCheckbox_Changed;
                
                _moduleCheckboxes[module.ModuleId ?? ""] = checkbox;
                ModuleCheckboxPanel.Children.Add(checkbox);
            }
        });
    }

    private void ModuleCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        UpdateFeaturesOutput();
    }

    private void TierCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        ApplyTierSelection();
    }

    private void ApplyTierSelection()
    {
        if (!_isInitialized) return;
        if (TierCombo?.SelectedItem is not ComboBoxItem selectedItem) return;
        if (_moduleCheckboxes == null || _moduleCheckboxes.Count == 0) return;
        
        var tierTag = selectedItem.Tag?.ToString() ?? "Professional";

        Dispatcher.Invoke(() =>
        {
            bool isCustom = tierTag == "Custom";
            
            // Enable/disable checkboxes based on tier
            foreach (var kvp in _moduleCheckboxes)
            {
                kvp.Value.IsEnabled = isCustom;
            }

            if (!isCustom)
            {
                // Find tier and apply module selection
                var tier = _allTiers?.FirstOrDefault(t => 
                    t.TierId?.Equals(tierTag, StringComparison.OrdinalIgnoreCase) == true ||
                    t.DisplayName?.Equals(tierTag, StringComparison.OrdinalIgnoreCase) == true);

                // First clear all
                foreach (var kvp in _moduleCheckboxes)
                {
                    kvp.Value.IsChecked = false;
                }

                if (tier?.Modules != null && tier.Modules.Count > 0)
                {
                    // Select modules from tier
                    foreach (var module in tier.Modules)
                    {
                        if (_moduleCheckboxes.TryGetValue(module.ModuleId ?? "", out var checkbox))
                        {
                            checkbox.IsChecked = true;
                        }
                    }
                }
                else
                {
                    // Fallback: select based on tier defaults
                    ApplyDefaultTierSelection(tierTag);
                }

                if (ModuleSelectionHint != null)
                    ModuleSelectionHint.Text = $"Modules auto-selected for {tierTag} tier";
            }
            else
            {
                if (ModuleSelectionHint != null)
                    ModuleSelectionHint.Text = "Custom tier - select modules manually";
            }

            UpdateFeaturesOutput();
        });
    }

    private void ApplyDefaultTierSelection(string tier)
    {
        if (_moduleCheckboxes == null || _allModules == null) return;
        
        // Default module selection based on tier
        foreach (var kvp in _moduleCheckboxes)
        {
            var module = _allModules.FirstOrDefault(m => m.ModuleId == kvp.Key);
            if (module == null) continue;

            bool shouldSelect = tier switch
            {
                "Basic" => module.DefaultTier == "Basic" || module.ModuleId == "SurveyListing",
                "Professional" => module.DefaultTier != "Enterprise",
                "Enterprise" => true,
                _ => false
            };

            kvp.Value.IsChecked = shouldSelect;
        }
    }

    private void SelectAllModules_Click(object sender, RoutedEventArgs e)
    {
        if (_moduleCheckboxes == null) return;
        
        foreach (var checkbox in _moduleCheckboxes.Values)
        {
            if (checkbox.IsEnabled)
                checkbox.IsChecked = true;
        }
        UpdateFeaturesOutput();
    }

    private void ClearModules_Click(object sender, RoutedEventArgs e)
    {
        if (_moduleCheckboxes == null) return;
        
        foreach (var checkbox in _moduleCheckboxes.Values)
        {
            if (checkbox.IsEnabled)
                checkbox.IsChecked = false;
        }
        UpdateFeaturesOutput();
    }

    private void UpdateFeaturesOutput()
    {
        if (!_isInitialized) return;
        if (_moduleCheckboxes == null) return;
        
        Dispatcher.Invoke(() =>
        {
            var features = new List<string>();

            // Add tier
            if (TierCombo?.SelectedItem is ComboBoxItem selectedTier)
            {
                features.Add($"Tier:{selectedTier.Tag}");
            }

            // Add selected modules
            foreach (var kvp in _moduleCheckboxes)
            {
                if (kvp.Value.IsChecked == true)
                {
                    features.Add($"Module:{kvp.Key}");
                }
            }

            if (FeaturesOutput != null)
                FeaturesOutput.Text = string.Join(",", features);
        });
    }

    private string GetSelectedEdition()
    {
        if (TierCombo?.SelectedItem is ComboBoxItem item)
        {
            return item.Tag?.ToString() ?? "Professional";
        }
        return "Professional";
    }

    private List<string> GetSelectedFeatures()
    {
        var features = new List<string>();
        
        // Add tier
        features.Add($"Tier:{GetSelectedEdition()}");
        
        // Add selected modules
        if (_moduleCheckboxes != null)
        {
            foreach (var kvp in _moduleCheckboxes)
            {
                if (kvp.Value.IsChecked == true)
                {
                    features.Add($"Module:{kvp.Key}");
                }
            }
        }
        
        return features;
    }

    private async void LicenseeCodeInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        if (LicenseeCodeInput == null || LicenseeCodeStatus == null) return;
        
        var code = LicenseeCodeInput.Text.Trim().ToUpperInvariant();
        
        if (string.IsNullOrEmpty(code))
        {
            LicenseeCodeStatus.Text = "";
            return;
        }

        if (code.Length < 2)
        {
            LicenseeCodeStatus.Text = "Enter 2 characters";
            LicenseeCodeStatus.Foreground = FindResource("TextMutedBrush") as Brush;
            return;
        }

        // Validate format
        foreach (var c in code)
        {
            if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
            {
                LicenseeCodeStatus.Text = "❌ Only A-Z and 0-9 allowed";
                LicenseeCodeStatus.Foreground = FindResource("ErrorBrush") as Brush;
                return;
            }
        }

        // Check availability on server
        if (_isConnected)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/branding/check-code/{code}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LicenseeCodeCheckResult>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result?.IsAvailable == true)
                    {
                        LicenseeCodeStatus.Text = $"✅ Code '{code}' is available";
                        LicenseeCodeStatus.Foreground = FindResource("SuccessBrush") as Brush;
                    }
                    else
                    {
                        LicenseeCodeStatus.Text = $"❌ Code '{code}' is already in use";
                        LicenseeCodeStatus.Foreground = FindResource("ErrorBrush") as Brush;
                    }
                }
            }
            catch
            {
                LicenseeCodeStatus.Text = "⚠️ Could not verify code";
                LicenseeCodeStatus.Foreground = FindResource("WarningBrush") as Brush;
            }
        }
        else
        {
            LicenseeCodeStatus.Text = "⚠️ Connect to server to verify";
            LicenseeCodeStatus.Foreground = FindResource("WarningBrush") as Brush;
        }
    }

    #endregion

    #region License Type Selection

    private void OnlineLicense_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OnlineLicenseRadio.IsChecked = true;
    }

    private void OfflineLicense_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OfflineLicenseRadio.IsChecked = true;
    }

    private void LicenseType_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        
        _isOfflineLicense = OfflineLicenseRadio?.IsChecked == true;
        
        // Update UI
        if (OnlineLicenseOption != null && OfflineLicenseOption != null && HardwareIdSection != null)
        {
            if (_isOfflineLicense)
            {
                OnlineLicenseOption.BorderBrush = FindResource("BorderBrush") as Brush;
                OnlineLicenseOption.BorderThickness = new Thickness(1);
                OfflineLicenseOption.BorderBrush = FindResource("AccentBrush") as Brush;
                OfflineLicenseOption.BorderThickness = new Thickness(2);
                HardwareIdSection.Visibility = Visibility.Visible;
            }
            else
            {
                OnlineLicenseOption.BorderBrush = FindResource("AccentBrush") as Brush;
                OnlineLicenseOption.BorderThickness = new Thickness(2);
                OfflineLicenseOption.BorderBrush = FindResource("BorderBrush") as Brush;
                OfflineLicenseOption.BorderThickness = new Thickness(1);
                HardwareIdSection.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void GenerateSupportCode_Click(object sender, RoutedEventArgs e)
    {
        // Generate support code in format: SUP-XX-XXXXX
        var licenseeCode = LicenseeCodeInput?.Text?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(licenseeCode) || licenseeCode.Length != 2)
        {
            licenseeCode = "00";
        }
        
        var random = new Random();
        var randomPart = new string(Enumerable.Range(0, 5)
            .Select(_ => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[random.Next(32)])
            .ToArray());
        
        var supportCode = $"SUP-{licenseeCode}-{randomPart}";
        
        if (SupportCodeInput != null)
        {
            SupportCodeInput.Text = supportCode;
        }
    }

    private void SaveLicenseFile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_lastOfflineLicenseContent))
        {
            MessageBox.Show("No license file to save.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Save License File",
            Filter = "License Files (*.lic)|*.lic",
            FileName = $"FathomOS_License_{DateTime.Now:yyyyMMdd}.lic"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(saveDialog.FileName, _lastOfflineLicenseContent);
                MessageBox.Show($"License file saved to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save license file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SavePrivateKey_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_privateKey))
        {
            MessageBox.Show("No private key to save. Generate keys first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Save Private Key",
            Filter = "PEM Files (*.pem)|*.pem|All Files (*.*)|*.*",
            FileName = "private_key.pem"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(saveDialog.FileName, _privateKey);
                MessageBox.Show($"Private key saved to:\n{saveDialog.FileName}\n\n⚠️ Keep this file secure!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save private key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SavePublicKey_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_publicKey))
        {
            MessageBox.Show("No public key to save. Generate keys first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Save Public Key",
            Filter = "PEM Files (*.pem)|*.pem|All Files (*.*)|*.*",
            FileName = "public_key.pem"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(saveDialog.FileName, _publicKey);
                MessageBox.Show($"Public key saved to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save public key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Create License

    private async void CreateLicense_Click(object sender, RoutedEventArgs e)
    {
        await CreateLicenseAsync(exportPdf: false);
    }

    private async void CreateLicenseWithPdf_Click(object sender, RoutedEventArgs e)
    {
        await CreateLicenseAsync(exportPdf: true);
    }

    private async Task CreateLicenseAsync(bool exportPdf)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(CustomerNameInput.Text))
        {
            MessageBox.Show("Customer name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            CustomerNameInput.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerEmailInput.Text))
        {
            MessageBox.Show("Customer email is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            CustomerEmailInput.Focus();
            return;
        }

        // Validate Hardware ID for offline licenses
        if (_isOfflineLicense && string.IsNullOrWhiteSpace(HardwareIdInput?.Text))
        {
            MessageBox.Show("Hardware ID is required for offline licenses.\n\nCustomer can find this in:\nFathom OS → Help → License Info → Copy Hardware ID", 
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            HardwareIdInput?.Focus();
            return;
        }

        if (!_isConnected)
        {
            MessageBox.Show("Not connected to server. Please configure server in Settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var subscription = (SubscriptionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Yearly";
            int.TryParse(DurationInput.Text, out var duration);
            if (duration <= 0) duration = 12;

            var supportCode = SupportCodeInput?.Text?.Trim();
            if (string.IsNullOrEmpty(supportCode))
            {
                // Auto-generate if not set
                GenerateSupportCode_Click(null!, null!);
                supportCode = SupportCodeInput?.Text?.Trim();
            }

            var request = new
            {
                customerName = CustomerNameInput.Text.Trim(),
                customerEmail = CustomerEmailInput.Text.Trim(),
                edition = GetSelectedEdition(),
                subscriptionType = subscription,
                durationMonths = duration,
                features = GetSelectedFeatures(),
                brand = string.IsNullOrWhiteSpace(BrandNameInput.Text) ? null : BrandNameInput.Text.Trim(),
                licenseeCode = string.IsNullOrWhiteSpace(LicenseeCodeInput.Text) ? null : LicenseeCodeInput.Text.Trim().ToUpperInvariant(),
                supportCode = supportCode,
                licenseType = _isOfflineLicense ? "Offline" : "Online",
                hardwareId = _isOfflineLicense ? HardwareIdInput?.Text?.Trim() : null
            };

            var response = await _httpClient.PostAsync(
                $"{_serverUrl}/api/admin/licenses",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CreateLicenseResult>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                ResultPreview.Visibility = Visibility.Visible;
                
                var licenseType = _isOfflineLicense ? "OFFLINE" : "ONLINE";
                var resultText = $"✅ {licenseType} LICENSE CREATED\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                $"License Key: {result?.LicenseKey}\n" +
                                $"Support Code: {result?.SupportCode ?? supportCode ?? "N/A"}\n" +
                                $"Type: {licenseType}\n" +
                                $"Expires: {result?.ExpiresAt:yyyy-MM-dd}\n\n" +
                                $"Features: {string.Join(", ", GetSelectedFeatures())}";

                if (_isOfflineLicense)
                {
                    resultText += $"\n\nHardware ID: {request.hardwareId}";
                    resultText += "\n\n⚠️ Click 'Save .lic File' to download the license file for the customer.";
                    
                    // Store the offline license content
                    _lastOfflineLicenseContent = result?.LicenseFileContent;
                    SaveLicenseFileButton.Visibility = Visibility.Visible;
                }
                else
                {
                    _lastOfflineLicenseContent = null;
                    SaveLicenseFileButton.Visibility = Visibility.Collapsed;
                }

                ResultTextBox.Text = resultText;

                if (exportPdf && result != null)
                {
                    ExportPdfCertificate(result, request.customerName, request.customerEmail, request.edition, 
                        subscription, request.brand, request.licenseeCode, result.SupportCode ?? supportCode, 
                        GetSelectedFeatures(), _isOfflineLicense);
                }

                // Refresh dashboard
                await LoadLicensesAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Failed to create license:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating license: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportPdfCertificate(CreateLicenseResult result, string customerName, string customerEmail, 
        string edition, string subscriptionType, string? brand, string? licenseeCode, string? supportCode, 
        List<string> modules, bool isOffline = false)
    {
        try
        {
            // Ask user for theme preference
            var themeResult = MessageBox.Show(
                "Would you like to generate a Dark theme PDF?\n\nClick 'Yes' for Dark theme\nClick 'No' for Light theme",
                "PDF Theme", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            
            if (themeResult == MessageBoxResult.Cancel) return;
            bool useDarkTheme = themeResult == MessageBoxResult.Yes;
            
            var saveDialog = new SaveFileDialog
            {
                Title = "Save License Certificate",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"License_{customerName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var certData = new LicenseCertificateData
                {
                    LicenseId = result.LicenseId ?? "",
                    // Only include LicenseKey for offline licenses
                    LicenseKey = isOffline ? (result.LicenseKey ?? "") : "",
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    Edition = edition,
                    SubscriptionType = subscriptionType,
                    LicenseType = isOffline ? "Offline" : "Online",
                    IssuedAt = DateTime.Now,
                    ExpiresAt = result.ExpiresAt ?? DateTime.Now.AddYears(1),
                    Brand = brand,
                    LicenseeCode = licenseeCode,
                    SupportCode = supportCode,
                    Modules = modules
                };
                
                if (useDarkTheme)
                    LicenseCertificateGenerator.GenerateDarkModeCertificate(saveDialog.FileName, certData);
                else
                    LicenseCertificateGenerator.GenerateCertificate(saveDialog.FileName, certData);
                    
                MessageBox.Show($"Certificate saved to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to generate PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyResult_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ResultTextBox.Text))
        {
            Clipboard.SetText(ResultTextBox.Text);
            MessageBox.Show("Copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region Manage Licenses

    private void OpenLicenseFile_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Open License File",
            Filter = "License Files (*.lic)|*.lic|All Files (*.*)|*.*"
        };

        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var content = File.ReadAllText(openDialog.FileName);
                var license = JsonSerializer.Deserialize<LicenseFile>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (license != null)
                {
                    LoadedLicenseInfo.Visibility = Visibility.Visible;
                    LoadedLicenseText.Text = $"License ID: {license.LicenseId}\n" +
                                            $"Customer: {license.CustomerName}\n" +
                                            $"Email: {license.CustomerEmail}\n" +
                                            $"Edition: {license.Edition}\n" +
                                            $"Expires: {license.ExpiresAt:yyyy-MM-dd}\n" +
                                            $"Valid: {(license.ExpiresAt > DateTime.Now ? "Yes" : "No")}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read license file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Generate Keys

    private void LoadStoredKeys()
    {
        if (!string.IsNullOrEmpty(_privateKey) && !string.IsNullOrEmpty(_publicKey))
        {
            PrivateKeyBox.Text = _privateKey;
            PublicKeyBox.Text = _publicKey;
            KeysPanel.Visibility = Visibility.Visible;
        }
    }

    private void GenerateKeys_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            
            // Export private key
            var privateKeyBytes = ecdsa.ExportECPrivateKey();
            _privateKey = "-----BEGIN EC PRIVATE KEY-----\n" +
                         Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                         "\n-----END EC PRIVATE KEY-----";
            
            // Export public key
            var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
            _publicKey = "-----BEGIN PUBLIC KEY-----\n" +
                        Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                        "\n-----END PUBLIC KEY-----";

            PrivateKeyBox.Text = _privateKey;
            PublicKeyBox.Text = _publicKey;
            KeysPanel.Visibility = Visibility.Visible;

            // Save to settings
            _settings.PrivateKey = _privateKey;
            _settings.PublicKey = _publicKey;
            _settings.Save();

            MessageBox.Show("Keys generated successfully!\n\n⚠️ IMPORTANT: Keep the private key SECRET!\nOnly share the public key.", 
                "Keys Generated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to generate keys: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadKeys_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Load Private Key",
            Filter = "PEM Files (*.pem)|*.pem|All Files (*.*)|*.*"
        };

        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var keyContent = File.ReadAllText(openDialog.FileName);
                _privateKey = keyContent;
                PrivateKeyBox.Text = keyContent;
                KeysPanel.Visibility = Visibility.Visible;
                
                _settings.PrivateKey = _privateKey;
                _settings.Save();
                
                MessageBox.Show("Private key loaded!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CopyPrivateKey_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(PrivateKeyBox.Text))
        {
            Clipboard.SetText(PrivateKeyBox.Text);
            MessageBox.Show("Private key copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CopyPublicKey_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(PublicKeyBox.Text))
        {
            Clipboard.SetText(PublicKeyBox.Text);
            MessageBox.Show("Public key copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportPrivateKey_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_privateKey))
        {
            MessageBox.Show("No private key to export. Generate keys first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Export Private Key",
            Filter = "PEM Files (*.pem)|*.pem",
            FileName = "private_key.pem"
        };

        if (saveDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveDialog.FileName, _privateKey);
            MessageBox.Show("Private key exported!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportPublicKey_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_publicKey))
        {
            MessageBox.Show("No public key to export. Generate keys first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Export Public Key",
            Filter = "PEM Files (*.pem)|*.pem",
            FileName = "public_key.pem"
        };

        if (saveDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveDialog.FileName, _publicKey);
            MessageBox.Show("Public key exported!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region Modules & Tiers Management

    private async void RefreshModules_Click(object sender, RoutedEventArgs e)
    {
        await LoadModulesAndTiersAsync();
    }

    private async void RefreshTiers_Click(object sender, RoutedEventArgs e)
    {
        await LoadModulesAndTiersAsync();
    }

    private async Task LoadModulesAndTiersAsync()
    {
        if (!_isConnected) 
        {
            Dispatcher.Invoke(() =>
            {
                ModulesListPanel.Children.Clear();
                ModulesListPanel.Children.Add(new TextBlock 
                { 
                    Text = "Connect to server to manage modules.", 
                    FontSize = 12, 
                    Foreground = FindResource("TextMutedBrush") as Brush, 
                    Margin = new Thickness(0, 8, 0, 8) 
                });
                
                TiersListPanel.Children.Clear();
                TiersListPanel.Children.Add(new TextBlock 
                { 
                    Text = "Connect to server to manage tiers.", 
                    FontSize = 12, 
                    Foreground = FindResource("TextMutedBrush") as Brush, 
                    Margin = new Thickness(0, 8, 0, 8) 
                });
            });
            return;
        }

        try
        {
            // Load modules
            var modulesResponse = await _httpClient.GetAsync($"{_serverUrl}/api/admin/modules");
            if (modulesResponse.IsSuccessStatusCode)
            {
                var json = await modulesResponse.Content.ReadAsStringAsync();
                var modules = JsonSerializer.Deserialize<List<ModuleInfoFull>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                
                _allModules = modules;
                
                Dispatcher.Invoke(() =>
                {
                    ModulesListPanel.Children.Clear();
                    if (modules.Count == 0)
                    {
                        ModulesListPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No modules configured. Click 'Add Module' to create one.", 
                            FontSize = 12, 
                            Foreground = FindResource("TextMutedBrush") as Brush, 
                            Margin = new Thickness(0, 8, 0, 8) 
                        });
                    }
                    else
                    {
                        foreach (var module in modules)
                        {
                            ModulesListPanel.Children.Add(CreateModuleItem(module));
                        }
                    }
                });
            }

            // Load tiers
            var tiersResponse = await _httpClient.GetAsync($"{_serverUrl}/api/admin/tiers");
            if (tiersResponse.IsSuccessStatusCode)
            {
                var json = await tiersResponse.Content.ReadAsStringAsync();
                var tiers = JsonSerializer.Deserialize<List<TierInfoFull>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                
                _allTiers = tiers;
                
                Dispatcher.Invoke(() =>
                {
                    TiersListPanel.Children.Clear();
                    if (tiers.Count == 0)
                    {
                        TiersListPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No tiers configured.", 
                            FontSize = 12, 
                            Foreground = FindResource("TextMutedBrush") as Brush, 
                            Margin = new Thickness(0, 8, 0, 8) 
                        });
                    }
                    else
                    {
                        foreach (var tier in tiers)
                        {
                            TiersListPanel.Children.Add(CreateTierItem(tier));
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading modules/tiers: {ex.Message}");
            MessageBox.Show($"Failed to load data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Border CreateModuleItem(ModuleInfoFull module)
    {
        var border = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock 
        { 
            Text = module.Icon ?? "📦", 
            FontSize = 16, 
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center 
        });
        headerPanel.Children.Add(new TextBlock 
        { 
            Text = module.DisplayName ?? module.ModuleId, 
            FontSize = 14, 
            FontWeight = FontWeights.Medium, 
            Foreground = FindResource("TextPrimaryBrush") as Brush 
        });
        info.Children.Add(headerPanel);
        
        var detailsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 4, 0, 0) };
        detailsPanel.Children.Add(new TextBlock 
        { 
            Text = $"Code: {module.CertificateCode ?? "N/A"}", 
            FontSize = 11, 
            Foreground = FindResource("PrimaryBrush") as Brush 
        });
        detailsPanel.Children.Add(new TextBlock 
        { 
            Text = $" • ID: {module.ModuleId}", 
            FontSize = 11, 
            Foreground = FindResource("TextMutedBrush") as Brush 
        });
        if (!string.IsNullOrEmpty(module.DefaultTier))
        {
            detailsPanel.Children.Add(new TextBlock 
            { 
                Text = $" • Default: {module.DefaultTier}", 
                FontSize = 11, 
                Foreground = FindResource("TextMutedBrush") as Brush 
            });
        }
        info.Children.Add(detailsPanel);
        
        if (!string.IsNullOrEmpty(module.Description))
        {
            info.Children.Add(new TextBlock 
            { 
                Text = module.Description, 
                FontSize = 11, 
                Foreground = FindResource("TextMutedBrush") as Brush,
                Margin = new Thickness(24, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }
        
        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        // Action buttons
        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        
        var editBtn = new Button
        {
            Content = "✏️ Edit",
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363D")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = module
        };
        editBtn.Click += EditModule_Click;
        buttonsPanel.Children.Add(editBtn);
        
        var deleteBtn = new Button
        {
            Content = "🗑️ Delete",
            Padding = new Thickness(10, 6, 10, 6),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D1F1F")),
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149")),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = module
        };
        deleteBtn.Click += DeleteModule_Click;
        buttonsPanel.Children.Add(deleteBtn);
        
        Grid.SetColumn(buttonsPanel, 1);
        grid.Children.Add(buttonsPanel);

        border.Child = grid;
        return border;
    }

    private Border CreateTierItem(TierInfoFull tier)
    {
        var border = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        info.Children.Add(new TextBlock 
        { 
            Text = tier.DisplayName ?? tier.TierId, 
            FontSize = 14, 
            FontWeight = FontWeights.Medium, 
            Foreground = FindResource("TextPrimaryBrush") as Brush 
        });
        
        var moduleCount = tier.Modules?.Count ?? 0;
        info.Children.Add(new TextBlock 
        { 
            Text = $"{moduleCount} module{(moduleCount != 1 ? "s" : "")} included", 
            FontSize = 11, 
            Foreground = FindResource("TextMutedBrush") as Brush,
            Margin = new Thickness(0, 4, 0, 0)
        });
        
        if (!string.IsNullOrEmpty(tier.Description))
        {
            info.Children.Add(new TextBlock 
            { 
                Text = tier.Description, 
                FontSize = 11, 
                Foreground = FindResource("TextMutedBrush") as Brush,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }
        
        // Show included modules
        if (tier.Modules?.Any() == true)
        {
            var moduleNames = string.Join(", ", tier.Modules.Take(5).Select(m => m.DisplayName ?? m.ModuleId));
            if (tier.Modules.Count > 5)
                moduleNames += $" +{tier.Modules.Count - 5} more";
            
            info.Children.Add(new TextBlock 
            { 
                Text = $"Modules: {moduleNames}", 
                FontSize = 10, 
                Foreground = FindResource("PrimaryBrush") as Brush,
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }
        
        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        // Edit button
        var editBtn = new Button
        {
            Content = "✏️ Edit Modules",
            Padding = new Thickness(10, 6, 10, 6),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363D")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = tier
        };
        editBtn.Click += EditTier_Click;
        Grid.SetColumn(editBtn, 1);
        grid.Children.Add(editBtn);

        border.Child = grid;
        return border;
    }

    private async void AddModule_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ModuleDialog();
        dialog.Owner = this;
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var request = new
                {
                    moduleId = dialog.ModuleId,
                    displayName = dialog.ModuleDisplayName,
                    certificateCode = dialog.CertificateCode,
                    description = string.IsNullOrEmpty(dialog.ModuleDescription) ? null : dialog.ModuleDescription,
                    icon = string.IsNullOrEmpty(dialog.ModuleIcon) ? null : dialog.ModuleIcon,
                    defaultTier = dialog.DefaultTier
                };

                var response = await _httpClient.PostAsync(
                    $"{_serverUrl}/api/admin/modules",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Module '{dialog.ModuleDisplayName}' created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadModulesAndTiersAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to create module:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void EditModule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ModuleInfoFull module)
        {
            var dialog = new ModuleDialog();
            dialog.Owner = this;
            dialog.SetEditMode(module.Id, module.ModuleId ?? "", module.DisplayName ?? "", 
                module.CertificateCode ?? "", module.Description, module.Icon, module.DefaultTier);
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var request = new
                    {
                        displayName = dialog.ModuleDisplayName,
                        certificateCode = dialog.CertificateCode,
                        description = string.IsNullOrEmpty(dialog.ModuleDescription) ? null : dialog.ModuleDescription,
                        icon = string.IsNullOrEmpty(dialog.ModuleIcon) ? null : dialog.ModuleIcon,
                        defaultTier = dialog.DefaultTier
                    };

                    var response = await _httpClient.PutAsync(
                        $"{_serverUrl}/api/admin/modules/{module.Id}",
                        new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Module '{dialog.ModuleDisplayName}' updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadModulesAndTiersAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to update module:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void DeleteModule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ModuleInfoFull module)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the module '{module.DisplayName}'?\n\nThis will deactivate the module (soft delete).",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"{_serverUrl}/api/admin/modules/{module.Id}");

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Module '{module.DisplayName}' deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadModulesAndTiersAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to delete module:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void EditTier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TierInfoFull tier)
        {
            var dialog = new TierDialog();
            dialog.Owner = this;
            
            var allModuleItems = _allModules.Select(m => new TierModuleItem
            {
                ModuleId = m.ModuleId ?? "",
                DisplayName = m.DisplayName ?? m.ModuleId ?? "",
                CertificateCode = m.CertificateCode,
                Icon = m.Icon
            }).ToList();
            
            var currentModuleIds = tier.Modules?.Select(m => m.ModuleId ?? "").ToList() ?? new List<string>();
            
            dialog.Initialize(tier.TierId ?? "", tier.DisplayName ?? "", allModuleItems, currentModuleIds);
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var request = new { moduleIds = dialog.SelectedModuleIds };

                    var response = await _httpClient.PutAsync(
                        $"{_serverUrl}/api/admin/tiers/{tier.TierId}/modules",
                        new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Tier '{tier.DisplayName}' modules updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadModulesAndTiersAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to update tier:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    #endregion

    #region Server Admin

    private async Task LoadServerHealthAsync()
    {
        if (!_isConnected || string.IsNullOrEmpty(_serverUrl))
        {
            Dispatcher.Invoke(() =>
            {
                HealthStatus.Text = "Not Connected";
                HealthStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149"));
                ServerUptime.Text = "--";
                ServerMemory.Text = "-- MB";
                DatabaseSize.Text = "-- MB";
                LastBackupInfo.Text = "Last backup: --";
            });
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/health");
            var json = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"Health response status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Health response body: {json}");
            
            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };
                    var health = System.Text.Json.JsonSerializer.Deserialize<ServerHealthInfo>(json, options);
                    
                    if (health != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Handle both "Healthy" and "healthy" 
                            var status = health.Status ?? "Healthy";
                            var isHealthy = status.Equals("Healthy", StringComparison.OrdinalIgnoreCase);
                            var isDegraded = status.Equals("Degraded", StringComparison.OrdinalIgnoreCase);
                            
                            HealthStatus.Text = isHealthy ? "Healthy" : status;
                            HealthStatus.Foreground = new SolidColorBrush(
                                isHealthy ? (Color)ColorConverter.ConvertFromString("#3FB950") :
                                isDegraded ? (Color)ColorConverter.ConvertFromString("#D29922") :
                                (Color)ColorConverter.ConvertFromString("#F85149"));
                            ServerUptime.Text = health.Uptime ?? "--";
                            ServerMemory.Text = $"{health.MemoryMb:F0} MB";
                            DatabaseSize.Text = $"{health.DatabaseSizeMb:F1} MB";
                            LastBackupInfo.Text = health.LastBackup != null 
                                ? $"Last backup: {health.LastBackup:g}" 
                                : "Last backup: Never";
                        });
                        
                        await LoadSessionsAsync();
                        await LoadBlockedIpsAsync();
                        return;
                    }
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON deserialization failed: {jsonEx.Message}");
                }
            }
            
            // Response not successful or deserialization failed
            Dispatcher.Invoke(() =>
            {
                HealthStatus.Text = response.IsSuccessStatusCode ? "Parse Error" : $"HTTP {(int)response.StatusCode}";
                HealthStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149"));
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Health check failed: {ex.Message}");
            Dispatcher.Invoke(() =>
            {
                HealthStatus.Text = "Unavailable";
                HealthStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149"));
                ServerUptime.Text = "--";
                ServerMemory.Text = "-- MB";
                DatabaseSize.Text = "-- MB";
            });
        }
    }

    private void RefreshHealth_Click(object sender, RoutedEventArgs e) => _ = LoadServerHealthAsync();

    private async void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected) return;

        try
        {
            BackupStatus.Text = "Creating backup...";
            var response = await _httpClient.PostAsync($"{_serverUrl}/api/admin/backup", null);
            if (response.IsSuccessStatusCode)
            {
                BackupStatus.Text = "✓ Backup created successfully!";
                BackupStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950"));
            }
            else
            {
                BackupStatus.Text = "✗ Backup failed";
                BackupStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149"));
            }
        }
        catch (Exception ex)
        {
            BackupStatus.Text = $"✗ Error: {ex.Message}";
            BackupStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149"));
        }
    }

    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to restore from the latest backup?\n\nThis will overwrite current data!",
            "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var response = await _httpClient.PostAsync($"{_serverUrl}/api/admin/restore", null);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Database restored successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Restore failed: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DownloadBackup_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected) return;

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save Backup",
            Filter = "Database Files (*.db)|*.db|All Files (*.*)|*.*",
            FileName = $"license_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/backup/download");
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(saveDialog.FileName, bytes);
                    MessageBox.Show("Backup downloaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task LoadSessionsAsync()
    {
        if (!_isConnected) return;

        try
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/sessions");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var sessions = System.Text.Json.JsonSerializer.Deserialize<List<SessionInfo>>(json, jsonOptions);
                Dispatcher.Invoke(() =>
                {
                    SessionsListPanel.Children.Clear();
                    ActiveSessionCount.Text = $"{sessions?.Count ?? 0} active sessions";

                    if (sessions == null || sessions.Count == 0)
                    {
                        SessionsListPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No active sessions", 
                            FontSize = 12, 
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                        });
                        return;
                    }

                    foreach (var session in sessions)
                    {
                        var card = CreateSessionCard(session);
                        SessionsListPanel.Children.Add(card);
                    }
                });
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"LoadSessionsAsync error: {ex.Message}");
        }
    }

    private Border CreateSessionCard(SessionInfo session)
    {
        var card = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        info.Children.Add(new TextBlock 
        { 
            Text = session.MachineName ?? "Unknown", 
            FontSize = 14, 
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6EDF3"))
        });
        info.Children.Add(new TextBlock 
        { 
            Text = $"License: {session.LicenseId?.Substring(0, Math.Min(12, session.LicenseId?.Length ?? 0))}...", 
            FontSize = 11, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E")),
            Margin = new Thickness(0, 4, 0, 0)
        });
        info.Children.Add(new TextBlock 
        { 
            Text = $"IP: {session.IpAddress} • Last: {session.LastHeartbeat:g}", 
            FontSize = 11, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        var terminateBtn = new Button
        {
            Content = "⛔ Terminate",
            Padding = new Thickness(12, 8, 12, 8),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DA3633")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = session.SessionToken
        };
        terminateBtn.Click += TerminateSession_Click;
        Grid.SetColumn(terminateBtn, 1);
        grid.Children.Add(terminateBtn);

        card.Child = grid;
        return card;
    }

    private async void TerminateSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string sessionToken)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_serverUrl}/api/admin/sessions/{sessionToken}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadSessionsAsync();
                }
            }
            catch { }
        }
    }

    private void RefreshSessions_Click(object sender, RoutedEventArgs e) => _ = LoadSessionsAsync();

    private async void TerminateAllSessions_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to terminate ALL active sessions?\n\nThis will force all users to re-authenticate.",
            "Confirm Terminate All", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var response = await _httpClient.DeleteAsync($"{_serverUrl}/api/admin/sessions");
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("All sessions terminated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSessionsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadBlockedIpsAsync()
    {
        if (!_isConnected) return;

        try
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/blocked-ips");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var ips = System.Text.Json.JsonSerializer.Deserialize<List<BlockedIpInfo>>(json, jsonOptions);
                Dispatcher.Invoke(() =>
                {
                    BlockedIpsPanel.Children.Clear();

                    if (ips == null || ips.Count == 0)
                    {
                        BlockedIpsPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No blocked IPs", 
                            FontSize = 12, 
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                        });
                        return;
                    }

                    foreach (var ip in ips)
                    {
                        var card = CreateBlockedIpCard(ip);
                        BlockedIpsPanel.Children.Add(card);
                    }
                });
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"LoadBlockedIpsAsync error: {ex.Message}");
        }
    }

    private Border CreateBlockedIpCard(BlockedIpInfo ip)
    {
        var card = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new TextBlock 
        { 
            Text = $"{ip.IpAddress} - {ip.Reason}", 
            FontSize = 12, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6EDF3")),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        var unblockBtn = new Button
        {
            Content = "🔓 Unblock",
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E")),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363D")),
            Tag = ip.IpAddress
        };
        unblockBtn.Click += UnblockIp_Click;
        Grid.SetColumn(unblockBtn, 1);
        grid.Children.Add(unblockBtn);

        card.Child = grid;
        return card;
    }

    private void RefreshBlockedIps_Click(object sender, RoutedEventArgs e) => _ = LoadBlockedIpsAsync();

    private async void BlockIp_Click(object sender, RoutedEventArgs e)
    {
        var ip = BlockIpInput.Text.Trim();
        if (string.IsNullOrEmpty(ip)) return;

        try
        {
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { IpAddress = ip, Reason = "Blocked from admin UI" }),
                System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_serverUrl}/api/admin/blocked-ips", content);
            if (response.IsSuccessStatusCode)
            {
                BlockIpInput.Text = "";
                await LoadBlockedIpsAsync();
            }
        }
        catch { }
    }

    private async void UnblockIp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string ip)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_serverUrl}/api/admin/blocked-ips/{ip}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadBlockedIpsAsync();
                }
            }
            catch { }
        }
    }

    #endregion

    #region Audit Logs

    private async Task LoadAuditLogsAsync()
    {
        if (!_isConnected) return;

        try
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/audit?limit=50");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var logs = System.Text.Json.JsonSerializer.Deserialize<List<AuditLogEntry>>(json, jsonOptions);
                Dispatcher.Invoke(() =>
                {
                    AuditLogsPanel.Children.Clear();

                    if (logs == null || logs.Count == 0)
                    {
                        AuditLogsPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No audit logs found", 
                            FontSize = 12, 
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                        });
                        return;
                    }

                    foreach (var log in logs)
                    {
                        var card = CreateAuditLogCard(log);
                        AuditLogsPanel.Children.Add(card);
                    }
                });
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"LoadAuditLogsAsync error: {ex.Message}");
        }
    }

    private Border CreateAuditLogCard(AuditLogEntry log)
    {
        var card = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var eventColor = log.EventType switch
        {
            "Activation" => "#3FB950",
            "Validation" => "#58A6FF",
            "Revocation" => "#F85149",
            "Security" => "#D29922",
            _ => "#8B949E"
        };

        var stack = new StackPanel();
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(eventColor)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Child = new TextBlock { Text = log.EventType, FontSize = 10, Foreground = Brushes.White }
        });
        header.Children.Add(new TextBlock 
        { 
            Text = log.Timestamp?.ToString("g") ?? "", 
            FontSize = 11, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(header);

        stack.Children.Add(new TextBlock 
        { 
            Text = log.Message, 
            FontSize = 12, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6EDF3")),
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrEmpty(log.IpAddress))
        {
            stack.Children.Add(new TextBlock 
            { 
                Text = $"IP: {log.IpAddress} • License: {log.LicenseId ?? "N/A"}", 
                FontSize = 10, 
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        card.Child = stack;
        return card;
    }

    private void RefreshAuditLogs_Click(object sender, RoutedEventArgs e) => _ = LoadAuditLogsAsync();

    private async void SearchAuditLogs_Click(object sender, RoutedEventArgs e)
    {
        var eventType = (AuditEventTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var licenseId = AuditLicenseFilter.Text.Trim();
        var ipAddress = AuditIpFilter.Text.Trim();

        var query = "?limit=100";
        if (!string.IsNullOrEmpty(eventType) && eventType != "All Events")
            query += $"&eventType={eventType}";
        if (!string.IsNullOrEmpty(licenseId))
            query += $"&licenseId={licenseId}";
        if (!string.IsNullOrEmpty(ipAddress))
            query += $"&ip={ipAddress}";

        try
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/audit{query}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var logs = System.Text.Json.JsonSerializer.Deserialize<List<AuditLogEntry>>(json, jsonOptions);
                Dispatcher.Invoke(() =>
                {
                    AuditLogsPanel.Children.Clear();
                    if (logs == null || logs.Count == 0)
                    {
                        AuditLogsPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No matching logs found", 
                            FontSize = 12, 
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                        });
                        return;
                    }
                    foreach (var log in logs)
                    {
                        AuditLogsPanel.Children.Add(CreateAuditLogCard(log));
                    }
                });
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"SearchAuditLogs error: {ex.Message}");
        }
    }

    private async void ExportAuditLogs_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Audit Logs",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"audit_logs_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/audit/export");
                if (response.IsSuccessStatusCode)
                {
                    var csv = await response.Content.ReadAsStringAsync();
                    await File.WriteAllTextAsync(saveDialog.FileName, csv);
                    MessageBox.Show("Audit logs exported!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Customers

    private async Task LoadCustomersAsync()
    {
        if (!_isConnected) return;

        try
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/customers/stats");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var stats = System.Text.Json.JsonSerializer.Deserialize<CustomerStats>(json, jsonOptions);
                if (stats != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TotalCustomers.Text = stats.TotalCustomers.ToString();
                        ActiveSubscriptions.Text = stats.ActiveSubscriptions.ToString();
                        ExpiringSubscriptions.Text = stats.ExpiringSoon.ToString();
                    });
                }
            }

            var listResponse = await _httpClient.GetAsync($"{_serverUrl}/api/admin/customers?limit=50");
            if (listResponse.IsSuccessStatusCode)
            {
                var json = await listResponse.Content.ReadAsStringAsync();
                var customers = System.Text.Json.JsonSerializer.Deserialize<List<CustomerInfo>>(json, jsonOptions);
                Dispatcher.Invoke(() =>
                {
                    CustomersListPanel.Children.Clear();
                    if (customers == null || customers.Count == 0)
                    {
                        CustomersListPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No customers found", 
                            FontSize = 12, 
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                        });
                        return;
                    }
                    foreach (var customer in customers)
                    {
                        CustomersListPanel.Children.Add(CreateCustomerCard(customer));
                    }
                });
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"LoadCustomersAsync error: {ex.Message}");
        }
    }

    private Border CreateCustomerCard(CustomerInfo customer)
    {
        var card = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var statusColor = customer.IsActive ? "#3FB950" : "#F85149";

        var stack = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var name = new TextBlock 
        { 
            Text = customer.CustomerName ?? "Unknown", 
            FontSize = 14, 
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6EDF3"))
        };
        Grid.SetColumn(name, 0);
        header.Children.Add(name);

        var status = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Child = new TextBlock 
            { 
                Text = customer.IsActive ? "Active" : "Inactive", 
                FontSize = 11, 
                Foreground = Brushes.White 
            }
        };
        Grid.SetColumn(status, 1);
        header.Children.Add(status);
        stack.Children.Add(header);

        stack.Children.Add(new TextBlock 
        { 
            Text = customer.CustomerEmail ?? "", 
            FontSize = 12, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E")),
            Margin = new Thickness(0, 4, 0, 0)
        });

        stack.Children.Add(new TextBlock 
        { 
            Text = $"Edition: {customer.Edition} • Expires: {customer.ExpiresAt:d}", 
            FontSize = 11, 
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
            Margin = new Thickness(0, 4, 0, 0)
        });

        card.Child = stack;
        return card;
    }

    private void RefreshCustomers_Click(object sender, RoutedEventArgs e) => _ = LoadCustomersAsync();

    private async void SearchCustomers_Click(object sender, RoutedEventArgs e)
    {
        var email = CustomerEmailFilter.Text.Trim();
        var name = CustomerNameFilter.Text.Trim();

        var query = "?limit=100";
        if (!string.IsNullOrEmpty(email)) query += $"&email={email}";
        if (!string.IsNullOrEmpty(name)) query += $"&name={name}";

        try
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/customers{query}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var customers = System.Text.Json.JsonSerializer.Deserialize<List<CustomerInfo>>(json, jsonOptions);
                Dispatcher.Invoke(() =>
                {
                    CustomersListPanel.Children.Clear();
                    if (customers == null || customers.Count == 0)
                    {
                        CustomersListPanel.Children.Add(new TextBlock 
                        { 
                            Text = "No matching customers found", 
                            FontSize = 12, 
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6E7681")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                        });
                        return;
                    }
                    foreach (var customer in customers)
                    {
                        CustomersListPanel.Children.Add(CreateCustomerCard(customer));
                    }
                });
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"SearchCustomers error: {ex.Message}");
        }
    }

    #endregion
}

#region Models

public class LicenseInfo
{
    public int Id { get; set; }
    public string? LicenseId { get; set; }
    public string? LicenseKey { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    public string? Edition { get; set; }
    public string? SubscriptionType { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsOffline { get; set; }
    public List<string>? Modules { get; set; }
}

public class CreateLicenseResult
{
    public string? LicenseId { get; set; }
    public string? LicenseKey { get; set; }
    public string? SupportCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? LicenseFileContent { get; set; }  // For offline licenses
    public string? LicenseType { get; set; }
}

public class ModuleInfoFull
{
    public int Id { get; set; }
    public string? ModuleId { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? CertificateCode { get; set; }
    public string? Category { get; set; }
    public string? DefaultTier { get; set; }
    public int DisplayOrder { get; set; }
    public string? Icon { get; set; }
    public string? Version { get; set; }
    public bool IsActive { get; set; }
}

public class TierInfoFull
{
    public int Id { get; set; }
    public string? TierId { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<ModuleInfoFull>? Modules { get; set; }
}

public class LicenseeCodeCheckResult
{
    public bool IsValid { get; set; }
    public bool IsAvailable { get; set; }
    public string? Code { get; set; }
    public string? Message { get; set; }
}

public class ServerHealthInfo
{
    public string? Status { get; set; }
    public string? Uptime { get; set; }
    public double MemoryMb { get; set; }
    public double DatabaseSizeMb { get; set; }
    public DateTime? LastBackup { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalLicenses { get; set; }
}

public class SessionInfo
{
    public string? SessionToken { get; set; }
    public string? LicenseId { get; set; }
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
}

public class BlockedIpInfo
{
    public string? IpAddress { get; set; }
    public string? Reason { get; set; }
    public DateTime? BlockedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AuditLogEntry
{
    public int Id { get; set; }
    public string? EventType { get; set; }
    public string? Message { get; set; }
    public string? LicenseId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? Details { get; set; }
}

public class CustomerStats
{
    public int TotalCustomers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ExpiringSoon { get; set; }
}

public class CustomerInfo
{
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Edition { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? LicenseKey { get; set; }
}

public class ServerRootInfo
{
    public string? Service { get; set; }
    public string? Status { get; set; }
    public string? Version { get; set; }
    public string[]? Features { get; set; }
}

public class DbStatusInfo
{
    public string? Status { get; set; }
    public bool CanConnect { get; set; }
    public int LicenseCount { get; set; }
    public int ActivationCount { get; set; }
    public int TransferCount { get; set; }
    public int AuditLogCount { get; set; }
}

#endregion
