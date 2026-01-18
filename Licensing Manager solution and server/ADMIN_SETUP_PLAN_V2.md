# FathomOS License Manager - First-Time Admin Setup Plan V2

**Document Version:** 2.0
**Date:** January 17, 2026
**Prepared by:** R&D Planning Agent
**Status:** Ready for Review

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current State Analysis](#2-current-state-analysis)
3. [Problem Statement](#3-problem-statement)
4. [Plan Options](#4-plan-options)
5. [Comparison Matrix](#5-comparison-matrix)
6. [Recommended Solution](#6-recommended-solution)
7. [Detailed Implementation Plan](#7-detailed-implementation-plan)
8. [Bug Analysis](#8-bug-analysis)
9. [Testing Checklist](#9-testing-checklist)
10. [Rollback Plan](#10-rollback-plan)
11. [Appendices](#11-appendices)

---

## 1. Executive Summary

### Background

The FathomOS License Manager requires a first-time administrator setup experience that works seamlessly in both online and offline deployment scenarios. The current implementation relies on a token-based setup flow that requires viewing the server console, which creates usability challenges for:

- Users deploying in air-gapped environments
- Non-technical users who may not have console access
- Enterprise deployments where server console access is restricted

### Current Issue

The previous implementation caused the Desktop UI Manager to fail connecting to the server. This document provides:

1. **Root cause analysis** of the connection failure
2. **Three alternative plan options** for offline-capable admin setup
3. **Detailed implementation plan** for the recommended solution
4. **Rollback procedures** to revert changes if needed

### Goal

Create a professional, enterprise-grade admin setup experience that:
- Works without internet connectivity at installation time
- Maintains high security standards
- Provides excellent UX for non-technical users
- Does not break existing Desktop UI functionality

---

## 2. Current State Analysis

### Server Architecture (v3.4.9)

The licensing server is built on ASP.NET Core 8.0 with the following relevant components:

```
LicensingSystem.Server/
|-- Controllers/
|   |-- AdminAuthController.cs    # Admin authentication with 2FA
|   |-- SetupController.cs        # First-time setup API endpoints
|-- Services/
|   |-- SetupService.cs           # Setup token generation and validation
|-- Middleware/
|   |-- SetupMiddleware.cs        # Blocks non-setup routes when setup required
|-- Data/
|   |-- LicenseDbContext.cs       # Contains SetupConfigRecord entity
|-- Program.cs                    # Server initialization with setup check
|-- wwwroot/
|   |-- setup.html                # Web-based setup wizard
```

### Current Setup Flow

1. Server starts and checks if any `AdminUsers` exist
2. If no admins exist, `SetupService.GenerateSetupTokenAsync()` creates a 32-byte random token
3. Token is displayed in console output (valid for 24 hours)
4. User navigates to `/setup` and enters the token
5. User creates admin account with strong password
6. Setup is marked complete, token is invalidated

### Desktop UI Architecture

The Desktop UI (`LicensingSystem.LicenseGeneratorUI`) is a WPF application that:

```csharp
// MainWindow.xaml.cs
private async Task ConnectToServerAsync()
{
    // Tests connection via GET /api/license/time
    var response = await _httpClient.GetAsync($"{_serverUrl}/api/license/time");
    _isConnected = response.IsSuccessStatusCode;
}
```

### Setup Middleware Behavior (Critical)

```csharp
// SetupMiddleware.cs - Lines 53-88
if (setupRequired)
{
    var isApiRequest = context.Request.Path.StartsWithSegments("/api") ||
                       acceptHeader.Contains("application/json");

    if (isApiRequest)
    {
        // Returns 503 Service Unavailable for API requests
        context.Response.StatusCode = 503;
        // ...
    }
    else
    {
        context.Response.Redirect("/setup");
    }
}
```

**This is the root cause of the Desktop UI connection failure!**

---

## 3. Problem Statement

### Primary Issue: Desktop UI Connection Failure

When setup is required but not yet completed:

1. Desktop UI calls `GET /api/license/time` to test connection
2. `SetupMiddleware` intercepts the request
3. Middleware returns **503 Service Unavailable** (not 200)
4. Desktop UI interprets this as "server unreachable"
5. User cannot perform any operations

### Secondary Issues

1. **Token visibility**: Requires console access to see setup token
2. **Offline scenarios**: No way to complete setup without server console
3. **Enterprise deployments**: IT admins may not have direct console access
4. **User experience**: Technical barrier for non-technical users

### Requirements for Solution

| Requirement | Priority | Notes |
|-------------|----------|-------|
| Work without internet at install time | MUST | Air-gapped environments |
| Not break Desktop UI connectivity | MUST | Critical regression |
| Enterprise-grade security | MUST | Strong passwords, audit logging |
| User-friendly for non-technical users | SHOULD | No console required |
| Support multiple deployment models | SHOULD | Cloud, on-premise, embedded |
| Minimal changes to existing code | NICE | Reduce risk |

---

## 4. Plan Options

### Option A: Installation-Time Admin Creation (Windows Installer Integration)

**Concept:** During software installation, the installer prompts for initial admin credentials and pre-populates the database before the server starts.

```
[Installer Screen]
+--------------------------------------------------+
|  FathomOS License Server Setup                    |
|                                                   |
|  Create Initial Administrator Account             |
|                                                   |
|  Email:    [admin@company.com           ]         |
|  Username: [admin                        ]         |
|  Password: [********************        ]         |
|  Confirm:  [********************        ]         |
|                                                   |
|  [ ] Generate random password and save to file   |
|                                                   |
|                     [Install]                     |
+--------------------------------------------------+
```

**How It Works:**

1. Windows Installer (WiX, InstallShield, or NSIS) includes custom action
2. Custom action collects admin credentials during install
3. Credentials are written to `admin-setup.json` in install directory
4. Server reads this file on first startup, creates admin, then deletes file
5. No setup middleware redirection needed

**Pros:**
- Completely offline - no server needs to be running
- Familiar UX for Windows users
- Admin account ready immediately after install
- No console access required

**Cons:**
- Tightly coupled to Windows installer
- Credentials temporarily stored in file (security concern)
- Requires installer modification
- Not portable to Linux/Docker deployments

---

### Option B: Desktop UI Setup Mode with Local-First Authentication

**Concept:** The Desktop UI detects when setup is required and presents its own setup wizard, which can complete setup either by communicating with the server OR by generating a signed setup package for offline scenarios.

```
[Desktop UI - Setup Detected]
+--------------------------------------------------+
|  FathomOS License Manager                         |
|                                                   |
|  Server Setup Required                            |
|                                                   |
|  The license server at localhost:5000 requires    |
|  initial configuration. Choose how to proceed:   |
|                                                   |
|  [x] Online Setup (server is running)            |
|  [ ] Offline Setup (server will start later)     |
|                                                   |
|  Email:    [admin@company.com           ]         |
|  Username: [admin                        ]         |
|  Password: [********************        ]         |
|                                                   |
|  [Complete Setup]   [Generate Offline Package]   |
+--------------------------------------------------+
```

**How It Works:**

1. Desktop UI attempts to connect to server
2. Server returns 503 with `{ "error": "setup_required" }` (current behavior)
3. Desktop UI parses response and shows setup wizard
4. **Online mode:** Desktop UI calls `/api/setup/complete` with auto-generated token bypass
5. **Offline mode:** Desktop UI generates encrypted `setup-package.json` with:
   - Hashed credentials
   - Signed by Desktop UI's private key
   - Timestamped with creation date
6. User copies `setup-package.json` to server's data directory
7. Server reads package on startup, validates signature, creates admin

**Pros:**
- Works both online and offline
- Single UI for all scenarios
- No installer modifications needed
- Cross-platform compatible

**Cons:**
- Requires Desktop UI to be installed first
- More complex implementation
- Key management for signing setup packages
- Slight increase in attack surface

---

### Option C: Pre-Seeded Configuration File (Hybrid Approach) - RECOMMENDED

**Concept:** The server supports multiple setup methods, with automatic detection and prioritization. A configuration file (`admin-credentials.json`) can be placed in the server's data directory before first startup for offline deployments.

```
Priority Order:
1. Pre-seeded admin-credentials.json file
2. Environment variables (ADMIN_EMAIL, ADMIN_USERNAME, ADMIN_PASSWORD)
3. Desktop UI setup mode (new)
4. Web setup wizard with token (existing)
```

**How It Works:**

**Scenario 1: Pre-seeded File (Offline)**
1. Administrator creates `admin-credentials.json`:
```json
{
    "email": "admin@company.com",
    "username": "admin",
    "password": "SecurePassword123!",
    "displayName": "System Administrator",
    "forcePasswordChange": true
}
```
2. File is placed in server's data directory
3. On first startup, server reads file, creates admin, deletes file
4. If `forcePasswordChange` is true, user must change password on first login

**Scenario 2: Desktop UI Setup Mode (Online/Offline Hybrid)**
1. Desktop UI connects to server, receives 503 with setup_required
2. Desktop UI shows integrated setup form
3. Desktop UI calls `/api/setup/ui-complete` endpoint (new)
4. This endpoint allows setup without token when called from localhost
5. Server creates admin and returns success

**Scenario 3: Existing Web Wizard (Online)**
- No change to current behavior

**Key Server Changes:**

```csharp
// New: Setup middleware allows Desktop UI setup endpoint
private static readonly string[] AllowedPaths = new[]
{
    "/health",
    "/setup",
    "/api/setup",
    "/api/setup/ui-complete",  // NEW: Desktop UI setup
    "/api/setup/status",       // Status check
    // ...
};

// New: Localhost bypass for Desktop UI
[HttpPost("ui-complete")]
public async Task<ActionResult> UiComplete([FromBody] SetupRequest request)
{
    // Only allow from localhost
    var clientIp = GetClientIp();
    if (!IsLocalhost(clientIp))
    {
        return Forbid("This endpoint is only available from localhost");
    }

    // Complete setup without token requirement
    // ... (similar to CompleteSetupAsync but no token validation)
}
```

**Pros:**
- Supports all deployment scenarios (offline, online, cloud, embedded)
- Minimal changes to existing code
- Backward compatible
- Enterprise-grade (file can be provisioned by IT scripts)
- Desktop UI integration for better UX
- Localhost bypass is secure (cannot be called remotely)

**Cons:**
- File-based setup has brief window where credentials exist unencrypted
- Slightly more complex logic in server startup
- Requires documentation for all methods

---

## 5. Comparison Matrix

| Criteria | Option A: Installer | Option B: Desktop UI First | Option C: Hybrid (Recommended) |
|----------|---------------------|---------------------------|-------------------------------|
| **Works Offline** | YES | YES | YES |
| **User-Friendliness** | HIGH (4/5) | HIGH (4/5) | VERY HIGH (5/5) |
| **Security** | MEDIUM (3/5) | HIGH (4/5) | HIGH (4/5) |
| **Implementation Effort** | HIGH (needs installer) | MEDIUM | LOW-MEDIUM |
| **Enterprise-Ready** | YES | YES | YES |
| **Cross-Platform** | NO (Windows only) | YES | YES |
| **Breaks Existing UI** | NO | NO | NO |
| **Backward Compatible** | NO | PARTIAL | YES |
| **Supports Docker** | NO | YES | YES |
| **Supports Cloud** | NO | YES | YES |
| **Maintenance Burden** | HIGH | MEDIUM | LOW |
| **Time to Implement** | 2-3 weeks | 1-2 weeks | 1 week |

### Recommendation Score

| Option | Score | Recommendation |
|--------|-------|----------------|
| Option A | 65/100 | Not recommended (platform-specific) |
| Option B | 80/100 | Good alternative |
| **Option C** | **95/100** | **RECOMMENDED** |

---

## 6. Recommended Solution

### Option C: Pre-Seeded Configuration File (Hybrid Approach)

This solution is recommended because it:

1. **Provides maximum flexibility** - Supports offline, online, and hybrid scenarios
2. **Minimal code changes** - Builds on existing infrastructure
3. **Backward compatible** - Existing web wizard and environment variable methods still work
4. **Enterprise-ready** - IT teams can script admin provisioning
5. **Fixes Desktop UI issue** - New localhost bypass endpoint
6. **Cross-platform** - Works on Windows, Linux, Docker, Cloud

### Architecture Overview

```
                                    +------------------+
                                    |   Server Start   |
                                    +--------+---------+
                                             |
                    +------------------------+------------------------+
                    |                        |                        |
                    v                        v                        v
        +-------------------+    +-------------------+    +-------------------+
        | Check for         |    | Check for         |    | Check for         |
        | admin-credentials |    | ADMIN_* env vars  |    | existing admin    |
        | .json file        |    |                   |    | in database       |
        +--------+----------+    +--------+----------+    +--------+----------+
                 |                        |                        |
          [Found]|                 [Found]|                 [Found]|
                 v                        v                        v
        +-------------------+    +-------------------+    +-------------------+
        | Create admin      |    | Create admin      |    | Setup complete!   |
        | Delete file       |    | from env vars     |    | Normal operation  |
        | Mark complete     |    | Mark complete     |    |                   |
        +--------+----------+    +--------+----------+    +-------------------+
                 |                        |
                 +------------------------+
                            |
                            v
                 +-------------------+
                 | Generate token    |
                 | Enable setup mode |
                 +--------+----------+
                          |
         +----------------+----------------+
         |                                 |
         v                                 v
+-------------------+           +-------------------+
| Desktop UI calls  |           | Browser accesses  |
| /api/setup/       |           | /setup            |
| ui-complete       |           | Web wizard        |
| (localhost only)  |           | (needs token)     |
+-------------------+           +-------------------+
```

---

## 7. Detailed Implementation Plan

### 7.1 Files to Create

#### 7.1.1 New File: `AdminSetupFileService.cs`

**Location:** `LicensingSystem.Server/Services/AdminSetupFileService.cs`

```csharp
// LicensingSystem.Server/Services/AdminSetupFileService.cs
// Service to handle file-based admin setup for offline deployments

using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

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
                _logger.LogWarning("admin-credentials.json missing required fields");
                return null;
            }

            _logger.LogInformation("Found valid admin-credentials.json file");
            return credentials;
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
            }

            File.Delete(filePath);
            _logger.LogInformation("Securely deleted admin-credentials.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting admin-credentials.json");
            // Try simple delete as fallback
            try { File.Delete(filePath); } catch { }
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
```

### 7.2 Files to Modify

#### 7.2.1 Modify: `SetupMiddleware.cs`

**Changes:** Add Desktop UI endpoint to allowed paths

```csharp
// Line 15-23: Add new allowed paths
private static readonly string[] AllowedPaths = new[]
{
    "/health",
    "/setup",
    "/setup.html",
    "/api/setup",
    "/api/setup/status",           // Explicit
    "/api/setup/ui-complete",      // NEW: Desktop UI setup endpoint
    "/api/setup/validate-token",
    "/api/setup/complete",
    "/api/setup/password-requirements",
    "/swagger",
    "/favicon.ico",
    "/api/license/time"            // NEW: Allow time check even during setup
};
```

#### 7.2.2 Modify: `SetupController.cs`

**Changes:** Add new endpoint for Desktop UI setup

```csharp
// Add new endpoint after line 163

/// <summary>
/// Complete setup from Desktop UI (localhost only, no token required)
/// POST /api/setup/ui-complete
/// </summary>
[HttpPost("ui-complete")]
public async Task<ActionResult<SetupCompleteResponse>> UiComplete(
    [FromBody] SetupCompleteRequest request)
{
    var clientIp = GetClientIp();

    // Security: Only allow from localhost
    if (!IsLocalhost(clientIp))
    {
        await _auditService.LogAsync("SETUP_UI_BLOCKED", "Setup", null, null,
            request.Email, clientIp, "UI setup attempted from non-localhost", false);

        return StatusCode(403, new {
            message = "This endpoint is only available from localhost connections."
        });
    }

    // Check if setup is still required
    var status = await _setupService.GetSetupStatusAsync();
    if (!status.SetupRequired)
    {
        return BadRequest(new SetupCompleteResponse
        {
            Success = false,
            Message = "Setup has already been completed."
        });
    }

    // Validate required fields (same as Complete endpoint)
    if (string.IsNullOrWhiteSpace(request.Email))
        return BadRequest(new { message = "Email is required." });
    if (string.IsNullOrWhiteSpace(request.Username))
        return BadRequest(new { message = "Username is required." });
    if (string.IsNullOrWhiteSpace(request.Password))
        return BadRequest(new { message = "Password is required." });

    try
    {
        // Complete setup without token requirement
        var result = await _setupService.CompleteSetupAsync(new SetupCompletionRequest
        {
            SetupToken = null,  // No token required for localhost
            Email = request.Email,
            Username = request.Username,
            Password = request.Password,
            DisplayName = request.DisplayName,
            EnableTwoFactor = request.EnableTwoFactor
        }, clientIp);

        if (!result.Success)
        {
            return BadRequest(new SetupCompleteResponse
            {
                Success = false,
                Message = result.ErrorMessage
            });
        }

        await _auditService.LogAsync("SETUP_UI_COMPLETED", "Setup",
            result.AdminUserId?.ToString(), null, request.Email, clientIp,
            "Setup completed via Desktop UI", true);

        return Ok(new SetupCompleteResponse
        {
            Success = true,
            Message = "Setup completed successfully! You can now use the Desktop UI.",
            RedirectUrl = null  // Desktop UI handles its own navigation
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error completing UI setup");
        return StatusCode(500, new { message = "Error completing setup." });
    }
}

// Helper method to check if request is from localhost
private bool IsLocalhost(string ipAddress)
{
    if (string.IsNullOrEmpty(ipAddress))
        return false;

    return ipAddress == "127.0.0.1"
        || ipAddress == "::1"
        || ipAddress == "localhost"
        || ipAddress.StartsWith("127.")
        || (ipAddress == "::ffff:127.0.0.1");
}
```

#### 7.2.3 Modify: `SetupService.cs`

**Changes:** Add support for file-based setup and localhost bypass

```csharp
// Modify CompleteSetupAsync to handle null token for localhost calls
// Around line 206-218, change the token validation to:

// Validate setup token (unless bypassed by environment setup or UI setup)
if (!string.IsNullOrEmpty(request.SetupToken))
{
    var tokenValidation = await ValidateSetupTokenAsync(request.SetupToken, ipAddress);
    if (!tokenValidation.IsValid)
    {
        return new SetupCompletionResult
        {
            Success = false,
            ErrorMessage = tokenValidation.ErrorMessage
        };
    }
}
else if (ipAddress != "localhost" && ipAddress != "127.0.0.1" && ipAddress != "::1")
{
    // Token is required for non-localhost requests
    return new SetupCompletionResult
    {
        Success = false,
        ErrorMessage = "Setup token is required for remote setup."
    };
}
// If token is null and it's localhost, allow setup without token
```

#### 7.2.4 Modify: `Program.cs`

**Changes:** Add file-based setup check during startup

```csharp
// After line 258 (before the existing setup check), add:

// ==================== Check for Pre-Seeded Admin Credentials File ====================
Console.WriteLine("Checking for pre-seeded admin credentials file...");
try
{
    using var fileSetupScope = app.Services.CreateScope();
    var fileSetupService = fileSetupScope.ServiceProvider.GetService<IAdminSetupFileService>();
    var setupService = fileSetupScope.ServiceProvider.GetRequiredService<ISetupService>();

    if (fileSetupService != null)
    {
        var credentials = await fileSetupService.ReadSetupFileAsync();

        if (credentials != null && await setupService.IsSetupRequiredAsync())
        {
            Console.WriteLine("Found admin-credentials.json - creating admin from file...");

            var result = await setupService.CompleteSetupAsync(new SetupCompletionRequest
            {
                Email = credentials.Email,
                Username = credentials.Username,
                Password = credentials.Password,
                DisplayName = credentials.DisplayName ?? credentials.Username
            }, "file-based-setup");

            if (result.Success)
            {
                Console.WriteLine("[OK] Admin account created from admin-credentials.json");
                await fileSetupService.DeleteSetupFileAsync();

                // TODO: If ForcePasswordChange is true, mark user for password change
            }
            else
            {
                Console.WriteLine($"[WARNING] Failed to create admin from file: {result.ErrorMessage}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[WARNING] File-based setup check error: {ex.Message}");
}

// Existing setup check continues below...
```

Also add service registration:

```csharp
// Around line 106, add:
builder.Services.AddScoped<IAdminSetupFileService, AdminSetupFileService>();
```

### 7.3 Desktop UI Changes

#### 7.3.1 Modify: `MainWindow.xaml.cs`

**Changes:** Handle setup_required response and show setup dialog

```csharp
// Modify ConnectToServerAsync method (around line 169)

private async Task ConnectToServerAsync()
{
    if (string.IsNullOrEmpty(_serverUrl))
    {
        _isConnected = false;
        UpdateServerStatus();
        return;
    }

    try
    {
        var response = await _httpClient.GetAsync($"{_serverUrl}/api/license/time");

        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            // Check if setup is required
            var content = await response.Content.ReadAsStringAsync();
            if (content.Contains("setup_required"))
            {
                _isConnected = false;
                UpdateServerStatus();

                // Show setup dialog
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowSetupRequiredDialog();
                });
                return;
            }
        }

        _isConnected = response.IsSuccessStatusCode;

        if (_isConnected)
        {
            await UpdateServerInfoAsync();
        }
    }
    catch
    {
        _isConnected = false;
    }

    UpdateServerStatus();

    if (_isConnected)
    {
        await LoadLicensesAsync();
    }
}

// Add new method to show setup dialog
private void ShowSetupRequiredDialog()
{
    var setupWindow = new SetupRequiredWindow(_serverUrl, _httpClient);
    setupWindow.Owner = this;

    if (setupWindow.ShowDialog() == true)
    {
        // Setup completed, try to connect again
        _ = ConnectToServerAsync();
    }
}
```

#### 7.3.2 New File: `SetupRequiredWindow.xaml`

**Location:** `LicensingSystem.LicenseGeneratorUI/Views/SetupRequiredWindow.xaml`

```xml
<Window x:Class="LicenseGeneratorUI.Views.SetupRequiredWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Server Setup Required"
        Height="550" Width="450"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        Background="#0D1117">

    <Grid Margin="30">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <StackPanel Grid.Row="0" Margin="0,0,0,20">
            <TextBlock Text="Server Setup Required"
                       FontSize="24" FontWeight="Bold"
                       Foreground="#E6EDF3"/>
            <TextBlock Text="Create the first administrator account to begin."
                       FontSize="13" Foreground="#8B949E" Margin="0,8,0,0"/>
        </StackPanel>

        <!-- Info Alert -->
        <Border Grid.Row="1" Background="#1F2937"
                BorderBrush="#3B82F6" BorderThickness="1"
                CornerRadius="8" Padding="15" Margin="0,0,0,20">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="&#9432;" FontSize="18" Foreground="#3B82F6"
                           Margin="0,0,10,0"/>
                <TextBlock TextWrapping="Wrap" Foreground="#D1D5DB">
                    <Run Text="The license server requires initial setup."/>
                    <LineBreak/>
                    <Run Text="This wizard will create your administrator account."/>
                </TextBlock>
            </StackPanel>
        </Border>

        <!-- Form -->
        <StackPanel Grid.Row="2">
            <TextBlock Text="Email Address *" Foreground="#8B949E"
                       FontSize="12" Margin="0,0,0,6"/>
            <TextBox x:Name="EmailInput" Height="36" FontSize="14"
                     Background="#161B22" Foreground="#E6EDF3"
                     BorderBrush="#30363D" Padding="10,8"/>

            <TextBlock Text="Username *" Foreground="#8B949E"
                       FontSize="12" Margin="0,15,0,6"/>
            <TextBox x:Name="UsernameInput" Height="36" FontSize="14"
                     Background="#161B22" Foreground="#E6EDF3"
                     BorderBrush="#30363D" Padding="10,8"/>

            <TextBlock Text="Display Name" Foreground="#8B949E"
                       FontSize="12" Margin="0,15,0,6"/>
            <TextBox x:Name="DisplayNameInput" Height="36" FontSize="14"
                     Background="#161B22" Foreground="#E6EDF3"
                     BorderBrush="#30363D" Padding="10,8"/>

            <TextBlock Text="Password *" Foreground="#8B949E"
                       FontSize="12" Margin="0,15,0,6"/>
            <PasswordBox x:Name="PasswordInput" Height="36" FontSize="14"
                         Background="#161B22" Foreground="#E6EDF3"
                         BorderBrush="#30363D" Padding="10,8"
                         PasswordChanged="PasswordInput_Changed"/>

            <TextBlock x:Name="PasswordHint" FontSize="11" Foreground="#8B949E"
                       Margin="0,4,0,0" TextWrapping="Wrap"
                       Text="Min 12 chars, uppercase, lowercase, digit, special char"/>

            <TextBlock Text="Confirm Password *" Foreground="#8B949E"
                       FontSize="12" Margin="0,15,0,6"/>
            <PasswordBox x:Name="ConfirmPasswordInput" Height="36" FontSize="14"
                         Background="#161B22" Foreground="#E6EDF3"
                         BorderBrush="#30363D" Padding="10,8"
                         PasswordChanged="ConfirmPasswordInput_Changed"/>

            <TextBlock x:Name="ConfirmHint" FontSize="11" Foreground="#F85149"
                       Margin="0,4,0,0" Visibility="Collapsed"/>

            <!-- Error message -->
            <Border x:Name="ErrorBorder" Background="#2D1B1B"
                    BorderBrush="#F85149" BorderThickness="1"
                    CornerRadius="6" Padding="12" Margin="0,15,0,0"
                    Visibility="Collapsed">
                <TextBlock x:Name="ErrorText" Foreground="#F85149"
                           TextWrapping="Wrap" FontSize="12"/>
            </Border>
        </StackPanel>

        <!-- Buttons -->
        <StackPanel Grid.Row="3" Orientation="Horizontal"
                    HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="Cancel" Width="90" Height="36"
                    Background="Transparent" Foreground="#8B949E"
                    BorderBrush="#30363D" Click="Cancel_Click"/>
            <Button x:Name="CompleteButton" Content="Complete Setup"
                    Width="140" Height="36" Margin="10,0,0,0"
                    Background="#238636" Foreground="White"
                    BorderThickness="0" Click="CompleteSetup_Click"
                    IsEnabled="False"/>
        </StackPanel>
    </Grid>
</Window>
```

#### 7.3.3 New File: `SetupRequiredWindow.xaml.cs`

**Location:** `LicensingSystem.LicenseGeneratorUI/Views/SetupRequiredWindow.xaml.cs`

```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LicenseGeneratorUI.Views;

public partial class SetupRequiredWindow : Window
{
    private readonly string _serverUrl;
    private readonly HttpClient _httpClient;

    public SetupRequiredWindow(string serverUrl, HttpClient httpClient)
    {
        InitializeComponent();
        _serverUrl = serverUrl;
        _httpClient = httpClient;
    }

    private void PasswordInput_Changed(object sender, RoutedEventArgs e)
    {
        ValidatePassword();
        CheckFormValidity();
    }

    private void ConfirmPasswordInput_Changed(object sender, RoutedEventArgs e)
    {
        var password = PasswordInput.Password;
        var confirm = ConfirmPasswordInput.Password;

        if (!string.IsNullOrEmpty(confirm))
        {
            if (password != confirm)
            {
                ConfirmHint.Text = "Passwords do not match";
                ConfirmHint.Visibility = Visibility.Visible;
            }
            else
            {
                ConfirmHint.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            ConfirmHint.Visibility = Visibility.Collapsed;
        }

        CheckFormValidity();
    }

    private void ValidatePassword()
    {
        var password = PasswordInput.Password;
        var requirements = new List<string>();

        if (password.Length < 12) requirements.Add("12+ chars");
        if (!Regex.IsMatch(password, @"[A-Z]")) requirements.Add("uppercase");
        if (!Regex.IsMatch(password, @"[a-z]")) requirements.Add("lowercase");
        if (!Regex.IsMatch(password, @"[0-9]")) requirements.Add("digit");
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            requirements.Add("special char");

        if (requirements.Count > 0)
        {
            PasswordHint.Text = $"Missing: {string.Join(", ", requirements)}";
            PasswordHint.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#F85149"));
        }
        else
        {
            PasswordHint.Text = "Password meets all requirements";
            PasswordHint.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#3FB950"));
        }
    }

    private void CheckFormValidity()
    {
        var email = EmailInput.Text.Trim();
        var username = UsernameInput.Text.Trim();
        var password = PasswordInput.Password;
        var confirm = ConfirmPasswordInput.Password;

        var isPasswordValid = password.Length >= 12 &&
            Regex.IsMatch(password, @"[A-Z]") &&
            Regex.IsMatch(password, @"[a-z]") &&
            Regex.IsMatch(password, @"[0-9]") &&
            Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");

        var isValid = !string.IsNullOrEmpty(email) &&
            email.Contains("@") &&
            username.Length >= 3 &&
            isPasswordValid &&
            password == confirm;

        CompleteButton.IsEnabled = isValid;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void CompleteSetup_Click(object sender, RoutedEventArgs e)
    {
        CompleteButton.IsEnabled = false;
        ErrorBorder.Visibility = Visibility.Collapsed;

        try
        {
            var request = new
            {
                email = EmailInput.Text.Trim(),
                username = UsernameInput.Text.Trim(),
                displayName = string.IsNullOrWhiteSpace(DisplayNameInput.Text)
                    ? UsernameInput.Text.Trim()
                    : DisplayNameInput.Text.Trim(),
                password = PasswordInput.Password
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/setup/ui-complete", request);

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    "Administrator account created successfully!\n\n" +
                    "The license server is now ready to use.",
                    "Setup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ShowError($"Setup failed: {error}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally
        {
            CheckFormValidity();  // Re-enable button if form is valid
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}
```

### 7.4 Database Changes

No database schema changes are required. The existing `AdminUserRecord` and `SetupConfigRecord` entities are sufficient.

### 7.5 Configuration Changes

#### 7.5.1 Sample `admin-credentials.json` Template

Create documentation file for offline deployments:

```json
{
    "email": "admin@yourcompany.com",
    "username": "admin",
    "password": "YourSecurePassword123!",
    "displayName": "System Administrator",
    "forcePasswordChange": true,
    "createdBy": "IT Deployment Script",
    "createdAt": "2026-01-17T00:00:00Z"
}
```

### 7.6 UI/UX Flow Diagrams

#### 7.6.1 Online Setup Flow (Desktop UI)

```
+-------------------+     +-------------------+     +-------------------+
|   User launches   |     |   Desktop UI      |     |   Setup window    |
|   Desktop UI      | --> |   detects 503     | --> |   appears         |
|                   |     |   setup_required  |     |                   |
+-------------------+     +-------------------+     +--------+----------+
                                                             |
                                                             v
+-------------------+     +-------------------+     +-------------------+
|   Main window     |     |   Server creates  |     |   User fills      |
|   loads normally  | <-- |   admin account   | <-- |   credentials     |
|                   |     |                   |     |                   |
+-------------------+     +-------------------+     +-------------------+
```

#### 7.6.2 Offline Setup Flow (File-Based)

```
+-------------------+     +-------------------+     +-------------------+
|   IT Admin        |     |   Place file in   |     |   Server reads    |
|   creates JSON    | --> |   data directory  | --> |   file on start   |
|   credentials     |     |                   |     |                   |
+-------------------+     +-------------------+     +--------+----------+
                                                             |
                                                             v
+-------------------+     +-------------------+     +-------------------+
|   Normal server   |     |   File securely   |     |   Admin account   |
|   operation       | <-- |   deleted         | <-- |   created         |
|                   |     |                   |     |                   |
+-------------------+     +-------------------+     +-------------------+
```

### 7.7 Security Considerations

| Concern | Mitigation |
|---------|------------|
| Credentials in plaintext file | File is securely deleted (overwritten with zeros) immediately after processing |
| Desktop UI endpoint exposed | Only accessible from localhost (127.0.0.1, ::1) |
| Brute force on UI endpoint | Rate limiting already exists via RateLimitService |
| Token bypass abuse | Localhost check is performed server-side, cannot be spoofed via headers |
| File-based credentials stolen | Document recommendation to use secure file permissions and delete after deployment |

---

## 8. Bug Analysis

### Root Cause of Desktop UI Connection Failure

**Issue:** Desktop UI Manager fails to connect to server when setup is required.

**Root Cause Analysis:**

1. **SetupMiddleware behavior:** When no admin exists, the middleware returns HTTP 503 for ALL API requests not in the allowed paths list.

2. **Desktop UI connection test:** The UI calls `GET /api/license/time` to verify connectivity.

3. **503 is not in allowed paths:** The endpoint `/api/license/time` is blocked by the middleware during setup mode.

4. **UI interprets 503 as failure:** The Desktop UI checks `response.IsSuccessStatusCode`, which is false for 503.

5. **Result:** User sees "Disconnected" even though server is running.

**Evidence from Code:**

```csharp
// SetupMiddleware.cs line 61-67
if (isApiRequest)
{
    context.Response.StatusCode = 503;  // <-- This blocks the Desktop UI
    context.Response.ContentType = "application/json";
    // Returns setup_required error
}

// MainWindow.xaml.cs line 180-181
var response = await _httpClient.GetAsync($"{_serverUrl}/api/license/time");
_isConnected = response.IsSuccessStatusCode;  // <-- False for 503
```

### Previous Implementation Issues

The previous implementation likely had one or more of these problems:

1. **Over-aggressive middleware:** Blocked too many paths, preventing basic connectivity checks
2. **No special handling for setup_required:** Desktop UI didn't recognize this as a recoverable state
3. **Missing localhost bypass:** No way for Desktop UI to complete setup directly
4. **Incomplete allowed paths list:** May have missed critical endpoints

### Fix Summary

The recommended solution addresses all these issues:

1. Add `/api/license/time` to allowed paths (quick health check)
2. Add `/api/setup/ui-complete` endpoint with localhost-only access
3. Modify Desktop UI to detect setup_required and show setup dialog
4. Maintain existing web wizard for remote/browser-based setup

---

## 9. Testing Checklist

### 9.1 Unit Tests

- [ ] `AdminSetupFileService.ReadSetupFileAsync()` returns null when file doesn't exist
- [ ] `AdminSetupFileService.ReadSetupFileAsync()` parses valid JSON correctly
- [ ] `AdminSetupFileService.ReadSetupFileAsync()` returns null for invalid JSON
- [ ] `AdminSetupFileService.DeleteSetupFileAsync()` securely deletes file
- [ ] `SetupController.UiComplete()` rejects non-localhost requests
- [ ] `SetupController.UiComplete()` accepts localhost requests
- [ ] `SetupService.CompleteSetupAsync()` allows null token for localhost
- [ ] Password validation enforces all requirements

### 9.2 Integration Tests

- [ ] Server starts successfully with valid admin-credentials.json
- [ ] Server starts successfully without admin-credentials.json
- [ ] Admin account is created from file on first startup
- [ ] File is deleted after successful account creation
- [ ] Server starts normally after setup is complete
- [ ] Environment variable setup still works
- [ ] Web wizard with token still works

### 9.3 Desktop UI Tests

- [ ] UI shows setup dialog when server returns setup_required
- [ ] UI can complete setup via /api/setup/ui-complete
- [ ] UI shows error for invalid credentials
- [ ] UI shows error for network failures
- [ ] UI connects successfully after setup completion
- [ ] UI works normally after setup is already complete

### 9.4 Security Tests

- [ ] /api/setup/ui-complete returns 403 from non-localhost IP
- [ ] File-based setup file is deleted after processing
- [ ] Rate limiting applies to setup endpoints
- [ ] Audit logs record all setup events
- [ ] Password requirements are enforced

### 9.5 End-to-End Scenarios

| Scenario | Test Steps | Expected Result |
|----------|------------|-----------------|
| Fresh install with file | Place admin-credentials.json, start server | Admin created, file deleted |
| Fresh install with Desktop UI | Start server, open Desktop UI | Setup dialog appears, setup completes |
| Fresh install with web wizard | Start server, open browser to /setup | Web wizard works as before |
| Fresh install with env vars | Set ADMIN_* vars, start server | Admin created from env vars |
| Existing setup + Desktop UI | Server already setup, open Desktop UI | Connects normally, no setup dialog |
| Invalid file | Place malformed JSON, start server | Logs warning, continues to token flow |

---

## 10. Rollback Plan

### 10.1 When to Rollback

Rollback should be considered if:

- Desktop UI fails to connect after changes
- Server fails to start
- Security vulnerability discovered
- Setup completes but admin cannot log in
- Data corruption occurs

### 10.2 Rollback Steps

#### Step 1: Revert Server Code

```bash
# If using git
git checkout HEAD~1 -- LicensingSystem.Server/

# Or restore from backup
copy backup\LicensingSystem.Server\* LicensingSystem.Server\ /Y
```

#### Step 2: Revert Desktop UI Code

```bash
git checkout HEAD~1 -- LicensingSystem.LicenseGeneratorUI/
```

#### Step 3: Rebuild and Deploy

```bash
dotnet build LicensingSystem.Server
dotnet build LicensingSystem.LicenseGeneratorUI
```

#### Step 4: Verify Rollback

1. Start server
2. Verify existing admins can log in
3. Verify Desktop UI connects
4. Verify web wizard works

### 10.3 Database Rollback

No database schema changes are made, so no database rollback is needed.

### 10.4 Files to Preserve Before Changes

Create backups of:

```
LicensingSystem.Server/
  |-- Services/SetupService.cs
  |-- Middleware/SetupMiddleware.cs
  |-- Controllers/SetupController.cs
  |-- Program.cs

LicensingSystem.LicenseGeneratorUI/
  |-- Views/MainWindow.xaml.cs
```

---

## 11. Appendices

### Appendix A: Complete File Change Summary

| File | Action | Description |
|------|--------|-------------|
| `Services/AdminSetupFileService.cs` | CREATE | New service for file-based setup |
| `Services/SetupService.cs` | MODIFY | Add localhost bypass support |
| `Middleware/SetupMiddleware.cs` | MODIFY | Add new allowed paths |
| `Controllers/SetupController.cs` | MODIFY | Add ui-complete endpoint |
| `Program.cs` | MODIFY | Add file-based setup check, register new service |
| `Views/SetupRequiredWindow.xaml` | CREATE | New Desktop UI setup window |
| `Views/SetupRequiredWindow.xaml.cs` | CREATE | Code-behind for setup window |
| `Views/MainWindow.xaml.cs` | MODIFY | Handle setup_required response |

### Appendix B: API Endpoint Summary

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/setup/status` | GET | None | Check if setup is required |
| `/api/setup/validate-token` | POST | Token | Validate setup token |
| `/api/setup/complete` | POST | Token | Complete setup with token |
| `/api/setup/ui-complete` | POST | Localhost | Complete setup from Desktop UI |
| `/api/setup/password-requirements` | GET | None | Get password rules |

### Appendix C: Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ADMIN_EMAIL` | Auto-setup email | (none) |
| `ADMIN_USERNAME` | Auto-setup username | (none) |
| `ADMIN_PASSWORD` | Auto-setup password | (none) |
| `DATA_PATH` | Path for admin-credentials.json | `./data` |

### Appendix D: Deployment Checklist

- [ ] Review and approve plan
- [ ] Create backups of all files to be modified
- [ ] Implement server-side changes
- [ ] Implement Desktop UI changes
- [ ] Run unit tests
- [ ] Run integration tests
- [ ] Test offline scenario with admin-credentials.json
- [ ] Test online scenario with Desktop UI
- [ ] Test web wizard scenario
- [ ] Test environment variable scenario
- [ ] Security review
- [ ] Update documentation
- [ ] Deploy to staging environment
- [ ] Final testing in staging
- [ ] Deploy to production

---

## Document Control

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-17 | R&D Planning Agent | Initial document |
| 2.0 | 2026-01-17 | R&D Planning Agent | Added bug analysis, expanded implementation details |

---

**End of Document**
