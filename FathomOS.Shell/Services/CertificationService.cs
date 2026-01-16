using FathomOS.Core.Certificates;
using FathomOS.Core.Interfaces;
using FathomOS.Core.Messaging;
using LicensingSystem.Client;
using System.Windows;

namespace FathomOS.Shell.Services;

/// <summary>
/// Implementation of ICertificationService that wraps the LicenseManager certificate functionality.
/// Provides DI-friendly access to certificate operations.
/// </summary>
public class CertificationService : ICertificationService
{
    private readonly Func<LicenseManager> _getLicenseManager;
    private readonly IEventAggregator? _eventAggregator;
    private const string VerificationBaseUrl = "https://verify.fathom.io/c/";

    public CertificationService(Func<LicenseManager> getLicenseManager, IEventAggregator? eventAggregator = null)
    {
        _getLicenseManager = getLicenseManager ?? throw new ArgumentNullException(nameof(getLicenseManager));
        _eventAggregator = eventAggregator;
    }

    private LicenseManager LicenseManager => _getLicenseManager();

    /// <inheritdoc />
    public event EventHandler<string>? CertificateCreated;

    /// <inheritdoc />
    public event EventHandler<int>? CertificatesSynced;

    /// <inheritdoc />
    public async Task<string?> CreateWithDialogAsync(CertificationRequest request, Window? owner = null)
    {
        var cert = await CertificateHelper.CreateWithDialogAsync(
            LicenseManager,
            request.ModuleId,
            request.ModuleCertificateCode,
            request.ModuleVersion,
            request.ProjectName,
            request.ProcessingData,
            request.InputFiles,
            request.OutputFiles,
            request.ProjectLocation,
            request.VesselName,
            request.ClientName,
            owner
        );

        if (cert != null)
        {
            OnCertificateCreated(cert.CertificateId);
            return cert.CertificateId;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<string> CreateSilentAsync(CertificationRequest request, CertificateSignatory signatory)
    {
        var cert = await CertificateHelper.CreateSilentAsync(
            LicenseManager,
            request.ModuleId,
            request.ModuleCertificateCode,
            request.ModuleVersion,
            request.ProjectName,
            signatory.Name,
            signatory.CompanyName,
            request.ProcessingData,
            request.InputFiles,
            request.OutputFiles,
            request.ProjectLocation,
            request.VesselName,
            request.ClientName,
            signatory.Title
        );

        OnCertificateCreated(cert.CertificateId);
        return cert.CertificateId;
    }

    /// <inheritdoc />
    public Task<CertificateVerificationResult> VerifyAsync(string certificateId)
    {
        var entry = LicenseManager.GetLocalCertificate(certificateId);

        if (entry == null)
        {
            return Task.FromResult(new CertificateVerificationResult
            {
                IsValid = false,
                WasFound = false,
                CertificateId = certificateId,
                VerificationMessage = "Certificate not found in local storage"
            });
        }

        return Task.FromResult(new CertificateVerificationResult
        {
            IsValid = true,
            WasFound = true,
            CertificateId = certificateId,
            ModuleId = entry.Certificate.ModuleId,
            CreatedAt = entry.Certificate.CreatedAt,
            VerificationMessage = entry.IsSyncedToServer
                ? "Certificate verified and synced to server"
                : "Certificate verified locally (pending sync)"
        });
    }

    /// <inheritdoc />
    public Task<IEnumerable<CertificateSummary>> GetPendingSyncAsync()
    {
        var entries = LicenseManager.GetAllCertificates()
            .Where(e => !e.IsSyncedToServer)
            .Select(e => new CertificateSummary
            {
                CertificateId = e.Certificate.CertificateId,
                ModuleId = e.Certificate.ModuleId,
                ProjectName = e.Certificate.ProjectName,
                CreatedAt = e.Certificate.CreatedAt,
                SyncStatus = CertificateSyncStatus.Pending,
                ClientCode = GetClientCode()
            });

        return Task.FromResult(entries);
    }

    /// <inheritdoc />
    public Task<IEnumerable<CertificateSummary>> GetAllCertificatesAsync()
    {
        var entries = LicenseManager.GetAllCertificates()
            .Select(e => new CertificateSummary
            {
                CertificateId = e.Certificate.CertificateId,
                ModuleId = e.Certificate.ModuleId,
                ProjectName = e.Certificate.ProjectName,
                CreatedAt = e.Certificate.CreatedAt,
                SyncStatus = e.IsSyncedToServer ? CertificateSyncStatus.Synced : CertificateSyncStatus.Pending,
                ClientCode = GetClientCode()
            });

        return Task.FromResult(entries);
    }

    /// <inheritdoc />
    public async Task<int> SyncAsync()
    {
        var pendingBefore = LicenseManager.GetAllCertificates()
            .Count(e => !e.IsSyncedToServer);

        if (pendingBefore == 0) return 0;

        await LicenseManager.SyncPendingCertificatesAsync();

        var pendingAfter = LicenseManager.GetAllCertificates()
            .Count(e => !e.IsSyncedToServer);

        var syncedCount = pendingBefore - pendingAfter;

        if (syncedCount > 0)
        {
            CertificatesSynced?.Invoke(this, syncedCount);
            _eventAggregator?.Publish(new CertificatesSyncedEvent(syncedCount, 0));
        }

        return syncedCount;
    }

    /// <inheritdoc />
    public void OpenCertificateManager(Window? owner = null)
    {
        _ = Task.Run(async () =>
        {
            string? brandLogo = null;
            try
            {
                var (logoUrl, logoBase64, error) = await LicenseManager.GetBrandLogoAsync();
                brandLogo = logoBase64 ?? logoUrl;
            }
            catch { /* Ignore logo errors */ }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CertificateHelper.OpenCertificateManager(LicenseManager, owner, brandLogo);
            });
        });
    }

    /// <inheritdoc />
    public void ViewCertificate(string certificateId, Window? owner = null)
    {
        var entry = LicenseManager.GetLocalCertificate(certificateId);
        if (entry == null)
        {
            MessageBox.Show($"Certificate {certificateId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _ = Task.Run(async () =>
        {
            string? brandLogo = null;
            try
            {
                var (logoUrl, logoBase64, error) = await LicenseManager.GetBrandLogoAsync();
                brandLogo = logoBase64 ?? logoUrl;
            }
            catch { /* Ignore logo errors */ }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CertificateHelper.ViewCertificate(entry.Certificate, entry.IsSyncedToServer, brandLogo, owner);
            });
        });
    }

    /// <inheritdoc />
    public string GetVerificationUrl(string certificateId)
    {
        return $"{VerificationBaseUrl}{certificateId}";
    }

    /// <inheritdoc />
    public string? GetClientCode()
    {
        var brandingInfo = LicenseManager.GetBrandingInfo();
        return brandingInfo?.ClientCode;
    }

    private void OnCertificateCreated(string certificateId)
    {
        CertificateCreated?.Invoke(this, certificateId);
        _eventAggregator?.Publish(new CertificateCreatedEvent(
            certificateId,
            certificateId.Split('-').FirstOrDefault() ?? "Unknown",
            DateTime.UtcNow
        ));
    }
}
