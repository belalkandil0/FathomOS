using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

/// <summary>
/// ViewModel for equipment search dialog.
/// Used when adding equipment manually during manifest creation or verification.
/// </summary>
public class EquipmentSearchViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    
    private string _searchText = string.Empty;
    private Equipment? _selectedEquipment;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool? _dialogResult;
    
    public EquipmentSearchViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        SearchResults = new ObservableCollection<Equipment>();
        
        InitializeCommands();
    }
    
    #region Properties
    
    public ObservableCollection<Equipment> SearchResults { get; }
    
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
    
    public Equipment? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (SetProperty(ref _selectedEquipment, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }
    
    public bool HasSelection => SelectedEquipment != null;
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }
    
    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }
    
    public int ResultCount => SearchResults.Count;
    
    public bool ShowEmptyState => !IsBusy && SearchResults.Count == 0;
    
    public string EmptyStateMessage => string.IsNullOrWhiteSpace(SearchText)
        ? "Enter a search term to find equipment"
        : "No equipment found matching your search";
    
    #endregion
    
    #region Commands
    
    public ICommand SearchCommand { get; private set; } = null!;
    public ICommand SelectCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    
    private void InitializeCommands()
    {
        SearchCommand = new AsyncRelayCommand(async _ => await SearchAsync());
        SelectCommand = new RelayCommand(_ => Select(), _ => HasSelection);
        CancelCommand = new RelayCommand(_ => Cancel());
    }
    
    #endregion
    
    #region Methods
    
    private async Task SearchAsync()
    {
        IsBusy = true;
        StatusMessage = "Searching...";
        
        try
        {
            SearchResults.Clear();
            
            var results = await _dbService.GetEquipmentAsync(search: SearchText);
            
            foreach (var item in results.Take(100)) // Limit results
            {
                SearchResults.Add(item);
            }
            
            OnPropertyChanged(nameof(ResultCount));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyStateMessage));
            
            StatusMessage = SearchResults.Count == 0 
                ? "No results found" 
                : "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void Select()
    {
        if (SelectedEquipment != null)
        {
            DialogResult = true;
        }
    }
    
    private void Cancel()
    {
        DialogResult = false;
    }
    
    #endregion
}
