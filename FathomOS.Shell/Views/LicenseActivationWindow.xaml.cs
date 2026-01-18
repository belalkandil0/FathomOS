using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using LicensingSystem.Client;
using LicensingSystem.Shared;

namespace FathomOS.Shell.Views;

/// <summary>
/// First-launch license activation window for FathomOS.
/// Supports offline license activation via file or pasted key.
/// </summary>
public partial class LicenseActivationWindow : Window
{
    private readonly LicenseManager _licenseManager;
    private SignedLicense? _validatedLicense;
    private string? _licenseFilePath;

    /// <summary>
    /// Indicates if activation was successful.
    /// </summary>
    public bool IsActivated { get; private set; }

    /// <summary>
    /// The validated license after successful activation.
    /// </summary>
    public LicenseFile? ActivatedLicense => _validatedLicense?.License;

    /// <summary>
    /// Creates a new LicenseActivationWindow.
    /// </summary>
    public LicenseActivationWindow()
    {
        InitializeComponent();

        // Get license manager from App
        _licenseManager = App.LicenseManager
            ?? throw new InvalidOperationException("LicenseManager not initialized");

        // Display hardware ID
        HardwareIdTextBox.Text = _licenseManager.GetHardwareId();

        // Handle window loaded
        Loaded += (s, e) => FilePathTextBox.Focus();
    }

    /// <summary>
    /// Creates a new LicenseActivationWindow with a specific license manager.
    /// </summary>
    /// <param name="licenseManager">The license manager to use</param>
    public LicenseActivationWindow(LicenseManager licenseManager)
    {
        InitializeComponent();

        _licenseManager = licenseManager
            ?? throw new ArgumentNullException(nameof(licenseManager));

        // Display hardware ID
        HardwareIdTextBox.Text = _licenseManager.GetHardwareId();

        Loaded += (s, e) => FilePathTextBox.Focus();
    }

