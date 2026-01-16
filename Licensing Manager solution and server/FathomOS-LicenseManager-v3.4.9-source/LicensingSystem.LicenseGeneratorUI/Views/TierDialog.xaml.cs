using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LicenseGeneratorUI.Views;

/// <summary>
/// Simple module info for display in tier dialog
/// </summary>
public class TierModuleItem
{
    public string ModuleId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? CertificateCode { get; set; }
    public string? Icon { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// Dialog for editing tier module assignments
/// </summary>
public partial class TierDialog : Window
{
    public string TierId { get; private set; } = "";
    public string TierDisplayName { get; private set; } = "";
    public List<string> SelectedModuleIds { get; private set; } = new();
    
    private List<TierModuleItem> _modules = new();
    private Dictionary<string, CheckBox> _checkboxes = new();

    public TierDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initialize dialog with tier and available modules
    /// </summary>
    public void Initialize(string tierId, string tierDisplayName, List<TierModuleItem> allModules, List<string> currentModuleIds)
    {
        TierId = tierId;
        TierDisplayName = tierDisplayName;
        _modules = allModules;
        
        DialogTitle.Text = $"Edit {tierDisplayName} Tier";
        TierNameLabel.Text = $"Select which modules are included in the {tierDisplayName} tier";
        
        ModulesCheckboxPanel.Children.Clear();
        _checkboxes.Clear();
        
        foreach (var module in allModules.OrderBy(m => m.DisplayName))
        {
            var isSelected = currentModuleIds.Contains(module.ModuleId);
            
            var checkbox = new CheckBox
            {
                Content = $"{module.Icon ?? "📦"} {module.DisplayName} ({module.CertificateCode ?? "??"})",
                Tag = module.ModuleId,
                IsChecked = isSelected,
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            
            _checkboxes[module.ModuleId] = checkbox;
            ModulesCheckboxPanel.Children.Add(checkbox);
        }
        
        if (allModules.Count == 0)
        {
            ModulesCheckboxPanel.Children.Add(new TextBlock 
            { 
                Text = "No modules available. Add modules first.", 
                FontSize = 12, 
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 10, 0, 0)
            });
        }
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var checkbox in _checkboxes.Values)
        {
            checkbox.IsChecked = true;
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var checkbox in _checkboxes.Values)
        {
            checkbox.IsChecked = false;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SelectedModuleIds = _checkboxes
            .Where(kvp => kvp.Value.IsChecked == true)
            .Select(kvp => kvp.Key)
            .ToList();
        
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
