// FathomOS.Core/Certificates/CertificatePdfGenerator.cs
// Generates professional HTML certificates (can print to PDF from browser)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LicensingSystem.Shared;

namespace FathomOS.Core.Certificates;

/// <summary>
/// Generates professional HTML certificates
/// Print to PDF using browser or WebBrowser control
/// </summary>
public class CertificatePdfGenerator
{
    /// <summary>
    /// Generate HTML certificate
    /// </summary>
    public string GenerateHtml(ProcessingCertificate certificate, string? brandLogo = null, string? companyLogoUrl = null)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine($"    <title>Certificate {EscapeHtml(certificate.CertificateId)}</title>");
        html.AppendLine("    <style>");
        html.AppendLine(GetCssStyles());
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"certificate\">");
        
        // Header with logo and title
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <div class=\"logo-container\">");
        if (!string.IsNullOrEmpty(brandLogo))
        {
            html.AppendLine($"                <img src=\"{brandLogo}\" alt=\"Company Logo\" class=\"logo\">");
        }
        else
        {
            html.AppendLine($"                <div class=\"company-name-large\">{EscapeHtml(certificate.CompanyName)}</div>");
        }
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"title-container\">");
        html.AppendLine("                <h1 class=\"certificate-title\">CERTIFICATE OF PROCESSING</h1>");
        html.AppendLine("                <div class=\"title-underline\"></div>");
        html.AppendLine($"                <div class=\"certificate-id\">No: {EscapeHtml(certificate.CertificateId)}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        
        // Divider
        html.AppendLine("        <div class=\"divider\"></div>");
        
        // Certification statement
        html.AppendLine("        <div class=\"statement-section\">");
        html.AppendLine("            <p class=\"intro-text\">This is to certify that the data processing for:</p>");
        html.AppendLine("            <div class=\"project-box\">");
        html.AppendLine($"                <div class=\"project-name\">{EscapeHtml(certificate.ProjectName)}</div>");
        if (!string.IsNullOrEmpty(certificate.ProjectLocation))
        {
            html.AppendLine($"                <div class=\"project-location\">{EscapeHtml(certificate.ProjectLocation)}</div>");
        }
        html.AppendLine("            </div>");
        html.AppendLine("            <p class=\"statement-text\">has been performed using validated algorithms and quality-controlled procedures.</p>");
        html.AppendLine("        </div>");
        
        // Processing Details Card
        html.AppendLine("        <div class=\"card\">");
        html.AppendLine("            <div class=\"card-header\">PROCESSING DETAILS</div>");
        html.AppendLine("            <div class=\"card-content\">");
        html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">Module:</span><span class=\"value\">{EscapeHtml(certificate.ModuleId)} v{EscapeHtml(certificate.ModuleVersion)}</span></div>");
        html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">Processing Date:</span><span class=\"value\">{certificate.IssuedAt:dd MMMM yyyy, HH:mm} UTC</span></div>");
        
        if (!string.IsNullOrEmpty(certificate.Vessel))
            html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">Vessel:</span><span class=\"value\">{EscapeHtml(certificate.Vessel)}</span></div>");
        
        if (!string.IsNullOrEmpty(certificate.Client))
            html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">Client:</span><span class=\"value\">{EscapeHtml(certificate.Client)}</span></div>");
        
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        
        // Processing Data Card (module-specific data)
        if (certificate.ProcessingData.Count > 0)
        {
            html.AppendLine("        <div class=\"card\">");
            html.AppendLine("            <div class=\"card-header\">DATA SUMMARY</div>");
            html.AppendLine("            <div class=\"card-content\">");
            foreach (var kvp in certificate.ProcessingData)
            {
                html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">{EscapeHtml(kvp.Key)}:</span><span class=\"value\">{EscapeHtml(kvp.Value)}</span></div>");
            }
            html.AppendLine("            </div>");
            html.AppendLine("        </div>");
        }
        
        // Files section
        if (certificate.InputFiles.Count > 0 || certificate.OutputFiles.Count > 0)
        {
            html.AppendLine("        <div class=\"files-section\">");
            
            if (certificate.InputFiles.Count > 0)
            {
                html.AppendLine("            <div class=\"card files-card\">");
                html.AppendLine("                <div class=\"card-header\">INPUT FILES</div>");
                html.AppendLine("                <div class=\"card-content\">");
                html.AppendLine("                    <ul class=\"file-list\">");
                foreach (var file in certificate.InputFiles)
                {
                    html.AppendLine($"                        <li>{EscapeHtml(file)}</li>");
                }
                html.AppendLine("                    </ul>");
                html.AppendLine("                </div>");
                html.AppendLine("            </div>");
            }
            
            if (certificate.OutputFiles.Count > 0)
            {
                html.AppendLine("            <div class=\"card files-card\">");
                html.AppendLine("                <div class=\"card-header\">OUTPUT FILES</div>");
                html.AppendLine("                <div class=\"card-content\">");
                html.AppendLine("                    <ul class=\"file-list\">");
                foreach (var file in certificate.OutputFiles)
                {
                    html.AppendLine($"                        <li>{EscapeHtml(file)}</li>");
                }
                html.AppendLine("                    </ul>");
                html.AppendLine("                </div>");
                html.AppendLine("            </div>");
            }
            
            html.AppendLine("        </div>");
        }
        
        // Divider
        html.AppendLine("        <div class=\"divider\"></div>");
        
        // Signatory section
        html.AppendLine("        <div class=\"signatory-section\">");
        html.AppendLine("            <div class=\"signatory-info\">");
        html.AppendLine("                <h3 class=\"section-title\">AUTHORIZED BY</h3>");
        html.AppendLine("                <div class=\"signature-line\"></div>");
        html.AppendLine($"                <div class=\"signatory-name\">{EscapeHtml(certificate.SignatoryName)}</div>");
        if (!string.IsNullOrEmpty(certificate.SignatoryTitle))
            html.AppendLine($"                <div class=\"signatory-title\">{EscapeHtml(certificate.SignatoryTitle)}</div>");
        html.AppendLine($"                <div class=\"signatory-company\">{EscapeHtml(certificate.CompanyName)}</div>");
        html.AppendLine($"                <div class=\"signatory-date\">Date: {certificate.IssuedAt:dd MMMM yyyy}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"qr-container\">");
        
        // Generate QR code URL for verification
        var verificationUrl = $"https://fathom-os-license-server.onrender.com/verify.html?id={Uri.EscapeDataString(certificate.CertificateId)}";
        var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=120x120&data={Uri.EscapeDataString(verificationUrl)}";
        
        html.AppendLine($"                <img src=\"{qrCodeUrl}\" alt=\"QR Code\" class=\"qr-code\" width=\"120\" height=\"120\">");
        html.AppendLine("                <div class=\"qr-hint\">Scan to verify</div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        
        // Divider
        html.AppendLine("        <div class=\"divider\"></div>");
        
        // Verification section
        html.AppendLine("        <div class=\"verification-card\">");
        html.AppendLine("            <div class=\"verification-header\">");
        html.AppendLine("                <span>DIGITAL VERIFICATION</span>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"verification-content\">");
        html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">Certificate ID:</span><span class=\"value mono\">{EscapeHtml(certificate.CertificateId)}</span></div>");
        html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">Issued:</span><span class=\"value\">{certificate.IssuedAt:dd MMM yyyy HH:mm:ss} UTC</span></div>");
        var verifyLink = $"https://fathom-os-license-server.onrender.com/verify.html?id={Uri.EscapeDataString(certificate.CertificateId)}";
        html.AppendLine($"                <div class=\"detail-row\"><span class=\"label\">Verify online:</span><span class=\"value\"><a href=\"{verifyLink}\">{verifyLink}</a></span></div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        
        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine($"            <div class=\"footer-text\">Licensed to: {EscapeHtml(certificate.CompanyName)}</div>");
        html.AppendLine("            <div class=\"footer-copyright\">© 2025 Fathom OS</div>");
        html.AppendLine("        </div>");
        
        // Corner accent
        html.AppendLine("        <div class=\"corner-accent\">");
        html.AppendLine("            <span class=\"accent-text\">F</span>");
        html.AppendLine("            <span class=\"accent-text\">O</span>");
        html.AppendLine("            <span class=\"accent-text\">S</span>");
        html.AppendLine("        </div>");
        
        // Watermark
        html.AppendLine("        <div class=\"watermark\">FATHOM OS</div>");
        
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    /// <summary>
    /// Save HTML certificate to file
    /// </summary>
    public async Task SaveToFileAsync(ProcessingCertificate certificate, string outputPath, string? brandLogo = null)
    {
        var html = GenerateHtml(certificate, brandLogo);
        await File.WriteAllTextAsync(outputPath, html);
    }

    private string GetCssStyles()
    {
        return @"
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Arial, sans-serif; background: #f0f0f0; padding: 20px; }
        .certificate { width: 210mm; min-height: 297mm; margin: 0 auto; background: white; padding: 40px; position: relative; box-shadow: 0 0 20px rgba(0,0,0,0.1); overflow: hidden; }
        .header { display: flex; align-items: flex-start; margin-bottom: 20px; }
        .logo-container { width: 120px; margin-right: 30px; }
        .logo { max-width: 100px; max-height: 80px; }
        .company-name-large { font-size: 14px; font-weight: bold; color: #333; }
        .title-container { flex: 1; }
        .certificate-title { font-size: 28px; font-weight: 600; color: #0066CC; letter-spacing: 1px; }
        .title-underline { width: 200px; height: 3px; background: linear-gradient(90deg, #0066CC, #004499); margin: 8px 0; }
        .certificate-id { font-size: 12px; color: #666; font-family: 'Consolas', monospace; }
        .divider { height: 1px; background: linear-gradient(90deg, transparent, #ddd, transparent); margin: 20px 0; }
        .statement-section { text-align: center; margin: 30px 0; }
        .intro-text { font-size: 14px; color: #666; margin-bottom: 15px; }
        .project-box { border: 2px solid #0066CC; padding: 15px 30px; display: inline-block; margin: 10px 0; }
        .project-name { font-size: 20px; font-weight: 600; color: #333; }
        .project-location { font-size: 14px; color: #666; margin-top: 5px; }
        .statement-text { font-size: 13px; color: #444; max-width: 600px; margin: 15px auto; line-height: 1.6; }
        .card { border: 1px solid #ddd; border-radius: 4px; margin: 15px 0; overflow: hidden; }
        .card-header { background: #f8f8f8; padding: 10px 15px; font-size: 11px; font-weight: 600; color: #666; letter-spacing: 1px; border-bottom: 1px solid #ddd; }
        .card-content { padding: 15px; }
        .detail-row { display: flex; margin-bottom: 8px; font-size: 12px; }
        .detail-row .label { width: 180px; color: #666; flex-shrink: 0; }
        .detail-row .value { color: #333; flex: 1; }
        .detail-row .value.mono { font-family: 'Consolas', monospace; font-size: 11px; }
        .files-section { display: flex; gap: 15px; }
        .files-card { flex: 1; }
        .file-list { list-style: none; padding: 0; }
        .file-list li { font-size: 11px; color: #333; padding: 3px 0; }
        .file-list li:before { content: '• '; color: #0066CC; }
        .section-title { font-size: 11px; font-weight: 600; color: #666; letter-spacing: 1px; margin-bottom: 10px; }
        .signatory-section { display: flex; justify-content: space-between; align-items: flex-start; margin: 20px 0; }
        .signatory-info { flex: 1; }
        .signature-line { width: 250px; height: 1px; background: #333; margin: 30px 0 10px; }
        .signatory-name { font-size: 14px; font-weight: 600; color: #333; }
        .signatory-title { font-size: 12px; color: #666; }
        .signatory-company { font-size: 12px; color: #666; margin-top: 5px; }
        .signatory-date { font-size: 11px; color: #888; margin-top: 10px; }
        .qr-container { width: 130px; text-align: center; }
        .qr-code { width: 120px; height: 120px; border: 1px solid #ddd; padding: 5px; background: white; }
        .qr-hint { font-size: 9px; color: #999; margin-top: 5px; }
        .verification-card { border: 1px solid #ddd; border-radius: 4px; margin: 15px 0; overflow: hidden; }
        .verification-header { background: #f8f8f8; padding: 10px 15px; font-size: 11px; font-weight: 600; color: #666; letter-spacing: 1px; border-bottom: 1px solid #ddd; }
        .verification-content { padding: 15px; }
        .footer { margin-top: 30px; padding-top: 15px; border-top: 1px solid #eee; }
        .footer-text { font-size: 10px; color: #888; }
        .footer-copyright { font-size: 9px; color: #aaa; margin-top: 5px; }
        .corner-accent { position: absolute; bottom: 0; right: 0; width: 30px; background: linear-gradient(180deg, #0066CC, #004499); display: flex; flex-direction: column; align-items: center; padding: 15px 5px; }
        .accent-text { color: white; font-size: 14px; font-weight: bold; line-height: 1.5; }
        .watermark { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%) rotate(-45deg); font-size: 100px; color: rgba(0, 102, 204, 0.03); font-weight: bold; letter-spacing: 20px; white-space: nowrap; pointer-events: none; z-index: 0; }
        @media print { body { background: white; padding: 0; } .certificate { box-shadow: none; width: 100%; height: 100%; } }
        ";
    }

    private static string EscapeHtml(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return System.Net.WebUtility.HtmlEncode(text);
    }
}
