// FathomOS.Shell/Views/CertificateViewerWindow.xaml.cs
// Window for viewing and exporting certificates with PDF and HTML support

using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using LicensingSystem.Shared;
using FathomOS.Core.Certificates;

namespace FathomOS.Shell.Views;

/// <summary>
/// Window for viewing a certificate with dual PDF/HTML export capability
/// </summary>
public partial class CertificateViewerWindow : Window
{
    private readonly ProcessingCertificate _certificate;
    private readonly CertificatePdfGenerator _htmlGenerator;
    private readonly ICertificatePdfService _pdfService;
    private readonly bool _isSynced;
    private readonly string? _brandLogo;

    public CertificateViewerWindow(ProcessingCertificate certificate, bool isSynced = false, string? brandLogo = null)
    {
        InitializeComponent();

        _certificate = certificate;
        _isSynced = isSynced;
        _brandLogo = brandLogo;
        _htmlGenerator = new CertificatePdfGenerator();
        _pdfService = new CertificatePdfService();

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
            txtSyncStatus.Text = "Synced to server";
            txtSyncStatus.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            badgeSynced.Visibility = Visibility.Collapsed;
            badgePending.Visibility = Visibility.Visible;
            txtSyncStatus.Text = "Pending server sync (will sync automatically)";
            txtSyncStatus.Foreground = System.Windows.Media.Brushes.DarkOrange;
        }

        // Generate and display HTML preview
        try
        {
            var html = _htmlGenerator.GenerateHtml(_certificate, _brandLogo);
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
            Filter = "PDF Document (*.pdf)|*.pdf|HTML File (*.html)|*.html",
            FileName = $"Certificate_{_certificate.CertificateId}",
            DefaultExt = ".pdf",
            FilterIndex = 1
        };

        if (saveDialog.ShowDialog() == true)
        {
            btnExport.IsEnabled = false;
            btnExport.Content = "Exporting...";

            try
            {
                var extension = Path.GetExtension(saveDialog.FileName).ToLowerInvariant();

                if (extension == ".pdf")
                {
                    // Use CertificatePdfService for PDF export
                    var certificate = _certificate.ToCertificate();
                    var options = new CertificatePdfOptions
                    {
                        BrandLogo = _brandLogo,
                        IncludeQrCode = true
                    };

                    var result = await _pdfService.GeneratePdfToFileAsync(certificate, saveDialog.FileName, options);

                    if (result.Success)
                    {
                        MessageBox.Show(
                            $"Certificate exported to:\n{saveDialog.FileName}",
                            "Export Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Export error: {result.ErrorMessage}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Use HTML generator for HTML export
                    await _htmlGenerator.SaveToFileAsync(_certificate, saveDialog.FileName, _brandLogo);

                    MessageBox.Show(
                        $"Certificate exported to:\n{saveDialog.FileName}\n\n" +
                        "Tip: Open in a browser and use 'Print to PDF' for additional PDF formatting options.",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnExport.IsEnabled = true;
                btnExport.Content = "Export";
            }
        }
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
