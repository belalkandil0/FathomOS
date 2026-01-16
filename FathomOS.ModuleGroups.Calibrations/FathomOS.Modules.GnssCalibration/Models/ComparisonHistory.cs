namespace FathomOS.Modules.GnssCalibration.Models;

/// <summary>
/// Represents a single comparison run in the history.
/// </summary>
public class ComparisonHistoryEntry
{
    /// <summary>Unique identifier for this entry.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    
    /// <summary>When the comparison was run.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>Source NPD file name.</summary>
    public string NpdFileName { get; set; } = "";
    
    /// <summary>System 1 name.</summary>
    public string System1Name { get; set; } = "";
    
    /// <summary>System 2 name.</summary>
    public string System2Name { get; set; } = "";
    
    /// <summary>Total points processed.</summary>
    public int TotalPoints { get; set; }
    
    /// <summary>Points accepted after filtering.</summary>
    public int AcceptedPoints { get; set; }
    
    /// <summary>Points rejected by filter.</summary>
    public int RejectedPoints { get; set; }
    
    /// <summary>Sigma threshold used.</summary>
    public double SigmaThreshold { get; set; }
    
    /// <summary>2DRMS result value.</summary>
    public double TwoDRMS { get; set; }
    
    /// <summary>CEP 50% result.</summary>
    public double Cep50 { get; set; }
    
    /// <summary>CEP 95% result.</summary>
    public double Cep95 { get; set; }
    
    /// <summary>Max radial from average.</summary>
    public double MaxRadialFromAverage { get; set; }
    
    /// <summary>Mean delta easting.</summary>
    public double MeanDeltaEasting { get; set; }
    
    /// <summary>Mean delta northing.</summary>
    public double MeanDeltaNorthing { get; set; }
    
    /// <summary>Tolerance value used.</summary>
    public double ToleranceValue { get; set; }
    
    /// <summary>Whether tolerance check was enabled.</summary>
    public bool ToleranceEnabled { get; set; }
    
    /// <summary>Whether tolerance was passed.</summary>
    public bool TolerancePassed { get; set; }
    
    /// <summary>Unit abbreviation (m, ft, US ft).</summary>
    public string Unit { get; set; } = "m";
    
    /// <summary>Processing time in milliseconds.</summary>
    public double ProcessingTimeMs { get; set; }
    
    /// <summary>Optional notes from user.</summary>
    public string Notes { get; set; } = "";
    
    /// <summary>
    /// Creates a history entry from current processing results.
    /// </summary>
    public static ComparisonHistoryEntry FromResults(
        GnssProject project,
        GnssStatisticsResult statistics,
        int totalPoints,
        double processingTimeMs)
    {
        var filtered = statistics.FilteredStatistics;
        
        return new ComparisonHistoryEntry
        {
            Timestamp = DateTime.Now,
            NpdFileName = System.IO.Path.GetFileName(project.NpdFilePath ?? ""),
            System1Name = project.System1Name,
            System2Name = project.System2Name,
            TotalPoints = totalPoints,
            AcceptedPoints = statistics.AcceptedCount,
            RejectedPoints = statistics.RejectedCount,
            SigmaThreshold = project.SigmaThreshold,
            TwoDRMS = filtered.Delta2DRMS,
            Cep50 = filtered.Cep50,
            Cep95 = filtered.Cep95,
            MaxRadialFromAverage = filtered.MaxRadialFromAverage,
            MeanDeltaEasting = filtered.DeltaMeanEasting,
            MeanDeltaNorthing = filtered.DeltaMeanNorthing,
            ToleranceValue = project.ToleranceValue,
            ToleranceEnabled = project.ToleranceCheckEnabled,
            TolerancePassed = statistics.PassesTolerance,
            Unit = project.UnitAbbreviation,
            ProcessingTimeMs = processingTimeMs
        };
    }
}

/// <summary>
/// Container for comparison history with persistence support.
/// </summary>
public class ComparisonHistory
{
    /// <summary>List of history entries, newest first.</summary>
    public List<ComparisonHistoryEntry> Entries { get; set; } = new();
    
    /// <summary>Maximum entries to keep in history.</summary>
    public int MaxEntries { get; set; } = 50;
    
    /// <summary>
    /// Adds a new entry to the history.
    /// </summary>
    public void Add(ComparisonHistoryEntry entry)
    {
        // Insert at front (newest first)
        Entries.Insert(0, entry);
        
        // Trim to max
        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }
    
    /// <summary>
    /// Clears all history entries.
    /// </summary>
    public void Clear()
    {
        Entries.Clear();
    }
    
    /// <summary>
    /// Gets entries filtered by date range.
    /// </summary>
    public IEnumerable<ComparisonHistoryEntry> GetByDateRange(DateTime start, DateTime end)
    {
        return Entries.Where(e => e.Timestamp >= start && e.Timestamp <= end);
    }
    
    /// <summary>
    /// Gets entries for a specific NPD file.
    /// </summary>
    public IEnumerable<ComparisonHistoryEntry> GetByFile(string fileName)
    {
        return Entries.Where(e => 
            e.NpdFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Removes an entry by ID.
    /// </summary>
    public bool Remove(string id)
    {
        var entry = Entries.FirstOrDefault(e => e.Id == id);
        if (entry != null)
        {
            Entries.Remove(entry);
            return true;
        }
        return false;
    }
}
