using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// ViewModel for the personnel detail view with form editing and validation
/// </summary>
public class PersonnelDetailViewModel : ViewModelBase, IDataErrorInfo
{
    private readonly PersonnelDatabaseService _dbService;
    private readonly Guid? _personnelId;
    private IPersonnelService? _personnelService;
    private bool _isNewRecord;

    #region Constructor

    public PersonnelDetailViewModel(PersonnelDatabaseService dbService, Guid? personnelId = null)
    {
        _dbService = dbService;
        _personnelId = personnelId;
        _personnelService = _dbService.GetPersonnelService();
        _isNewRecord = !personnelId.HasValue;

        // Initialize collections
        Positions = new ObservableCollection<Position>();
        RotationPatterns = new ObservableCollection<RotationPattern>();
        Departments = new ObservableCollection<Department>(Enum.GetValues(typeof(Department)).Cast<Department>());
        EmploymentStatuses = new ObservableCollection<EmploymentStatus>(Enum.GetValues(typeof(EmploymentStatus)).Cast<EmploymentStatus>());
        EmploymentTypes = new ObservableCollection<EmploymentType>(Enum.GetValues(typeof(EmploymentType)).Cast<EmploymentType>());
        Managers = new ObservableCollection<Personnel>();
        Certifications = new ObservableCollection<PersonnelCertification>();
        VesselAssignments = new ObservableCollection<VesselAssignment>();
        Titles = new ObservableCollection<string> { "", "Mr.", "Ms.", "Mrs.", "Dr.", "Capt." };

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        AddCertificationCommand = new RelayCommand(AddCertification);
        AddAssignmentCommand = new RelayCommand(AddAssignment);

        // Set defaults
        HireDate = DateTime.Today;
        Department = Department.Operations;
        EmploymentStatus = EmploymentStatus.Active;
        EmploymentType = EmploymentType.Permanent;
    }

    #endregion

    #region Properties - Personal Information

