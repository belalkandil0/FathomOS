using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service implementation for managing equipment check-out and check-in operations.
/// </summary>
public class CheckoutService : ICheckoutService
{
    private readonly LocalDatabaseService _dbService;

    public CheckoutService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <inheritdoc />
    public async Task<EquipmentCheckout> CheckOutAsync(
        Guid equipmentId,
        string checkedOutTo,
        Guid? projectId = null,
        DateTime? expectedReturn = null,
        string? notes = null)
    {
        return await CheckOutAsync(new CheckoutRequest
        {
            EquipmentId = equipmentId,
            CheckedOutTo = checkedOutTo,
            ProjectId = projectId,
            ExpectedReturnDate = expectedReturn,
            Notes = notes
        });
    }

    /// <inheritdoc />
    public async Task<EquipmentCheckout> CheckOutAsync(CheckoutRequest request)
    {
        // Validate equipment exists
        var equipment = await _dbService.Context.Equipment
            .FirstOrDefaultAsync(e => e.EquipmentId == request.EquipmentId);

        if (equipment == null)
        {
            throw new InvalidOperationException($"Equipment with ID {request.EquipmentId} not found.");
        }

        // Check if equipment is available
        if (!await IsEquipmentAvailableAsync(request.EquipmentId))
        {
            throw new InvalidOperationException($"Equipment '{equipment.Name}' is already checked out.");
        }

        // Validate equipment status allows checkout
        if (equipment.Status == EquipmentStatus.Retired ||
            equipment.Status == EquipmentStatus.Disposed ||
            equipment.Status == EquipmentStatus.Lost)
        {
            throw new InvalidOperationException($"Equipment '{equipment.Name}' cannot be checked out due to status: {equipment.Status}");
        }

        // Create checkout record
        var checkout = new EquipmentCheckout
        {
            EquipmentId = request.EquipmentId,
            CheckedOutTo = request.CheckedOutTo,
            CheckedOutToUserId = request.CheckedOutToUserId,
            ProjectId = request.ProjectId,
            ExpectedReturnDate = request.ExpectedReturnDate,
            CheckoutNotes = request.Notes,
            ConditionAtCheckout = request.ConditionAtCheckout ?? equipment.Condition,
            CheckoutLocationId = request.CheckoutLocationId ?? equipment.CurrentLocationId,
            CheckedOutBy = request.CheckedOutBy,
            CheckedOutAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbService.Context.EquipmentCheckouts.Add(checkout);

        // Update equipment status
        equipment.Status = EquipmentStatus.CheckedOut;
        equipment.CurrentProjectId = request.ProjectId;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;

        // Record history
        var history = new EquipmentHistory
        {
            EquipmentId = equipment.EquipmentId,
            Action = HistoryAction.StatusChanged,
            Description = $"Checked out to {request.CheckedOutTo}",
            OldValue = EquipmentStatus.Available.ToString(),
            NewValue = EquipmentStatus.CheckedOut.ToString(),
            PerformedBy = request.CheckedOutBy,
            PerformedAt = DateTime.UtcNow,
            Notes = request.Notes
        };
        _dbService.Context.EquipmentHistory.Add(history);

        await _dbService.Context.SaveChangesAsync();

        return checkout;
    }

    /// <inheritdoc />
    public async Task<EquipmentCheckout> CheckInAsync(Guid checkoutId, string? notes = null)
    {
        return await CheckInAsync(new CheckInRequest
        {
            CheckoutId = checkoutId,
            Notes = notes
        });
    }

    /// <inheritdoc />
    public async Task<EquipmentCheckout> CheckInAsync(CheckInRequest request)
    {
        var checkout = await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.CheckoutId == request.CheckoutId);

        if (checkout == null)
        {
            throw new InvalidOperationException($"Checkout record with ID {request.CheckoutId} not found.");
        }

        if (checkout.IsReturned)
        {
            throw new InvalidOperationException("This equipment has already been returned.");
        }

        // Update checkout record
        checkout.ReturnedAt = DateTime.UtcNow;
        checkout.ReturnNotes = request.Notes;
        checkout.ConditionAtReturn = request.ConditionAtReturn;
        checkout.ReturnLocationId = request.ReturnLocationId;
        checkout.ReturnedBy = request.ReturnedBy;
        checkout.UpdatedAt = DateTime.UtcNow;

        // Update equipment status
        if (checkout.Equipment != null)
        {
            checkout.Equipment.Status = EquipmentStatus.Available;
            checkout.Equipment.CurrentProjectId = null;
            checkout.Equipment.UpdatedAt = DateTime.UtcNow;
            checkout.Equipment.IsModifiedLocally = true;

            // Update condition if changed
            if (request.ConditionAtReturn.HasValue)
            {
                checkout.Equipment.Condition = request.ConditionAtReturn.Value;
            }

            // Update location if specified
            if (request.ReturnLocationId.HasValue)
            {
                checkout.Equipment.CurrentLocationId = request.ReturnLocationId.Value;
            }

            // Record history
            var history = new EquipmentHistory
            {
                EquipmentId = checkout.EquipmentId,
                Action = HistoryAction.StatusChanged,
                Description = $"Returned from {checkout.CheckedOutTo}",
                OldValue = EquipmentStatus.CheckedOut.ToString(),
                NewValue = EquipmentStatus.Available.ToString(),
                PerformedBy = request.ReturnedBy,
                PerformedAt = DateTime.UtcNow,
                Notes = request.Notes
            };
            _dbService.Context.EquipmentHistory.Add(history);
        }

        await _dbService.Context.SaveChangesAsync();

        return checkout;
    }

