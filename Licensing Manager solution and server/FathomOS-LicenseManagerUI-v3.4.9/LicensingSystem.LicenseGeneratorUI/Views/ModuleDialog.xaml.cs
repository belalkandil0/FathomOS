using System.Windows;
using System.Windows.Controls;

namespace LicenseGeneratorUI.Views;

/// <summary>
/// Dialog for adding or editing a module
/// </summary>
public partial class ModuleDialog : Window
{
    public bool IsEditMode { get; private set; }
    public int ModuleDbId { get; private set; }
    
    // Result properties
    public string ModuleId => ModuleIdInput.Text.Trim();
    public string ModuleDisplayName => DisplayNameInput.Text.Trim();
    public string CertificateCode => CertificateCodeInput.Text.Trim().ToUpperInvariant();
    public string ModuleDescription => DescriptionInput.Text.Trim();
    public string ModuleIcon => IconInput.Text.Trim();
    public string DefaultTier => (DefaultTierCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Professional";

    // Common module icons for picker
    private readonly string[] _commonIcons = new[]
    {
        "📊", "📈", "📉", "📋", "📁", "📂", "🗂️", "📄", "📝", "✏️",
        "🔧", "⚙️", "🛠️", "🔩", "🔨", "🔬", "🔭", "📡", "📐", "📏",
        "🌊", "⚓", "🚢", "🛳️", "⛵", "🎯", "🗺️", "🧭", "📍", "🔍",
        "💾", "💿", "📀", "🖥️", "💻", "⌨️", "🖱️", "🖨️", "📱", "📲",
        "⏱️", "⏰", "🕐", "📅", "📆", "🗓️", "📌", "📎", "✂️", "🔗",
        "🔒", "🔓", "🔑", "🗝️", "🛡️", "⚡", "💡", "🔋", "🔌", "💎"
    };

    public ModuleDialog()
    {
        InitializeComponent();
        IsEditMode = false;
        DialogTitle.Text = "Add Module";
        SaveButton.Content = "Add Module";
    }

    /// <summary>
    /// Initialize dialog for editing an existing module
    /// </summary>
    public void SetEditMode(int dbId, string moduleId, string displayName, string certificateCode, string? description, string? icon, string? defaultTier)
    {
        IsEditMode = true;
        ModuleDbId = dbId;
        DialogTitle.Text = "Edit Module";
        SaveButton.Content = "Save Changes";
        
        ModuleIdInput.Text = moduleId;
        ModuleIdInput.IsEnabled = false; // Can't change module ID
        ModuleIdInput.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D2D"));
        
        DisplayNameInput.Text = displayName;
        CertificateCodeInput.Text = certificateCode;
        DescriptionInput.Text = description ?? "";
        IconInput.Text = icon ?? "📦";
        IconPreview.Text = icon ?? "📦";
        
        // Set default tier
        foreach (ComboBoxItem item in DefaultTierCombo.Items)
        {
            if (item.Tag?.ToString() == defaultTier)
            {
                DefaultTierCombo.SelectedItem = item;
                break;
            }
        }
    }

    private void IconInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Update preview
        var icon = IconInput.Text.Trim();
        IconPreview.Text = string.IsNullOrEmpty(icon) ? "📦" : icon;
    }

    private void PickIcon_Click(object sender, RoutedEventArgs e)
    {
        // Create a simple icon picker popup
        var popup = new Window
        {
            Title = "Pick Icon",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0D1117"))
        };

        var wrapPanel = new WrapPanel { Margin = new Thickness(10) };
        var scrollViewer = new ScrollViewer { Content = wrapPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        foreach (var icon in _commonIcons)
        {
            var btn = new Button
            {
                Content = icon,
                FontSize = 20,
                Width = 45,
                Height = 45,
                Margin = new Thickness(3),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#161B22")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btn.Click += (s, args) =>
            {
                IconInput.Text = icon;
                IconPreview.Text = icon;
                popup.Close();
            };
            wrapPanel.Children.Add(btn);
        }

        popup.Content = scrollViewer;
        popup.ShowDialog();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(ModuleIdInput.Text))
        {
            MessageBox.Show("Module ID is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            ModuleIdInput.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayNameInput.Text))
        {
            MessageBox.Show("Display Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            DisplayNameInput.Focus();
            return;
        }

        var code = CertificateCodeInput.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2)
        {
            MessageBox.Show("Certificate Code must be exactly 2 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            CertificateCodeInput.Focus();
            return;
        }

        // Validate certificate code format (A-Z, 0-9 only)
        foreach (var c in code)
        {
            if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
            {
                MessageBox.Show("Certificate Code must contain only A-Z or 0-9.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CertificateCodeInput.Focus();
                return;
            }
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
