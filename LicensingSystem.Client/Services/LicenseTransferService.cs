// LicensingSystem.Client/Services/LicenseTransferService.cs
// Handles license transfer between machines
// Supports generating, validating, and completing transfer tokens

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LicensingSystem.Client.Services;

/// <summary>
/// Interface for license transfer operations
/// </summary>
public interface ILicenseTransferService
{
    /// <summary>
    /// Generates a transfer token to move license to another machine
    /// </summary>
    Task<TransferToken> GenerateTransferTokenAsync();

    /// <summary>
    /// Validates a transfer token before completing transfer
    /// </summary>
    Task<bool> ValidateTransferTokenAsync(string token);

    /// <summary>
    /// Completes the license transfer using a token
    /// </summary>
    Task<TransferResult> CompleteLicenseTransferAsync(string token);

    /// <summary>
    /// Gets the transfer history for this license
    /// </summary>
    Task<List<TransferRecord>> GetTransferHistoryAsync();

    /// <summary>
    /// Gets the number of transfers remaining
    /// </summary>
    Task<int> GetRemainingTransfersAsync();

    /// <summary>
    /// Cancels a pending transfer token
    /// </summary>
    Task<bool> CancelTransferTokenAsync(string token);
}

/// <summary>
/// Service for handling license transfers between machines.
/// Generates, validates, and completes transfer tokens.
/// </summary>
public class LicenseTransferService : ILicenseTransferService, IDisposable
{
    private readonly string _licenseId;
    private readonly string _serverUrl;
    private readonly HttpClient _httpClient;
    private readonly Func<List<string>> _getHardwareFingerprints;
    private bool _disposed;

    /// <summary>
    /// Token validity period in hours (default: 24 hours)
    /// </summary>
    public int TokenValidityHours { get; set; } = 24;

    /// <summary>
    /// Event fired when transfer is initiated
    /// </summary>
    public event EventHandler<TransferInitiatedEventArgs>? TransferInitiated;

    /// <summary>
    /// Event fired when transfer is completed
    /// </summary>
    public event EventHandler<TransferCompletedEventArgs>? TransferCompleted;

