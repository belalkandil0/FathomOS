using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using FathomOS.Core.Models;
using FathomOS.Modules.SurveyListing.Views.Steps;
using FathomOS.Modules.SurveyListing.Services;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.ViewModels;

/// <summary>
/// Main ViewModel managing wizard navigation and project state
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private Project _project;
    private int _currentStepIndex;
    private UserControl? _currentStepView;
    private bool _isProcessing;

    // Cache for step views to avoid recreating on each navigation
    private readonly Dictionary<int, UserControl> _stepViewCache = new();

    // Step ViewModels
    private readonly Step1ViewModel _step1ViewModel;
    private readonly Step2ViewModel _step2ViewModel;
    private readonly Step3ViewModel _step3ViewModel;
    private readonly Step4ViewModel _step4ViewModel;
    private readonly Step5ViewModel _step5ViewModel;
    private readonly Step6ViewModel _step6ViewModel;
    private readonly Step7ViewModel _step7ViewModel;

    public MainViewModel(Project project)
    {
        _project = project;
        
        // Initialize step ViewModels
        _step1ViewModel = new Step1ViewModel(project);
        _step2ViewModel = new Step2ViewModel(project);
        _step3ViewModel = new Step3ViewModel(project);
        _step4ViewModel = new Step4ViewModel(project);
        _step5ViewModel = new Step5ViewModel(project);
        _step6ViewModel = new Step6ViewModel(project);
        _step7ViewModel = new Step7ViewModel(project);

        // Wire up inter-step dependencies
        _step4ViewModel.SetStep3Reference(_step3ViewModel);
        _step6ViewModel.SetStep2Reference(_step2ViewModel);
        _step6ViewModel.SetStep4Reference(_step4ViewModel);
        _step7ViewModel.SetStep6Reference(_step6ViewModel);

        // Sync route requirement based on KP/DCC mode from Step 1
        _step1ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Step1ViewModel.SelectedKpDccMode))
            {
                _step2ViewModel.IsRouteRequired = _step1ViewModel.SelectedKpDccMode != KpDccMode.None;
                // Also sync to Step6 for processing
                _step6ViewModel.KpDccMode = _step1ViewModel.SelectedKpDccMode;
            }
        };
        _step2ViewModel.IsRouteRequired = _step1ViewModel.SelectedKpDccMode != KpDccMode.None;

        // Initialize steps
        Steps = new ObservableCollection<StepInfo>
        {
            new(1, "Project", true),
            new(2, "Route", false),
            new(3, "Survey Data", false),
            new(4, "Review", false),
            new(5, "Tide", false),
            new(6, "Process", false),
            new(7, "Output", false)
        };

        _currentStepIndex = 0;
        UpdateStepIndicators();
        LoadCurrentStepView();
    }

    public ObservableCollection<StepInfo> Steps { get; }

    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        set
        {
            if (_currentStepIndex != value && value >= 0 && value < Steps.Count)
            {
                _currentStepIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(ProcessButtonVisibility));
                UpdateStepIndicators();
                LoadCurrentStepView();
            }
        }
    }

    public UserControl? CurrentStepView
    {
        get => _currentStepView;
        private set
        {
            _currentStepView = value;
            OnPropertyChanged();
        }
    }

    public bool CanGoBack => _currentStepIndex > 0 && !_isProcessing;
    public bool CanGoNext => _currentStepIndex < Steps.Count - 1 && !_isProcessing;
    public Visibility ProcessButtonVisibility => 
        _currentStepIndex == 5 ? Visibility.Visible : Visibility.Collapsed; // Step 6 (Process)

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public void LoadProject(Project project)
    {
        _project = project;

        // Clear the view cache when loading a new project
        ClearViewCache();

        // Update all step ViewModels
        _step1ViewModel.LoadProject(project);
        _step2ViewModel.LoadProject(project);
        _step3ViewModel.LoadProject(project);
        _step4ViewModel.LoadProject(project);
        _step5ViewModel.LoadProject(project);
        _step6ViewModel.LoadProject(project);
        _step7ViewModel.LoadProject(project);

        // Reset to first step
        CurrentStepIndex = 0;
    }

    public void UpdateProject(Project project)
    {
        // Collect data from all step ViewModels
        _step1ViewModel.SaveToProject(project);
        _step2ViewModel.SaveToProject(project);
        _step3ViewModel.SaveToProject(project);
        _step4ViewModel.SaveToProject(project);
        _step5ViewModel.SaveToProject(project);
        _step6ViewModel.SaveToProject(project);
        _step7ViewModel.SaveToProject(project);
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            CurrentStepIndex--;
        }
    }

    public void GoNext()
    {
        if (CanGoNext)
        {
            CurrentStepIndex++;
        }
    }
    
    /// <summary>
    /// Navigate directly to a specific step (0-indexed)
    /// </summary>
    /// <param name="stepIndex">The step index to navigate to (0-6)</param>
    public void GoToStep(int stepIndex)
    {
        // Allow navigating to any completed or current step
        if (stepIndex >= 0 && stepIndex < Steps.Count && stepIndex <= _currentStepIndex)
        {
            CurrentStepIndex = stepIndex;
        }
    }

    public bool ValidateCurrentStep()
    {
        return _currentStepIndex switch
        {
            0 => _step1ViewModel.Validate(),
            1 => _step2ViewModel.Validate(routeRequired: _step1ViewModel.SelectedKpDccMode != KpDccMode.None),
            2 => _step3ViewModel.Validate(),
            3 => _step4ViewModel.Validate(),
            4 => _step5ViewModel.Validate(),
            5 => _step6ViewModel.Validate(),
            6 => _step7ViewModel.Validate(),
            _ => true
        };
    }

    public bool ValidateAllSteps()
    {
        var issues = new List<string>();

        if (!_step1ViewModel.Validate()) issues.Add("Step 1: Project information incomplete");
        
        // Route is only required if KP/DCC calculation is enabled
        bool routeRequired = _step1ViewModel.SelectedKpDccMode != KpDccMode.None;
        if (routeRequired && !_step2ViewModel.Validate(routeRequired)) 
            issues.Add("Step 2: Route file not loaded (required for KP/DCC calculation)");
            
        if (!_step3ViewModel.Validate()) issues.Add("Step 3: Survey data not loaded");
        if (!_step4ViewModel.Validate()) issues.Add("Step 4: Data review not confirmed");
        // Step 5 (Tide) is optional
        // Step 6 (Process) validation happens during processing

        if (issues.Count > 0)
        {
            DialogService.Instance.ShowWarning(
                "Validation",
                "Please complete the following before processing:\n\n" + string.Join("\n", issues));
            return false;
        }

        return true;
    }

    public async void StartProcessing()
    {
        IsProcessing = true;
        try
        {
            await _step6ViewModel.ProcessAsync();
            
            // Move to output step
            CurrentStepIndex = 6;
        }
        catch (Exception ex)
        {
            await DialogService.Instance.ShowErrorAsync(
                "Error",
                $"Processing error:\n\n{ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void UpdateStepIndicators()
    {
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsCurrent = i == _currentStepIndex;
            Steps[i].IsCompleted = i < _currentStepIndex;
            Steps[i].IsEnabled = i <= _currentStepIndex;
        }
    }

    private void LoadCurrentStepView()
    {
        try
        {
            // Check if view exists in cache
            if (_stepViewCache.TryGetValue(_currentStepIndex, out var cachedView))
            {
                CurrentStepView = cachedView;
                return;
            }

            // Create new view and add to cache
            UserControl? newView = _currentStepIndex switch
            {
                0 => new Step1_ProjectSetup { DataContext = _step1ViewModel },
                1 => new Step2_RouteFile { DataContext = _step2ViewModel },
                2 => new Step3_SurveyData { DataContext = _step3ViewModel },
                3 => new Step4_DataReview { DataContext = _step4ViewModel },
                4 => new Step5_TideCorrections { DataContext = _step5ViewModel },
                5 => new Step6_Processing { DataContext = _step6ViewModel },
                6 => new Step7_OutputOptions { DataContext = _step7ViewModel },
                _ => null
            };

            if (newView != null)
            {
                _stepViewCache[_currentStepIndex] = newView;
            }

            CurrentStepView = newView;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading step view {_currentStepIndex}: {ex.Message}");
            DialogService.Instance.ShowError("Navigation Error",
                $"Error navigating to step {_currentStepIndex + 1}:\n\n{ex.Message}");
        }
    }

    /// <summary>
    /// Clears the view cache, disposing any disposable views.
    /// Call this when the wizard is reset or a new project is loaded.
    /// </summary>
    public void ClearViewCache()
    {
        foreach (var view in _stepViewCache.Values)
        {
            if (view is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _stepViewCache.Clear();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Step indicator information for the wizard header
/// </summary>
public class StepInfo : INotifyPropertyChanged
{
    private bool _isCurrent;
    private bool _isCompleted;
    private bool _isEnabled;

    public StepInfo(int number, string name, bool isEnabled = false)
    {
        StepNumber = number;
        StepName = name;
        _isEnabled = isEnabled;
    }

    public int StepNumber { get; }
    public string StepName { get; }

    public bool IsCurrent
    {
        get => _isCurrent;
        set { _isCurrent = value; OnPropertyChanged(); UpdateVisuals(); }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(); UpdateVisuals(); }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; OnPropertyChanged(); UpdateVisuals(); }
    }

    // Visual properties for binding
    public string StepBackground => IsCurrent ? "#0078D4" : IsCompleted ? "#107C10" : "#E0E0E0";
    public string StepBorder => IsCurrent ? "#0078D4" : IsCompleted ? "#107C10" : "#BDBDBD";
    public string StepForeground => IsCurrent || IsCompleted ? "White" : "#757575";
    public string LabelForeground => IsCurrent ? "#0078D4" : IsCompleted ? "#107C10" : "#757575";
    public string LabelWeight => IsCurrent ? "SemiBold" : "Normal";
    public Visibility ConnectorVisibility => StepNumber < 7 ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateVisuals()
    {
        OnPropertyChanged(nameof(StepBackground));
        OnPropertyChanged(nameof(StepBorder));
        OnPropertyChanged(nameof(StepForeground));
        OnPropertyChanged(nameof(LabelForeground));
        OnPropertyChanged(nameof(LabelWeight));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
