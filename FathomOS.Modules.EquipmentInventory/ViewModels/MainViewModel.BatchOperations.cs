using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.Views.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.ViewModels;

/// <summary>
/// Partial class for batch operations, templates, and favorites functionality
/// </summary>
public partial class MainViewModel
{
    #region Batch Selection
    
    private ObservableCollection<Equipment> _selectedEquipmentItems = new();
    private bool _isSelectionMode;
    private bool _selectAll;
    
    /// <summary>
    /// Collection of selected equipment items for batch operations
    /// </summary>
    public ObservableCollection<Equipment> SelectedEquipmentItems
    {
        get => _selectedEquipmentItems;
        set
        {
            SetProperty(ref _selectedEquipmentItems, value);
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }
    
    /// <summary>
    /// Whether batch selection mode is active
    /// </summary>
    public bool IsSelectionMode
    {
        get => _isSelectionMode;
        set
        {
            SetProperty(ref _isSelectionMode, value);
            if (!value)
            {
                SelectedEquipmentItems.Clear();
                SelectAll = false;
            }
        }
    }
    
    /// <summary>
    /// Select all checkbox state
    /// </summary>
    public bool SelectAll
    {
        get => _selectAll;
        set
        {
            SetProperty(ref _selectAll, value);
            if (value)
            {
                SelectedEquipmentItems.Clear();
                foreach (var item in Equipment)
                    SelectedEquipmentItems.Add(item);
            }
            else if (SelectedEquipmentItems.Count == Equipment.Count)
            {
                SelectedEquipmentItems.Clear();
            }
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }
    
    public int SelectedCount => SelectedEquipmentItems.Count;
    public bool HasSelectedItems => SelectedEquipmentItems.Count > 0;
    public string SelectionSummary => SelectedCount == 0 ? "" : $"{SelectedCount} item{(SelectedCount > 1 ? "s" : "")} selected";
    
    /// <summary>
    /// Toggle selection of a single equipment item
    /// </summary>
    public void ToggleSelection(Equipment equipment)
    {
        if (SelectedEquipmentItems.Contains(equipment))
            SelectedEquipmentItems.Remove(equipment);
        else
            SelectedEquipmentItems.Add(equipment);
        
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedItems));
        OnPropertyChanged(nameof(SelectionSummary));
        
        // Update SelectAll state
        _selectAll = SelectedEquipmentItems.Count == Equipment.Count && Equipment.Count > 0;
        OnPropertyChanged(nameof(SelectAll));
    }
    
    /// <summary>
    /// Check if an equipment item is selected
    /// </summary>
    public bool IsSelected(Equipment equipment) => SelectedEquipmentItems.Contains(equipment);
    
    #endregion
    
    #region Batch Operation Commands
    
    public ICommand ToggleSelectionModeCommand { get; private set; } = null!;
    public ICommand SelectAllCommand { get; private set; } = null!;
    public ICommand ClearSelectionCommand { get; private set; } = null!;
    public ICommand BatchUpdateStatusCommand { get; private set; } = null!;
    public ICommand BatchUpdateLocationCommand { get; private set; } = null!;
    public ICommand BatchDeleteCommand { get; private set; } = null!;
    public ICommand BatchPrintLabelsCommand { get; private set; } = null!;
    public ICommand BatchExportLabelsCommand { get; private set; } = null!;
    public ICommand DuplicateEquipmentCommand { get; private set; } = null!;
    
    // Template Commands
    public ICommand SaveAsTemplateCommand { get; private set; } = null!;
    public ICommand CreateFromTemplateCommand { get; private set; } = null!;
    public ICommand ManageTemplatesCommand { get; private set; } = null!;
    
    // Favorites Commands
    public ICommand ToggleFavoriteCommand { get; private set; } = null!;
    public ICommand ViewFavoritesCommand { get; private set; } = null!;
    public ICommand ClearFavoritesCommand { get; private set; } = null!;
    
    private void InitializeBatchCommands()
    {
        var batchService = new BatchOperationsService(_dbService);
        var templateService = new EquipmentTemplateService(_dbService);
        var favoritesService = new FavoritesService(_dbService);
        
        ToggleSelectionModeCommand = new RelayCommand(_ => IsSelectionMode = !IsSelectionMode);
        SelectAllCommand = new RelayCommand(_ => SelectAll = !SelectAll);
        ClearSelectionCommand = new RelayCommand(_ => 
        {
            SelectedEquipmentItems.Clear();
            SelectAll = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(SelectionSummary));
        });
        
