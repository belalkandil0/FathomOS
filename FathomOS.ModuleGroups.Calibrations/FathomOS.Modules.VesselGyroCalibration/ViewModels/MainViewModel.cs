using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.VesselGyroCalibration.Models;
using FathomOS.Modules.VesselGyroCalibration.Services;
using FathomOS.Modules.VesselGyroCalibration.ViewModels.Steps;

namespace FathomOS.Modules.VesselGyroCalibration.ViewModels;

/// <summary>
/// Main ViewModel for the Vessel Gyro Calibration wizard.
/// Orchestrates the 7-step "Fathom 7 Process" workflow.
/// </summary>
public class MainViewModel : ViewModelBase
{
    #region Fields

    private int _currentStepIndex;
    private WizardStepViewModelBase? _currentStepViewModel;
    private string _statusMessage = "Ready";
    private bool _isBusy;
    private double _progressPercentage;

    // Services
    private readonly GyroCalculationService _calculationService;
    private readonly DataParsingService _parsingService;

    #endregion

    #region Constructor

    public MainViewModel()
    {
        try
        {
            // Initialize services
            _calculationService = new GyroCalculationService();
            _parsingService = new DataParsingService();

            // Initialize shared data
            Project = new CalibrationProject();
            DataPoints = new ObservableCollection<GyroDataPoint>();
            Result = new CalibrationResult();
            Validation = new ValidationResult();
            ColumnMapping = new GyroColumnMapping();
            
            // Initialize unit context for dynamic labels
            UnitContext = new UnitContext();
            UnitContext.PropertyChanged += (s, e) => OnPropertyChanged(nameof(UnitAbbreviation));

            // Initialize step ViewModels
            InitializeSteps();

            // Initialize commands
            NextCommand = new RelayCommand(_ => GoToNextStep(), _ => CanGoNext);
            PreviousCommand = new RelayCommand(_ => GoToPreviousStep(), _ => CanGoPrevious);
            GoToStepCommand = new RelayCommand(p => GoToStep(Convert.ToInt32(p)), p => CanGoToStep(Convert.ToInt32(p)));
            ProcessDataCommand = new AsyncRelayCommand(_ => ProcessDataAsync(), _ => CanProcess);
            ExportReportCommand = new AsyncRelayCommand(_ => ExportReportAsync(), _ => CanExport);
            ShowHelpCommand = new RelayCommand(_ => ShowHelp());

            // Start at step 1
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

    /// <summary>
    /// Project configuration and metadata
    /// </summary>
    public CalibrationProject Project { get; }

    /// <summary>
    /// Loaded and processed data points
    /// </summary>
    public ObservableCollection<GyroDataPoint> DataPoints { get; }

    /// <summary>
    /// Calculation results
    /// </summary>
    public CalibrationResult Result { get; set; }

    /// <summary>
    /// QC validation results
    /// </summary>
    public ValidationResult Validation { get; set; }

    /// <summary>
    /// Column mapping configuration
    /// </summary>
    public GyroColumnMapping ColumnMapping { get; }

    /// <summary>
    /// Path to the loaded data file
    /// </summary>
    public string? LoadedFilePath { get; set; }

    /// <summary>
    /// Preview info for the loaded file
    /// </summary>
    public FilePreviewInfo? FilePreview { get; set; }

    /// <summary>
    /// Available columns from the loaded file
    /// </summary>
    public ObservableCollection<string> AvailableColumns { get; } = new();

    /// <summary>
    /// Raw file data loaded in Step 2 (before column mapping)
    /// </summary>
    public RawFileData? RawFileData { get; set; }

    #endregion

    #region Wizard Navigation Properties

    /// <summary>
    /// Collection of all step ViewModels
    /// </summary>
    public ObservableCollection<WizardStepViewModelBase> Steps { get; } = new();

    /// <summary>
    /// Current step index (0-based)
    /// </summary>
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

    /// <summary>
    /// Current step number (1-based, for display)
    /// </summary>
    public int CurrentStepNumber => CurrentStepIndex + 1;

    /// <summary>
    /// Currently active step ViewModel
    /// </summary>
    public WizardStepViewModelBase? CurrentStepViewModel
    {
        get => _currentStepViewModel;
        private set => SetProperty(ref _currentStepViewModel, value);
    }

    public bool IsFirstStep => CurrentStepIndex == 0;
    public bool IsLastStep => CurrentStepIndex == Steps.Count - 1;
    public bool CanGoNext => !IsLastStep && CurrentStepViewModel?.CanProceed == true && !IsBusy;
    public bool CanGoPrevious => !IsFirstStep && !IsBusy;

    /// <summary>
    /// Called by step view models when their CanProceed status changes
    /// </summary>
    public void RaiseCanGoNextChanged()
    {
        OnPropertyChanged(nameof(CanGoNext));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    public string NextButtonText => IsLastStep ? "Finish" : "Next ▶";

    #endregion

    #region Status Properties

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
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
            }
        }
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
    public ICommand ExportReportCommand { get; }
    public ICommand ShowHelpCommand { get; }

    #endregion

    #region Initialization

    private void InitializeSteps()
    {
        Steps.Clear();

        // Create all 7 step ViewModels with new workflow
        Steps.Add(new Step1SelectViewModel(this) 
        { 
            StepNumber = 1, 
            StepTitle = "Project Setup",
            StepDescription = "Enter project details and select calibration/verification mode"
        });
        
        Steps.Add(new Step2ImportViewModel(this) 
        { 
            StepNumber = 2, 
            StepTitle = "Load File",
            StepDescription = "Load NPD/CSV data file and preview contents"
        });
        
        Steps.Add(new Step3ConfigureViewModel(this) 
        { 
            StepNumber = 3, 
            StepTitle = "Map Columns",
            StepDescription = "Select which columns contain Reference and Calibrated headings"
        });
        
        Steps.Add(new Step4ProcessViewModel(this) 
        { 
            StepNumber = 4, 
            StepTitle = "Filter & Preview",
            StepDescription = "Filter data by time range and preview C-O calculations"
        });
        
        Steps.Add(new Step5AnalyzeViewModel(this) 
        { 
            StepNumber = 5, 
            StepTitle = "Process",
            StepDescription = "Apply 3-sigma outlier rejection and calculate statistics"
        });
        
        Steps.Add(new Step6ValidateViewModel(this) 
        { 
            StepNumber = 6, 
            StepTitle = "Validate",
            StepDescription = "Review QC checks and validation results"
        });
        
        Steps.Add(new Step7ExportViewModel(this) 
        { 
            StepNumber = 7, 
            StepTitle = "Export",
            StepDescription = "Generate reports and export data"
        });
    }

    #endregion

    #region Navigation Methods

    private void GoToStep(int index)
    {
        if (index < 0 || index >= Steps.Count) return;

        // Deactivate current step
        CurrentStepViewModel?.OnDeactivated();
        if (CurrentStepViewModel != null)
        {
            CurrentStepViewModel.IsActive = false;
        }

        // Activate new step
        CurrentStepIndex = index;
        CurrentStepViewModel = Steps[index];
        CurrentStepViewModel.IsActive = true;
        CurrentStepViewModel.OnActivated();

        // Update progress
        ProgressPercentage = (index + 1) / (double)Steps.Count * 100;
        StatusMessage = $"Step {CurrentStepNumber} of {Steps.Count}: {CurrentStepViewModel.StepTitle}";
    }

    private bool CanGoToStep(int index)
    {
        if (index < 0 || index >= Steps.Count) return false;
        if (IsBusy) return false;

        // Can always go back
        if (index < CurrentStepIndex) return true;

        // Can only go forward if all previous steps are complete
        for (int i = 0; i < index; i++)
        {
            if (!Steps[i].IsCompleted) return false;
        }
        return true;
    }

    private void GoToNextStep()
    {
        if (!CanGoNext) return;

        // Validate current step
        if (CurrentStepViewModel?.Validate() != true)
        {
            StatusMessage = "Please complete all required fields before proceeding.";
            return;
        }

        // Mark current step as completed
        if (CurrentStepViewModel != null)
        {
            CurrentStepViewModel.IsCompleted = true;
        }

        GoToStep(CurrentStepIndex + 1);
    }

    private void GoToPreviousStep()
    {
        if (!CanGoPrevious) return;
        GoToStep(CurrentStepIndex - 1);
    }

    #endregion

    #region Processing Methods

    private bool CanProcess => LoadedFilePath != null && ColumnMapping.IsValid;

    public async Task ProcessDataAsync()
    {
        if (!CanProcess) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Processing data...";

            await Task.Run(() =>
            {
                // Parse the data file
                var points = _parsingService.ParseFile(LoadedFilePath!, ColumnMapping);

                // Update UI collection on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DataPoints.Clear();
                    foreach (var point in points)
                    {
                        DataPoints.Add(point);
                    }
                });

                // Process calculations
                _calculationService.ProcessDataPoints(points);

                // Calculate results
                Result = _calculationService.CalculateResults(points);

                // Run validation
                Validation = _calculationService.ValidateResults(Result, Project);
            });

