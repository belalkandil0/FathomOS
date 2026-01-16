using System.Windows;

namespace FathomOS.Modules.MruCalibration.Views;

/// <summary>
/// Modern Help Dialog with tabbed interface for MRU Calibration Module
/// </summary>
public partial class HelpDialog : Window
{
    public HelpDialog()
    {
        // Load theme before InitializeComponent
        LoadTheme();
        InitializeComponent();
    }

    public HelpDialog(Window owner) : this()
    {
        Owner = owner;
    }

    /// <summary>
    /// Open help dialog to a specific step tab
    /// </summary>
    public HelpDialog(Window owner, int stepNumber) : this(owner)
    {
        // Tab indices: 0=Overview, 1-7=Steps 1-7
        if (stepNumber >= 1 && stepNumber <= 7)
        {
            // Find the TabControl and set selected index
            Loaded += (s, e) =>
            {
                if (Content is System.Windows.Controls.Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is System.Windows.Controls.TabControl tabControl)
                        {
                            tabControl.SelectedIndex = stepNumber; // 0=Overview, 1=Step1, etc.
                            break;
                        }
                    }
                }
            };
        }
    }

    private void LoadTheme()
    {
        try
        {
            // Load dark theme by default (matches MainWindow default)
            var themeUri = new System.Uri("/FathomOS.Modules.MruCalibration;component/Themes/Subsea7DarkTheme.xaml", System.UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load theme: {ex.Message}");
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
