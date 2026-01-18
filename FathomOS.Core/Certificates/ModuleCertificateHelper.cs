using System.Security.Cryptography;
using System.Text;
using System.Windows;
using FathomOS.Core.Interfaces;

namespace FathomOS.Core.Certificates;

/// <summary>
/// Helper class for modules to integrate with the certification system.
/// Provides standardized methods for generating certificate requests and computing data hashes.
/// </summary>
public static class ModuleCertificateHelper
{
    /// <summary>
    /// Creates a certificate request with standard fields populated.
    /// </summary>
    /// <param name="moduleId">Module identifier.</param>
    /// <param name="moduleCertificateCode">Short code for certificate (e.g., "SL", "EI").</param>
    /// <param name="moduleVersion">Module version string.</param>
    /// <param name="projectName">Project name.</param>
    /// <param name="processingData">Module-specific processing data.</param>
    /// <returns>A populated CertificationRequest ready for submission.</returns>
    public static CertificationRequest CreateRequest(
        string moduleId,
        string moduleCertificateCode,
        string moduleVersion,
        string projectName,
        Dictionary<string, string>? processingData = null)
    {
        return new CertificationRequest
        {
            ModuleId = moduleId,
            ModuleCertificateCode = moduleCertificateCode,
            ModuleVersion = moduleVersion,
            ProjectName = projectName,
            ProcessingData = processingData ?? new Dictionary<string, string>(),
            DataHash = null
        };
    }

    /// <summary>
    /// Computes a SHA256 hash of data for verification purposes.
    /// </summary>
    /// <param name="data">String data to hash.</param>
    /// <returns>Hex-encoded SHA256 hash.</returns>
    public static string ComputeHash(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Computes a SHA256 hash from a collection of values.
    /// </summary>
    /// <param name="values">Values to include in the hash.</param>
    /// <returns>Hex-encoded SHA256 hash.</returns>
    public static string ComputeHash(params object?[] values)
    {
        var sb = new StringBuilder();
        foreach (var value in values)
        {
            if (value != null)
            {
                sb.Append(value.ToString());
                sb.Append('|');
            }
        }
        return ComputeHash(sb.ToString());
    }

    /// <summary>
    /// Computes a hash from a dictionary of processing data.
    /// </summary>
    /// <param name="processingData">Key-value pairs to hash.</param>
    /// <returns>Hex-encoded SHA256 hash.</returns>
    public static string ComputeHashFromData(Dictionary<string, string> processingData)
    {
        if (processingData == null || processingData.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var kvp in processingData.OrderBy(k => k.Key))
        {
            sb.Append(kvp.Key);
            sb.Append(':');
            sb.Append(kvp.Value);
            sb.Append('|');
        }
        return ComputeHash(sb.ToString());
    }

    /// <summary>
    /// Generates a certificate for module processing results.
    /// Shows the certificate dialog and returns the certificate ID if created.
    /// </summary>
    /// <param name="certService">The certification service.</param>
    /// <param name="request">The certification request.</param>
    /// <param name="owner">Parent window for the dialog.</param>
    /// <returns>Certificate ID if created, null if cancelled.</returns>
    public static async Task<string?> GenerateCertificateAsync(
        ICertificationService? certService,
        CertificationRequest request,
        Window? owner = null)
    {
        if (certService == null)
        {
            System.Diagnostics.Debug.WriteLine($"[{request.ModuleId}] Certificate service not available");
            return null;
        }

        try
        {
            // Compute data hash if not already set
            if (string.IsNullOrEmpty(request.DataHash) && request.ProcessingData != null)
            {
                request = request with { DataHash = ComputeHashFromData(request.ProcessingData) };
            }

            var certificateId = await certService.CreateWithDialogAsync(request, owner);

            if (!string.IsNullOrEmpty(certificateId))
            {
                System.Diagnostics.Debug.WriteLine($"[{request.ModuleId}] Certificate created: {certificateId}");
            }

            return certificateId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{request.ModuleId}] Certificate generation failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates a certificate silently without UI (for automated workflows).
    /// </summary>
    /// <param name="certService">The certification service.</param>
    /// <param name="request">The certification request.</param>
    /// <param name="signatory">The signatory information.</param>
    /// <returns>Certificate ID if created, null on failure.</returns>
    public static async Task<string?> GenerateCertificateSilentAsync(
        ICertificationService? certService,
        CertificationRequest request,
        CertificateSignatory signatory)
    {
        if (certService == null)
        {
            System.Diagnostics.Debug.WriteLine($"[{request.ModuleId}] Certificate service not available");
            return null;
        }

        try
        {
            // Compute data hash if not already set
            if (string.IsNullOrEmpty(request.DataHash) && request.ProcessingData != null)
            {
                request = request with { DataHash = ComputeHashFromData(request.ProcessingData) };
            }

            var certificateId = await certService.CreateSilentAsync(request, signatory);
            System.Diagnostics.Debug.WriteLine($"[{request.ModuleId}] Certificate created silently: {certificateId}");
            return certificateId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{request.ModuleId}] Silent certificate generation failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Standard processing data keys used across modules.
    /// </summary>
    public static class DataKeys
    {
        // Project information
        public const string ProjectName = "Project Name";
        public const string ClientName = "Client Name";
        public const string VesselName = "Vessel Name";
        public const string ProjectLocation = "Project Location";

        // Processing information
        public const string ProcessingDate = "Processing Date";
        public const string ProcessedBy = "Processed By";
        public const string SoftwareVersion = "Software Version";

        // Input/Output counts
        public const string InputFiles = "Input Files";
        public const string OutputFiles = "Output Files";
        public const string RecordsProcessed = "Records Processed";
        public const string PointsProcessed = "Points Processed";

        // Quality metrics
        public const string QualityCheck = "Quality Check";
        public const string ValidationStatus = "Validation Status";
        public const string ErrorCount = "Error Count";
        public const string WarningCount = "Warning Count";

        // Survey-specific
        public const string SurveyDate = "Survey Date";
        public const string StartKp = "Start KP";
        public const string EndKp = "End KP";
        public const string RouteLength = "Route Length";
        public const string TideCorrection = "Tide Correction";
        public const string SmoothingApplied = "Smoothing Applied";

        // Equipment-specific
        public const string AssetNumber = "Asset Number";
        public const string SerialNumber = "Serial Number";
        public const string CalibrationDate = "Calibration Date";
        public const string ExpiryDate = "Expiry Date";
    }

    /// <summary>
    /// Standard signatory titles for survey industry certificates.
    /// </summary>
    public static readonly string[] StandardSignatoryTitles = new[]
    {
        "Survey Manager",
        "Project Manager",
        "Party Chief",
        "Lead Surveyor",
        "Senior Surveyor",
        "Online Surveyor",
        "Survey Engineer",
        "QA/QC Engineer",
        "Data Processor",
        "Operations Manager",
        "Technical Manager",
        "Client Representative"
    };
}
