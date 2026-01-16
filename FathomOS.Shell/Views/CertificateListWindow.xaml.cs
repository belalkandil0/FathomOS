// FathomOS.Shell/Views/CertificateListWindow.xaml.cs
// Window for listing and managing certificates using LicenseManager

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using LicensingSystem.Client;
using LicensingSystem.Shared;
using FathomOS.Core.Certificates;

namespace FathomOS.Shell.Views;

/// <summary>
/// View model for certificate list item
/// </summary>
public class CertificateListItem
{
    public string CertificateId { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string ModuleId { get; set; } = "";
    public DateTime IssuedAt { get; set; }
    public string SignatoryName { get; set; } = "";
    public bool IsSynced { get; set; }
    public bool IsVerified { get; set; }
    public ProcessingCertificate Certificate { get; set; } = null!;
    
    public string SyncStatusDisplay => IsVerified ? "✓ Verified" : (IsSynced ? "✓ Synced" : "⏳ Pending");
}

/// <summary>
/// Window for listing and managing certificates
/// </summary>
public partial class CertificateListWindow : Window
{
    private readonly LicenseManager _licenseManager;
    private readonly ObservableCollection<CertificateListItem> _certificates = new();
    private readonly List<CertificateListItem> _allCertificates = new();
    private readonly string? _brandLogo;

    public CertificateListWindow(LicenseManager licenseManager, string? brandLogo = null)
    {
        InitializeComponent();
        
        _licenseManager = licenseManager;
        _brandLogo = brandLogo;
        dgCertificates.ItemsSource = _certificates;
        
        Loaded += CertificateListWindow_Loaded;
    }

    private void CertificateListWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadCertificates();
    }

    private void LoadCertificates()
    {
        try
        {
            txtStatus.Text = "Loading certificates...";
            
            _allCertificates.Clear();
            
            // Get all local certificates from LicenseManager
            var localCerts = _licenseManager.GetLocalCertificates();
            
            foreach (var entry in localCerts)
            {
                _allCertificates.Add(new CertificateListItem
                {
                    CertificateId = entry.CertificateId,
                    ProjectName = entry.Certificate.ProjectName,
                    ModuleId = entry.Certificate.ModuleId,
                    IssuedAt = entry.Certificate.IssuedAt,
                    SignatoryName = entry.Certificate.SignatoryName,
                    IsSynced = entry.IsSyncedToServer,
                    IsVerified = entry.IsVerifiedByServer,
                    Certificate = entry.Certificate
                });
            }
            
            RefreshList();
            
            // Update stats
            var stats = _licenseManager.GetLocalCertificateStats();
            txtCertCount.Text = $"{stats.TotalLocal} certificate(s)";
            
            if (stats.PendingSync > 0)
            {
                txtSyncStatus.Text = $"⏳ {stats.PendingSync} pending sync";
            }
            else if (stats.PendingVerification > 0)
            {
                txtSyncStatus.Text = $"⏳ {stats.PendingVerification} pending verification";
            }
            else
            {
                txtSyncStatus.Text = "✓ All synced";
            }
            
            txtStatus.Text = "Double-click a certificate to view details";
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Error loading certificates: {ex.Message}";
        }
    }

    private void RefreshList()
    {
        var filtered = _allCertificates.AsEnumerable();

        // Apply search filter
        var searchText = txtSearch.Text?.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(c =>
                c.CertificateId.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.ProjectName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.SignatoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.ModuleId.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        _certificates.Clear();
        foreach (var cert in filtered.OrderByDescending(c => c.IssuedAt))
        {
            _certificates.Add(cert);
        }

        txtStatus.Text = $"Showing {_certificates.Count} of {_allCertificates.Count} certificates";
    }

    private void txtSearch_KeyUp(object sender, KeyEventArgs e)
    {
        RefreshList();
    }

    private void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadCertificates();
    }

    private void dgCertificates_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelectedCertificate();
    }

    private void btnView_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedCertificate();
    }

    private void OpenSelectedCertificate()
    {
        if (dgCertificates.SelectedItem is CertificateListItem item)
        {
            var viewer = new CertificateViewerWindow(item.Certificate, item.IsSynced, _brandLogo);
            viewer.Owner = this;
            viewer.ShowDialog();
        }
        else
        {
            MessageBox.Show("Please select a certificate to view.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void btnExport_Click(object sender, RoutedEventArgs e)
    {
        if (dgCertificates.SelectedItem is not CertificateListItem item)
        {
            MessageBox.Show("Please select a certificate to export.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Export Certificate",
            Filter = "HTML File (*.html)|*.html",
            FileName = $"Certificate_{item.CertificateId}",
            DefaultExt = ".html"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var generator = new CertificatePdfGenerator();
                await generator.SaveToFileAsync(item.Certificate, saveDialog.FileName, _brandLogo);
                
                MessageBox.Show($"Certificate exported to:\n{saveDialog.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
