// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: ViewModels/FieldConfigurationViewModel.cs
// Purpose: ViewModel for configuring NaviPac UDO field mappings
// Version: 9.0.0
// ============================================================================

using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// ViewModel for the Field Configuration window.
/// Allows users to define custom field mappings for NaviPac UDO data.
/// </summary>
public class FieldConfigurationViewModel : ViewModelBase
{
    #region Fields
    
    private readonly ApplicationSettings _settings;
    private UserFieldDefinition? _selectedField;
    private FieldTemplate? _selectedTemplate;
    private string _statusMessage = "Ready";
    private bool _hasChanges;
    
    /// <summary>
    /// Maps display-friendly separator names to actual separator characters.
    /// </summary>
    private Dictionary<string, string> SeparatorMap { get; set; } = new();
    
    /// <summary>
    /// Maps actual separator characters to display-friendly names.
    /// </summary>
    private Dictionary<string, string> ReverseSeparatorMap { get; set; } = new();
    
    #endregion
    
    #region Constructor
    
    /// <summary>
    /// Initializes the FieldConfigurationViewModel with application settings.
    /// </summary>
    public FieldConfigurationViewModel(ApplicationSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        // Initialize collections
        Fields = new ObservableCollection<UserFieldDefinition>();
        Templates = new ObservableCollection<FieldTemplate>();
        DataTypes = Enum.GetValues<FieldDataType>().ToList();
        Separators = new List<string> { "Comma (,)", "Semicolon (;)", "Colon (:)", "Space ( )", "Tab" };
        SeparatorMap = new Dictionary<string, string>
        {
            { "Comma (,)", "," },
            { "Semicolon (;)", ";" },
            { "Colon (:)", ":" },
            { "Space ( )", " " },
            { "Tab", "\t" }
        };
        ReverseSeparatorMap = SeparatorMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        
        // Initialize commands
        AddFieldCommand = new RelayCommand(_ => AddField());
        RemoveFieldCommand = new RelayCommand(_ => RemoveField(), _ => SelectedField != null);
        MoveUpCommand = new RelayCommand(_ => MoveFieldUp(), _ => CanMoveUp());
        MoveDownCommand = new RelayCommand(_ => MoveFieldDown(), _ => CanMoveDown());
        DuplicateFieldCommand = new RelayCommand(_ => DuplicateField(), _ => SelectedField != null);
        ClearAllCommand = new RelayCommand(_ => ClearAllFields(), _ => Fields.Count > 0);
        
        SaveTemplateCommand = new RelayCommand(_ => SaveTemplate(), _ => Fields.Count > 0);
        LoadTemplateCommand = new RelayCommand(_ => LoadTemplate());
        DeleteTemplateCommand = new RelayCommand(_ => DeleteTemplate(), _ => SelectedTemplate != null);
        ApplyTemplateCommand = new RelayCommand(_ => ApplyTemplate(), _ => SelectedTemplate != null);
        
        ImportFromFileCommand = new RelayCommand(_ => ImportFromFile());
        ExportToFileCommand = new RelayCommand(_ => ExportToFile(), _ => Fields.Count > 0);
        
        ResetToDefaultCommand = new RelayCommand(_ => ResetToDefault());
        AutoDetectFieldsCommand = new RelayCommand(_ => AutoDetectFields());
        
        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => Cancel());
        
        // Load existing configuration
        LoadConfiguration();
        LoadTemplates();
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Collection of user-defined fields.
    /// </summary>
    public ObservableCollection<UserFieldDefinition> Fields { get; }
    
    /// <summary>
    /// Collection of saved templates.
    /// </summary>
    public ObservableCollection<FieldTemplate> Templates { get; }
    
    /// <summary>
    /// Available data types for field configuration.
    /// </summary>
    public List<FieldDataType> DataTypes { get; }
    
    /// <summary>
    /// Available separator options.
    /// </summary>
    public List<string> Separators { get; }
    
    /// <summary>
    /// Currently selected field.
    /// </summary>
    public UserFieldDefinition? SelectedField
    {
        get => _selectedField;
        set
        {
            if (SetProperty(ref _selectedField, value))
            {
                OnPropertyChanged(nameof(HasSelectedField));
            }
        }
    }
    
    /// <summary>
    /// Whether a field is selected.
    /// </summary>
    public bool HasSelectedField => SelectedField != null;
    
