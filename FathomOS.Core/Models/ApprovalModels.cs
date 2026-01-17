// FathomOS.Core/Models/ApprovalModels.cs
// Domain models for the shared approval/signatory workflow service
// Used by all calibration modules for consistent approval handling

namespace FathomOS.Core.Models;

/// <summary>
/// Status of an approval request
/// </summary>
public enum ApprovalStatus
{
    /// <summary>Approval is pending review</summary>
    Pending = 0,

    /// <summary>Approval has been granted</summary>
    Approved = 1,

    /// <summary>Approval has been rejected</summary>
    Rejected = 2,

    /// <summary>Approval request has expired</summary>
    Expired = 3
}

/// <summary>
/// Represents a signatory who can approve documents.
/// Signatories are stored for reuse across multiple approvals.
/// </summary>
public class Signatory
{
    /// <summary>
    /// Unique identifier for the signatory (GUID format)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Full name of the signatory
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Professional title (e.g., "Senior Survey Engineer", "Project Manager")
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Company or organization name
    /// </summary>
    public required string Company { get; set; }

    /// <summary>
    /// Email address for the signatory (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Base64-encoded signature image data (optional).
    /// Used for visual signature representation on documents.
    /// </summary>
    public string? SignatureImageData { get; set; }

    /// <summary>
    /// Raw signature data as byte array (optional).
    /// Alternative to SignatureImageData for binary signature storage.
    /// </summary>
    public byte[]? SignatureData { get; set; }

    /// <summary>
    /// When this signatory was first created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this signatory was last used for an approval (UTC)
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether this is the default signatory for quick selection
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Number of times this signatory has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Creates a display-friendly representation
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Title)
        ? Name
        : $"{Name}, {Title}";

    /// <summary>
    /// Creates a full display with company
    /// </summary>
    public string FullDisplayName => string.IsNullOrWhiteSpace(Company)
        ? DisplayName
        : $"{DisplayName} ({Company})";
}

/// <summary>
/// Request for document approval.
/// Contains all information needed to process an approval.
/// </summary>
public class ApprovalRequest
{
    /// <summary>
    /// Unique identifier for this approval request (GUID format)
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Identifier of the document being approved
    /// </summary>
    public required string DocumentId { get; set; }

    /// <summary>
    /// Type of document (e.g., "Certificate", "Calibration Report", "Survey Listing")
    /// </summary>
    public required string DocumentType { get; set; }

    /// <summary>
    /// Human-readable title of the document
    /// </summary>
    public required string DocumentTitle { get; set; }

    /// <summary>
    /// Module that initiated the approval request
    /// </summary>
    public required string ModuleId { get; set; }

    /// <summary>
    /// List of required signatories for this approval.
    /// Multiple signatories support multi-level approval workflows.
    /// </summary>
    public List<Signatory> RequiredSignatories { get; set; } = new();

    /// <summary>
    /// Minimum number of signatures required for approval.
    /// Defaults to 1 if not specified.
    /// </summary>
    public int MinimumSignaturesRequired { get; set; } = 1;

    /// <summary>
    /// Optional expiration time for the approval request (UTC)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the approval was requested (UTC)
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata as key-value pairs.
    /// Can store module-specific data needed for the approval.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Optional description or notes about what is being approved
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Project name associated with this approval
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Client name associated with this approval
    /// </summary>
    public string? ClientName { get; set; }
}

/// <summary>
/// Represents a single signature on an approval.
/// Tracks who signed, when, and any comments.
/// </summary>
public class ApprovalSignature
{
    /// <summary>
    /// Unique identifier for this signature
    /// </summary>
    public string SignatureId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Reference to the approval request this signature belongs to
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// The signatory who provided this signature
    /// </summary>
    public required Signatory Signatory { get; set; }

    /// <summary>
    /// When the signature was provided (UTC)
    /// </summary>
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional comments from the signatory
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// The cryptographic hash of the document at time of signing.
    /// Used for integrity verification.
    /// </summary>
    public string? DocumentHash { get; set; }

