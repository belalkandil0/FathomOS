namespace FathomOS.Application.Common.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// Returns null if no user is authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the username of the current user.
    /// Returns null if no user is authenticated.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the roles assigned to the current user.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Checks if the current user has the specified role.
    /// </summary>
    /// <param name="role">The role to check</param>
    /// <returns>True if the user has the role; otherwise, false</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Gets the claims for the current user.
    /// </summary>
    IReadOnlyDictionary<string, string> Claims { get; }
}