    /// <inheritdoc />
    public async Task<List<EquipmentCheckout>> GetCurrentCheckoutsAsync()
    {
        return await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.Project)
            .Include(c => c.CheckedOutToUser)
            .Where(c => c.ReturnedAt == null)
            .OrderByDescending(c => c.CheckedOutAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EquipmentCheckout>> GetCurrentCheckoutsAsync(CheckoutFilter filter)
    {
        var query = _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.Project)
            .Include(c => c.CheckedOutToUser)
            .AsQueryable();

        // Apply filters
        if (filter.EquipmentId.HasValue)
        {
            query = query.Where(c => c.EquipmentId == filter.EquipmentId.Value);
        }

        if (filter.ProjectId.HasValue)
        {
            query = query.Where(c => c.ProjectId == filter.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CheckedOutTo))
        {
            query = query.Where(c => c.CheckedOutTo.Contains(filter.CheckedOutTo));
        }

        if (filter.CheckedOutToUserId.HasValue)
        {
            query = query.Where(c => c.CheckedOutToUserId == filter.CheckedOutToUserId.Value);
        }

        if (filter.IncludeReturned != true)
        {
            query = query.Where(c => c.ReturnedAt == null);
        }

        if (filter.OverdueOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(c => c.ReturnedAt == null &&
                                     c.ExpectedReturnDate != null &&
                                     c.ExpectedReturnDate < now);
        }

        if (filter.CheckedOutAfter.HasValue)
        {
            query = query.Where(c => c.CheckedOutAt >= filter.CheckedOutAfter.Value);
        }

        if (filter.CheckedOutBefore.HasValue)
        {
            query = query.Where(c => c.CheckedOutAt <= filter.CheckedOutBefore.Value);
        }

        if (filter.ExpectedReturnBefore.HasValue)
        {
            query = query.Where(c => c.ExpectedReturnDate != null &&
                                     c.ExpectedReturnDate <= filter.ExpectedReturnBefore.Value);
        }

        return await query
            .OrderByDescending(c => c.CheckedOutAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EquipmentCheckout>> GetCheckoutHistoryAsync(Guid equipmentId)
    {
        return await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.Project)
            .Include(c => c.CheckedOutToUser)
            .Where(c => c.EquipmentId == equipmentId)
            .OrderByDescending(c => c.CheckedOutAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsEquipmentAvailableAsync(Guid equipmentId)
    {
        // Check for any active (unreturned) checkouts
        var hasActiveCheckout = await _dbService.Context.EquipmentCheckouts
            .AnyAsync(c => c.EquipmentId == equipmentId && c.ReturnedAt == null);

        if (hasActiveCheckout)
        {
            return false;
        }

        // Also check equipment status
        var equipment = await _dbService.Context.Equipment
            .FirstOrDefaultAsync(e => e.EquipmentId == equipmentId);

        if (equipment == null)
        {
            return false;
        }

        return equipment.Status == EquipmentStatus.Available ||
               equipment.Status == EquipmentStatus.InService ||
               equipment.Status == EquipmentStatus.InStock;
    }

    /// <inheritdoc />
    public async Task<EquipmentCheckout?> GetCurrentCheckoutAsync(Guid equipmentId)
    {
        return await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.Project)
            .Include(c => c.CheckedOutToUser)
            .Where(c => c.EquipmentId == equipmentId && c.ReturnedAt == null)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.Project)
            .Include(c => c.CheckedOutToUser)
            .Where(c => c.ReturnedAt == null &&
                        c.ExpectedReturnDate != null &&
                        c.ExpectedReturnDate < now)
            .OrderBy(c => c.ExpectedReturnDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EquipmentCheckout>> GetCheckoutsByPersonAsync(string personName)
    {
        return await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.Project)
            .Where(c => c.CheckedOutTo.Contains(personName))
            .OrderByDescending(c => c.CheckedOutAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EquipmentCheckout>> GetCheckoutsByProjectAsync(Guid projectId)
    {
        return await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.CheckedOutToUser)
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CheckedOutAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<EquipmentCheckout?> GetCheckoutByIdAsync(Guid checkoutId)
    {
        return await _dbService.Context.EquipmentCheckouts
            .Include(c => c.Equipment)
            .Include(c => c.Project)
            .Include(c => c.CheckedOutToUser)
            .FirstOrDefaultAsync(c => c.CheckoutId == checkoutId);
    }

    /// <inheritdoc />
    public async Task<EquipmentCheckout> UpdateCheckoutAsync(EquipmentCheckout checkout)
    {
        checkout.UpdatedAt = DateTime.UtcNow;
        _dbService.Context.EquipmentCheckouts.Update(checkout);
        await _dbService.Context.SaveChangesAsync();
        return checkout;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCheckoutAsync(Guid checkoutId)
    {
        var checkout = await _dbService.Context.EquipmentCheckouts
            .FirstOrDefaultAsync(c => c.CheckoutId == checkoutId);

        if (checkout == null)
        {
            return false;
        }

        // If checkout is active, update equipment status back to available
        if (!checkout.IsReturned)
        {
            var equipment = await _dbService.Context.Equipment
                .FirstOrDefaultAsync(e => e.EquipmentId == checkout.EquipmentId);

            if (equipment != null && equipment.Status == EquipmentStatus.CheckedOut)
            {
                equipment.Status = EquipmentStatus.Available;
                equipment.UpdatedAt = DateTime.UtcNow;
            }
        }

        _dbService.Context.EquipmentCheckouts.Remove(checkout);
        await _dbService.Context.SaveChangesAsync();

        return true;
    }
}
