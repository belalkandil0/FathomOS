// LicensingSystem.Client/Services/GracePeriodManager.cs
// Manages grace period tracking and warnings for FathomOS licenses
// Provides proper warnings at 7, 3, 1 days remaining and handles expiration

using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace LicensingSystem.Client.Services;

/// <summary>
/// Manages grace period tracking, warnings, and expiration behavior.
/// Provides warnings at configurable intervals (default: 7, 3, 1 days).
/// </summary>
public class GracePeriodManager
{
    private readonly string _storagePath;
    private readonly string _registryPath;
    private GracePeriodState _state;

    // Default warning thresholds in days
    private readonly int[] _warningThresholds = { 7, 3, 1 };

    // Events
    public event EventHandler<GracePeriodWarningEventArgs>? WarningTriggered;
    public event EventHandler<GracePeriodExpiredEventArgs>? GracePeriodExpired;
    public event EventHandler<GracePeriodEnteredEventArgs>? GracePeriodEntered;

    /// <summary>
    /// Creates a new GracePeriodManager instance.
    /// </summary>
    /// <param name="productName">Product name for storage paths</param>
    public GracePeriodManager(string productName = "FathomOS")
    {
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            productName,
            "License");
        _registryPath = $@"SOFTWARE\{productName}\License";

