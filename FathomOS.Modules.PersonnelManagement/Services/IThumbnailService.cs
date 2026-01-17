namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Interface for image thumbnail generation
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Generates a thumbnail from image data while maintaining aspect ratio
    /// </summary>
    /// <param name="imageData">The original image data as a byte array</param>
    /// <param name="maxWidth">Maximum width of the thumbnail in pixels (default: 150)</param>
    /// <param name="maxHeight">Maximum height of the thumbnail in pixels (default: 150)</param>
    /// <returns>The thumbnail image data as a byte array, or null if generation fails</returns>
    byte[]? GenerateThumbnail(byte[] imageData, int maxWidth = 150, int maxHeight = 150);

    /// <summary>
    /// Generates a thumbnail from a file path while maintaining aspect ratio
    /// </summary>
    /// <param name="filePath">Path to the original image file</param>
    /// <param name="maxWidth">Maximum width of the thumbnail in pixels (default: 150)</param>
    /// <param name="maxHeight">Maximum height of the thumbnail in pixels (default: 150)</param>
    /// <returns>The thumbnail image data as a byte array, or null if generation fails</returns>
    byte[]? GenerateThumbnailFromFile(string filePath, int maxWidth = 150, int maxHeight = 150);

    /// <summary>
    /// Generates a thumbnail and saves it to a file
    /// </summary>
    /// <param name="imageData">The original image data as a byte array</param>
    /// <param name="outputPath">Path where the thumbnail will be saved</param>
    /// <param name="maxWidth">Maximum width of the thumbnail in pixels (default: 150)</param>
    /// <param name="maxHeight">Maximum height of the thumbnail in pixels (default: 150)</param>
    /// <returns>True if the thumbnail was generated and saved successfully</returns>
    bool GenerateThumbnailToFile(byte[] imageData, string outputPath, int maxWidth = 150, int maxHeight = 150);

    /// <summary>
    /// Gets the dimensions that would be used for a thumbnail while maintaining aspect ratio
    /// </summary>
    /// <param name="originalWidth">Original image width</param>
    /// <param name="originalHeight">Original image height</param>
    /// <param name="maxWidth">Maximum width constraint</param>
    /// <param name="maxHeight">Maximum height constraint</param>
    /// <returns>Tuple of (width, height) for the thumbnail</returns>
    (int Width, int Height) CalculateThumbnailSize(int originalWidth, int originalHeight, int maxWidth, int maxHeight);

    /// <summary>
    /// Checks if the service is available (required libraries are present)
    /// </summary>
    /// <returns>True if thumbnail generation is available</returns>
    bool IsAvailable();
}
