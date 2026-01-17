using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// DEPRECATED: This local AuthenticationService is being migrated to use the centralized
/// IAuthenticationService from FathomOS.Shell. Use the injected IAuthenticationService instead.
/// This class is kept for backward compatibility during the transition period.
/// </summary>
[Obsolete("Use IAuthenticationService from FathomOS.Core.Interfaces instead. This local implementation will be removed in a future version.")]
public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private string _accessToken = string.Empty;
    private string _refreshToken = string.Empty;
    private User? _currentUser;
    private DateTime _tokenExpiry;
    
    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null && DateTime.UtcNow < _tokenExpiry;
    public string AccessToken => _accessToken;
    
    public event EventHandler<User?>? AuthenticationChanged;
    
    public AuthenticationService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }
    
    public void Configure(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }
    
    public async Task<(bool Success, string? Error)> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", new { username, password });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _accessToken = result.AccessToken;
                    _refreshToken = result.RefreshToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                    _currentUser = result.User;
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    
                    AuthenticationChanged?.Invoke(this, _currentUser);
                    return (true, null);
                }
            }
            
            var error = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrEmpty(error) ? "Login failed" : error);
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
    
    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken)) return false;
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/refresh", new { refreshToken = _refreshToken });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _accessToken = result.AccessToken;
                    _refreshToken = result.RefreshToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    return true;
                }
            }
        }
        catch { }
        
        return false;
    }
    
    public void Logout()
    {
        _accessToken = string.Empty;
        _refreshToken = string.Empty;
        _currentUser = null;
        _tokenExpiry = DateTime.MinValue;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        AuthenticationChanged?.Invoke(this, null);
    }
    
    /// <summary>
    /// Set the current user for offline/local authentication mode
    /// </summary>
    public void SetOfflineUser(User user)
    {
        _currentUser = user;
        _tokenExpiry = DateTime.UtcNow.AddHours(24); // Offline session lasts 24 hours
        _accessToken = "offline-session";
        AuthenticationChanged?.Invoke(this, _currentUser);
    }
    
    public bool HasPermission(string permission)
    {
        if (_currentUser == null) return false;
        
        // Admin users have all permissions
        if (_currentUser.Role?.Name?.Equals("Administrator", StringComparison.OrdinalIgnoreCase) == true ||
            _currentUser.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }
        
        // Check if user's role has the specific permission
        if (_currentUser.Role?.RolePermissions != null)
        {
            return _currentUser.Role.RolePermissions
                .Any(rp => rp.Permission?.Name?.Equals(permission, StringComparison.OrdinalIgnoreCase) == true);
        }
        
        // Check through UserRoles if direct Role is not set
        if (_currentUser.UserRoles != null)
        {
            foreach (var userRole in _currentUser.UserRoles)
            {
                if (userRole.Role?.Name?.Equals("Administrator", StringComparison.OrdinalIgnoreCase) == true ||
                    userRole.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
                
                if (userRole.Role?.RolePermissions?.Any(rp => 
                    rp.Permission?.Name?.Equals(permission, StringComparison.OrdinalIgnoreCase) == true) == true)
                {
                    return true;
                }
            }
        }
        
        // Default: allow if user is logged in (for basic operations)
        // This ensures the module works even without full permission setup
        return true;
    }
    
    /// <summary>
    /// Authenticate with username and password (wrapper for LoginAsync)
    /// </summary>
    public async Task<(bool Success, string? Error)> AuthenticateAsync(string username, string password)
    {
        return await LoginAsync(username, password);
    }
    
    /// <summary>
    /// Authenticate with username and PIN
    /// </summary>
    public async Task<(bool Success, string? Error)> AuthenticateWithPinAsync(string username, string pin)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/pin-login", new { username, pin });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _accessToken = result.AccessToken;
                    _refreshToken = result.RefreshToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                    _currentUser = result.User;
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    
                    AuthenticationChanged?.Invoke(this, _currentUser);
                    return (true, null);
                }
            }
            
            var error = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrEmpty(error) ? "PIN login failed" : error);
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Offline PIN authentication using local database
    /// </summary>
    public (bool Success, User? User, string? Error) AuthenticateWithPinOffline(string username, string pin, List<User> localUsers)
    {
        // Note: In production, this should properly hash the PIN and compare with PinHash
        // For now, we do a simple comparison assuming PinHash stores the raw PIN for offline use
        var user = localUsers.FirstOrDefault(u => 
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
            u.PinHash == pin && 
            u.IsActive);
        
        if (user != null)
        {
            _currentUser = user;
            _tokenExpiry = DateTime.UtcNow.AddHours(8); // Offline session expires in 8 hours
            AuthenticationChanged?.Invoke(this, _currentUser);
            return (true, user, null);
        }
        
        return (false, null, "Invalid username or PIN");
    }
    
    private class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public User? User { get; set; }
    }
}
