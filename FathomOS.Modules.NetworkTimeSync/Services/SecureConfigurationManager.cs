// SECURITY FIX (MISSING-005): Secure Configuration Storage using DPAPI
// This class provides secure storage for sensitive configuration data like the SharedSecret
// using Windows Data Protection API (DPAPI) with LocalMachine scope.

namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

/// <summary>
/// Provides secure storage for sensitive configuration data using Windows DPAPI.
/// SECURITY FIX (MISSING-005): Secrets are encrypted using DPAPI and stored in the registry
/// instead of plain text config files.
/// </summary>
public static class SecureConfigurationManager
{
    private const string RegistryPath = @"SOFTWARE\FathomOS\NetworkTimeSync";
    private const string SecretValueName = "ProtectedSecret";
    private const string SecretConfiguredValueName = "SecretConfigured";

    /// <summary>
    /// Minimum required length for a secure secret.
    /// </summary>
    public const int MinimumSecretLength = 16;

    /// <summary>
    /// Stores the shared secret securely using DPAPI encryption.
    /// The secret is encrypted with LocalMachine scope, meaning any process
    /// running on this machine can decrypt it (but not processes on other machines).
    /// </summary>
    /// <param name="secret">The secret to store securely.</param>
    /// <exception cref="ArgumentException">Thrown when the secret is null, empty, or too short.</exception>
    /// <exception cref="CryptographicException">Thrown when DPAPI encryption fails.</exception>
    public static void StoreSecret(string secret)
    {
        // SECURITY FIX: Validate secret meets minimum requirements
        if (string.IsNullOrEmpty(secret))
        {
            throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));
        }

        if (secret.Length < MinimumSecretLength)
        {
            throw new ArgumentException(
                $"Secret must be at least {MinimumSecretLength} characters long.",
                nameof(secret));
        }

        // SECURITY FIX: Reject known weak secrets
        if (secret == "FathomOSTimeSync2024")
        {
            throw new ArgumentException(
                "Cannot use known weak secret. Generate a unique secret using GenerateSecureSecret().",
                nameof(secret));
        }

        try
        {
            // SECURITY FIX: Encrypt using DPAPI with LocalMachine scope
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var protectedBytes = ProtectedData.Protect(
                secretBytes,
                null, // No additional entropy - DPAPI provides sufficient protection
                DataProtectionScope.LocalMachine);

            // Store in registry
            using var key = Registry.LocalMachine.CreateSubKey(RegistryPath);
            if (key == null)
            {
                throw new InvalidOperationException(
                    "Failed to create registry key. Ensure the application has administrative privileges.");
            }

            key.SetValue(SecretValueName, Convert.ToBase64String(protectedBytes));
            key.SetValue(SecretConfiguredValueName, true);

            // Clear sensitive data from memory
            Array.Clear(secretBytes, 0, secretBytes.Length);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(
                "Failed to encrypt secret using DPAPI. Ensure Windows Data Protection is available.",
                ex);
        }
    }

    /// <summary>
    /// Retrieves the shared secret from secure storage.
    /// </summary>
    /// <returns>The decrypted secret, or null if not configured.</returns>
    public static string? GetSecret()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            var protectedBase64 = key?.GetValue(SecretValueName) as string;

            if (string.IsNullOrEmpty(protectedBase64))
            {
                return null;
            }

            var protectedBytes = Convert.FromBase64String(protectedBase64);

            // SECURITY FIX: Decrypt using DPAPI
            var unprotectedBytes = ProtectedData.Unprotect(
                protectedBytes,
                null,
                DataProtectionScope.LocalMachine);

            var secret = Encoding.UTF8.GetString(unprotectedBytes);

            // Clear sensitive data from memory
            Array.Clear(unprotectedBytes, 0, unprotectedBytes.Length);

            return secret;
        }
        catch (CryptographicException)
        {
            // SECURITY FIX: Log but don't expose decryption failures
            System.Diagnostics.Debug.WriteLine(
                "[SecureConfig] Failed to decrypt secret - may have been created on a different machine.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SecureConfig] Error retrieving secret: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if a secret has been securely configured.
    /// </summary>
    /// <returns>True if a valid secret is stored, false otherwise.</returns>
    public static bool IsSecretConfigured()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            var configured = key?.GetValue(SecretConfiguredValueName);

            if (configured is bool isConfigured && isConfigured)
            {
                // Verify we can actually retrieve the secret
                var secret = GetSecret();
                return !string.IsNullOrEmpty(secret) && secret.Length >= MinimumSecretLength;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Removes the stored secret from secure storage.
    /// </summary>
    /// <returns>True if successfully removed, false otherwise.</returns>
    public static bool ClearSecret()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath, writable: true);
            if (key != null)
            {
                key.DeleteValue(SecretValueName, throwOnMissingValue: false);
                key.DeleteValue(SecretConfiguredValueName, throwOnMissingValue: false);
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SecureConfig] Error clearing secret: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random secret.
    /// </summary>
    /// <returns>A base64-encoded random secret string.</returns>
    public static string GenerateSecureSecret()
    {
        var randomBytes = new byte[32]; // 256 bits of entropy
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Performs first-time setup by generating and storing a new secure secret.
    /// </summary>
    /// <returns>The newly generated secret that must be shared with agents.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a secret is already configured.</exception>
    public static string PerformFirstTimeSetup()
    {
        if (IsSecretConfigured())
        {
            throw new InvalidOperationException(
                "A secret is already configured. Use ClearSecret() first if you want to regenerate.");
        }

        var newSecret = GenerateSecureSecret();
        StoreSecret(newSecret);
        return newSecret;
    }

    /// <summary>
    /// Gets setup instructions for first-time configuration.
    /// </summary>
    /// <returns>A string containing setup instructions.</returns>
    public static string GetSetupInstructions()
    {
        return @"
=== Fathom OS Time Sync - Secure Configuration Setup ===

A shared secret is required for secure communication between the
Fathom OS module and Time Sync Agents on your network.

SETUP STEPS:
1. Generate a new secret using 'GenerateSecureSecret()'
2. Store it securely using 'StoreSecret(secret)'
3. Copy the SAME secret to each agent's appsettings.json file

AGENT CONFIGURATION (appsettings.json):
{
  ""AgentSettings"": {
    ""Port"": 7700,
    ""SharedSecret"": ""<paste-your-secret-here>"",
    ""AllowTimeSet"": false,
    ""AllowNtpSync"": false
  }
}

SECURITY NOTES:
- The secret is stored encrypted using Windows DPAPI
- Each installation should use a UNIQUE secret
- Never share secrets over unencrypted channels
- Agents should be deployed with AllowTimeSet=false initially
- Only enable AllowTimeSet on agents that need time synchronization
";
    }
}
