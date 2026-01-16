using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for managing report templates
/// </summary>
public class ReportTemplateService
{
    private static readonly string TemplatesFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "UsblVerification", "Templates");
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public ReportTemplateService()
    {
        EnsureTemplatesFolderExists();
    }
    
    /// <summary>
    /// Get all available templates
    /// </summary>
    public List<ReportTemplate> GetAllTemplates()
    {
        var templates = new List<ReportTemplate>();
        
        // Add default template
        templates.Add(CreateDefaultTemplate());
        
        // Load custom templates
        if (Directory.Exists(TemplatesFolder))
        {
            foreach (var file in Directory.GetFiles(TemplatesFolder, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var template = JsonSerializer.Deserialize<ReportTemplate>(json, JsonOptions);
                    if (template != null)
                    {
                        template.IsDefault = false;
                        templates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading template {file}: {ex.Message}");
                }
            }
        }
        
        return templates;
    }
    
    /// <summary>
    /// Save a custom template
    /// </summary>
    public void SaveTemplate(ReportTemplate template)
    {
        if (template.IsDefault)
        {
            throw new InvalidOperationException("Cannot modify the default template");
        }
        
        EnsureTemplatesFolderExists();
        
        var fileName = SanitizeFileName(template.TemplateName) + ".json";
        var filePath = Path.Combine(TemplatesFolder, fileName);
        
        var json = JsonSerializer.Serialize(template, JsonOptions);
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// Delete a custom template
    /// </summary>
    public void DeleteTemplate(string templateName)
    {
        var fileName = SanitizeFileName(templateName) + ".json";
        var filePath = Path.Combine(TemplatesFolder, fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
    
    /// <summary>
    /// Load a template by name
    /// </summary>
    public ReportTemplate? LoadTemplate(string templateName)
    {
        if (templateName == "Default")
        {
            return CreateDefaultTemplate();
        }
        
        var fileName = SanitizeFileName(templateName) + ".json";
        var filePath = Path.Combine(TemplatesFolder, fileName);
        
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ReportTemplate>(json, JsonOptions);
        }
        
        return null;
    }
    
    /// <summary>
    /// Import template from a file
    /// </summary>
    public ReportTemplate? ImportTemplate(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ReportTemplate>(json, JsonOptions);
    }
    
    /// <summary>
    /// Export template to a file
    /// </summary>
    public void ExportTemplate(ReportTemplate template, string filePath)
    {
        var json = JsonSerializer.Serialize(template, JsonOptions);
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// Load logo image from file
    /// </summary>
    public byte[]? LoadLogoFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        
        return File.ReadAllBytes(filePath);
    }
    
    /// <summary>
    /// Load signature image from file
    /// </summary>
    public byte[]? LoadSignatureFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        
        return File.ReadAllBytes(filePath);
    }
    
    /// <summary>
    /// Create a copy of a template with a new name
    /// </summary>
    public ReportTemplate DuplicateTemplate(ReportTemplate source, string newName)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        var copy = JsonSerializer.Deserialize<ReportTemplate>(json, JsonOptions)!;
        copy.TemplateName = newName;
        copy.IsDefault = false;
        return copy;
    }
    
    private static ReportTemplate CreateDefaultTemplate()
    {
        return new ReportTemplate
        {
            TemplateName = "Default",
            IsDefault = true,
            CompanyName = "S7 Survey Solutions",
            ReportTitle = "USBL VERIFICATION REPORT",
            CertificateTitle = "USBL VERIFICATION CERTIFICATE",
            ShowLogo = true,
            ShowCompanyName = true,
            FooterLeftText = "{ProjectName} - {VesselName}",
            FooterRightText = "Generated: {GeneratedDate}",
            ShowPageNumbers = true,
            PrimaryColor = "#1E3A5F",
            SecondaryColor = "#4A90D9",
            AccentColor = "#2ECC71",
            HeaderBackground = "#1E3A5F",
            HeaderTextColor = "#FFFFFF",
            PassColor = "#2ECC71",
            FailColor = "#E74C3C",
            CertificateNumber = "USBL-{Year}-{Sequence}",
            SignatoryTitle = "Survey Manager"
        };
    }
    
    private void EnsureTemplatesFolderExists()
    {
        if (!Directory.Exists(TemplatesFolder))
        {
            Directory.CreateDirectory(TemplatesFolder);
        }
    }
    
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid)
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
