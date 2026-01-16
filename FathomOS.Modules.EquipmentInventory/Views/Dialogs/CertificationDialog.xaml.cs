using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class CertificationDialog : MetroWindow
{
    public CertificationDialog(LocalDatabaseService dbService, Certification? certification = null, Equipment? equipment = null)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        var viewModel = new CertificationDialogViewModel(dbService, certification, equipment);
        viewModel.RequestClose += (s, result) =>
        {
            DialogResult = result;
            Close();
        };
        DataContext = viewModel;
    }
    
    public Certification? SavedCertification => (DataContext as CertificationDialogViewModel)?.SavedCertification;
}

public class CertificationDialogViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly Certification _certification;
    private readonly bool _isNew;
    
    public CertificationDialogViewModel(LocalDatabaseService dbService, Certification? certification = null, Equipment? equipment = null)
    {
        _dbService = dbService;
        _isNew = certification == null;
        _certification = certification ?? new Certification
        {
            CertificationId = Guid.NewGuid(),
            IssueDate = DateTime.Today,
            ExpiryDate = DateTime.Today.AddYears(1)
        };
        
        if (equipment != null)
        {
            _certification.EquipmentId = equipment.EquipmentId;
            SelectedEquipment = equipment;
        }
        
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));
        
        LoadData();
    }
    
    public event EventHandler<bool>? RequestClose;
    
    public Certification? SavedCertification { get; private set; }
    
    public string DialogTitle => _isNew ? "Add Certification" : "Edit Certification";
    
    public ObservableCollection<Equipment> EquipmentList { get; } = new();
    
    private Equipment? _selectedEquipment;
    public Equipment? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (SetProperty(ref _selectedEquipment, value))
            {
                if (value != null)
                    _certification.EquipmentId = value.EquipmentId;
                OnPropertyChanged(nameof(HasSelectedEquipment));
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }
    
    public bool HasSelectedEquipment => SelectedEquipment != null;
    
    public string CertificateType
    {
        get => _certification.CertificateType;
        set { _certification.CertificateType = value; OnPropertyChanged(); }
    }
    
    public string? CertificateNumber
    {
        get => _certification.CertificateNumber;
        set { _certification.CertificateNumber = value; OnPropertyChanged(); }
    }
    
    public string? CertifyingBody
    {
        get => _certification.CertifyingBody;
        set { _certification.CertifyingBody = value; OnPropertyChanged(); }
    }
    
    public DateTime IssueDate
    {
        get => _certification.IssueDate;
        set { _certification.IssueDate = value; OnPropertyChanged(); }
    }
    
    public DateTime ExpiryDate
    {
        get => _certification.ExpiryDate;
        set { _certification.ExpiryDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }
    
    public string? Notes
    {
        get => _certification.Notes;
        set { _certification.Notes = value; OnPropertyChanged(); }
    }
    
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool CanSave => SelectedEquipment != null && ExpiryDate > DateTime.MinValue;
    
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    private async void LoadData()
    {
        try
        {
            var equipment = await _dbService.GetAllEquipmentAsync();
            EquipmentList.Clear();
            foreach (var eq in equipment.Where(e => e.IsActive).OrderBy(e => e.Name))
            {
                EquipmentList.Add(eq);
            }
            
            // If editing, select the equipment
            if (!_isNew && _certification.EquipmentId != Guid.Empty)
            {
                SelectedEquipment = EquipmentList.FirstOrDefault(e => e.EquipmentId == _certification.EquipmentId);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
        }
    }
    
    private async void Save()
    {
        try
        {
            StatusMessage = "Saving...";
            
            await _dbService.SaveCertificationAsync(_certification);
            
            SavedCertification = _certification;
            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }
}
