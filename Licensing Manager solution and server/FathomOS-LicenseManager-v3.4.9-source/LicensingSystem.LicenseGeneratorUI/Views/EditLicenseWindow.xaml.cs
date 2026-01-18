using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LicensingSystem.Shared;
using LicenseGeneratorUI.Models;

namespace LicenseGeneratorUI.Views;

public partial class EditLicenseWindow : Window
{
    private readonly LicenseEditDetailsDto _license;
    private readonly List<ServerModuleInfo> _modules;
    private readonly List<ServerTierInfo> _tiers;
    private readonly string _serverUrl;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CheckBox> _moduleCheckboxes = new();
    private readonly HashSet<string> _currentModules;
    
    // Branding state
    private string? _logoBase64;
    private bool _licenseeCodeValid = true;
    private System.Timers.Timer? _codeCheckTimer;

    public EditLicenseWindow(
        LicenseEditDetailsDto license,
        List<ServerModuleInfo> modules,
        List<ServerTierInfo> tiers,
        string serverUrl,
        HttpClient httpClient)
    {
        InitializeComponent();
        
        _license = license;
        _modules = modules;
        _tiers = tiers;
        _serverUrl = serverUrl;
        _httpClient = httpClient;

        // Parse current modules from features
        _currentModules = license.Features?
            .Where(f => f.StartsWith("Module:"))
            .Select(f => f.Replace("Module:", ""))
            .ToHashSet() ?? new HashSet<string>();

        InitializeUI();
    }

    private void InitializeUI()
    {
        // Set customer info
        CustomerInfoText.Text = $"Customer: {_license.CustomerName} ({_license.CustomerEmail})";
        LicenseKeyText.Text = _license.LicenseKey;
        ExpiresText.Text = _license.ExpiresAt.ToString("yyyy-MM-dd HH:mm");

        // Set branding fields
        BrandNameTextBox.Text = _license.Brand ?? "";
        LicenseeCodeTextBox.Text = _license.LicenseeCode ?? "";
        SupportCodeTextBox.Text = _license.SupportCode ?? "";
        
        // If licensee code exists, show it's in use by this license
        if (!string.IsNullOrEmpty(_license.LicenseeCode))
        {
            LicenseeCodeStatus.Text = "✓ Assigned to this license";
            LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
        }

        // Populate tier combo
        foreach (var tier in _tiers.OrderBy(t => t.DisplayOrder))
        {
            var item = new ComboBoxItem
            {
                Content = tier.DisplayName,
                Tag = tier.TierId
            };
            
            // Check if this is the current tier
            if (_license.Edition == tier.TierId)
                item.IsSelected = true;
            
            TierCombo.Items.Add(item);
        }

        // Add Custom option
        var customItem = new ComboBoxItem { Content = "Custom (Select Modules)", Tag = "Custom" };
        if (_license.Edition == "Custom" || !_tiers.Any(t => t.TierId == _license.Edition))
        {
            customItem.IsSelected = true;
        }
        TierCombo.Items.Add(customItem);

        // Create module checkboxes
        foreach (var module in _modules.OrderBy(m => m.DisplayOrder))
        {
            var checkbox = new CheckBox
            {
                Content = $"{module.Icon ?? "📦"} {module.DisplayName}",
                Tag = module.ModuleId,
                IsChecked = _currentModules.Contains(module.ModuleId),
                Margin = new Thickness(0, 5, 20, 5),
                FontSize = 13,
                MinWidth = 180
            };
            
            checkbox.Checked += ModuleCheckbox_Changed;
            checkbox.Unchecked += ModuleCheckbox_Changed;
            
            _moduleCheckboxes[module.ModuleId] = checkbox;
            ModulesPanel.Children.Add(checkbox);
        }

        // Update checkboxes based on current tier
        UpdateModuleCheckboxesForTier();
    }