    private void CopyHardwareId_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(HardwareIdTextBox.Text);
            ShowStatus("Hardware ID copied to clipboard.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to copy: {ex.Message}", isError: true);
        }
    }

    private void CopyFullFingerprints_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var fingerprints = _licenseManager.GetHardwareFingerprintsForLicense();
            Clipboard.SetText(fingerprints);

            var count = _licenseManager.GetHardwareFingerprintsList().Count;
            ShowStatus($"{count} fingerprints copied. Use these when creating an offline license.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to copy fingerprints: {ex.Message}", isError: true);
        }
    }

    private void BrowseLicenseFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "License Files (*.lic;*.json)|*.lic;*.json|All Files (*.*)|*.*",
            Title = "Select FathomOS License File"
        };

        if (dialog.ShowDialog() == true)
        {
            _licenseFilePath = dialog.FileName;
            FilePathTextBox.Text = Path.GetFileName(dialog.FileName);
            LicenseKeyTextBox.Clear();

            // Reset validation state
            ResetValidationState();

            ShowStatus("License file selected. Click 'Validate License' to verify.", isError: false);
        }
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {
        ShowStatus("Validating license...", isError: false);
        SetControlsEnabled(false);

        try
        {
            SignedLicense? signedLicense = null;
            string? licenseJson = null;

            // Try to load from file first
            if (!string.IsNullOrEmpty(_licenseFilePath) && File.Exists(_licenseFilePath))
            {
                licenseJson = File.ReadAllText(_licenseFilePath);
            }
            // Otherwise try from pasted key
            else if (!string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text))
            {
                licenseJson = LicenseKeyTextBox.Text.Trim();

                // If it looks like Base64, try to decode it
                if (IsBase64String(licenseJson))
                {
                    try
                    {
                        var decoded = Convert.FromBase64String(licenseJson);
                        licenseJson = System.Text.Encoding.UTF8.GetString(decoded);
                    }
                    catch
                    {
                        // Not Base64, use as-is (might be raw JSON)
                    }
                }
            }
            else
            {
                ShowStatus("Please select a license file or paste a license key.", isError: true);
                SetControlsEnabled(true);
                return;
            }

            // Parse the license JSON
            try
            {
                signedLicense = System.Text.Json.JsonSerializer.Deserialize<SignedLicense>(licenseJson);
            }
            catch (Exception ex)
            {
                ShowStatus($"Invalid license format: {ex.Message}", isError: true);
                SetControlsEnabled(true);
                return;
            }

            if (signedLicense?.License == null)
            {
                ShowStatus("Could not read license data. Please ensure the file is valid.", isError: true);
                SetControlsEnabled(true);
                return;
            }

            // Validate the license using temporary file if needed
            LicenseValidationResult result;

            if (!string.IsNullOrEmpty(_licenseFilePath) && File.Exists(_licenseFilePath))
            {
                // Pre-validate from file
                result = PreValidateLicenseFromFile(_licenseFilePath);
            }
            else
            {
                // Write to temp file and pre-validate
                var tempPath = Path.Combine(Path.GetTempPath(), $"fathomos_license_{Guid.NewGuid()}.json");
                try
                {
                    File.WriteAllText(tempPath, licenseJson);
                    result = PreValidateLicenseFromFile(tempPath);
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }

            if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
            {
                _validatedLicense = signedLicense;
                ShowLicensePreview(signedLicense.License, result);
                ActivateButton.IsEnabled = true;
                HideStatus();
            }
            else
            {
                ShowStatus($"License validation failed: {result.Message}", isError: true);
                ResetValidationState();
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Validation error: {ex.Message}", isError: true);
            ResetValidationState();
        }

        SetControlsEnabled(true);
    }

    private LicenseValidationResult PreValidateLicenseFromFile(string filePath)
    {
        // Pre-validate license file by parsing and checking basic properties
        // without actually storing it. Full validation happens on Activate.
        try
        {
            var json = File.ReadAllText(filePath);
            var signedLicense = System.Text.Json.JsonSerializer.Deserialize<SignedLicense>(json);

            if (signedLicense?.License == null)
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.Corrupted,
                    Message = "Invalid license file format."
                };
            }

            var license = signedLicense.License;

            // Check product name
            if (!string.Equals(license.Product, LicenseConstants.ProductName, StringComparison.OrdinalIgnoreCase))
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.InvalidSignature,
                    Message = "License is not valid for this product."
                };
            }

            // Check if expired beyond grace period
            var now = DateTime.UtcNow;
            var gracePeriodEnd = license.ExpiresAt.AddDays(LicenseConstants.GracePeriodDays);
            if (now > gracePeriodEnd)
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.Expired,
                    Message = "License has expired and is beyond the grace period."
                };
            }

            // Check if in grace period
            var daysRemaining = (int)(license.ExpiresAt - now).TotalDays;
            var graceDaysRemaining = (int)(gracePeriodEnd - now).TotalDays;

            if (now > license.ExpiresAt)
            {
                return new LicenseValidationResult
                {
                    IsValid = true,
                    Status = LicenseStatus.GracePeriod,
                    Message = $"License expired but in grace period. {graceDaysRemaining} days remaining.",
                    License = license,
                    DaysRemaining = 0,
                    GraceDaysRemaining = graceDaysRemaining,
                    EnabledFeatures = license.Features
                };
            }

            // Pre-validation passed
            return new LicenseValidationResult
            {
                IsValid = true,
                Status = LicenseStatus.Valid,
                Message = "License appears valid. Click Activate to complete.",
                License = license,
                DaysRemaining = daysRemaining,
                EnabledFeatures = license.Features
            };
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = $"Error reading license: {ex.Message}"
            };
        }
    }

    private void ShowLicensePreview(LicenseFile license, LicenseValidationResult result)
    {
        ClientNameText.Text = !string.IsNullOrEmpty(license.CustomerName)
            ? license.CustomerName
            : license.CustomerEmail;

        EditionText.Text = license.Edition;
        ExpiryText.Text = license.ExpiresAt.ToString("MMMM dd, yyyy");
        LicenseIdText.Text = license.LicenseId;

        // Show modules
        var modules = license.Modules.Any()
            ? license.Modules
            : license.Features.Where(f => f.StartsWith("Module:")).Select(f => f.Substring(7)).ToList();

        if (modules.Any())
        {
            ModulesList.ItemsSource = modules;
        }
        else
        {
            ModulesList.ItemsSource = new[] { "All Modules" };
        }

        LicensePreviewPanel.Visibility = Visibility.Visible;

        // Show grace period warning if applicable
        if (result.Status == LicenseStatus.GracePeriod)
        {
            ShowStatus($"Warning: License is in grace period. {result.GraceDaysRemaining} days remaining.", isError: true);
        }
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (_validatedLicense == null)
        {
            ShowStatus("Please validate a license first.", isError: true);
            return;
        }

        ShowStatus("Activating license...", isError: false);
        SetControlsEnabled(false);

        try
        {
            LicenseValidationResult result;

            // Store the license by activating from file
            if (!string.IsNullOrEmpty(_licenseFilePath) && File.Exists(_licenseFilePath))
            {
                result = _licenseManager.ActivateFromFile(_licenseFilePath);
            }
            else if (!string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text))
            {
                // Create temp file from pasted key
                var licenseJson = LicenseKeyTextBox.Text.Trim();
                if (IsBase64String(licenseJson))
                {
                    try
                    {
                        var decoded = Convert.FromBase64String(licenseJson);
                        licenseJson = System.Text.Encoding.UTF8.GetString(decoded);
                    }
                    catch { }
                }

                var tempPath = Path.Combine(Path.GetTempPath(), $"fathomos_license_{Guid.NewGuid()}.json");
                try
                {
                    File.WriteAllText(tempPath, licenseJson);
                    result = _licenseManager.ActivateFromFile(tempPath);
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
            else
            {
                ShowStatus("No license data available to activate.", isError: true);
                SetControlsEnabled(true);
                return;
            }

            if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
            {
                IsActivated = true;
                ShowSuccessOverlay(result);
            }
            else
            {
                ShowStatus($"Activation failed: {result.Message}", isError: true);
                SetControlsEnabled(true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Activation error: {ex.Message}", isError: true);
            SetControlsEnabled(true);
        }
    }

    private void ShowSuccessOverlay(LicenseValidationResult result)
    {
        SuccessEditionText.Text = result.License?.Edition ?? "Licensed";
        SuccessCustomerText.Text = result.License?.CustomerName ?? result.License?.CustomerEmail ?? "N/A";
        SuccessExpiryText.Text = result.License?.ExpiresAt.ToString("MMMM dd, yyyy") ?? "N/A";

        if (result.Status == LicenseStatus.GracePeriod)
        {
            SuccessMessage.Text = $"License activated. Note: You have {result.GraceDaysRemaining} grace days remaining to renew.";
        }
        else
        {
            SuccessMessage.Text = $"Your FathomOS {result.License?.Edition ?? ""} license has been activated successfully.";
        }

        MainContent.Visibility = Visibility.Collapsed;
        SuccessOverlay.Visibility = Visibility.Visible;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ResetValidationState()
    {
        _validatedLicense = null;
        ActivateButton.IsEnabled = false;
        LicensePreviewPanel.Visibility = Visibility.Collapsed;
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
        ValidateButton.IsEnabled = enabled;
        if (enabled && _validatedLicense != null)
        {
            ActivateButton.IsEnabled = true;
        }
        else if (!enabled)
        {
            ActivateButton.IsEnabled = false;
        }
    }

    private static bool IsBase64String(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        return s.Length % 4 == 0 &&
               System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$",
                   System.Text.RegularExpressions.RegexOptions.None);
    }
}