        Directory.CreateDirectory(_storagePath);
        _state = LoadState();
    }

    /// <summary>
    /// Gets the current grace period state.
    /// </summary>
    public GracePeriodState CurrentState => _state;

    /// <summary>
    /// Gets the number of days remaining in the grace period.
    /// Returns 0 if not in grace period.
    /// </summary>
    public int DaysRemaining => _state.IsInGracePeriod ? _state.GraceDaysRemaining : 0;

    /// <summary>
    /// Gets whether the license is currently in grace period.
    /// </summary>
    public bool IsInGracePeriod => _state.IsInGracePeriod && _state.GraceDaysRemaining > 0;

    /// <summary>
    /// Gets whether the grace period has expired.
    /// </summary>
    public bool HasExpired => _state.IsInGracePeriod && _state.GraceDaysRemaining <= 0;

    /// <summary>
    /// Enters grace period mode. Called when license expires.
    /// </summary>
    /// <param name="licenseId">License ID that expired</param>
    /// <param name="expirationDate">Original expiration date</param>
    /// <param name="gracePeriodDays">Total grace period days allowed</param>
    public void EnterGracePeriod(string licenseId, DateTime expirationDate, int gracePeriodDays)
    {
        var now = DateTime.UtcNow;
        var graceEndDate = expirationDate.AddDays(gracePeriodDays);
        var daysRemaining = (int)(graceEndDate - now).TotalDays;

        _state = new GracePeriodState
        {
            LicenseId = licenseId,
            IsInGracePeriod = true,
            GracePeriodStartDate = expirationDate,
            GracePeriodEndDate = graceEndDate,
            GraceDaysRemaining = Math.Max(0, daysRemaining),
            TotalGracePeriodDays = gracePeriodDays,
            LastWarningShown = DateTime.MinValue,
            WarningsShown = new List<int>()
        };

        SaveState();

        GracePeriodEntered?.Invoke(this, new GracePeriodEnteredEventArgs
        {
            LicenseId = licenseId,
            DaysRemaining = _state.GraceDaysRemaining,
            GracePeriodEndDate = graceEndDate
        });

        // Check if we should show a warning immediately
        CheckAndTriggerWarnings();
    }

    /// <summary>
    /// Updates the grace period status. Call this periodically (e.g., on app start, hourly).
    /// </summary>
    /// <param name="licenseExpirationDate">Current license expiration date</param>
    /// <param name="gracePeriodDays">Total grace period days allowed</param>
    /// <returns>Updated grace period state</returns>
    public GracePeriodState UpdateGracePeriodStatus(DateTime licenseExpirationDate, int gracePeriodDays)
    {
        var now = DateTime.UtcNow;

        // Check if license has expired
        if (now > licenseExpirationDate)
        {
            var graceEndDate = licenseExpirationDate.AddDays(gracePeriodDays);
            var daysRemaining = (int)(graceEndDate - now).TotalDays;

            if (!_state.IsInGracePeriod)
            {
                // Just entered grace period
                EnterGracePeriod(_state.LicenseId, licenseExpirationDate, gracePeriodDays);
            }
            else
            {
                // Update days remaining
                _state.GraceDaysRemaining = Math.Max(0, daysRemaining);
                _state.GracePeriodEndDate = graceEndDate;
                SaveState();

                // Check for expiration
                if (_state.GraceDaysRemaining <= 0)
                {
                    HandleGracePeriodExpired();
                }
                else
                {
                    CheckAndTriggerWarnings();
                }
            }
        }
        else
        {
            // License is valid, not in grace period
            if (_state.IsInGracePeriod)
            {
                // License was renewed, exit grace period
                ExitGracePeriod();
            }
        }

        return _state;
    }

    /// <summary>
    /// Checks if warnings should be shown and triggers appropriate events.
    /// </summary>
    public void CheckAndTriggerWarnings()
    {
        if (!_state.IsInGracePeriod || _state.GraceDaysRemaining <= 0)
            return;

        var now = DateTime.UtcNow;

        // Don't show warnings more than once per day
        if (_state.LastWarningShown.Date == now.Date)
            return;

        // Check each threshold
        foreach (var threshold in _warningThresholds)
        {
            if (_state.GraceDaysRemaining <= threshold && !_state.WarningsShown.Contains(threshold))
            {
                var warning = CreateWarning(_state.GraceDaysRemaining);
                _state.WarningsShown.Add(threshold);
                _state.LastWarningShown = now;
                SaveState();

                WarningTriggered?.Invoke(this, new GracePeriodWarningEventArgs
                {
                    Warning = warning,
                    ThresholdDays = threshold
                });

                break; // Only show one warning at a time
            }
        }
    }

    /// <summary>
    /// Gets a warning object for the current grace period status.
    /// Returns null if not in grace period.
    /// </summary>
    public GracePeriodWarningInfo? GetCurrentWarning()
    {
        if (!_state.IsInGracePeriod)
            return null;

        return CreateWarning(_state.GraceDaysRemaining);
    }

    /// <summary>
    /// Exits grace period mode. Called when license is renewed.
    /// </summary>
    public void ExitGracePeriod()
    {
        _state = new GracePeriodState
        {
            LicenseId = _state.LicenseId,
            IsInGracePeriod = false,
            GraceDaysRemaining = 0
        };

        SaveState();
    }

    /// <summary>
    /// Resets the grace period state completely.
    /// </summary>
    public void Reset()
    {
        _state = new GracePeriodState();
        SaveState();
    }

    /// <summary>
    /// Configures custom warning thresholds.
    /// </summary>
    /// <param name="thresholds">Array of day thresholds (e.g., [7, 3, 1])</param>
    public void SetWarningThresholds(int[] thresholds)
    {
        Array.Sort(thresholds);
        Array.Reverse(thresholds); // Largest first
    }

    private void HandleGracePeriodExpired()
    {
        GracePeriodExpired?.Invoke(this, new GracePeriodExpiredEventArgs
        {
            LicenseId = _state.LicenseId,
            ExpiredAt = DateTime.UtcNow
        });
    }

    private GracePeriodWarningInfo CreateWarning(int daysRemaining)
    {
        return daysRemaining switch
        {
            <= 0 => new GracePeriodWarningInfo
            {
                DaysRemaining = 0,
                Severity = GracePeriodSeverity.Expired,
                Title = "License Grace Period Expired",
                Message = "Your license grace period has expired. FathomOS will now operate in limited mode.",
                RecommendedAction = "Please renew your license immediately to restore full functionality.",
                ShowAsModal = true,
                AllowDismiss = false
            },
            1 => new GracePeriodWarningInfo
            {
                DaysRemaining = 1,
                Severity = GracePeriodSeverity.Critical,
                Title = "License Expires Tomorrow!",
                Message = "URGENT: Your license grace period expires TOMORROW! This is your final warning.",
                RecommendedAction = "Renew your license now to avoid service interruption.",
                ShowAsModal = true,
                AllowDismiss = true
            },
            <= 3 => new GracePeriodWarningInfo
            {
                DaysRemaining = daysRemaining,
                Severity = GracePeriodSeverity.Critical,
                Title = "License Expiring Soon",
                Message = $"Your license grace period expires in {daysRemaining} days.",
                RecommendedAction = "Please renew your license as soon as possible.",
                ShowAsModal = true,
                AllowDismiss = true
            },
            <= 7 => new GracePeriodWarningInfo
            {
                DaysRemaining = daysRemaining,
                Severity = GracePeriodSeverity.Warning,
                Title = "License Expired - Grace Period Active",
                Message = $"Your license has expired. You have {daysRemaining} days remaining in the grace period.",
                RecommendedAction = "Please renew your license to continue using FathomOS.",
                ShowAsModal = true,
                AllowDismiss = true
            },
            _ => new GracePeriodWarningInfo
            {
                DaysRemaining = daysRemaining,
                Severity = GracePeriodSeverity.Info,
                Title = "License Expired",
                Message = $"Your license has expired. Grace period: {daysRemaining} days remaining.",
                RecommendedAction = "Consider renewing your license.",
                ShowAsModal = false,
                AllowDismiss = true
            }
        };
    }

    private GracePeriodState LoadState()
    {
        try
        {
            var filePath = Path.Combine(_storagePath, "grace.dat");
            if (File.Exists(filePath))
            {
                var encrypted = Convert.FromBase64String(File.ReadAllText(filePath));
                var json = Unprotect(encrypted);
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<GracePeriodState>(json) ?? new GracePeriodState();
                }
            }
        }
        catch
        {
            // Failed to load, return default
        }

        return new GracePeriodState();
    }

    private void SaveState()
    {
        try
        {
            var json = JsonSerializer.Serialize(_state);
            var encrypted = Protect(json);
            var filePath = Path.Combine(_storagePath, "grace.dat");
            File.WriteAllText(filePath, Convert.ToBase64String(encrypted));

            // Also save to registry as backup
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(_registryPath);
                key?.SetValue("GraceState", Convert.ToBase64String(encrypted));
            }
            catch { }
        }
        catch { }
    }

    private static readonly byte[] Entropy =
    {
        0x47, 0x52, 0x41, 0x43, 0x45, 0x50, 0x45, 0x52,
        0x49, 0x4F, 0x44, 0x4D, 0x47, 0x52, 0x21, 0x21
    };

    private byte[] Protect(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
    }

    private string Unprotect(byte[] encryptedData)
    {
        try
        {
            var bytes = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Persistent state for grace period tracking
/// </summary>
public class GracePeriodState
{
    public string LicenseId { get; set; } = string.Empty;
    public bool IsInGracePeriod { get; set; }
    public DateTime GracePeriodStartDate { get; set; }
    public DateTime GracePeriodEndDate { get; set; }
    public int GraceDaysRemaining { get; set; }
    public int TotalGracePeriodDays { get; set; }
    public DateTime LastWarningShown { get; set; }
    public List<int> WarningsShown { get; set; } = new();
}

/// <summary>
/// Detailed grace period warning information
/// </summary>
public class GracePeriodWarningInfo
{
    public int DaysRemaining { get; set; }
    public GracePeriodSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool ShowAsModal { get; set; }
    public bool AllowDismiss { get; set; } = true;
}

/// <summary>
/// Grace period severity levels
/// </summary>
public enum GracePeriodSeverity
{
    Info,
    Warning,
    Critical,
    Expired
}

/// <summary>
/// Event args for grace period warnings
/// </summary>
public class GracePeriodWarningEventArgs : EventArgs
{
    public GracePeriodWarningInfo Warning { get; set; } = new();
    public int ThresholdDays { get; set; }
}

/// <summary>
/// Event args for grace period expiration
/// </summary>
public class GracePeriodExpiredEventArgs : EventArgs
{
    public string LicenseId { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
}

/// <summary>
/// Event args for entering grace period
/// </summary>
public class GracePeriodEnteredEventArgs : EventArgs
{
    public string LicenseId { get; set; } = string.Empty;
    public int DaysRemaining { get; set; }
    public DateTime GracePeriodEndDate { get; set; }
}
