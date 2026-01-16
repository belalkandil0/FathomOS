using QRCoder;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for generating QR codes and equipment labels.
/// Creates labels with organization name, QR code, and unique searchable ID.
/// </summary>
public class QRCodeService
{
    private readonly QRCodeGenerator _generator;
    private readonly ModuleSettings _settings;
    
    public QRCodeService()
    {
        _generator = new QRCodeGenerator();
        _settings = ModuleSettings.Load();
    }
    
    public QRCodeService(ModuleSettings settings)
    {
        _generator = new QRCodeGenerator();
        _settings = settings;
    }
    
    #region QR Code Generation (Raw)
    
    /// <summary>
    /// Generate a raw QR code PNG bytes
    /// </summary>
    public byte[] GenerateQrCodePng(string content, int pixelsPerModule = 10)
    {
        var qrCodeData = _generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }
    
    /// <summary>
    /// Generate a raw QR code as Base64 string
    /// </summary>
    public string GenerateQrCodeBase64(string content, int pixelsPerModule = 10)
    {
        var pngBytes = GenerateQrCodePng(content, pixelsPerModule);
        return Convert.ToBase64String(pngBytes);
    }
    
    /// <summary>
    /// Save a raw QR code to file
    /// </summary>
    public void SaveQrCodeToFile(string content, string filePath, int pixelsPerModule = 10)
    {
        var pngBytes = GenerateQrCodePng(content, pixelsPerModule);
        File.WriteAllBytes(filePath, pngBytes);
    }
    
    #endregion
    
    #region Label Generation (with Organization Name and Unique ID)
    
    /// <summary>
    /// Generate a complete label image with organization name, QR code, and unique ID.
    /// Similar to Subsea 7 style labels.
    /// </summary>
    /// <param name="uniqueId">The unique ID to display (e.g., S7WSS04068)</param>
    /// <param name="qrContent">The content encoded in the QR code</param>
    /// <param name="labelWidth">Label width in pixels (default 400)</param>
    /// <param name="labelHeight">Label height in pixels (default 450)</param>
    /// <returns>PNG bytes of the complete label</returns>
    public byte[] GenerateLabelPng(string uniqueId, string qrContent, int labelWidth = 400, int labelHeight = 450)
    {
        return GenerateLabelPng(uniqueId, qrContent, _settings.OrganizationName, labelWidth, labelHeight);
    }
    
    /// <summary>
    /// Generate a complete label image with custom organization name.
    /// </summary>
    public byte[] GenerateLabelPng(string uniqueId, string qrContent, string organizationName, 
        int labelWidth = 400, int labelHeight = 450)
    {
        // Generate QR code data
        var qrCodeData = _generator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.M);
        
        using var bitmap = new Bitmap(labelWidth, labelHeight);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Configure high quality rendering
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        
        // Fill white background
        graphics.Clear(Color.White);
        
        // Define layout
        int padding = 20;
        int orgNameHeight = 50;
        int qrSize = Math.Min(labelWidth - (padding * 2), labelHeight - orgNameHeight - 60 - (padding * 2));
        int uniqueIdHeight = 40;
        
        // Calculate positions
        int qrX = (labelWidth - qrSize) / 2;
        int qrY = orgNameHeight + padding;
        int uniqueIdY = qrY + qrSize + 10;
        
        // Draw organization name at top
        if (_settings.ShowOrganizationOnLabel && !string.IsNullOrEmpty(organizationName))
        {
            using var orgFont = new Font("Arial", 24, FontStyle.Bold);
            using var orgBrush = new SolidBrush(Color.Black);
            var orgSize = graphics.MeasureString(organizationName, orgFont);
            float orgX = (labelWidth - orgSize.Width) / 2;
            graphics.DrawString(organizationName, orgFont, orgBrush, orgX, padding);
        }
        
        // Draw QR code
        using var qrCode = new QRCode(qrCodeData);
        using var qrBitmap = qrCode.GetGraphic(20, Color.Black, Color.White, true);
        graphics.DrawImage(qrBitmap, qrX, qrY, qrSize, qrSize);
        
