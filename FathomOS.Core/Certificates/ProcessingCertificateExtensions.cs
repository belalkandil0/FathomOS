// FathomOS.Core/Certificates/ProcessingCertificateExtensions.cs
// Extension methods for converting between ProcessingCertificate and Certificate types

using System.Text.Json;
using FathomOS.Core.Models;
using LicensingSystem.Shared;

namespace FathomOS.Core.Certificates;

/// <summary>
/// Extension methods for converting ProcessingCertificate (LicensingSystem.Shared)
/// to Certificate (FathomOS.Core.Models) for use with CertificatePdfService.
/// </summary>
public static class ProcessingCertificateExtensions
{
    /// <summary>
    /// Converts a ProcessingCertificate to a Certificate model for PDF generation.
    /// </summary>
    /// <param name="source">The ProcessingCertificate from LicensingSystem.Shared</param>
    /// <returns>A Certificate model compatible with CertificatePdfService</returns>
    public static Certificate ToCertificate(this ProcessingCertificate source)
    {
        return new Certificate
        {
            // Primary Identity
            CertificateId = source.CertificateId,

            // License Information
            LicenseId = source.LicenseId,
            LicenseeCode = source.LicenseeCode,

            // Module Information
            ModuleId = source.ModuleId,
            ModuleCertificateCode = source.ModuleCertificateCode,
            ModuleVersion = source.ModuleVersion,

            // Certificate Metadata
            IssuedAt = source.IssuedAt,
            ProjectName = source.ProjectName,
            ProjectLocation = source.ProjectLocation,
            Vessel = source.Vessel,
            Client = source.Client,

            // Signatory Information
            SignatoryName = source.SignatoryName,
            SignatoryTitle = source.SignatoryTitle,
            CompanyName = source.CompanyName,

            // Processing Data - serialize to JSON
            ProcessingDataJson = source.ProcessingData.Count > 0
                ? JsonSerializer.Serialize(source.ProcessingData)
                : null,
            InputFilesJson = source.InputFiles.Count > 0
                ? JsonSerializer.Serialize(source.InputFiles)
                : null,
            OutputFilesJson = source.OutputFiles.Count > 0
                ? JsonSerializer.Serialize(source.OutputFiles)
                : null,

            // Cryptographic Signature
            Signature = source.Signature,
            SignatureAlgorithm = source.SignatureAlgorithm,

            // Sync Status - defaults for new conversion
            SyncStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts a Certificate model back to a ProcessingCertificate.
    /// </summary>
    /// <param name="source">The Certificate from FathomOS.Core.Models</param>
    /// <returns>A ProcessingCertificate compatible with LicensingSystem</returns>
    public static ProcessingCertificate ToProcessingCertificate(this Certificate source)
    {
        var processingData = string.IsNullOrEmpty(source.ProcessingDataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(source.ProcessingDataJson)
              ?? new Dictionary<string, string>();

        var inputFiles = string.IsNullOrEmpty(source.InputFilesJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(source.InputFilesJson)
              ?? new List<string>();

        var outputFiles = string.IsNullOrEmpty(source.OutputFilesJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(source.OutputFilesJson)
              ?? new List<string>();

        return new ProcessingCertificate
        {
            CertificateId = source.CertificateId,
            LicenseId = source.LicenseId,
            LicenseeCode = source.LicenseeCode,
            ModuleId = source.ModuleId,
            ModuleCertificateCode = source.ModuleCertificateCode,
            ModuleVersion = source.ModuleVersion,
            IssuedAt = source.IssuedAt,
            ProjectName = source.ProjectName,
            ProjectLocation = source.ProjectLocation,
            Vessel = source.Vessel,
            Client = source.Client,
            SignatoryName = source.SignatoryName,
            SignatoryTitle = source.SignatoryTitle,
            CompanyName = source.CompanyName,
            ProcessingData = processingData,
            InputFiles = inputFiles,
            OutputFiles = outputFiles,
            Signature = source.Signature,
            SignatureAlgorithm = source.SignatureAlgorithm
        };
    }
}
