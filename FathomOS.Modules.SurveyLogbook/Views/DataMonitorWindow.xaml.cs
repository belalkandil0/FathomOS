// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Views/DataMonitorWindow.xaml.cs
// Purpose: Code-behind for real-time NaviPac data monitoring window
// Version: 9.0.0
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyLogbook.ViewModels;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Interaction logic for DataMonitorWindow.xaml
/// Real-time NaviPac data monitor for debugging and verification.
/// </summary>
public partial class DataMonitorWindow : MetroWindow
{
    private readonly DataMonitorViewModel _viewModel;
    
    /// <summary>
    /// Initializes the DataMonitorWindow with a ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind to.</param>
    public DataMonitorWindow(DataMonitorViewModel viewModel)
    {
        // Load theme BEFORE InitializeComponent
        try
        {
            var themeUri = new Uri("/FathomOS.Modules.SurveyLogbook;component/Themes/DarkTheme.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
        }
        
        InitializeComponent();
        
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        
        // Setup auto-scroll behavior
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }
    
    /// <summary>
    /// Default constructor for design-time support.
    /// </summary>
    public DataMonitorWindow() : this(new DataMonitorViewModel())
    {
    }
    
    /// <summary>
    /// Handles property changes to implement auto-scroll.
    /// </summary>
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DataMonitorViewModel.SelectedMessage) && _viewModel.AutoScroll)
        {
            // Auto-scroll to the latest message
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MessagesListBox.Items.Count > 0)
                {
                    MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
    
    /// <summary>
    /// Handles window closing to dispose resources.
    /// </summary>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _viewModel.Dispose();
    }
    
    /// <summary>
    /// Test button click handler - sends test data for debugging.
    /// </summary>
    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        // Generate sample NaviPac-like data
        var random = new Random();
        var eventNum = random.Next(1000, 9999);
        var gyro = random.NextDouble() * 360;
        var roll = (random.NextDouble() - 0.5) * 10;
        var pitch = (random.NextDouble() - 0.5) * 10;
        var heave = (random.NextDouble() - 0.5) * 2;
        var easting = 2129798.0 + random.NextDouble() * 10;
        var northing = 9765368.0 + random.NextDouble() * 10;
        var height = -87.0 + random.NextDouble() * 2;
        var kp = random.NextDouble() * 100;
        var dcc = (random.NextDouble() - 0.5) * 20;
        
        var testData = $"{eventNum},{gyro:F2},{roll:F2},{pitch:F2},{heave:F3},{easting:F2},{northing:F2},{height:F2},{kp:F3},{dcc:F2}";
        
        _viewModel.AddTestMessage(testData);
    }
}
