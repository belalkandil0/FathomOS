// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/DprReport.cs
// Purpose: Daily Progress Report (DPR) model - comprehensive shift documentation
// ============================================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Represents a Daily Progress Report (DPR) and Shift Handover document.
/// Captures all sections from the standard DPR Word template.
/// </summary>
public class DprReport : INotifyPropertyChanged
{
    private Guid _id;
    private DateTime _reportDate;
    
    // Header fields (auto-populated from project)
    private string _client = string.Empty;
    private string _vessel = string.Empty;
    private string _projectNumber = string.Empty;
    private string _locationDepth = string.Empty;
    private string _offshoreManager = string.Empty;
    private string _projectSurveyor = string.Empty;
    private string _partyChief = string.Empty;
    
    // Text sections
    private string _last24HrsHighlights = string.Empty;
    private string _knownIssues = string.Empty;
    private string _generalSurveyComments = string.Empty;
    private string _surveyTasksToComplete = string.Empty;
    private string _projectInformation = string.Empty;
    private string _speedOfSoundInfo = string.Empty;
    private string _mocsIssued = string.Empty;
    private string _crewComments = string.Empty;
    private string _surveyEquipmentIssues = string.Empty;
    private string _thirdPartyEquipmentIssues = string.Empty;
    private string _itemsWetStored = string.Empty;
    private string _hseNotes = string.Empty;
    private string _weatherConditions = string.Empty;
    private string _dataManagement = string.Empty;
    private string _materialRequests = string.Empty;
    
    // ========================================================================
    // Identity & Header
    // ========================================================================
    
    /// <summary>
    /// Unique identifier for this DPR.
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    /// <summary>
    /// Date of the report.
    /// </summary>
    public DateTime ReportDate
    {
        get => _reportDate;
        set => SetProperty(ref _reportDate, value);
    }
    
    /// <summary>
    /// Client name (auto-populated from project).
    /// </summary>
    public string Client
    {
        get => _client;
        set => SetProperty(ref _client, value ?? string.Empty);
    }
    
    /// <summary>
    /// Vessel name (auto-populated from project).
    /// </summary>
    public string Vessel
    {
        get => _vessel;
        set => SetProperty(ref _vessel, value ?? string.Empty);
    }
    
    /// <summary>
    /// Project number (auto-populated from project).
    /// </summary>
    public string ProjectNumber
    {
        get => _projectNumber;
        set => SetProperty(ref _projectNumber, value ?? string.Empty);
    }
    
    /// <summary>
    /// Location and water depth.
    /// </summary>
    public string LocationDepth
    {
        get => _locationDepth;
        set => SetProperty(ref _locationDepth, value ?? string.Empty);
    }
    
    /// <summary>
    /// Offshore manager name.
    /// </summary>
    public string OffshoreManager
    {
        get => _offshoreManager;
        set => SetProperty(ref _offshoreManager, value ?? string.Empty);
    }
    
    /// <summary>
    /// Project surveyor name.
    /// </summary>
    public string ProjectSurveyor
    {
        get => _projectSurveyor;
        set => SetProperty(ref _projectSurveyor, value ?? string.Empty);
    }
    
    /// <summary>
    /// Party chief name.
    /// </summary>
    public string PartyChief
    {
        get => _partyChief;
        set => SetProperty(ref _partyChief, value ?? string.Empty);
    }
    
    private string _shift = string.Empty;
    private string _reportNumber = string.Empty;
    private List<string> _crewOnDuty = new();
    
    /// <summary>
    /// Shift identifier (e.g., "Day", "Night", "A", "B").
    /// </summary>
    public string Shift
    {
        get => _shift;
        set => SetProperty(ref _shift, value ?? string.Empty);
    }
    
    /// <summary>
    /// Report number/identifier.
    /// </summary>
    public string ReportNumber
    {
        get => _reportNumber;
        set => SetProperty(ref _reportNumber, value ?? string.Empty);
    }
    
