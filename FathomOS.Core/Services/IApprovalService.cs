// FathomOS.Core/Services/IApprovalService.cs
// Contract for the shared approval/signatory workflow service
// Used by all calibration modules for consistent approval handling

using FathomOS.Core.Models;

namespace FathomOS.Core.Services;

/// <summary>
/// Contract for approval workflow management.
/// Provides signatory management and document approval capabilities
/// shared across all calibration modules.
/// </summary>
public interface IApprovalService
{
    #region Approval Workflow

    /// <summary>
    /// Request approval for a document.
    /// Creates an approval request and returns the result after processing.
    /// </summary>
    /// <param name="request">The approval request containing document and signatory information.</param>
    /// <returns>The result of the approval request.</returns>
    Task<ApprovalResult> RequestApprovalAsync(ApprovalRequest request);

    /// <summary>
    /// Add a signature to an existing approval request.
    /// Supports multi-signature workflows.
    /// </summary>
    /// <param name="requestId">The ID of the approval request.</param>
    /// <param name="signatory">The signatory providing the signature.</param>
    /// <param name="comments">Optional comments from the signatory.</param>
    /// <returns>Updated approval result.</returns>
    Task<ApprovalResult> AddSignatureAsync(string requestId, Signatory signatory, string? comments = null);

    /// <summary>
    /// Reject an approval request.
    /// </summary>
    /// <param name="requestId">The ID of the approval request to reject.</param>
    /// <param name="reason">The reason for rejection.</param>
    /// <param name="rejectedBy">The signatory who rejected (optional).</param>
    /// <returns>Updated approval result with rejection status.</returns>
    Task<ApprovalResult> RejectApprovalAsync(string requestId, string reason, Signatory? rejectedBy = null);

    /// <summary>
    /// Get the current status of an approval request.
    /// </summary>
    /// <param name="requestId">The ID of the approval request.</param>
    /// <returns>The approval result, or null if not found.</returns>
    Task<ApprovalResult?> GetApprovalStatusAsync(string requestId);

    #endregion

    #region Signature Validation

    /// <summary>
    /// Validate a signature by checking if the signatory exists and is valid.
    /// </summary>
    /// <param name="signatureData">The signature data to validate (Base64-encoded image).</param>
    /// <param name="signatoryId">The ID of the signatory who provided the signature.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    Task<bool> ValidateSignatureAsync(byte[] signatureData, string signatoryId);

    /// <summary>
    /// Validate a signatory exists and has the required information.
    /// </summary>
    /// <param name="signatoryId">The ID of the signatory to validate.</param>
    /// <returns>True if the signatory is valid; otherwise, false.</returns>
    Task<bool> ValidateSignatoryAsync(string signatoryId);

    #endregion

    #region Signatory Management

    /// <summary>
    /// Get all registered signatories.
    /// </summary>
    /// <returns>Collection of all signatories.</returns>
    Task<IEnumerable<Signatory>> GetSignatoriesAsync();

    /// <summary>
    /// Get a signatory by their unique identifier.
    /// </summary>
    /// <param name="id">The signatory ID.</param>
    /// <returns>The signatory if found; otherwise, null.</returns>
    Task<Signatory?> GetSignatoryByIdAsync(string id);

    /// <summary>
    /// Get the default signatory, if one is set.
    /// </summary>
    /// <returns>The default signatory if set; otherwise, null.</returns>
    Task<Signatory?> GetDefaultSignatoryAsync();

    /// <summary>
    /// Get recently used signatories for quick selection.
    /// </summary>
    /// <param name="count">Maximum number of recent signatories to return.</param>
    /// <returns>Collection of recently used signatories, ordered by last use.</returns>
    Task<IEnumerable<Signatory>> GetRecentSignatoriesAsync(int count = 5);

    /// <summary>
    /// Save a new or update an existing signatory.
    /// </summary>
    /// <param name="signatory">The signatory to save.</param>
    Task SaveSignatoryAsync(Signatory signatory);

    /// <summary>
    /// Set a signatory as the default for quick selection.
    /// </summary>
    /// <param name="signatoryId">The ID of the signatory to set as default.</param>
    Task SetDefaultSignatoryAsync(string signatoryId);

    /// <summary>
    /// Delete a signatory by their unique identifier.
    /// </summary>
    /// <param name="id">The signatory ID to delete.</param>
    Task DeleteSignatoryAsync(string id);

    /// <summary>
    /// Record that a signatory was used for an approval.
    /// Updates the signatory's usage statistics.
    /// </summary>
    /// <param name="signatoryId">The ID of the signatory that was used.</param>
    Task RecordSignatoryUsageAsync(string signatoryId);

    #endregion

    #region Events

    /// <summary>
    /// Raised when a signatory is added or updated.
    /// </summary>
    event EventHandler<Signatory>? SignatoryChanged;

    /// <summary>
    /// Raised when a signatory is deleted.
    /// </summary>
    event EventHandler<string>? SignatoryDeleted;

    /// <summary>
    /// Raised when an approval is completed (approved or rejected).
    /// </summary>
    event EventHandler<ApprovalResult>? ApprovalCompleted;

    #endregion
}
