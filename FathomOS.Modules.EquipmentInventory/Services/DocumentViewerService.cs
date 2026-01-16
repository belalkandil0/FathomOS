using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for viewing documents (PDFs, images) inline or externally.
/// Supports preview generation and file management.
/// </summary>
public class DocumentViewerService
{
    private readonly string _cacheFolder;
    
    public DocumentViewerService()
    {
        _cacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FathomOS", "EquipmentInventory", "DocumentCache");
        Directory.CreateDirectory(_cacheFolder);
    }
    
    #region Document Info
    
    /// <summary>
    /// Get information about a document
    /// </summary>
    public DocumentInfo GetDocumentInfo(string filePath)
    {
        if (!File.Exists(filePath))
            return new DocumentInfo { Exists = false };
        
        var fileInfo = new FileInfo(filePath);
        var extension = fileInfo.Extension.ToLowerInvariant();
        
        return new DocumentInfo
        {
            Exists = true,
            FilePath = filePath,
            FileName = fileInfo.Name,
            Extension = extension,
            FileSize = fileInfo.Length,
            ModifiedDate = fileInfo.LastWriteTime,
            DocumentType = GetDocumentType(extension),
            CanPreview = CanPreviewDocument(extension),
            Icon = GetDocumentIcon(extension)
        };
    }
    
    /// <summary>
    /// Get document type from extension
    /// </summary>
    public DocumentType GetDocumentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => DocumentType.Pdf,
            ".jpg" or ".jpeg" => DocumentType.Image,
            ".png" => DocumentType.Image,
            ".gif" => DocumentType.Image,
            ".bmp" => DocumentType.Image,
            ".tiff" or ".tif" => DocumentType.Image,
            ".doc" or ".docx" => DocumentType.Word,
            ".xls" or ".xlsx" => DocumentType.Excel,
            ".ppt" or ".pptx" => DocumentType.PowerPoint,
            ".txt" => DocumentType.Text,
            ".rtf" => DocumentType.RichText,
            ".csv" => DocumentType.Csv,
            ".xml" => DocumentType.Xml,
            ".json" => DocumentType.Json,
            _ => DocumentType.Unknown
        };
    }
    
    /// <summary>
    /// Check if document can be previewed inline
    /// </summary>
    public bool CanPreviewDocument(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => true,
            ".txt" or ".csv" or ".xml" or ".json" => true,
            _ => false
        };
    }
    
    #endregion
    
    #region Image Preview
    
    /// <summary>
    /// Load image for preview
    /// </summary>
    public BitmapImage? LoadImagePreview(string filePath, int maxWidth = 800, int maxHeight = 600)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;
            
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            
            // Limit size for performance
            bitmap.DecodePixelWidth = maxWidth;
            
            bitmap.EndInit();
            bitmap.Freeze();
            
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Generate thumbnail for image
    /// </summary>
    public byte[]? GenerateThumbnail(string filePath, int width = 150, int height = 150)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = width;
            bitmap.DecodePixelHeight = height;
            bitmap.EndInit();
            bitmap.Freeze();
            
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Get cached thumbnail or generate new one
    /// </summary>
    public string? GetCachedThumbnail(string filePath, int size = 150)
    {
        var hash = GetFileHash(filePath);
        var cachePath = Path.Combine(_cacheFolder, $"{hash}_{size}.png");
        
        if (File.Exists(cachePath))
            return cachePath;
        
        var thumbnailBytes = GenerateThumbnail(filePath, size, size);
        if (thumbnailBytes == null)
            return null;
        
        File.WriteAllBytes(cachePath, thumbnailBytes);
        return cachePath;
    }
    
    #endregion
    
    #region Text Preview
    
    /// <summary>
    /// Load text file for preview
    /// </summary>
    public TextPreview? LoadTextPreview(string filePath, int maxLines = 100, int maxChars = 10000)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;
            
            var lines = new List<string>();
            int totalLines = 0;
            int totalChars = 0;
            
            using var reader = new StreamReader(filePath);
            string? line;
            while ((line = reader.ReadLine()) != null && lines.Count < maxLines && totalChars < maxChars)
            {
                lines.Add(line);
                totalChars += line.Length;
                totalLines++;
            }
            
            // Count remaining lines
            while (reader.ReadLine() != null)
                totalLines++;
            
            return new TextPreview
            {
                Lines = lines,
                TotalLines = totalLines,
                Truncated = totalLines > maxLines || totalChars >= maxChars
            };
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region Document Actions
    
    /// <summary>
    /// Open document with default application
    /// </summary>
    public bool OpenDocument(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;
            
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Open document location in explorer
    /// </summary>
    public bool ShowInExplorer(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;
            
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Copy document to new location
    /// </summary>
    public bool CopyDocument(string sourcePath, string destinationPath)
    {
        try
        {
            File.Copy(sourcePath, destinationPath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Delete document
    /// </summary>
    public bool DeleteDocument(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
    
    #region Cache Management
    
    /// <summary>
    /// Clear document cache
    /// </summary>
    public void ClearCache()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_cacheFolder))
            {
                File.Delete(file);
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Get cache size
    /// </summary>
    public long GetCacheSize()
    {
        try
        {
            return Directory.GetFiles(_cacheFolder)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }
    
    #endregion
    
    #region Helpers
    
    private static string GetFileHash(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var combined = $"{filePath}_{fileInfo.Length}_{fileInfo.LastWriteTimeUtc.Ticks}";
        return combined.GetHashCode().ToString("X8");
    }
    
    private static string GetDocumentIcon(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "FilePdfBox",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "FileImage",
            ".doc" or ".docx" => "FileWord",
            ".xls" or ".xlsx" => "FileExcel",
            ".ppt" or ".pptx" => "FilePowerpoint",
            ".txt" => "FileDocument",
            ".csv" => "FileDelimited",
            ".xml" or ".json" => "FileCode",
            ".zip" or ".rar" or ".7z" => "FolderZip",
            _ => "File"
        };
    }
    
    #endregion
}

#region Models

public enum DocumentType
{
    Unknown,
    Pdf,
    Image,
    Word,
    Excel,
    PowerPoint,
    Text,
    RichText,
    Csv,
    Xml,
    Json,
    Archive
}

public class DocumentInfo
{
    public bool Exists { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime ModifiedDate { get; set; }
    public DocumentType DocumentType { get; set; }
    public bool CanPreview { get; set; }
    public string Icon { get; set; } = "File";
    
    public string FileSizeDisplay => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{FileSize / (1024.0 * 1024):F1} MB",
        _ => $"{FileSize / (1024.0 * 1024 * 1024):F2} GB"
    };
}

public class TextPreview
{
    public List<string> Lines { get; set; } = new();
    public int TotalLines { get; set; }
    public bool Truncated { get; set; }
    
    public string Content => string.Join(Environment.NewLine, Lines);
}

#endregion