    /// <summary>
    /// List of crew members on duty for this shift.
    /// </summary>
    public List<string> CrewOnDuty
    {
        get => _crewOnDuty;
        set => SetProperty(ref _crewOnDuty, value ?? new List<string>());
    }
    
    // ========================================================================
    // Daily Log (Time-based entries)
    // ========================================================================
    
    /// <summary>
    /// Time-based daily log entries (0000, 0600, 1200, etc.).
    /// </summary>
    public ObservableCollection<DailyLogEntry> DailyLog { get; set; } = new();
    
    // ========================================================================
    // Text Sections
    // ========================================================================
    
    /// <summary>
    /// Last 24hrs highlights summary.
    /// </summary>
    public string Last24HrsHighlights
    {
        get => _last24HrsHighlights;
        set => SetProperty(ref _last24HrsHighlights, value ?? string.Empty);
    }
    
    /// <summary>
    /// Known issues and problems.
    /// </summary>
    public string KnownIssues
    {
        get => _knownIssues;
        set => SetProperty(ref _knownIssues, value ?? string.Empty);
    }
    
    /// <summary>
    /// General survey comments.
    /// </summary>
    public string GeneralSurveyComments
    {
        get => _generalSurveyComments;
        set => SetProperty(ref _generalSurveyComments, value ?? string.Empty);
    }
    
    /// <summary>
    /// Survey tasks to complete.
    /// </summary>
    public string SurveyTasksToComplete
    {
        get => _surveyTasksToComplete;
        set => SetProperty(ref _surveyTasksToComplete, value ?? string.Empty);
    }
    
    /// <summary>
    /// Project information section.
    /// </summary>
    public string ProjectInformation
    {
        get => _projectInformation;
        set => SetProperty(ref _projectInformation, value ?? string.Empty);
    }
    
    /// <summary>
    /// Current speed of sound information.
    /// </summary>
    public string SpeedOfSoundInfo
    {
        get => _speedOfSoundInfo;
        set => SetProperty(ref _speedOfSoundInfo, value ?? string.Empty);
    }
    
    /// <summary>
    /// MOCs (Management of Change) issued.
    /// </summary>
    public string MocsIssued
    {
        get => _mocsIssued;
        set => SetProperty(ref _mocsIssued, value ?? string.Empty);
    }
    
    /// <summary>
    /// Crew status comments.
    /// </summary>
    public string CrewComments
    {
        get => _crewComments;
        set => SetProperty(ref _crewComments, value ?? string.Empty);
    }
    
    /// <summary>
    /// Subsea 7 survey equipment issues and fault reports.
    /// </summary>
    public string SurveyEquipmentIssues
    {
        get => _surveyEquipmentIssues;
        set => SetProperty(ref _surveyEquipmentIssues, value ?? string.Empty);
    }
    
    /// <summary>
    /// Third party equipment issues (ROV, Speedcast, etc.).
    /// </summary>
    public string ThirdPartyEquipmentIssues
    {
        get => _thirdPartyEquipmentIssues;
        set => SetProperty(ref _thirdPartyEquipmentIssues, value ?? string.Empty);
    }
    
    /// <summary>
    /// Items wet stored on seabed.
    /// </summary>
    public string ItemsWetStored
    {
        get => _itemsWetStored;
        set => SetProperty(ref _itemsWetStored, value ?? string.Empty);
    }
    
    /// <summary>
    /// HSE (Health, Safety, Environment) notes.
    /// </summary>
    public string HseNotes
    {
        get => _hseNotes;
        set => SetProperty(ref _hseNotes, value ?? string.Empty);
    }
    
    /// <summary>
    /// Weather conditions (local time).
    /// </summary>
    public string WeatherConditions
    {
        get => _weatherConditions;
        set => SetProperty(ref _weatherConditions, value ?? string.Empty);
    }
    
