// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Views/SurveyLogView.xaml.cs
// Purpose: Survey Log tab user control code-behind
// Version: 9.0.0 - Dynamic column generation
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.IconPacks;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Services;
using FathomOS.Modules.SurveyLogbook.ViewModels;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Interaction logic for SurveyLogView.xaml
/// Handles dynamic column generation based on field configuration.
/// </summary>
public partial class SurveyLogView : UserControl
{
    private SurveyLogViewModel? _viewModel;
    
    /// <summary>
    /// Initializes a new instance of the SurveyLogView class.
    /// </summary>
    public SurveyLogView()
    {
        InitializeComponent();
        
        // Subscribe to DataContext changes to regenerate columns
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Generate columns when the view is loaded
        if (DataContext is SurveyLogViewModel vm)
        {
            _viewModel = vm;
            RegenerateColumns();
        }
    }
    
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (e.OldValue is SurveyLogViewModel oldVm)
        {
            oldVm.FieldConfigurationChanged -= OnFieldConfigurationChanged;
        }
        
        if (e.NewValue is SurveyLogViewModel vm)
        {
            _viewModel = vm;
            
            // Subscribe to field configuration changes
            _viewModel.FieldConfigurationChanged += OnFieldConfigurationChanged;
            
            RegenerateColumns();
        }
    }
    
    private void OnFieldConfigurationChanged(object? sender, EventArgs e)
    {
        // Regenerate columns on UI thread
        Dispatcher.BeginInvoke(() => RegenerateColumns());
    }
    
    /// <summary>
    /// Regenerates the DataGrid columns based on current field configuration.
    /// </summary>
    public void RegenerateColumns()
    {
        // Find the DataGrid in the visual tree
        var dataGrid = FindName("SurveyLogDataGrid") as DataGrid;
        if (dataGrid == null)
        {
            // Try to find it by walking the visual tree
            dataGrid = FindChild<DataGrid>(this);
        }
        
        if (dataGrid == null) return;
        
        // Clear existing columns (except icon and actions columns we'll recreate)
        dataGrid.Columns.Clear();
        
        // Add icon column
        AddIconColumn(dataGrid);
        
        // Add core columns (Time, Date, Type, Description, Source)
        var coreColumns = DynamicColumnService.GetCoreColumns();
        foreach (var colDef in coreColumns)
        {
            // Skip Description and Source - we'll add them after dynamic columns
            if (colDef.BindingPath == "Description" || colDef.BindingPath == "Source")
                continue;
            
            dataGrid.Columns.Add(DynamicColumnService.CreateDataGridColumn(colDef));
        }
        
        // Add Type column with color
        AddTypeColumn(dataGrid);
        
        // Add Description column (expandable)
        var descCol = coreColumns.FirstOrDefault(c => c.BindingPath == "Description");
        if (descCol != null)
        {
            dataGrid.Columns.Add(DynamicColumnService.CreateDataGridColumn(descCol));
        }
        
        // Add dynamic columns from field configuration
        var fieldConfig = _viewModel?.FieldConfiguration;
        var dynamicColumns = DynamicColumnService.GetDynamicColumns(fieldConfig);
        foreach (var colDef in dynamicColumns)
        {
            dataGrid.Columns.Add(DynamicColumnService.CreateDataGridColumn(colDef));
        }
        
        // Add Source column
        var sourceCol = coreColumns.FirstOrDefault(c => c.BindingPath == "Source");
        if (sourceCol != null)
        {
            dataGrid.Columns.Add(DynamicColumnService.CreateDataGridColumn(sourceCol));
        }
        
        // Add actions column
        AddActionsColumn(dataGrid);
    }
    
    /// <summary>
    /// Adds the entry type icon column.
    /// </summary>
    private void AddIconColumn(DataGrid dataGrid)
    {
        var iconColumn = new DataGridTemplateColumn
        {
            Header = "",
            Width = new DataGridLength(40),
            CanUserResize = false
        };
        
        var cellTemplate = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(PackIconMaterial));
        
        factory.SetBinding(PackIconMaterial.KindProperty, 
            new Binding("EntryType") 
            { 
                Converter = (IValueConverter)Resources["EntryTypeToIcon"] 
            });
        factory.SetBinding(PackIconMaterial.ForegroundProperty, 
            new Binding("EntryType") 
            { 
                Converter = (IValueConverter)Resources["EntryTypeToColor"] 
            });
        factory.SetValue(PackIconMaterial.WidthProperty, 16.0);
        factory.SetValue(PackIconMaterial.HeightProperty, 16.0);
        factory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        
        cellTemplate.VisualTree = factory;
        iconColumn.CellTemplate = cellTemplate;
        
        dataGrid.Columns.Add(iconColumn);
    }
    
    /// <summary>
    /// Adds the entry type column with color coding.
    /// </summary>
    private void AddTypeColumn(DataGrid dataGrid)
    {
        var typeColumn = new DataGridTemplateColumn
        {
            Header = "Type",
            Width = new DataGridLength(150)
        };
        
        var cellTemplate = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding("EntryType"));
        factory.SetBinding(TextBlock.ForegroundProperty, 
            new Binding("EntryType") 
            { 
                Converter = (IValueConverter)Resources["EntryTypeToColor"] 
            });
        
        cellTemplate.VisualTree = factory;
        typeColumn.CellTemplate = cellTemplate;
        
        dataGrid.Columns.Add(typeColumn);
    }
    
    /// <summary>
    /// Adds the actions column with View/Delete buttons.
    /// </summary>
    private void AddActionsColumn(DataGrid dataGrid)
    {
        var actionsColumn = new DataGridTemplateColumn
        {
            Header = "",
            Width = new DataGridLength(80),
            CanUserResize = false
        };
        
        var cellTemplate = new DataTemplate();
        var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
        stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        stackFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        
        // View button
        var viewButtonFactory = new FrameworkElementFactory(typeof(Button));
        viewButtonFactory.SetBinding(Button.CommandProperty, 
            new Binding("DataContext.ViewDetailsCommand") 
            { 
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1) 
            });
        viewButtonFactory.SetBinding(Button.CommandParameterProperty, new Binding());
        viewButtonFactory.SetValue(Button.ToolTipProperty, "View Details");
        viewButtonFactory.SetValue(Button.WidthProperty, 28.0);
        viewButtonFactory.SetValue(Button.HeightProperty, 28.0);
        viewButtonFactory.SetResourceReference(Button.StyleProperty, "IconButton");
        
        var viewIconFactory = new FrameworkElementFactory(typeof(PackIconMaterial));
        viewIconFactory.SetValue(PackIconMaterial.KindProperty, PackIconMaterialKind.Eye);
        viewIconFactory.SetValue(PackIconMaterial.WidthProperty, 12.0);
        viewIconFactory.SetValue(PackIconMaterial.HeightProperty, 12.0);
        viewButtonFactory.AppendChild(viewIconFactory);
        
        // Delete button
        var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
        deleteButtonFactory.SetBinding(Button.CommandProperty, 
            new Binding("DataContext.DeleteEntryCommand") 
            { 
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1) 
            });
        deleteButtonFactory.SetBinding(Button.CommandParameterProperty, new Binding());
        deleteButtonFactory.SetValue(Button.ToolTipProperty, "Delete Entry");
        deleteButtonFactory.SetValue(Button.WidthProperty, 28.0);
        deleteButtonFactory.SetValue(Button.HeightProperty, 28.0);
        deleteButtonFactory.SetResourceReference(Button.StyleProperty, "IconButton");
        
        var deleteIconFactory = new FrameworkElementFactory(typeof(PackIconMaterial));
        deleteIconFactory.SetValue(PackIconMaterial.KindProperty, PackIconMaterialKind.Delete);
        deleteIconFactory.SetValue(PackIconMaterial.WidthProperty, 12.0);
        deleteIconFactory.SetValue(PackIconMaterial.HeightProperty, 12.0);
        deleteButtonFactory.AppendChild(deleteIconFactory);
        
        stackFactory.AppendChild(viewButtonFactory);
        stackFactory.AppendChild(deleteButtonFactory);
        
        cellTemplate.VisualTree = stackFactory;
        actionsColumn.CellTemplate = cellTemplate;
        
        dataGrid.Columns.Add(actionsColumn);
    }
    
    /// <summary>
    /// Helper to find a child element of a specific type.
    /// </summary>
    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        
        int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is T typedChild)
                return typedChild;
            
            var result = FindChild<T>(child);
            if (result != null)
                return result;
        }
        
        return null;
    }
}
