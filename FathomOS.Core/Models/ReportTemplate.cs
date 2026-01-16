using System.Text.Json;
using System.Text.Json.Serialization;

namespace FathomOS.Core.Models;

/// <summary>
/// Report template configuration for PDF and Excel exports
/// </summary>
public class ReportTemplate
{
    /// <summary>
    /// Template version for compatibility
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Company information
    /// </summary>
    public CompanyInfo Company { get; set; } = new();
    
    /// <summary>
    /// Header configuration
    /// </summary>
    public HeaderConfig Header { get; set; } = new();
    
    /// <summary>
    /// Footer configuration
    /// </summary>
    public FooterConfig Footer { get; set; } = new();
    
    /// <summary>
    /// Color scheme
    /// </summary>
    public ColorScheme Colors { get; set; } = new();
    
    /// <summary>
    /// Load template from JSON file
    /// </summary>
    public static ReportTemplate Load(string filePath)
    {
        if (!File.Exists(filePath))
            return new ReportTemplate();
            
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ReportTemplate>(json) ?? new ReportTemplate();
    }
    
    /// <summary>
    /// Save template to JSON file
    /// </summary>
    public void Save(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// Get default template
    /// </summary>
    public static ReportTemplate GetDefault()
    {
        return new ReportTemplate
        {
            Company = new CompanyInfo
            {
                Name = "Fathom OS",
                LogoFileName = "company_logo.png",
                Address = "",
                Phone = "",
                Email = "",
                Website = ""
            },
            Header = new HeaderConfig
            {
                ShowLogo = true,
                LogoPosition = LogoPosition.Left,
                LogoWidth = 80,
                LogoHeight = 40,
                Title = "SURVEY LISTING REPORT",
                ShowCompanyName = true,
                ShowProjectInfo = true
            },
            Footer = new FooterConfig
            {
                LeftText = "{ProjectName}",
                CenterText = "Page {PageNumber} of {TotalPages}",
                RightText = "{GeneratedDate}",
                ShowBorder = true
            },
            Colors = new ColorScheme
            {
                PrimaryColor = "#1E3A5F",      // Dark blue
                SecondaryColor = "#4A90D9",    // Light blue
                AccentColor = "#FF6B35",       // Orange
                HeaderBackground = "#1E3A5F",
                HeaderText = "#FFFFFF",
                TableHeaderBackground = "#4A90D9",
                TableHeaderText = "#FFFFFF",
                TableAlternateRow = "#F0F7FF"
            }
        };
    }
    
    /// <summary>
    /// Replace placeholders in text with actual values
    /// </summary>
    public static string ReplacePlaceholders(string text, Project project, int pageNumber = 0, int totalPages = 0)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        return text
            .Replace("{ProjectName}", project.ProjectName)
            .Replace("{ClientName}", project.ClientName)
            .Replace("{VesselName}", project.VesselName)
            .Replace("{ProcessorName}", project.ProcessorName)
            .Replace("{ProductName}", project.ProductName ?? "")
            .Replace("{RovName}", project.RovName ?? "")
            .Replace("{SurveyDate}", project.SurveyDate?.ToString("yyyy-MM-dd") ?? "")
            .Replace("{SurveyType}", project.SurveyType.ToString())
            .Replace("{CoordinateSystem}", project.CoordinateSystem)
            .Replace("{GeneratedDate}", DateTime.Now.ToString("yyyy-MM-dd"))
            .Replace("{GeneratedTime}", DateTime.Now.ToString("HH:mm:ss"))
            .Replace("{GeneratedDateTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{PageNumber}", pageNumber.ToString())
            .Replace("{TotalPages}", totalPages.ToString())
            .Replace("{Year}", DateTime.Now.Year.ToString())
            .Replace("{CompanyName}", "Fathom OS");
    }
}

/// <summary>
/// Company information for branding
/// </summary>
public class CompanyInfo
{
    public string Name { get; set; } = "Fathom OS";
    public string LogoFileName { get; set; } = "company_logo.png";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Website { get; set; } = "";
}

/// <summary>
/// Header configuration
/// </summary>
public class HeaderConfig
{
    public bool ShowLogo { get; set; } = true;
    public LogoPosition LogoPosition { get; set; } = LogoPosition.Left;
    public int LogoWidth { get; set; } = 80;
    public int LogoHeight { get; set; } = 40;
    public string Title { get; set; } = "SURVEY LISTING REPORT";
    public bool ShowCompanyName { get; set; } = true;
    public bool ShowProjectInfo { get; set; } = true;
    public bool ShowBorder { get; set; } = true;
}

/// <summary>
/// Footer configuration
/// </summary>
public class FooterConfig
{
    public string LeftText { get; set; } = "{ProjectName}";
    public string CenterText { get; set; } = "Page {PageNumber} of {TotalPages}";
    public string RightText { get; set; } = "{GeneratedDate}";
    public bool ShowBorder { get; set; } = true;
}

/// <summary>
/// Color scheme for reports
/// </summary>
public class ColorScheme
{
    public string PrimaryColor { get; set; } = "#1E3A5F";
    public string SecondaryColor { get; set; } = "#4A90D9";
    public string AccentColor { get; set; } = "#FF6B35";
    public string HeaderBackground { get; set; } = "#1E3A5F";
    public string HeaderText { get; set; } = "#FFFFFF";
    public string TableHeaderBackground { get; set; } = "#4A90D9";
    public string TableHeaderText { get; set; } = "#FFFFFF";
    public string TableAlternateRow { get; set; } = "#F0F7FF";
}

/// <summary>
/// Logo position options
/// </summary>
public enum LogoPosition
{
    Left,
    Center,
    Right
}
