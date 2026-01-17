namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Project status values - tracks the overall lifecycle of a project
/// </summary>
public enum ProjectStatus
{
    /// <summary>Project is in initial planning phase</summary>
    Draft,

    /// <summary>Project proposal submitted for approval</summary>
    Proposed,

    /// <summary>Project approved and in detailed planning</summary>
    Planning,

    /// <summary>Project is actively being executed</summary>
    Active,

    /// <summary>Project temporarily suspended</summary>
    OnHold,

    /// <summary>Project successfully completed</summary>
    Completed,

    /// <summary>Project cancelled before completion</summary>
    Cancelled,

    /// <summary>Project closed after completion/cancellation</summary>
    Closed
}

/// <summary>
/// Project phase values - tracks the current execution phase
/// </summary>
public enum ProjectPhase
{
    /// <summary>Initial project setup and planning</summary>
    Initiation,

    /// <summary>Mobilization of resources and equipment</summary>
    Mobilization,

    /// <summary>Survey operations in progress</summary>
    Operations,

    /// <summary>Data processing and analysis</summary>
    Processing,

    /// <summary>Deliverable preparation and reporting</summary>
    Reporting,

    /// <summary>Demobilization of resources</summary>
    Demobilization,

    /// <summary>Project closeout and handover</summary>
    Closeout
}

/// <summary>
/// Project type values - categorizes the type of survey project
/// </summary>
public enum ProjectType
{
    /// <summary>Hydrographic survey project</summary>
    Hydrographic,

    /// <summary>Geophysical survey project</summary>
    Geophysical,

    /// <summary>Geotechnical survey project</summary>
    Geotechnical,

    /// <summary>Environmental survey project</summary>
    Environmental,

    /// <summary>Positioning services project</summary>
    Positioning,

    /// <summary>ROV/AUV inspection project</summary>
    Inspection,

    /// <summary>Construction support project</summary>
    ConstructionSupport,

    /// <summary>Cable/Pipeline route survey</summary>
    RouteSurvey,

    /// <summary>As-built survey project</summary>
    AsBuilt,

    /// <summary>Multi-discipline project</summary>
    MultiDiscipline,

    /// <summary>Other project type</summary>
    Other
}

/// <summary>
/// Vessel role values - defines the role of a vessel in a project
/// </summary>
public enum VesselRole
{
    /// <summary>Primary survey vessel</summary>
    Primary,

    /// <summary>Secondary/backup survey vessel</summary>
    Secondary,

    /// <summary>Support vessel</summary>
    Support,

    /// <summary>Chase/guard vessel</summary>
    Chase,

    /// <summary>Crew transfer vessel</summary>
    CrewTransfer,

    /// <summary>Equipment tender vessel</summary>
    Tender
}

/// <summary>
/// Equipment role values - defines the role of equipment in a project
/// </summary>
public enum EquipmentRole
{
    /// <summary>Primary equipment for the task</summary>
    Primary,

    /// <summary>Secondary/backup equipment</summary>
    Secondary,

    /// <summary>Spare equipment</summary>
    Spare,

    /// <summary>Support equipment</summary>
    Support,

    /// <summary>Calibration equipment</summary>
    Calibration
}

/// <summary>
/// Personnel role values - defines the role of personnel in a project
/// </summary>
public enum PersonnelRole
{
    /// <summary>Project Manager</summary>
    ProjectManager,

    /// <summary>Party Chief / Offshore Manager</summary>
    PartyChief,

    /// <summary>Survey Party Chief</summary>
    SurveyPartyChief,

    /// <summary>Senior Surveyor</summary>
    SeniorSurveyor,

    /// <summary>Surveyor</summary>
    Surveyor,

    /// <summary>Junior Surveyor</summary>
    JuniorSurveyor,

    /// <summary>Data Processor</summary>
    DataProcessor,

    /// <summary>ROV Pilot</summary>
    ROVPilot,

    /// <summary>ROV Supervisor</summary>
    ROVSupervisor,

    /// <summary>Electronics Technician</summary>
    ElectronicsTechnician,

    /// <summary>Mechanical Technician</summary>
    MechanicalTechnician,

    /// <summary>HSE Officer</summary>
    HSEOfficer,

    /// <summary>Client Representative</summary>
    ClientRepresentative,

    /// <summary>Other role</summary>
    Other
}

/// <summary>
/// Milestone status values - tracks the status of project milestones
/// </summary>
public enum MilestoneStatus
{
    /// <summary>Milestone not yet started</summary>
    Pending,

    /// <summary>Milestone work in progress</summary>
    InProgress,

    /// <summary>Milestone completed</summary>
    Completed,

    /// <summary>Milestone delayed</summary>
    Delayed,

    /// <summary>Milestone cancelled</summary>
    Cancelled,

    /// <summary>Milestone on hold</summary>
    OnHold
}

/// <summary>
/// Milestone type values - categorizes milestones
/// </summary>
public enum MilestoneType
{
    /// <summary>Project start milestone</summary>
    ProjectStart,

    /// <summary>Mobilization milestone</summary>
    Mobilization,

    /// <summary>Survey commencement</summary>
    SurveyStart,

    /// <summary>Survey completion</summary>
    SurveyComplete,

    /// <summary>Data delivery milestone</summary>
    DataDelivery,

    /// <summary>Report delivery milestone</summary>
    ReportDelivery,

    /// <summary>Demobilization milestone</summary>
    Demobilization,

    /// <summary>Project completion</summary>
    ProjectComplete,

    /// <summary>Payment milestone</summary>
    Payment,