        // Draw unique ID at bottom
        if (_settings.ShowUniqueIdOnLabel && !string.IsNullOrEmpty(uniqueId))
        {
            using var idFont = new Font("Arial", 20, FontStyle.Regular);
            using var idBrush = new SolidBrush(Color.Black);
            var idSize = graphics.MeasureString(uniqueId, idFont);
            float idX = (labelWidth - idSize.Width) / 2;
            graphics.DrawString(uniqueId, idFont, idBrush, idX, uniqueIdY);
        }
        
        // Draw border
        using var borderPen = new Pen(Color.LightGray, 1);
        graphics.DrawRectangle(borderPen, 0, 0, labelWidth - 1, labelHeight - 1);
        
        // Convert to PNG bytes
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
    
    /// <summary>
    /// Generate a label as Base64 string
    /// </summary>
    public string GenerateLabelBase64(string uniqueId, string qrContent, int labelWidth = 400, int labelHeight = 450)
    {
        var pngBytes = GenerateLabelPng(uniqueId, qrContent, labelWidth, labelHeight);
        return Convert.ToBase64String(pngBytes);
    }
    
    /// <summary>
    /// Save a label to file
    /// </summary>
    public void SaveLabelToFile(string uniqueId, string qrContent, string filePath, 
        int labelWidth = 400, int labelHeight = 450)
    {
        var pngBytes = GenerateLabelPng(uniqueId, qrContent, labelWidth, labelHeight);
        File.WriteAllBytes(filePath, pngBytes);
    }
    
    #endregion
    
    #region Unique ID Generation
    
    /// <summary>
    /// Generate a unique ID for equipment (e.g., S7WSS04068)
    /// Format: {OrgCode}{CategoryCode}{SequenceNumber}
    /// </summary>
    /// <param name="categoryCode">Category code (e.g., "WSS" for Workshop Supplies)</param>
    /// <param name="sequenceNumber">Sequential number</param>
    /// <returns>Unique ID like "S7WSS04068"</returns>
    public string GenerateUniqueId(string categoryCode, int sequenceNumber)
    {
        var orgCode = _settings.OrganizationCode;
        return $"{orgCode}{categoryCode}{sequenceNumber:D5}";
    }
    
    /// <summary>
    /// Generate a unique ID using organization code from settings
    /// </summary>
    public string GenerateUniqueId(string categoryCode, int sequenceNumber, string organizationCode)
    {
        return $"{organizationCode}{categoryCode}{sequenceNumber:D5}";
    }
    
