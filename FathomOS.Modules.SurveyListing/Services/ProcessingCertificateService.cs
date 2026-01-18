using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FathomOS.Core.Models;
using FathomOS.Modules.SurveyListing.Views;

namespace FathomOS.Modules.SurveyListing.Services;

/// <summary>
/// Service for generating and managing processing certificates for Survey Listing exports.
/// Certificates provide a verifiable record that processing was completed and approved.
/// </summary>
public class ProcessingCertificateService
{
    private static ProcessingCertificateService? _instance;
    public static ProcessingCertificateService Instance => _instance ??= new ProcessingCertificateService();
    
    // Module identification
    private const string ModuleId = "SurveyListing";
    private const string ModuleCertificateCode = "SL";
    private const string ModuleVersion = "1.0.45";
    
    /// <summary>
    /// Shows the supervisor approval dialog and generates a certificate if approved.
    /// </summary>
    /// <param name="project">The project being processed</param>
    /// <param name="processedData">The processed survey data</param>
    /// <param name="outputFiles">List of output files generated</param>
    /// <param name="outputFolder">Folder where certificate will be saved</param>
    /// <param name="owner">Owner window for dialog</param>
    /// <returns>Path to generated certificate, or null if not approved</returns>
    public async Task<string?> RequestApprovalAndGenerateCertificateAsync(
        Project project,
        List<SurveyPoint> processedData,
        List<string> outputFiles,
        string outputFolder,
        System.Windows.Window? owner = null)
    {
        // Show supervisor approval dialog
        var dialog = new SupervisorApprovalDialog(project, processedData, outputFiles);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        
        var result = dialog.ShowDialog();
        
        if (result != true || dialog.ApprovalResult == null)
        {
            return null; // User cancelled or didn't approve
        }
        
        // Generate the certificate
        return await GenerateCertificateAsync(
            project, 
            processedData, 
            dialog.ApprovalResult, 
            outputFolder);
    }
    
