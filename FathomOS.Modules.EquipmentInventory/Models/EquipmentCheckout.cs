using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Models;

/// <summary>
/// Represents an equipment check-out/check-in record for tracking equipment custody.
/// </summary>
public class EquipmentCheckout
{
    [Key]
    public Guid CheckoutId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Alias for CheckoutId to maintain compatibility with task spec
    /// </summary>
    [NotMapped]
    public string Id => CheckoutId.ToString();

    /// <summary>
    /// The equipment item being checked out
    /// </summary>
    [Required]
    public Guid EquipmentId { get; set; }

    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }

    /// <summary>
    /// Person or entity the equipment is checked out to
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CheckedOutTo { get; set; } = string.Empty;

    /// <summary>
    /// User ID of the person checking out (if known)
    /// </summary>
    public Guid? CheckedOutToUserId { get; set; }

    [ForeignKey(nameof(CheckedOutToUserId))]
    [JsonIgnore]
    public virtual User? CheckedOutToUser { get; set; }

    /// <summary>
    /// Project the equipment is assigned to (optional)
    /// </summary>
    public Guid? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    [JsonIgnore]
    public virtual Project? Project { get; set; }

    /// <summary>
    /// Timestamp when the equipment was checked out
    /// </summary>
    public DateTime CheckedOutAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who performed the checkout
    /// </summary>
    public Guid? CheckedOutBy { get; set; }

    /// <summary>
    /// Expected return date (optional)
    /// </summary>
    public DateTime? ExpectedReturnDate { get; set; }

    /// <summary>
    /// Actual return date/time (null if not yet returned)
    /// </summary>
    public DateTime? ReturnedAt { get; set; }

    /// <summary>
    /// User who performed the check-in
    /// </summary>
    public Guid? ReturnedBy { get; set; }

    /// <summary>
    /// Notes at time of checkout
    /// </summary>
    [MaxLength(1000)]
    public string? CheckoutNotes { get; set; }

    /// <summary>
    /// Notes at time of return
    /// </summary>
    [MaxLength(1000)]
    public string? ReturnNotes { get; set; }

    /// <summary>
    /// Combined notes property for compatibility
    /// </summary>
    [NotMapped]
    public string? Notes
    {
        get => CheckoutNotes;
        set => CheckoutNotes = value;
    }

    /// <summary>
    /// Condition of equipment at checkout
    /// </summary>
    public EquipmentCondition? ConditionAtCheckout { get; set; }

    /// <summary>
    /// Condition of equipment at return
    /// </summary>
    public EquipmentCondition? ConditionAtReturn { get; set; }

    /// <summary>
    /// Location where equipment was checked out from
    /// </summary>
    public Guid? CheckoutLocationId { get; set; }

    /// <summary>
    /// Location where equipment was returned to
    /// </summary>
    public Guid? ReturnLocationId { get; set; }

    /// <summary>
    /// Indicates whether the equipment has been returned
    /// </summary>
    [NotMapped]
    public bool IsReturned => ReturnedAt.HasValue;

    /// <summary>
    /// Indicates if the checkout is overdue
    /// </summary>
    [NotMapped]
    public bool IsOverdue => !IsReturned && ExpectedReturnDate.HasValue && ExpectedReturnDate.Value < DateTime.UtcNow;

    /// <summary>
    /// Number of days until expected return (negative if overdue)
    /// </summary>
    [NotMapped]
    public int? DaysUntilDue => ExpectedReturnDate.HasValue
        ? (int)(ExpectedReturnDate.Value - DateTime.UtcNow).TotalDays
        : null;

    /// <summary>
    /// Duration of checkout (or current duration if not returned)
    /// </summary>
    [NotMapped]
    public TimeSpan Duration => (ReturnedAt ?? DateTime.UtcNow) - CheckedOutAt;

    /// <summary>
    /// Sync version for offline-first operation
    /// </summary>
    public long SyncVersion { get; set; }

    /// <summary>
    /// Audit timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
