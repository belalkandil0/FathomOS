using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.PersonnelManagement.Data;
using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Implementation of personnel CRUD and vessel assignment operations
/// </summary>
public class PersonnelService : IPersonnelService
{
    private readonly PersonnelDbContext _context;

    public PersonnelService(PersonnelDbContext context)
    {
        _context = context;
    }

    #region Personnel CRUD

    public async Task<IEnumerable<Personnel>> GetAllPersonnelAsync()
    {
        return await _context.Personnel
            .Include(p => p.Position)
            .Include(p => p.RotationPattern)
            .Where(p => p.IsActive)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<Personnel?> GetPersonnelByIdAsync(Guid personnelId)
    {
        return await _context.Personnel
            .Include(p => p.Position)
            .Include(p => p.RotationPattern)
            .Include(p => p.Certifications)
            .ThenInclude(c => c.CertificationType)
            .Include(p => p.VesselAssignments)
            .FirstOrDefaultAsync(p => p.PersonnelId == personnelId);
    }

    public async Task<Personnel?> GetPersonnelByEmployeeNumberAsync(string employeeNumber)
    {
        return await _context.Personnel
            .Include(p => p.Position)
            .FirstOrDefaultAsync(p => p.EmployeeNumber == employeeNumber);
    }

    public async Task<IEnumerable<Personnel>> SearchPersonnelAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _context.Personnel
            .Include(p => p.Position)
            .Where(p => p.IsActive &&
                (p.FirstName.ToLower().Contains(term) ||
                 p.LastName.ToLower().Contains(term) ||
                 p.EmployeeNumber.ToLower().Contains(term) ||
                 (p.Email != null && p.Email.ToLower().Contains(term))))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Personnel>> GetPersonnelByDepartmentAsync(Department department)
    {
        return await _context.Personnel
            .Include(p => p.Position)
            .Where(p => p.IsActive && p.Department == department)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Personnel>> GetActivePersonnelAsync()
    {
        return await _context.Personnel
            .Include(p => p.Position)
            .Where(p => p.IsActive && p.EmploymentStatus == EmploymentStatus.Active)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Personnel>> GetOffshorePersonnelAsync()
    {
        return await _context.Personnel
            .Include(p => p.Position)
            .Where(p => p.IsActive && p.IsOffshore)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<Personnel> CreatePersonnelAsync(Personnel personnel)
    {
        personnel.CreatedAt = DateTime.UtcNow;
        personnel.UpdatedAt = DateTime.UtcNow;
        _context.Personnel.Add(personnel);
        await _context.SaveChangesAsync();
        return personnel;
    }

    public async Task<Personnel> UpdatePersonnelAsync(Personnel personnel)
    {
        personnel.UpdatedAt = DateTime.UtcNow;
        personnel.IsModifiedLocally = true;
        _context.Personnel.Update(personnel);
        await _context.SaveChangesAsync();
        return personnel;
    }

    public async Task<bool> DeletePersonnelAsync(Guid personnelId)
    {
        var personnel = await _context.Personnel.FindAsync(personnelId);
        if (personnel == null) return false;

        personnel.IsActive = false;
        personnel.UpdatedAt = DateTime.UtcNow;
        personnel.IsModifiedLocally = true;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Vessel Assignments

    public async Task<IEnumerable<VesselAssignment>> GetVesselAssignmentsAsync(Guid personnelId)
    {
        return await _context.VesselAssignments
            .Include(a => a.Position)
            .Where(a => a.PersonnelId == personnelId && a.IsActive)
            .OrderByDescending(a => a.ScheduledStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<VesselAssignment>> GetActiveVesselAssignmentsAsync()
    {
        return await _context.VesselAssignments
            .Include(a => a.Personnel)
            .Include(a => a.Position)
            .Where(a => a.IsActive && a.Status == AssignmentStatus.SignedOn)
            .OrderBy(a => a.VesselName)
            .ThenBy(a => a.Personnel!.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<VesselAssignment>> GetUpcomingAssignmentsAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.VesselAssignments
            .Include(a => a.Personnel)
            .Include(a => a.Position)
            .Where(a => a.IsActive &&
                a.Status == AssignmentStatus.Scheduled &&
                a.ScheduledStartDate <= cutoffDate)
            .OrderBy(a => a.ScheduledStartDate)
            .ToListAsync();
    }

    public async Task<VesselAssignment> CreateVesselAssignmentAsync(VesselAssignment assignment)
    {
        assignment.CreatedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.VesselAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<VesselAssignment> UpdateVesselAssignmentAsync(VesselAssignment assignment)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.VesselAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<bool> SignOnAsync(Guid assignmentId, DateTime signOnDateTime, string? location, string? port)
    {
        var assignment = await _context.VesselAssignments.FindAsync(assignmentId);
        if (assignment == null) return false;

        assignment.SignOnDateTime = signOnDateTime;
        assignment.SignOnLocation = location;
        assignment.SignOnPort = port;
        assignment.Status = AssignmentStatus.SignedOn;
        assignment.UpdatedAt = DateTime.UtcNow;

        // Update personnel offshore status
        var personnel = await _context.Personnel.FindAsync(assignment.PersonnelId);
        if (personnel != null)
        {
            personnel.IsOffshore = true;
            personnel.CurrentVesselId = assignment.VesselId;
            personnel.CurrentProjectId = assignment.ProjectId;
            personnel.CurrentRotationStartDate = signOnDateTime;
            personnel.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SignOffAsync(Guid assignmentId, DateTime signOffDateTime, string? location, string? port, string? reason)
    {
        var assignment = await _context.VesselAssignments.FindAsync(assignmentId);
        if (assignment == null) return false;

        assignment.SignOffDateTime = signOffDateTime;
        assignment.SignOffLocation = location;
        assignment.SignOffPort = port;
        assignment.SignOffReason = reason;
        assignment.Status = AssignmentStatus.SignedOff;
        assignment.UpdatedAt = DateTime.UtcNow;

        // Update personnel offshore status
        var personnel = await _context.Personnel.FindAsync(assignment.PersonnelId);
        if (personnel != null)
        {
            personnel.IsOffshore = false;
            personnel.CurrentVesselId = null;
            personnel.CurrentProjectId = null;
            personnel.CurrentRotationEndDate = signOffDateTime;
            personnel.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Certifications

    public async Task<IEnumerable<PersonnelCertification>> GetPersonnelCertificationsAsync(Guid personnelId)
    {
        return await _context.PersonnelCertifications
            .Include(c => c.CertificationType)
            .Where(c => c.PersonnelId == personnelId)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<PersonnelCertification>> GetExpiringCertificationsAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.PersonnelCertifications
            .Include(c => c.Personnel)
            .Include(c => c.CertificationType)
            .Where(c => c.ExpiryDate <= cutoffDate &&
                c.Status == CertificationStatus.Valid)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    public async Task<PersonnelCertification> AddCertificationAsync(PersonnelCertification certification)
    {
        certification.CreatedAt = DateTime.UtcNow;
        certification.UpdatedAt = DateTime.UtcNow;
        _context.PersonnelCertifications.Add(certification);
        await _context.SaveChangesAsync();
        return certification;
    }

    public async Task<PersonnelCertification> UpdateCertificationAsync(PersonnelCertification certification)
    {
        certification.UpdatedAt = DateTime.UtcNow;
        _context.PersonnelCertifications.Update(certification);
        await _context.SaveChangesAsync();
        return certification;
    }

    #endregion

    #region Statistics

    public async Task<int> GetTotalPersonnelCountAsync()
    {
        return await _context.Personnel.CountAsync(p => p.IsActive);
    }

    public async Task<int> GetActivePersonnelCountAsync()
    {
        return await _context.Personnel.CountAsync(p => p.IsActive && p.EmploymentStatus == EmploymentStatus.Active);
    }

    public async Task<int> GetOffshorePersonnelCountAsync()
    {
        return await _context.Personnel.CountAsync(p => p.IsActive && p.IsOffshore);
    }

    #endregion
}
