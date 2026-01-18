using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;
using FathomOS.Modules.ProjectManagement.ViewModels;

namespace FathomOS.Modules.ProjectManagement.Views;

/// <summary>
/// Main window for Project Management module
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly ProjectDatabaseService _dbService;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize database service (NOT initialized yet - will be done async)
        _dbService = new ProjectDatabaseService();

        // Initialize ViewModel (database not ready yet)
        _viewModel = new MainViewModel(_dbService);
        DataContext = _viewModel;

        // Wire up events
        _viewModel.OpenProjectDetail += OnOpenProjectDetail;
        _viewModel.CloseProjectDetail += OnCloseProjectDetail;
        _viewModel.OpenClientDetail += OnOpenClientDetail;
        _viewModel.CloseClientDetail += OnCloseClientDetail;

        // Load data asynchronously
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show loading state
            IsEnabled = false;
            Cursor = System.Windows.Input.Cursors.Wait;

            // Initialize database asynchronously (non-blocking)
            await _dbService.InitializeAsync();

            // Now initialize the ViewModel
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing: {ex.Message}");
            MessageBox.Show($"Error initializing module: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
            Cursor = System.Windows.Input.Cursors.Arrow;
        }
    }

    private void OnOpenProjectDetail(object? sender, SurveyProject? project)
    {
        if (_viewModel.ProjectDetailViewModel == null) return;

        var detailView = new ProjectDetailView();
        detailView.DataContext = _viewModel.ProjectDetailViewModel;

        var window = new Window
        {
            Title = _viewModel.ProjectDetailViewModel.Title,
            Content = detailView,
            Width = 950,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.CanResize
        };

        // Wire up close events
        _viewModel.ProjectDetailViewModel.SaveCompleted += (s, e) =>
        {
            window.DialogResult = true;
            window.Close();
        };

        _viewModel.ProjectDetailViewModel.CancelRequested += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        // Wire up milestone/deliverable dialog events
        _viewModel.ProjectDetailViewModel.AddMilestoneRequested += (s, m) => ShowMilestoneDialog(window, null);
        _viewModel.ProjectDetailViewModel.EditMilestoneRequested += (s, m) => ShowMilestoneDialog(window, m);
        _viewModel.ProjectDetailViewModel.AddDeliverableRequested += (s, d) => ShowDeliverableDialog(window, null);
        _viewModel.ProjectDetailViewModel.EditDeliverableRequested += (s, d) => ShowDeliverableDialog(window, d);

        // Load project data
        _ = _viewModel.ProjectDetailViewModel.LoadAsync();

        window.ShowDialog();
    }

    private void OnCloseProjectDetail(object? sender, EventArgs e)
    {
        // Detail view closed via ViewModel
    }

    private void ShowMilestoneDialog(Window owner, ProjectMilestone? milestone)
    {
        var milestoneVM = new MilestoneViewModel(milestone);
        var dialog = new MilestoneDetailView { DataContext = milestoneVM };

        var window = new Window
        {
            Title = milestone == null ? "Add Milestone" : "Edit Milestone",
            Content = dialog,
            Width = 600,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            ResizeMode = ResizeMode.NoResize
        };

        milestoneVM.SaveCompleted += async (s, m) =>
        {
            window.DialogResult = true;
            window.Close();

            if (_viewModel.ProjectDetailViewModel != null && m != null)
            {
                if (milestone == null)
                {
                    await _viewModel.ProjectDetailViewModel.AddMilestoneToProjectAsync(m);
                }
                else
                {
                    await _viewModel.ProjectDetailViewModel.UpdateMilestoneAsync(m);
                }
            }
        };

        milestoneVM.CancelRequested += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        window.ShowDialog();
    }

    private void ShowDeliverableDialog(Window owner, ProjectDeliverable? deliverable)
    {
        var deliverableVM = new DeliverableViewModel(deliverable);
        var dialog = new DeliverableDetailView { DataContext = deliverableVM };

        var window = new Window
        {
            Title = deliverable == null ? "Add Deliverable" : "Edit Deliverable",
            Content = dialog,
            Width = 650,
            Height = 550,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            ResizeMode = ResizeMode.NoResize
        };

        deliverableVM.SaveCompleted += async (s, d) =>
        {
            window.DialogResult = true;
            window.Close();

            if (_viewModel.ProjectDetailViewModel != null && d != null)
            {
                if (deliverable == null)
                {
                    await _viewModel.ProjectDetailViewModel.AddDeliverableToProjectAsync(d);
                }
                else
                {
                    await _viewModel.ProjectDetailViewModel.UpdateDeliverableAsync(d);
                }
            }
        };

        deliverableVM.CancelRequested += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        window.ShowDialog();
    }

    private void OnOpenClientDetail(object? sender, Client? client)
    {
        var clientVM = new ClientDetailViewModel(client);

        var detailView = new ClientDetailView { DataContext = clientVM };

        var window = new Window
        {
            Title = clientVM.Title,
            Content = detailView,
            Width = 700,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.CanResize
        };

        clientVM.SaveCompleted += async (s, e) =>
        {
            window.DialogResult = true;
            window.Close();
            await _viewModel.ClientListViewModel.LoadAsync();
        };

        clientVM.CancelRequested += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        window.ShowDialog();
    }

    private void OnCloseClientDetail(object? sender, EventArgs e)
    {
        // Client detail view closed via ViewModel
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _dbService?.Dispose();
    }
}
