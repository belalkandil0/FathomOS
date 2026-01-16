using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.RovGyroCalibration.Models;
using FathomOS.Modules.RovGyroCalibration.Services;
using FathomOS.Modules.RovGyroCalibration.ViewModels.Steps;

namespace FathomOS.Modules.RovGyroCalibration.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Fields
    private int _currentStepIndex;
    private WizardStepViewModelBase? _currentStepViewModel;
    private string _statusMessage = "Ready";
    private bool _isBusy;
    private double _progressPercentage;

    private readonly RovGyroCalculationService _calculationService;
    private readonly DataParsingService _parsingService;
    private readonly Visualization3DService _visualization3DService;
    private readonly ReportExportService _reportExportService;
    #endregion

    #region Constructor
    public MainViewModel()
    {
        try
        {
            _calculationService = new RovGyroCalculationService();
            _parsingService = new DataParsingService();
            _visualization3DService = new Visualization3DService();
            _reportExportService = new ReportExportService();

            Project = new CalibrationProject();
            DataPoints = new ObservableCollection<RovGyroDataPoint>();
            Result = new CalibrationResult();
            Validation = new ValidationResult();
            ColumnMapping = new RovGyroColumnMapping();
            
            // Initialize unit context for dynamic labels
            UnitContext = new UnitContext();
            UnitContext.PropertyChanged += (s, e) => OnPropertyChanged(nameof(UnitAbbreviation));

            InitializeSteps();

            NextCommand = new RelayCommand(_ => GoToNextStep(), _ => CanGoNext);
            PreviousCommand = new RelayCommand(_ => GoToPreviousStep(), _ => CanGoPrevious);
            GoToStepCommand = new RelayCommand(p => GoToStep(Convert.ToInt32(p)), p => CanGoToStep(Convert.ToInt32(p)));
            ProcessDataCommand = new AsyncRelayCommand(_ => ProcessDataAsync(), _ => CanProcess);
            ShowHelpCommand = new RelayCommand(_ => ShowHelp());

            GoToStep(0);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"MainViewModel init error:\n{ex.Message}\n\nStack:\n{ex.StackTrace}",
                "ViewModel Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            throw;
        }
    }
    #endregion

    #region Shared Data Properties
    
    /// <summary>
    /// Unit context for dynamic unit labels throughout the wizard
    /// </summary>
    public UnitContext UnitContext { get; }
    
    /// <summary>
    /// Current unit abbreviation for display
    /// </summary>
    public string UnitAbbreviation => UnitConversionService.GetAbbreviation(Project.DisplayUnit);
    
    /// <summary>
    /// Update unit context when user changes display unit
    /// </summary>
    public void UpdateDisplayUnit(LengthUnit unit)
    {
        Project.DisplayUnit = unit;
        UnitContext.DisplayUnit = unit;
        OnPropertyChanged(nameof(UnitAbbreviation));
    }
    
    public CalibrationProject Project { get; }
    public ObservableCollection<RovGyroDataPoint> DataPoints { get; }
    public CalibrationResult Result { get; set; }
    public ValidationResult Validation { get; set; }
    public RovGyroColumnMapping ColumnMapping { get; }
    public RovConfiguration RovConfiguration { get; } = new();
    public string? LoadedFilePath { get; set; }
    public FilePreviewInfo? FilePreview { get; set; }
    public ObservableCollection<string> AvailableColumns { get; } = new();
    public RawFileData? RawFileData { get; set; }
    #endregion

    #region Services
    public RovGyroCalculationService CalculationService => _calculationService;
    public DataParsingService ParsingService => _parsingService;
    public Visualization3DService Visualization3DService => _visualization3DService;
    #endregion

    #region Wizard Navigation
    public ObservableCollection<WizardStepViewModelBase> Steps { get; } = new();

    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set
        {
            if (SetProperty(ref _currentStepIndex, value))
            {
                OnPropertyChanged(nameof(CurrentStepNumber));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(IsFirstStep));
                OnPropertyChanged(nameof(IsLastStep));
                OnPropertyChanged(nameof(NextButtonText));
            }
        }
    }

    public int CurrentStepNumber => CurrentStepIndex + 1;
    public WizardStepViewModelBase? CurrentStepViewModel
    {
        get => _currentStepViewModel;
        private set => SetProperty(ref _currentStepViewModel, value);
    }

    public bool IsFirstStep => CurrentStepIndex == 0;
    public bool IsLastStep => CurrentStepIndex == Steps.Count - 1;
    public bool CanGoNext => !IsLastStep && CurrentStepViewModel?.CanProceed == true && !IsBusy;
    public bool CanGoPrevious => !IsFirstStep && !IsBusy;
    public string NextButtonText => IsLastStep ? "Finish" : "Next ▶";

    /// <summary>
    /// Called by step view models when their CanProceed status changes
    /// </summary>
    public void RaiseCanGoNextChanged()
    {
        OnPropertyChanged(nameof(CanGoNext));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { if (SetProperty(ref _isBusy, value)) { OnPropertyChanged(nameof(CanGoNext)); OnPropertyChanged(nameof(CanGoPrevious)); } }
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => SetProperty(ref _progressPercentage, value);
    }
    #endregion

    #region Commands
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand GoToStepCommand { get; }
    public ICommand ProcessDataCommand { get; }
    public ICommand ShowHelpCommand { get; }
    #endregion

    #region Initialization
    private void InitializeSteps()
    {
        Steps.Clear();
        Steps.Add(new Step1SetupViewModel(this) { StepNumber = 1, StepTitle = "Project Setup", StepDescription = "Enter project details and configure ROV geometry" });
        Steps.Add(new Step2ImportViewModel(this) { StepNumber = 2, StepTitle = "Load File", StepDescription = "Load NPD/CSV data file" });
        Steps.Add(new Step3ConfigureViewModel(this) { StepNumber = 3, StepTitle = "Map Columns", StepDescription = "Select vessel and ROV heading columns" });
        Steps.Add(new Step4ProcessViewModel(this) { StepNumber = 4, StepTitle = "Filter & Preview", StepDescription = "Filter by time and preview C-O" });
        Steps.Add(new Step5AnalyzeViewModel(this) { StepNumber = 5, StepTitle = "Process", StepDescription = "Apply 3-sigma rejection" });
        Steps.Add(new Step6ValidateViewModel(this) { StepNumber = 6, StepTitle = "Validate", StepDescription = "QC checks" });
        Steps.Add(new Step7ExportViewModel(this) { StepNumber = 7, StepTitle = "Export", StepDescription = "Generate reports" });
    }
    #endregion

    #region Navigation
    private void GoToStep(int index)
    {
        if (index < 0 || index >= Steps.Count) return;
        CurrentStepViewModel?.OnDeactivated();
        if (CurrentStepViewModel != null) CurrentStepViewModel.IsActive = false;

        CurrentStepIndex = index;
        CurrentStepViewModel = Steps[index];
        CurrentStepViewModel.IsActive = true;
        CurrentStepViewModel.OnActivated();

        ProgressPercentage = (index + 1) / (double)Steps.Count * 100;
        StatusMessage = $"Step {CurrentStepNumber} of {Steps.Count}: {CurrentStepViewModel.StepTitle}";
    }

    private bool CanGoToStep(int index)
    {
        if (index < 0 || index >= Steps.Count || IsBusy) return false;
        if (index < CurrentStepIndex) return true;
        for (int i = 0; i < index; i++)
            if (!Steps[i].IsCompleted) return false;
        return true;
    }

    private void GoToNextStep()
    {
        if (!CanGoNext) return;
        if (CurrentStepViewModel?.Validate() != true) { StatusMessage = "Please complete all required fields."; return; }
        if (CurrentStepViewModel != null) CurrentStepViewModel.IsCompleted = true;
        GoToStep(CurrentStepIndex + 1);
    }

    private void GoToPreviousStep()
    {
        if (CanGoPrevious) GoToStep(CurrentStepIndex - 1);
    }
    #endregion

    #region Processing
    private bool CanProcess => LoadedFilePath != null && ColumnMapping.IsValid;

    public async Task ProcessDataAsync()
    {
        if (!CanProcess) return;
        try
        {
            IsBusy = true;
            StatusMessage = "Processing data with geometric corrections...";

            await Task.Run(() =>
            {
                var points = _parsingService.ParseFile(LoadedFilePath!, ColumnMapping);
                System.Windows.Application.Current.Dispatcher.Invoke(() => { DataPoints.Clear(); foreach (var p in points) DataPoints.Add(p); });

                _calculationService.ProcessDataPoints(points, Project.RovConfig);
                Result = _calculationService.CalculateResults(points, Project.RovConfig);
                Validation = _calculationService.ValidateResults(Result, Project);
            });

            StatusMessage = $"Processed {DataPoints.Count} observations. θ={Result.BaselineAngleTheta:F2}°";
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }
    #endregion

    #region File Loading
    public void OnFileLoaded(string filePath, FilePreviewInfo preview)
    {
        LoadedFilePath = filePath;
        FilePreview = preview;
        AvailableColumns.Clear();
        foreach (var col in preview.Columns) AvailableColumns.Add(col);

        var autoMapping = _parsingService.AutoDetectColumns(preview.Columns);
        ColumnMapping.TimeColumnIndex = autoMapping.TimeColumnIndex;
        ColumnMapping.VesselHeadingColumnIndex = autoMapping.VesselHeadingColumnIndex;
        ColumnMapping.RovHeadingColumnIndex = autoMapping.RovHeadingColumnIndex;

        StatusMessage = $"Loaded {preview.TotalRows} rows from {preview.FileName}";
    }
    #endregion

    #region Export Methods
    public async Task ExportReportAsync(string outputDirectory, bool exportPdf, bool exportExcel, bool exportCsv)
    {
        if (DataPoints.Count == 0) return;

        try
        {
            IsBusy = true;
            var baseName = $"{Project.ProjectName.Replace(" ", "_")}_ROV_{DateTime.Now:yyyyMMdd_HHmmss}";
            var dataList = DataPoints.ToList();
            var exportedFiles = new List<string>();

            await Task.Run(() =>
            {
                if (exportPdf)
                {
                    var pdfPath = System.IO.Path.Combine(outputDirectory, $"{baseName}_Report.pdf");
                    StatusMessage = "Generating PDF report...";
                    _reportExportService.ExportPdf(pdfPath, Project, dataList, Result, Validation);
                    exportedFiles.Add(pdfPath);
                }

                if (exportExcel)
                {
                    var xlsxPath = System.IO.Path.Combine(outputDirectory, $"{baseName}_Data.xlsx");
                    StatusMessage = "Generating Excel export...";
                    _reportExportService.ExportExcel(xlsxPath, Project, dataList, Result, Validation);
                    exportedFiles.Add(xlsxPath);
                }

                if (exportCsv)
                {
                    var csvPath = System.IO.Path.Combine(outputDirectory, $"{baseName}_Data.csv");
                    StatusMessage = "Generating CSV export...";
                    _reportExportService.ExportCsv(csvPath, dataList);
                    exportedFiles.Add(csvPath);
                }
            });

            StatusMessage = $"Exported {exportedFiles.Count} file(s) successfully.";
            
            if (exportedFiles.Count > 0)
            {
                try { System.Diagnostics.Process.Start("explorer.exe", outputDirectory); } catch { }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Shows the help/user guide window
    /// </summary>
    private void ShowHelp()
    {
        var helpWindow = new Views.HelpWindow();
        helpWindow.ShowDialog();
    }
    #endregion
}
