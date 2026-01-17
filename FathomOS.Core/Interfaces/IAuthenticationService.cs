namespace FathomOS.Core.Interfaces;

/// <summary>
/// Result of an authentication attempt.
/// </summary>
/// <param name="Success">Whether authentication was successful</param>
/// <param name="User">The authenticated user if successful, null otherwise</param>
/// <param name="Error">Error message if authentication failed, null otherwise</param>
public record AuthenticationResult(bool Success, IUser? User = null, string? Error = null);

/// <summary>
/// Contract for centralized authentication services in FathomOS.
/// Provides login, logout, token management, and authorization checking.
/// Implemented by Shell, consumed by all modules requiring authentication.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Gets the currently authenticated user, or null if not logged in.
    /// </summary>
    IUser? CurrentUser { get; }

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current access token for API calls.
    /// Returns empty string if not authenticated.
    /// </summary>
    string AccessToken { get; }

    /// <summary>
    /// Event fired when authentication state changes (login/logout).
    /// Subscribers should update their UI and state accordingly.
    /// </summary>
    event EventHandler<IUser?>? AuthenticationChanged;

    /// <summary>
    /// Authenticate a user with username and password.
    /// </summary>
    /// <param name="username">The username to authenticate</param>
    /// <param name="password">The password to authenticate</param>
    /// <returns>Authentication result with user info or error</returns>
    Task<AuthenticationResult> LoginAsync(string username, string password);

    /// <summary>
    /// Authenticate a user with username and PIN (quick login).
    /// </summary>
    /// <param name="username">The username to authenticate</param>
    /// <param name="pin">The PIN to authenticate</param>
    /// <returns>Authentication result with user info or error</returns>
    Task<AuthenticationResult> LoginWithPinAsync(string username, string pin);

    /// <summary>
    /// Log out the current user and clear all authentication state.
    /// </summary>
    void Logout();

    /// <summary>
    /// Attempt to refresh the current access token.
    /// </summary>
    /// <returns>True if token was refreshed successfully, false otherwise</returns>
    Task<bool> RefreshTokenAsync();

    /// <summary>
    /// Check if the current user has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check (e.g., "certificates.create")</param>
    /// <returns>True if user has the permission, false otherwise</returns>
    bool HasPermission(string permission);

    /// <summary>
    /// Check if the current user has any of the specified roles.
    /// </summary>
    /// <param name="roles">The roles to check</param>
    /// <returns>True if user has at least one of the roles, false otherwise</returns>
    bool HasRole(params string[] roles);

    /// <summary>
    /// Show the login dialog and wait for authentication.
    /// </summary>
    /// <param name="owner">Optional parent window for modal behavior</param>
    /// <returns>True if user successfully authenticated, false if cancelled</returns>
    Task<bool> ShowLoginDialogAsync(object? owner = null);
}
