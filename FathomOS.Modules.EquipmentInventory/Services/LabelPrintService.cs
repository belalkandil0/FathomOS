using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for printing equipment labels to various label printers.
/// Supports standard Windows printers, Zebra, DYMO, Brother, and thermal printers.
/// </summary>
public class LabelPrintService
{
    private readonly ModuleSettings _settings;
    private readonly QRCodeService _qrService;
    
    public LabelPrintService()
    {
        _settings = ModuleSettings.Load();
        _qrService = new QRCodeService(_settings);
    }
    
    public LabelPrintService(ModuleSettings settings)
    {
        _settings = settings;
        _qrService = new QRCodeService(settings);
    }
    
    #region Printer Discovery
    
    /// <summary>
    /// Get list of installed printers
    /// </summary>
    public List<PrinterInfo> GetAvailablePrinters()
    {
        var printers = new List<PrinterInfo>();
        
        try
        {
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                var info = new PrinterInfo
                {
                    Name = printerName,
                    IsDefault = printerName == new PrinterSettings().PrinterName,
                    Type = DetectPrinterType(printerName)
                };
                
                // Try to get printer status
                try
                {
                    var ps = new PrinterSettings { PrinterName = printerName };
                    info.IsOnline = ps.IsValid;
                }
                catch
                {
                    info.IsOnline = false;
                }
                
                printers.Add(info);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting printers: {ex.Message}");
        }
        
        return printers;
    }
    
    /// <summary>
    /// Detect printer type from name
    /// </summary>
    private LabelPrinterType DetectPrinterType(string printerName)
    {
        var name = printerName.ToLower();
        
        if (name.Contains("zebra") || name.Contains("zd4") || name.Contains("zt4") || name.Contains("zpl"))
            return LabelPrinterType.ZebraZPL;
        if (name.Contains("dymo") || name.Contains("labelwriter"))
            return LabelPrinterType.DYMO;
        if (name.Contains("brother") || name.Contains("p-touch") || name.Contains("ql-"))
            return LabelPrinterType.Brother;
        if (name.Contains("epson") && (name.Contains("label") || name.Contains("lw-")))
            return LabelPrinterType.Epson;
        if (name.Contains("tsc") || name.Contains("ttp-"))
            return LabelPrinterType.TSC;
        if (name.Contains("honeywell") || name.Contains("intermec"))
            return LabelPrinterType.Honeywell;
        if (name.Contains("thermal"))
            return LabelPrinterType.GenericThermal;
        
        return LabelPrinterType.Standard;
    }
    
    #endregion
    
    #region Label Size Helpers
    
    /// <summary>
    /// Get label dimensions in millimeters
    /// </summary>
    public (double WidthMm, double HeightMm) GetLabelDimensions(LabelSize size)
    {
        return size switch
        {
            LabelSize.Small25x25mm => (25, 25),
            LabelSize.Small38x25mm => (38, 25),
            LabelSize.Medium50x25mm => (50, 25),
            LabelSize.Medium50x50mm => (50, 50),
            LabelSize.Medium60x40mm => (60, 40),
            LabelSize.Large75x50mm => (75, 50),
            LabelSize.Large100x50mm => (100, 50),
            LabelSize.Large100x75mm => (100, 75),
            LabelSize.Custom => (_settings.PrinterSettings.CustomWidthMm, _settings.PrinterSettings.CustomHeightMm),
            _ => (50, 50)
        };
    }
    
    /// <summary>
    /// Convert mm to pixels at given DPI
    /// </summary>
    public int MmToPixels(double mm, int dpi = 300)
    {
        return (int)Math.Round(mm * dpi / 25.4);
    }
    
