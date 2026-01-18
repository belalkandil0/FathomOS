using System.Text.RegularExpressions;
using FathomOS.Domain.Common;

namespace FathomOS.Domain.ValueObjects;

/// <summary>
/// Represents a unique certificate identifier.
/// Format: FOS-{LicenseeCode}-{YYMM}-{Sequence}-{CheckDigit}
/// Example: FOS-OCS-2601-0001-X
/// </summary>
public sealed partial class CertificateId : ValueObject
{
    /// <summary>
    /// The prefix for all certificate IDs.
    /// </summary>
    public const string Prefix = "FOS";

    /// <summary>
    /// Regular expression pattern for validating certificate IDs.
    /// </summary>
    private const string Pattern = @"^FOS-[A-Z]{3}-\d{4}-\d{4}-[A-Z0-9]$";

    /// <summary>
    /// Gets the full certificate ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the licensee code component.
    /// </summary>
    public string LicenseeCode { get; }

    /// <summary>
    /// Gets the year-month component (YYMM format).
    /// </summary>
    public string YearMonth { get; }

    /// <summary>
    /// Gets the sequence number component.
    /// </summary>
    public string Sequence { get; }

    /// <summary>
    /// Gets the check digit component.
    /// </summary>
    public string CheckDigit { get; }

    private CertificateId(string value, string licenseeCode, string yearMonth, string sequence, string checkDigit)
    {
        Value = value;
        LicenseeCode = licenseeCode;
        YearMonth = yearMonth;
        Sequence = sequence;
        CheckDigit = checkDigit;
    }

    /// <summary>
    /// Creates a new CertificateId from components.
    /// </summary>
    /// <param name="licenseeCode">The three-letter licensee code</param>
    /// <param name="yearMonth">The year-month in YYMM format</param>
    /// <param name="sequence">The sequence number (0001-9999)</param>
    /// <param name="checkDigit">The check digit</param>
    /// <returns>A Result containing the CertificateId or an error</returns>
    public static Result<CertificateId> Create(string licenseeCode, string yearMonth, string sequence, string checkDigit)
    {
        var value = $"{Prefix}-{licenseeCode}-{yearMonth}-{sequence}-{checkDigit}";
        return Parse(value);
    }

    /// <summary>
    /// Creates a new CertificateId by generating a new sequence number.
    /// </summary>
    /// <param name="licenseeCode">The licensee code value object</param>
    /// <param name="sequenceNumber">The sequence number (will be zero-padded to 4 digits)</param>
    /// <returns>A Result containing the CertificateId or an error</returns>
    public static Result<CertificateId> Generate(LicenseeCode licenseeCode, int sequenceNumber)
    {
        var now = DateTime.UtcNow;
        var yearMonth = now.ToString("yyMM");
        var sequence = sequenceNumber.ToString("D4");

        if (sequenceNumber < 1 || sequenceNumber > 9999)
        {
            return Result<CertificateId>.Failure(
                Error.Validation("CERTIFICATE_SEQUENCE_RANGE",
                    "Sequence number must be between 1 and 9999."));
        }

        var checkDigit = CalculateCheckDigit(licenseeCode.Value, yearMonth, sequence);
        var value = $"{Prefix}-{licenseeCode.Value}-{yearMonth}-{sequence}-{checkDigit}";

        return Result<CertificateId>.Success(
            new CertificateId(value, licenseeCode.Value, yearMonth, sequence, checkDigit));
    }

    /// <summary>
    /// Parses a certificate ID string into a CertificateId value object.
    /// </summary>
    /// <param name="value">The certificate ID string to parse</param>
    /// <returns>A Result containing the CertificateId or an error</returns>
    public static Result<CertificateId> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CertificateId>.Failure(
                Error.Validation("CERTIFICATE_ID_EMPTY", "Certificate ID cannot be empty."));
        }

        var trimmed = value.Trim().ToUpperInvariant();

        if (!CertificateIdRegex().IsMatch(trimmed))
        {
            return Result<CertificateId>.Failure(
                Error.Validation("CERTIFICATE_ID_FORMAT",
                    $"Certificate ID must match format FOS-XXX-YYMM-NNNN-C. Got: {trimmed}"));
        }

        var parts = trimmed.Split('-');
        var licenseeCode = parts[1];
        var yearMonth = parts[2];
        var sequence = parts[3];
        var checkDigit = parts[4];

        // Validate check digit
        var expectedCheckDigit = CalculateCheckDigit(licenseeCode, yearMonth, sequence);
        if (checkDigit != expectedCheckDigit)
        {
            return Result<CertificateId>.Failure(
                Error.Validation("CERTIFICATE_ID_CHECKSUM",
                    $"Certificate ID has invalid check digit. Expected: {expectedCheckDigit}, Got: {checkDigit}"));
        }

        return Result<CertificateId>.Success(
            new CertificateId(trimmed, licenseeCode, yearMonth, sequence, checkDigit));
    }

    /// <summary>
    /// Calculates the check digit for a certificate ID.
    /// Uses a simple modulo-36 checksum algorithm.
    /// </summary>
    private static string CalculateCheckDigit(string licenseeCode, string yearMonth, string sequence)
    {
        var input = $"{licenseeCode}{yearMonth}{sequence}";
        var sum = 0;
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            var value = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
            sum += value * (i + 1);
        }

        var checkValue = sum % 36;
        return checkValue < 10 ? checkValue.ToString() : ((char)('A' + checkValue - 10)).ToString();
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
    public static implicit operator string(CertificateId id) => id.Value;

    [GeneratedRegex(Pattern)]
    private static partial Regex CertificateIdRegex();
}
