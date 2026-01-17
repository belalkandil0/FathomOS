using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.Views;

/// <summary>
/// Project list view with search/filter capabilities
/// </summary>
public partial class ProjectListView : UserControl
{
    private readonly ProjectDatabaseService? _dbService;

    public ProjectListView()
    {
        InitializeComponent();
    }

    public ProjectListView(ProjectDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        Loaded += ProjectListView_Loaded;
    }

    private async void ProjectListView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadReferenceDataAsync();
        await LoadProjectsAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        if (_dbService == null) return;

        try
        {
            // Load status enum
            StatusFilter.ItemsSource = Enum.GetValues(typeof(ProjectStatus));
            StatusFilter.SelectedIndex = 0;

            // Load clients
            var projectService = _dbService.GetProjectService();
            var clients = await projectService.GetAllClientsAsync();
            ClientFilter.ItemsSource = clients;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading reference data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadProjectsAsync()
    {
        if (_dbService == null) return;

        try
        {
            var projectService = _dbService.GetProjectService();
            var projects = await projectService.GetAllProjectsAsync();
            ProjectsGrid.ItemsSource = projects;

            var totalCount = await projectService.GetTotalProjectCountAsync();
            var activeCount = await projectService.GetActiveProjectCountAsync();
            StatusText.Text = $"{totalCount} projects";
            ActiveText.Text = $"{activeCount} active";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var detailView = new ProjectDetailView(_dbService!, null);
        var window = new Window
        {
            Title = "New Project",
            Content = detailView,
            Width = 900,
            Height = 750,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            _ = LoadProjectsAsync();
        }
    }

    private void ViewButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedProject(readOnly: true);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedProject(readOnly: false);
    }

    private void ProjectsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelectedProject(readOnly: false);
    }

    private void OpenSelectedProject(bool readOnly)
    {
        if (ProjectsGrid.SelectedItem is not SurveyProject selected)
        {
            MessageBox.Show("Please select a project.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var detailView = new ProjectDetailView(_dbService!, selected.ProjectId);
        var window = new Window
        {
            Title = readOnly ? $"View: {selected.ProjectName}" : $"Edit: {selected.ProjectName}",
            Content = detailView,
            Width = 900,
            Height = 750,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            _ = LoadProjectsAsync();
        }
    }

    private void ReportButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Report generation will be implemented.", "Report",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
