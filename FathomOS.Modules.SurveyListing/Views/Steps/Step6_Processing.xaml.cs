using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FathomOS.Modules.SurveyListing.ViewModels;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.Views.Steps;

public partial class Step6_Processing : UserControl
{
    public Step6_Processing()
    {
        InitializeComponent();
    }

    private async void Process_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            await vm.ProcessAsync();
        }
    }

    private void Open3DViewer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            vm.Open3DViewer();
        }
    }

    private void OpenEditor_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            vm.OpenSurveyEditor();
        }
    }

    private void OpenCharts_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            vm.OpenCharts();
        }
    }
    
    // Pagination handlers
    private void FirstPage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            vm.GoToFirstPage();
        }
    }
    
    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            vm.GoToPreviousPage();
        }
    }
    
    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            vm.GoToNextPage();
        }
    }
    
    private void LastPage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            vm.GoToLastPage();
        }
    }

    private async void ExportSmoothingReport_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = "SmoothingReport.csv",
                Title = "Export Smoothing Report"
            };

            if (dialog.ShowDialog() == true)
            {
                await vm.ExportSmoothingReportAsync(dialog.FileName);
                MessageBox.Show($"Smoothing report exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            var logText = string.Join(Environment.NewLine, vm.ProcessingLog);
            if (!string.IsNullOrEmpty(logText))
            {
                Clipboard.SetText(logText);
                MessageBox.Show("Log copied to clipboard!", "Copy Log", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Log is empty.", "Copy Log", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = $"ProcessingLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                Title = "Save Processing Log"
            };

            if (dialog.ShowDialog() == true)
            {
                var logText = string.Join(Environment.NewLine, vm.ProcessingLog);
                System.IO.File.WriteAllText(dialog.FileName, logText);
                MessageBox.Show($"Log saved to:\n{dialog.FileName}", "Save Log",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
    
    private void ExportIntervalPoints_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step6ViewModel vm && vm.IntervalPointCount > 0)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = $"IntervalPoints_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Interval Points"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var writer = new System.IO.StreamWriter(dialog.FileName);
                    writer.WriteLine("Index,Distance_m,X_Easting,Y_Northing,Z_Depth");
                    
                    foreach (var pt in vm.IntervalPointItems)
                    {
                        writer.WriteLine($"{pt.Index},{pt.Distance:F3},{pt.X:F4},{pt.Y:F4},{pt.Z:F4}");
                    }
                    
                    MessageBox.Show($"Interval points exported to:\n{dialog.FileName}", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting:\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
