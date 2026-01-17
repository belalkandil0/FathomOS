using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Interface for personnel CRUD and vessel assignment operations
/// </summary>
public interface IPersonnelService
{
    // Personnel CRUD
    Task<IEnumerable<Personnel>> GetAllPersonnelAsync();
    Task<Personnel?> GetPersonnelByIdAsync(Guid personnelId);
    Task<Personnel?> GetPersonnelByEmployeeNumberAsync(string employeeNumber);
    Task<IEnumerable<Personnel>> SearchPersonnelAsync(string searchTerm);
    Task<IEnumerable<Personnel>> GetPersonnelByDepartmentAsync(Department department);
    Task<IEnumerable<Personnel>> GetActivePersonnelAsync();
    Task<IEnumerable<Personnel>> GetOffshorePersonnelAsync();
    Task<Personnel> CreatePersonnelAsync(Personnel personnel);
    Task<Personnel> UpdatePersonnelAsync(Personnel personnel);
    Task<bool> DeletePersonnelAsync(Guid personnelId);

    // Vessel Assignments
    Task<IEnumerable<VesselAssignment>> GetVesselAssignmentsAsync(Guid personnelId);
    Task<IEnumerable<VesselAssignment>> GetActiveVesselAssignmentsAsync();
    Task<IEnumerable<VesselAssignment>> GetUpcomingAssignmentsAsync(int daysAhead = 30);
    Task<VesselAssignment> CreateVesselAssignmentAsync(VesselAssignment assignment);
    Task<VesselAssignment> UpdateVesselAssignmentAsync(VesselAssignment assignment);
    Task<bool> SignOnAsync(Guid assignmentId, DateTime signOnDateTime, string? location, string? port);
    Task<bool> SignOffAsync(Guid assignmentId, DateTime signOffDateTime, string? location, string? port, string? reason);

    // Certifications
    Task<IEnumerable<PersonnelCertification>> GetPersonnelCertificationsAsync(Guid personnelId);
    Task<IEnumerable<PersonnelCertification>> GetExpiringCertificationsAsync(int daysAhead = 30);
    Task<PersonnelCertification> AddCertificationAsync(PersonnelCertification certification);
    Task<PersonnelCertification> UpdateCertificationAsync(PersonnelCertification certification);

    // Statistics
    Task<int> GetTotalPersonnelCountAsync();
    Task<int> GetActivePersonnelCountAsync();
    Task<int> GetOffshorePersonnelCountAsync();
}
