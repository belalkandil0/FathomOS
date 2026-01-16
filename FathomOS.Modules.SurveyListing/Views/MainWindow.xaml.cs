using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using FathomOS.Core.Services;
using FathomOS.Core.Models;
using FathomOS.Modules.SurveyListing.ViewModels;
using FathomOS.Modules.SurveyListing.Services;
using ControlzEx.Theming;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FathomOS.Modules.SurveyListing.Views;

/// <summary>
/// Main window - Wizard container for the Survey Listing Generator
/// Uses MahApps.Metro for modern Windows 11 styling
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly ProjectService _projectService;
    private readonly MainViewModel _viewModel;
    private Project _currentProject;
    private bool _isDarkTheme = true;
    private bool _hasUnsavedChanges = false;
    private string? _lastSavedProjectHash = null;

    public MainWindow()
    {
        InitializeComponent();
        
        _projectService = new ProjectService();
        _currentProject = _projectService.CreateNew();
        _viewModel = new MainViewModel(_currentProject);
        
        DataContext = _viewModel;
        UpdateProjectLabel();
        
        // Initialize DialogService with this window for themed dialogs
        DialogService.Instance.Initialize(this);
        
        // Subscribe to step changes to sync project data and track changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Store initial state for change detection
        _lastSavedProjectHash = GetProjectHash();
        
        // Apply dark theme by default
        ApplyTheme(_isDarkTheme);
        UpdateThemeButton();
    }
    
    #region Change Tracking
    
    /// <summary>
    /// Mark the project as having unsaved changes
    /// </summary>
    private void MarkAsChanged()
    {
        if (!_hasUnsavedChanges)
        {
            _hasUnsavedChanges = true;
            UpdateProjectLabel();
        }
    }
    
    /// <summary>
    /// Mark the project as saved (no unsaved changes)
    /// </summary>
    private void MarkAsSaved()
    {
        _hasUnsavedChanges = false;
        _lastSavedProjectHash = GetProjectHash();
        UpdateProjectLabel();
    }
    
    /// <summary>
    /// Get a simple hash of project state for change detection
    /// </summary>
    private string GetProjectHash()
    {
        try
        {
            // Sync current state from ViewModels
            _viewModel.UpdateProject(_currentProject);
            
            // Create a simple hash from key project properties
            var hashData = $"{_currentProject.ProjectName}|{_currentProject.ClientName}|" +
                          $"{_currentProject.VesselName}|{_currentProject.RouteFilePath}|" +
                          $"{string.Join(",", _currentProject.SurveyDataFiles)}|" +
                          $"{_currentProject.TideFilePath}|{_currentProject.OutputOptions.OutputFolder}";
            return hashData.GetHashCode().ToString();
        }
        catch
        {
            return string.Empty;
        }
    }
    
    #endregion
    
    #region Theme Management
    
    private void ApplyTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        
        // Apply MahApps theme
        var themeName = isDark ? "Dark.Blue" : "Light.Blue";
        ThemeManager.Current.ChangeTheme(this, themeName);
        
        // Also update app-level theme for consistency
        if (System.Windows.Application.Current != null)
        {
            ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, themeName);
        }
        
        // Update the ThemeService state
        ThemeService.Instance.SetCurrentTheme(isDark ? AppTheme.Dark : AppTheme.Light);
    }
    
    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme(_isDarkTheme);
        UpdateThemeButton();
    }
    
    private void UpdateThemeButton()
    {
        ThemeIcon.Text = _isDarkTheme ? "🌙" : "☀️";
        ThemeLabel.Text = _isDarkTheme ? "Dark" : "Light";
    }
    
    #endregion
    
    #region Module Integration Methods
    
    /// <summary>
    /// Load a project file (called from module)
    /// </summary>
    public void LoadProject(string filePath)
    {
        try
        {
            _currentProject = _projectService.Load(filePath);
            _viewModel.LoadProject(_currentProject);
            UpdateProjectLabel();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading project:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Load a survey data file (NPD) (called from module)
    /// </summary>
    public void LoadSurveyFile(string filePath)
    {
        try
        {
            if (!_currentProject.SurveyDataFiles.Contains(filePath))
            {
                _currentProject.SurveyDataFiles.Add(filePath);
            }
            _viewModel.LoadProject(_currentProject);
            _viewModel.GoToStep(2); // Step 3 (0-indexed)
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading survey file:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Load a route file (RLX) (called from module)
    /// </summary>
    public void LoadRouteFile(string filePath)
    {
        try
        {
            _currentProject.RouteFilePath = filePath;
            _viewModel.LoadProject(_currentProject);
            _viewModel.GoToStep(1); // Step 2 (0-indexed)
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading route file:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Load a tide file (called from module)
    /// </summary>
    public void LoadTideFile(string filePath)
    {
        try
        {
            _currentProject.TideFilePath = filePath;
            _viewModel.LoadProject(_currentProject);
            _viewModel.GoToStep(4); // Step 5 (0-indexed)
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tide file:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentStepIndex))
        {
            _viewModel.UpdateProject(_currentProject);
            // Mark as changed when user moves between steps (data may have changed)
            MarkAsChanged();
            UpdateProjectLabel();
        }
    }

    private async void NewProject_Click(object sender, RoutedEventArgs e)
    {
        if (HasUnsavedChanges())
        {
            var result = await DialogService.Instance.ShowConfirmWithCancelAsync(
                "Save Changes",
                "Do you want to save changes to the current project?");

            if (result == null) return;
            if (result == true) SaveProject_Click(sender, e);
        }

        _currentProject = _projectService.CreateNew();
        _viewModel.LoadProject(_currentProject);
        _hasUnsavedChanges = false;
        _lastSavedProjectHash = GetProjectHash();
        UpdateProjectLabel();
    }

    private async void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = ProjectService.FileFilter,
            Title = "Open Project"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _currentProject = _projectService.Load(dialog.FileName);
                _viewModel.LoadProject(_currentProject);
                _hasUnsavedChanges = false;
                _lastSavedProjectHash = GetProjectHash();
                UpdateProjectLabel();
            }
            catch (Exception ex)
            {
                await DialogService.Instance.ShowErrorAsync(
                    "Error",
                    $"Error loading project:\n\n{ex.Message}");
            }
        }
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentProject.ProjectFilePath))
        {
            SaveProject(_currentProject.ProjectFilePath);
        }
        else
        {
            SaveProjectAs_Click(sender, e);
        }
    }

    private void SaveProjectAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = ProjectService.FileFilter,
            Title = "Save Project As",
            FileName = _currentProject.ProjectName
        };

        if (dialog.ShowDialog() == true)
        {
            SaveProject(dialog.FileName);
        }
    }

    private void SaveProject(string path)
    {
        try
        {
            _viewModel.UpdateProject(_currentProject);
            _projectService.Save(_currentProject, path);
            MarkAsSaved();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving project:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool HasUnsavedChanges()
    {
        // Check if explicitly marked as changed
        if (_hasUnsavedChanges)
            return true;
            
        // Also check if current state differs from last saved state
        var currentHash = GetProjectHash();
        return currentHash != _lastSavedProjectHash;
    }

    private void UpdateProjectLabel()
    {
        var name = _currentProject.DisplayName;
        var modified = HasUnsavedChanges() ? " *" : "";
        ProjectNameLabel.Text = name + modified;
    }

    private void StepIndicator_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is int stepNumber)
        {
            _viewModel.GoToStep(stepNumber - 1);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.GoBack();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.GoNext();
    }

    private void Process_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.UpdateProject(_currentProject);
        
        // Validate before processing
        if (!_viewModel.ValidateAllSteps())
        {
            return;
        }
        
        // Process the survey data using the existing StartProcessing method
        _viewModel.StartProcessing();
    }

    private void DxfToSurveyListing_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dxfWindow = new DxfToListingWindow();
            dxfWindow.Owner = this;
            dxfWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening DXF tool:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var helpWindow = new HelpWindow();
        helpWindow.Owner = this;
        helpWindow.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.Owner = this;
        aboutWindow.ShowDialog();
    }
}
