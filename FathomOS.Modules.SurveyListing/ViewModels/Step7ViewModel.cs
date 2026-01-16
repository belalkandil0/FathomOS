using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using FathomOS.Core.Models;
using FathomOS.Core.Export;
using FathomOS.Modules.SurveyListing.Services;
using FathomOS.Modules.SurveyListing.Views;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace FathomOS.Modules.SurveyListing.ViewModels;

/// <summary>
/// ViewModel for Step 7: Output Options
/// </summary>
public class Step7ViewModel : INotifyPropertyChanged
{
    private string _outputFolder = string.Empty;
    private bool _isExporting;
    private string _statusMessage = "Configure output options";

    // Text file options
    private bool _exportTextFile = true;
    private string _textFormat = "CSV";
    private bool _textIncludeHeader = true;

    // Excel options
    private bool _exportExcel = true;
    private bool _excelIncludeRawData = true;
    private bool _excelIncludeCalculations = true;
    private bool _excelApplyFormatting = true;

    // CAD options
    private bool _exportDxf = true;
    private bool _exportCadScript = true;
    private string _dwgTemplatePath = string.Empty;
    private double _kpLabelInterval = 1.0;
    
    // 3D Polyline options
    private bool _export3DPolyline = true;
    private double _depthExaggeration = 10.0;
    
    // Raw data export
    private bool _exportRawData = false;

    // PDF options
    private bool _exportPdfReport;
    
    // Certificate options
    private bool _generateCertificate = true;

    // Generated files
    private List<GeneratedFile> _generatedFiles = new();
    
    // References
    private Project? _project;
    private Step6ViewModel? _step6ViewModel;

    public Step7ViewModel(Project project)
    {
        _project = project;
        TextFormats = new[] { "CSV", "Tab-delimited", "Fixed-width" };
        GeneratedFiles = new System.Collections.ObjectModel.ObservableCollection<GeneratedFile>();
        LoadProject(project);
        UpdateStatusMessage();
    }

    /// <summary>
    /// Set reference to Step 6 ViewModel to access processed data
    /// </summary>
    public void SetStep6Reference(Step6ViewModel step6)
    {
        _step6ViewModel = step6;
    }

    /// <summary>
    /// Export using data from Step 6
    /// </summary>
    public async Task ExportAsync()
    {
        if (_step6ViewModel == null)
        {
            await DialogService.Instance.ShowErrorAsync("Error", "Processing step not configured.");
            return;
        }

        var processedData = _step6ViewModel.GetProcessedData();
        await ExportAsync(_project ?? new Project(), processedData);
    }

    public string[] TextFormats { get; }
    public System.Collections.ObjectModel.ObservableCollection<GeneratedFile> GeneratedFiles { get; }

