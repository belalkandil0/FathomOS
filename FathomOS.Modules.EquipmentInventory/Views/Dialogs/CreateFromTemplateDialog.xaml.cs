using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class CreateFromTemplateDialog : MetroWindow
{
    private readonly LocalDatabaseService _dbService;
    private readonly EquipmentTemplateService _templateService;
    private List<EquipmentTemplate> _allTemplates = new();
    
    public Equipment? CreatedEquipment { get; private set; }
    
    public CreateFromTemplateDialog(LocalDatabaseService dbService, EquipmentTemplateService templateService)
    {
        _dbService = dbService;
        _templateService = templateService;
        
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        Loaded += async (s, e) => await LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        try
        {
            // Load templates
            _allTemplates = await _templateService.GetAllTemplatesAsync();
            UpdateTemplatesList();
            
            // Load locations
            var locations = await _dbService.GetLocationsAsync();
            LocationCombo.ItemsSource = locations.OrderBy(l => l.Name).ToList();
            if (locations.Any())
                LocationCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
    }
    
    private void UpdateTemplatesList()
    {
        var search = SearchBox.Text?.ToLower() ?? "";
        
        var filtered = string.IsNullOrWhiteSpace(search)
            ? _allTemplates
            : _allTemplates.Where(t => 
                t.TemplateName.ToLower().Contains(search) ||
                (t.Description?.ToLower().Contains(search) ?? false) ||
                (t.CategoryName?.ToLower().Contains(search) ?? false)).ToList();
        
        TemplatesList.ItemsSource = filtered.OrderByDescending(t => t.UsageCount).ToList();
        EmptyState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTemplatesList();
    }
    
    private void TemplatesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CreateButton.IsEnabled = TemplatesList.SelectedItem != null;
    }
    
    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (TemplatesList.SelectedItem is not EquipmentTemplate template) return;
        
        try
        {
            var customName = string.IsNullOrWhiteSpace(CustomNameBox.Text) ? null : CustomNameBox.Text.Trim();
            var locationId = (LocationCombo.SelectedItem as Location)?.LocationId;
            var count = (int)(CountBox.Value ?? 1);
            
            if (count == 1)
            {
                CreatedEquipment = await _templateService.CreateFromTemplateAsync(
                    template.TemplateId, customName, locationId);
                
                MessageBox.Show($"Created equipment: {CreatedEquipment.AssetNumber}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var created = new List<Equipment>();
                for (int i = 0; i < count; i++)
                {
                    var name = customName != null ? $"{customName} {i + 1}" : null;
                    var equipment = await _templateService.CreateFromTemplateAsync(
                        template.TemplateId, name, locationId);
                    created.Add(equipment);
                }
                
                CreatedEquipment = created.FirstOrDefault();
                MessageBox.Show($"Created {created.Count} equipment items.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating equipment: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