    private string _employeeNumber = string.Empty;
    [Required(ErrorMessage = "Employee Number is required")]
    [MaxLength(20, ErrorMessage = "Employee Number cannot exceed 20 characters")]
    public string EmployeeNumber
    {
        get => _employeeNumber;
        set
        {
            if (SetProperty(ref _employeeNumber, value))
            {
                ValidateProperty(value, nameof(EmployeeNumber));
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private string? _title;
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _firstName = string.Empty;
    [Required(ErrorMessage = "First Name is required")]
    [MaxLength(100, ErrorMessage = "First Name cannot exceed 100 characters")]
    public string FirstName
    {
        get => _firstName;
        set
        {
            if (SetProperty(ref _firstName, value))
            {
                ValidateProperty(value, nameof(FirstName));
                OnPropertyChanged(nameof(FullName));
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private string? _middleName;
    [MaxLength(100, ErrorMessage = "Middle Name cannot exceed 100 characters")]
    public string? MiddleName
    {
        get => _middleName;
        set
        {
            if (SetProperty(ref _middleName, value))
            {
                OnPropertyChanged(nameof(FullName));
            }
        }
    }

    private string _lastName = string.Empty;
    [Required(ErrorMessage = "Last Name is required")]
    [MaxLength(100, ErrorMessage = "Last Name cannot exceed 100 characters")]
    public string LastName
    {
        get => _lastName;
        set
        {
            if (SetProperty(ref _lastName, value))
            {
                ValidateProperty(value, nameof(LastName));
                OnPropertyChanged(nameof(FullName));
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private string? _preferredName;
    [MaxLength(100, ErrorMessage = "Preferred Name cannot exceed 100 characters")]
    public string? PreferredName
    {
        get => _preferredName;
        set => SetProperty(ref _preferredName, value);
    }

    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    private DateTime? _dateOfBirth;
    public DateTime? DateOfBirth
    {
        get => _dateOfBirth;
        set => SetProperty(ref _dateOfBirth, value);
    }

    private string? _nationality;
    [MaxLength(50, ErrorMessage = "Nationality cannot exceed 50 characters")]
    public string? Nationality
    {
        get => _nationality;
        set => SetProperty(ref _nationality, value);
    }

    private string? _passportNumber;
    [MaxLength(50, ErrorMessage = "Passport Number cannot exceed 50 characters")]
    public string? PassportNumber
    {
        get => _passportNumber;
        set => SetProperty(ref _passportNumber, value);
    }

    private DateTime? _passportExpiryDate;
    public DateTime? PassportExpiryDate
    {
        get => _passportExpiryDate;
        set => SetProperty(ref _passportExpiryDate, value);
    }

    #endregion

    #region Properties - Contact Information

    private string? _email;
    [MaxLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ValidateProperty(value, nameof(Email));
            }
        }
    }

    private string? _personalEmail;
    [MaxLength(200, ErrorMessage = "Personal Email cannot exceed 200 characters")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? PersonalEmail
    {
        get => _personalEmail;
        set => SetProperty(ref _personalEmail, value);
    }

    private string? _phoneNumber;
    [MaxLength(30, ErrorMessage = "Phone Number cannot exceed 30 characters")]
    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    private string? _mobileNumber;
    [MaxLength(30, ErrorMessage = "Mobile Number cannot exceed 30 characters")]
    public string? MobileNumber
    {
        get => _mobileNumber;
        set => SetProperty(ref _mobileNumber, value);
    }

    private string? _address;
    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    private string? _city;
    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public string? City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    private string? _country;
    [MaxLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
    public string? Country
    {
        get => _country;
        set => SetProperty(ref _country, value);
    }

    private string? _postalCode;
    [MaxLength(20, ErrorMessage = "Postal Code cannot exceed 20 characters")]
    public string? PostalCode
    {
        get => _postalCode;
        set => SetProperty(ref _postalCode, value);
    }

    #endregion

    #region Properties - Emergency Contact

    private string? _emergencyContactName;
    [MaxLength(200, ErrorMessage = "Emergency Contact Name cannot exceed 200 characters")]
    public string? EmergencyContactName
    {
        get => _emergencyContactName;
        set => SetProperty(ref _emergencyContactName, value);
    }

    private string? _emergencyContactRelation;
    [MaxLength(100, ErrorMessage = "Emergency Contact Relation cannot exceed 100 characters")]
    public string? EmergencyContactRelation
    {
        get => _emergencyContactRelation;
        set => SetProperty(ref _emergencyContactRelation, value);
    }

    private string? _emergencyContactPhone;
    [MaxLength(30, ErrorMessage = "Emergency Contact Phone cannot exceed 30 characters")]
    public string? EmergencyContactPhone
    {
        get => _emergencyContactPhone;
        set => SetProperty(ref _emergencyContactPhone, value);
    }

    private string? _emergencyContactEmail;
    [MaxLength(200, ErrorMessage = "Emergency Contact Email cannot exceed 200 characters")]
    public string? EmergencyContactEmail
    {
        get => _emergencyContactEmail;
        set => SetProperty(ref _emergencyContactEmail, value);
    }

    #endregion

    #region Properties - Employment

    private ObservableCollection<Position> _positions = null!;
    public ObservableCollection<Position> Positions
    {
        get => _positions;
        set => SetProperty(ref _positions, value);
    }

    private Position? _selectedPosition;
    public Position? SelectedPosition
    {
        get => _selectedPosition;
        set => SetProperty(ref _selectedPosition, value);
    }

    private ObservableCollection<Department> _departments = null!;
    public ObservableCollection<Department> Departments
    {
        get => _departments;
        set => SetProperty(ref _departments, value);
    }

    private Department _department;
    public Department Department
    {
        get => _department;
        set => SetProperty(ref _department, value);
    }

    private ObservableCollection<EmploymentStatus> _employmentStatuses = null!;
    public ObservableCollection<EmploymentStatus> EmploymentStatuses
    {
        get => _employmentStatuses;
        set => SetProperty(ref _employmentStatuses, value);
    }

    private EmploymentStatus _employmentStatus;
    public EmploymentStatus EmploymentStatus
    {
        get => _employmentStatus;
        set => SetProperty(ref _employmentStatus, value);
    }

    private ObservableCollection<EmploymentType> _employmentTypes = null!;
    public ObservableCollection<EmploymentType> EmploymentTypes
    {
        get => _employmentTypes;
        set => SetProperty(ref _employmentTypes, value);
    }

    private EmploymentType _employmentType;
    public EmploymentType EmploymentType
    {
        get => _employmentType;
        set => SetProperty(ref _employmentType, value);
    }

    private DateTime _hireDate;
    public DateTime HireDate
    {
        get => _hireDate;
        set => SetProperty(ref _hireDate, value);
    }

    private DateTime? _terminationDate;
    public DateTime? TerminationDate
    {
        get => _terminationDate;
        set => SetProperty(ref _terminationDate, value);
    }

    private ObservableCollection<RotationPattern> _rotationPatterns = null!;
    public ObservableCollection<RotationPattern> RotationPatterns
    {
        get => _rotationPatterns;
        set => SetProperty(ref _rotationPatterns, value);
    }

    private RotationPattern? _selectedRotationPattern;
    public RotationPattern? SelectedRotationPattern
    {
        get => _selectedRotationPattern;
        set => SetProperty(ref _selectedRotationPattern, value);
    }

    private ObservableCollection<Personnel> _managers = null!;
    public ObservableCollection<Personnel> Managers
    {
        get => _managers;
        set => SetProperty(ref _managers, value);
    }

    private Personnel? _selectedManager;
    public Personnel? SelectedManager
    {
        get => _selectedManager;
        set => SetProperty(ref _selectedManager, value);
    }

    private string? _homeBase;
    [MaxLength(100, ErrorMessage = "Home Base cannot exceed 100 characters")]
    public string? HomeBase
    {
        get => _homeBase;
        set => SetProperty(ref _homeBase, value);
    }

    private string? _preferredAirport;
    [MaxLength(100, ErrorMessage = "Preferred Airport cannot exceed 100 characters")]
    public string? PreferredAirport
    {
        get => _preferredAirport;
        set => SetProperty(ref _preferredAirport, value);
    }

    #endregion

    #region Properties - Medical

    private string? _bloodType;
    [MaxLength(10, ErrorMessage = "Blood Type cannot exceed 10 characters")]
    public string? BloodType
    {
        get => _bloodType;
        set => SetProperty(ref _bloodType, value);
    }

    private DateTime? _lastMedicalExamDate;
    public DateTime? LastMedicalExamDate
    {
        get => _lastMedicalExamDate;
        set => SetProperty(ref _lastMedicalExamDate, value);
    }

    private DateTime? _medicalFitnessExpiryDate;
    public DateTime? MedicalFitnessExpiryDate
    {
        get => _medicalFitnessExpiryDate;
        set => SetProperty(ref _medicalFitnessExpiryDate, value);
    }

    #endregion

    #region Properties - Notes

    private string? _notes;
    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    #endregion

    #region Properties - Collections

    private ObservableCollection<PersonnelCertification> _certifications = null!;
    public ObservableCollection<PersonnelCertification> Certifications
    {
        get => _certifications;
        set => SetProperty(ref _certifications, value);
    }

    private ObservableCollection<VesselAssignment> _vesselAssignments = null!;
    public ObservableCollection<VesselAssignment> VesselAssignments
    {
        get => _vesselAssignments;
        set => SetProperty(ref _vesselAssignments, value);
    }

    private ObservableCollection<string> _titles = null!;
    public ObservableCollection<string> Titles
    {
        get => _titles;
        set => SetProperty(ref _titles, value);
    }

    #endregion

    #region Properties - View State

    public bool IsNewRecord => _isNewRecord;

    public string HeaderText => _isNewRecord ? "Add New Personnel" : $"Edit: {FullName}";

    private bool _isReadOnly;
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddCertificationCommand { get; }
    public ICommand AddAssignmentCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when save is successful
    /// </summary>
    public event EventHandler<Personnel>? SaveCompleted;

    /// <summary>
    /// Event raised when cancel is requested
    /// </summary>
    public event EventHandler? CancelRequested;

    /// <summary>
    /// Event raised when add certification is requested
    /// </summary>
    public event EventHandler? AddCertificationRequested;

    /// <summary>
    /// Event raised when add assignment is requested
    /// </summary>
    public event EventHandler? AddAssignmentRequested;

    #endregion

    #region Validation

    private readonly Dictionary<string, string?> _validationErrors = new();

    public string Error => string.Join(Environment.NewLine, _validationErrors.Values.Where(v => v != null));

    public string this[string columnName]
    {
        get
        {
            _validationErrors.TryGetValue(columnName, out var error);
            return error ?? string.Empty;
        }
    }

    public bool HasErrors => _validationErrors.Values.Any(v => v != null);

    private void ValidateProperty<T>(T value, string propertyName)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(this) { MemberName = propertyName };
        Validator.TryValidateProperty(value, context, results);

        _validationErrors[propertyName] = results.FirstOrDefault()?.ErrorMessage;
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasErrors));
    }

    private bool ValidateAll()
    {
        ValidateProperty(EmployeeNumber, nameof(EmployeeNumber));
        ValidateProperty(FirstName, nameof(FirstName));
        ValidateProperty(LastName, nameof(LastName));
        ValidateProperty(Email, nameof(Email));

        return !HasErrors;
    }

    private bool CanSave()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(EmployeeNumber) &&
               !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Loads all reference data and personnel details
    /// </summary>
    public async Task LoadDataAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _personnelService = _dbService.GetPersonnelService();

            // Load reference data
            var positions = await Task.Run(() => _dbService.Context.Positions.Where(p => p.IsActive).ToList());
            Positions.Clear();
            foreach (var p in positions) Positions.Add(p);

            var rotations = await Task.Run(() => _dbService.Context.RotationPatterns.Where(r => r.IsActive).ToList());
            RotationPatterns.Clear();
            foreach (var r in rotations) RotationPatterns.Add(r);

            var managers = await _personnelService.GetActivePersonnelAsync();
            Managers.Clear();
            foreach (var m in managers) Managers.Add(m);

            // Load personnel if editing
            if (_personnelId.HasValue)
            {
                var personnel = await _personnelService.GetPersonnelByIdAsync(_personnelId.Value);
                if (personnel != null)
                {
                    PopulateFromPersonnel(personnel);
                }
            }

            OnPropertyChanged(nameof(HeaderText));
        }, "Loading data...");
    }

    /// <summary>
    /// Populates the form fields from a Personnel object
    /// </summary>
    private void PopulateFromPersonnel(Personnel personnel)
    {
        EmployeeNumber = personnel.EmployeeNumber;
        FirstName = personnel.FirstName;
        MiddleName = personnel.MiddleName;
        LastName = personnel.LastName;
        PreferredName = personnel.PreferredName;
        DateOfBirth = personnel.DateOfBirth;
        Nationality = personnel.Nationality;
        PassportNumber = personnel.PassportNumber;
        PassportExpiryDate = personnel.PassportExpiryDate;

        Email = personnel.Email;
        PersonalEmail = personnel.PersonalEmail;
        PhoneNumber = personnel.PhoneNumber;
        MobileNumber = personnel.MobileNumber;
        Address = personnel.Address;
        City = personnel.City;
        Country = personnel.Country;
        PostalCode = personnel.PostalCode;

        EmergencyContactName = personnel.EmergencyContactName;
        EmergencyContactRelation = personnel.EmergencyContactRelation;
        EmergencyContactPhone = personnel.EmergencyContactPhone;
        EmergencyContactEmail = personnel.EmergencyContactEmail;

        SelectedPosition = Positions.FirstOrDefault(p => p.PositionId == personnel.PositionId);
        Department = personnel.Department;
        EmploymentStatus = personnel.EmploymentStatus;
        EmploymentType = personnel.EmploymentType;
        HireDate = personnel.HireDate;
        TerminationDate = personnel.TerminationDate;
        SelectedRotationPattern = RotationPatterns.FirstOrDefault(r => r.RotationPatternId == personnel.RotationPatternId);
        SelectedManager = Managers.FirstOrDefault(m => m.PersonnelId == personnel.SupervisorId);
        HomeBase = personnel.HomeBase;
        PreferredAirport = personnel.PreferredAirport;

        BloodType = personnel.BloodType;
        LastMedicalExamDate = personnel.LastMedicalExamDate;
        MedicalFitnessExpiryDate = personnel.MedicalFitnessExpiryDate;

        Notes = personnel.Notes;

        // Load certifications
        Certifications.Clear();
        foreach (var cert in personnel.Certifications)
        {
            Certifications.Add(cert);
        }

        // Load vessel assignments
        VesselAssignments.Clear();
        foreach (var assignment in personnel.VesselAssignments)
        {
            VesselAssignments.Add(assignment);
        }
    }

    /// <summary>
    /// Creates a Personnel object from the form fields
    /// </summary>
    private Personnel CreatePersonnelFromForm()
    {
        return new Personnel
        {
            PersonnelId = _personnelId ?? Guid.NewGuid(),
            EmployeeNumber = EmployeeNumber.Trim(),
            FirstName = FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName.Trim(),
            LastName = LastName.Trim(),
            PreferredName = string.IsNullOrWhiteSpace(PreferredName) ? null : PreferredName.Trim(),
            DateOfBirth = DateOfBirth,
            Nationality = string.IsNullOrWhiteSpace(Nationality) ? null : Nationality.Trim(),
            PassportNumber = string.IsNullOrWhiteSpace(PassportNumber) ? null : PassportNumber.Trim(),
            PassportExpiryDate = PassportExpiryDate,
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            PersonalEmail = string.IsNullOrWhiteSpace(PersonalEmail) ? null : PersonalEmail.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
            MobileNumber = string.IsNullOrWhiteSpace(MobileNumber) ? null : MobileNumber.Trim(),
            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
            City = string.IsNullOrWhiteSpace(City) ? null : City.Trim(),
            Country = string.IsNullOrWhiteSpace(Country) ? null : Country.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(EmergencyContactName) ? null : EmergencyContactName.Trim(),
            EmergencyContactRelation = string.IsNullOrWhiteSpace(EmergencyContactRelation) ? null : EmergencyContactRelation.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(EmergencyContactPhone) ? null : EmergencyContactPhone.Trim(),
            EmergencyContactEmail = string.IsNullOrWhiteSpace(EmergencyContactEmail) ? null : EmergencyContactEmail.Trim(),
            PositionId = SelectedPosition?.PositionId,
            Department = Department,
            EmploymentStatus = EmploymentStatus,
            EmploymentType = EmploymentType,
            HireDate = HireDate,
            TerminationDate = TerminationDate,
            RotationPatternId = SelectedRotationPattern?.RotationPatternId,
            SupervisorId = SelectedManager?.PersonnelId,
            HomeBase = string.IsNullOrWhiteSpace(HomeBase) ? null : HomeBase.Trim(),
            PreferredAirport = string.IsNullOrWhiteSpace(PreferredAirport) ? null : PreferredAirport.Trim(),
            BloodType = string.IsNullOrWhiteSpace(BloodType) ? null : BloodType.Trim(),
            LastMedicalExamDate = LastMedicalExamDate,
            MedicalFitnessExpiryDate = MedicalFitnessExpiryDate,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };
    }

    /// <summary>
    /// Saves the personnel record
    /// </summary>
    private async Task SaveAsync()
    {
        if (!ValidateAll())
        {
            return;
        }

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _personnelService = _dbService.GetPersonnelService();
            var personnel = CreatePersonnelFromForm();

            Personnel savedPersonnel;
            if (_isNewRecord)
            {
                savedPersonnel = await _personnelService.CreatePersonnelAsync(personnel);
            }
            else
            {
                savedPersonnel = await _personnelService.UpdatePersonnelAsync(personnel);
            }

            SaveCompleted?.Invoke(this, savedPersonnel);
        }, "Saving personnel...");
    }

    /// <summary>
    /// Cancels the edit operation
    /// </summary>
    private void Cancel()
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the AddCertificationRequested event
    /// </summary>
    private void AddCertification()
    {
        AddCertificationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the AddAssignmentRequested event
    /// </summary>
    private void AddAssignment()
    {
        AddAssignmentRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