    /// <summary>
    /// Currently selected template.
    /// </summary>
    public FieldTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }
    
    /// <summary>
    /// Status message for UI feedback.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    /// <summary>
    /// Whether there are unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get => _hasChanges;
        set => SetProperty(ref _hasChanges, value);
    }
    
    /// <summary>
    /// Current separator display name (e.g., "Comma (,)").
    /// Converts to/from actual separator character stored in settings.
    /// </summary>
    public string CurrentSeparator
    {
        get
        {
            var actual = _settings.NaviPacFieldSeparator;
            return ReverseSeparatorMap.TryGetValue(actual, out var display) ? display : "Comma (,)";
        }
        set
        {
            if (SeparatorMap.TryGetValue(value, out var actual))
            {
                if (_settings.NaviPacFieldSeparator != actual)
                {
                    _settings.NaviPacFieldSeparator = actual;
                    OnPropertyChanged();
                    HasChanges = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Event raised when user wants to close the window.
    /// </summary>
    public event Action<bool>? RequestClose;
    
    #endregion
    
    #region Commands
    
    public ICommand AddFieldCommand { get; private set; } = null!;
    public ICommand RemoveFieldCommand { get; private set; } = null!;
    public ICommand MoveUpCommand { get; private set; } = null!;
    public ICommand MoveDownCommand { get; private set; } = null!;
    public ICommand DuplicateFieldCommand { get; private set; } = null!;
    public ICommand ClearAllCommand { get; private set; } = null!;
    
    public ICommand SaveTemplateCommand { get; private set; } = null!;
    public ICommand LoadTemplateCommand { get; private set; } = null!;
    public ICommand DeleteTemplateCommand { get; private set; } = null!;
    public ICommand ApplyTemplateCommand { get; private set; } = null!;
    
    public ICommand ImportFromFileCommand { get; private set; } = null!;
    public ICommand ExportToFileCommand { get; private set; } = null!;
    
    public ICommand ResetToDefaultCommand { get; private set; } = null!;
    public ICommand AutoDetectFieldsCommand { get; private set; } = null!;
    
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    
    #endregion
    
    #region Field Management Methods
    
    private void AddField()
    {
        var newPosition = Fields.Count > 0 ? Fields.Max(f => f.Position) + 1 : 0;
        var newField = new UserFieldDefinition
        {
            Position = newPosition,
            FieldName = $"Field_{newPosition}",
            DataType = FieldDataType.Auto,
            ShowInLog = true
        };
        
        Fields.Add(newField);
        SelectedField = newField;
        HasChanges = true;
        StatusMessage = $"Added field at position {newPosition}";
    }
    
    private void RemoveField()
    {
        if (SelectedField == null) return;
        
        var fieldName = SelectedField.FieldName;
        Fields.Remove(SelectedField);
        SelectedField = Fields.FirstOrDefault();
        RenumberPositions();
        HasChanges = true;
        StatusMessage = $"Removed field '{fieldName}'";
    }
    
    private bool CanMoveUp()
    {
        if (SelectedField == null) return false;
        var index = Fields.IndexOf(SelectedField);
        return index > 0;
    }
    
    private bool CanMoveDown()
    {
        if (SelectedField == null) return false;
        var index = Fields.IndexOf(SelectedField);
        return index >= 0 && index < Fields.Count - 1;
    }
    
    private void MoveFieldUp()
    {
        if (!CanMoveUp()) return;
        
        var index = Fields.IndexOf(SelectedField!);
        Fields.Move(index, index - 1);
        RenumberPositions();
        HasChanges = true;
    }
    
    private void MoveFieldDown()
    {
        if (!CanMoveDown()) return;
        
        var index = Fields.IndexOf(SelectedField!);
        Fields.Move(index, index + 1);
        RenumberPositions();
        HasChanges = true;
    }
    
    private void DuplicateField()
    {
        if (SelectedField == null) return;
        
        var clone = SelectedField.Clone();
        clone.Position = Fields.Max(f => f.Position) + 1;
        clone.FieldName = $"{SelectedField.FieldName}_copy";
        
        Fields.Add(clone);
        SelectedField = clone;
        HasChanges = true;
        StatusMessage = $"Duplicated field to position {clone.Position}";
    }
    
    private void ClearAllFields()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to remove all field definitions?",
            "Confirm Clear All",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            Fields.Clear();
            SelectedField = null;
            HasChanges = true;
            StatusMessage = "All fields cleared";
        }
    }
    
    private void RenumberPositions()
    {
        for (int i = 0; i < Fields.Count; i++)
        {
            Fields[i].Position = i;
        }
    }
    
    #endregion
    
    #region Template Methods
    
    private void SaveTemplate()
    {
        var dialog = new SaveTemplateDialog();
        if (dialog.ShowDialog() == true)
        {
            var template = new FieldTemplate
            {
                Name = dialog.TemplateName,
                Description = dialog.TemplateDescription,
                Separator = CurrentSeparator,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                Fields = Fields.Select(f => f.Clone()).ToList()
            };
            
            // Add or update template
            var existing = Templates.FirstOrDefault(t => t.Name == template.Name);
            if (existing != null)
            {
                var index = Templates.IndexOf(existing);
                Templates[index] = template;
            }
            else
            {
                Templates.Add(template);
            }
            
            SaveTemplates();
            StatusMessage = $"Template '{template.Name}' saved";
        }
    }
    
    private void LoadTemplate()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Field Template",
            Filter = "Field Template (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName);
                var template = JsonSerializer.Deserialize<FieldTemplate>(json, GetJsonOptions());
                
                if (template != null)
                {
                    ApplyTemplateInternal(template);
                    StatusMessage = $"Loaded template from {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading template: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
    
    private void DeleteTemplate()
    {
        if (SelectedTemplate == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Delete template '{SelectedTemplate.Name}'?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var name = SelectedTemplate.Name;
            Templates.Remove(SelectedTemplate);
            SelectedTemplate = null;
            SaveTemplates();
            StatusMessage = $"Deleted template '{name}'";
        }
    }
    
    private void ApplyTemplate()
    {
        if (SelectedTemplate == null) return;
        ApplyTemplateInternal(SelectedTemplate);
        StatusMessage = $"Applied template '{SelectedTemplate.Name}'";
    }
    
    private void ApplyTemplateInternal(FieldTemplate template)
    {
        Fields.Clear();
        foreach (var field in template.Fields)
        {
            Fields.Add(field.Clone());
        }
        
        CurrentSeparator = template.Separator;
        SelectedField = Fields.FirstOrDefault();
        HasChanges = true;
    }
    
    #endregion
    
    #region Import/Export Methods
    
    private void ImportFromFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Field Configuration",
            Filter = "JSON Files (*.json)|*.json|NaviPac UDO Files (*.out2)|*.out2|All Files (*.*)|*.*",
            DefaultExt = ".json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                
                if (extension == ".out2")
                {
                    // Try to parse NaviPac .out2 file format
                    ImportNaviPacOut2File(dialog.FileName);
                }
                else
                {
                    // Assume JSON format
                    var json = File.ReadAllText(dialog.FileName);
                    var template = JsonSerializer.Deserialize<FieldTemplate>(json, GetJsonOptions());
                    
                    if (template != null)
                    {
                        ApplyTemplateInternal(template);
                    }
                }
                
                StatusMessage = $"Imported from {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
    
    private void ImportNaviPacOut2File(string filePath)
    {
        // NaviPac .out2 files are XML-based configuration files
        // This is a simplified parser - real implementation would need full parsing
        var content = File.ReadAllText(filePath);
        
        Fields.Clear();
        var position = 0;
        
        // Look for common NaviPac field patterns
        var fieldPatterns = new Dictionary<string, FieldDataType>
        {
            { "Event", FieldDataType.EventNumber },
            { "Date", FieldDataType.DateTime },
            { "Time", FieldDataType.DateTime },
            { "Easting", FieldDataType.Easting },
            { "Northing", FieldDataType.Northing },
            { "Height", FieldDataType.HeightDepth },
            { "Depth", FieldDataType.HeightDepth },
            { "Lat", FieldDataType.Latitude },
            { "Lon", FieldDataType.Longitude },
            { "KP", FieldDataType.KP },
            { "DCC", FieldDataType.DCC },
            { "DOL", FieldDataType.DOL },
            { "Gyro", FieldDataType.HeadingBearing },
            { "Heading", FieldDataType.HeadingBearing },
            { "Roll", FieldDataType.Roll },
            { "Pitch", FieldDataType.Pitch },
            { "Heave", FieldDataType.Heave },
            { "SMG", FieldDataType.Speed },
            { "CMG", FieldDataType.HeadingBearing }
        };
        
        foreach (var pattern in fieldPatterns)
        {
            if (content.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
            {
                Fields.Add(new UserFieldDefinition
                {
                    Position = position++,
                    FieldName = pattern.Key,
                    DataType = pattern.Value,
                    ShowInLog = true
                });
            }
        }
        
        if (Fields.Count == 0)
        {
            System.Windows.MessageBox.Show("Could not detect fields from the .out2 file. Please configure manually.",
                "Import Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        
        HasChanges = true;
    }
    
    private void ExportToFile()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Field Configuration",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = "FieldConfiguration"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var template = new FieldTemplate
                {
                    Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                    Description = "Exported field configuration",
                    Separator = CurrentSeparator,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    Fields = Fields.Select(f => f.Clone()).ToList()
                };
                
                var json = JsonSerializer.Serialize(template, GetJsonOptions());
                File.WriteAllText(dialog.FileName, json);
                
                StatusMessage = $"Exported to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private void ResetToDefault()
    {
        var result = System.Windows.MessageBox.Show(
            "Reset to default field configuration? This will replace all current fields.",
            "Confirm Reset",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var defaults = UserFieldDefinition.CreateDefaultFields();
            
            Fields.Clear();
            foreach (var field in defaults)
            {
                Fields.Add(field);
            }
            
            SelectedField = Fields.FirstOrDefault();
            HasChanges = true;
            StatusMessage = "Reset to default configuration";
        }
    }
    
    private void AutoDetectFields()
    {
        System.Windows.MessageBox.Show(
            "Auto-detect requires sample data from NaviPac.\n\n" +
            "Please use the Data Monitor to view incoming data,\n" +
            "then manually configure fields based on the detected format.",
            "Auto-Detect Fields",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
    
    private void LoadConfiguration()
    {
        Fields.Clear();
        
        if (_settings.NaviPacFields != null && _settings.NaviPacFields.Count > 0)
        {
            foreach (var field in _settings.NaviPacFields)
            {
                Fields.Add(field.Clone());
            }
        }
        else
        {
            // Load defaults if no configuration exists
            foreach (var field in UserFieldDefinition.CreateDefaultFields())
            {
                Fields.Add(field);
            }
        }
        
        SelectedField = Fields.FirstOrDefault();
        HasChanges = false;
    }
    
    private void LoadTemplates()
    {
        Templates.Clear();
        
        // Add built-in templates
        Templates.Add(FieldTemplate.CreateDefaultTemplate());
        Templates.Add(FieldTemplate.CreateMinimalTemplate());
        
        // Load saved templates from settings directory
        try
        {
            var templatesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FathomOS", "SurveyLogbook", "Templates");
            
            if (Directory.Exists(templatesPath))
            {
                foreach (var file in Directory.GetFiles(templatesPath, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var template = JsonSerializer.Deserialize<FieldTemplate>(json, GetJsonOptions());
                        if (template != null)
                        {
                            Templates.Add(template);
                        }
                    }
                    catch
                    {
                        // Skip invalid template files
                    }
                }
            }
        }
        catch
        {
            // Ignore errors loading templates
        }
    }
    
    private void SaveTemplates()
    {
        try
        {
            var templatesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FathomOS", "SurveyLogbook", "Templates");
            
            Directory.CreateDirectory(templatesPath);
            
            // Save user templates (skip built-in)
            foreach (var template in Templates.Where(t => 
                t.Name != "Standard NaviPac UDO" && t.Name != "Minimal Position"))
            {
                var json = JsonSerializer.Serialize(template, GetJsonOptions());
                var filePath = Path.Combine(templatesPath, $"{template.Name}.json");
                File.WriteAllText(filePath, json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving templates: {ex.Message}");
        }
    }
    
    private void Save()
    {
        // Save fields to settings
        _settings.NaviPacFields = Fields.Select(f => f.Clone()).ToList();
        _settings.Save();
        
        HasChanges = false;
        StatusMessage = "Configuration saved";
        RequestClose?.Invoke(true);
    }
    
    private void Cancel()
    {
        if (HasChanges)
        {
            var result = System.Windows.MessageBox.Show(
                "You have unsaved changes. Discard changes?",
                "Confirm Cancel",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            
            if (result != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }
        }
        
        RequestClose?.Invoke(false);
    }
    
    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    #endregion
}

/// <summary>
/// Simple dialog for entering template name and description.
/// </summary>
public class SaveTemplateDialog : Window
{
    public string TemplateName { get; private set; } = string.Empty;
    public string TemplateDescription { get; private set; } = string.Empty;
    
    public SaveTemplateDialog()
    {
        Title = "Save Template";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        
        var grid = new System.Windows.Controls.Grid();
        grid.Margin = new Thickness(15);
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        
        var nameLabel = new System.Windows.Controls.Label { Content = "Template Name:" };
        System.Windows.Controls.Grid.SetRow(nameLabel, 0);
        
        var nameBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 5, 0, 10) };
        System.Windows.Controls.Grid.SetRow(nameBox, 1);
        
        var descLabel = new System.Windows.Controls.Label { Content = "Description:" };
        System.Windows.Controls.Grid.SetRow(descLabel, 2);
        
        var descBox = new System.Windows.Controls.TextBox 
        { 
            Margin = new Thickness(0, 5, 0, 10),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = 50
        };
        System.Windows.Controls.Grid.SetRow(descBox, 3);
        
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        System.Windows.Controls.Grid.SetRow(buttonPanel, 5);
        
        var okButton = new System.Windows.Controls.Button
        {
            Content = "Save",
            Width = 80,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                System.Windows.MessageBox.Show("Please enter a template name.", "Validation", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            TemplateName = nameBox.Text.Trim();
            TemplateDescription = descBox.Text.Trim();
            DialogResult = true;
        };
        
        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            Width = 80,
            IsCancel = true
        };
        
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        
        grid.Children.Add(nameLabel);
        grid.Children.Add(nameBox);
        grid.Children.Add(descLabel);
        grid.Children.Add(descBox);
        grid.Children.Add(buttonPanel);
        
        Content = grid;
    }
}
