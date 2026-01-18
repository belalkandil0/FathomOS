using FathomOS.Domain.Common;

namespace FathomOS.Domain.ValueObjects;

/// <summary>
/// Represents a semantic version for a module.
/// Follows SemVer 2.0.0 specification: Major.Minor.Patch[-Prerelease][+Build]
/// </summary>
public sealed class ModuleVersion : ValueObject, IComparable<ModuleVersion>
{
    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Gets the prerelease label (e.g., "alpha", "beta", "rc.1").
    /// Null if this is a release version.
    /// </summary>
    public string? Prerelease { get; }

    /// <summary>
    /// Gets the build metadata.
    /// </summary>
    public string? BuildMetadata { get; }

    /// <summary>
    /// Gets a value indicating whether this is a prerelease version.
    /// </summary>
    public bool IsPrerelease => !string.IsNullOrEmpty(Prerelease);

    private ModuleVersion(int major, int minor, int patch, string? prerelease = null, string? buildMetadata = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        BuildMetadata = buildMetadata;
    }

    /// <summary>
    /// Creates a new ModuleVersion from components.
    /// </summary>
    /// <param name="major">The major version number</param>
    /// <param name="minor">The minor version number</param>
    /// <param name="patch">The patch version number</param>
    /// <param name="prerelease">Optional prerelease label</param>
    /// <param name="buildMetadata">Optional build metadata</param>
    /// <returns>A Result containing the ModuleVersion or an error</returns>
    public static Result<ModuleVersion> Create(
        int major,
        int minor,
        int patch,
        string? prerelease = null,
        string? buildMetadata = null)
    {
        if (major < 0)
        {
            return Result<ModuleVersion>.Failure(
                Error.Validation("VERSION_MAJOR_NEGATIVE", "Major version cannot be negative."));
        }

        if (minor < 0)
        {
            return Result<ModuleVersion>.Failure(
                Error.Validation("VERSION_MINOR_NEGATIVE", "Minor version cannot be negative."));
        }

        if (patch < 0)
        {
            return Result<ModuleVersion>.Failure(
                Error.Validation("VERSION_PATCH_NEGATIVE", "Patch version cannot be negative."));
        }

        return Result<ModuleVersion>.Success(
            new ModuleVersion(major, minor, patch, prerelease, buildMetadata));
    }

    /// <summary>
    /// Parses a version string into a ModuleVersion.
    /// </summary>
    /// <param name="value">The version string (e.g., "1.2.3", "1.0.0-alpha", "2.0.0+build.123")</param>
    /// <returns>A Result containing the ModuleVersion or an error</returns>
    public static Result<ModuleVersion> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ModuleVersion>.Failure(
                Error.Validation("VERSION_EMPTY", "Version string cannot be empty."));
        }

        var trimmed = value.Trim();

        // Extract build metadata
        string? buildMetadata = null;
        var buildIndex = trimmed.IndexOf('+');
        if (buildIndex >= 0)
        {
            buildMetadata = trimmed[(buildIndex + 1)..];
            trimmed = trimmed[..buildIndex];
        }

        // Extract prerelease
        string? prerelease = null;
        var prereleaseIndex = trimmed.IndexOf('-');
        if (prereleaseIndex >= 0)
        {
            prerelease = trimmed[(prereleaseIndex + 1)..];
            trimmed = trimmed[..prereleaseIndex];
        }

        // Parse version numbers
        var parts = trimmed.Split('.');
        if (parts.Length < 1 || parts.Length > 3)
        {
            return Result<ModuleVersion>.Failure(
                Error.Validation("VERSION_FORMAT", "Version must have 1-3 numeric parts separated by dots."));
        }

        if (!int.TryParse(parts[0], out var major) || major < 0)
        {
            return Result<ModuleVersion>.Failure(
                Error.Validation("VERSION_MAJOR_INVALID", "Major version must be a non-negative integer."));
        }

        var minor = 0;
        if (parts.Length > 1)
        {
            if (!int.TryParse(parts[1], out minor) || minor < 0)
            {
                return Result<ModuleVersion>.Failure(
                    Error.Validation("VERSION_MINOR_INVALID", "Minor version must be a non-negative integer."));
            }
        }

        var patch = 0;
        if (parts.Length > 2)
        {
            if (!int.TryParse(parts[2], out patch) || patch < 0)
            {
                return Result<ModuleVersion>.Failure(
                    Error.Validation("VERSION_PATCH_INVALID", "Patch version must be a non-negative integer."));
            }
        }

        return Create(major, minor, patch, prerelease, buildMetadata);
    }

    /// <summary>
    /// Creates a ModuleVersion from a System.Version.
    /// </summary>
    /// <param name="version">The System.Version to convert</param>
    /// <returns>A Result containing the ModuleVersion</returns>
    public static Result<ModuleVersion> FromVersion(Version version)
    {
        return Create(
            version.Major,
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build));
    }

    /// <summary>
    /// Converts to a System.Version (loses prerelease and build metadata).
    /// </summary>
    /// <returns>A System.Version instance</returns>
    public Version ToVersion()
    {
        return new Version(Major, Minor, Patch);
    }

    /// <inheritdoc />
    public int CompareTo(ModuleVersion? other)
    {
        if (other is null) return 1;

        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;

        var minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0) return minorCompare;

        var patchCompare = Patch.CompareTo(other.Patch);
        if (patchCompare != 0) return patchCompare;

        // Prerelease versions have lower precedence than release versions
        if (IsPrerelease && !other.IsPrerelease) return -1;
        if (!IsPrerelease && other.IsPrerelease) return 1;

        // Compare prerelease labels lexically
        return string.Compare(Prerelease, other.Prerelease, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
        yield return Prerelease;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(Prerelease))
            version += $"-{Prerelease}";
        if (!string.IsNullOrEmpty(BuildMetadata))
            version += $"+{BuildMetadata}";
        return version;
    }

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    public static implicit operator string(ModuleVersion version) => version.ToString();

    public static bool operator <(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) >= 0;
}
