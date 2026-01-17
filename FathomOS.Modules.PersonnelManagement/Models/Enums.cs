namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Employment status values for personnel
/// </summary>
public enum EmploymentStatus
{
    /// <summary>Currently active employee</summary>
    Active,

    /// <summary>Employee on leave (vacation, sick, etc.)</summary>
    OnLeave,

    /// <summary>Employee suspended</summary>
    Suspended,

    /// <summary>Employment terminated</summary>
    Terminated,

    /// <summary>Employee resigned</summary>
    Resigned,

    /// <summary>Employee retired</summary>
    Retired,

    /// <summary>Contract expired</summary>
    ContractExpired,

    /// <summary>Pending onboarding</summary>
    PendingOnboarding
}

/// <summary>
/// Employment type values
/// </summary>
public enum EmploymentType
{
    /// <summary>Full-time permanent employee</summary>
    Permanent,

    /// <summary>Fixed-term contract employee</summary>
    Contract,

    /// <summary>Part-time employee</summary>
    PartTime,

    /// <summary>Temporary/casual worker</summary>
    Temporary,

    /// <summary>Agency/subcontracted worker</summary>
    Agency,

    /// <summary>Consultant</summary>
    Consultant
}

/// <summary>
/// Timesheet status values for approval workflow
/// </summary>
public enum TimesheetStatus
{
    /// <summary>Timesheet is being edited</summary>
    Draft,

    /// <summary>Timesheet submitted for approval</summary>
    Submitted,

    /// <summary>Timesheet approved by supervisor</summary>
    Approved,

    /// <summary>Timesheet rejected - needs revision</summary>
    Rejected,

    /// <summary>Timesheet processed for payroll</summary>
    Processed,

    /// <summary>Timesheet cancelled</summary>
    Cancelled
}

/// <summary>
/// Certification status values
/// </summary>
public enum CertificationStatus
{
    /// <summary>Certification is valid and current</summary>
    Valid,

    /// <summary>Certification is expiring soon (within 30 days)</summary>
    ExpiringSoon,

    /// <summary>Certification has expired</summary>
    Expired,

    /// <summary>Certification is pending verification</summary>
    PendingVerification,

    /// <summary>Certification was revoked</summary>
    Revoked,

    /// <summary>Certification renewal in progress</summary>
    RenewalInProgress
}

/// <summary>
/// Vessel assignment status values
/// </summary>
public enum AssignmentStatus
{
    /// <summary>Assignment is scheduled for future</summary>
    Scheduled,

    /// <summary>Personnel has signed on to vessel</summary>
    SignedOn,

    /// <summary>Personnel has signed off from vessel</summary>
    SignedOff,

    /// <summary>Assignment was cancelled</summary>
    Cancelled,

    /// <summary>Assignment completed</summary>
    Completed,

    /// <summary>Assignment extended</summary>
    Extended
}

/// <summary>
/// Time entry type values
/// </summary>
public enum TimeEntryType
{
    /// <summary>Regular work hours</summary>
    Regular,

    /// <summary>Overtime hours</summary>
    Overtime,

    /// <summary>Double time</summary>
    DoubleTime,

    /// <summary>Night shift differential</summary>
    NightShift,

    /// <summary>Weekend work</summary>
    Weekend,

    /// <summary>Holiday work</summary>
    Holiday,

    /// <summary>Standby time</summary>
    Standby,

    /// <summary>Travel time</summary>
    Travel,

    /// <summary>Training time</summary>
    Training
}

/// <summary>
/// Leave type values
/// </summary>
public enum LeaveType
{
    /// <summary>Annual/vacation leave</summary>
    Annual,

    /// <summary>Sick leave</summary>
    Sick,

    /// <summary>Personal/emergency leave</summary>
    Personal,

    /// <summary>Bereavement leave</summary>
    Bereavement,

    /// <summary>Maternity leave</summary>
    Maternity,

    /// <summary>Paternity leave</summary>
    Paternity,

    /// <summary>Unpaid leave</summary>
    Unpaid,

    /// <summary>Compensation/time off in lieu</summary>
    Compensation,

    /// <summary>Training/study leave</summary>
    Study,

    /// <summary>Field break/rotation leave</summary>
    FieldBreak
}

/// <summary>
/// Department values for personnel organization
/// </summary>
public enum Department
{
    Operations,
    Survey,
    Engineering,
    Navigation,
    ROV,
    Marine,
    QHSE,
    Technical,
    Geotechnical,
    Positioning,
    IT,
    HR,
    Finance,
    Administration,
    Management
}

/// <summary>
/// Certification category for STCW and other maritime certificates
/// </summary>
public enum CertificationCategory
{
    /// <summary>STCW mandatory certificates</summary>
    STCW,

    /// <summary>Medical fitness certificates</summary>
    Medical,

    /// <summary>Safety training certificates</summary>
    Safety,

    /// <summary>Technical/professional certificates</summary>
    Technical,

    /// <summary>Company-specific training</summary>
    CompanyTraining,

    /// <summary>Client-specific requirements</summary>
    ClientSpecific,

    /// <summary>Offshore survival certificates</summary>
    OffshoreSurvival,

    /// <summary>Competency certificates</summary>
    Competency,

    /// <summary>Flag state certificates</summary>
    FlagState,

    /// <summary>Other certificates</summary>
    Other
}

/// <summary>
/// Rotation pattern type
/// </summary>
public enum RotationType
{
    /// <summary>Equal time on/off (e.g., 28/28)</summary>
    EqualRotation,

    /// <summary>Back-to-back rotation</summary>
    BackToBack,

    /// <summary>Shore-based with occasional offshore</summary>
    ShoreBased,

    /// <summary>Project-based assignment</summary>
    ProjectBased,

    /// <summary>Ad-hoc/as needed</summary>
    AdHoc
}

/// <summary>
/// Sync status for offline support
/// </summary>
public enum SyncStatus
{
    /// <summary>Record pending upload to server</summary>
    Pending,

    /// <summary>Record synced with server</summary>
    Synced,

    /// <summary>Record has local changes not yet uploaded</summary>
    Modified,

    /// <summary>Sync conflict detected</summary>
    Conflict,

    /// <summary>Sync failed - retry needed</summary>
    Failed
}