    /// <summary>
    /// Get available label sizes for display
    /// </summary>
    public static List<LabelSizeOption> GetLabelSizeOptions()
    {
        return new List<LabelSizeOption>
        {
            new(LabelSize.Small25x25mm, "Small (25x25mm / 1\"x1\")"),
            new(LabelSize.Small38x25mm, "Small Wide (38x25mm / 1.5\"x1\")"),
            new(LabelSize.Medium50x25mm, "Medium Narrow (50x25mm / 2\"x1\")"),
            new(LabelSize.Medium50x50mm, "Medium Square (50x50mm / 2\"x2\") - Recommended"),
            new(LabelSize.Medium60x40mm, "Medium (60x40mm / 2.4\"x1.6\")"),
            new(LabelSize.Large75x50mm, "Large (75x50mm / 3\"x2\")"),
            new(LabelSize.Large100x50mm, "Large Wide (100x50mm / 4\"x2\")"),
            new(LabelSize.Large100x75mm, "Extra Large (100x75mm / 4\"x3\")"),
            new(LabelSize.Custom, "Custom Size...")
        };
    }
    
    #endregion
    
    #region Label Generation
    
    /// <summary>
    /// Generate label image bytes for printing
    /// </summary>
    public byte[] GenerateLabelForPrinting(string uniqueId, string qrContent, string? equipmentName = null)
    {
        var (widthMm, heightMm) = GetLabelDimensions(_settings.PrinterSettings.LabelSize);
        var dpi = _settings.PrinterSettings.DPI;
        
        int widthPx = MmToPixels(widthMm, dpi);
        int heightPx = MmToPixels(heightMm, dpi);
        
        // Swap for landscape
        if (_settings.PrinterSettings.Orientation == LabelOrientation.Landscape)
            (widthPx, heightPx) = (heightPx, widthPx);
        
        return _qrService.GenerateLabelPng(uniqueId, qrContent, widthPx, heightPx);
    }
    
    #endregion
    
    #region Printing
    
