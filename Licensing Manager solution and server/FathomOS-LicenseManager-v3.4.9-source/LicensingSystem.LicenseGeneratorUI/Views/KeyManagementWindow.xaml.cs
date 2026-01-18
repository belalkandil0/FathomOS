using System.IO;
using System.Security.Cryptography;
using System.Windows;
using LicenseGeneratorUI.Services;
using Microsoft.Win32;

namespace LicenseGeneratorUI.Views;

/// <summary>
/// First-launch key setup dialog for standalone mode.
/// Allows generating new keys or importing existing ones.
/// </summary>
public partial class KeyManagementWindow : Window
{
    private readonly KeyStorageService _keyService;
    private string? _importedPrivateKeyPath;
    private string? _generatedPublicKey;
    private string? _generatedPrivateKey;

    public KeyManagementWindow()
    {
        InitializeComponent();
        _keyService = new KeyStorageService();
    }

    /// <summary>
    /// Constructor with existing key service for dependency injection
    /// </summary>
    public KeyManagementWindow(KeyStorageService keyService)
    {
        InitializeComponent();
        _keyService = keyService;
    }

    private void KeyOption_Changed(object sender, RoutedEventArgs e)
    {
        if (BrowseButton == null) return;

        BrowseButton.IsEnabled = ImportExistingRadio.IsChecked == true;

        // Clear import status when switching options
        if (GenerateNewRadio.IsChecked == true)
        {
            ImportedFileText.Text = "";
            _importedPrivateKeyPath = null;
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Private Key File",
            Filter = "PEM files (*.pem)|*.pem|All files (*.*)|*.*",
            DefaultExt = ".pem"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Validate the key file
                var keyContent = File.ReadAllText(dialog.FileName);

                if (!keyContent.Contains("PRIVATE KEY"))
                {
                    ShowError("The selected file does not appear to be a valid private key file.");
                    return;
                }

                // Try to parse it to make sure it's valid
                var privateKey = ParsePrivateKey(keyContent);
                if (privateKey == null)
                {
                    ShowError("Could not parse the private key. Ensure it's a valid ECDSA P-256 key in PEM format.");
                    return;
                }

                _importedPrivateKeyPath = dialog.FileName;
                ImportedFileText.Text = $"Selected: {Path.GetFileName(dialog.FileName)}";
                ErrorBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to read key file: {ex.Message}");
            }
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        ErrorBorder.Visibility = Visibility.Collapsed;

        try
        {
            if (GenerateNewRadio.IsChecked == true)
            {
                // Generate new key pair
                var (privateKey, publicKey) = _keyService.GenerateKeyPair();
                _generatedPrivateKey = privateKey;
                _generatedPublicKey = publicKey;

                // Store the keys
                _keyService.StorePrivateKey(privateKey);
                _keyService.StorePublicKey(publicKey);

                // Show success page
                ShowSuccessPage();
            }
            else if (ImportExistingRadio.IsChecked == true)
            {
                if (string.IsNullOrEmpty(_importedPrivateKeyPath))
                {
                    ShowError("Please select a private key file to import.");
                    return;
                }

                // Import the key
                _keyService.ImportPrivateKey(_importedPrivateKeyPath);

                // Load the imported key to derive public key
                var privateKeyPem = File.ReadAllText(_importedPrivateKeyPath);
                _generatedPrivateKey = privateKeyPem;

                // Derive public key from private key
                var privateKey = ParsePrivateKey(privateKeyPem);
                if (privateKey != null)
                {
                    var publicKeyBytes = privateKey.ExportSubjectPublicKeyInfo();
                    _generatedPublicKey = "-----BEGIN PUBLIC KEY-----\n" +
                                         Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                                         "\n-----END PUBLIC KEY-----";
                    _keyService.StorePublicKey(_generatedPublicKey);
                }

                // Show success page
                ShowSuccessPage();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to set up keys: {ex.Message}");
        }
    }

    private void ShowSuccessPage()
    {
        SetupPage.Visibility = Visibility.Collapsed;
        SuccessPage.Visibility = Visibility.Visible;

        PublicKeyDisplay.Text = _generatedPublicKey ?? "";

        CancelButton.Visibility = Visibility.Collapsed;
        ContinueButton.Visibility = Visibility.Collapsed;
        DoneButton.Visibility = Visibility.Visible;
    }

    private void CopyPublicKey_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_generatedPublicKey))
        {
            Clipboard.SetText(_generatedPublicKey);
            MessageBox.Show("Public key copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportPublicKey_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_generatedPublicKey)) return;

        var dialog = new SaveFileDialog
        {
            Title = "Export Public Key",
            Filter = "PEM files (*.pem)|*.pem|All files (*.*)|*.*",
            DefaultExt = ".pem",
            FileName = "fathom_public_key.pem"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, _generatedPublicKey);
                MessageBox.Show($"Public key exported to:\n{dialog.FileName}", "Exported", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export public key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportPrivateKeyBackup_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_generatedPrivateKey)) return;

        var result = MessageBox.Show(
            "WARNING: The private key is extremely sensitive!\n\n" +
            "Anyone with access to this file can create valid licenses.\n\n" +
            "Store this backup in a secure location (encrypted drive, password manager, etc.).\n\n" +
            "Do you want to continue?",
            "Security Warning",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        var dialog = new SaveFileDialog
        {
            Title = "Export Private Key Backup",
            Filter = "PEM files (*.pem)|*.pem|All files (*.*)|*.*",
            DefaultExt = ".pem",
            FileName = "fathom_private_key_BACKUP.pem"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, _generatedPrivateKey);
                MessageBox.Show(
                    $"Private key backup saved to:\n{dialog.FileName}\n\n" +
                    "IMPORTANT: Store this file securely and delete it from any unsecured locations!",
                    "Backup Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export private key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }

    private ECDsa? ParsePrivateKey(string pem)
    {
        try
        {
            var ecdsa = ECDsa.Create();

            // Remove PEM headers and decode
            var base64 = pem
                .Replace("-----BEGIN EC PRIVATE KEY-----", "")
                .Replace("-----END EC PRIVATE KEY-----", "")
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            var keyBytes = Convert.FromBase64String(base64);

            // Try ECPrivateKey format first
            try
            {
                ecdsa.ImportECPrivateKey(keyBytes, out _);
                return ecdsa;
            }
            catch
            {
                // Try PKCS8 format
                ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);
                return ecdsa;
            }
        }
        catch
        {
            return null;
        }
    }
}