    /// <summary>
    /// Creates a new LicenseTransferService instance
    /// </summary>
    /// <param name="licenseId">License ID to transfer</param>
    /// <param name="serverUrl">Server URL for transfer operations</param>
    /// <param name="getHardwareFingerprints">Function to get current hardware fingerprints</param>
    public LicenseTransferService(
        string licenseId,
        string serverUrl,
        Func<List<string>> getHardwareFingerprints)
    {
        _licenseId = licenseId;
        _serverUrl = serverUrl.TrimEnd('/');
        _getHardwareFingerprints = getHardwareFingerprints;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    /// <summary>
    /// Generates a transfer token to move license to another machine.
    /// The current machine will be deactivated once the token is used.
    /// </summary>
    public async Task<TransferToken> GenerateTransferTokenAsync()
    {
        try
        {
            var fingerprints = _getHardwareFingerprints();
            var request = new GenerateTransferRequest
            {
                LicenseId = _licenseId,
                SourceFingerprints = fingerprints,
                SourceMachineName = Environment.MachineName,
                ValidityHours = TokenValidityHours
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/transfer/generate",
                request);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<TransferErrorResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new TransferToken
                {
                    Success = false,
                    ErrorCode = errorResponse?.ErrorCode ?? "GENERATION_FAILED",
                    ErrorMessage = errorResponse?.Message ?? "Failed to generate transfer token."
                };
            }

            var result = JsonSerializer.Deserialize<GenerateTransferResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || string.IsNullOrEmpty(result.Token))
            {
                return new TransferToken
                {
                    Success = false,
                    ErrorCode = "INVALID_RESPONSE",
                    ErrorMessage = "Invalid server response."
                };
            }

            var token = new TransferToken
            {
                Success = true,
                Token = result.Token,
                ExpiresAt = result.ExpiresAt,
                TransferNumber = result.TransferNumber,
                RemainingTransfers = result.RemainingTransfers
            };

            TransferInitiated?.Invoke(this, new TransferInitiatedEventArgs
            {
                Token = token,
                SourceMachine = Environment.MachineName
            });

            return token;
        }
        catch (HttpRequestException ex)
        {
            return new TransferToken
            {
                Success = false,
                ErrorCode = "NETWORK_ERROR",
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new TransferToken
            {
                Success = false,
                ErrorCode = "UNEXPECTED_ERROR",
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates a transfer token before completing transfer.
    /// Use this to check if a token is valid before proceeding.
    /// </summary>
    public async Task<bool> ValidateTransferTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var request = new ValidateTransferRequest
            {
                Token = token
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/transfer/validate",
                request);

            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ValidateTransferResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.IsValid ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Completes the license transfer using a token.
    /// This will activate the license on the current machine and
    /// deactivate it on the source machine.
    /// </summary>
    public async Task<TransferResult> CompleteLicenseTransferAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new TransferResult
            {
                Success = false,
                ErrorCode = "INVALID_TOKEN",
                ErrorMessage = "Transfer token is required."
            };
        }

        try
        {
            var fingerprints = _getHardwareFingerprints();
            var request = new CompleteTransferRequest
            {
                Token = token,
                TargetFingerprints = fingerprints,
                TargetMachineName = Environment.MachineName,
                TargetUserName = Environment.UserName
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/transfer/complete",
                request);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<TransferErrorResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new TransferResult
                {
                    Success = false,
                    ErrorCode = errorResponse?.ErrorCode ?? "TRANSFER_FAILED",
                    ErrorMessage = errorResponse?.Message ?? GetTransferErrorMessage(errorResponse?.ErrorCode)
                };
            }

            var result = JsonSerializer.Deserialize<CompleteTransferResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
            {
                return new TransferResult
                {
                    Success = false,
                    ErrorCode = "INVALID_RESPONSE",
                    ErrorMessage = "Invalid server response."
                };
            }

            var transferResult = new TransferResult
            {
                Success = true,
                LicenseId = result.LicenseId,
                NewLicenseData = result.NewLicenseData,
                TransferredAt = result.TransferredAt,
                Message = "License transferred successfully."
            };

            TransferCompleted?.Invoke(this, new TransferCompletedEventArgs
            {
                Result = transferResult,
                TargetMachine = Environment.MachineName
            });

            return transferResult;
        }
        catch (HttpRequestException ex)
        {
            return new TransferResult
            {
                Success = false,
                ErrorCode = "NETWORK_ERROR",
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new TransferResult
            {
                Success = false,
                ErrorCode = "UNEXPECTED_ERROR",
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets the transfer history for this license
    /// </summary>
    public async Task<List<TransferRecord>> GetTransferHistoryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_serverUrl}/api/license/transfer/history?licenseId={_licenseId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TransferRecord>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TransferRecord>();
            }
        }
        catch { }

        return new List<TransferRecord>();
    }

    /// <summary>
    /// Gets the number of transfers remaining
    /// </summary>
    public async Task<int> GetRemainingTransfersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_serverUrl}/api/license/transfer/remaining?licenseId={_licenseId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RemainingTransfersResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.RemainingTransfers ?? -1;
            }
        }
        catch { }

        return -1; // Unknown
    }

    /// <summary>
    /// Cancels a pending transfer token
    /// </summary>
    public async Task<bool> CancelTransferTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var request = new CancelTransferRequest
            {
                Token = token,
                LicenseId = _licenseId
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/transfer/cancel",
                request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a user-friendly error message for transfer error codes
    /// </summary>
    private static string GetTransferErrorMessage(string? errorCode)
    {
        return errorCode switch
        {
            "TOKEN_EXPIRED" => "The transfer token has expired. Please generate a new token.",
            "TOKEN_USED" => "This transfer token has already been used.",
            "TOKEN_NOT_FOUND" => "Invalid transfer token.",
            "TRANSFER_LIMIT_REACHED" => "You have reached the maximum number of license transfers allowed.",
            "LICENSE_NOT_FOUND" => "License not found.",
            "LICENSE_REVOKED" => "This license has been revoked and cannot be transferred.",
            "LICENSE_EXPIRED" => "This license has expired and cannot be transferred.",
            "HARDWARE_MISMATCH" => "The license was transferred from a different machine than expected.",
            "SAME_MACHINE" => "Cannot transfer license to the same machine.",
            "TRANSFER_DISABLED" => "License transfer is not enabled for this license type.",
            _ => "Failed to complete license transfer. Please contact support."
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

#region Request/Response DTOs

internal class GenerateTransferRequest
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("sourceFingerprints")]
    public List<string> SourceFingerprints { get; set; } = new();

    [JsonPropertyName("sourceMachineName")]
    public string SourceMachineName { get; set; } = string.Empty;

    [JsonPropertyName("validityHours")]
    public int ValidityHours { get; set; }
}

internal class GenerateTransferResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("transferNumber")]
    public int TransferNumber { get; set; }

    [JsonPropertyName("remainingTransfers")]
    public int RemainingTransfers { get; set; }
}