    /// <summary>
    /// Data management status.
    /// </summary>
    public string DataManagement
    {
        get => _dataManagement;
        set => SetProperty(ref _dataManagement, value ?? string.Empty);
    }
    
    /// <summary>
    /// Material requests.
    /// </summary>
    public string MaterialRequests
    {
        get => _materialRequests;
        set => SetProperty(ref _materialRequests, value ?? string.Empty);
    }
    
    // ========================================================================
    // Collections (Tables)
    // ========================================================================
    
    /// <summary>
    /// Survey crew roster.
    /// </summary>
    public ObservableCollection<CrewMember> SurveyCrew { get; set; } = new();
    
    /// <summary>
    /// Alias for SurveyCrew property (for compatibility).
    /// </summary>
    public ObservableCollection<CrewMember> CrewMembers
    {
        get => SurveyCrew;
        set => SurveyCrew = value;
    }
    
    /// <summary>
    /// Transponder management table.
    /// </summary>
    public ObservableCollection<TransponderInfo> Transponders { get; set; } = new();
    
    /// <summary>
    /// Subsea equipment management table.
    /// </summary>
    public ObservableCollection<SubseaEquipment> SubseaEquipment { get; set; } = new();
    
    /// <summary>
    /// Field report status table.
    /// </summary>
    public ObservableCollection<FieldReport> FieldReports { get; set; } = new();
    
    /// <summary>
    /// Operational status breakdown.
    /// </summary>
    public OperationalStatus OperationalStatus { get; set; } = new();
    
    // ========================================================================
    // Computed Properties
    // ========================================================================
    
    /// <summary>
    /// Gets formatted report date string.
    /// </summary>
    public string ReportDateDisplay => ReportDate.ToString("dd MMMM yyyy");
    
    /// <summary>
    /// Gets the report title.
    /// </summary>
    public string Title => $"DPR - {ReportDateDisplay}";
    
    /// <summary>
    /// Gets total crew count.
    /// </summary>
    public int CrewCount => SurveyCrew?.Count ?? 0;
    
    /// <summary>
    /// Gets total transponder count.
    /// </summary>
    public int TransponderCount => Transponders?.Count ?? 0;
    
    // ========================================================================
    // Constructors
    // ========================================================================
    
    /// <summary>
    /// Creates a new DPR with generated ID and today's date.
    /// </summary>
    public DprReport()
    {
        _id = Guid.NewGuid();
        _reportDate = DateTime.Today;
        InitializeDefaultEntries();
    }
    
    /// <summary>
    /// Creates a new DPR for a specific date.
    /// </summary>
    public DprReport(DateTime date) : this()
    {
        _reportDate = date;
    }
    
    /// <summary>
    /// Creates a new DPR with project info auto-populated.
    /// </summary>
    public DprReport(ProjectInfo project) : this()
    {
        PopulateFromProject(project);
    }
    
    // ========================================================================
    // Methods
    // ========================================================================
    
    /// <summary>
    /// Initializes default daily log time entries.
    /// </summary>
    public void InitializeDefaultEntries()
    {
        DailyLog = new ObservableCollection<DailyLogEntry>
        {
            new DailyLogEntry { Time = new TimeSpan(0, 0, 0), Activity = "Midnight position:" },
            new DailyLogEntry { Time = new TimeSpan(6, 0, 0), Activity = "" },
            new DailyLogEntry { Time = new TimeSpan(12, 0, 0), Activity = "" },
            new DailyLogEntry { Time = new TimeSpan(18, 0, 0), Activity = "" },
            new DailyLogEntry { Time = new TimeSpan(23, 59, 0), Activity = "" }
        };
    }
    
