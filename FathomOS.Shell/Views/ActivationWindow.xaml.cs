using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using LicensingSystem.Client;
using LicensingSystem.Shared;

namespace FathomOS.Shell.Views;

/// <summary>
/// License activation dialog for Fathom OS
/// </summary>
public partial class ActivationWindow : Window
{
    /// <summary>
    /// Indicates if activation was successful (used by App.xaml.cs)
    /// </summary>
    public bool ActivationSuccessful { get; private set; } = false;
    
    public ActivationWindow()
    {
        InitializeComponent();
        
        // Display hardware ID from license manager
        HardwareIdTextBox.Text = App.LicenseManager.GetHardwareId();
    }

    private void CopyHardwareId_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(HardwareIdTextBox.Text);
        ShowStatus("✓ Hardware ID copied to clipboard!", isError: false);
    }

    private void CopyFullFingerprints_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get full fingerprints as plain text (one per line) - NOT JSON
            // This is what needs to go into the license file's hardwareFingerprints field
            var fingerprints = App.LicenseManager.GetHardwareFingerprintsForLicense();
            var fingerprintList = App.LicenseManager.GetHardwareFingerprintsList();
            
            // Copy plain text format (one fingerprint per line)
            Clipboard.SetText(fingerprints);
            ShowStatus($"✓ {fingerprintList.Count} fingerprints copied! Paste into license generator.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"❌ Failed to copy fingerprints: {ex.Message}", isError: true);
        }
    }

    private void DiagnoseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var diagnostic = App.LicenseManager.DiagnoseHardwareMismatch();
            
            // Show in a dialog with option to copy
            var result = MessageBox.Show(
                diagnostic + "\n\nCopy to clipboard?",
                "Hardware Fingerprint Diagnostic",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
                
            if (result == MessageBoxResult.Yes)
            {
                Clipboard.SetText(diagnostic);
                ShowStatus("✓ Diagnostic info copied to clipboard!", isError: false);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"❌ Diagnostic failed: {ex.Message}", isError: true);
        }
    }

    private async void ActivateOnline_Click(object sender, RoutedEventArgs e)
    {
        var key = LicenseKeyTextBox.Text.Trim();
        var email = EmailTextBox.Text.Trim();

        if (string.IsNullOrEmpty(key))
        {
            ShowStatus("Please enter your license key.", isError: true);
            LicenseKeyTextBox.Focus();
            return;
        }
        
        if (string.IsNullOrEmpty(email))
        {
            ShowStatus("Please enter your email address.", isError: true);
            EmailTextBox.Focus();
            return;
        }

        ShowStatus("🔄 Activating your license...", isError: false);
        SetButtonsEnabled(false);

        try
        {
            var result = await App.LicenseManager.ActivateOnlineAsync(key, email);

            if (result.IsValid)
            {
                // Mark activation as successful
                ActivationSuccessful = true;
                
                // Show success overlay
                ShowSuccessOverlay(result);
            }
            else
            {
                ShowStatus($"❌ {result.Message}", isError: true);
                SetButtonsEnabled(true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"❌ Activation failed: {ex.Message}", isError: true);
            SetButtonsEnabled(true);
        }
    }

    private void LoadLicenseFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "License Files (*.lic;*.json)|*.lic;*.json|All Files (*.*)|*.*",
            Title = "Select Fathom OS License File"
        };

        if (dialog.ShowDialog() == true)
        {
            ShowStatus("🔄 Loading license file...", isError: false);
            SetButtonsEnabled(false);
            
            try
            {
                var result = App.LicenseManager.ActivateFromFile(dialog.FileName);

                if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
                {
                    // Mark activation as successful
                    ActivationSuccessful = true;
                    
                    // Show success overlay
                    ShowSuccessOverlay(result);
                }
                else
                {
                    ShowStatus($"❌ {result.Message}", isError: true);
                    SetButtonsEnabled(true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Failed to load license: {ex.Message}", isError: true);
                SetButtonsEnabled(true);
            }
        }
    }

    private void ShowSuccessOverlay(LicenseValidationResult result)
    {
        // Update success overlay with license details
        LicenseEditionText.Text = result.License?.Edition ?? "Licensed";
        CustomerNameText.Text = result.License?.CustomerName ?? result.License?.CustomerEmail ?? "N/A";
        ExpiryDateText.Text = result.License?.ExpiresAt.ToString("MMMM dd, yyyy") ?? "N/A";
        
        if (result.Status == LicenseStatus.GracePeriod)
        {
            SuccessMessage.Text = $"Your license is in grace period. You have {result.GraceDaysRemaining} days remaining to renew.";
        }
        else
        {
            SuccessMessage.Text = $"Your Fathom OS {result.License?.Edition ?? ""} license has been activated successfully.";
        }
        
        // Show the overlay
        MainContent.Visibility = Visibility.Collapsed;
        SuccessOverlay.Visibility = Visibility.Visible;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        // Close with success
        DialogResult = true;
        Close();
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
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(60, 30, 35));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 120));
        }
        else
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(30, 50, 45));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 212, 170));
        }
    }
    
    private void SetButtonsEnabled(bool enabled)
    {
        ActivateOnlineButton.IsEnabled = enabled;
        LoadFileButton.IsEnabled = enabled;
        LicenseKeyTextBox.IsEnabled = enabled;
        EmailTextBox.IsEnabled = enabled;
    }
}
