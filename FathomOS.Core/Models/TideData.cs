namespace FathomOS.Core.Models;

/// <summary>
/// Represents a single tide record
/// </summary>
public class TideRecord
{
    /// <summary>
    /// Date and time of the tide prediction
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Tide value in meters above MSL
    /// </summary>
    public double TideMeters { get; set; }

    /// <summary>
    /// Tide value in feet above MSL (from file or calculated)
    /// </summary>
    public double TideFeet { get; set; }

    public override string ToString()
    {
        return $"{DateTime:MM/dd/yyyy HH:mm} - {TideMeters:F3}m ({TideFeet:F3}ft)";
    }
}

/// <summary>
/// Complete tide data from a tide file
/// </summary>
public class TideData
{
    /// <summary>
    /// Source file path
    /// </summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>
    /// Software used to generate the tide file
    /// </summary>
    public string Software { get; set; } = string.Empty;

    /// <summary>
    /// Version of the tide software
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Latitude of tide location
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude of tide location
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Time zone offset from GMT
    /// </summary>
    public double TimeZoneOffset { get; set; }

    /// <summary>
    /// Date the listing was generated
    /// </summary>
    public DateTime? ListingDate { get; set; }

    /// <summary>
    /// All tide records
    /// </summary>
    public List<TideRecord> Records { get; set; } = new();

    /// <summary>
    /// Start date/time of tide data
    /// </summary>
    public DateTime? StartTime => Records.Count > 0 ? Records.First().DateTime : null;

    /// <summary>
    /// End date/time of tide data
    /// </summary>
    public DateTime? EndTime => Records.Count > 0 ? Records.Last().DateTime : null;

    /// <summary>
    /// Total number of records
    /// </summary>
    public int RecordCount => Records.Count;

    /// <summary>
    /// Get interpolated tide value at a specific time (in meters)
    /// </summary>
    /// <param name="dateTime">The time to get tide for</param>
    /// <returns>Interpolated tide value in meters, or null if outside data range</returns>
    public double? GetTideAtTime(DateTime dateTime)
    {
        if (Records.Count == 0)
            return null;

        // Check if outside range
        if (dateTime < Records.First().DateTime || dateTime > Records.Last().DateTime)
            return null;

        // Find bracketing records
        TideRecord? before = null;
        TideRecord? after = null;

        for (int i = 0; i < Records.Count - 1; i++)
        {
            if (Records[i].DateTime <= dateTime && Records[i + 1].DateTime >= dateTime)
            {
                before = Records[i];
                after = Records[i + 1];
                break;
            }
        }

        if (before == null || after == null)
            return null;

        // Exact match
        if (before.DateTime == dateTime)
            return before.TideMeters;

        // Linear interpolation
        double totalSeconds = (after.DateTime - before.DateTime).TotalSeconds;
        double elapsedSeconds = (dateTime - before.DateTime).TotalSeconds;
        double fraction = elapsedSeconds / totalSeconds;

        return before.TideMeters + fraction * (after.TideMeters - before.TideMeters);
    }

    /// <summary>
    /// Get interpolated tide value at a specific time (in feet)
    /// </summary>
    public double? GetTideAtTimeInFeet(DateTime dateTime)
    {
        var tideMeters = GetTideAtTime(dateTime);
        if (!tideMeters.HasValue)
            return null;

        // Convert meters to feet (standard conversion)
        return tideMeters.Value * 3.28084;
    }

    /// <summary>
    /// Validate that tide data covers a given time range
    /// </summary>
    public bool CoversTimeRange(DateTime start, DateTime end)
    {
        if (Records.Count == 0)
            return false;

        return Records.First().DateTime <= start && Records.Last().DateTime >= end;
    }

    /// <summary>
    /// Get statistics about the tide data
    /// </summary>
    public TideStatistics GetStatistics()
    {
        if (Records.Count == 0)
            return new TideStatistics();

        return new TideStatistics
        {
            MinTideMeters = Records.Min(r => r.TideMeters),
            MaxTideMeters = Records.Max(r => r.TideMeters),
            MeanTideMeters = Records.Average(r => r.TideMeters),
            RecordCount = Records.Count,
            StartTime = Records.First().DateTime,
            EndTime = Records.Last().DateTime
        };
    }
}

/// <summary>
/// Statistics about tide data
/// </summary>
public class TideStatistics
{
    public double MinTideMeters { get; set; }
    public double MaxTideMeters { get; set; }
    public double MeanTideMeters { get; set; }
    public int RecordCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public double MinTideFeet => MinTideMeters * 3.28084;
    public double MaxTideFeet => MaxTideMeters * 3.28084;
    public double TidalRangeMeters => MaxTideMeters - MinTideMeters;
    public double TidalRangeFeet => TidalRangeMeters * 3.28084;
    public TimeSpan Duration => EndTime - StartTime;
}