    private void TierCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateModuleCheckboxesForTier();
    }

    private void UpdateModuleCheckboxesForTier()
    {
        if (TierCombo.SelectedItem is not ComboBoxItem selectedItem) return;
        
        var tierId = selectedItem.Tag?.ToString() ?? "Custom";
        var tier = _tiers.FirstOrDefault(t => t.TierId == tierId);
        var tierModuleIds = tier?.Modules?.Select(m => m.ModuleId).ToHashSet() ?? new HashSet<string>();
        
        bool isCustom = tierId == "Custom";

        foreach (var kvp in _moduleCheckboxes)
        {
            var moduleId = kvp.Key;
            var checkbox = kvp.Value;
            
            checkbox.IsEnabled = isCustom;
            if (!isCustom)
            {
                checkbox.IsChecked = tierModuleIds.Contains(moduleId);
            }
        }

        // Update description
        if (tier != null)
        {
            TierDescriptionText.Text = tier.Description ?? $"{tier.DisplayName} tier modules";
        }
        else if (isCustom)
        {
            TierDescriptionText.Text = "Select individual modules for this license";
        }
    }

    private void ModuleCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        // Module checkbox changed - just for UI feedback
    }

    #region Branding Handlers

    private void LicenseeCode_TextChanged(object sender, TextChangedEventArgs e)
    {
        var code = LicenseeCodeTextBox.Text.Trim().ToUpperInvariant();
        
        // Reset timer for debouncing
        _codeCheckTimer?.Stop();
        _codeCheckTimer?.Dispose();
        
        if (string.IsNullOrEmpty(code))
        {
            LicenseeCodeStatus.Text = "";
            _licenseeCodeValid = true;
            return;
        }

        if (code.Length < 2 || code.Length > 3)
        {
            LicenseeCodeStatus.Text = "Must be 2-3 characters (A-Z, 0-9)";
            LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            _licenseeCodeValid = false;
            return;
        }

        if (!code.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
        {
            LicenseeCodeStatus.Text = "Only uppercase letters (A-Z) and numbers (0-9) allowed";
            LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            _licenseeCodeValid = false;
            return;
        }

        // If it's the same as the current license's code, it's valid
        if (code == _license.LicenseeCode)
        {
            LicenseeCodeStatus.Text = "✓ Assigned to this license";
            LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
            _licenseeCodeValid = true;
            return;
        }

        // Debounce the server check
        LicenseeCodeStatus.Text = "Checking availability...";
        LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        
        _codeCheckTimer = new System.Timers.Timer(500);
        _codeCheckTimer.Elapsed += async (s, args) =>
        {
            _codeCheckTimer?.Stop();
            await CheckLicenseeCodeAvailability(code);
        };
        _codeCheckTimer.Start();
    }

    private async Task CheckLicenseeCodeAvailability(string code)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/admin/branding/check-code/{code}");
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LicenseeCodeCheckResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            await Dispatcher.InvokeAsync(() =>
            {
                if (result?.IsAvailable == true)
                {
                    LicenseeCodeStatus.Text = "✓ Code available";
                    LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                    _licenseeCodeValid = true;
                }
                else if (result?.IsValid == false)
                {
                    // Invalid format from server
                    LicenseeCodeStatus.Text = result.Message ?? "Invalid code format";
                    LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    _licenseeCodeValid = false;
                }
                else
                {
                    // Code exists but duplicates ARE allowed (same company can have multiple licenses)
                    LicenseeCodeStatus.Text = $"ℹ️ Code in use - OK for same company";
                    LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                    _licenseeCodeValid = true; // ALLOW - duplicates are fine
                }
            });
        }
        catch (HttpRequestException)
        {
            // Server not reachable - ALLOW the code (offline mode)
            await Dispatcher.InvokeAsync(() =>
            {
                LicenseeCodeStatus.Text = "⚠ Server offline - code accepted (verify later)";
                LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                _licenseeCodeValid = true; // ALLOW when offline
            });
        }
        catch (TaskCanceledException)
        {
            // Timeout - ALLOW the code
            await Dispatcher.InvokeAsync(() =>
            {
                LicenseeCodeStatus.Text = "⚠ Server timeout - code accepted (verify later)";
                LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                _licenseeCodeValid = true; // ALLOW when timeout
            });
        }
        catch (Exception ex)
        {
            // Other error - ALLOW but warn
            await Dispatcher.InvokeAsync(() =>
            {
                LicenseeCodeStatus.Text = $"⚠ Could not verify - code accepted";
                LicenseeCodeStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                _licenseeCodeValid = true; // ALLOW on other errors
                System.Diagnostics.Debug.WriteLine($"Code check error: {ex.Message}");
            });
        }
    }

    private async void RegenerateSupportCode_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_license.LicenseeCode) && string.IsNullOrEmpty(LicenseeCodeTextBox.Text))
        {
            MessageBox.Show("Please enter a Licensee Code first.", "Missing Code", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            "Are you sure you want to regenerate the support code?\n\nThis will invalidate the old support code.",
            "Regenerate Support Code",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var response = await _httpClient.PostAsync($"{_serverUrl}/api/admin/licenses/{_license.Id}/regenerate-support-code", null);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                if (data.TryGetProperty("supportCode", out var codeElement))
                {
                    SupportCodeTextBox.Text = codeElement.GetString() ?? "";
                }
                MessageBox.Show("Support code regenerated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to regenerate support code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Logo Image",
            Filter = "PNG Images (*.png)|*.png|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var fileInfo = new FileInfo(dialog.FileName);
                if (fileInfo.Length > 20 * 1024) // 20KB limit
                {
                    MessageBox.Show("Logo file must be 20KB or smaller.", "File Too Large", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Read and convert to base64
                var bytes = File.ReadAllBytes(dialog.FileName);
                _logoBase64 = Convert.ToBase64String(bytes);

                // Show preview
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                LogoPreview.Source = bitmap;

                LogoFilename.Text = fileInfo.Name;
                ClearLogoBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ClearLogo_Click(object sender, RoutedEventArgs e)
    {
        _logoBase64 = null;
        LogoPreview.Source = null;
        LogoFilename.Text = "";
        ClearLogoBtn.Visibility = Visibility.Collapsed;
    }

    #endregion

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate licensee code
        var licenseeCode = LicenseeCodeTextBox.Text.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(licenseeCode) && !_licenseeCodeValid)
        {
            MessageBox.Show("Please fix the Licensee Code issue before saving.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            SaveBtn.IsEnabled = false;
            SaveBtn.Content = "Saving...";

            var selectedTier = (TierCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Custom";
            
            // Build features list
            var features = new List<string> { $"Tier:{selectedTier}" };
            
            foreach (var kvp in _moduleCheckboxes)
            {
                if (kvp.Value.IsChecked == true)
                {
                    features.Add($"Module:{kvp.Key}");
                }
            }

            // Update features
            var featuresRequest = new
            {
                Edition = selectedTier,
                Features = features
            };

            var featuresJson = JsonSerializer.Serialize(featuresRequest);
            var featuresContent = new StringContent(featuresJson, Encoding.UTF8, "application/json");
            var featuresResponse = await _httpClient.PutAsync($"{_serverUrl}/api/admin/licenses/{_license.Id}/features", featuresContent);

            if (!featuresResponse.IsSuccessStatusCode)
            {
                var error = await featuresResponse.Content.ReadAsStringAsync();
                MessageBox.Show($"Failed to update features: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update branding if any branding fields are set
            var brandName = BrandNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(brandName) || !string.IsNullOrEmpty(licenseeCode) || _logoBase64 != null)
            {
                var brandingRequest = new
                {
                    Brand = string.IsNullOrEmpty(brandName) ? (string?)null : brandName,
                    LicenseeCode = string.IsNullOrEmpty(licenseeCode) ? (string?)null : licenseeCode,
                    BrandLogo = _logoBase64
                };

                var brandingJson = JsonSerializer.Serialize(brandingRequest);
                var brandingContent = new StringContent(brandingJson, Encoding.UTF8, "application/json");
                var brandingResponse = await _httpClient.PutAsync($"{_serverUrl}/api/admin/licenses/{_license.Id}/branding", brandingContent);

                if (!brandingResponse.IsSuccessStatusCode)
                {
                    var error = await brandingResponse.Content.ReadAsStringAsync();
                    MessageBox.Show($"Features saved but branding update failed: {error}", "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SaveBtn.IsEnabled = true;
            SaveBtn.Content = "💾 Save Changes";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

// LicenseEditDetailsDto and LicenseeCodeCheckResult are defined in Models/Models.cs and LicensingSystem.Shared
