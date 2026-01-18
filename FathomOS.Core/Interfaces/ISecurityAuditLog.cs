// FathomOS.Core/Interfaces/ISecurityAuditLog.cs
// SECURITY FIX: MISSING-006 - Security Audit Logging Interface
// Provides contract for security audit logging with tamper detection

namespace FathomOS.Core.Interfaces;

/// <summary>
/// Contract for security audit logging in FathomOS.
/// Provides tamper-resistant logging of security-relevant events including
/// license validation, authentication, certificate operations, and access control decisions.
/// </summary>
public interface ISecurityAuditLog : IDisposable
{
    /// <summary>
    /// Gets the directory where audit logs are stored.
    /// </summary>
    string LogDirectory { get; }

    /// <summary>
    /// Gets the path to the current active log file.
    /// </summary>
    string CurrentLogFilePath { get; }

    /// <summary>
    /// Logs a license validation attempt.
    /// </summary>
    /// <param name="licenseKey">The license key being validated (masked for security).</param>
    /// <param name="success">Whether validation succeeded.</param>
    /// <param name="reason">Reason for success or failure.</param>
    /// <param name="additionalDetails">Optional additional context.</param>
    void LogLicenseValidation(string licenseKey, bool success, string reason, IDictionary<string, object>? additionalDetails = null);

    /// <summary>
    /// Logs an authentication event.
    /// </summary>
    /// <param name="username">The username attempting authentication.</param>
    /// <param name="success">Whether authentication succeeded.</param>
    /// <param name="authMethod">The authentication method used (password, PIN, token, etc.).</param>
    /// <param name="reason">Reason for success or failure.</param>
    /// <param name="additionalDetails">Optional additional context.</param>
    void LogAuthentication(string username, bool success, string authMethod, string reason, IDictionary<string, object>? additionalDetails = null);

    /// <summary>
    /// Logs a certificate operation.
    /// </summary>
    /// <param name="operation">The operation performed (create, update, delete, approve, etc.).</param>
    /// <param name="certificateId">The certificate identifier.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="reason">Reason for success or failure.</param>
    /// <param name="additionalDetails">Optional additional context.</param>
    void LogCertificateOperation(string operation, string certificateId, bool success, string reason, IDictionary<string, object>? additionalDetails = null);

    /// <summary>
    /// Logs an access control decision.
    /// </summary>
    /// <param name="resource">The resource being accessed.</param>
    /// <param name="action">The action being performed.</param>
    /// <param name="allowed">Whether access was allowed.</param>
    /// <param name="reason">Reason for the decision.</param>
    /// <param name="additionalDetails">Optional additional context.</param>
    void LogAccessDecision(string resource, string action, bool allowed, string reason, IDictionary<string, object>? additionalDetails = null);

    /// <summary>
    /// Logs a configuration change.
    /// </summary>
    /// <param name="settingName">The name of the setting being changed.</param>
    /// <param name="oldValue">The previous value (should not contain sensitive data).</param>
    /// <param name="newValue">The new value (should not contain sensitive data).</param>
    /// <param name="additionalDetails">Optional additional context.</param>
    void LogConfigurationChange(string settingName, string? oldValue, string? newValue, IDictionary<string, object>? additionalDetails = null);

    /// <summary>
    /// Logs a generic security event.
    /// </summary>
    /// <param name="eventType">The type of security event.</param>
    /// <param name="description">Description of the event.</param>
    /// <param name="success">Whether the event represents a successful operation.</param>
    /// <param name="additionalDetails">Optional additional context.</param>
    void LogSecurityEvent(Security.AuditEventType eventType, string description, bool success, IDictionary<string, object>? additionalDetails = null);

    /// <summary>
    /// Logs a generic security event asynchronously.
    /// </summary>
    /// <param name="eventType">The type of security event.</param>
    /// <param name="description">Description of the event.</param>
    /// <param name="success">Whether the event represents a successful operation.</param>
    /// <param name="additionalDetails">Optional additional context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogSecurityEventAsync(Security.AuditEventType eventType, string description, bool success, IDictionary<string, object>? additionalDetails = null);

    /// <summary>
    /// Verifies the integrity of a specific log file.
    /// </summary>
    /// <param name="logFilePath">Path to the log file to verify.</param>
    /// <returns>True if all entries pass HMAC verification, false otherwise.</returns>
    bool VerifyLogIntegrity(string logFilePath);

    /// <summary>
    /// Verifies the integrity of all log files in the log directory.
    /// </summary>
    /// <returns>A dictionary mapping log file paths to their verification results.</returns>
    IDictionary<string, bool> VerifyAllLogs();

    /// <summary>
    /// Forces rotation of the current log file regardless of size or date.
    /// </summary>
    void ForceRotate();

    /// <summary>
    /// Flushes any buffered log entries to disk.
    /// </summary>
    void Flush();

    /// <summary>
    /// Flushes any buffered log entries to disk asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync();
}