        BatchUpdateStatusCommand = new AsyncRelayCommand(async p => await BatchUpdateStatusAsync(batchService, p), _ => HasSelectedItems);
        BatchUpdateLocationCommand = new AsyncRelayCommand(async _ => await BatchUpdateLocationAsync(batchService), _ => HasSelectedItems);
        BatchDeleteCommand = new AsyncRelayCommand(async _ => await BatchDeleteAsync(batchService), _ => HasSelectedItems);
        BatchPrintLabelsCommand = new AsyncRelayCommand(async _ => await BatchPrintLabelsAsync(batchService), _ => HasSelectedItems);
        BatchExportLabelsCommand = new AsyncRelayCommand(async _ => await BatchExportLabelsAsync(batchService), _ => HasSelectedItems);
        DuplicateEquipmentCommand = new AsyncRelayCommand(async _ => await DuplicateSelectedEquipmentAsync(batchService), _ => HasSelectedEquipment);
        
        SaveAsTemplateCommand = new AsyncRelayCommand(async _ => await SaveAsTemplateAsync(templateService), _ => HasSelectedEquipment);
        CreateFromTemplateCommand = new RelayCommand(_ => ShowCreateFromTemplateDialog(templateService));
        ManageTemplatesCommand = new RelayCommand(_ => ShowManageTemplatesDialog(templateService));
        