    /// <summary>
    /// Parse a unique ID to extract components
    /// </summary>
    /// <param name="uniqueId">The unique ID (e.g., S7WSS04068)</param>
    /// <returns>Tuple of (OrgCode, CategoryCode, SequenceNumber) or null if invalid</returns>
    public static (string OrgCode, string CategoryCode, int SequenceNumber)? ParseUniqueId(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId) || uniqueId.Length < 8)
            return null;
        
        // Try to extract: First 2-3 chars = org code, next 2-4 chars = category, rest = number
        // Example: S7WSS04068 -> S7 + WSS + 04068
        
        try
        {
            // Find where the number starts (from the end)
            int numStart = uniqueId.Length - 1;
            while (numStart > 0 && char.IsDigit(uniqueId[numStart - 1]))
                numStart--;
            
            if (numStart < 4) return null; // Need at least org + category codes
            
            var numberPart = uniqueId[numStart..];
            var codePart = uniqueId[..numStart];
            
            // Org code is typically 2-3 characters
            var orgCode = codePart.Length >= 5 ? codePart[..2] : codePart[..Math.Min(2, codePart.Length)];
            var categoryCode = codePart[orgCode.Length..];
            
            if (int.TryParse(numberPart, out int seqNum))
                return (orgCode, categoryCode, seqNum);
        }
        catch { }
        
        return null;
    }
    
    #endregion
    
    #region QR Code Content Formatters
    
    /// <summary>
    /// Generate QR code content for equipment
    /// </summary>
    public static string GenerateEquipmentQrCode(string assetNumber) => $"foseq:{assetNumber}";
    
    /// <summary>
    /// Generate QR code content for manifest
    /// </summary>
    public static string GenerateManifestQrCode(string manifestNumber) => $"s7mn:{manifestNumber}";
    
    /// <summary>
    /// Instance method wrapper for manifest QR content generation
    /// </summary>
    public string GenerateManifestQrContent(string manifestNumber) => GenerateManifestQrCode(manifestNumber);
    
    /// <summary>
    /// Generate QR code content for location
    /// </summary>
    public static string GenerateLocationQrCode(string locationCode) => $"s7loc:{locationCode}";
    
    /// <summary>
    /// Generate QR code content including unique ID for equipment
    /// This allows scanning to find equipment by unique ID
    /// </summary>
    public static string GenerateEquipmentQrCodeWithUniqueId(string assetNumber, string uniqueId) 
        => $"foseq:{assetNumber}|{uniqueId}";
    
    /// <summary>
    /// Parse a QR code to determine its type and extract values
    /// </summary>
    /// <param name="qrCode">The scanned QR code content</param>
    /// <returns>Tuple of (Type, Value, UniqueId) or null if invalid</returns>
    public static (string Type, string Value, string? UniqueId)? ParseQrCode(string qrCode)
    {
        if (string.IsNullOrWhiteSpace(qrCode)) return null;
        
        if (qrCode.StartsWith("foseq:"))
        {
            var content = qrCode[5..];
            if (content.Contains('|'))
            {
                var parts = content.Split('|', 2);
                return ("Equipment", parts[0], parts[1]);
            }
            return ("Equipment", content, null);
        }
        
        if (qrCode.StartsWith("s7mn:"))
            return ("Manifest", qrCode[5..], null);
        
        if (qrCode.StartsWith("s7loc:"))
            return ("Location", qrCode[6..], null);
        
        // Check if it's a plain unique ID (like S7WSS04068)
        var parsed = ParseUniqueId(qrCode);
        if (parsed != null)
            return ("UniqueId", qrCode, qrCode);
        
        return null;
    }
    
    #endregion
    
    #region Equipment Label Generation (Convenience Methods)
    
    /// <summary>
    /// Generate a complete equipment label with auto-generated unique ID
    /// </summary>
    /// <param name="assetNumber">The equipment asset number (e.g., EQ-2024-00001)</param>
    /// <param name="categoryCode">Category code for unique ID (e.g., "WSS")</param>
    /// <param name="sequenceNumber">Sequence number for unique ID</param>
    /// <returns>Tuple of (LabelPngBytes, UniqueId, QrContent)</returns>
    public (byte[] LabelPng, string UniqueId, string QrContent) GenerateEquipmentLabel(
        string assetNumber, string categoryCode, int sequenceNumber)
    {
        var uniqueId = GenerateUniqueId(categoryCode, sequenceNumber);
        var qrContent = GenerateEquipmentQrCodeWithUniqueId(assetNumber, uniqueId);
        var labelPng = GenerateLabelPng(uniqueId, qrContent);
        
        return (labelPng, uniqueId, qrContent);
    }
    
    /// <summary>
    /// Generate a complete equipment label with provided unique ID
    /// </summary>
    public byte[] GenerateEquipmentLabel(string assetNumber, string uniqueId)
    {
        var qrContent = GenerateEquipmentQrCodeWithUniqueId(assetNumber, uniqueId);
        return GenerateLabelPng(uniqueId, qrContent);
    }
    
    /// <summary>
    /// Generate a manifest label
    /// </summary>
    public byte[] GenerateManifestLabel(string manifestNumber)
    {
        var uniqueId = manifestNumber;
        var qrContent = GenerateManifestQrCode(manifestNumber);
        return GenerateLabelPng(uniqueId, qrContent);
    }
    
    /// <summary>
    /// Generate a location label
    /// </summary>
    public byte[] GenerateLocationLabel(string locationCode, string locationName)
    {
        var qrContent = GenerateLocationQrCode(locationCode);
        return GenerateLabelPng(locationCode, qrContent);
    }
    
    #endregion
}

/// <summary>
/// Label size presets for printing
/// </summary>
public static class LabelPresets
{
    public static readonly (int Width, int Height) Small = (300, 350);
    public static readonly (int Width, int Height) Standard = (400, 450);
    public static readonly (int Width, int Height) Large = (500, 550);
    public static readonly (int Width, int Height) ExtraLarge = (600, 700);
    
    /// <summary>
    /// Get label size by preset name
    /// </summary>
    public static (int Width, int Height) GetPreset(string presetName)
    {
        return presetName?.ToLower() switch
        {
            "small" => Small,
            "standard" => Standard,
            "large" => Large,
            "extralarge" or "extra large" or "xl" => ExtraLarge,
            _ => Standard
        };
    }
}
