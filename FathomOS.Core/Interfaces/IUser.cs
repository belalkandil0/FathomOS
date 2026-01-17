namespace FathomOS.Core.Interfaces;

/// <summary>
/// Represents an authenticated user in the FathomOS system.
/// Provides access to user identity, profile information, and authorization data.
/// This interface is the central contract for user information across all modules.
/// </summary>
public interface IUser
{
    /// <summary>
    /// Unique identifier for the user (from backend system).
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// The user's login username.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    string Email { get; }

    /// <summary>
    /// The user's first name (optional).
    /// </summary>
    string? FirstName { get; }

    /// <summary>
    /// The user's last name (optional).
    /// </summary>
    string? LastName { get; }

    /// <summary>
    /// Display name for UI (e.g., "John Smith" or username if name not set).
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The user's job title (optional).
    /// </summary>
    string? JobTitle { get; }

    /// <summary>
    /// The user's department (optional).
    /// </summary>
    string? Department { get; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Whether the user has super admin privileges.
    /// Super admins have unrestricted access to all features.
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// The roles assigned to this user (e.g., "Admin", "Surveyor", "Viewer").
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// The specific permissions granted to this user.
    /// Permissions are fine-grained access controls (e.g., "certificates.create", "reports.export").
    /// </summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// The user's default location ID for multi-location deployments (optional).
    /// </summary>
    Guid? DefaultLocationId { get; }
}
