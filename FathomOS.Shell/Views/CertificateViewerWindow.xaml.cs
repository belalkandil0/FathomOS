// FathomOS.Shell/Views/CertificateViewerWindow.xaml.cs
// Window for viewing and exporting certificates

using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using LicensingSystem.Shared;
using FathomOS.Core.Certificates;

namespace FathomOS.Shell.Views;

/// <summary>
/// Window for viewing a certificate
/// </summary>
public partial class CertificateViewerWindow : Window
{
    private readonly ProcessingCertificate _certificate;
    private readonly CertificatePdfGenerator _pdfGenerator;
    private readonly bool _isSynced;
    private readonly string? _brandLogo;

    public CertificateViewerWindow(ProcessingCertificate certificate, bool isSynced = false, string? brandLogo = null)
    {
        InitializeComponent();
        
        _certificate = certificate;
        _isSynced = isSynced;
        _brandLogo = brandLogo;
        _pdfGenerator = new CertificatePdfGenerator();
        
        LoadCertificate();
    }

    private void LoadCertificate()
    {
        // Update header
        txtCertTitle.Text = "Certificate of Processing";
        txtCertId.Text = _certificate.CertificateId;

        // Update sync status
        if (_isSynced)
        {
            badgeSynced.Visibility = Visibility.Visible;
            badgePending.Visibility = Visibility.Collapsed;
            txtSyncStatus.Text = "✓ Synced to server";
            txtSyncStatus.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            badgeSynced.Visibility = Visibility.Collapsed;
            badgePending.Visibility = Visibility.Visible;
            txtSyncStatus.Text = "⏳ Pending server sync (will sync automatically)";
            txtSyncStatus.Foreground = System.Windows.Media.Brushes.DarkOrange;
        }

        // Generate and display HTML preview
        try
        {
            var html = _pdfGenerator.GenerateHtml(_certificate, _brandLogo);
            webBrowser.NavigateToString(html);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading certificate preview: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use WebBrowser's print functionality
            dynamic doc = webBrowser.Document;
            doc?.execCommand("Print", true, null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Print error: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void btnExport_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Title = "Export Certificate",
            Filter = "HTML File (*.html)|*.html",
            FileName = $"Certificate_{_certificate.CertificateId}",
            DefaultExt = ".html"
        };

        if (saveDialog.ShowDialog() == true)
        {
            btnExport.IsEnabled = false;
            btnExport.Content = "Exporting...";

            try
            {
                await _pdfGenerator.SaveToFileAsync(_certificate, saveDialog.FileName, _brandLogo);
                
                MessageBox.Show(
                    $"Certificate exported to:\n{saveDialog.FileName}\n\n" +
                    "Tip: Open in a browser and use 'Print to PDF' for PDF format.",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnExport.IsEnabled = true;
                btnExport.Content = "📄 Export HTML";
            }
        }
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
