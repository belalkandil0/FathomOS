using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.Views;

/// <summary>
/// Timesheet management view with entry grid
/// </summary>
public partial class TimesheetView : UserControl
{
    private readonly PersonnelDatabaseService? _dbService;

    public TimesheetView()
    {
        InitializeComponent();
    }

    public TimesheetView(PersonnelDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        Loaded += TimesheetView_Loaded;
    }

    private async void TimesheetView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadReferenceDataAsync();
        await LoadTimesheetsAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        if (_dbService == null) return;

        try
        {
            // Load personnel
            var personnelService = _dbService.GetPersonnelService();
            var personnel = await personnelService.GetActivePersonnelAsync();
            PersonnelFilter.ItemsSource = personnel;

            // Load status enum
            StatusFilter.ItemsSource = Enum.GetValues(typeof(TimesheetStatus));
            StatusFilter.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading reference data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadTimesheetsAsync()
    {
        if (_dbService == null) return;

        try
        {
            var timesheetService = _dbService.GetTimesheetService();

            // Get timesheets based on filter
            IEnumerable<Timesheet> timesheets;
            if (StatusFilter.SelectedItem != null)
            {
                var status = (TimesheetStatus)StatusFilter.SelectedItem;
                timesheets = await timesheetService.GetTimesheetsByStatusAsync(status);
            }
            else
            {
                // Get all timesheets for selected personnel
                if (PersonnelFilter.SelectedItem is Personnel selected)
                {
                    timesheets = await timesheetService.GetTimesheetsForPersonnelAsync(selected.PersonnelId);
                }
                else
                {
                    timesheets = await timesheetService.GetTimesheetsByStatusAsync(TimesheetStatus.Draft);
                }
            }

            TimesheetsGrid.ItemsSource = timesheets;
            StatusText.Text = $"{timesheets.Count()} timesheets";

            var pending = await timesheetService.GetTimesheetsPendingApprovalAsync();
            PendingText.Text = $"{pending.Count()} pending approval";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading timesheets: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewTimesheetButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("New timesheet creation will be implemented.", "New Timesheet",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ViewButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedTimesheet();
    }

    private void TimesheetsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelectedTimesheet();
    }

    private void OpenSelectedTimesheet()
    {
        if (TimesheetsGrid.SelectedItem is not Timesheet selected)
        {
            MessageBox.Show("Please select a timesheet.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show($"Viewing timesheet: {selected.TimesheetNumber}\n" +
                        $"Period: {selected.PeriodDisplay}\n" +
                        $"Total Hours: {selected.TotalHours:F1}",
                        "Timesheet Details",
                        MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ApproveButton_Click(object sender, RoutedEventArgs e)
    {
        if (TimesheetsGrid.SelectedItem is not Timesheet selected)
        {
            MessageBox.Show("Please select a timesheet to approve.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.Status != TimesheetStatus.Submitted)
        {
            MessageBox.Show("Only submitted timesheets can be approved.", "Invalid Status",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"Approve timesheet {selected.TimesheetNumber}?",
                                      "Confirm Approval",
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var timesheetService = _dbService!.GetTimesheetService();
                await timesheetService.ApproveTimesheetAsync(selected.TimesheetId, Guid.Empty, null);
                await LoadTimesheetsAsync();
                MessageBox.Show("Timesheet approved successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error approving timesheet: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (TimesheetsGrid.SelectedItem is not Timesheet selected)
        {
            MessageBox.Show("Please select a timesheet to reject.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.Status != TimesheetStatus.Submitted)
        {
            MessageBox.Show("Only submitted timesheets can be rejected.", "Invalid Status",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Would normally show a dialog to get rejection reason
        var result = MessageBox.Show($"Reject timesheet {selected.TimesheetNumber}?",
                                      "Confirm Rejection",
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var timesheetService = _dbService!.GetTimesheetService();
                await timesheetService.RejectTimesheetAsync(selected.TimesheetId, Guid.Empty, "Rejected by supervisor");
                await LoadTimesheetsAsync();
                MessageBox.Show("Timesheet rejected successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rejecting timesheet: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Export functionality will be implemented.", "Export",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
