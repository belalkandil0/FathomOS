// LicensingSystem.LicenseGeneratorUI/Models/Models.cs
// Shared model definitions for the License Manager UI

namespace LicenseGeneratorUI.Models;

/// <summary>
/// Module information from server
/// </summary>
public class ServerModuleInfo
{
    public int Id { get; set; }
    public string ModuleId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public string CertificateCode { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? Icon { get; set; }
}

/// <summary>
/// Module reference used in tier definitions
/// </summary>
public class TierModuleRef
{
    public string ModuleId { get; set; } = "";
}

/// <summary>
/// License tier information from server
/// </summary>
public class ServerTierInfo
{
    public int Id { get; set; }
    public string TierId { get; set; } = "";
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public List<string> IncludedModules { get; set; } = new();
    public List<TierModuleRef> Modules { get; set; } = new();
}

/// <summary>
/// License details for editing (used by EditLicenseWindow)
/// </summary>
public class LicenseEditDetailsDto
{
    public int Id { get; set; }
    public string LicenseId { get; set; } = "";
    public string LicenseKey { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Edition { get; set; } = "";
    public string SubscriptionType { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? HardwareId { get; set; }
    public List<string>? Features { get; set; }
    
    // White-Label Branding
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    public string? BrandLogo { get; set; }
    public string? BrandLogoUrl { get; set; }
}

// Note: LicenseFile and SignedLicense are defined in LicensingSystem.Shared