        ToggleFavoriteCommand = new RelayCommand(_ => ToggleFavorite(favoritesService), _ => HasSelectedEquipment);
        ViewFavoritesCommand = new RelayCommand(_ => ShowFavoritesDialog(favoritesService));
        ClearFavoritesCommand = new RelayCommand(_ => favoritesService.ClearFavorites());
    }
    
    #endregion
    
    #region Batch Operations Implementation
    
    private async Task BatchUpdateStatusAsync(BatchOperationsService batchService, object? parameter)
    {
        if (!HasSelectedItems) return;
        
        var statusString = parameter?.ToString();
        if (string.IsNullOrEmpty(statusString)) return;
        
        if (!Enum.TryParse<EquipmentStatus>(statusString, out var newStatus)) return;
        
        var result = MessageBox.Show(
            $"Update status to '{newStatus}' for {SelectedCount} item(s)?",
            "Confirm Status Update",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Updating status for {SelectedCount} items...";
            
            var ids = SelectedEquipmentItems.Select(e => e.EquipmentId).ToList();
            var updateResult = await batchService.BulkUpdateStatusAsync(ids, newStatus, "Batch status update");
            
            if (updateResult.Success)
            {
                StatusMessage = $"Successfully updated {updateResult.UpdatedCount} item(s)";
                await SearchEquipmentAsync();
                SelectedEquipmentItems.Clear();
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelectedItems));
            }
            else
            {
                MessageBox.Show($"Some updates failed:\n{string.Join("\n", updateResult.Errors.Take(5))}",
                    "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task BatchUpdateLocationAsync(BatchOperationsService batchService)
    {
        if (!HasSelectedItems) return;
        
        // Show location selection dialog
        var dialog = new LocationSelectionDialog(_dbService);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() != true || dialog.SelectedLocation == null) return;
        
        var result = MessageBox.Show(
            $"Move {SelectedCount} item(s) to '{dialog.SelectedLocation.Name}'?",
            "Confirm Location Change",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Moving {SelectedCount} items...";
            
            var ids = SelectedEquipmentItems.Select(e => e.EquipmentId).ToList();
            var updateResult = await batchService.BulkUpdateLocationAsync(ids, dialog.SelectedLocation.LocationId);
            
            if (updateResult.Success)
            {
                StatusMessage = $"Successfully moved {updateResult.UpdatedCount} item(s)";
                await SearchEquipmentAsync();
                SelectedEquipmentItems.Clear();
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelectedItems));
            }
            else
            {
                MessageBox.Show($"Some moves failed:\n{string.Join("\n", updateResult.Errors.Take(5))}",
                    "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task BatchDeleteAsync(BatchOperationsService batchService)
    {
        if (!HasSelectedItems) return;
        
        var result = MessageBox.Show(
            $"⚠️ DELETE {SelectedCount} item(s)?\n\nThis action cannot be undone!",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;
        
        // Double confirmation for safety
        result = MessageBox.Show(
            $"Are you SURE you want to permanently delete {SelectedCount} equipment record(s)?",
            "Final Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Deleting {SelectedCount} items...";
            
            var ids = SelectedEquipmentItems.Select(e => e.EquipmentId).ToList();
            var deleteResult = await batchService.BulkDeleteAsync(ids);
            
            if (deleteResult.Success)
            {
                StatusMessage = $"Successfully deleted {deleteResult.UpdatedCount} item(s)";
                await SearchEquipmentAsync();
                SelectedEquipmentItems.Clear();
                IsSelectionMode = false;
            }
            else
            {
                MessageBox.Show($"Some deletes failed:\n{string.Join("\n", deleteResult.Errors.Take(5))}",
                    "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task BatchPrintLabelsAsync(BatchOperationsService batchService)
    {
        if (!HasSelectedItems) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Preparing labels for {SelectedCount} items...";
            
            var ids = SelectedEquipmentItems.Select(e => e.EquipmentId).ToList();
            var printResult = await batchService.PrintLabelsAsync(ids);
            
            if (printResult.Success)
            {
                StatusMessage = $"Printed {printResult.PrintedCount} labels";
                MessageBox.Show($"Successfully printed {printResult.PrintedCount} labels.",
                    "Print Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Print failed:\n{string.Join("\n", printResult.Errors.Take(5))}",
                    "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task BatchExportLabelsAsync(BatchOperationsService batchService)
    {
        if (!HasSelectedItems) return;
        
        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Labels to PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"QR_Labels_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };
        
        if (saveDialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Exporting labels for {SelectedCount} items...";
            
            var ids = SelectedEquipmentItems.Select(e => e.EquipmentId).ToList();
            var filePath = await batchService.ExportLabelsToPdfAsync(ids, saveDialog.FileName);
            
            StatusMessage = "Labels exported successfully";
            
            if (MessageBox.Show($"Labels exported to:\n{filePath}\n\nOpen file?",
                "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task DuplicateSelectedEquipmentAsync(BatchOperationsService batchService)
    {
        if (SelectedEquipment == null) return;
        
        var dialog = new DuplicateEquipmentDialog(SelectedEquipment);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Duplicating equipment...";
            
            var options = new DuplicateOptions
            {
                CopySerialNumber = dialog.CopySerialNumber,
                CopyPurchaseInfo = dialog.CopyPurchaseInfo,
                CopyPhotos = dialog.CopyPhotos,
                CopyDocuments = dialog.CopyDocuments
            };
            
            if (dialog.CopyCount > 1)
            {
                var copies = await batchService.DuplicateMultipleAsync(
                    SelectedEquipment.EquipmentId, dialog.CopyCount, options);
                StatusMessage = $"Created {copies.Count} duplicate(s)";
            }
            else
            {
                var copy = await batchService.DuplicateEquipmentAsync(
                    SelectedEquipment.EquipmentId, options);
                if (copy != null)
                    StatusMessage = $"Created duplicate: {copy.AssetNumber}";
            }
            
            await SearchEquipmentAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    #endregion
    
    #region Template Operations
    
    private async Task SaveAsTemplateAsync(EquipmentTemplateService templateService)
    {
        if (SelectedEquipment == null) return;
        
        var dialog = new SaveAsTemplateDialog(SelectedEquipment);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Saving template...";
            
            await templateService.SaveAsTemplateAsync(SelectedEquipment, dialog.TemplateName, dialog.TemplateDescription);
            
            StatusMessage = $"Template '{dialog.TemplateName}' saved";
            MessageBox.Show($"Template '{dialog.TemplateName}' has been saved.\n\nYou can use it to quickly create similar equipment.",
                "Template Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void ShowCreateFromTemplateDialog(EquipmentTemplateService templateService)
    {
        var dialog = new CreateFromTemplateDialog(_dbService, templateService);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true && dialog.CreatedEquipment != null)
        {
            _ = SearchEquipmentAsync();
            SelectedEquipment = dialog.CreatedEquipment;
            StatusMessage = $"Created {dialog.CreatedEquipment.AssetNumber} from template";
        }
    }
    
    private void ShowManageTemplatesDialog(EquipmentTemplateService templateService)
    {
        var dialog = new ManageTemplatesDialog(_dbService, templateService);
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
    }
    
    #endregion
    
    #region Favorites Operations
    
    private void ToggleFavorite(FavoritesService favoritesService)
    {
        if (SelectedEquipment == null) return;
        
        var wasFavorite = favoritesService.ToggleFavorite(SelectedEquipment.EquipmentId);
        StatusMessage = wasFavorite ? "Added to favorites" : "Removed from favorites";
        OnPropertyChanged(nameof(IsSelectedEquipmentFavorite));
    }
    
    public bool IsSelectedEquipmentFavorite
    {
        get
        {
            if (SelectedEquipment == null) return false;
            var favoritesService = new FavoritesService(_dbService);
            return favoritesService.IsFavorite(SelectedEquipment.EquipmentId);
        }
    }
    
    private void ShowFavoritesDialog(FavoritesService favoritesService)
    {
        var dialog = new FavoritesDialog(_dbService, favoritesService);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true && dialog.SelectedEquipment != null)
        {
            SelectedEquipment = dialog.SelectedEquipment;
        }
    }
    
    #endregion
}