    /// <summary>
    /// Populates header fields from project info.
    /// </summary>
    public void PopulateFromProject(ProjectInfo project)
    {
        if (project == null) return;
        
        Client = project.Client;
        Vessel = project.Vessel;
        ProjectNumber = project.ProjectNumber;
        LocationDepth = project.Location;
        OffshoreManager = project.OffshoreManager;
        ProjectSurveyor = project.ProjectSurveyor;
        PartyChief = project.PartyChief;
    }
    
    /// <summary>
    /// Adds a daily log entry.
    /// </summary>
    public void AddDailyLogEntry(TimeSpan time, string activity)
    {
        DailyLog ??= new ObservableCollection<DailyLogEntry>();
        DailyLog.Add(new DailyLogEntry { Time = time, Activity = activity });
        
        // Sort by time
        var sorted = DailyLog.OrderBy(e => e.Time).ToList();
        DailyLog.Clear();
        foreach (var entry in sorted)
            DailyLog.Add(entry);
    }
    
    /// <summary>
    /// Adds a crew member.
    /// </summary>
    public void AddCrewMember(CrewMember crew)
    {
        SurveyCrew ??= new ObservableCollection<CrewMember>();
        SurveyCrew.Add(crew);
    }
    
    /// <summary>
    /// Adds a transponder.
    /// </summary>
    public void AddTransponder(TransponderInfo transponder)
    {
        Transponders ??= new ObservableCollection<TransponderInfo>();
        Transponders.Add(transponder);
    }
    
    /// <summary>
    /// Creates a deep copy of this DPR.
    /// </summary>
    public DprReport Clone()
    {
        var clone = new DprReport
        {
            _reportDate = this._reportDate,
            _client = this._client,
            _vessel = this._vessel,
            _projectNumber = this._projectNumber,
            _locationDepth = this._locationDepth,
            _offshoreManager = this._offshoreManager,
            _projectSurveyor = this._projectSurveyor,
            _partyChief = this._partyChief,
            _last24HrsHighlights = this._last24HrsHighlights,
            _knownIssues = this._knownIssues,
            _generalSurveyComments = this._generalSurveyComments,
            _surveyTasksToComplete = this._surveyTasksToComplete,
            _projectInformation = this._projectInformation,
            _speedOfSoundInfo = this._speedOfSoundInfo,
            _mocsIssued = this._mocsIssued,
            _crewComments = this._crewComments,
            _surveyEquipmentIssues = this._surveyEquipmentIssues,
            _thirdPartyEquipmentIssues = this._thirdPartyEquipmentIssues,
            _itemsWetStored = this._itemsWetStored,
            _hseNotes = this._hseNotes,
            _weatherConditions = this._weatherConditions,
            _dataManagement = this._dataManagement,
            _materialRequests = this._materialRequests
        };
        
        // Clone collections
        foreach (var entry in DailyLog)
            clone.DailyLog.Add(new DailyLogEntry { Time = entry.Time, Activity = entry.Activity });
        
        foreach (var crew in SurveyCrew)
            clone.SurveyCrew.Add(crew.Clone());
        
        foreach (var txp in Transponders)
            clone.Transponders.Add(txp.Clone());
        
        foreach (var eq in SubseaEquipment)
            clone.SubseaEquipment.Add(eq.Clone());
        
        foreach (var report in FieldReports)
            clone.FieldReports.Add(report.Clone());
        
        clone.OperationalStatus = OperationalStatus.Clone();
        
        return clone;
    }
    
    // ========================================================================
    // INotifyPropertyChanged Implementation
    // ========================================================================
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// Represents a single time-based entry in the daily log.
/// </summary>
public class DailyLogEntry : INotifyPropertyChanged
{
    private TimeSpan _time;
    private string _activity = string.Empty;
    
    public TimeSpan Time
    {
        get => _time;
        set
        {
            _time = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TimeDisplay));
        }
    }
    
    public string Activity
    {
        get => _activity;
        set
        {
            _activity = value ?? string.Empty;
            OnPropertyChanged();
        }
    }
    
    public string TimeDisplay => Time.ToString(@"hhmm");
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
