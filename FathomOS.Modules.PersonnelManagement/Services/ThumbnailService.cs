using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Service for generating image thumbnails using System.Drawing
/// </summary>
public class ThumbnailService : IThumbnailService
{
    /// <inheritdoc />
    public byte[]? GenerateThumbnail(byte[] imageData, int maxWidth = 150, int maxHeight = 150)
    {
        if (imageData == null || imageData.Length == 0)
            return null;

        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var originalImage = Image.FromStream(inputStream);

            var (newWidth, newHeight) = CalculateThumbnailSize(
                originalImage.Width,
                originalImage.Height,
                maxWidth,
                maxHeight);

            // If the original is already smaller than the max, return the original
            if (originalImage.Width <= maxWidth && originalImage.Height <= maxHeight)
            {
                return imageData;
            }

            using var thumbnail = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(thumbnail);

            // Set high quality rendering options
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            // Draw the resized image
            graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);

            // Save to memory stream
            using var outputStream = new MemoryStream();

            // Get the appropriate encoder for the image format
            var format = GetImageFormat(originalImage);
            var encoderParams = GetEncoderParameters(format);
            var encoder = GetEncoder(format);

            if (encoder != null && encoderParams != null)
            {
                thumbnail.Save(outputStream, encoder, encoderParams);
            }
            else
            {
                thumbnail.Save(outputStream, format);
            }

            return outputStream.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public byte[]? GenerateThumbnailFromFile(string filePath, int maxWidth = 150, int maxHeight = 150)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        try
        {
            var imageData = File.ReadAllBytes(filePath);
            return GenerateThumbnail(imageData, maxWidth, maxHeight);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool GenerateThumbnailToFile(byte[] imageData, string outputPath, int maxWidth = 150, int maxHeight = 150)
    {
        if (string.IsNullOrEmpty(outputPath))
            return false;

        try
        {
            var thumbnail = GenerateThumbnail(imageData, maxWidth, maxHeight);
            if (thumbnail == null)
                return false;

            // Ensure the output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, thumbnail);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public (int Width, int Height) CalculateThumbnailSize(
        int originalWidth,
        int originalHeight,
        int maxWidth,
        int maxHeight)
    {
        if (originalWidth <= 0 || originalHeight <= 0)
            return (maxWidth, maxHeight);

        // Calculate aspect ratio
        var aspectRatio = (double)originalWidth / originalHeight;

        int newWidth, newHeight;

        // Determine dimensions based on which constraint is more limiting
        if (originalWidth > originalHeight)
        {
            // Landscape orientation - width is the limiting factor
            newWidth = Math.Min(originalWidth, maxWidth);
            newHeight = (int)(newWidth / aspectRatio);

            // If height still exceeds max, recalculate based on height
            if (newHeight > maxHeight)
            {
                newHeight = maxHeight;
                newWidth = (int)(newHeight * aspectRatio);
            }
        }
        else
        {
            // Portrait or square - height is the limiting factor
            newHeight = Math.Min(originalHeight, maxHeight);
            newWidth = (int)(newHeight * aspectRatio);

            // If width still exceeds max, recalculate based on width
            if (newWidth > maxWidth)
            {
                newWidth = maxWidth;
                newHeight = (int)(newWidth / aspectRatio);
            }
        }

        // Ensure minimum dimensions of 1 pixel
        newWidth = Math.Max(1, newWidth);
        newHeight = Math.Max(1, newHeight);

        return (newWidth, newHeight);
    }

    /// <inheritdoc />
    public bool IsAvailable()
    {
        try
        {
            // Try to create a small bitmap to verify System.Drawing is available
            using var testBitmap = new Bitmap(1, 1);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the image format from an image
    /// </summary>
    private static ImageFormat GetImageFormat(Image image)
    {
        if (ImageFormat.Jpeg.Equals(image.RawFormat))
            return ImageFormat.Jpeg;
        if (ImageFormat.Png.Equals(image.RawFormat))
            return ImageFormat.Png;
        if (ImageFormat.Gif.Equals(image.RawFormat))
            return ImageFormat.Gif;
        if (ImageFormat.Bmp.Equals(image.RawFormat))
            return ImageFormat.Bmp;

        // Default to JPEG for best compression
        return ImageFormat.Jpeg;
    }

    /// <summary>
    /// Gets the image encoder for a specific format
    /// </summary>
    private static ImageCodecInfo? GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();

        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets encoder parameters for optimal quality/size balance
    /// </summary>
    private static EncoderParameters? GetEncoderParameters(ImageFormat format)
    {
        if (format.Equals(ImageFormat.Jpeg))
        {
            var encoderParams = new EncoderParameters(1);
            // Quality level 85 provides good balance between file size and quality
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
            return encoderParams;
        }

        return null;
    }
}