            StatusMessage = $"Processed {DataPoints.Count} observations. {Result.RejectedCount} rejected.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Processing failed: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Export Methods

    private readonly ReportExportService _reportExportService = new();
    
    private bool CanExport => DataPoints.Count > 0 && Result != null;

    /// <summary>
    /// Export with folder dialog
    /// </summary>
    public async Task ExportReportAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Export Location",
            FileName = $"{Project.ProjectName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
            DefaultExt = ".pdf",
            Filter = "PDF Files|*.pdf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var outputDirectory = System.IO.Path.GetDirectoryName(dialog.FileName) ?? "";
            await ExportReportAsync(outputDirectory, exportPdf: true, exportExcel: true, exportCsv: false);
        }
    }

    public async Task ExportReportAsync(string outputDirectory, bool exportPdf, bool exportExcel, bool exportCsv)
    {
        if (!CanExport) return;

        try
        {
            IsBusy = true;
            var baseName = $"{Project.ProjectName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}";
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
            
            // Open folder
            if (exportedFiles.Count > 0)
            {
                try { System.Diagnostics.Process.Start("explorer.exe", outputDirectory); } catch { }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
            System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Public Methods for Step ViewModels

    /// <summary>
    /// Called by Step 2 when a file is loaded
    /// </summary>
    public void OnFileLoaded(string filePath, FilePreviewInfo preview)
    {
        LoadedFilePath = filePath;
        FilePreview = preview;

        AvailableColumns.Clear();
        foreach (var col in preview.Columns)
        {
            AvailableColumns.Add(col);
        }

        // Try to auto-detect column mappings
        var autoMapping = _parsingService.AutoDetectColumns(preview.Columns);
        ColumnMapping.TimeColumnIndex = autoMapping.TimeColumnIndex;
        ColumnMapping.ReferenceHeadingColumnIndex = autoMapping.ReferenceHeadingColumnIndex;
        ColumnMapping.CalibratedHeadingColumnIndex = autoMapping.CalibratedHeadingColumnIndex;

        StatusMessage = $"Loaded {preview.TotalRows} rows from {preview.FileName}";
    }

    /// <summary>
    /// Gets the calculation service for step ViewModels
    /// </summary>
    public GyroCalculationService CalculationService => _calculationService;

    /// <summary>
    /// Gets the parsing service for step ViewModels
    /// </summary>
    public DataParsingService ParsingService => _parsingService;

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