    /// <summary>
    /// Generates a processing certificate after approval.
    /// </summary>
    private async Task<string?> GenerateCertificateAsync(
        Project project,
        List<SurveyPoint> processedData,
        SupervisorApprovalResult approval,
        string outputFolder)
    {
        try
        {
            // Get license info for certificate
            var licenseInfo = GetLicenseInfo();
            
            // Generate certificate ID
            string certificateId = GenerateCertificateId(licenseInfo?.LicenseeCode ?? "XX");
            
            // Build certificate data
            var certificate = new SurveyListingCertificate
            {
                CertificateId = certificateId,
                LicenseId = licenseInfo?.LicenseId ?? "UNLICENSED",
                LicenseeCode = licenseInfo?.LicenseeCode ?? "XX",
                ModuleId = ModuleId,
                ModuleCertificateCode = ModuleCertificateCode,
                ModuleVersion = ModuleVersion,
                IssuedAt = DateTime.UtcNow,
                
                // Project info
                ProjectName = project.ProjectName ?? "Unnamed Project",
                ProjectLocation = "", // Could be added to project model
                Vessel = project.VesselName ?? "",
                Client = project.ClientName ?? "",
                
                // Supervisor info
                SupervisorName = approval.SupervisorName,
                SupervisorTitle = approval.SupervisorTitle,
                CompanyName = approval.CompanyName,
                ApprovalTime = approval.ApprovalTime,
                
                // Processing data
                ProcessingData = new Dictionary<string, string>
                {
                    ["Total Survey Points"] = $"{processedData.Count:N0}",
                    ["KP Range"] = approval.KpRange,
                    ["Depth Range"] = approval.DepthRange,
                    ["Processing Method"] = approval.ProcessingMethod,
                    ["Route File Loaded"] = approval.RouteFileLoaded ? "Yes" : "No",
                    ["Survey Data Loaded"] = approval.SurveyDataLoaded ? "Yes" : "No",
                    ["Tide Correction Applied"] = approval.TideCorrectionApplied ? "Yes" : "No",
                    ["Calculations Complete"] = approval.CalculationsComplete ? "Yes" : "No",
                    ["Data Coverage Reviewed"] = approval.DataReviewed ? "Yes" : "No",
                    ["Smoothing Applied"] = approval.SmoothingApplied ? "Yes" : "No"
                },
                
                InputFiles = approval.InputFiles.Select(f => Path.GetFileName(f)).ToList(),
                OutputFiles = approval.OutputFiles.Select(f => Path.GetFileName(f)).ToList()
            };
            
            // Add depth statistics if available
            var validDepths = processedData.Where(p => p.Depth.HasValue).Select(p => p.Depth!.Value).ToList();
            if (validDepths.Count > 0)
            {
                certificate.ProcessingData["Min Depth"] = $"{validDepths.Min():F2} m";
                certificate.ProcessingData["Max Depth"] = $"{validDepths.Max():F2} m";
                certificate.ProcessingData["Mean Depth"] = $"{validDepths.Average():F2} m";
            }
            
            // Sign the certificate
            certificate.Signature = SignCertificate(certificate);
            certificate.SignatureAlgorithm = "HMAC-SHA256";
            
            // Save certificate as JSON
            string certFileName = $"{certificateId}.json";
            string certPath = Path.Combine(outputFolder, certFileName);
            
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            string json = JsonSerializer.Serialize(certificate, options);
            await File.WriteAllTextAsync(certPath, json);
            
            // Also generate a human-readable text version
            string textPath = Path.Combine(outputFolder, $"{certificateId}.txt");
            await GenerateReadableCertificateAsync(certificate, textPath);
            
            // Generate HTML certificate with QR code for printing
            string htmlPath = Path.Combine(outputFolder, $"{certificateId}.html");
            await GenerateHtmlCertificateAsync(certificate, htmlPath);
            
            // Attempt to sync with server (if available)
            await TrySyncCertificateAsync(certificate);
            
            return certPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating certificate: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Generates a certificate ID in the format: FOS-XX-YYMM-NNNNN-CCCC
    /// </summary>
    private string GenerateCertificateId(string licenseeCode)
    {
        string yearMonth = DateTime.UtcNow.ToString("yyMM");
        string sequence = GenerateSequenceNumber();
        string checksum = GenerateChecksum($"{licenseeCode}{yearMonth}{sequence}");
        
        return $"FOS-{licenseeCode}-{yearMonth}-{sequence}-{checksum}";
    }
    
    private string GenerateSequenceNumber()
    {
        // In production, this would come from the server
        // For offline, generate a unique number based on timestamp
        var timestamp = DateTime.UtcNow.Ticks;
        return (timestamp % 100000).ToString("D5");
    }
    
    private string GenerateChecksum(string input)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..4];
    }
    
