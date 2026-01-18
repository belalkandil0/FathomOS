// LicensingSystem.Server/Services/SetupService.cs
// DEPRECATED - Setup is no longer required.
// The server now uses API key authentication via ApiKeyService.
// This file is kept for backward compatibility but methods return defaults.

namespace LicensingSystem.Server.Services;

/// <summary>
/// DEPRECATED: The server no longer requires setup.
/// Use API key authentication instead - see ApiKeyService.
/// </summary>
public interface ISetupService
{
    Task<bool> IsSetupRequiredAsync();
    Task<string> GenerateSetupTokenAsync();
    Task<SetupTokenValidationResult> ValidateSetupTokenAsync(string token, string ipAddress);
    Task<SetupCompletionResult> CompleteSetupAsync(SetupCompletionRequest request, string ipAddress);
    Task<bool> TryAutoSetupFromEnvironmentAsync();
    Task<SetupStatusResponse> GetSetupStatusAsync();
}

/// <summary>
/// DEPRECATED: Minimal stub implementation for backward compatibility.
/// </summary>
public class SetupService : ISetupService
{
    private readonly ILogger<SetupService> _logger;

    public SetupService(ILogger<SetupService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsSetupRequiredAsync()
    {
        // Setup is never required anymore
        return Task.FromResult(false);
    }

    public Task<string> GenerateSetupTokenAsync()
    {
        _logger.LogWarning("GenerateSetupTokenAsync called but setup is deprecated. Use API keys instead.");
        return Task.FromResult("DEPRECATED-USE-API-KEYS");
    }

    public Task<SetupTokenValidationResult> ValidateSetupTokenAsync(string token, string ipAddress)
    {
        return Task.FromResult(new SetupTokenValidationResult
        {
            IsValid = false,
            ErrorMessage = "Setup tokens are deprecated. Use API key authentication instead."
        });
    }

    public Task<SetupCompletionResult> CompleteSetupAsync(SetupCompletionRequest request, string ipAddress)
    {
        return Task.FromResult(new SetupCompletionResult
        {
            Success = false,
            ErrorMessage = "Setup is deprecated. Use API key authentication instead."
        });
    }

    public Task<bool> TryAutoSetupFromEnvironmentAsync()
    {
        // No auto-setup needed
        return Task.FromResult(true);
    }

    public Task<SetupStatusResponse> GetSetupStatusAsync()
    {
        return Task.FromResult(new SetupStatusResponse
        {
            SetupRequired = false,
            SetupCompleted = true,
            SetupMethod = "ApiKey"
        });
    }
}

// ==================== Request/Response Models ====================

public class SetupTokenValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SetupCompletionRequest
{
    public string? SetupToken { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool EnableTwoFactor { get; set; }
}

public class SetupCompletionResult
{
    public bool Success { get; set; }
    public int? AdminUserId { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SetupStatusResponse
{
    public bool SetupRequired { get; set; }
    public bool SetupCompleted { get; set; }
    public DateTime? SetupCompletedAt { get; set; }
    public string? SetupMethod { get; set; }
    public bool TokenRequired { get; set; }
    public bool TokenExpired { get; set; }
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
