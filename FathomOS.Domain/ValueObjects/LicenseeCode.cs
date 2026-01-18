using FathomOS.Domain.Common;

namespace FathomOS.Domain.ValueObjects;

/// <summary>
/// Represents a three-letter licensee code used in certificate identification.
/// Example: "OCS", "ABC"
/// </summary>
public sealed class LicenseeCode : ValueObject
{
    /// <summary>
    /// The expected length of a licensee code.
    /// </summary>
    public const int RequiredLength = 3;

    /// <summary>
    /// Gets the three-letter licensee code value.
    /// </summary>
    public string Value { get; }

    private LicenseeCode(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new LicenseeCode from a string value.
    /// </summary>
    /// <param name="value">The three-letter code</param>
    /// <returns>A Result containing the LicenseeCode or an error</returns>
    public static Result<LicenseeCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<LicenseeCode>.Failure(
                Error.Validation("LICENSEE_CODE_EMPTY", "Licensee code cannot be empty."));
        }

        var trimmed = value.Trim().ToUpperInvariant();

        if (trimmed.Length != RequiredLength)
        {
            return Result<LicenseeCode>.Failure(
                Error.Validation("LICENSEE_CODE_LENGTH",
                    $"Licensee code must be exactly {RequiredLength} characters. Got: {trimmed.Length}"));
        }

        if (!trimmed.All(char.IsLetter))
        {
            return Result<LicenseeCode>.Failure(
                Error.Validation("LICENSEE_CODE_FORMAT",
                    "Licensee code must contain only letters."));
        }

        return Result<LicenseeCode>.Success(new LicenseeCode(trimmed));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    public static implicit operator string(LicenseeCode code) => code.Value;
}
