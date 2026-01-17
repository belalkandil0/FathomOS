using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a rotation pattern for offshore personnel (e.g., 28/28, 21/21)
/// </summary>
public class RotationPattern
{
    [Key]
    public Guid RotationPatternId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Pattern code (e.g., 28-28, 21-21, B2B)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Pattern name (e.g., "28/28 Standard", "Back to Back")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the rotation pattern
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of rotation pattern
    /// </summary>
    public RotationType RotationType { get; set; } = RotationType.EqualRotation;

    /// <summary>
    /// Number of days on (offshore/working)
    /// </summary>
    public int DaysOn { get; set; } = 28;

    /// <summary>
    /// Number of days off (leave/shore)
    /// </summary>
    public int DaysOff { get; set; } = 28;

    /// <summary>
    /// Total cycle length in days
    /// </summary>
    [NotMapped]
    public int TotalCycleDays => DaysOn + DaysOff;

    /// <summary>
    /// Maximum consecutive days allowed offshore (for safety/regulation compliance)
    /// </summary>
    public int? MaxConsecutiveDaysOffshore { get; set; }

    /// <summary>
    /// Minimum rest days required after offshore period
    /// </summary>
    public int? MinRestDays { get; set; }

    /// <summary>
    /// Whether travel days count as work days
    /// </summary>
    public bool TravelDaysCountAsWork { get; set; } = true;

    /// <summary>
    /// Number of travel days typically required
    /// </summary>
    public int TypicalTravelDays { get; set; } = 2;

    /// <summary>
    /// Whether pattern is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    #region Audit

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long SyncVersion { get; set; }

    #endregion

    #region Navigation Properties

    [JsonIgnore]
    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    [JsonIgnore]
    public virtual ICollection<Personnel> Personnel { get; set; } = new List<Personnel>();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Display string for the rotation pattern
    /// </summary>
    [NotMapped]
    public string DisplayName => $"{DaysOn}/{DaysOff}";

    /// <summary>
    /// Annual offshore days (approximate)
    /// </summary>
    [NotMapped]
    public int AnnualOffshoreDays => TotalCycleDays > 0
        ? (int)Math.Round(365.0 / TotalCycleDays * DaysOn)
        : 0;

    #endregion
}
