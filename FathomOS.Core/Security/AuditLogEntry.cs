// FathomOS.Core/Security/AuditLogEntry.cs
// SECURITY FIX: MISSING-006 - Audit Log Entry Model
// Represents a single entry in the security audit log with HMAC signature

using System.Text.Json;
using System.Text.Json.Serialization;

namespace FathomOS.Core.Security;

/// <summary>
/// Represents a single entry in the security audit log.
/// Each entry includes an HMAC signature for tamper detection.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for this log entry.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the timestamp when the event occurred (UTC).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the type of security event.
    /// </summary>
    [JsonPropertyName("eventType")]
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the string representation of the event type for readability.
    /// </summary>
    [JsonPropertyName("eventTypeName")]
    public string EventTypeName => EventType.ToString();

    /// <summary>
    /// Gets or sets the numeric code of the event type for filtering.
    /// </summary>
    [JsonPropertyName("eventCode")]
    public int EventCode => (int)EventType;

    /// <summary>
    /// Gets or sets a description of the event.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the event represents a successful operation.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the username associated with the event, if applicable.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the machine name where the event occurred.
    /// </summary>
    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = Environment.MachineName;

    /// <summary>
    /// Gets or sets the machine ID (derived from hardware identifiers).
    /// </summary>
    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the process ID of the application.
    /// </summary>
    [JsonPropertyName("processId")]
    public int ProcessId { get; set; } = Environment.ProcessId;

    /// <summary>
    /// Gets or sets the Windows session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public int SessionId { get; set; }

    /// <summary>
    /// Gets or sets the source module or component that generated the event.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets additional details about the event as key-value pairs.
    /// </summary>
    [JsonPropertyName("details")]
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Gets or sets the HMAC-SHA256 signature of the entry for tamper detection.
    /// This is computed over all other fields and set when the entry is written.
    /// </summary>
    [JsonPropertyName("hmac")]
    public string? Hmac { get; set; }

    /// <summary>
    /// Gets or sets the sequence number within the log file.
    /// Used to detect missing or inserted entries.
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the HMAC of the previous entry (chain validation).
    /// Used to detect tampering with log order.
    /// </summary>
    [JsonPropertyName("previousHmac")]
    public string? PreviousHmac { get; set; }

    /// <summary>
    /// Gets or sets the application version that generated this entry.
    /// </summary>
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = AppInfo.VersionString;

    /// <summary>
    /// Creates a deep copy of this entry without the HMAC signature.
    /// Used for computing the HMAC over entry content.
    /// </summary>
    /// <returns>A copy of the entry with Hmac set to null.</returns>
    public AuditLogEntry CloneForSigning()
    {
        return new AuditLogEntry
        {
            Id = Id,
            Timestamp = Timestamp,
            EventType = EventType,
            Description = Description,
            Success = Success,
            Username = Username,
            MachineName = MachineName,
            MachineId = MachineId,
            ProcessId = ProcessId,
            SessionId = SessionId,
            Source = Source,
            Details = new Dictionary<string, object>(Details),
            Hmac = null, // Exclude HMAC when computing signature
            SequenceNumber = SequenceNumber,
            PreviousHmac = PreviousHmac,
            AppVersion = AppVersion
        };
    }

    /// <summary>
    /// Serializes the entry to JSON for signing or storage.
    /// </summary>
    /// <param name="includeHmac">Whether to include the HMAC field in output.</param>
    /// <returns>JSON string representation of the entry.</returns>
    public string ToJson(bool includeHmac = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (includeHmac)
        {
            return JsonSerializer.Serialize(this, options);
        }

        var clone = CloneForSigning();
        return JsonSerializer.Serialize(clone, options);
    }

    /// <summary>
    /// Serializes the entry to JSON with pretty formatting for display.
    /// </summary>
    /// <returns>Indented JSON string representation.</returns>
    public string ToJsonPretty()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Deserializes an entry from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized entry, or null if deserialization fails.</returns>
    public static AuditLogEntry? FromJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<AuditLogEntry>(json, options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a summary string for logging/debugging purposes.
    /// </summary>
    /// <returns>A human-readable summary of the entry.</returns>
    public override string ToString()
    {
        var status = Success ? "SUCCESS" : "FAILURE";
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{EventTypeName}] [{status}] {Description}";
    }
}

/// <summary>
/// Result of log integrity verification.
/// </summary>
public sealed class LogVerificationResult
{
    /// <summary>
    /// Gets or sets the path to the verified log file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether all entries passed verification.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the total number of entries in the log file.
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of entries that passed verification.
    /// </summary>
    public int ValidEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of entries that failed verification.
    /// </summary>
    public int InvalidEntries { get; set; }

    /// <summary>
    /// Gets or sets the list of invalid entry IDs and their failure reasons.
    /// </summary>
    public List<(string EntryId, string Reason)> Failures { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when verification was performed.
    /// </summary>
    public DateTime VerificationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any error message if verification could not be completed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