    /// <summary>Client review milestone</summary>
    ClientReview,

    /// <summary>Custom milestone</summary>
    Custom
}

/// <summary>
/// Deliverable status values - tracks the status of project deliverables
/// </summary>
public enum DeliverableStatus
{
    /// <summary>Deliverable not started</summary>
    NotStarted,

    /// <summary>Deliverable in preparation</summary>
    InProgress,

    /// <summary>Deliverable under internal review</summary>
    UnderReview,

    /// <summary>Deliverable pending client approval</summary>
    PendingApproval,

    /// <summary>Deliverable approved by client</summary>
    Approved,

    /// <summary>Deliverable rejected, requires revision</summary>
    Rejected,

    /// <summary>Deliverable submitted to client</summary>
    Submitted,

    /// <summary>Deliverable accepted and closed</summary>
    Accepted
}

/// <summary>
/// Deliverable type values - categorizes deliverables
/// </summary>
public enum DeliverableType
{
    /// <summary>Raw survey data</summary>
    RawData,

    /// <summary>Processed data</summary>
    ProcessedData,

    /// <summary>Daily Progress Report</summary>
    DailyReport,

    /// <summary>Weekly Progress Report</summary>
    WeeklyReport,

    /// <summary>Final Survey Report</summary>
    FinalReport,

    /// <summary>Chart or map deliverable</summary>
    Chart,

    /// <summary>CAD drawings</summary>
    CADDrawings,

    /// <summary>GIS data package</summary>
    GISData,

    /// <summary>As-built documentation</summary>
    AsBuiltDocumentation,

    /// <summary>Inspection report</summary>
    InspectionReport,

    /// <summary>Certificate of completion</summary>
    Certificate,

    /// <summary>Other deliverable type</summary>
    Other
}

/// <summary>
/// Deliverable format values - specifies the format of deliverables
/// </summary>
public enum DeliverableFormat
{
    /// <summary>PDF document</summary>
    PDF,

    /// <summary>Microsoft Word document</summary>
    Word,

    /// <summary>Microsoft Excel spreadsheet</summary>
    Excel,

    /// <summary>AutoCAD DWG file</summary>
    DWG,

    /// <summary>AutoCAD DXF file</summary>
    DXF,

    /// <summary>Shapefile</summary>
    Shapefile,

    /// <summary>GeoTIFF image</summary>
    GeoTIFF,

    /// <summary>XYZ data file</summary>
    XYZ,

    /// <summary>LAS point cloud</summary>
    LAS,

    /// <summary>SEG-Y seismic data</summary>
    SEGY,

    /// <summary>Compressed archive (ZIP)</summary>
    ZIP,

    /// <summary>Other format</summary>
    Other
}

/// <summary>
/// Client type values - categorizes clients
/// </summary>
public enum ClientType
{
    /// <summary>Oil and gas company</summary>
    OilAndGas,

    /// <summary>Offshore wind developer</summary>
    OffshoreWind,

    /// <summary>Telecommunications company</summary>
    Telecommunications,

    /// <summary>Government agency</summary>
    Government,

    /// <summary>Port authority</summary>
    PortAuthority,

    /// <summary>Dredging contractor</summary>
    Dredging,

    /// <summary>Construction contractor</summary>
    Construction,

    /// <summary>Engineering consultant</summary>
    Engineering,

    /// <summary>Research institution</summary>
    Research,

    /// <summary>Other client type</summary>
    Other
}

/// <summary>
/// Contact type values - categorizes client contacts
/// </summary>
public enum ContactType
{
    /// <summary>Primary business contact</summary>
    Primary,

    /// <summary>Technical contact</summary>
    Technical,

    /// <summary>Commercial/contracts contact</summary>
    Commercial,

    /// <summary>Finance/invoicing contact</summary>
    Finance,

    /// <summary>Project-specific contact</summary>
    Project,

    /// <summary>HSE contact</summary>
    HSE,

    /// <summary>Emergency contact</summary>
    Emergency
}

/// <summary>
/// Assignment status values - tracks the status of resource assignments
/// </summary>
public enum AssignmentStatus
{
    /// <summary>Assignment proposed but not confirmed</summary>
    Proposed,

    /// <summary>Assignment confirmed</summary>
    Confirmed,

    /// <summary>Resource is active on the project</summary>
    Active,

    /// <summary>Assignment completed</summary>
    Completed,

    /// <summary>Assignment cancelled</summary>
    Cancelled
}

/// <summary>
/// Priority level values - used for milestones and deliverables
/// </summary>
public enum PriorityLevel
{
    /// <summary>Low priority</summary>
    Low,

    /// <summary>Normal priority</summary>
    Normal,

    /// <summary>High priority</summary>
    High,

    /// <summary>Critical priority</summary>
    Critical
}

/// <summary>
/// Currency codes commonly used in projects
/// </summary>
public enum CurrencyCode
{
    USD,
    EUR,
    GBP,
    AED,
    SAR,
    SGD,
    AUD,
    NOK,
    MYR,
    BRL,
    INR
}

/// <summary>
/// Billing type values - defines how a project is billed
/// </summary>
public enum BillingType
{
    /// <summary>Fixed price contract</summary>
    FixedPrice,

    /// <summary>Time and materials</summary>
    TimeAndMaterials,

    /// <summary>Day rate contract</summary>
    DayRate,

    /// <summary>Unit rate contract</summary>
    UnitRate,

    /// <summary>Cost plus contract</summary>
    CostPlus,

    /// <summary>Mixed billing</summary>
    Mixed
}
