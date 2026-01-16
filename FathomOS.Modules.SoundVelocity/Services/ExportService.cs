using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using FathomOS.Modules.SoundVelocity.Models;

namespace FathomOS.Modules.SoundVelocity.Services;

/// <summary>
/// Export CTD/SVP data to various industry formats
/// Translated from VBA USRfile, VELfile, PROfile subs
/// </summary>
public class ExportService
{
    /// <summary>
    /// Export to USR format (QINSy)
    /// Format: depth, sound_velocity (comma-separated)
    /// </summary>
    public void ExportUSR(string filePath, List<CtdDataPoint> data, ProcessingSettings settings)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.ASCII);
        
        foreach (var point in data)
        {
            double sv = Math.Round(point.SoundVelocity, 2);
            writer.WriteLine($"{point.Depth:F2},{sv:F2}");
        }

        // Add extra point at end (per VBA logic)
        if (data.Count > 0)
        {
            var last = data[^1];
            double sv = Math.Round(last.SoundVelocity, 2);
            writer.WriteLine($"{last.Depth + settings.DepthInterval:F2},{sv:F2}");
        }
    }

    /// <summary>
    /// Export to VEL format (EIVA/NaviModel)
    /// Format: 0, -depth, sound_velocity (comma-separated)
    /// </summary>
    public void ExportVEL(string filePath, List<CtdDataPoint> data, ProcessingSettings settings)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.ASCII);
        
        foreach (var point in data)
        {
            double sv = Math.Round(point.SoundVelocity, 2);
            writer.WriteLine($"0,{-point.Depth:F2},{sv:F2}");
        }

        // Add extra point at end
        if (data.Count > 0)
        {
            var last = data[^1];
            double sv = Math.Round(last.SoundVelocity, 2);
            writer.WriteLine($"0,{-(last.Depth + settings.DepthInterval):F2},{sv:F2}");
        }
    }

    /// <summary>
    /// Export to PRO format (generic profile)
    /// Format: Header lines, then depth<TAB>sound_velocity
    /// </summary>
    public void ExportPRO(string filePath, List<CtdDataPoint> data, ProcessingSettings settings, ProjectInfo project)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.ASCII);
        
        // Write header
        writer.WriteLine(project.VesselName);
        writer.WriteLine(project.CastDateTime.ToString("dd/MM/yyyy"));
        writer.WriteLine(project.CastDateTime.ToString("HH:mm:ss"));
        writer.WriteLine(project.Equipment);
        writer.WriteLine("SV Profile");

        // Write data
        foreach (var point in data)
        {
            double sv = Math.Round(point.SoundVelocity, 2);
            writer.WriteLine($"{point.Depth:F2}\t{sv:F2}");
        }

        // Add extra point at end
        if (data.Count > 0)
        {
            var last = data[^1];
            double sv = Math.Round(last.SoundVelocity, 2);
            writer.WriteLine($"{last.Depth + settings.DepthInterval:F2}\t{sv:F2}");
        }
    }

    /// <summary>
    /// Export to TSDIP Excel format with all columns
    /// </summary>
    public void ExportExcel(string filePath, List<CtdDataPoint> data, ProcessingSettings settings, 
        ProjectInfo project, DataStatistics stats, List<CtdDataPoint>? smoothedData = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("TSDIP");

        // Header information
        worksheet.Cell(1, 1).Value = "Sound Velocity Profile Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Cell(3, 1).Value = "Project:";
        worksheet.Cell(3, 2).Value = project.ProjectTitle;
        worksheet.Cell(4, 1).Value = "Vessel:";
        worksheet.Cell(4, 2).Value = project.VesselName;
        worksheet.Cell(5, 1).Value = "Date:";
        worksheet.Cell(5, 2).Value = project.CastDateTime.ToString("yyyy-MM-dd HH:mm");
        worksheet.Cell(6, 1).Value = "Location:";
        worksheet.Cell(6, 2).Value = $"{project.Location}, KP{project.KP:F3}";
        worksheet.Cell(7, 1).Value = "Equipment:";
        worksheet.Cell(7, 2).Value = project.Equipment;
        worksheet.Cell(8, 1).Value = "Latitude:";
        worksheet.Cell(8, 2).Value = $"{Math.Abs(project.Latitude):F4} {(project.Latitude >= 0 ? "N" : "S")}";
        worksheet.Cell(9, 1).Value = "Longitude:";
        worksheet.Cell(9, 2).Value = $"{Math.Abs(project.Longitude):F4} {(project.Longitude >= 0 ? "E" : "W")}";
        worksheet.Cell(10, 1).Value = "Observed by:";
        worksheet.Cell(10, 2).Value = project.ObservedBy;
        worksheet.Cell(11, 1).Value = "Checked by:";
        worksheet.Cell(11, 2).Value = project.CheckedBy;

        // Statistics
        worksheet.Cell(13, 1).Value = "Statistics";
        worksheet.Cell(13, 1).Style.Font.Bold = true;
        worksheet.Cell(14, 1).Value = "Depth Range:";
        worksheet.Cell(14, 2).Value = $"{stats.MinDepth:F1} - {stats.MaxDepth:F1} m";
        worksheet.Cell(15, 1).Value = "SV Range:";
        worksheet.Cell(15, 2).Value = $"{stats.MinSoundVelocity:F2} - {stats.MaxSoundVelocity:F2} m/s";
        worksheet.Cell(16, 1).Value = "Temp Range:";
        worksheet.Cell(16, 2).Value = $"{stats.MinTemperature:F2} - {stats.MaxTemperature:F2} °C";

        // Data table headers
        int startRow = 19;
        worksheet.Cell(startRow, 1).Value = "Depth (m)";
        worksheet.Cell(startRow, 2).Value = "SV (m/s)";
        worksheet.Cell(startRow, 3).Value = "Temp (°C)";
        worksheet.Cell(startRow, 4).Value = settings.CalculationMode == CalculationMode.CtdSvWithConductivity 
            ? "Cond (mS/cm)" : "Sal (PSU)";
        worksheet.Cell(startRow, 5).Value = "Density";

        var headerRange = worksheet.Range(startRow, 1, startRow, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data rows
        for (int i = 0; i < data.Count; i++)
        {
            var point = data[i];
            int row = startRow + 1 + i;
            worksheet.Cell(row, 1).Value = point.Depth;
            worksheet.Cell(row, 2).Value = Math.Round(point.SoundVelocity, 2);
            worksheet.Cell(row, 3).Value = Math.Round(point.Temperature, 2);
            worksheet.Cell(row, 4).Value = Math.Round(point.SalinityOrConductivity, 3);
            worksheet.Cell(row, 5).Value = Math.Round(point.Density, 4);
        }

        // Add smoothed data sheet if available
        if (smoothedData != null && smoothedData.Count > 0)
        {
            var smoothSheet = workbook.Worksheets.Add("Smoothed Data");
            smoothSheet.Cell(1, 1).Value = "Smoothed Sound Velocity Profile";
            smoothSheet.Cell(1, 1).Style.Font.Bold = true;

            smoothSheet.Cell(3, 1).Value = "Depth (m)";
            smoothSheet.Cell(3, 2).Value = "SV (m/s)";
            smoothSheet.Cell(3, 3).Value = "Temp (°C)";
            smoothSheet.Cell(3, 4).Value = settings.CalculationMode == CalculationMode.CtdSvWithConductivity 
                ? "Cond (mS/cm)" : "Sal (PSU)";
            smoothSheet.Cell(3, 5).Value = "Density";

            var smoothHeaderRange = smoothSheet.Range(3, 1, 3, 5);
            smoothHeaderRange.Style.Font.Bold = true;
            smoothHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

            for (int i = 0; i < smoothedData.Count; i++)
            {
                var point = smoothedData[i];
                int row = 4 + i;
                smoothSheet.Cell(row, 1).Value = point.Depth;
                smoothSheet.Cell(row, 2).Value = Math.Round(point.SoundVelocity, 2);
                smoothSheet.Cell(row, 3).Value = Math.Round(point.Temperature, 2);
                smoothSheet.Cell(row, 4).Value = Math.Round(point.SalinityOrConductivity, 3);
                smoothSheet.Cell(row, 5).Value = Math.Round(point.Density, 4);
            }

            smoothSheet.Columns().AdjustToContents();
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// Export to CSV format
    /// </summary>
    public void ExportCSV(string filePath, List<CtdDataPoint> data, ProcessingSettings settings)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        
        // Header
        writer.WriteLine("Depth,SoundVelocity,Temperature,Salinity,Density,Pressure");

        // Data
        foreach (var point in data)
        {
            writer.WriteLine($"{point.Depth:F2},{point.SoundVelocity:F2},{point.Temperature:F2},{point.SalinityOrConductivity:F3},{point.Density:F4},{point.Pressure:F2}");
        }
    }

    /// <summary>
    /// Export all selected formats
    /// </summary>
    public List<string> ExportAll(List<CtdDataPoint> data, ProcessingSettings settings, 
        ProjectInfo project, ExportOptions options, DataStatistics stats, 
        List<CtdDataPoint>? smoothedData = null)
    {
        var exportedFiles = new List<string>();
        string basePath = Path.Combine(options.OutputDirectory, options.BaseFileName);

        try
        {
            if (options.ExportUsr)
            {
                string usrPath = basePath + ".usr";
                ExportUSR(usrPath, data, settings);
                exportedFiles.Add(usrPath);
            }

            if (options.ExportVel)
            {
                string velPath = basePath + ".vel";
                ExportVEL(velPath, data, settings);
                exportedFiles.Add(velPath);
            }

            if (options.ExportPro)
            {
                string proPath = basePath + ".pro";
                ExportPRO(proPath, data, settings, project);
                exportedFiles.Add(proPath);
            }

            if (options.ExportExcel)
            {
                string excelPath = basePath + ".xlsx";
                ExportExcel(excelPath, data, settings, project, stats, 
                    options.ExportSmoothed ? smoothedData : null);
                exportedFiles.Add(excelPath);
            }

            if (options.ExportCsv)
            {
                string csvPath = basePath + ".csv";
                ExportCSV(csvPath, data, settings);
                exportedFiles.Add(csvPath);
            }

            // Export smoothed data separately if requested
            if (options.ExportSmoothed && smoothedData != null && smoothedData.Count > 0)
            {
                string smoothUsrPath = basePath + "_smoothed.usr";
                ExportUSR(smoothUsrPath, smoothedData, settings);
                exportedFiles.Add(smoothUsrPath);

                string smoothVelPath = basePath + "_smoothed.vel";
                ExportVEL(smoothVelPath, smoothedData, settings);
                exportedFiles.Add(smoothVelPath);

                string smoothCsvPath = basePath + "_smoothed.csv";
                ExportCSV(smoothCsvPath, smoothedData, settings);
                exportedFiles.Add(smoothCsvPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Export error: {ex.Message}");
            throw;
        }

        return exportedFiles;
    }
}
