using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.PersonnelManagement.Services;
using FathomOS.Modules.PersonnelManagement.ViewModels;

namespace FathomOS.Modules.PersonnelManagement.Views;

/// <summary>
/// Main window for Personnel Management module
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly PersonnelDatabaseService _dbService;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize database service (NOT initialized yet - will be done async)
        _dbService = new PersonnelDatabaseService();

        // Initialize main ViewModel (database not ready yet)
        _viewModel = new MainViewModel(_dbService);
        DataContext = _viewModel;

        // Wire up child views with their ViewModels
        PersonnelListViewControl.DataContext = _viewModel.PersonnelListViewModel;
        CertificationListViewControl.DataContext = _viewModel.CertificationViewModel;
        VesselAssignmentViewControl.DataContext = _viewModel.VesselAssignmentViewModel;
        TimesheetViewControl.DataContext = _viewModel.TimesheetListViewModel;

        // Wire up events
        WireUpEvents();

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
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

            // Now load data
            await _viewModel.LoadDataAsync();
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

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _dbService.Dispose();
    }

    private void WireUpEvents()
    {
        // Personnel list events
        _viewModel.PersonnelListViewModel.AddPersonnelRequested += (s, e) => OpenPersonnelDetail(null);
        _viewModel.PersonnelListViewModel.EditPersonnelRequested += (s, p) => OpenPersonnelDetail(p.PersonnelId);
        _viewModel.PersonnelListViewModel.ViewPersonnelRequested += (s, p) => OpenPersonnelDetail(p.PersonnelId, readOnly: true);
        _viewModel.PersonnelListViewModel.ExportRequested += (s, e) => ShowExportMessage();

        // Certification events
        _viewModel.CertificationViewModel.AddCertificationRequested += (s, e) => ShowFeatureMessage("Add Certification");
        _viewModel.CertificationViewModel.EditCertificationRequested += (s, c) => ShowFeatureMessage("Edit Certification");
        _viewModel.CertificationViewModel.ViewPersonnelRequested += (s, p) => OpenPersonnelDetail(p.PersonnelId, readOnly: true);
        _viewModel.CertificationViewModel.ExportRequested += (s, e) => ShowExportMessage();
        _viewModel.CertificationViewModel.MessageRequested += (s, msg) => ShowMessage(msg);

        // Vessel assignment events
        _viewModel.VesselAssignmentViewModel.NewAssignmentRequested += (s, e) => ShowFeatureMessage("New Assignment");
        _viewModel.VesselAssignmentViewModel.EditAssignmentRequested += (s, a) => ShowFeatureMessage("Edit Assignment");
        _viewModel.VesselAssignmentViewModel.SignOnRequested += (s, a) => ShowSignOnDialog(a);
        _viewModel.VesselAssignmentViewModel.SignOffRequested += (s, a) => ShowSignOffDialog(a);
        _viewModel.VesselAssignmentViewModel.ViewPersonnelRequested += (s, p) => OpenPersonnelDetail(p.PersonnelId, readOnly: true);
        _viewModel.VesselAssignmentViewModel.ExportRequested += (s, e) => ShowExportMessage();
        _viewModel.VesselAssignmentViewModel.MessageRequested += (s, msg) => ShowMessage(msg);

        // Timesheet events
        _viewModel.TimesheetListViewModel.NewTimesheetRequested += (s, e) => OpenTimesheetDetail(null);
        _viewModel.TimesheetListViewModel.ViewTimesheetRequested += (s, t) => OpenTimesheetDetail(t.TimesheetId, readOnly: true);
        _viewModel.TimesheetListViewModel.EditTimesheetRequested += (s, t) => OpenTimesheetDetail(t.TimesheetId);
        _viewModel.TimesheetListViewModel.ExportRequested += (s, e) => ShowExportMessage();
        _viewModel.TimesheetListViewModel.MessageRequested += (s, msg) => ShowMessage(msg);

        // Main ViewModel events
        _viewModel.ImportRequested += (s, e) => ShowFeatureMessage("Import");
        _viewModel.ExportRequested += (s, e) => ShowExportMessage();
        _viewModel.GenerateReportRequested += (s, e) => ShowFeatureMessage("Generate Report");
    }

    private void OpenPersonnelDetail(Guid? personnelId, bool readOnly = false)
    {
        var detailViewModel = new PersonnelDetailViewModel(_dbService, personnelId);
        detailViewModel.IsReadOnly = readOnly;

        var detailView = new PersonnelDetailView();
        detailView.DataContext = detailViewModel;

        var window = new Window
        {
            Title = personnelId.HasValue
                ? (readOnly ? "View Personnel" : "Edit Personnel")
                : "Add Personnel",
            Content = detailView,
            Width = 850,
            Height = 750,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        detailViewModel.SaveCompleted += (s, p) =>
        {
            window.DialogResult = true;
            window.Close();
        };

        detailViewModel.CancelRequested += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        _ = detailViewModel.LoadDataAsync();

        if (window.ShowDialog() == true)
        {
            _ = _viewModel.PersonnelListViewModel.RefreshAfterEditAsync();
        }
    }

    private void OpenTimesheetDetail(Guid? timesheetId, bool readOnly = false)
    {
        var detailViewModel = new TimesheetDetailViewModel(_dbService, timesheetId);

        var window = new Window
        {
            Title = timesheetId.HasValue
                ? (readOnly ? "View Timesheet" : "Edit Timesheet")
                : "New Timesheet",
            Content = new TimesheetDetailView { DataContext = detailViewModel },
            Width = 950,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        detailViewModel.SaveCompleted += (s, t) =>
        {
            window.DialogResult = true;
            window.Close();
        };

        detailViewModel.CancelRequested += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        _ = detailViewModel.LoadDataAsync();

        if (window.ShowDialog() == true)
        {
            _ = _viewModel.TimesheetListViewModel.LoadDataAsync();
        }
    }

    private async void ShowSignOnDialog(Models.VesselAssignment assignment)
    {
        var result = MessageBox.Show(
            $"Sign on {assignment.Personnel?.FullName ?? "personnel"} to {assignment.VesselName}?",
            "Confirm Sign-On",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.VesselAssignmentViewModel.PerformSignOnAsync(
                assignment.VesselAssignmentId,
                DateTime.Now,
                "Vessel Location",
                "Port");
        }
    }

    private async void ShowSignOffDialog(Models.VesselAssignment assignment)
    {
        var result = MessageBox.Show(
            $"Sign off {assignment.Personnel?.FullName ?? "personnel"} from {assignment.VesselName}?",
            "Confirm Sign-Off",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.VesselAssignmentViewModel.PerformSignOffAsync(
                assignment.VesselAssignmentId,
                DateTime.Now,
                "Vessel Location",
                "Port",
                "End of rotation");
        }
    }

    private void ShowMessage(string message)
    {
        MessageBox.Show(message, "Personnel Management", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowFeatureMessage(string feature)
    {
        MessageBox.Show($"{feature} functionality will be fully implemented in a future update.",
            feature, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowExportMessage()
    {
        MessageBox.Show("Export functionality will be implemented using ClosedXML for Excel export.",
            "Export", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
