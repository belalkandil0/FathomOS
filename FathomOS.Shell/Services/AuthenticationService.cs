using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FathomOS.Core.Interfaces;
using FathomOS.Core.Messaging;
using FathomOS.Shell.Models;

namespace FathomOS.Shell.Services;

/// <summary>
/// Shell-level authentication service implementing IAuthenticationService.
/// Provides centralized authentication for all FathomOS modules with:
/// - HTTP-based authentication against API endpoints
/// - Offline authentication using securely cached credentials
/// - Token management (storage, refresh)
/// - Event publishing via IEventAggregator
/// </summary>
public class AuthenticationService : IAuthenticationService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISettingsService _settingsService;

    private AuthenticatedUser? _currentUser;
    private string _accessToken = string.Empty;
    private string _refreshToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private bool _disposed;

    // Settings keys for persistent storage
    private const string SettingsKeyApiBaseUrl = "Auth.ApiBaseUrl";
    private const string SettingsKeyCachedCredentials = "Auth.CachedCredentials";
    private const string SettingsKeyStoredTokens = "Auth.StoredTokens";
    private const string DefaultApiBaseUrl = "https://api.fathomos.com";

    // Cache expiration settings
    private const int CachedCredentialsDays = 30;
    private const int OfflineSessionHours = 24;

    /// <inheritdoc />
    public IUser? CurrentUser => _currentUser;

    /// <inheritdoc />
    public bool IsAuthenticated => _currentUser != null && DateTime.UtcNow < _tokenExpiry;

    /// <inheritdoc />
    public string AccessToken => _accessToken;

    /// <inheritdoc />
    public event EventHandler<IUser?>? AuthenticationChanged;

    /// <summary>
    /// Creates a new AuthenticationService instance.
    /// </summary>
    /// <param name="eventAggregator">Event aggregator for publishing authentication events</param>
    /// <param name="settingsService">Settings service for persistent storage</param>
    public AuthenticationService(IEventAggregator eventAggregator, ISettingsService settingsService)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Configure base URL from settings
        var baseUrl = _settingsService.Get(SettingsKeyApiBaseUrl, DefaultApiBaseUrl);
        Configure(baseUrl);

        // Subscribe to logout request events
        _eventAggregator.Subscribe<UserLogoutRequestedEvent>(OnLogoutRequested);

        // Attempt to restore session from stored tokens
        TryRestoreSession();
    }

    /// <summary>
    /// Configure the API base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL for authentication API calls</param>
    public void Configure(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _settingsService.Set(SettingsKeyApiBaseUrl, baseUrl);
        _settingsService.Save();
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new AuthenticationResult(false, Error: "Username is required");

        if (string.IsNullOrWhiteSpace(password))
            return new AuthenticationResult(false, Error: "Password is required");

        try
        {
            // Try online authentication first
            var result = await AuthenticateOnlineAsync(username, password, "auth/login");

            if (result.Success)
            {
                // Cache credentials for offline use
                await CacheCredentialsAsync(username, password, null);
                return result;
            }

            // If online auth failed due to network error, try offline
            if (result.Error?.Contains("Connection error", StringComparison.OrdinalIgnoreCase) == true)
            {
                var offlineResult = TryOfflineAuthentication(username, password, isPin: false);
                if (offlineResult.Success)
                {
                    return offlineResult;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.LoginAsync error: {ex.Message}");

            // Try offline authentication on any exception
            var offlineResult = TryOfflineAuthentication(username, password, isPin: false);
            if (offlineResult.Success)
            {
                return offlineResult;
            }

            return new AuthenticationResult(false, Error: $"Authentication failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult> LoginWithPinAsync(string username, string pin)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new AuthenticationResult(false, Error: "Username is required");

        if (string.IsNullOrWhiteSpace(pin))
            return new AuthenticationResult(false, Error: "PIN is required");

        try
        {
            // Try online PIN authentication first
            var response = await _httpClient.PostAsJsonAsync("auth/pin-login", new { username, pin });

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
                if (loginResponse?.User != null)
                {
                    SetAuthenticatedSession(loginResponse);

                    // Update cached PIN
                    await CacheCredentialsAsync(username, null, pin);

                    return new AuthenticationResult(true, _currentUser);
                }
            }

            // If online failed, try offline
            var offlineResult = TryOfflineAuthentication(username, pin, isPin: true);
            if (offlineResult.Success)
            {
                return offlineResult;
            }

            var error = await response.Content.ReadAsStringAsync();
            return new AuthenticationResult(false, Error: string.IsNullOrEmpty(error) ? "PIN login failed" : error);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.LoginWithPinAsync error: {ex.Message}");

            // Try offline authentication
            var offlineResult = TryOfflineAuthentication(username, pin, isPin: true);
            if (offlineResult.Success)
            {
                return offlineResult;
            }

            return new AuthenticationResult(false, Error: $"PIN authentication failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void Logout()
    {
        _currentUser = null;
        _accessToken = string.Empty;
        _refreshToken = string.Empty;
        _tokenExpiry = DateTime.MinValue;

        _httpClient.DefaultRequestHeaders.Authorization = null;

        // Clear stored tokens (but keep cached credentials for offline use)
        _settingsService.Remove(SettingsKeyStoredTokens);
        _settingsService.Save();

        // Publish events
        AuthenticationChanged?.Invoke(this, null);
        _eventAggregator.Publish(new UserAuthenticationChangedEvent(null, false));

        System.Diagnostics.Debug.WriteLine("AuthenticationService: User logged out");
    }

    /// <inheritdoc />
    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            System.Diagnostics.Debug.WriteLine("AuthenticationService: No refresh token available");
            return false;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/refresh", new { refreshToken = _refreshToken });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
                if (result != null)
                {
                    _accessToken = result.AccessToken;
                    _refreshToken = result.RefreshToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn);

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                    // Store the refreshed tokens
                    StoreTokens();

                    System.Diagnostics.Debug.WriteLine("AuthenticationService: Token refreshed successfully");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.RefreshTokenAsync error: {ex.Message}");
        }

        return false;
    }

    /// <inheritdoc />
    public bool HasPermission(string permission)
    {
        if (_currentUser == null) return false;

        // Super admins have all permissions
        if (_currentUser.IsSuperAdmin) return true;

        // Check explicit permissions
        if (_currentUser.Permissions.Any(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check for wildcard permissions (e.g., "certificates.*" grants "certificates.create")
        var permissionCategory = permission.Split('.').FirstOrDefault();
        if (permissionCategory != null &&
            _currentUser.Permissions.Any(p => p.Equals($"{permissionCategory}.*", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check role-based permission grants
        // Administrator role has all permissions
        if (_currentUser.Roles.Any(r =>
            r.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool HasRole(params string[] roles)
    {
        if (_currentUser == null) return false;

        // Super admins implicitly have all roles
        if (_currentUser.IsSuperAdmin) return true;

        return _currentUser.Roles.Any(userRole =>
            roles.Any(r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public Task<bool> ShowLoginDialogAsync(object? owner = null)
    {
        // Run on UI thread
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var ownerWindow = owner as System.Windows.Window;

            // Check if we have a remembered/cached user for PIN login
            var cachedUsers = GetCachedCredentials();
            var rememberedUsername = _settingsService.Get<string?>("Auth.RememberedUsername", null);

            if (!string.IsNullOrEmpty(rememberedUsername))
            {
                // Find the cached user with a PIN
                var cachedUser = cachedUsers.FirstOrDefault(u =>
                    u.Username.Equals(rememberedUsername, StringComparison.OrdinalIgnoreCase) &&
                    u.IsActive &&
                    u.ExpiresAt > DateTime.UtcNow &&
                    !string.IsNullOrEmpty(u.PinHash));

                if (cachedUser != null)
                {
                    // Show PIN dialog for returning user
                    var displayName = !string.IsNullOrWhiteSpace(cachedUser.FirstName)
                        ? $"{cachedUser.FirstName} {cachedUser.LastName}".Trim()
                        : cachedUser.Username;

                    var pinDialog = new Views.PinLoginDialog(this, cachedUser.Username, displayName);
                    if (ownerWindow != null)
                    {
                        pinDialog.Owner = ownerWindow;
                    }

                    var pinResult = pinDialog.ShowDialog();

                    // If user wants to switch accounts, show the full login
                    if (pinDialog.SwitchUserRequested)
                    {
                        return ShowFullLoginDialog(ownerWindow);
                    }

                    return pinResult == true && pinDialog.LoginSuccessful;
                }
            }

            // No remembered user or no PIN cached - show full login dialog
            return ShowFullLoginDialog(ownerWindow);
        }).Task;
    }

    /// <summary>
    /// Shows the full login dialog (username + password).
    /// </summary>
    private bool ShowFullLoginDialog(System.Windows.Window? owner)
    {
        var loginWindow = new Views.LoginWindow(this);
        if (owner != null)
        {
            loginWindow.Owner = owner;
        }

        var result = loginWindow.ShowDialog();
        return result == true && loginWindow.LoginSuccessful;
    }

    /// <summary>
    /// Performs online authentication against the API.
    /// </summary>
    private async Task<AuthenticationResult> AuthenticateOnlineAsync(string username, string password, string endpoint)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, new { username, password });

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
                if (loginResponse?.User != null)
                {
                    SetAuthenticatedSession(loginResponse);
                    return new AuthenticationResult(true, _currentUser);
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            return new AuthenticationResult(false, Error: string.IsNullOrEmpty(error) ? "Login failed" : error);
        }
        catch (HttpRequestException ex)
        {
            return new AuthenticationResult(false, Error: $"Connection error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return new AuthenticationResult(false, Error: "Connection error: Request timed out");
        }
    }

    /// <summary>
    /// Sets the authenticated session from a login response.
    /// </summary>
    private void SetAuthenticatedSession(LoginApiResponse loginResponse)
    {
        _accessToken = loginResponse.AccessToken;
        _refreshToken = loginResponse.RefreshToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(loginResponse.ExpiresIn);
        _currentUser = AuthenticatedUser.FromLoginResponse(loginResponse.User!);

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        // Store tokens for session restoration
        StoreTokens();

        // Publish events
        AuthenticationChanged?.Invoke(this, _currentUser);
        _eventAggregator.Publish(new UserAuthenticationChangedEvent(_currentUser, true));

        System.Diagnostics.Debug.WriteLine($"AuthenticationService: User '{_currentUser.Username}' logged in successfully");
    }

    /// <summary>
    /// Sets an offline session for a cached user.
    /// </summary>
    private void SetOfflineSession(CachedUserData cachedUser)
    {
        _currentUser = AuthenticatedUser.FromCachedCredentials(cachedUser);
        _accessToken = "offline-session";
        _refreshToken = string.Empty;
        _tokenExpiry = DateTime.UtcNow.AddHours(OfflineSessionHours);

        // Publish events
        AuthenticationChanged?.Invoke(this, _currentUser);
        _eventAggregator.Publish(new UserAuthenticationChangedEvent(_currentUser, true));

        System.Diagnostics.Debug.WriteLine($"AuthenticationService: User '{_currentUser.Username}' logged in offline");
    }

    /// <summary>
    /// Attempts offline authentication using cached credentials.
    /// </summary>
    private AuthenticationResult TryOfflineAuthentication(string username, string credential, bool isPin)
    {
        try
        {
            var cachedUsers = GetCachedCredentials();
            var cachedUser = cachedUsers.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.IsActive &&
                u.ExpiresAt > DateTime.UtcNow);

            if (cachedUser == null)
            {
                System.Diagnostics.Debug.WriteLine($"AuthenticationService: No valid cached credentials for user '{username}'");
                return new AuthenticationResult(false, Error: "No cached credentials available for offline login");
            }

            // Verify the credential
            bool credentialValid;
            if (isPin)
            {
                credentialValid = VerifyHash(credential, cachedUser.PinHash);
            }
            else
            {
                credentialValid = VerifyHash(credential, cachedUser.PasswordHash);
            }

            if (!credentialValid)
            {
                System.Diagnostics.Debug.WriteLine($"AuthenticationService: Invalid {(isPin ? "PIN" : "password")} for offline user '{username}'");
                return new AuthenticationResult(false, Error: isPin ? "Invalid PIN" : "Invalid password");
            }

            SetOfflineSession(cachedUser);
            return new AuthenticationResult(true, _currentUser);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.TryOfflineAuthentication error: {ex.Message}");
            return new AuthenticationResult(false, Error: "Offline authentication failed");
        }
    }

    /// <summary>
    /// Caches user credentials for offline authentication.
    /// </summary>
    private async Task CacheCredentialsAsync(string username, string? password, string? pin)
    {
        if (_currentUser == null) return;

        try
        {
            var cachedUsers = GetCachedCredentials();

            // Find or create cached user entry
            var existingIndex = cachedUsers.FindIndex(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            var cachedUser = existingIndex >= 0 ? cachedUsers[existingIndex] : new CachedUserData();

            // Update user data from current session
            cachedUser.UserId = _currentUser.UserId;
            cachedUser.Username = _currentUser.Username;
            cachedUser.Email = _currentUser.Email;
            cachedUser.FirstName = _currentUser.FirstName;
            cachedUser.LastName = _currentUser.LastName;
            cachedUser.JobTitle = _currentUser.JobTitle;
            cachedUser.Department = _currentUser.Department;
            cachedUser.IsActive = _currentUser.IsActive;
            cachedUser.IsSuperAdmin = _currentUser.IsSuperAdmin;
            cachedUser.Roles = _currentUser.Roles.ToList();
            cachedUser.Permissions = _currentUser.Permissions.ToList();
            cachedUser.DefaultLocationId = _currentUser.DefaultLocationId;
            cachedUser.CachedAt = DateTime.UtcNow;
            cachedUser.ExpiresAt = DateTime.UtcNow.AddDays(CachedCredentialsDays);

            // Hash and store credentials
            if (!string.IsNullOrEmpty(password))
            {
                cachedUser.PasswordHash = ComputeHash(password);
            }
            if (!string.IsNullOrEmpty(pin))
            {
                cachedUser.PinHash = ComputeHash(pin);
            }

            // Update the list
            if (existingIndex >= 0)
            {
                cachedUsers[existingIndex] = cachedUser;
            }
            else
            {
                cachedUsers.Add(cachedUser);
            }

            // Store securely
            StoreCachedCredentials(cachedUsers);

            System.Diagnostics.Debug.WriteLine($"AuthenticationService: Cached credentials for user '{username}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.CacheCredentialsAsync error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets cached credentials from secure storage.
    /// </summary>
    private List<CachedUserData> GetCachedCredentials()
    {
        try
        {
            var encrypted = _settingsService.Get<string?>(SettingsKeyCachedCredentials, null);
            if (string.IsNullOrEmpty(encrypted))
            {
                return new List<CachedUserData>();
            }

            var decrypted = DecryptData(encrypted);
            return JsonSerializer.Deserialize<List<CachedUserData>>(decrypted) ?? new List<CachedUserData>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.GetCachedCredentials error: {ex.Message}");
            return new List<CachedUserData>();
        }
    }

    /// <summary>
    /// Stores cached credentials securely.
    /// </summary>
    private void StoreCachedCredentials(List<CachedUserData> credentials)
    {
        try
        {
            var json = JsonSerializer.Serialize(credentials);
            var encrypted = EncryptData(json);
            _settingsService.Set(SettingsKeyCachedCredentials, encrypted);
            _settingsService.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.StoreCachedCredentials error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stores tokens for session restoration.
    /// </summary>
    private void StoreTokens()
    {
        try
        {
            var tokenData = new StoredTokenData
            {
                AccessToken = _accessToken,
                RefreshToken = _refreshToken,
                TokenExpiry = _tokenExpiry,
                UserId = _currentUser?.UserId ?? Guid.Empty
            };

            var json = JsonSerializer.Serialize(tokenData);
            var encrypted = EncryptData(json);
            _settingsService.Set(SettingsKeyStoredTokens, encrypted);
            _settingsService.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.StoreTokens error: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to restore session from stored tokens.
    /// </summary>
    private void TryRestoreSession()
    {
        try
        {
            var encrypted = _settingsService.Get<string?>(SettingsKeyStoredTokens, null);
            if (string.IsNullOrEmpty(encrypted))
            {
                return;
            }

            var decrypted = DecryptData(encrypted);
            var tokenData = JsonSerializer.Deserialize<StoredTokenData>(decrypted);

            if (tokenData == null || tokenData.TokenExpiry < DateTime.UtcNow)
            {
                // Tokens expired, clear them
                _settingsService.Remove(SettingsKeyStoredTokens);
                _settingsService.Save();
                return;
            }

            // Restore token state
            _accessToken = tokenData.AccessToken;
            _refreshToken = tokenData.RefreshToken;
            _tokenExpiry = tokenData.TokenExpiry;

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            // Try to restore user from cached credentials
            var cachedUsers = GetCachedCredentials();
            var cachedUser = cachedUsers.FirstOrDefault(u => u.UserId == tokenData.UserId);

            if (cachedUser != null)
            {
                _currentUser = AuthenticatedUser.FromCachedCredentials(cachedUser);

                // Publish events
                AuthenticationChanged?.Invoke(this, _currentUser);
                _eventAggregator.Publish(new UserAuthenticationChangedEvent(_currentUser, true));

                System.Diagnostics.Debug.WriteLine($"AuthenticationService: Session restored for user '{_currentUser.Username}'");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService.TryRestoreSession error: {ex.Message}");
            // Clear corrupted tokens
            _settingsService.Remove(SettingsKeyStoredTokens);
            _settingsService.Save();
        }
    }

    /// <summary>
    /// Handles logout request events from other components.
    /// </summary>
    private void OnLogoutRequested(UserLogoutRequestedEvent evt)
    {
        System.Diagnostics.Debug.WriteLine("AuthenticationService: Logout requested via event");
        Logout();
    }

    #region Encryption Helpers

    /// <summary>
    /// Encrypts data using DPAPI (Windows Data Protection API).
    /// </summary>
    private static string EncryptData(string data)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            // If DPAPI fails, fall back to base64 encoding (less secure but works)
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }
    }

    /// <summary>
    /// Decrypts data using DPAPI (Windows Data Protection API).
    /// </summary>
    private static string DecryptData(string encrypted)
    {
        try
        {
            var bytes = Convert.FromBase64String(encrypted);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            // Try to decode as base64 if DPAPI fails
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Computes a SHA256 hash of the input (with salt for passwords).
    /// </summary>
    private static string ComputeHash(string input)
    {
        // Generate a random salt
        var saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        var salt = Convert.ToBase64String(saltBytes);
        var saltedInput = salt + input;

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
        var hash = Convert.ToBase64String(hashBytes);

        // Store salt and hash together
        return $"{salt}:{hash}";
    }

    /// <summary>
    /// Verifies an input against a stored hash.
    /// </summary>
    private static bool VerifyHash(string input, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        var salt = parts[0];
        var expectedHash = parts[1];
        var saltedInput = salt + input;

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
        var actualHash = Convert.ToBase64String(hashBytes);

        return actualHash == expectedHash;
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _eventAggregator.Unsubscribe<UserLogoutRequestedEvent>(OnLogoutRequested);
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Stored token data for session restoration.
/// </summary>
internal class StoredTokenData
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }
    public Guid UserId { get; set; }
}
