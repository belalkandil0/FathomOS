using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FathomOS.Modules.EquipmentInventory.Api.DTOs;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Api;

/// <summary>
/// HTTP client for Fathom OS Equipment API.
/// Handles authentication, token refresh, and all API operations.
/// </summary>
public class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly string _deviceId;
    
    public event EventHandler? TokenExpired;
    public event EventHandler<string>? ApiError;
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;
    public string? CurrentUsername { get; private set; }
    public UserDto? CurrentUser { get; private set; }
    
    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        // Generate unique device ID for this installation
        _deviceId = $"desktop-{Environment.MachineName}-{GetMachineGuid()}";
        
        _httpClient.DefaultRequestHeaders.Add("X-Device-Id", _deviceId);
        _httpClient.DefaultRequestHeaders.Add("X-App-Version", "1.0.0");
    }
    
    private static string GetMachineGuid()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid")?.ToString() ?? Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }
    
    public void SetBaseUrl(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }
    
    #region Authentication
    
    /// <summary>
    /// Login with username and password
    /// </summary>
    public async Task<(bool Success, string? Error)> LoginAsync(string username, string password)
    {
        try
        {
            var request = new LoginRequest { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("auth/login", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                if (result != null)
                {
                    SetTokens(result.AccessToken, result.RefreshToken, result.ExpiresIn);
                    CurrentUser = result.User;
                    CurrentUsername = username;
                    return (true, null);
                }
            }
            
            var error = await response.Content.ReadAsStringAsync();
            return (false, $"Login failed: {error}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Refresh the access token
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
            return false;
            
        try
        {
            var request = new RefreshTokenRequest { RefreshToken = _refreshToken };
            var response = await _httpClient.PostAsJsonAsync("auth/refresh", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                if (result != null)
                {
                    SetTokens(result.AccessToken, result.RefreshToken, result.ExpiresIn);
                    return true;
                }
            }
            
            // Refresh failed - need to re-login
            TokenExpired?.Invoke(this, EventArgs.Empty);
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Logout and revoke tokens
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await SendAuthenticatedAsync(HttpMethod.Post, "auth/logout", null);
        }
        catch { }
        finally
        {
            ClearTokens();
        }
    }
    
    private void SetTokens(string accessToken, string refreshToken, int expiresIn)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Refresh 1 minute before expiry
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);
    }
    
    private void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;
        _tokenExpiry = DateTime.MinValue;
        CurrentUser = null;
        CurrentUsername = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
    
    /// <summary>
    /// Ensure token is valid, refresh if needed
    /// </summary>
    private async Task EnsureAuthenticatedAsync()
    {
        if (DateTime.UtcNow >= _tokenExpiry && !string.IsNullOrEmpty(_refreshToken))
        {
            await RefreshTokenAsync();
        }
        
        if (!IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }
    }
    
    #endregion
    
    #region Equipment API
    
    public async Task<PagedResult<EquipmentDto>?> GetEquipmentAsync(
        string? search = null, 
        Guid? locationId = null,
        Guid? categoryId = null,
        int page = 1, 
        int pageSize = 20)
    {
        await EnsureAuthenticatedAsync();
        
        var query = $"equipment?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) query += $"&search={Uri.EscapeDataString(search)}";
        if (locationId.HasValue) query += $"&locationId={locationId}";
        if (categoryId.HasValue) query += $"&categoryId={categoryId}";
        
        return await GetAsync<PagedResult<EquipmentDto>>(query);
    }
    
    public async Task<EquipmentDto?> GetEquipmentByIdAsync(Guid id)
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<EquipmentDto>($"equipment/{id}");
    }
    
    public async Task<EquipmentDto?> GetEquipmentByQrCodeAsync(string qrCode)
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<EquipmentDto>($"equipment/qr/{Uri.EscapeDataString(qrCode)}");
    }
    
    public async Task<EquipmentDto?> GetEquipmentByAssetNumberAsync(string assetNumber)
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<EquipmentDto>($"equipment/asset/{Uri.EscapeDataString(assetNumber)}");
    }
    
    /// <summary>
    /// Get equipment by unique ID (e.g., S7WSS04068)
    /// </summary>
    public async Task<EquipmentDto?> GetEquipmentByUniqueIdAsync(string uniqueId)
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<EquipmentDto>($"equipment/unique/{Uri.EscapeDataString(uniqueId)}");
    }
    
    public async Task<EquipmentDto?> CreateEquipmentAsync(CreateEquipmentRequest request)
    {
        await EnsureAuthenticatedAsync();
        return await PostAsync<CreateEquipmentRequest, EquipmentDto>("equipment", request);
    }
    
    public async Task<EquipmentDto?> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequest request)
    {
        await EnsureAuthenticatedAsync();
        return await PutAsync<UpdateEquipmentRequest, EquipmentDto>($"equipment/{id}", request);
    }
    
    public async Task<bool> DeleteEquipmentAsync(Guid id)
    {
        await EnsureAuthenticatedAsync();
        return await DeleteAsync($"equipment/{id}");
    }
    
    #endregion
    
    #region Manifest API
    
    public async Task<PagedResult<ManifestDto>?> GetManifestsAsync(
        string? status = null,
        string? type = null,
        int page = 1,
        int pageSize = 20)
    {
        await EnsureAuthenticatedAsync();
        
        var query = $"manifests?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) query += $"&status={status}";
        if (!string.IsNullOrEmpty(type)) query += $"&type={type}";
        
        return await GetAsync<PagedResult<ManifestDto>>(query);
    }
    
    public async Task<ManifestDto?> GetManifestByIdAsync(Guid id)
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<ManifestDto>($"manifests/{id}");
    }
    
    public async Task<ManifestDto?> GetManifestByQrCodeAsync(string qrCode)
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<ManifestDto>($"manifests/qr/{Uri.EscapeDataString(qrCode)}");
    }
    
    public async Task<ManifestDto?> CreateManifestAsync(CreateManifestRequest request)
    {
        await EnsureAuthenticatedAsync();
        return await PostAsync<CreateManifestRequest, ManifestDto>("manifests", request);
    }
    
    public async Task<bool> AddManifestItemsAsync(Guid manifestId, AddManifestItemsRequest request)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"manifests/{manifestId}/items", request);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> SubmitManifestAsync(Guid manifestId)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"manifests/{manifestId}/submit", null);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> ApproveManifestAsync(Guid manifestId)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"manifests/{manifestId}/approve", null);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> RejectManifestAsync(Guid manifestId, RejectManifestRequest request)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"manifests/{manifestId}/reject", request);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> ShipManifestAsync(Guid manifestId, ShipManifestRequest request)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"manifests/{manifestId}/ship", request);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> ReceiveManifestAsync(Guid manifestId, ReceiveManifestRequest request)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"manifests/{manifestId}/receive", request);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> AddSignatureAsync(Guid manifestId, AddSignatureRequest request)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"manifests/{manifestId}/sign", request);
        return response.IsSuccessStatusCode;
    }
    
    #endregion
    
    #region Sync API
    
    public async Task<SyncPullResponse?> PullChangesAsync(long lastSyncVersion)
    {
        await EnsureAuthenticatedAsync();
        
        var request = new SyncPullRequest
        {
            DeviceId = _deviceId,
            LastSyncVersion = lastSyncVersion
        };
        
        return await PostAsync<SyncPullRequest, SyncPullResponse>("sync/pull", request);
    }
    
    public async Task<SyncPushResponse?> PushChangesAsync(List<SyncPushChange> changes)
    {
        await EnsureAuthenticatedAsync();
        
        var request = new SyncPushRequest
        {
            DeviceId = _deviceId,
            Changes = changes
        };
        
        return await PostAsync<SyncPushRequest, SyncPushResponse>("sync/push", request);
    }
    
    public async Task<SyncStatusResponse?> GetSyncStatusAsync()
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<SyncStatusResponse>("sync/status");
    }
    
    public async Task<List<SyncConflictDto>?> GetConflictsAsync()
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<List<SyncConflictDto>>("sync/conflicts");
    }
    
    public async Task<bool> ResolveConflictAsync(Guid conflictId, ResolveConflictRequest request)
    {
        await EnsureAuthenticatedAsync();
        var response = await SendAuthenticatedAsync(HttpMethod.Post, $"sync/conflicts/{conflictId}/resolve", request);
        return response.IsSuccessStatusCode;
    }
    
    #endregion
    
    #region Lookups API
    
    public async Task<List<LocationDto>?> GetLocationsAsync()
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<List<LocationDto>>("lookups/locations");
    }
    
    public async Task<List<CategoryDto>?> GetCategoriesAsync()
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<List<CategoryDto>>("lookups/categories");
    }
    
    public async Task<List<EquipmentTypeDto>?> GetTypesAsync(Guid? categoryId = null)
    {
        await EnsureAuthenticatedAsync();
        var query = categoryId.HasValue ? $"lookups/types?categoryId={categoryId}" : "lookups/types";
        return await GetAsync<List<EquipmentTypeDto>>(query);
    }
    
    public async Task<List<ProjectDto>?> GetProjectsAsync()
    {
        await EnsureAuthenticatedAsync();
        return await GetAsync<List<ProjectDto>>("lookups/projects");
    }
    
    #endregion
    
    #region HTTP Helpers
    
    private async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }
            
            HandleError(response);
            return default;
        }
        catch (Exception ex)
        {
            ApiError?.Invoke(this, ex.Message);
            return default;
        }
    }
    
    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }
            
            HandleError(response);
            return default;
        }
        catch (Exception ex)
        {
            ApiError?.Invoke(this, ex.Message);
            return default;
        }
    }
    
    private async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }
            
            HandleError(response);
            return default;
        }
        catch (Exception ex)
        {
            ApiError?.Invoke(this, ex.Message);
            return default;
        }
    }
    
    private async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                HandleError(response);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            ApiError?.Invoke(this, ex.Message);
            return false;
        }
    }
    
    private async Task<HttpResponseMessage> SendAuthenticatedAsync(HttpMethod method, string endpoint, object? content)
    {
        var request = new HttpRequestMessage(method, endpoint);
        
        if (content != null)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        return await _httpClient.SendAsync(request);
    }
    
    private void HandleError(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            TokenExpired?.Invoke(this, EventArgs.Empty);
        }
        
        ApiError?.Invoke(this, $"API Error: {response.StatusCode}");
    }
    
    #endregion
    
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
