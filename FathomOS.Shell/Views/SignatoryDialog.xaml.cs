// FathomOS.Shell/Views/SignatoryDialog.xaml.cs
// Dialog for collecting certificate signatory information

using System;
using System.IO;
using System.Windows;

namespace FathomOS.Shell.Views;

/// <summary>
/// Dialog for collecting certificate signatory information
/// </summary>
public partial class SignatoryDialog : Window
{
    public SignatoryDialog()
    {
        InitializeComponent();
        LoadLastUsedValues();
    }

    /// <summary>
    /// Pre-fill company name from license
    /// </summary>
    public void SetCompanyName(string companyName)
    {
        txtCompanyName.Text = companyName;
    }

    // Results
    public string SignatoryName => txtSignatoryName.Text?.Trim() ?? "";
    public string SignatoryTitle => cboTitle.Text?.Trim() ?? "";
    public string CompanyName => txtCompanyName.Text?.Trim() ?? "";

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void btnCreate_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(SignatoryName))
        {
            MessageBox.Show("Please enter the signatory name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtSignatoryName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(SignatoryTitle))
        {
            MessageBox.Show("Please select or enter a professional title.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            cboTitle.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            MessageBox.Show("Please enter the company name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtCompanyName.Focus();
            return;
        }

        // Save for next time
        SaveLastUsedValues();

        DialogResult = true;
        Close();
    }

    private void LoadLastUsedValues()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsPath = Path.Combine(appData, "FathomOS", "signatory_defaults.txt");
            
            if (File.Exists(settingsPath))
            {
                var lines = File.ReadAllLines(settingsPath);
                if (lines.Length >= 1) txtSignatoryName.Text = lines[0];
                if (lines.Length >= 2) cboTitle.Text = lines[1];
                if (lines.Length >= 3) txtCompanyName.Text = lines[2];
            }
        }
        catch
        {
            // Ignore errors loading defaults
        }
    }

    private void SaveLastUsedValues()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(appData, "FathomOS");
            Directory.CreateDirectory(settingsDir);
            
            var settingsPath = Path.Combine(settingsDir, "signatory_defaults.txt");
            File.WriteAllLines(settingsPath, new[]
            {
                SignatoryName,
                SignatoryTitle,
                CompanyName
            });
        }
        catch
        {
            // Ignore errors saving defaults
        }
    }
}
