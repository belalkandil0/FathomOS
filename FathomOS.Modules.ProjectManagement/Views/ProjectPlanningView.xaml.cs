using System.Windows;
using System.Windows.Controls;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.Views;

/// <summary>
/// Project planning view for resource assignments
/// </summary>
public partial class ProjectPlanningView : UserControl
{
    private readonly ProjectDatabaseService? _dbService;
    private SurveyProject? _selectedProject;

    public ProjectPlanningView()
    {
        InitializeComponent();
    }

    public ProjectPlanningView(ProjectDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        Loaded += ProjectPlanningView_Loaded;
    }

    private async void ProjectPlanningView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        if (_dbService == null) return;

        try
        {
            var projectService = _dbService.GetProjectService();
            var projects = await projectService.GetActiveProjectsAsync();
            ProjectCombo.ItemsSource = projects;

            if (projects.Any())
            {
                ProjectCombo.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectCombo.SelectedItem is not SurveyProject project)
        {
            return;
        }

        _selectedProject = project;
        await LoadProjectAssignmentsAsync();
    }

    private async Task LoadProjectAssignmentsAsync()
    {
        if (_dbService == null || _selectedProject == null) return;

        try
        {
            var assignmentService = _dbService.GetAssignmentService();

            // Load vessel assignments
            var vessels = await assignmentService.GetProjectVesselAssignmentsAsync(_selectedProject.ProjectId);
            VesselAssignmentsGrid.ItemsSource = vessels;

            // Load equipment assignments
            var equipment = await assignmentService.GetProjectEquipmentAssignmentsAsync(_selectedProject.ProjectId);
            EquipmentAssignmentsGrid.ItemsSource = equipment;

            // Load personnel assignments
            var personnel = await assignmentService.GetProjectPersonnelAssignmentsAsync(_selectedProject.ProjectId);
            PersonnelAssignmentsGrid.ItemsSource = personnel;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading assignments: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddVesselButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProject == null)
        {
            MessageBox.Show("Please select a project first.", "No Project Selected",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show("Vessel assignment dialog will be implemented.", "Assign Vessel",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddEquipmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProject == null)
        {
            MessageBox.Show("Please select a project first.", "No Project Selected",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show("Equipment assignment dialog will be implemented.", "Assign Equipment",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddPersonnelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProject == null)
        {
            MessageBox.Show("Please select a project first.", "No Project Selected",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show("Personnel assignment dialog will be implemented.", "Assign Personnel",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
