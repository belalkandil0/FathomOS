using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using LicenseGeneratorUI.Services;
using LicenseGeneratorUI.Models;
using LicensingSystem.Shared;

namespace LicenseGeneratorUI.Views;

public partial class EditOfflineLicenseWindow : Window
{
    private readonly LicenseFile _originalLicense;
    private readonly List<ServerModuleInfo> _modules;
    private readonly List<ServerTierInfo> _tiers;
    private readonly LicenseSigningService _signingService;
    private readonly ECDsa _privateKey;
    private readonly Dictionary<string, CheckBox> _moduleCheckboxes = new();
    private readonly HashSet<string> _currentModules;

    public SignedLicense? UpdatedLicense { get; private set; }

    public EditOfflineLicenseWindow(
        LicenseFile license,
        List<ServerModuleInfo> modules,
        List<ServerTierInfo> tiers,
        LicenseSigningService signingService,
        ECDsa privateKey)
    {
        InitializeComponent();
        
        _originalLicense = license;
        _modules = modules;
        _tiers = tiers;
        _signingService = signingService;
        _privateKey = privateKey;

        // Parse current modules from features
        _currentModules = license.Features?
            .Where(f => f.StartsWith("MODULE:", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Replace("Module:", "", StringComparison.OrdinalIgnoreCase).Replace("MODULE:", ""))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        InitializeUI();
    }

    private void InitializeUI()
    {
        // Set license info
        LicenseIdText.Text = _originalLicense.LicenseId;
        CustomerText.Text = $"{_originalLicense.CustomerName} ({_originalLicense.CustomerEmail})";
        ExpiresText.Text = _originalLicense.ExpiresAt.ToString("yyyy-MM-dd HH:mm");
        
        // Show hardware IDs
        var hwIds = _originalLicense.HardwareFingerprints ?? new List<string>();
        HardwareIdsText.Text = hwIds.Count > 0 
            ? string.Join(", ", hwIds.Select(h => h.Length > 12 ? h[..12] + "..." : h))
            : "No hardware IDs (unusual for offline license)";

        // Populate tier combo
        string currentTier = _originalLicense.Edition ?? "Professional";
        
        foreach (var tier in _tiers.OrderBy(t => t.DisplayOrder))
        {
            var item = new ComboBoxItem
            {
                Content = tier.DisplayName,
                Tag = tier.TierId
            };
            
            if (tier.TierId.Equals(currentTier, StringComparison.OrdinalIgnoreCase))
                item.IsSelected = true;
            
            TierCombo.Items.Add(item);
        }

        // Add Custom option
        var customItem = new ComboBoxItem { Content = "Custom (Select Modules)", Tag = "Custom" };
        if (!_tiers.Any(t => t.TierId.Equals(currentTier, StringComparison.OrdinalIgnoreCase)))
        {
            customItem.IsSelected = true;
        }
        TierCombo.Items.Add(customItem);

        // If no tiers from server, add defaults
        if (TierCombo.Items.Count <= 1)
        {
            TierCombo.Items.Clear();
            TierCombo.Items.Add(new ComboBoxItem { Content = "Basic", Tag = "Basic" });
            TierCombo.Items.Add(new ComboBoxItem { Content = "Professional", Tag = "Professional", IsSelected = currentTier == "Professional" });
            TierCombo.Items.Add(new ComboBoxItem { Content = "Enterprise", Tag = "Enterprise", IsSelected = currentTier == "Enterprise" });
            TierCombo.Items.Add(new ComboBoxItem { Content = "Custom (Select Modules)", Tag = "Custom" });
        }

        // Create module checkboxes
        if (_modules.Any())
        {
            foreach (var module in _modules.OrderBy(m => m.DisplayOrder))
            {
                CreateModuleCheckbox(module.ModuleId, module.DisplayName, module.Icon ?? "📦");
            }
        }
        else
        {
            // Fallback to defaults if no server modules
            CreateModuleCheckbox("SurveyListing", "Survey Listing", "📊");
            CreateModuleCheckbox("TideAnalysis", "Tide Analysis", "🌊");
            CreateModuleCheckbox("Calibrations", "Calibrations", "🔧");
            CreateModuleCheckbox("SoundVelocity", "Sound Velocity", "🔊");
            CreateModuleCheckbox("NetworkTimeSync", "Network Time Sync", "⏱️");
            CreateModuleCheckbox("BatchProcessor", "Batch Processor", "⚡");
        }

        // Update checkboxes based on current tier
        UpdateModuleCheckboxesForTier();
    }

    private void CreateModuleCheckbox(string moduleId, string displayName, string icon)
    {
        var checkbox = new CheckBox
        {
            Content = $"{icon} {displayName}",
            Tag = moduleId,
            IsChecked = _currentModules.Contains(moduleId),
            Margin = new Thickness(0, 5, 20, 5),
            FontSize = 13,
            MinWidth = 180
        };
        
        _moduleCheckboxes[moduleId] = checkbox;
        ModulesPanel.Children.Add(checkbox);
    }

    private void TierCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateModuleCheckboxesForTier();
    }

