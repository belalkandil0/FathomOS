using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.PersonnelManagement.Data;
using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// SQLite database operations for Personnel Management module
/// </summary>
public class PersonnelDatabaseService : IDisposable
{
    private readonly PersonnelDbContext _context;
    private bool _disposed;

    public PersonnelDatabaseService()
    {
        _context = new PersonnelDbContext();
    }

    public PersonnelDatabaseService(string dbPath)
    {
        _context = new PersonnelDbContext(dbPath);
    }

    /// <summary>
    /// Initialize the database (create if not exists, apply migrations) - SYNCHRONOUS
    /// Prefer InitializeAsync() for non-blocking initialization.
    /// </summary>
    public void Initialize()
    {
        _context.Database.EnsureCreated();
        SeedReferenceData();
    }

    /// <summary>
    /// Initialize the database asynchronously (non-blocking)
    /// </summary>
    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            _context.Database.EnsureCreated();
            SeedReferenceData();
        });
    }

    /// <summary>
    /// Seed reference data (positions, rotation patterns, certification types)
    /// </summary>
    private void SeedReferenceData()
    {
        // Seed positions if empty
        if (!_context.Positions.Any())
        {
            var positions = new List<Position>
            {
                new() { Code = "PM", Title = "Project Manager", Department = Department.Management, IsActive = true },
                new() { Code = "PC", Title = "Party Chief", Department = Department.Survey, IsActive = true },
                new() { Code = "SNR-SUR", Title = "Senior Surveyor", Department = Department.Survey, IsActive = true },
                new() { Code = "SUR", Title = "Surveyor", Department = Department.Survey, IsActive = true },
                new() { Code = "JNR-SUR", Title = "Junior Surveyor", Department = Department.Survey, IsActive = true },
                new() { Code = "DP", Title = "Data Processor", Department = Department.Survey, IsActive = true },
                new() { Code = "ROV-SUP", Title = "ROV Supervisor", Department = Department.ROV, IsActive = true },
                new() { Code = "ROV-PLT", Title = "ROV Pilot", Department = Department.ROV, IsActive = true },
                new() { Code = "ELEC", Title = "Electronics Technician", Department = Department.Technical, IsActive = true },
                new() { Code = "MECH", Title = "Mechanical Technician", Department = Department.Technical, IsActive = true },
                new() { Code = "HSE", Title = "HSE Officer", Department = Department.QHSE, IsActive = true },
            };
            _context.Positions.AddRange(positions);
        }

        // Seed rotation patterns if empty
        if (!_context.RotationPatterns.Any())
        {
            var rotations = new List<RotationPattern>
            {
                new() { Code = "28-28", Name = "28 Days On / 28 Days Off", DaysOn = 28, DaysOff = 28, RotationType = RotationType.EqualRotation, IsActive = true },
                new() { Code = "21-21", Name = "21 Days On / 21 Days Off", DaysOn = 21, DaysOff = 21, RotationType = RotationType.EqualRotation, IsActive = true },
                new() { Code = "14-14", Name = "14 Days On / 14 Days Off", DaysOn = 14, DaysOff = 14, RotationType = RotationType.EqualRotation, IsActive = true },
                new() { Code = "B2B", Name = "Back-to-Back", DaysOn = 56, DaysOff = 28, RotationType = RotationType.BackToBack, IsActive = true },
                new() { Code = "PROJ", Name = "Project Based", DaysOn = 0, DaysOff = 0, RotationType = RotationType.ProjectBased, IsActive = true },
                new() { Code = "SHORE", Name = "Shore Based", DaysOn = 0, DaysOff = 0, RotationType = RotationType.ShoreBased, IsActive = true },
            };
            _context.RotationPatterns.AddRange(rotations);
        }

        // Seed certification types if empty
        if (!_context.CertificationTypes.Any())
        {
            var certTypes = new List<CertificationType>
            {
                new() { Code = "BOSIET", Name = "BOSIET (Basic Offshore Safety Induction & Emergency Training)", Category = CertificationCategory.OffshoreSurvival, ValidityMonths = 48, IsMandatory = true, IsActive = true },
                new() { Code = "FOET", Name = "FOET (Further Offshore Emergency Training)", Category = CertificationCategory.OffshoreSurvival, ValidityMonths = 48, IsMandatory = true, IsActive = true },
                new() { Code = "HUET", Name = "HUET (Helicopter Underwater Escape Training)", Category = CertificationCategory.OffshoreSurvival, ValidityMonths = 48, IsMandatory = true, IsActive = true },
                new() { Code = "MED", Name = "Offshore Medical Fitness Certificate", Category = CertificationCategory.Medical, ValidityMonths = 24, IsMandatory = true, IsActive = true },
                new() { Code = "STCW-PST", Name = "STCW Personal Safety Techniques", Category = CertificationCategory.STCW, ValidityMonths = 60, IsMandatory = true, IsActive = true },
                new() { Code = "STCW-FPFF", Name = "STCW Fire Prevention & Fire Fighting", Category = CertificationCategory.STCW, ValidityMonths = 60, IsMandatory = true, IsActive = true },
                new() { Code = "STCW-EFA", Name = "STCW Elementary First Aid", Category = CertificationCategory.STCW, ValidityMonths = 60, IsMandatory = true, IsActive = true },
                new() { Code = "STCW-SSR", Name = "STCW Personal Survival Techniques", Category = CertificationCategory.STCW, ValidityMonths = 60, IsMandatory = true, IsActive = true },
                new() { Code = "H2S", Name = "H2S Awareness", Category = CertificationCategory.Safety, ValidityMonths = 24, IsMandatory = false, IsActive = true },
                new() { Code = "RIGGING", Name = "Rigging & Slinging", Category = CertificationCategory.Technical, ValidityMonths = 36, IsMandatory = false, IsActive = true },
            };
            _context.CertificationTypes.AddRange(certTypes);
        }

        _context.SaveChanges();
    }

    /// <summary>
    /// Get the database context
    /// </summary>
    public PersonnelDbContext Context => _context;

    /// <summary>
    /// Get personnel service
    /// </summary>
    public IPersonnelService GetPersonnelService() => new PersonnelService(_context);

    /// <summary>
    /// Get timesheet service
    /// </summary>
    public ITimesheetService GetTimesheetService() => new TimesheetService(_context);

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
