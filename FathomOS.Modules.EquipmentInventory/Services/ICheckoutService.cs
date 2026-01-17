using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service interface for managing equipment check-out and check-in operations.
/// </summary>
public interface ICheckoutService
{
    /// <summary>
    /// Check out equipment to a person or project.
    /// </summary>
    /// <param name="equipmentId">The ID of the equipment to check out</param>
    /// <param name="checkedOutTo">Name of person or entity receiving the equipment</param>
    /// <param name="projectId">Optional project ID the equipment is assigned to</param>
    /// <param name="expectedReturn">Optional expected return date</param>
    /// <param name="notes">Optional notes for the checkout</param>
    /// <returns>The created checkout record</returns>
    Task<EquipmentCheckout> CheckOutAsync(
        Guid equipmentId,
        string checkedOutTo,
        Guid? projectId = null,
        DateTime? expectedReturn = null,
        string? notes = null);

    /// <summary>
    /// Check out equipment with additional options.
    /// </summary>
    Task<EquipmentCheckout> CheckOutAsync(CheckoutRequest request);

    /// <summary>
    /// Check in (return) equipment.
    /// </summary>
    /// <param name="checkoutId">The checkout record ID</param>
    /// <param name="notes">Optional notes for the return</param>
    /// <returns>The updated checkout record</returns>
    Task<EquipmentCheckout> CheckInAsync(Guid checkoutId, string? notes = null);

    /// <summary>
    /// Check in equipment with additional options.
    /// </summary>
    Task<EquipmentCheckout> CheckInAsync(CheckInRequest request);

    /// <summary>
    /// Get all current (unreturned) checkouts.
    /// </summary>
    /// <returns>List of active checkout records</returns>
    Task<List<EquipmentCheckout>> GetCurrentCheckoutsAsync();

    /// <summary>
    /// Get current checkouts filtered by criteria.
    /// </summary>
    Task<List<EquipmentCheckout>> GetCurrentCheckoutsAsync(CheckoutFilter filter);

    /// <summary>
    /// Get checkout history for a specific equipment item.
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <returns>List of all checkout records for the equipment</returns>
    Task<List<EquipmentCheckout>> GetCheckoutHistoryAsync(Guid equipmentId);

    /// <summary>
    /// Check if equipment is currently available for checkout.
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <returns>True if equipment can be checked out</returns>
    Task<bool> IsEquipmentAvailableAsync(Guid equipmentId);

    /// <summary>
    /// Get the current checkout record for equipment if it's checked out.
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <returns>Current checkout record or null if available</returns>
    Task<EquipmentCheckout?> GetCurrentCheckoutAsync(Guid equipmentId);

    /// <summary>
    /// Get all overdue checkouts.
    /// </summary>
    /// <returns>List of checkouts past their expected return date</returns>
    Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync();

    /// <summary>
    /// Get checkouts by person.
    /// </summary>
    /// <param name="personName">Name of the person</param>
    /// <returns>List of checkouts for that person</returns>
    Task<List<EquipmentCheckout>> GetCheckoutsByPersonAsync(string personName);

    /// <summary>
    /// Get checkouts by project.
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>List of checkouts for that project</returns>
    Task<List<EquipmentCheckout>> GetCheckoutsByProjectAsync(Guid projectId);

    /// <summary>
    /// Get a checkout record by ID.
    /// </summary>
    Task<EquipmentCheckout?> GetCheckoutByIdAsync(Guid checkoutId);

    /// <summary>
    /// Update an existing checkout record.
    /// </summary>
    Task<EquipmentCheckout> UpdateCheckoutAsync(EquipmentCheckout checkout);

    /// <summary>
    /// Delete a checkout record (admin only).
    /// </summary>
    Task<bool> DeleteCheckoutAsync(Guid checkoutId);
}

/// <summary>
/// Request model for checking out equipment with full options.
/// </summary>
public class CheckoutRequest
{
    public Guid EquipmentId { get; set; }
    public string CheckedOutTo { get; set; } = string.Empty;
    public Guid? CheckedOutToUserId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string? Notes { get; set; }
    public EquipmentCondition? ConditionAtCheckout { get; set; }
    public Guid? CheckoutLocationId { get; set; }
    public Guid? CheckedOutBy { get; set; }
}

/// <summary>
/// Request model for checking in equipment with full options.
/// </summary>
public class CheckInRequest
{
    public Guid CheckoutId { get; set; }
    public string? Notes { get; set; }
    public EquipmentCondition? ConditionAtReturn { get; set; }
    public Guid? ReturnLocationId { get; set; }
    public Guid? ReturnedBy { get; set; }
}

/// <summary>
/// Filter options for querying checkouts.
/// </summary>
public class CheckoutFilter
{
    public Guid? EquipmentId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? CheckedOutTo { get; set; }
    public Guid? CheckedOutToUserId { get; set; }
    public bool? IncludeReturned { get; set; }
    public bool? OverdueOnly { get; set; }
    public DateTime? CheckedOutAfter { get; set; }
    public DateTime? CheckedOutBefore { get; set; }
    public DateTime? ExpectedReturnBefore { get; set; }
}
