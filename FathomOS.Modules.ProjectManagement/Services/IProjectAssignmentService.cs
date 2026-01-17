using FathomOS.Modules.ProjectManagement.Models;

namespace FathomOS.Modules.ProjectManagement.Services;

/// <summary>
/// Interface for managing project resource assignments (vessels, equipment, personnel)
/// </summary>
public interface IProjectAssignmentService
{
    // Vessel Assignments
    Task<IEnumerable<ProjectVesselAssignment>> GetProjectVesselAssignmentsAsync(Guid projectId);
    Task<IEnumerable<ProjectVesselAssignment>> GetVesselProjectsAsync(Guid vesselId);
    Task<ProjectVesselAssignment> AssignVesselAsync(ProjectVesselAssignment assignment);
    Task<ProjectVesselAssignment> UpdateVesselAssignmentAsync(ProjectVesselAssignment assignment);
    Task<bool> RemoveVesselAssignmentAsync(Guid assignmentId);

    // Equipment Assignments
    Task<IEnumerable<ProjectEquipmentAssignment>> GetProjectEquipmentAssignmentsAsync(Guid projectId);
    Task<IEnumerable<ProjectEquipmentAssignment>> GetEquipmentProjectsAsync(Guid equipmentId);
    Task<ProjectEquipmentAssignment> AssignEquipmentAsync(ProjectEquipmentAssignment assignment);
    Task<ProjectEquipmentAssignment> UpdateEquipmentAssignmentAsync(ProjectEquipmentAssignment assignment);
    Task<bool> RemoveEquipmentAssignmentAsync(Guid assignmentId);

    // Personnel Assignments
    Task<IEnumerable<ProjectPersonnelAssignment>> GetProjectPersonnelAssignmentsAsync(Guid projectId);
    Task<IEnumerable<ProjectPersonnelAssignment>> GetPersonnelProjectsAsync(Guid personnelId);
    Task<ProjectPersonnelAssignment> AssignPersonnelAsync(ProjectPersonnelAssignment assignment);
    Task<ProjectPersonnelAssignment> UpdatePersonnelAssignmentAsync(ProjectPersonnelAssignment assignment);
    Task<bool> RemovePersonnelAssignmentAsync(Guid assignmentId);

    // Availability
    Task<bool> IsVesselAvailableAsync(Guid vesselId, DateTime startDate, DateTime endDate, Guid? excludeProjectId = null);
    Task<bool> IsEquipmentAvailableAsync(Guid equipmentId, DateTime startDate, DateTime endDate, Guid? excludeProjectId = null);
    Task<bool> IsPersonnelAvailableAsync(Guid personnelId, DateTime startDate, DateTime endDate, Guid? excludeProjectId = null);
}
