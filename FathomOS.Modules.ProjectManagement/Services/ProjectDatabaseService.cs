using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.ProjectManagement.Data;
using FathomOS.Modules.ProjectManagement.Models;

namespace FathomOS.Modules.ProjectManagement.Services;

/// <summary>
/// SQLite database operations for Project Management module
/// </summary>
public class ProjectDatabaseService : IDisposable
{
    private readonly ProjectDbContext _context;
    private bool _disposed;

    public ProjectDatabaseService()
    {
        _context = new ProjectDbContext();
    }

    public ProjectDatabaseService(string dbPath)
    {
        _context = new ProjectDbContext(dbPath);
    }

    /// <summary>
    /// Initialize the database (create if not exists, apply migrations) - SYNCHRONOUS
    /// Use InitializeAsync for non-blocking initialization.
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
    /// Seed reference data (sample clients, project types)
    /// </summary>
    private void SeedReferenceData()
    {
        // Seed sample clients if empty
        if (!_context.Clients.Any())
        {
            var clients = new List<Client>
            {
                new()
                {
                    ClientCode = "CLI-0001",
                    CompanyName = "Sample Oil & Gas Company",
                    ShortName = "SOG",
                    ClientType = ClientType.OilAndGas,
                    Country = "United Arab Emirates",
                    IsActive = true
                },
                new()
                {
                    ClientCode = "CLI-0002",
                    CompanyName = "Sample Offshore Wind Developer",
                    ShortName = "SOWD",
                    ClientType = ClientType.OffshoreWind,
                    Country = "United Kingdom",
                    IsActive = true
                },
                new()
                {
                    ClientCode = "CLI-0003",
                    CompanyName = "Sample Telecommunications Corp",
                    ShortName = "STC",
                    ClientType = ClientType.Telecommunications,
                    Country = "Singapore",
                    IsActive = true
                },
            };
            _context.Clients.AddRange(clients);
            _context.SaveChanges();
        }
    }

    /// <summary>
    /// Get the database context
    /// </summary>
    public ProjectDbContext Context => _context;

    /// <summary>
    /// Get project service
    /// </summary>
    public IProjectService GetProjectService() => new ProjectService(_context);

    /// <summary>
    /// Get project assignment service
    /// </summary>
    public IProjectAssignmentService GetAssignmentService() => new ProjectAssignmentService(_context);

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