    // Output folder
    public string OutputFolder
    {
        get => _outputFolder;
        set { _outputFolder = value; OnPropertyChanged(); UpdateStatusMessage(); }
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set { _isExporting = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool HasAnyExportSelected => ExportTextFile || ExportExcel || ExportDxf || ExportCadScript || ExportPdfReport || Export3DPolyline || ExportRawData || ExportSmoothedComparison || ExportIntervalPointsCsv;
    
    public bool CanExport => !IsExporting && !string.IsNullOrEmpty(OutputFolder) && HasAnyExportSelected;

    private void UpdateStatusMessage()
    {
        if (string.IsNullOrEmpty(OutputFolder))
        {
            StatusMessage = "⚠ Please select an output folder to enable export";
        }
        else if (!HasAnyExportSelected)
        {
            StatusMessage = "⚠ Please select at least one export format";
        }
        else
        {
            StatusMessage = "✓ Ready to export";
        }
    }

    // Text file options
    public bool ExportTextFile
    {
        get => _exportTextFile;
        set { _exportTextFile = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }

    public string SelectedTextFormat
    {
        get => _textFormat;
        set { _textFormat = value; OnPropertyChanged(); }
    }

    public bool TextIncludeHeader
    {
        get => _textIncludeHeader;
        set { _textIncludeHeader = value; OnPropertyChanged(); }
    }

    // Excel options
    public bool ExportExcel
    {
        get => _exportExcel;
        set { _exportExcel = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }

    public bool ExcelIncludeRawData
    {
        get => _excelIncludeRawData;
        set { _excelIncludeRawData = value; OnPropertyChanged(); }
    }

    public bool ExcelIncludeCalculations
    {
        get => _excelIncludeCalculations;
        set { _excelIncludeCalculations = value; OnPropertyChanged(); }
    }

    public bool ExcelApplyFormatting
    {
        get => _excelApplyFormatting;
        set { _excelApplyFormatting = value; OnPropertyChanged(); }
    }

    // CAD options
    public bool ExportDxf
    {
        get => _exportDxf;
        set { _exportDxf = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }

    public bool ExportCadScript
    {
        get => _exportCadScript;
        set { _exportCadScript = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }

    public string DwgTemplatePath
    {
        get => _dwgTemplatePath;
        set { _dwgTemplatePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(DwgTemplateFileName)); }
    }

    public string DwgTemplateFileName => string.IsNullOrEmpty(_dwgTemplatePath) 
        ? "None selected" 
        : Path.GetFileName(_dwgTemplatePath);

    public double KpLabelInterval
    {
        get => _kpLabelInterval;
        set { _kpLabelInterval = value; OnPropertyChanged(); }
    }

    // 3D Polyline options
    public bool Export3DPolyline
    {
        get => _export3DPolyline;
        set { _export3DPolyline = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }

    public double DepthExaggeration
    {
        get => _depthExaggeration;
        set { _depthExaggeration = value; OnPropertyChanged(); }
    }

    // Raw data export
    public bool ExportRawData
    {
        get => _exportRawData;
        set { _exportRawData = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }

    // PDF options
    public bool ExportPdfReport
    {
        get => _exportPdfReport;
        set { _exportPdfReport = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }
    
    // Export options added in later versions - use AppInfo.Version for current version
    private bool _exportSmoothedComparison = false;
    private bool _exportIntervalPointsCsv = false;
    private bool _pdfIncludeFullData = false;
    private bool _pdfIncludeDepthChart = true;
    
    /// <summary>
    /// Export smoothed vs original comparison CSV
    /// </summary>
    public bool ExportSmoothedComparison
    {
        get => _exportSmoothedComparison;
        set { _exportSmoothedComparison = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }
    
    /// <summary>
    /// Export interval points to separate CSV
    /// </summary>
    public bool ExportIntervalPointsCsv
    {
        get => _exportIntervalPointsCsv;
        set { _exportIntervalPointsCsv = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); UpdateStatusMessage(); }
    }
    
    /// <summary>
    /// Include full data table in PDF (can be many pages)
    /// </summary>
    public bool PdfIncludeFullData
    {
        get => _pdfIncludeFullData;
        set { _pdfIncludeFullData = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Include depth profile chart in PDF
    /// </summary>
    public bool PdfIncludeDepthChart
    {
        get => _pdfIncludeDepthChart;
        set { _pdfIncludeDepthChart = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Generate a processing certificate after export (requires supervisor approval)
    /// </summary>
    public bool GenerateCertificate
    {
        get => _generateCertificate;
        set { _generateCertificate = value; OnPropertyChanged(); }
    }

    public void BrowseOutputFolder()
    {
        // Use Windows Forms FolderBrowserDialog as a fallback
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Output Folder",
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputFolder = dialog.SelectedPath;
        }
    }

    public void BrowseDwgTemplate()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "DWG Files (*.dwg)|*.dwg|All Files (*.*)|*.*",
            Title = "Select DWG Template"
        };

        if (dialog.ShowDialog() == true)
        {
            DwgTemplatePath = dialog.FileName;
        }
    }

    public async Task ExportAsync(Project project, List<SurveyPoint>? processedData)
    {
        if (string.IsNullOrEmpty(OutputFolder))
        {
            await DialogService.Instance.ShowWarningAsync("Validation", "Please select an output folder.");
            return;
        }

        if (processedData == null || processedData.Count == 0)
        {
            await DialogService.Instance.ShowWarningAsync("Validation", 
                "No processed data available. Please run processing in Step 6 first.");
            return;
        }

        if (!Directory.Exists(OutputFolder))
        {
            try
            {
                Directory.CreateDirectory(OutputFolder);
            }
            catch (Exception ex)
            {
                await DialogService.Instance.ShowErrorAsync("Error", 
                    $"Cannot create output folder:\n\n{ex.Message}");
                return;
            }
        }
        
        // Get additional export data from Step 6 (spline and interval points)
        List<SurveyPoint>? splinePoints = null;
        List<(double X, double Y, double Z, double Distance)>? intervalPoints = null;
        RouteData? routeData = null;
        
        if (_step6ViewModel != null)
        {
            splinePoints = _step6ViewModel.GetSplineFittedPoints();
            intervalPoints = _step6ViewModel.GetIntervalPoints();
            routeData = _step6ViewModel.GetRouteData();
        }

        IsExporting = true;
        GeneratedFiles.Clear();

        try
        {
            string baseName = string.IsNullOrEmpty(project.ProjectName) 
                ? "survey_output" 
                : project.ProjectName.Replace(" ", "_");

            // Export Text File (Survey Listing format: KP, DCC, X, Y, Z)
            if (ExportTextFile)
            {
                StatusMessage = "Generating survey listing text file...";
                await Task.Delay(100);

                string extension = SelectedTextFormat == "CSV" ? ".csv" : ".txt";
                string textPath = Path.Combine(OutputFolder, baseName + "_SurveyListing" + extension);
                
                try
                {
                    var format = TextExporter.ParseFormat(SelectedTextFormat);
                    // Use SurveyListing mode for KP, DCC, X, Y, Z output
                    var textExporter = new TextExporter(format, TextIncludeHeader, 4, TextExporter.ExportMode.SurveyListing);
                    await Task.Run(() => textExporter.Export(textPath, processedData!, project));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(textPath),
                        FilePath = textPath,
                        FileType = "Survey Listing",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(textPath),
                        FilePath = textPath,
                        FileType = "Survey Listing",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Export Excel
            if (ExportExcel)
            {
                StatusMessage = "Generating Excel workbook...";
                await Task.Delay(100);

                string excelPath = Path.Combine(OutputFolder, baseName + ".xlsx");
                
                try
                {
                    var excelOptions = new ExcelExportOptions
                    {
                        IncludeRawData = ExcelIncludeRawData,
                        IncludeCalculations = ExcelIncludeCalculations,
                        ApplyFormatting = ExcelApplyFormatting,
                        IncludeSmoothedData = true,
                        IncludeSplineData = splinePoints?.Count > 0,
                        IncludeIntervalPoints = intervalPoints?.Count > 0
                    };
                    var excelExporter = new ExcelExporter(excelOptions);
                    await Task.Run(() => excelExporter.Export(excelPath, processedData!, project, splinePoints, intervalPoints));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(excelPath),
                        FilePath = excelPath,
                        FileType = "Excel",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(excelPath),
                        FilePath = excelPath,
                        FileType = "Excel",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Export DXF
            if (ExportDxf)
            {
                StatusMessage = "Generating DXF file...";
                await Task.Delay(100);

                string dxfPath = Path.Combine(OutputFolder, baseName + ".dxf");
                
                try
                {
                    var dxfOptions = new DxfExportOptions
                    {
                        IncludePoints = true,
                        IncludeKpLabels = true,
                        KpLabelInterval = KpLabelInterval
                    };
                    var dxfExporter = new DxfExporter(dxfOptions);
                    await Task.Run(() => dxfExporter.Export(dxfPath, processedData!, routeData, project, 
                        splinePoints?.Count > 0 ? splinePoints : null, 
                        intervalPoints?.Count > 0 ? intervalPoints : null));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(dxfPath),
                        FilePath = dxfPath,
                        FileType = "DXF",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(dxfPath),
                        FilePath = dxfPath,
                        FileType = "DXF",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Export CAD Script
            if (ExportCadScript)
            {
                StatusMessage = "Generating CAD script...";
                await Task.Delay(100);

                string scrPath = Path.Combine(OutputFolder, baseName + ".scr");
                
                try
                {
                    var cadOptions = new CadScriptOptions
                    {
                        IncludeKpLabels = true,
                        KpLabelInterval = KpLabelInterval,
                        TemplatePath = string.IsNullOrEmpty(DwgTemplatePath) ? null : DwgTemplatePath
                    };
                    var cadExporter = new CadScriptExporter(cadOptions);
                    await Task.Run(() => cadExporter.Export(scrPath, processedData!, routeData, project,
                        splinePoints?.Count > 0 ? splinePoints : null,
                        intervalPoints?.Count > 0 ? intervalPoints : null));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(scrPath),
                        FilePath = scrPath,
                        FileType = "CAD Script",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(scrPath),
                        FilePath = scrPath,
                        FileType = "CAD Script",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Export 3D Polyline DXF
            if (Export3DPolyline)
            {
                StatusMessage = "Generating 3D polyline DXF...";
                await Task.Delay(100);

                string polyPath = Path.Combine(OutputFolder, baseName + "_3D_Polyline.dxf");
                
                try
                {
                    var dxfExporter = new DxfExporter();
                    await Task.Run(() => dxfExporter.Export3DPolyline(polyPath, processedData!, project, DepthExaggeration));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(polyPath),
                        FilePath = polyPath,
                        FileType = "3D Polyline DXF",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(polyPath),
                        FilePath = polyPath,
                        FileType = "3D Polyline DXF",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Export Raw Data CSV
            if (ExportRawData)
            {
                StatusMessage = "Generating raw data CSV...";
                await Task.Delay(100);

                string rawPath = Path.Combine(OutputFolder, baseName + "_RawData.csv");
                
                try
                {
                    var rawExporter = new TextExporter(TextExporter.TextFormat.Csv, true, 4, TextExporter.ExportMode.RawData);
                    await Task.Run(() => rawExporter.Export(rawPath, processedData!, project));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(rawPath),
                        FilePath = rawPath,
                        FileType = "Raw Data CSV",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(rawPath),
                        FilePath = rawPath,
                        FileType = "Raw Data CSV",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Export PDF Report
            if (ExportPdfReport)
            {
                StatusMessage = "Generating PDF report...";
                await Task.Delay(100);

                string pdfPath = Path.Combine(OutputFolder, baseName + "_Report.pdf");
                
                try
                {
                    // Configure PDF options
                    var pdfOptions = new PdfReportOptions
                    {
                        IncludeFullDataTable = PdfIncludeFullData,
                        IncludeDepthChart = PdfIncludeDepthChart,
                        IncludeCribSheet = true,
                        IncludeDataSummary = true,
                        IncludeSampleData = !PdfIncludeFullData // Only include sample if not full data
                    };
                    var pdfGenerator = new PdfReportGenerator(pdfOptions);
                    
                    // Load template if available
                    var templatePath = GetTemplatePath();
                    if (templatePath != null)
                    {
                        try
                        {
                            var template = ReportTemplate.Load(templatePath);
                            var logoPath = GetLogoPath();
                            pdfGenerator.SetTemplate(template, logoPath);
                        }
                        catch
                        {
                            // Template loading failed - continue without template
                        }
                    }
                    
                    // Get ProcessingTracker data for Crib Sheet
                    var tracker = ProcessingTracker.Instance;
                    var trackerData = new ProcessingTrackerData
                    {
                        ProductLayRouteLoaded = tracker.ProductLayRouteLoaded,
                        TidalDataLoaded = tracker.TidalDataLoaded,
                        RawDataFilesLoaded = tracker.RawDataFilesLoaded,
                        CadBackgroundLoaded = tracker.CadBackgroundLoaded,
                        ProductLayRouteFile = tracker.ProductLayRouteFile,
                        TidalDataFile = tracker.TidalDataFile,
                        RawDataStartFile = tracker.RawDataStartFile,
                        RawDataEndFile = tracker.RawDataEndFile,
                        CadBackgroundFile = tracker.CadBackgroundFile,
                        RawDataAdded = tracker.RawDataAdded,
                        TideDataAdded = tracker.TideDataAdded,
                        CalculationsUpdated = tracker.CalculationsUpdated,
                        ParametersUpdated = tracker.ParametersUpdated,
                        WorkingTrackCreated = tracker.WorkingTrackCreated,
                        FixesAddedToCad = tracker.FixesAddedToCad,
                        XyzScriptCreated = tracker.XyzScriptCreated,
                        TrackAddedToBricsCAD = tracker.TrackAddedToBricsCAD,
                        DataCoverageReviewed = tracker.DataCoverageReviewed,
                        SplineFitCreated = tracker.SplineFitCreated,
                        ProductPositionReviewed = tracker.ProductPositionReviewed,
                        PointsAt1mCreated = tracker.PointsAt1mCreated,
                        PointsXyzExtracted = tracker.PointsXyzExtracted,
                        SeabedDepthKpDccCalculated = tracker.SeabedDepthKpDccCalculated,
                        RovDepthKpDccCalculated = tracker.RovDepthKpDccCalculated,
                        FinalListingProduced = true, // We're producing it now
                        ProcessingStartTime = tracker.ProcessingStartTime,
                        ProcessingEndTime = DateTime.Now
                    };
                    
                    // Pass routeData to PDF generator for route statistics
                    await Task.Run(() => pdfGenerator.Generate(pdfPath, processedData!, project, routeData, trackerData));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(pdfPath),
                        FilePath = pdfPath,
                        FileType = "PDF Report",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    // Get detailed error message
                    string errorDetail = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorDetail += $" ({ex.InnerException.Message})";
                    }
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(pdfPath),
                        FilePath = pdfPath,
                        FileType = "PDF Report",
                        IsSuccess = false,
                        ErrorMessage = errorDetail
                    });
                }
            }
            
            // Export Smoothed Comparison CSV
            if (ExportSmoothedComparison)
            {
                StatusMessage = "Generating smoothed comparison CSV...";
                await Task.Delay(100);

                string smoothedPath = Path.Combine(OutputFolder, baseName + "_SmoothedComparison.csv");
                
                try
                {
                    // Only export points that have smoothed data
                    var smoothedPoints = processedData!
                        .Where(p => p.SmoothedEasting.HasValue || p.SmoothedNorthing.HasValue)
                        .ToList();
                    
                    if (smoothedPoints.Count > 0)
                    {
                        var smoothedExporter = new TextExporter(TextExporter.TextFormat.Csv, true, 4, TextExporter.ExportMode.SmoothedComparison);
                        await Task.Run(() => smoothedExporter.Export(smoothedPath, smoothedPoints, project));
                        
                        GeneratedFiles.Add(new GeneratedFile
                        {
                            FileName = Path.GetFileName(smoothedPath),
                            FilePath = smoothedPath,
                            FileType = "Smoothed Comparison CSV",
                            IsSuccess = true
                        });
                    }
                    else
                    {
                        GeneratedFiles.Add(new GeneratedFile
                        {
                            FileName = Path.GetFileName(smoothedPath),
                            FilePath = smoothedPath,
                            FileType = "Smoothed Comparison CSV",
                            IsSuccess = false,
                            ErrorMessage = "No smoothed data available"
                        });
                    }
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(smoothedPath),
                        FilePath = smoothedPath,
                        FileType = "Smoothed Comparison CSV",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
            
            // Export Interval Points CSV
            if (ExportIntervalPointsCsv && intervalPoints != null && intervalPoints.Count > 0)
            {
                StatusMessage = "Generating interval points CSV...";
                await Task.Delay(100);

                string intervalPath = Path.Combine(OutputFolder, baseName + "_IntervalPoints.csv");
                
                try
                {
                    var intervalExporter = new TextExporter(TextExporter.TextFormat.Csv, true, 4, TextExporter.ExportMode.IntervalPoints);
                    await Task.Run(() => intervalExporter.ExportIntervalPoints(intervalPath, intervalPoints, project));
                    
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(intervalPath),
                        FilePath = intervalPath,
                        FileType = "Interval Points CSV",
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    GeneratedFiles.Add(new GeneratedFile
                    {
                        FileName = Path.GetFileName(intervalPath),
                        FilePath = intervalPath,
                        FileType = "Interval Points CSV",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            StatusMessage = $"Export complete! {GeneratedFiles.Count(f => f.IsSuccess)} file(s) generated.";

            // Generate Processing Certificate if requested
            if (GenerateCertificate && GeneratedFiles.Any(f => f.IsSuccess))
            {
                StatusMessage = "Requesting supervisor approval for certificate...";
                await Task.Delay(100);
                
                try
                {
                    var outputFilesList = GeneratedFiles
                        .Where(f => f.IsSuccess)
                        .Select(f => f.FilePath)
                        .ToList();
                    
                    var certPath = await ProcessingCertificateService.Instance
                        .RequestApprovalAndGenerateCertificateAsync(
                            project,
                            processedData!,
                            outputFilesList,
                            OutputFolder,
                            System.Windows.Application.Current.MainWindow);
                    
                    if (!string.IsNullOrEmpty(certPath))
                    {
                        GeneratedFiles.Add(new GeneratedFile
                        {
                            FileName = Path.GetFileName(certPath),
                            FilePath = certPath,
                            FileType = "Processing Certificate (JSON)",
                            IsSuccess = true
                        });
                        
                        // Also add the text version
                        var textCertPath = Path.ChangeExtension(certPath, ".txt");
                        if (File.Exists(textCertPath))
                        {
                            GeneratedFiles.Add(new GeneratedFile
                            {
                                FileName = Path.GetFileName(textCertPath),
                                FilePath = textCertPath,
                                FileType = "Processing Certificate (Readable)",
                                IsSuccess = true
                            });
                        }
                        
                        StatusMessage = $"Export complete with certificate! {GeneratedFiles.Count(f => f.IsSuccess)} file(s) generated.";
                    }
                    else
                    {
                        StatusMessage = $"Export complete (certificate skipped). {GeneratedFiles.Count(f => f.IsSuccess)} file(s) generated.";
                    }
                }
                catch (Exception certEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Certificate generation error: {certEx.Message}");
                    // Don't fail the export if certificate generation fails
                    StatusMessage = $"Export complete (certificate failed). {GeneratedFiles.Count(f => f.IsSuccess)} file(s) generated.";
                }
            }

            var successCount = GeneratedFiles.Count(f => f.IsSuccess);
            var failCount = GeneratedFiles.Count(f => !f.IsSuccess);
            
            string message = $"Export complete!\n\n{successCount} file(s) generated successfully.";
            if (failCount > 0)
                message += $"\n{failCount} file(s) failed.";
            message += $"\n\nOutput folder:\n{OutputFolder}";
            
            if (failCount > 0)
                await DialogService.Instance.ShowWarningAsync("Export Complete", message);
            else
                await DialogService.Instance.ShowInfoAsync("Export Complete", message);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
            await DialogService.Instance.ShowErrorAsync("Error", $"Export error:\n\n{ex.Message}");
        }
        finally
        {
            IsExporting = false;
        }
    }
    
    /// <summary>
    /// Get path to report template file
    /// </summary>
    private string? GetTemplatePath()
    {
        // Look for template in Assets folder
        var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        
        if (assemblyDir != null)
        {
            var templatePath = Path.Combine(assemblyDir, "Assets", "report_template.json");
            if (File.Exists(templatePath))
                return templatePath;
        }
        
        // Try relative path
        if (File.Exists("Assets/report_template.json"))
            return "Assets/report_template.json";
            
        return null;
    }
    
    /// <summary>
    /// Get path to company logo file
    /// </summary>
    private string? GetLogoPath()
    {
        // Look for logo in Assets folder
        var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        
        if (assemblyDir != null)
        {
            var logoPath = Path.Combine(assemblyDir, "Assets", "company_logo.png");
            if (File.Exists(logoPath))
                return logoPath;
        }
        
        // Try relative path
        if (File.Exists("Assets/company_logo.png"))
            return "Assets/company_logo.png";
            
        return null;
    }

    public void OpenOutputFolder()
    {
        if (!string.IsNullOrEmpty(OutputFolder) && Directory.Exists(OutputFolder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = OutputFolder,
                UseShellExecute = true
            });
        }
    }

    public void OpenFile(GeneratedFile file)
    {
        if (File.Exists(file.FilePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = file.FilePath,
                UseShellExecute = true
            });
        }
    }

    public void LoadProject(Project project)
    {
        OutputFolder = project.OutputOptions.OutputFolder;

        ExportTextFile = project.OutputOptions.ExportTextFile;
        SelectedTextFormat = project.OutputOptions.TextFormat;
        TextIncludeHeader = project.OutputOptions.TextIncludeHeader;

        ExportExcel = project.OutputOptions.ExportExcel;
        ExcelIncludeRawData = project.OutputOptions.ExcelIncludeRawData;
        ExcelIncludeCalculations = project.OutputOptions.ExcelIncludeCalculations;
        ExcelApplyFormatting = project.OutputOptions.ExcelApplyFormatting;

        ExportDxf = project.OutputOptions.ExportDxf;
        ExportCadScript = project.OutputOptions.ExportCadScript;
        DwgTemplatePath = project.OutputOptions.DwgTemplatePath;
        KpLabelInterval = project.OutputOptions.KpLabelInterval;

        ExportPdfReport = project.OutputOptions.ExportPdfReport;
    }

    public void SaveToProject(Project project)
    {
        project.OutputOptions.OutputFolder = OutputFolder;

        project.OutputOptions.ExportTextFile = ExportTextFile;
        project.OutputOptions.TextFormat = SelectedTextFormat;
        project.OutputOptions.TextIncludeHeader = TextIncludeHeader;

        project.OutputOptions.ExportExcel = ExportExcel;
        project.OutputOptions.ExcelIncludeRawData = ExcelIncludeRawData;
        project.OutputOptions.ExcelIncludeCalculations = ExcelIncludeCalculations;
        project.OutputOptions.ExcelApplyFormatting = ExcelApplyFormatting;

        project.OutputOptions.ExportDxf = ExportDxf;
        project.OutputOptions.ExportCadScript = ExportCadScript;
        project.OutputOptions.DwgTemplatePath = DwgTemplatePath;
        project.OutputOptions.KpLabelInterval = KpLabelInterval;

        project.OutputOptions.ExportPdfReport = ExportPdfReport;
    }

    public bool Validate()
    {
        if (string.IsNullOrEmpty(OutputFolder))
        {
            DialogService.Instance.ShowWarning("Validation", "Please select an output folder.");
            return false;
        }

        if (!ExportTextFile && !ExportExcel && !ExportDxf && !ExportCadScript && !ExportPdfReport)
        {
            DialogService.Instance.ShowWarning("Validation", "Please select at least one output format.");
            return false;
        }

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Information about a generated output file
/// </summary>
public class GeneratedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
