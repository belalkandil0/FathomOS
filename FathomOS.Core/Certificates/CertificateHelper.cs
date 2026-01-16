// FathomOS.Core/Certificates/CertificateHelper.cs
// Helper class for modules to easily create certificates
// Uses delegate pattern to avoid circular dependency with Shell

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using LicensingSystem.Client;
using LicensingSystem.Shared;

namespace FathomOS.Core.Certificates;

/// <summary>
/// Signatory information returned from dialog
/// </summary>
public class SignatoryInfo
{
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool Cancelled { get; set; }
}

/// <summary>
/// Helper class for modules to create certificates easily.
/// Shell must configure the delegates at startup for UI functionality.
/// </summary>
public static class CertificateHelper
{
    #region Delegates (Set by Shell at startup)
    
    /// <summary>
    /// Delegate to show the signatory dialog.
    /// Returns signatory info or null if cancelled.
    /// Set by Shell at startup.
    /// </summary>
    public static Func<string?, Window?, SignatoryInfo?> ShowSignatoryDialog { get; set; } = (companyName, owner) =>
    {
        // Default fallback if Shell hasn't set this
        MessageBox.Show("Certificate UI not initialized. Please restart the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return null;
    };

    /// <summary>
    /// Delegate to show the certificate viewer.
    /// Set by Shell at startup.
    /// </summary>
    public static Action<ProcessingCertificate, bool, string?, Window?> ShowCertificateViewer { get; set; } = (cert, synced, logo, owner) =>
    {
        // Default fallback
        MessageBox.Show($"Certificate created: {cert.CertificateId}\n\nViewer not initialized.", "Certificate Created", MessageBoxButton.OK, MessageBoxImage.Information);
    };

    /// <summary>
    /// Delegate to open the certificate manager window.
    /// Set by Shell at startup.
    /// </summary>
    public static Action<LicenseManager, string?, Window?> ShowCertificateManager { get; set; } = (mgr, logo, owner) =>
    {
        // Default fallback
        MessageBox.Show("Certificate Manager not initialized. Please restart the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    };
    
    #endregion

    /// <summary>
    /// Creates a certificate with UI dialog for signatory information.
    /// Shows the SignatoryDialog, creates the certificate, and shows the viewer.
    /// </summary>
    /// <param name="licenseManager">The license manager instance</param>
    /// <param name="moduleId">Module ID (e.g., "SurveyListing")</param>
    /// <param name="moduleCertificateCode">Module code for certificate ID (e.g., "SL")</param>
    /// <param name="moduleVersion">Module version</param>
    /// <param name="projectName">Project name</param>
    /// <param name="processingData">Module-specific processing data</param>
    /// <param name="inputFiles">List of input file names</param>
    /// <param name="outputFiles">List of output file names</param>
    /// <param name="projectLocation">Optional project location</param>
    /// <param name="vessel">Optional vessel name</param>
    /// <param name="client">Optional client name</param>
    /// <param name="owner">Parent window for dialogs</param>
    /// <returns>The created certificate, or null if user cancelled</returns>
    public static async Task<ProcessingCertificate?> CreateWithDialogAsync(
        LicenseManager licenseManager,
        string moduleId,
        string moduleCertificateCode,
        string moduleVersion,
        string projectName,
        Dictionary<string, string>? processingData = null,
        List<string>? inputFiles = null,
        List<string>? outputFiles = null,
        string? projectLocation = null,
        string? vessel = null,
        string? client = null,
        Window? owner = null)
    {
        // Get branding info
        var brandingInfo = licenseManager.GetBrandingInfo();
        
        // Show signatory dialog via delegate
        var signatoryInfo = ShowSignatoryDialog(brandingInfo?.Brand, owner);
        if (signatoryInfo == null || signatoryInfo.Cancelled)
        {
            return null; // User cancelled
        }
        
        // Create the certificate
        var certificate = await licenseManager.CreateCertificateAsync(
            moduleId: moduleId,
            moduleCertificateCode: moduleCertificateCode,
            moduleVersion: moduleVersion,
            projectName: projectName,
            signatoryName: signatoryInfo.SignatoryName,
            companyName: signatoryInfo.CompanyName,
            processingData: processingData,
            inputFiles: inputFiles,
            outputFiles: outputFiles,
            projectLocation: projectLocation,
            vessel: vessel,
            client: client,
            signatoryTitle: signatoryInfo.SignatoryTitle
        );
        
        // Get brand logo for viewer
        string? brandLogo = null;
        try
        {
            var (logoUrl, logoBase64, error) = await licenseManager.GetBrandLogoAsync();
            brandLogo = logoBase64 ?? logoUrl;
        }
        catch { /* Ignore logo errors */ }
        
        // Get sync status
        var entry = licenseManager.GetLocalCertificate(certificate.CertificateId);
        var isSynced = entry?.IsSyncedToServer ?? false;
        
        // Show the certificate viewer via delegate
        ShowCertificateViewer(certificate, isSynced, brandLogo, owner);
        
        return certificate;
    }

    /// <summary>
    /// Creates a certificate without UI (for batch processing or automated workflows)
    /// </summary>
    public static async Task<ProcessingCertificate> CreateSilentAsync(
        LicenseManager licenseManager,
        string moduleId,
        string moduleCertificateCode,
        string moduleVersion,
        string projectName,
        string signatoryName,
        string companyName,
        Dictionary<string, string>? processingData = null,
        List<string>? inputFiles = null,
        List<string>? outputFiles = null,
        string? projectLocation = null,
        string? vessel = null,
        string? client = null,
        string? signatoryTitle = null)
    {
        return await licenseManager.CreateCertificateAsync(
            moduleId: moduleId,
            moduleCertificateCode: moduleCertificateCode,
            moduleVersion: moduleVersion,
            projectName: projectName,
            signatoryName: signatoryName,
            companyName: companyName,
            processingData: processingData,
            inputFiles: inputFiles,
            outputFiles: outputFiles,
            projectLocation: projectLocation,
            vessel: vessel,
            client: client,
            signatoryTitle: signatoryTitle
        );
    }

    /// <summary>
    /// Opens the certificate manager window
    /// </summary>
    public static void OpenCertificateManager(LicenseManager licenseManager, Window? owner = null, string? brandLogo = null)
    {
        ShowCertificateManager(licenseManager, brandLogo, owner);
    }

    /// <summary>
    /// Views a specific certificate
    /// </summary>
    public static void ViewCertificate(ProcessingCertificate certificate, bool isSynced = false, string? brandLogo = null, Window? owner = null)
    {
        ShowCertificateViewer(certificate, isSynced, brandLogo, owner);
    }

    /// <summary>
    /// Computes MD5 hash of a file (for file verification in certificates)
    /// </summary>
    public static string ComputeFileHash(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = md5.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Creates a QuickCertificateBuilder for fluent API certificate creation
    /// </summary>
    public static QuickCertificateBuilder QuickCreate(LicenseManager licenseManager)
    {
        return new QuickCertificateBuilder(licenseManager);
    }
}

/// <summary>
/// Fluent builder for creating certificates with minimal code
/// </summary>
public class QuickCertificateBuilder
{
    private readonly LicenseManager _licenseManager;
    private string _moduleId = "";
    private string _moduleCertificateCode = "";
    private string _moduleVersion = "";
    private string _projectName = "";
    private string? _projectLocation;
    private string? _vessel;
    private string? _client;
    private readonly Dictionary<string, string> _processingData = new();
    private readonly List<string> _inputFiles = new();
    private readonly List<string> _outputFiles = new();

    public QuickCertificateBuilder(LicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
    }

    /// <summary>
    /// Sets the module information
    /// </summary>
    public QuickCertificateBuilder ForModule(string moduleId, string certificateCode, string version)
    {
        _moduleId = moduleId;
        _moduleCertificateCode = certificateCode;
        _moduleVersion = version;
        return this;
    }

    /// <summary>
    /// Sets project information
    /// </summary>
    public QuickCertificateBuilder WithProject(string name, string? location = null)
    {
        _projectName = name;
        _projectLocation = location;
        return this;
    }

    /// <summary>
    /// Sets vessel name
    /// </summary>
    public QuickCertificateBuilder WithVessel(string vessel)
    {
        _vessel = vessel;
        return this;
    }

    /// <summary>
    /// Sets client name
    /// </summary>
    public QuickCertificateBuilder WithClient(string client)
    {
        _client = client;
        return this;
    }

    /// <summary>
    /// Adds a processing data entry
    /// </summary>
    public QuickCertificateBuilder AddData(string key, string value)
    {
        _processingData[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple processing data entries
    /// </summary>
    public QuickCertificateBuilder WithData(Dictionary<string, string> data)
    {
        foreach (var kvp in data)
        {
            _processingData[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Adds an input file (extracts filename from path)
    /// </summary>
    public QuickCertificateBuilder AddInputFile(string filePath)
    {
        _inputFiles.Add(Path.GetFileName(filePath));
        return this;
    }

    /// <summary>
    /// Adds multiple input files
    /// </summary>
    public QuickCertificateBuilder AddInputFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            _inputFiles.Add(Path.GetFileName(path));
        }
        return this;
    }

    /// <summary>
    /// Adds an output file (extracts filename from path)
    /// </summary>
    public QuickCertificateBuilder AddOutputFile(string filePath)
    {
        _outputFiles.Add(Path.GetFileName(filePath));
        return this;
    }

    /// <summary>
    /// Adds multiple output files
    /// </summary>
    public QuickCertificateBuilder AddOutputFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            _outputFiles.Add(Path.GetFileName(path));
        }
        return this;
    }

    /// <summary>
    /// Creates the certificate with UI dialog
    /// </summary>
    public async Task<ProcessingCertificate?> CreateWithDialogAsync(Window? owner = null)
    {
        return await CertificateHelper.CreateWithDialogAsync(
            _licenseManager,
            _moduleId,
            _moduleCertificateCode,
            _moduleVersion,
            _projectName,
            _processingData.Count > 0 ? _processingData : null,
            _inputFiles.Count > 0 ? _inputFiles : null,
            _outputFiles.Count > 0 ? _outputFiles : null,
            _projectLocation,
            _vessel,
            _client,
            owner
        );
    }

    /// <summary>
    /// Creates the certificate without UI (requires signatory info)
    /// </summary>
    public async Task<ProcessingCertificate> CreateAsync(string signatoryName, string companyName, string? signatoryTitle = null)
    {
        return await CertificateHelper.CreateSilentAsync(
            _licenseManager,
            _moduleId,
            _moduleCertificateCode,
            _moduleVersion,
            _projectName,
            signatoryName,
            companyName,
            _processingData.Count > 0 ? _processingData : null,
            _inputFiles.Count > 0 ? _inputFiles : null,
            _outputFiles.Count > 0 ? _outputFiles : null,
            _projectLocation,
            _vessel,
            _client,
            signatoryTitle
        );
    }
}