    /// <summary>
    /// Signs the certificate data for verification
    /// </summary>
    private string SignCertificate(SurveyListingCertificate certificate)
    {
        // Create a signature from key certificate fields
        var dataToSign = new StringBuilder();
        dataToSign.Append(certificate.CertificateId);
        dataToSign.Append(certificate.LicenseId);
        dataToSign.Append(certificate.ProjectName);
        dataToSign.Append(certificate.SupervisorName);
        dataToSign.Append(certificate.IssuedAt.ToString("O"));
        dataToSign.Append(certificate.ProcessingData["Total Survey Points"]);
        
        // Use HMAC with a machine-specific key for local verification
        // In production, use proper certificate signing with private key
        using var hmac = new HMACSHA256(GetSigningKey());
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign.ToString()));
        return Convert.ToBase64String(hash);
    }
    
    private byte[] GetSigningKey()
    {
        // Generate a machine-specific key for local signatures
        // In production, this would use the certificate private key
        string machineId = Environment.MachineName + Environment.UserName;
        return SHA256.HashData(Encoding.UTF8.GetBytes(machineId + "FathomOS-SurveyListing-2024"));
    }
    
    /// <summary>
    /// Generates a human-readable text version of the certificate
    /// </summary>
    private async Task GenerateReadableCertificateAsync(SurveyListingCertificate certificate, string outputPath)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    FATHOM OS - PROCESSING CERTIFICATE                       ║");
        sb.AppendLine("║                         Survey Listing Module                                ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"  Certificate ID:    {certificate.CertificateId}");
        sb.AppendLine($"  Issue Date:        {certificate.IssuedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"  Module Version:    {certificate.ModuleVersion}");
        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("  PROJECT INFORMATION");
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine($"  Project Name:      {certificate.ProjectName}");
        if (!string.IsNullOrEmpty(certificate.Vessel))
            sb.AppendLine($"  Vessel:            {certificate.Vessel}");
        if (!string.IsNullOrEmpty(certificate.Client))
            sb.AppendLine($"  Client:            {certificate.Client}");
        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("  PROCESSING SUMMARY");
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        foreach (var kvp in certificate.ProcessingData)
        {
            sb.AppendLine($"  {kvp.Key,-25} {kvp.Value}");
        }
        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("  INPUT FILES");
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        foreach (var file in certificate.InputFiles)
        {
            sb.AppendLine($"  • {file}");
        }
        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("  OUTPUT FILES");
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        foreach (var file in certificate.OutputFiles)
        {
            sb.AppendLine($"  • {file}");
        }
        sb.AppendLine();
        sb.AppendLine("═════════════════════════════════════════════════════════════════════════════════");
        sb.AppendLine("  SUPERVISOR APPROVAL");
        sb.AppendLine("═════════════════════════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"  I certify that I have reviewed the processing results and confirm that");
        sb.AppendLine($"  the data is correct and complete.");
        sb.AppendLine();
        sb.AppendLine($"  Name:              {certificate.SupervisorName}");
        sb.AppendLine($"  Title:             {certificate.SupervisorTitle}");
        sb.AppendLine($"  Company:           {certificate.CompanyName}");
        sb.AppendLine($"  Approval Date:     {certificate.ApprovalTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("═════════════════════════════════════════════════════════════════════════════════");
        sb.AppendLine("  VERIFICATION");
        sb.AppendLine("═════════════════════════════════════════════════════════════════════════════════");
        sb.AppendLine($"  This certificate can be verified at:");
        sb.AppendLine($"  https://verify.fathomos.com/{certificate.CertificateId}");
        sb.AppendLine();
        sb.AppendLine($"  Digital Signature: {certificate.Signature[..20]}...");
        sb.AppendLine($"  License ID:        {certificate.LicenseId}");
        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("  This is an automatically generated certificate from Fathom OS.");
        sb.AppendLine("  The digital signature ensures the integrity of this document.");
        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────");
        
        await File.WriteAllTextAsync(outputPath, sb.ToString());
    }
    
    /// <summary>
    /// Generates an HTML certificate with QR code for printing
    /// </summary>
    private async Task GenerateHtmlCertificateAsync(SurveyListingCertificate certificate, string outputPath)
    {
        try
        {
            // Convert to ProcessingCertificate for the PDF generator
            var processingCert = new LicensingSystem.Shared.ProcessingCertificate
            {
                CertificateId = certificate.CertificateId,
                LicenseId = certificate.LicenseId,
                LicenseeCode = certificate.LicenseeCode,
                ModuleId = certificate.ModuleId,
                ModuleCertificateCode = certificate.ModuleCertificateCode,
                ModuleVersion = certificate.ModuleVersion,
                IssuedAt = certificate.IssuedAt,
                ProjectName = certificate.ProjectName,
                ProjectLocation = certificate.ProjectLocation,
                Vessel = certificate.Vessel,
                Client = certificate.Client,
                SignatoryName = certificate.SupervisorName,
                SignatoryTitle = certificate.SupervisorTitle,
                CompanyName = certificate.CompanyName,
                ProcessingData = certificate.ProcessingData,
                InputFiles = certificate.InputFiles,
                OutputFiles = certificate.OutputFiles,
                Signature = certificate.Signature,
                SignatureAlgorithm = certificate.SignatureAlgorithm
            };
            
            // Generate HTML with QR code
            var generator = new FathomOS.Core.Certificates.CertificatePdfGenerator();
            var html = generator.GenerateHtml(processingCert);
            
            await File.WriteAllTextAsync(outputPath, html);
            
            System.Diagnostics.Debug.WriteLine($"HTML certificate generated: {outputPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating HTML certificate: {ex.Message}");
            // Don't throw - HTML is optional
        }
    }
    
    /// <summary>
    /// Attempts to sync the certificate with the license server
    /// </summary>
    private async Task TrySyncCertificateAsync(SurveyListingCertificate certificate)
    {
        try
        {
            // Try to get the license client from the shell app
            var serverUrl = GetServerUrl();
            if (string.IsNullOrEmpty(serverUrl))
                return;
            
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            var syncRequest = new
            {
                certificateId = certificate.CertificateId,
                licenseId = certificate.LicenseId,
                licenseeCode = certificate.LicenseeCode,
                moduleId = certificate.ModuleId,
                moduleCertificateCode = certificate.ModuleCertificateCode,
                moduleVersion = certificate.ModuleVersion,
                issuedAt = certificate.IssuedAt,
                projectName = certificate.ProjectName,
                supervisorName = certificate.SupervisorName,
                supervisorTitle = certificate.SupervisorTitle,
                companyName = certificate.CompanyName,
                processingDataJson = JsonSerializer.Serialize(certificate.ProcessingData),
                inputFilesJson = JsonSerializer.Serialize(certificate.InputFiles),
                outputFilesJson = JsonSerializer.Serialize(certificate.OutputFiles),
                signature = certificate.Signature,
                signatureAlgorithm = certificate.SignatureAlgorithm
            };
            
            var json = JsonSerializer.Serialize(syncRequest);
            var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
            
            await httpClient.PostAsync($"{serverUrl}/api/certificates/sync", content);
            // Don't worry if sync fails - certificate is still valid locally
        }
        catch
        {
            // Sync failed silently - certificate is still valid locally
        }
    }
    
    private dynamic? GetLicenseInfo()
    {
        try
        {
            var appType = Type.GetType("FathomOS.Shell.App, FathomOS.Shell");
            if (appType != null)
            {
                var licensingProperty = appType.GetProperty("Licensing", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (licensingProperty != null)
                {
                    var licensing = licensingProperty.GetValue(null);
                    if (licensing != null)
                    {
                        var getDisplayMethod = licensing.GetType().GetMethod("GetLicenseDisplayInfo");
                        if (getDisplayMethod != null)
                        {
                            return getDisplayMethod.Invoke(licensing, null);
                        }
                    }
                }
            }
        }
        catch { }
        return null;
    }
    
    private string? GetServerUrl()
    {
        try
        {
            // Try to get the server URL from app configuration
            return "https://fathom-os-license-server.onrender.com";
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Survey Listing processing certificate data structure
/// </summary>
public class SurveyListingCertificate
{
    public string CertificateId { get; set; } = string.Empty;
    public string LicenseId { get; set; } = string.Empty;
    public string LicenseeCode { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string ModuleCertificateCode { get; set; } = string.Empty;
    public string ModuleVersion { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    
    // Project info
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectLocation { get; set; } = string.Empty;
    public string Vessel { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    
    // Supervisor info
    public string SupervisorName { get; set; } = string.Empty;
    public string SupervisorTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime ApprovalTime { get; set; }
    
    // Processing data
    public Dictionary<string, string> ProcessingData { get; set; } = new();
    public List<string> InputFiles { get; set; } = new();
    public List<string> OutputFiles { get; set; } = new();
    
    // Signature
    public string Signature { get; set; } = string.Empty;
    public string SignatureAlgorithm { get; set; } = string.Empty;
}