    private void UpdateModuleCheckboxesForTier()
    {
        if (TierCombo.SelectedItem is not ComboBoxItem selectedItem) return;
        
        var tierId = selectedItem.Tag?.ToString() ?? "Custom";
        var tier = _tiers.FirstOrDefault(t => t.TierId == tierId);
        var tierModuleIds = tier?.Modules?.Select(m => m.ModuleId).ToHashSet() ?? new HashSet<string>();
        
        bool isCustom = tierId == "Custom";

        // Fallback tier definitions if no server data
        if (!tierModuleIds.Any() && !isCustom)
        {
            tierModuleIds = tierId switch
            {
                "Basic" => new HashSet<string> { "SurveyListing" },
                "Professional" => new HashSet<string> { "SurveyListing", "TideAnalysis", "Calibrations", "SoundVelocity", "NetworkTimeSync" },
                "Enterprise" => new HashSet<string> { "SurveyListing", "TideAnalysis", "Calibrations", "SoundVelocity", "NetworkTimeSync", "BatchProcessor" },
                _ => new HashSet<string>()
            };
        }

        foreach (var kvp in _moduleCheckboxes)
        {
            var moduleId = kvp.Key;
            var checkbox = kvp.Value;
            
            checkbox.IsEnabled = isCustom;
            if (!isCustom)
            {
                checkbox.IsChecked = tierModuleIds.Contains(moduleId);
            }
        }

        // Update description
        if (tier != null)
        {
            TierDescriptionText.Text = tier.Description ?? $"{tier.DisplayName} tier modules";
        }
        else if (isCustom)
        {
            TierDescriptionText.Text = "Select individual modules for this license";
        }
        else
        {
            TierDescriptionText.Text = tierId switch
            {
                "Basic" => "Basic tier includes Survey Listing module only",
                "Professional" => "Professional tier includes all core modules",
                "Enterprise" => "Enterprise tier includes ALL modules",
                _ => ""
            };
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveBtn.IsEnabled = false;
            SaveBtn.Content = "Regenerating...";

            var selectedTier = (TierCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Custom";
            
            // Build features list
            var features = new List<string> { $"Tier:{selectedTier}".ToUpperInvariant() };
            
            foreach (var kvp in _moduleCheckboxes)
            {
                if (kvp.Value.IsChecked == true)
                {
                    features.Add($"Module:{kvp.Key}".ToUpperInvariant());
                }
            }

            // Create updated license (preserve everything except Edition and Features)
            var updatedLicense = new LicenseFile
            {
                Version = _originalLicense.Version,
                LicenseId = _originalLicense.LicenseId,
                Product = _originalLicense.Product,
                Edition = selectedTier,
                CustomerEmail = _originalLicense.CustomerEmail,
                CustomerName = _originalLicense.CustomerName,
                IssuedAt = _originalLicense.IssuedAt,
                ExpiresAt = _originalLicense.ExpiresAt,
                SubscriptionType = _originalLicense.SubscriptionType,
                HardwareFingerprints = _originalLicense.HardwareFingerprints,
                FingerprintMatchThreshold = _originalLicense.FingerprintMatchThreshold,
                Features = features,
                Metadata = new Dictionary<string, string>
                {
                    ["generatedBy"] = "LicenseGeneratorUI v1.0",
                    ["originalIssuedAt"] = _originalLicense.IssuedAt.ToString("O"),
                    ["regeneratedAt"] = DateTime.UtcNow.ToString("O")
                }
            };

            // Sign the updated license
            UpdatedLicense = _signingService.SignLicense(updatedLicense, _privateKey);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error regenerating license: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SaveBtn.IsEnabled = true;
            SaveBtn.Content = "🔄 Regenerate License";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
