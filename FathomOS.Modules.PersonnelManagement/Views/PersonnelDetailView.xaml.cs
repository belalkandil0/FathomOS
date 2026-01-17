using System.Windows;
using System.Windows.Controls;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.Views;

/// <summary>
/// Personnel detail view for viewing/editing personnel information
/// </summary>
public partial class PersonnelDetailView : UserControl
{
    private readonly PersonnelDatabaseService _dbService;
    private readonly Guid? _personnelId;
    private Personnel? _personnel;

    public PersonnelDetailView(PersonnelDatabaseService dbService, Guid? personnelId)
    {
        InitializeComponent();
        _dbService = dbService;
        _personnelId = personnelId;
        Loaded += PersonnelDetailView_Loaded;
    }

    private async void PersonnelDetailView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadReferenceDataAsync();

        if (_personnelId.HasValue)
        {
            await LoadPersonnelAsync();
            HeaderText.Text = $"Edit: {_personnel?.FullName}";
        }
        else
        {
            _personnel = new Personnel();
            HeaderText.Text = "Add New Personnel";
        }
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            // Load positions
            var positions = await Task.Run(() => _dbService.Context.Positions.Where(p => p.IsActive).ToList());
            PositionCombo.ItemsSource = positions;

            // Load rotation patterns
            var rotations = await Task.Run(() => _dbService.Context.RotationPatterns.Where(r => r.IsActive).ToList());
            RotationCombo.ItemsSource = rotations;

            // Load departments enum
            DepartmentCombo.ItemsSource = Enum.GetValues(typeof(Department));

            // Load status enum
            StatusCombo.ItemsSource = Enum.GetValues(typeof(EmploymentStatus));

            // Load employment type enum
            TypeCombo.ItemsSource = Enum.GetValues(typeof(EmploymentType));

            // Load managers
            var personnelService = _dbService.GetPersonnelService();
            var managers = await personnelService.GetActivePersonnelAsync();
            ManagerCombo.ItemsSource = managers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading reference data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadPersonnelAsync()
    {
        try
        {
            var personnelService = _dbService.GetPersonnelService();
            _personnel = await personnelService.GetPersonnelByIdAsync(_personnelId!.Value);

            if (_personnel == null)
            {
                MessageBox.Show("Personnel not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Populate form fields
            EmployeeNumberBox.Text = _personnel.EmployeeNumber;
            FirstNameBox.Text = _personnel.FirstName;
            LastNameBox.Text = _personnel.LastName;
            MiddleNameBox.Text = _personnel.MiddleName;
            PreferredNameBox.Text = _personnel.PreferredName;
            DateOfBirthPicker.SelectedDate = _personnel.DateOfBirth;
            NationalityBox.Text = _personnel.Nationality;
            EmailBox.Text = _personnel.Email;
            MobilePhoneBox.Text = _personnel.MobileNumber;
            EmergencyContactBox.Text = _personnel.EmergencyContactName;
            EmergencyPhoneBox.Text = _personnel.EmergencyContactPhone;
            AddressBox.Text = _personnel.Address;

            PositionCombo.SelectedValue = _personnel.PositionId;
            DepartmentCombo.SelectedItem = _personnel.Department;
            StatusCombo.SelectedItem = _personnel.EmploymentStatus;
            TypeCombo.SelectedItem = _personnel.EmploymentType;
            HireDatePicker.SelectedDate = _personnel.HireDate;
            RotationCombo.SelectedValue = _personnel.RotationPatternId;
            // Note: Company, DailyRate, CostCenter fields would need to be added to Personnel model
            // or these fields can be left empty for now
            CompanyBox.Text = "";
            DailyRateBox.Text = "";
            CostCenterBox.Text = "";

            // Load certifications
            CertificationsGrid.ItemsSource = _personnel.Certifications;

            // Load assignments
            AssignmentsGrid.ItemsSource = _personnel.VesselAssignments;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading personnel: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm())
        {
            return;
        }

        try
        {
            // Update personnel object from form
            _personnel!.EmployeeNumber = EmployeeNumberBox.Text.Trim();
            _personnel.FirstName = FirstNameBox.Text.Trim();
            _personnel.LastName = LastNameBox.Text.Trim();
            _personnel.MiddleName = string.IsNullOrWhiteSpace(MiddleNameBox.Text) ? null : MiddleNameBox.Text.Trim();
            _personnel.PreferredName = string.IsNullOrWhiteSpace(PreferredNameBox.Text) ? null : PreferredNameBox.Text.Trim();
            _personnel.DateOfBirth = DateOfBirthPicker.SelectedDate;
            _personnel.Nationality = string.IsNullOrWhiteSpace(NationalityBox.Text) ? null : NationalityBox.Text.Trim();
            _personnel.Email = string.IsNullOrWhiteSpace(EmailBox.Text) ? null : EmailBox.Text.Trim();
            _personnel.MobileNumber = string.IsNullOrWhiteSpace(MobilePhoneBox.Text) ? null : MobilePhoneBox.Text.Trim();
            _personnel.EmergencyContactName = string.IsNullOrWhiteSpace(EmergencyContactBox.Text) ? null : EmergencyContactBox.Text.Trim();
            _personnel.EmergencyContactPhone = string.IsNullOrWhiteSpace(EmergencyPhoneBox.Text) ? null : EmergencyPhoneBox.Text.Trim();
            _personnel.Address = string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim();

            _personnel.PositionId = (PositionCombo.SelectedItem as Position)?.PositionId;
            _personnel.Department = (Department)DepartmentCombo.SelectedItem;
            _personnel.EmploymentStatus = (EmploymentStatus)StatusCombo.SelectedItem;
            _personnel.EmploymentType = (EmploymentType)TypeCombo.SelectedItem;
            _personnel.HireDate = HireDatePicker.SelectedDate ?? DateTime.Today;
            _personnel.RotationPatternId = (RotationCombo.SelectedItem as RotationPattern)?.RotationPatternId;
            // Note: Company, DailyRate, CostCenter would need to be added to Personnel model
            // These fields are not saved for now

            var personnelService = _dbService.GetPersonnelService();

            if (_personnelId.HasValue)
            {
                await personnelService.UpdatePersonnelAsync(_personnel);
            }
            else
            {
                await personnelService.CreatePersonnelAsync(_personnel);
            }

            // Close with success
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving personnel: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.DialogResult = false;
            window.Close();
        }
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(EmployeeNumberBox.Text))
        {
            MessageBox.Show("Employee Number is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            EmployeeNumberBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
        {
            MessageBox.Show("First Name is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            FirstNameBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(LastNameBox.Text))
        {
            MessageBox.Show("Last Name is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            LastNameBox.Focus();
            return false;
        }

        return true;
    }
}
