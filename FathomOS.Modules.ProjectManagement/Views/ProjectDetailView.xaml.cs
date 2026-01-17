using System.Windows;
using System.Windows.Controls;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.Views;

/// <summary>
/// Project detail view for viewing/editing project information
/// </summary>
public partial class ProjectDetailView : UserControl
{
    private readonly ProjectDatabaseService _dbService;
    private readonly Guid? _projectId;
    private SurveyProject? _project;

    public ProjectDetailView(ProjectDatabaseService dbService, Guid? projectId)
    {
        InitializeComponent();
        _dbService = dbService;
        _projectId = projectId;
        Loaded += ProjectDetailView_Loaded;
    }

    private async void ProjectDetailView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadReferenceDataAsync();

        if (_projectId.HasValue)
        {
            await LoadProjectAsync();
            HeaderText.Text = $"Edit: {_project?.ProjectName}";
        }
        else
        {
            _project = new SurveyProject();
            HeaderText.Text = "New Project";
            ProjectNumberBox.Text = "(Auto-generated on save)";
        }
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            // Load clients
            var projectService = _dbService.GetProjectService();
            var clients = await projectService.GetAllClientsAsync();
            ClientCombo.ItemsSource = clients;

            // Load enums
            ProjectTypeCombo.ItemsSource = Enum.GetValues(typeof(ProjectType));
            StatusCombo.ItemsSource = Enum.GetValues(typeof(ProjectStatus));
            PhaseCombo.ItemsSource = Enum.GetValues(typeof(ProjectPhase));
            BillingTypeCombo.ItemsSource = Enum.GetValues(typeof(BillingType));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading reference data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadProjectAsync()
    {
        try
        {
            var projectService = _dbService.GetProjectService();
            _project = await projectService.GetProjectByIdAsync(_projectId!.Value);

            if (_project == null)
            {
                MessageBox.Show("Project not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Populate form fields
            ProjectNumberBox.Text = _project.ProjectNumber;
            ProjectNameBox.Text = _project.ProjectName;
            ClientCombo.SelectedValue = _project.ClientId;
            ProjectTypeCombo.SelectedItem = _project.ProjectType;
            StatusCombo.SelectedItem = _project.Status;
            PhaseCombo.SelectedItem = _project.Phase;
            StartDatePicker.SelectedDate = _project.PlannedStartDate;
            EndDatePicker.SelectedDate = _project.PlannedEndDate;
            LocationBox.Text = _project.LocationName;
            RegionBox.Text = _project.Country;
            BudgetBox.Text = _project.Budget.HasValue && _project.Budget.Value > 0 ? _project.Budget.Value.ToString("F2") : "";
            BillingTypeCombo.SelectedItem = _project.BillingType;
            DescriptionBox.Text = _project.Description;

            // Update progress display
            ProgressBar.Value = (double)_project.PercentComplete;
            ProgressText.Text = $"{_project.PercentComplete:F0}%";

            // Load related data
            MilestonesGrid.ItemsSource = _project.Milestones?.Where(m => m.IsActive);
            DeliverablesGrid.ItemsSource = _project.Deliverables?.Where(d => d.IsActive);
            VesselsGrid.ItemsSource = _project.VesselAssignments?.Where(a => a.IsActive);
            EquipmentGrid.ItemsSource = _project.EquipmentAssignments?.Where(a => a.IsActive);
            PersonnelGrid.ItemsSource = _project.PersonnelAssignments?.Where(a => a.IsActive);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading project: {ex.Message}", "Error",
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
            // Update project object from form
            _project!.ProjectName = ProjectNameBox.Text.Trim();
            _project.ClientId = (ClientCombo.SelectedItem as Client)?.ClientId;
            _project.ProjectType = ProjectTypeCombo.SelectedItem != null ? (ProjectType)ProjectTypeCombo.SelectedItem : ProjectType.Other;
            _project.Status = StatusCombo.SelectedItem != null ? (ProjectStatus)StatusCombo.SelectedItem : ProjectStatus.Draft;
            _project.Phase = PhaseCombo.SelectedItem != null ? (ProjectPhase)PhaseCombo.SelectedItem : ProjectPhase.Initiation;
            _project.PlannedStartDate = StartDatePicker.SelectedDate ?? DateTime.Today;
            _project.PlannedEndDate = EndDatePicker.SelectedDate;
            _project.LocationName = string.IsNullOrWhiteSpace(LocationBox.Text) ? null : LocationBox.Text.Trim();
            _project.Country = string.IsNullOrWhiteSpace(RegionBox.Text) ? null : RegionBox.Text.Trim();
            _project.BillingType = BillingTypeCombo.SelectedItem != null ? (BillingType)BillingTypeCombo.SelectedItem : BillingType.FixedPrice;
            _project.Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();

            if (decimal.TryParse(BudgetBox.Text, out var budget))
            {
                _project.Budget = budget;
            }

            var projectService = _dbService.GetProjectService();

            if (_projectId.HasValue)
            {
                await projectService.UpdateProjectAsync(_project);
            }
            else
            {
                await projectService.CreateProjectAsync(_project);
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
            MessageBox.Show($"Error saving project: {ex.Message}", "Error",
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
        if (string.IsNullOrWhiteSpace(ProjectNameBox.Text))
        {
            MessageBox.Show("Project Name is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ProjectNameBox.Focus();
            return false;
        }

        if (ClientCombo.SelectedItem == null)
        {
            MessageBox.Show("Client is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ClientCombo.Focus();
            return false;
        }

        if (!StartDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Start Date is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            StartDatePicker.Focus();
            return false;
        }

        return true;
    }
}
