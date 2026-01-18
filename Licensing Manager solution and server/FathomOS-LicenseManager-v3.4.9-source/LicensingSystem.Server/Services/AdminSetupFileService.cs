// LicensingSystem.Server/Services/AdminSetupFileService.cs
// Service to handle file-based admin setup for offline deployments
// This allows administrators to pre-seed credentials via admin-credentials.json

using System.Text.Json;

namespace LicensingSystem.Server.Services;

public interface IAdminSetupFileService
{
    /// <summary>
    /// Check if admin-credentials.json exists and is valid
    /// </summary>
    Task<AdminCredentialsFile?> ReadSetupFileAsync();

    /// <summary>
    /// Securely delete the setup file after processing
    /// </summary>
    Task DeleteSetupFileAsync();

    /// <summary>
    /// Get the expected path for the setup file
    /// </summary>
    string GetSetupFilePath();
}

public class AdminSetupFileService : IAdminSetupFileService
{
    private readonly ILogger<AdminSetupFileService> _logger;
    private readonly string _dataPath;

    public AdminSetupFileService(ILogger<AdminSetupFileService> logger)
    {
        _logger = logger;

        // Determine data path based on environment
        _dataPath = Environment.GetEnvironmentVariable("DATA_PATH")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "data");
    }

    public string GetSetupFilePath()
    {
        return Path.Combine(_dataPath, "admin-credentials.json");
    }

    public async Task<AdminCredentialsFile?> ReadSetupFileAsync()
    {
        var filePath = GetSetupFilePath();

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("No admin-credentials.json found at {Path}", filePath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var credentials = JsonSerializer.Deserialize<AdminCredentialsFile>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (credentials == null)
            {
                _logger.LogWarning("admin-credentials.json exists but is empty or invalid");
                return null;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(credentials.Email) ||
                string.IsNullOrWhiteSpace(credentials.Username) ||
                string.IsNullOrWhiteSpace(credentials.Password))
            {
                _logger.LogWarning("admin-credentials.json missing required fields (email, username, or password)");
                return null;
            }

            _logger.LogInformation("Found valid admin-credentials.json file");
            return credentials;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse admin-credentials.json - invalid JSON format");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading admin-credentials.json");
            return null;
        }
    }

    public async Task DeleteSetupFileAsync()
    {
        var filePath = GetSetupFilePath();

        if (!File.Exists(filePath))
            return;

        try
        {
            // Overwrite with zeros before deleting (secure delete)
            var fileInfo = new FileInfo(filePath);
            var length = fileInfo.Length;

            await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                var zeros = new byte[Math.Min(length, 4096)];
                long written = 0;
                while (written < length)
                {
                    var toWrite = (int)Math.Min(zeros.Length, length - written);
                    await stream.WriteAsync(zeros, 0, toWrite);
                    written += toWrite;
                }
                await stream.FlushAsync();
            }

            File.Delete(filePath);
            _logger.LogInformation("Securely deleted admin-credentials.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error securely deleting admin-credentials.json");
            // Try simple delete as fallback
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted admin-credentials.json (simple delete fallback)");
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to delete admin-credentials.json even with fallback");
            }
        }
    }
}

/// <summary>
/// Model for admin-credentials.json file
/// </summary>
public class AdminCredentialsFile
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool ForcePasswordChange { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
}
