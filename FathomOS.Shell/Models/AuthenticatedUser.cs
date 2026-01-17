using FathomOS.Core.Interfaces;

namespace FathomOS.Shell.Models;

/// <summary>
/// Concrete implementation of IUser representing an authenticated user in FathomOS.
/// Used by the Shell's AuthenticationService to store user data after login.
/// </summary>
public class AuthenticatedUser : IUser
{
    /// <inheritdoc />
    public Guid UserId { get; init; }

    /// <inheritdoc />
    public string Username { get; init; } = string.Empty;

    /// <inheritdoc />
    public string Email { get; init; } = string.Empty;

    /// <inheritdoc />
    public string? FirstName { get; init; }

    /// <inheritdoc />
    public string? LastName { get; init; }

    /// <inheritdoc />
    public string DisplayName => GetDisplayName();

    /// <inheritdoc />
    public string? JobTitle { get; init; }

    /// <inheritdoc />
    public string? Department { get; init; }

    /// <inheritdoc />
    public bool IsActive { get; init; } = true;

    /// <inheritdoc />
    public bool IsSuperAdmin { get; init; }

    /// <inheritdoc />
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <inheritdoc />
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    /// <inheritdoc />
    public Guid? DefaultLocationId { get; init; }

    /// <summary>
    /// Gets the display name, preferring "FirstName LastName" if available, otherwise Username.
    /// </summary>
    private string GetDisplayName()
    {
        var fullName = $"{FirstName} {LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? Username : fullName;
    }

    /// <summary>
    /// Creates a new AuthenticatedUser from a login API response.
    /// </summary>
    public static AuthenticatedUser FromLoginResponse(LoginUserResponse response)
    {
        return new AuthenticatedUser
        {
            UserId = response.UserId,
            Username = response.Username,
            Email = response.Email,
            FirstName = response.FirstName,
            LastName = response.LastName,
            JobTitle = response.JobTitle,
            Department = response.Department,
            IsActive = response.IsActive,
            IsSuperAdmin = response.IsSuperAdmin,
            Roles = response.Roles?.ToList() ?? new List<string>(),
            Permissions = response.Permissions?.ToList() ?? new List<string>(),
            DefaultLocationId = response.DefaultLocationId
        };
    }

    /// <summary>
    /// Creates a new AuthenticatedUser from cached credential data.
    /// </summary>
    public static AuthenticatedUser FromCachedCredentials(CachedUserData cached)
    {
        return new AuthenticatedUser
        {
            UserId = cached.UserId,
            Username = cached.Username,
            Email = cached.Email,
            FirstName = cached.FirstName,
            LastName = cached.LastName,
            JobTitle = cached.JobTitle,
            Department = cached.Department,
            IsActive = cached.IsActive,
            IsSuperAdmin = cached.IsSuperAdmin,
            Roles = cached.Roles?.ToList() ?? new List<string>(),
            Permissions = cached.Permissions?.ToList() ?? new List<string>(),
            DefaultLocationId = cached.DefaultLocationId
        };
    }
}

/// <summary>
/// Represents the user data returned from a login API response.
/// Used for JSON deserialization of the login endpoint response.
/// </summary>
public class LoginUserResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; }
    public List<string>? Roles { get; set; }
    public List<string>? Permissions { get; set; }
    public Guid? DefaultLocationId { get; set; }
}

/// <summary>
/// Represents cached user data for offline authentication.
/// Stored securely using ProtectedData.
/// </summary>
public class CachedUserData
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; }
    public List<string>? Roles { get; set; }
    public List<string>? Permissions { get; set; }
    public Guid? DefaultLocationId { get; set; }

    /// <summary>
    /// Hashed PIN for offline authentication (BCrypt or similar hash).
    /// </summary>
    public string? PinHash { get; set; }

    /// <summary>
    /// Hashed password for offline authentication (BCrypt or similar hash).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// When the credentials were cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// When the cached credentials expire (for security).
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Represents the full login API response including tokens.
/// </summary>
public class LoginApiResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public LoginUserResponse? User { get; set; }
}
