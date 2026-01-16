using System.ComponentModel.DataAnnotations;

namespace S7Fathom.Api.DTOs;

// ============ Authentication DTOs ============

public record LoginRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;
    
    [Required]
    public string Password { get; init; } = string.Empty;
}

public record PinLoginRequest
{
    [Required]
    public string DeviceId { get; init; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string Pin { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public UserDto User { get; init; } = null!;
}

public record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}

public record SetPinRequest
{
    public string? DeviceId { get; init; }  // Optional - defaults to header value
    
    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string Pin { get; init; } = string.Empty;
}

// ============ User DTOs ============

public record UserDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public Guid? DefaultLocationId { get; init; }
    public string? DefaultLocationName { get; init; }
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
}

public record CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; init; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
    
    [MaxLength(100)]
    public string? FirstName { get; init; }
    
    [MaxLength(100)]
    public string? LastName { get; init; }
    
    [Phone]
    public string? Phone { get; init; }
    
    public Guid? DefaultLocationId { get; init; }
    
    public List<Guid> RoleIds { get; init; } = new();
}

public record UpdateUserRequest
{
    [MaxLength(100)]
    public string? FirstName { get; init; }
    
    [MaxLength(100)]
    public string? LastName { get; init; }
    
    [Phone]
    public string? Phone { get; init; }
    
    public Guid? DefaultLocationId { get; init; }
    
    public List<Guid>? RoleIds { get; init; }
}