    /// <summary>
    /// Print a label with current settings
    /// </summary>
    public bool PrintLabel(byte[] labelImageBytes, int copies = 0)
    {
        if (copies <= 0)
            copies = _settings.PrinterSettings.DefaultCopies;
        
        try
        {
            var printerName = _settings.PrinterSettings.PrinterName;
            if (string.IsNullOrEmpty(printerName))
            {
                // Use default printer
                printerName = new PrinterSettings().PrinterName;
            }
            
            using var ms = new MemoryStream(labelImageBytes);
            using var image = System.Drawing.Image.FromStream(ms);
            
            using var printDoc = new PrintDocument();
            printDoc.PrinterSettings.PrinterName = printerName;
            printDoc.PrinterSettings.Copies = (short)copies;
            
            printDoc.PrintPage += (sender, e) =>
            {
                if (e.Graphics == null) return;
                
                // Calculate scaling to fit label
                var (widthMm, heightMm) = GetLabelDimensions(_settings.PrinterSettings.LabelSize);
                
                // Convert to hundredths of an inch (PrintDocument units)
                float labelWidthInches = (float)(widthMm / 25.4);
                float labelHeightInches = (float)(heightMm / 25.4);
                
                float printWidth = labelWidthInches * 100;
                float printHeight = labelHeightInches * 100;
                
                // Scale image to fit
                float scale = Math.Min(printWidth / image.Width, printHeight / image.Height);
                float scaledWidth = image.Width * scale;
                float scaledHeight = image.Height * scale;
                
                // Center on label
                float x = (printWidth - scaledWidth) / 2;
                float y = (printHeight - scaledHeight) / 2;
                
                e.Graphics.DrawImage(image, x, y, scaledWidth, scaledHeight);
            };
            
            if (_settings.PrinterSettings.ShowPreview && !_settings.PrinterSettings.AutoPrint)
            {
                // Show WinForms print dialog
                var printDialog = new System.Windows.Forms.PrintDialog();
                printDialog.Document = printDoc;
                
                if (printDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    printDoc.Print();
                    return true;
                }
                return false;
            }
            else
            {
                // Direct print
                printDoc.Print();
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Print error: {ex.Message}");
            MessageBox.Show($"Failed to print label:\n\n{ex.Message}", "Print Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
    
    /// <summary>
    /// Print label with print dialog for settings
    /// </summary>
    public bool PrintLabelWithDialog(byte[] labelImageBytes)
    {
        try
        {
            // Save to temp file
            var tempPath = Path.Combine(Path.GetTempPath(), $"Label_{Guid.NewGuid():N}.png");
            File.WriteAllBytes(tempPath, labelImageBytes);
            
            // Create print dialog
            var printDialog = new System.Windows.Controls.PrintDialog();
            
            if (printDialog.ShowDialog() == true)
            {
                // Load image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(tempPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                
                // Create visual for printing
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var rect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                    context.DrawImage(bitmap, rect);
                }
                
                // Print
                printDialog.PrintVisual(visual, "Equipment Label");
                
                // Cleanup
                try { File.Delete(tempPath); } catch { }
                
                return true;
            }
            
            // Cleanup
            try { File.Delete(tempPath); } catch { }
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to print:\n\n{ex.Message}", "Print Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
    
    /// <summary>
    /// Print multiple labels (batch print)
    /// </summary>
    public int PrintBatch(List<(string UniqueId, string QrContent)> labels)
    {
        int printed = 0;
        
        foreach (var (uniqueId, qrContent) in labels)
        {
            var imageBytes = GenerateLabelForPrinting(uniqueId, qrContent);
            if (PrintLabel(imageBytes, 1))
                printed++;
        }
        
        return printed;
    }
    
    #endregion
    
    #region Zebra ZPL Support
    
    /// <summary>
    /// Generate ZPL code for Zebra printers
    /// </summary>
    public string GenerateZplLabel(string uniqueId, string qrContent)
    {
        var (widthMm, heightMm) = GetLabelDimensions(_settings.PrinterSettings.LabelSize);
        
        // Convert to dots (assuming 203 DPI for Zebra)
        int widthDots = (int)(widthMm * 8); // 203 DPI ≈ 8 dots/mm
        int heightDots = (int)(heightMm * 8);
        
        // ZPL template
        var zpl = $@"^XA
^PW{widthDots}
^LL{heightDots}
^FO{widthDots / 2 - 100},20^A0N,30,30^FD{_settings.OrganizationName}^FS
^FO{widthDots / 2 - 80},60^BQN,2,5^FDQA,{qrContent}^FS
^FO{widthDots / 2 - 60},{heightDots - 50}^A0N,25,25^FD{uniqueId}^FS
^XZ";
        
        return zpl;
    }
    
    /// <summary>
    /// Send ZPL directly to Zebra printer
    /// </summary>
    public bool PrintZplLabel(string uniqueId, string qrContent)
    {
        try
        {
            var zpl = GenerateZplLabel(uniqueId, qrContent);
            var printerName = _settings.PrinterSettings.PrinterName;
            
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("Please select a Zebra printer in Settings.", "Printer Not Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            // Send raw ZPL to printer
            return RawPrinterHelper.SendStringToPrinter(printerName, zpl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ZPL print error:\n\n{ex.Message}", "Print Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
    
    #endregion
}

/// <summary>
/// Printer information
/// </summary>
public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsOnline { get; set; }
    public LabelPrinterType Type { get; set; }
    
    public string DisplayName => IsDefault ? $"{Name} (Default)" : Name;
    public string TypeName => Type.ToString();
}

/// <summary>
/// Label size option for UI
/// </summary>
public record LabelSizeOption(LabelSize Size, string DisplayName);

/// <summary>
/// Helper for sending raw data to printer (for ZPL/EPL)
/// </summary>
public static class RawPrinterHelper
{
    [System.Runtime.InteropServices.DllImport("winspool.drv", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFOA pDocInfo);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct DOCINFOA
    {
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string pDocName;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string? pOutputFile;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string? pDataType;
    }
    
    public static bool SendStringToPrinter(string printerName, string data)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        return SendBytesToPrinter(printerName, bytes);
    }
    
    public static bool SendBytesToPrinter(string printerName, byte[] bytes)
    {
        IntPtr hPrinter = IntPtr.Zero;
        var di = new DOCINFOA { pDocName = "Label", pDataType = "RAW" };
        bool success = false;
        
        if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
        {
            if (StartDocPrinter(hPrinter, 1, ref di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    IntPtr pBytes = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(bytes.Length);
                    System.Runtime.InteropServices.Marshal.Copy(bytes, 0, pBytes, bytes.Length);
                    
                    success = WritePrinter(hPrinter, pBytes, bytes.Length, out _);
                    
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pBytes);
                    EndPagePrinter(hPrinter);
                }
                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
        }
        
        return success;
    }
}
