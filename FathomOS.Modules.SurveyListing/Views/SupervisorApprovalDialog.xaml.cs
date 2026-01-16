using System.ComponentModel;
using System.Windows;
using FathomOS.Core.Models;
using FathomOS.Modules.SurveyListing.Services;

namespace FathomOS.Modules.SurveyListing.Views;

/// <summary>
/// Dialog for supervisor to review and approve processing before certificate generation.
/// Requires supervisor name and explicit confirmation to proceed.
/// </summary>
public partial class SupervisorApprovalDialog : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Result data after approval
    /// </summary>
    public SupervisorApprovalResult? ApprovalResult { get; private set; }
    
    /// <summary>
    /// Whether approval was granted
    /// </summary>
    public bool IsApproved { get; private set; }
    
    // Processing data
    private readonly Project _project;
    private readonly List<SurveyPoint> _processedData;
    private readonly ProcessingTracker _tracker;
    private readonly List<string> _outputFiles;
    
    /// <summary>
    /// Creates a new supervisor approval dialog
    /// </summary>
    /// <param name="project">The project being processed</param>
    /// <param name="processedData">The processed survey data</param>
    /// <param name="outputFiles">List of output files generated</param>
    public SupervisorApprovalDialog(Project project, List<SurveyPoint> processedData, List<string> outputFiles)
    {
        InitializeComponent();
        
        _project = project;
        _processedData = processedData;
        _outputFiles = outputFiles;
        _tracker = ProcessingTracker.Instance;
        
        LoadProcessingSummary();
        LoadDataQualityChecks();
        LoadFileInfo();
        LoadLicenseInfo();
        
        // Wire up confirmation checkbox
        ChkApprovalConfirmation.Checked += (s, e) => UpdateApproveButtonState();
        ChkApprovalConfirmation.Unchecked += (s, e) => UpdateApproveButtonState();
        TxtSupervisorName.TextChanged += (s, e) => UpdateApproveButtonState();
    }
    
    private void LoadProcessingSummary()
    {
        TxtProjectName.Text = _project.ProjectName ?? "Unnamed Project";
        TxtTotalPoints.Text = $"{_processedData.Count:N0} points";
        
        // Calculate KP range
        var validKps = _processedData.Where(p => p.Kp.HasValue).Select(p => p.Kp!.Value).ToList();
        if (validKps.Count > 0)
        {
            var minKp = validKps.Min();
            var maxKp = validKps.Max();
            TxtKpRange.Text = $"{minKp:F3} - {maxKp:F3} km ({maxKp - minKp:F3} km total)";
        }
        else
        {
            TxtKpRange.Text = "N/A";
        }
        
        // Calculate depth range
        var validDepths = _processedData.Where(p => p.Depth.HasValue).Select(p => p.Depth!.Value).ToList();
        if (validDepths.Count > 0)
        {
            var minDepth = validDepths.Min();
            var maxDepth = validDepths.Max();
            TxtDepthRange.Text = $"{minDepth:F2} - {maxDepth:F2} m";
        }
        else
        {
            TxtDepthRange.Text = "N/A";
        }
        
        // Processing method
        var methods = new List<string>();
        if (_tracker.SplineFitCreated) methods.Add("Spline Fitting");
        if (_tracker.TideDataAdded) methods.Add("Tide Correction");
        if (_tracker.CalculationsUpdated) methods.Add("KP/Depth Calculation");
        TxtProcessingMethod.Text = methods.Count > 0 ? string.Join(", ", methods) : "Standard Processing";
        
        // Processing time
        TxtProcessingTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    private void LoadDataQualityChecks()
    {
        ChkRouteLoaded.IsChecked = _tracker.ProductLayRouteLoaded;
        ChkSurveyDataLoaded.IsChecked = _tracker.RawDataAdded;
        ChkTideApplied.IsChecked = _tracker.TideDataAdded || !string.IsNullOrEmpty(_tracker.TidalDataFile);
        ChkCalculationsComplete.IsChecked = _tracker.CalculationsUpdated;
        ChkDataReviewed.IsChecked = _tracker.DataCoverageReviewed;
        ChkSmoothingApplied.IsChecked = _tracker.SplineFitCreated;
    }
    
    private void LoadFileInfo()
    {
        TxtRouteFile.Text = string.IsNullOrEmpty(_tracker.ProductLayRouteFile) 
            ? "N/A" 
            : System.IO.Path.GetFileName(_tracker.ProductLayRouteFile);
        
        // Survey files
        var surveyFiles = new List<string>();
        if (!string.IsNullOrEmpty(_tracker.RawDataStartFile))
            surveyFiles.Add(System.IO.Path.GetFileName(_tracker.RawDataStartFile));
        if (!string.IsNullOrEmpty(_tracker.RawDataEndFile) && 
            _tracker.RawDataEndFile != _tracker.RawDataStartFile)
            surveyFiles.Add(System.IO.Path.GetFileName(_tracker.RawDataEndFile));
        
        TxtSurveyFiles.Text = surveyFiles.Count > 0 
            ? string.Join(", ", surveyFiles) 
            : $"{_tracker.RawDataAdded} file(s) loaded";
        
        // Output files
        TxtOutputFiles.Text = _outputFiles.Count > 0 
            ? string.Join(", ", _outputFiles.Select(f => System.IO.Path.GetFileName(f)))
            : "None generated yet";
    }
    
    private void LoadLicenseInfo()
    {
        try
        {
            // Try to get license info from the shell app
            var licenseInfo = GetLicenseDisplayInfo();
            if (licenseInfo != null)
            {
                TxtLicenseInfo.Text = $"Licensed to: {licenseInfo.CustomerName ?? "Unknown"}";
                TxtCompanyName.Text = licenseInfo.Brand ?? licenseInfo.CustomerName ?? "";
            }
            else
            {
                TxtLicenseInfo.Text = "License information unavailable";
            }
        }
        catch
        {
            TxtLicenseInfo.Text = "License information unavailable";
        }
    }
    
    private dynamic? GetLicenseDisplayInfo()
    {
        try
        {
            // Access the shell app's license info via reflection
            var appType = Type.GetType("FathomOS.Shell.App, FathomOS.Shell");
            if (appType != null)
            {
                var licensingProperty = appType.GetProperty("Licensing", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (licensingProperty != null)
                {
                    var licensing = licensingProperty.GetValue(null);
                    if (licensing != null)
                    {
                        var getDisplayMethod = licensing.GetType().GetMethod("GetLicenseDisplayInfo");
                        if (getDisplayMethod != null)
                        {
                            return getDisplayMethod.Invoke(licensing, null);
                        }
                    }
                }
            }
        }
        catch { }
        return null;
    }
    
    private void UpdateApproveButtonState()
    {
        BtnApprove.IsEnabled = ChkApprovalConfirmation.IsChecked == true 
                              && !string.IsNullOrWhiteSpace(TxtSupervisorName.Text);
    }
    
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        IsApproved = false;
        DialogResult = false;
        Close();
    }
    
    private void BtnApprove_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSupervisorName.Text))
        {
            System.Windows.MessageBox.Show("Please enter the supervisor's name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSupervisorName.Focus();
            return;
        }
        
        if (ChkApprovalConfirmation.IsChecked != true)
        {
            System.Windows.MessageBox.Show("Please confirm that you have reviewed and approve the processing results.", 
                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Build approval result
        ApprovalResult = new SupervisorApprovalResult
        {
            SupervisorName = TxtSupervisorName.Text.Trim(),
            SupervisorTitle = TxtSupervisorTitle.Text.Trim(),
            CompanyName = TxtCompanyName.Text.Trim(),
            ApprovalTime = DateTime.UtcNow,
            ProjectName = _project.ProjectName ?? "Unnamed Project",
            TotalPointsProcessed = _processedData.Count,
            KpRange = TxtKpRange.Text,
            DepthRange = TxtDepthRange.Text,
            ProcessingMethod = TxtProcessingMethod.Text,
            RouteFileLoaded = ChkRouteLoaded.IsChecked == true,
            SurveyDataLoaded = ChkSurveyDataLoaded.IsChecked == true,
            TideCorrectionApplied = ChkTideApplied.IsChecked == true,
            CalculationsComplete = ChkCalculationsComplete.IsChecked == true,
            DataReviewed = ChkDataReviewed.IsChecked == true,
            SmoothingApplied = ChkSmoothingApplied.IsChecked == true,
            InputFiles = new List<string>
            {
                _tracker.ProductLayRouteFile ?? "",
                _tracker.RawDataStartFile ?? "",
                _tracker.TidalDataFile ?? ""
            }.Where(f => !string.IsNullOrEmpty(f)).ToList(),
            OutputFiles = _outputFiles
        };
        
        IsApproved = true;
        DialogResult = true;
        Close();
    }
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Result data from supervisor approval
/// </summary>
public class SupervisorApprovalResult
{
    public string SupervisorName { get; set; } = string.Empty;
    public string SupervisorTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime ApprovalTime { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TotalPointsProcessed { get; set; }
    public string KpRange { get; set; } = string.Empty;
    public string DepthRange { get; set; } = string.Empty;
    public string ProcessingMethod { get; set; } = string.Empty;
    public bool RouteFileLoaded { get; set; }
    public bool SurveyDataLoaded { get; set; }
    public bool TideCorrectionApplied { get; set; }
    public bool CalculationsComplete { get; set; }
    public bool DataReviewed { get; set; }
    public bool SmoothingApplied { get; set; }
    public List<string> InputFiles { get; set; } = new();
    public List<string> OutputFiles { get; set; } = new();
}