    /// <summary>
    /// IP address or machine identifier where signature was provided
    /// </summary>
    public string? SignedFrom { get; set; }
}

/// <summary>
/// Result of an approval request.
/// Contains the final status and all collected signatures.
/// </summary>
public class ApprovalResult
{
    /// <summary>
    /// Reference to the original approval request
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Reference to the document that was approved/rejected
    /// </summary>
    public required string DocumentId { get; set; }

    /// <summary>
    /// Final status of the approval
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// Whether the approval was granted
    /// </summary>
    public bool IsApproved => Status == ApprovalStatus.Approved;

    /// <summary>
    /// All signatures collected for this approval
    /// </summary>
    public List<ApprovalSignature> Signatures { get; set; } = new();

    /// <summary>
    /// When the approval process was completed (UTC)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Reason for rejection (if status is Rejected)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// General comments or notes about the approval
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Gets the primary signatory (first signature) for simple workflows
    /// </summary>
    public Signatory? PrimarySignatory => Signatures.FirstOrDefault()?.Signatory;

    /// <summary>
    /// Gets the primary signature timestamp for audit purposes
    /// </summary>
    public DateTime? SignedAt => Signatures.FirstOrDefault()?.SignedAt;

    /// <summary>
    /// Creates a successful approval result with a single signatory
    /// </summary>
    public static ApprovalResult CreateApproved(
        string requestId,
        string documentId,
        Signatory signatory,
        string? comments = null)
    {
        return new ApprovalResult
        {
            RequestId = requestId,
            DocumentId = documentId,
            Status = ApprovalStatus.Approved,
            CompletedAt = DateTime.UtcNow,
            Comments = comments,
            Signatures = new List<ApprovalSignature>
            {
                new ApprovalSignature
                {
                    RequestId = requestId,
                    Signatory = signatory,
                    Comments = comments
                }
            }
        };
    }

    /// <summary>
    /// Creates a rejected approval result
    /// </summary>
    public static ApprovalResult CreateRejected(
        string requestId,
        string documentId,
        string rejectionReason,
        Signatory? rejectedBy = null)
    {
        var result = new ApprovalResult
        {
            RequestId = requestId,
            DocumentId = documentId,
            Status = ApprovalStatus.Rejected,
            CompletedAt = DateTime.UtcNow,
            RejectionReason = rejectionReason
        };

        if (rejectedBy != null)
        {
            result.Signatures.Add(new ApprovalSignature
            {
                RequestId = requestId,
                Signatory = rejectedBy,
                Comments = rejectionReason
            });
        }

        return result;
    }

    /// <summary>
    /// Creates a cancelled/expired approval result
    /// </summary>
    public static ApprovalResult CreateExpired(string requestId, string documentId)
    {
        return new ApprovalResult
        {
            RequestId = requestId,
            DocumentId = documentId,
            Status = ApprovalStatus.Expired,
            CompletedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Options for approval dialog display
/// </summary>
public class ApprovalDialogOptions
{
    /// <summary>
    /// Title for the approval dialog window
    /// </summary>
    public string WindowTitle { get; set; } = "Approval Required";

    /// <summary>
    /// Message to display to the approver
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Whether to show the document preview
    /// </summary>
    public bool ShowDocumentPreview { get; set; } = true;

    /// <summary>
    /// Whether to require comments
    /// </summary>
    public bool RequireComments { get; set; } = false;

    /// <summary>
    /// Whether to allow signature image capture
    /// </summary>
    public bool AllowSignatureCapture { get; set; } = false;

    /// <summary>
    /// Whether to show recently used signatories for quick selection
    /// </summary>
    public bool ShowRecentSignatories { get; set; } = true;

    /// <summary>
    /// Maximum number of recent signatories to show
    /// </summary>
    public int MaxRecentSignatories { get; set; } = 5;
}
