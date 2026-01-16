// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/DprSupportModels.cs
// Purpose: Supporting models for DPR - Crew, Transponders, Equipment, etc.
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Represents a survey crew member.
/// </summary>
public class CrewMember : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _rank = string.Empty;
    private string _shift = string.Empty;
    private string _employer = string.Empty;
    private DateTime _dateOnBoard;
    
    public string Name
    {
        get => _name;
        set { _name = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string Rank
    {
        get => _rank;
        set { _rank = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string Shift
    {
        get => _shift;
        set { _shift = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string Employer
    {
        get => _employer;
        set { _employer = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public DateTime DateOnBoard
    {
        get => _dateOnBoard;
        set { _dateOnBoard = value; OnPropertyChanged(); OnPropertyChanged(nameof(DateOnBoardDisplay)); }
    }
    
    public string DateOnBoardDisplay => DateOnBoard.ToString("dd/MM/yyyy");
    
    public CrewMember Clone() => new()
    {
        Name = this.Name,
        Rank = this.Rank,
        Shift = this.Shift,
        Employer = this.Employer,
        DateOnBoard = this.DateOnBoard
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents transponder management information.
/// </summary>
public class TransponderInfo : INotifyPropertyChanged
{
    private string _location = string.Empty;
    private string _channel = string.Empty;
    private string _serialNumber = string.Empty;
    private DateTime _issuedDate;
    private DateTime _lastCharged;
    private DateTime _lastInspected;
    private string _inspectedBy = string.Empty;
    
    public string Location
    {
        get => _location;
        set { _location = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string Channel
    {
        get => _channel;
        set { _channel = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string SerialNumber
    {
        get => _serialNumber;
        set { _serialNumber = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public DateTime IssuedDate
    {
        get => _issuedDate;
        set { _issuedDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IssuedDisplay)); }
    }
    
    public DateTime LastCharged
    {
        get => _lastCharged;
        set { _lastCharged = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastChargedDisplay)); }
    }
    
    public DateTime LastInspected
    {
        get => _lastInspected;
        set { _lastInspected = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastInspectedDisplay)); }
    }
    
    public string InspectedBy
    {
        get => _inspectedBy;
        set { _inspectedBy = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string IssuedDisplay => IssuedDate.ToString("dd MMM");
    public string LastChargedDisplay => LastCharged.ToString("dd MMM");
    public string LastInspectedDisplay => LastInspected.ToString("dd MMM");
    
    public TransponderInfo Clone() => new()
    {
        Location = this.Location,
        Channel = this.Channel,
        SerialNumber = this.SerialNumber,
        IssuedDate = this.IssuedDate,
        LastCharged = this.LastCharged,
        LastInspected = this.LastInspected,
        InspectedBy = this.InspectedBy
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents subsea equipment management information.
/// </summary>
public class SubseaEquipment : INotifyPropertyChanged
{
    private string _sensorType = string.Empty;
    private string _location = string.Empty;
    private string _serialNumber = string.Empty;
    private DateTime _issuedDate;
    private DateTime _lastInspected;
    private string _inspectedBy = string.Empty;
    
    public string SensorType
    {
        get => _sensorType;
        set { _sensorType = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string Location
    {
        get => _location;
        set { _location = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string SerialNumber
    {
        get => _serialNumber;
        set { _serialNumber = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public DateTime IssuedDate
    {
        get => _issuedDate;
        set { _issuedDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IssuedDisplay)); }
    }
    
    public DateTime LastInspected
    {
        get => _lastInspected;
        set { _lastInspected = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastInspectedDisplay)); }
    }
    
    public string InspectedBy
    {
        get => _inspectedBy;
        set { _inspectedBy = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string IssuedDisplay => IssuedDate.ToString("dd MMM yyyy");
    public string LastInspectedDisplay => LastInspected.ToString("dd MMM yyyy");
    
    public SubseaEquipment Clone() => new()
    {
        SensorType = this.SensorType,
        Location = this.Location,
        SerialNumber = this.SerialNumber,
        IssuedDate = this.IssuedDate,
        LastInspected = this.LastInspected,
        InspectedBy = this.InspectedBy
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents field report status tracking.
/// </summary>
public class FieldReport : INotifyPropertyChanged
{
    private string _reportNumber = string.Empty;
    private string _reportTitle = string.Empty;
    private DateTime? _revAaIdcDate;
    private DateTime? _revAClientDate;
    private DateTime? _revBSignedDate;
    
    public string ReportNumber
    {
        get => _reportNumber;
        set { _reportNumber = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string ReportTitle
    {
        get => _reportTitle;
        set { _reportTitle = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public DateTime? RevAaIdcDate
    {
        get => _revAaIdcDate;
        set { _revAaIdcDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(RevAaIdcDisplay)); }
    }
    
    public DateTime? RevAClientDate
    {
        get => _revAClientDate;
        set { _revAClientDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(RevAClientDisplay)); }
    }
    
    public DateTime? RevBSignedDate
    {
        get => _revBSignedDate;
        set { _revBSignedDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(RevBSignedDisplay)); }
    }
    
    public string RevAaIdcDisplay => RevAaIdcDate?.ToString("dd/MM/yyyy") ?? "-";
    public string RevAClientDisplay => RevAClientDate?.ToString("dd/MM/yyyy") ?? "-";
    public string RevBSignedDisplay => RevBSignedDate?.ToString("dd/MM/yyyy") ?? "-";
    
    public FieldReport Clone() => new()
    {
        ReportNumber = this.ReportNumber,
        ReportTitle = this.ReportTitle,
        RevAaIdcDate = this.RevAaIdcDate,
        RevAClientDate = this.RevAClientDate,
        RevBSignedDate = this.RevBSignedDate
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents operational status hours breakdown.
/// </summary>
public class OperationalStatus : INotifyPropertyChanged
{
    private double _surveyOperational;
    private double _wow;
    private double _standbyOnClient;
    private double _downtimeSurvey;
    private double _downtimeOther;
    private string _comments = string.Empty;
    
    /// <summary>
    /// Hours survey was operational.
    /// </summary>
    public double SurveyOperational
    {
        get => _surveyOperational;
        set { _surveyOperational = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHours)); }
    }
    
    /// <summary>
    /// Weather standby hours (Waiting On Weather).
    /// </summary>
    public double Wow
    {
        get => _wow;
        set { _wow = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHours)); }
    }
    
    /// <summary>
    /// Standby on client hours.
    /// </summary>
    public double StandbyOnClient
    {
        get => _standbyOnClient;
        set { _standbyOnClient = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHours)); }
    }
    
    /// <summary>
    /// Survey equipment downtime hours.
    /// </summary>
    public double DowntimeSurvey
    {
        get => _downtimeSurvey;
        set { _downtimeSurvey = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHours)); }
    }
    
    /// <summary>
    /// Other downtime hours.
    /// </summary>
    public double DowntimeOther
    {
        get => _downtimeOther;
        set { _downtimeOther = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHours)); }
    }
    
    /// <summary>
    /// Additional comments.
    /// </summary>
    public string Comments
    {
        get => _comments;
        set { _comments = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Gets total hours (should equal 24 for a complete day).
    /// </summary>
    public double TotalHours => SurveyOperational + Wow + StandbyOnClient + DowntimeSurvey + DowntimeOther;
    
    /// <summary>
    /// Gets operational efficiency percentage.
    /// </summary>
    public double EfficiencyPercent => TotalHours > 0 ? (SurveyOperational / TotalHours) * 100 : 0;
    
    public OperationalStatus Clone() => new()
    {
        SurveyOperational = this.SurveyOperational,
        Wow = this.Wow,
        StandbyOnClient = this.StandbyOnClient,
        DowntimeSurvey = this.DowntimeSurvey,
        DowntimeOther = this.DowntimeOther,
        Comments = this.Comments
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents project configuration information.
/// Used for auto-populating DPR headers and export metadata.
/// </summary>
public class ProjectInfo : INotifyPropertyChanged
{
    private string _client = string.Empty;
    private string _vessel = string.Empty;
    private string _projectNumber = string.Empty;
    private string _projectName = string.Empty;
    private string _location = string.Empty;
    private string _offshoreManager = string.Empty;
    private string _projectSurveyor = string.Empty;
    private string _partyChief = string.Empty;
    private string _coordinateSystem = string.Empty;
    private DateTime _startDate;
    
    public string Client
    {
        get => _client;
        set { _client = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string Vessel
    {
        get => _vessel;
        set { _vessel = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string ProjectNumber
    {
        get => _projectNumber;
        set { _projectNumber = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string ProjectName
    {
        get => _projectName;
        set { _projectName = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string Location
    {
        get => _location;
        set { _location = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string OffshoreManager
    {
        get => _offshoreManager;
        set { _offshoreManager = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string ProjectSurveyor
    {
        get => _projectSurveyor;
        set { _projectSurveyor = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string PartyChief
    {
        get => _partyChief;
        set { _partyChief = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public string CoordinateSystem
    {
        get => _coordinateSystem;
        set { _coordinateSystem = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    public DateTime StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Creates a shallow copy of this ProjectInfo.
    /// </summary>
    /// <returns>A new ProjectInfo with copied values.</returns>
    public ProjectInfo Clone()
    {
        return (ProjectInfo)MemberwiseClone();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
