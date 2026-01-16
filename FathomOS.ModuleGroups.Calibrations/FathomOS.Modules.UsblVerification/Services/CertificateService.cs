using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for generating USBL verification certificates
/// </summary>
public class CertificateService
{
    private ReportTemplate _template;
    private static int _certificateSequence = 1;
    
    public CertificateService()
    {
        _template = new ReportTemplate();
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    public void SetTemplate(ReportTemplate template)
    {
        _template = template ?? new ReportTemplate();
    }
    
    /// <summary>
    /// Generate a verification certificate PDF
    /// </summary>
    public void GenerateCertificate(string filePath, UsblVerificationProject project, VerificationResults results)
    {
        if (!results.OverallPass)
        {
            throw new InvalidOperationException("Cannot generate certificate for failed verification");
        }
        
        var certNumber = GenerateCertificateNumber();
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));
                
                // Header
                page.Header().Element(c => ComposeHeader(c, project));
                
                // Content
                page.Content().Element(c => ComposeCertificateContent(c, project, results, certNumber));
                
                // Footer
                page.Footer().Element(c => ComposeFooter(c, project));
            });
        }).GeneratePdf(filePath);
    }
    
    private string GenerateCertificateNumber()
    {
        var template = _template.CertificateNumber;
        var number = template
            .Replace("{Year}", DateTime.Now.Year.ToString())
            .Replace("{Sequence}", (_certificateSequence++).ToString("D4"));
        return number;
    }
    
    private void ComposeHeader(IContainer container, UsblVerificationProject project)
    {
        container.Column(headerCol =>
        {
            headerCol.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    // Logo
                    if (_template.ShowLogo && _template.LogoData != null && _template.LogoData.Length > 0)
                    {
                        col.Item().Width((float)_template.LogoWidth).Image(_template.LogoData);
                    }
                    else if (_template.ShowCompanyName)
                    {
                        col.Item().Text(_template.CompanyName)
                            .FontSize(16).Bold().FontColor(HexToColor(_template.PrimaryColor));
                    }
                });
                
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text(_template.CertificateTitle)
                        .FontSize(20).Bold().FontColor(HexToColor(_template.PrimaryColor));
                    col.Item().Text("VERIFICATION PASSED")
                        .FontSize(14).Bold().FontColor(HexToColor(_template.PassColor));
                });
            });
            
            headerCol.Item().PaddingBottom(20);
        });
    }
    
    private void ComposeCertificateContent(IContainer container, UsblVerificationProject project, 
        VerificationResults results, string certNumber)
    {
        container.Column(col =>
        {
            // Certificate banner
            col.Item().Background(HexToColor(_template.PassColor)).Padding(15).Row(row =>
            {
                row.RelativeItem().AlignCenter().Text("✓ VERIFICATION SUCCESSFUL")
                    .FontSize(18).Bold().FontColor(Colors.White);
            });
            
            col.Item().PaddingVertical(20);
            
            // Certificate number and date
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"Certificate Number: {certNumber}").FontSize(12).Bold();
                row.RelativeItem().AlignRight().Text($"Date: {DateTime.Now:dd MMMM yyyy}").FontSize(12);
            });
            
            col.Item().PaddingVertical(15);
            
            // Main statement
            col.Item().Text(text =>
            {
                text.Span("This is to certify that the USBL positioning system aboard vessel ");
                text.Span(project.VesselName).Bold();
                text.Span(" has been successfully verified in accordance with industry standards.");
            });
            
            col.Item().PaddingVertical(15);
            
            // Project details box
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Column(details =>
            {
                details.Item().Text("VERIFICATION DETAILS").FontSize(12).Bold()
                    .FontColor(HexToColor(_template.PrimaryColor));
                details.Item().PaddingVertical(10);
                
                details.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Project: {project.ProjectName}");
                        c.Item().Text($"Client: {project.ClientName}");
                        c.Item().Text($"Vessel: {project.VesselName}");
                        c.Item().Text($"Survey Date: {project.SurveyDate:dd/MM/yyyy}");
                    });
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Transponder: {project.TransponderName}");
                        c.Item().Text($"Transponder ID: {project.TransponderId}");
                        c.Item().Text($"Nominal Depth: {project.NominalDepth:F1} m");
                        c.Item().Text($"Processed By: {project.ProcessorName}");
                    });
                });
            });
            
            col.Item().PaddingVertical(15);
            
            // Results summary
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Column(resultCol =>
            {
                resultCol.Item().Text("VERIFICATION RESULTS").FontSize(12).Bold()
                    .FontColor(HexToColor(_template.PrimaryColor));
                resultCol.Item().PaddingVertical(10);
                
                // Spin test
                resultCol.Item().Row(r =>
                {
                    r.RelativeItem(2).Text("Spin Test (Position Repeatability):");
                    r.RelativeItem().AlignRight().Text("PASS").Bold().FontColor(HexToColor(_template.PassColor));
                });
                resultCol.Item().Text($"   Maximum radial difference: {results.SpinMaxDiffRadial:F3} m (Tolerance: {results.SpinMaxAllowableDiff:F3} m)")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                resultCol.Item().Text($"   2DRMS: {results.Spin2DRMS:F3} m")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                
                resultCol.Item().PaddingVertical(8);
                
                // Transit test
                resultCol.Item().Row(r =>
                {
                    r.RelativeItem(2).Text("Transit Test (Dynamic Verification):");
                    r.RelativeItem().AlignRight().Text("PASS").Bold().FontColor(HexToColor(_template.PassColor));
                });
                resultCol.Item().Text($"   Maximum difference from spin: {results.TransitMaxDiffRadial:F3} m (Tolerance: {results.TransitMaxAllowableSpread:F3} m)")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                
                resultCol.Item().PaddingVertical(8);
                
                // Alignment
                resultCol.Item().Row(r =>
                {
                    r.RelativeItem(2).Text("Alignment Check:");
                    r.RelativeItem().AlignRight().Text("PASS").Bold().FontColor(HexToColor(_template.PassColor));
                });
                resultCol.Item().Text($"   Line 1: {results.Line1ResidualAlignment:F2}°, Line 2: {results.Line2ResidualAlignment:F2}° (Tolerance: ±{project.AlignmentTolerance:F2}°)")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
            });
            
            col.Item().PaddingVertical(15);
            
            // Verified position
            col.Item().Background(HexToColor(_template.PrimaryColor)).Padding(15).Column(posCol =>
            {
                posCol.Item().Text("VERIFIED TRANSPONDER POSITION").FontSize(12).Bold().FontColor(Colors.White);
                posCol.Item().PaddingVertical(8);
                posCol.Item().Row(r =>
                {
                    r.RelativeItem().Text($"Easting: {results.SpinOverallAverageEasting:F3} m").FontColor(Colors.White);
                    r.RelativeItem().Text($"Northing: {results.SpinOverallAverageNorthing:F3} m").FontColor(Colors.White);
                    r.RelativeItem().Text($"Depth: {results.SpinOverallAverageDepth:F2} m").FontColor(Colors.White);
                });
            });
            
            col.Item().PaddingVertical(30);
            
            // Signature section
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(sigCol =>
                {
                    if (_template.SignatureData != null && _template.SignatureData.Length > 0)
                    {
                        sigCol.Item().Width(150).Image(_template.SignatureData);
                    }
                    else
                    {
                        sigCol.Item().Height(40); // Space for signature
                    }
                    sigCol.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(5);
                    sigCol.Item().Text(_template.SignatoryName.Length > 0 ? _template.SignatoryName : project.ProcessorName).Bold();
                    sigCol.Item().Text(_template.SignatoryTitle).FontSize(10).FontColor(Colors.Grey.Darken1);
                });
                
                row.RelativeItem();
                
                row.RelativeItem().Column(dateCol =>
                {
                    dateCol.Item().Height(40);
                    dateCol.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(5);
                    dateCol.Item().Text("Date").Bold();
                    dateCol.Item().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(10);
                });
            });
            
            col.Item().PaddingVertical(20);
            
            // Disclaimer
            col.Item().Text("This certificate is valid only for the equipment and conditions specified above. " +
                "Re-verification is recommended after any major system changes or at regular intervals as per company policy.")
                .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
        });
    }
    
    private void ComposeFooter(IContainer container, UsblVerificationProject project)
    {
        var leftText = ReplacePlaceholders(_template.FooterLeftText, project);
        var rightText = ReplacePlaceholders(_template.FooterRightText, project);
        
        container.Row(row =>
        {
            row.RelativeItem().Text(leftText).FontSize(9).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().AlignRight().Text(rightText).FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }
    
    private string ReplacePlaceholders(string text, UsblVerificationProject project)
    {
        return text
            .Replace("{ProjectName}", project.ProjectName)
            .Replace("{ClientName}", project.ClientName)
            .Replace("{VesselName}", project.VesselName)
            .Replace("{SurveyDate}", project.SurveyDate?.ToString("dd/MM/yyyy") ?? "")
            .Replace("{GeneratedDate}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
            .Replace("{ProcessorName}", project.ProcessorName)
            .Replace("{Year}", DateTime.Now.Year.ToString());
    }
    
    private static QuestPDF.Infrastructure.Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Colors.Black;
        
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return QuestPDF.Infrastructure.Color.FromRGB(r, g, b);
        }
        return Colors.Black;
    }
    
    #region Fathom OS Certificate System Integration
    
    /// <summary>
    /// Generate the ProcessingData dictionary for the Fathom OS certificate system.
    /// This data will be included in the official Fathom OS verification certificate.
    /// </summary>
    public Dictionary<string, string> GetProcessingDataForCertificate(
        UsblVerificationProject project, 
        VerificationResults results)
    {
        var data = new Dictionary<string, string>
        {
            // Equipment Information
            ["USBL System"] = project.UsblModel ?? "Not Specified",
            ["Transponder"] = $"{project.TransponderName} ({project.TransponderId})",
            ["Transponder Model"] = project.TransponderModel ?? "Not Specified",
            ["Transponder Frequency"] = project.TransponderFrequency ?? "Not Specified",
            ["Nominal Depth"] = $"{project.NominalDepth:F1} m",
            
            // Spin Test Results
            ["Spin Test Points"] = results.SpinTotalPoints.ToString(),
            ["Spin Headings Tested"] = "0°, 90°, 180°, 270°",
            ["Spin Max Radial Diff"] = $"{results.SpinMaxDiffRadial:F3} m",
            ["Spin Tolerance"] = $"{results.SpinMaxAllowableDiff:F3} m",
            ["Spin 2DRMS"] = $"{results.Spin2DRMS:F3} m",
            ["Spin Test Result"] = results.SpinTestPassed ? "PASS" : "FAIL",
            
            // Transit Test Results
            ["Transit Test Points"] = results.TransitTotalPoints.ToString(),
            ["Transit Max Diff"] = $"{results.TransitMaxDiffRadial:F3} m",
            ["Transit Tolerance"] = $"{results.TransitMaxAllowableSpread:F3} m",
            ["Transit Test Result"] = results.TransitTestPassed ? "PASS" : "FAIL",
            
            // Overall Results
            ["Overall Result"] = results.OverallPass ? "PASS - Within Tolerance" : "FAIL - Exceeds Tolerance",
            ["Verification Date"] = project.SurveyDate?.ToString("dd MMM yyyy") ?? DateTime.Now.ToString("dd MMM yyyy"),
            ["Processed By"] = project.ProcessorName ?? "Not Specified"
        };
        
        // Add statistics if available
        if (results.SpinStatistics != null && results.SpinStatistics.Count > 0)
        {
            // Calculate overall statistics from per-heading stats
            var allStats = results.SpinStatistics.Values.ToList();
            var avgStdDevE = allStats.Average(s => s.StdDevEasting2Sigma);
            var avgStdDevN = allStats.Average(s => s.StdDevNorthing2Sigma);
            data["Avg Std Dev (E)"] = $"{avgStdDevE:F3} m";
            data["Avg Std Dev (N)"] = $"{avgStdDevN:F3} m";
            data["2DRMS"] = $"{results.Spin2DRMS:F3} m";
            data["Mean Position"] = $"E: {results.SpinOverallAverageEasting:F3}, N: {results.SpinOverallAverageNorthing:F3}";
        }
        else
        {
            // Fallback to basic stats
            data["2DRMS"] = $"{results.Spin2DRMS:F3} m";
            data["Mean Position"] = $"E: {results.SpinOverallAverageEasting:F3}, N: {results.SpinOverallAverageNorthing:F3}";
        }
        
        return data;
    }
    
    /// <summary>
    /// Get the list of input files for certificate audit trail
    /// </summary>
    public List<string> GetInputFilesForCertificate(UsblVerificationProject project)
    {
        var files = new List<string>();
        
        // Add spin data files
        if (!string.IsNullOrEmpty(project.Spin000?.FilePath))
            files.Add(Path.GetFileName(project.Spin000.FilePath));
        if (!string.IsNullOrEmpty(project.Spin090?.FilePath))
            files.Add(Path.GetFileName(project.Spin090.FilePath));
        if (!string.IsNullOrEmpty(project.Spin180?.FilePath))
            files.Add(Path.GetFileName(project.Spin180.FilePath));
        if (!string.IsNullOrEmpty(project.Spin270?.FilePath))
            files.Add(Path.GetFileName(project.Spin270.FilePath));
        
        // Add transit data files
        if (!string.IsNullOrEmpty(project.TransitLine1?.FilePath))
            files.Add(Path.GetFileName(project.TransitLine1.FilePath));
        if (!string.IsNullOrEmpty(project.TransitLine2?.FilePath))
            files.Add(Path.GetFileName(project.TransitLine2.FilePath));
        
        return files;
    }
    
    /// <summary>
    /// Get additional validity conditions specific to USBL verification
    /// </summary>
    public List<string> GetValidityConditions()
    {
        return new List<string>
        {
            "Re-verification recommended after 12 months or after any major system changes.",
            "Valid only for the USBL system and transponder specified above.",
            "Results apply to the water depth and environmental conditions at time of verification.",
            "Calibration offsets should be applied as determined during verification."
        };
    }
    
    /// <summary>
    /// Get available signatory titles for USBL verification certificates
    /// </summary>
    public static List<string> GetSignatoryTitles()
    {
        return new List<string>
        {
            "Survey Supervisor",
            "Senior Survey Engineer",
            "Survey Engineer",
            "Processing Engineer",
            "Quality Control Engineer",
            "Verification Engineer",
            "Calibration Engineer",
            "Operations Manager",
            "Project Manager",
            "Survey Manager",
            "Data Processing Specialist",
            "Technical Supervisor",
            "Positioning Engineer",
            "Navigation Engineer",
            "Hydrographic Surveyor"
        };
    }
    
    #endregion
}
