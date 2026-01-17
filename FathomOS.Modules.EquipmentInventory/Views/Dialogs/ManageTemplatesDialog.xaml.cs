using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class ManageTemplatesDialog : MetroWindow
{
    private readonly LocalDatabaseService _dbService;
    private readonly EquipmentTemplateService _templateService;
    private List<EquipmentTemplate> _allTemplates = new();
    
    public ManageTemplatesDialog(LocalDatabaseService dbService, EquipmentTemplateService templateService)
    {
        _dbService = dbService;
        _templateService = templateService;
        
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        Loaded += async (s, e) => await LoadTemplatesAsync();
    }
    
    private async Task LoadTemplatesAsync()
    {
        try
        {
            _allTemplates = await _templateService.GetAllTemplatesAsync();
            UpdateGrid();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading templates: {ex.Message}");
        }
    }
    
    private void UpdateGrid()
    {
        var search = SearchBox.Text?.ToLower() ?? "";
        
        var filtered = string.IsNullOrWhiteSpace(search)
            ? _allTemplates
            : _allTemplates.Where(t => 
                t.TemplateName.ToLower().Contains(search) ||
                (t.Description?.ToLower().Contains(search) ?? false) ||
                (t.CategoryName?.ToLower().Contains(search) ?? false)).ToList();
        
        TemplatesGrid.ItemsSource = filtered.OrderBy(t => t.TemplateName).ToList();
        EmptyState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateGrid();
    }
    
    private void TemplatesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = TemplatesGrid.SelectedItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }
    
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (TemplatesGrid.SelectedItem is not EquipmentTemplate template) return;
        
        var dialog = new EditTemplateDialog(template);
        dialog.Owner = this;
        
        if (dialog.ShowDialog() == true)
        {
            template.TemplateName = dialog.TemplateName;
            template.Description = dialog.TemplateDescription;
            _ = _templateService.UpdateTemplateAsync(template);
            _ = LoadTemplatesAsync();
        }
    }
    
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (TemplatesGrid.SelectedItem is not EquipmentTemplate template) return;
        
        var result = MessageBox.Show(
            $"Delete template '{template.TemplateName}'?\n\nThis cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;
        
        if (_templateService.DeleteTemplate(template.TemplateId))
        {
            _ = LoadTemplatesAsync();
        }
        else
        {
            MessageBox.Show("Failed to delete template.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Templates",
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"EquipmentTemplates_{DateTime.Now:yyyyMMdd}.json"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            var path = await _templateService.ExportTemplatesAsync(dialog.FileName);
            MessageBox.Show($"Exported {_allTemplates.Count} templates to:\n{path}",
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Templates",
            Filter = "JSON Files (*.json)|*.json"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            var count = await _templateService.ImportTemplatesAsync(dialog.FileName);
            MessageBox.Show($"Imported {count} templates.",
                "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
