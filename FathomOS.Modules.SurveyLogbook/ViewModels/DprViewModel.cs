// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: ViewModels/DprViewModel.cs
// Purpose: ViewModel for the DPR/Shift Handover tab (Tab 2)
// ============================================================================

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Services;

// Alias for QuestPDF types to avoid conflicts with System.Windows.Media
using QuestColors = QuestPDF.Helpers.Colors;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// ViewModel for the DPR (Daily Progress Report) and Shift Handover tab.
/// Manages DPR creation, editing, and export.
/// </summary>
public class DprViewModel : ViewModelBase
{
    private readonly LogEntryService _logService;
    private readonly ConnectionSettings _settings;
    
    private DprReport? _currentReport;
    private DprReport? _selectedReport;
    private CrewMember? _selectedCrewMember;
    private TransponderInfo? _selectedTransponder;
    private bool _isEditing;
    private DateTime _selectedDate = DateTime.Today;
    
    public DprViewModel(LogEntryService logService, ConnectionSettings settings)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        Reports = new ObservableCollection<DprReport>();
        
        // Initialize commands
        NewReportCommand = new RelayCommand(_ => CreateNewReport());
        SaveReportCommand = new RelayCommand(_ => SaveReport(), _ => CurrentReport != null);
        DeleteReportCommand = new RelayCommand(_ => DeleteReport(), _ => SelectedReport != null);
        ExportWordCommand = new AsyncRelayCommand(ExportWordAsync, _ => CurrentReport != null);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, _ => CurrentReport != null);
        
        AddCrewMemberCommand = new RelayCommand(_ => AddCrewMember(), _ => CurrentReport != null);
        RemoveCrewMemberCommand = new RelayCommand(p => RemoveCrewMember(p as CrewMember), p => CurrentReport != null && p is CrewMember);
        AddTransponderCommand = new RelayCommand(_ => AddTransponder(), _ => CurrentReport != null);
        RemoveTransponderCommand = new RelayCommand(p => RemoveTransponder(p as TransponderInfo), p => CurrentReport != null && p is TransponderInfo);
        AddDailyLogEntryCommand = new RelayCommand(_ => AddDailyLogEntry(), _ => CurrentReport != null);
        
        PopulateFromProjectCommand = new RelayCommand(_ => PopulateFromProject(), _ => CurrentReport != null);
        LoadFromDateCommand = new RelayCommand(_ => LoadFromDate());
        
        // Load existing reports
        RefreshReports();
    }
    
    #region Properties
    
    public ObservableCollection<DprReport> Reports { get; }
    
    public DprReport? CurrentReport
    {
        get => _currentReport;
        set
        {
            if (SetProperty(ref _currentReport, value))
            {
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(HasCurrentReport));
                
                // Notify all dependent properties when report changes
                OnPropertyChanged(nameof(Client));
                OnPropertyChanged(nameof(Vessel));
                OnPropertyChanged(nameof(ProjectNumber));
                OnPropertyChanged(nameof(LocationDepth));
                OnPropertyChanged(nameof(OffshoreManager));
                OnPropertyChanged(nameof(ProjectSurveyor));
                OnPropertyChanged(nameof(PartyChief));
                OnPropertyChanged(nameof(Last24HrsHighlights));
                OnPropertyChanged(nameof(KnownIssues));
                OnPropertyChanged(nameof(GeneralSurveyComments));
            }
        }
    }
    
    public DprReport? SelectedReport
    {
        get => _selectedReport;
        set
        {
            if (SetProperty(ref _selectedReport, value))
            {
                if (value != null)
                    CurrentReport = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    
    public CrewMember? SelectedCrewMember
    {
        get => _selectedCrewMember;
        set
        {
            if (SetProperty(ref _selectedCrewMember, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }
    
    public TransponderInfo? SelectedTransponder
    {
        get => _selectedTransponder;
        set
        {
            if (SetProperty(ref _selectedTransponder, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }
    
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }
    
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
    }
    
    public bool HasCurrentReport => CurrentReport != null;
    
    // Expose current report sections for binding
    public string Client
    {
        get => CurrentReport?.Client ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.Client = value; OnPropertyChanged(); } }
    }
    
    public string Vessel
    {
        get => CurrentReport?.Vessel ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.Vessel = value; OnPropertyChanged(); } }
    }
    
    public string ProjectNumber
    {
        get => CurrentReport?.ProjectNumber ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.ProjectNumber = value; OnPropertyChanged(); } }
    }
    
    public string LocationDepth
    {
        get => CurrentReport?.LocationDepth ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.LocationDepth = value; OnPropertyChanged(); } }
    }
    
    public string OffshoreManager
    {
        get => CurrentReport?.OffshoreManager ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.OffshoreManager = value; OnPropertyChanged(); } }
    }
    
    public string ProjectSurveyor
    {
        get => CurrentReport?.ProjectSurveyor ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.ProjectSurveyor = value; OnPropertyChanged(); } }
    }
    
    public string PartyChief
    {
        get => CurrentReport?.PartyChief ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.PartyChief = value; OnPropertyChanged(); } }
    }
    
    public string Last24HrsHighlights
    {
        get => CurrentReport?.Last24HrsHighlights ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.Last24HrsHighlights = value; OnPropertyChanged(); } }
    }
    
    public string KnownIssues
    {
        get => CurrentReport?.KnownIssues ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.KnownIssues = value; OnPropertyChanged(); } }
    }
    
    public string GeneralSurveyComments
    {
        get => CurrentReport?.GeneralSurveyComments ?? string.Empty;
        set { if (CurrentReport != null) { CurrentReport.GeneralSurveyComments = value; OnPropertyChanged(); } }
    }
    
    #endregion
    
    #region Commands
    
    public ICommand NewReportCommand { get; }
    public ICommand SaveReportCommand { get; }
    public ICommand DeleteReportCommand { get; }
    public ICommand ExportWordCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand AddCrewMemberCommand { get; }
    public ICommand RemoveCrewMemberCommand { get; }
    public ICommand AddTransponderCommand { get; }
    public ICommand RemoveTransponderCommand { get; }
    public ICommand AddDailyLogEntryCommand { get; }
    public ICommand PopulateFromProjectCommand { get; }
    public ICommand LoadFromDateCommand { get; }
    
    #endregion
    
    #region Command Implementations
    
    private void CreateNewReport()
    {
        var report = new DprReport
        {
            ReportDate = SelectedDate
        };
        
        report.InitializeDefaultEntries();
        
        // Auto-populate from settings
        if (_settings.ProjectInfo != null)
        {
            report.PopulateFromProject(_settings.ProjectInfo);
        }
        
        CurrentReport = report;
        IsEditing = true;
        
        NotifyReportPropertiesChanged();
    }
    
    private void SaveReport()
    {
        if (CurrentReport == null) return;
        
        // Check if already in list
        var existing = Reports.FirstOrDefault(r => r.ReportDate == CurrentReport.ReportDate);
        if (existing != null)
        {
            var index = Reports.IndexOf(existing);
            Reports[index] = CurrentReport;
        }
        else
        {
            Reports.Add(CurrentReport);
        }
        
        // Also add to service
        _logService.AddDprReport(CurrentReport);
        
        IsEditing = false;
    }
    
    private void DeleteReport()
    {
        if (SelectedReport == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Delete DPR for {SelectedReport.ReportDate:yyyy-MM-dd}?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            Reports.Remove(SelectedReport);
            if (CurrentReport == SelectedReport)
                CurrentReport = null;
        }
    }
    
    private async Task ExportWordAsync(object? _)
    {
        if (CurrentReport == null) return;
        
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export DPR to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = $"DPR_{CurrentReport.ReportDate:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            var report = CurrentReport;
            var filePath = dialog.FileName;
            
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("DPR Report");
                
                // Title
                ws.Cell(1, 1).Value = "Daily Progress Report";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;
                ws.Range(1, 1, 1, 4).Merge();
                
                // Report Info
                ws.Cell(3, 1).Value = "Report Date:";
                ws.Cell(3, 2).Value = report.ReportDate.ToString("yyyy-MM-dd");
                ws.Cell(4, 1).Value = "Project:";
                ws.Cell(4, 2).Value = report.ProjectNumber;
                ws.Cell(5, 1).Value = "Vessel:";
                ws.Cell(5, 2).Value = report.Vessel;
                ws.Cell(6, 1).Value = "Client:";
                ws.Cell(6, 2).Value = report.Client;
                ws.Cell(7, 1).Value = "Shift:";
                ws.Cell(7, 2).Value = report.Shift;
                
                // Personnel
                ws.Cell(9, 1).Value = "Party Chief:";
                ws.Cell(9, 2).Value = report.PartyChief;
                ws.Cell(10, 1).Value = "Project Surveyor:";
                ws.Cell(10, 2).Value = report.ProjectSurveyor;
                ws.Cell(11, 1).Value = "Offshore Manager:";
                ws.Cell(11, 2).Value = report.OffshoreManager;
                
                // Survey Comments
                ws.Cell(13, 1).Value = "General Survey Comments:";
                ws.Cell(13, 1).Style.Font.Bold = true;
                ws.Cell(14, 1).Value = report.GeneralSurveyComments;
                ws.Range(14, 1, 14, 4).Merge();
                
                // Known Issues
                ws.Cell(16, 1).Value = "Known Issues:";
                ws.Cell(16, 1).Style.Font.Bold = true;
                ws.Cell(17, 1).Value = report.KnownIssues;
                ws.Range(17, 1, 17, 4).Merge();
                
                // Crew List
                ws.Cell(19, 1).Value = "Crew Members";
                ws.Cell(19, 1).Style.Font.Bold = true;
                ws.Cell(20, 1).Value = "Name";
                ws.Cell(20, 2).Value = "Rank";
                ws.Cell(20, 3).Value = "Shift";
                ws.Cell(20, 4).Value = "Date On Board";
                ws.Row(20).Style.Font.Bold = true;
                
                int row = 21;
                foreach (var crew in report.CrewMembers)
                {
                    ws.Cell(row, 1).Value = crew.Name;
                    ws.Cell(row, 2).Value = crew.Rank;
                    ws.Cell(row, 3).Value = crew.Shift;
                    ws.Cell(row, 4).Value = crew.DateOnBoard.ToString("yyyy-MM-dd");
                    row++;
                }
                
                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            });
            
            System.Windows.MessageBox.Show($"Exported to: {dialog.FileName}", "Export Complete", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private async Task ExportPdfAsync(object? _)
    {
        if (CurrentReport == null) return;
        
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export DPR to PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = $"DPR_{CurrentReport.ReportDate:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            var report = CurrentReport;
            var filePath = dialog.FileName;
            
            await Task.Run(() =>
            {
                // Set QuestPDF license (Community license for open source)
                QuestPDF.Settings.License = LicenseType.Community;
                
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(10));
                        
                        page.Header().Column(col =>
                        {
                            col.Item().Text("Daily Progress Report").FontSize(20).Bold().FontColor(QuestColors.Blue.Darken2);
                            col.Item().Text($"Report Date: {report.ReportDate:yyyy-MM-dd}").FontSize(12);
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(QuestColors.Grey.Medium);
                        });
                        
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            
                            // Project Info Section
                            col.Item().Text("Project Information").Bold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });
                                
                                table.Cell().Text("Project:").Bold();
                                table.Cell().Text(report.ProjectNumber ?? "");
                                table.Cell().Text("Vessel:").Bold();
                                table.Cell().Text(report.Vessel ?? "");
                                table.Cell().Text("Client:").Bold();
                                table.Cell().Text(report.Client ?? "");
                                table.Cell().Text("Shift:").Bold();
                                table.Cell().Text(report.Shift ?? "");
                            });
                            
                            // Personnel Section
                            col.Item().PaddingTop(10).Text("Personnel").Bold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });
                                
                                table.Cell().Text("Party Chief:").Bold();
                                table.Cell().Text(report.PartyChief ?? "");
                                table.Cell().Text("Project Surveyor:").Bold();
                                table.Cell().Text(report.ProjectSurveyor ?? "");
                                table.Cell().Text("Offshore Manager:").Bold();
                                table.Cell().Text(report.OffshoreManager ?? "");
                            });
                            
                            // Comments Section
                            col.Item().PaddingTop(10).Text("General Survey Comments").Bold().FontSize(12);
                            col.Item().Border(1).Padding(5).Text(report.GeneralSurveyComments ?? "No comments");
                            
                            // Known Issues Section
                            col.Item().PaddingTop(10).Text("Known Issues").Bold().FontSize(12);
                            col.Item().Border(1).Padding(5).Text(report.KnownIssues ?? "No known issues");
                            
                            // Crew Section
                            if (report.CrewMembers.Count > 0)
                            {
                                col.Item().PaddingTop(10).Text("Crew Members").Bold().FontSize(12);
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });
                                    
                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(3).Text("Name").Bold();
                                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(3).Text("Rank").Bold();
                                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(3).Text("Shift").Bold();
                                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(3).Text("Date On Board").Bold();
                                    });
                                    
                                    foreach (var crew in report.CrewMembers)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten1).Padding(3).Text(crew.Name ?? "");
                                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten1).Padding(3).Text(crew.Rank ?? "");
                                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten1).Padding(3).Text(crew.Shift ?? "");
                                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten1).Padding(3).Text(crew.DateOnBoard.ToString("yyyy-MM-dd"));
                                    }
                                });
                            }
                        });
                        
                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Generated: ");
                            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            text.Span(" | Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                }).GeneratePdf(filePath);
            });
            
            System.Windows.MessageBox.Show($"Exported to: {dialog.FileName}", "Export Complete", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private void AddCrewMember()
    {
        if (CurrentReport == null) return;
        
        try
        {
            var dialog = new Views.CrewMemberDialog
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            
            if (dialog.ShowDialog() == true && dialog.CreatedCrewMember != null)
            {
                CurrentReport.AddCrewMember(dialog.CreatedCrewMember);
                OnPropertyChanged(nameof(CurrentReport));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding crew member: {ex.Message}");
            System.Windows.MessageBox.Show($"Error adding crew member: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private void RemoveCrewMember(CrewMember? crew)
    {
        if (CurrentReport == null || crew == null) return;
        CurrentReport.SurveyCrew.Remove(crew);
    }
    
    private void AddTransponder()
    {
        if (CurrentReport == null) return;
        
        CurrentReport.AddTransponder(new TransponderInfo
        {
            Location = "New Transponder",
            IssuedDate = DateTime.Today
        });
    }
    
    private void RemoveTransponder(TransponderInfo? transponder)
    {
        if (CurrentReport == null || transponder == null) return;
        CurrentReport.Transponders.Remove(transponder);
    }
    
    private void AddDailyLogEntry()
    {
        if (CurrentReport == null) return;
        
        CurrentReport.AddDailyLogEntry(DateTime.Now.TimeOfDay, "");
    }
    
    private void PopulateFromProject()
    {
        if (CurrentReport == null || _settings.ProjectInfo == null) return;
        CurrentReport.PopulateFromProject(_settings.ProjectInfo);
        NotifyReportPropertiesChanged();
    }
    
    private void LoadFromDate()
    {
        var existing = Reports.FirstOrDefault(r => r.ReportDate.Date == SelectedDate.Date);
        if (existing != null)
        {
            CurrentReport = existing;
        }
        else
        {
            CreateNewReport();
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void RefreshReports()
    {
        Reports.Clear();
        foreach (var report in _logService.DprReports)
        {
            Reports.Add(report);
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private void NotifyReportPropertiesChanged()
    {
        OnPropertyChanged(nameof(Client));
        OnPropertyChanged(nameof(Vessel));
        OnPropertyChanged(nameof(ProjectNumber));
        OnPropertyChanged(nameof(LocationDepth));
        OnPropertyChanged(nameof(OffshoreManager));
        OnPropertyChanged(nameof(ProjectSurveyor));
        OnPropertyChanged(nameof(PartyChief));
        OnPropertyChanged(nameof(Last24HrsHighlights));
        OnPropertyChanged(nameof(KnownIssues));
        OnPropertyChanged(nameof(GeneralSurveyComments));
    }
    
    #endregion
}
