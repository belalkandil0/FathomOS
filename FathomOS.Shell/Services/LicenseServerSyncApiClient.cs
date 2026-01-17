// FathomOS.Shell/Services/LicenseServerSyncApiClient.cs
// ISyncApiClient implementation that uses the LicenseManager for server communication

using FathomOS.Core.Data;
using FathomOS.Core.Models;
using LicensingSystem.Client;
using LicensingSystem.Shared;
using System.Diagnostics;

namespace FathomOS.Shell.Services;

/// <summary>
/// Implementation of ISyncApiClient that uses the LicenseManager for server communication.
/// Bridges the new certificate sync engine with the existing licensing system.
/// </summary>
public class LicenseServerSyncApiClient : ISyncApiClient
{
    private readonly Func<LicenseManager> _getLicenseManager;

    /// <summary>
    /// Creates a new LicenseServerSyncApiClient instance.
    /// </summary>
    /// <param name="getLicenseManager">Function to get the LicenseManager instance</param>
    public LicenseServerSyncApiClient(Func<LicenseManager> getLicenseManager)
    {
        _getLicenseManager = getLicenseManager ?? throw new ArgumentNullException(nameof(getLicenseManager));
    }

    private LicenseManager LicenseManager => _getLicenseManager();

    /// <inheritdoc/>
    public async Task<bool> PushCertificateAsync(Certificate certificate)
    {
        try
        {
            // Convert our Certificate model to LicensingSystem.Shared.ProcessingCertificate
            var processingCert = ConvertToProcessingCertificate(certificate);

            // Use LicenseManager to sync the certificate
            // The LicenseManager handles the actual HTTP communication
            var result = await LicenseManager.SyncCertificatesAsync(new List<ProcessingCertificate> { processingCert });

            var success = result?.Success == true;

            if (success)
            {
                Debug.WriteLine($"LicenseServerSyncApiClient: Certificate {certificate.CertificateId} synced successfully");
            }
            else
            {
                Debug.WriteLine($"LicenseServerSyncApiClient: Certificate {certificate.CertificateId} sync failed - {result?.Message}");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LicenseServerSyncApiClient: Error syncing certificate {certificate.CertificateId}: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            // Use LicenseManager's connectivity check
            return await LicenseManager.IsOnlineAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LicenseServerSyncApiClient: Error checking server connectivity: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Converts our Certificate model to the LicensingSystem ProcessingCertificate.
    /// </summary>
    private static ProcessingCertificate ConvertToProcessingCertificate(Certificate cert)
    {
        var processingCert = new ProcessingCertificate
        {
            CertificateId = cert.CertificateId,
            LicenseId = cert.LicenseId,
            LicenseeCode = cert.LicenseeCode,
            ModuleId = cert.ModuleId,
            ModuleCertificateCode = cert.ModuleCertificateCode,
            ModuleVersion = cert.ModuleVersion,
            IssuedAt = cert.IssuedAt,
            ProjectName = cert.ProjectName,
            ProjectLocation = cert.ProjectLocation,
            Vessel = cert.Vessel,
            Client = cert.Client,
            SignatoryName = cert.SignatoryName,
            SignatoryTitle = cert.SignatoryTitle,
            CompanyName = cert.CompanyName,
            Signature = cert.Signature,
            SignatureAlgorithm = cert.SignatureAlgorithm
        };

        // Convert JSON fields to their dictionary/list equivalents
        if (!string.IsNullOrEmpty(cert.ProcessingDataJson))
        {
            try
            {
                processingCert.ProcessingData = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, string>>(cert.ProcessingDataJson) ?? new();
            }
            catch { /* Ignore deserialization errors */ }
        }

        if (!string.IsNullOrEmpty(cert.InputFilesJson))
        {
            try
            {
                processingCert.InputFiles = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(cert.InputFilesJson) ?? new();
            }
            catch { /* Ignore deserialization errors */ }
        }

        if (!string.IsNullOrEmpty(cert.OutputFilesJson))
        {
            try
            {
                processingCert.OutputFiles = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(cert.OutputFilesJson) ?? new();
            }
            catch { /* Ignore deserialization errors */ }
        }

        return processingCert;
    }
}