internal class ValidateTransferRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

internal class ValidateTransferResponse
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("sourceMachine")]
    public string? SourceMachine { get; set; }
}

internal class CompleteTransferRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("targetFingerprints")]
    public List<string> TargetFingerprints { get; set; } = new();

    [JsonPropertyName("targetMachineName")]
    public string TargetMachineName { get; set; } = string.Empty;

    [JsonPropertyName("targetUserName")]
    public string? TargetUserName { get; set; }
}

internal class CompleteTransferResponse
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("newLicenseData")]
    public string? NewLicenseData { get; set; }

    [JsonPropertyName("transferredAt")]
    public DateTime TransferredAt { get; set; }
}

internal class TransferErrorResponse
{
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal class CancelTransferRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;
}

internal class RemainingTransfersResponse
{
    [JsonPropertyName("remainingTransfers")]
    public int RemainingTransfers { get; set; }

    [JsonPropertyName("totalTransfers")]
    public int TotalTransfers { get; set; }

    [JsonPropertyName("maxTransfers")]
    public int MaxTransfers { get; set; }
}

#endregion

#region Public DTOs

/// <summary>
/// Transfer token for moving license between machines
/// </summary>
public class TransferToken
{
    /// <summary>
    /// Whether token generation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The transfer token string
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// This transfer's sequence number
    /// </summary>
    public int TransferNumber { get; set; }

    /// <summary>
    /// Number of transfers remaining after this one
    /// </summary>
    public int RemainingTransfers { get; set; }

    /// <summary>
    /// Error code if failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time until token expires
    /// </summary>
    public TimeSpan TimeUntilExpiry => ExpiresAt > DateTime.UtcNow
        ? ExpiresAt - DateTime.UtcNow
        : TimeSpan.Zero;

    /// <summary>
    /// Whether the token is still valid (not expired)
    /// </summary>
    public bool IsValid => Success && DateTime.UtcNow < ExpiresAt;
}

/// <summary>
/// Result of a license transfer operation
/// </summary>
public class TransferResult
{
    /// <summary>
    /// Whether transfer succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// License ID that was transferred
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>
    /// New license data (JSON) for the target machine
    /// </summary>
    public string? NewLicenseData { get; set; }

    /// <summary>
    /// When the transfer was completed
    /// </summary>
    public DateTime TransferredAt { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code if failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Record of a past license transfer
/// </summary>
public class TransferRecord
{
    [JsonPropertyName("transferId")]
    public string TransferId { get; set; } = string.Empty;

    [JsonPropertyName("transferNumber")]
    public int TransferNumber { get; set; }

    [JsonPropertyName("sourceMachine")]
    public string SourceMachine { get; set; } = string.Empty;

    [JsonPropertyName("targetMachine")]
    public string TargetMachine { get; set; } = string.Empty;

    [JsonPropertyName("transferredAt")]
    public DateTime TransferredAt { get; set; }

    [JsonPropertyName("initiatedBy")]
    public string? InitiatedBy { get; set; }
}

/// <summary>
/// Event args when transfer is initiated
/// </summary>
public class TransferInitiatedEventArgs : EventArgs
{
    public TransferToken Token { get; set; } = new();
    public string SourceMachine { get; set; } = string.Empty;
}

/// <summary>
/// Event args when transfer is completed
/// </summary>
public class TransferCompletedEventArgs : EventArgs
{
    public TransferResult Result { get; set; } = new();
    public string TargetMachine { get; set; } = string.Empty;
}

#endregion
