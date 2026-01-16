namespace FathomOS.Core.Calculations;

using FathomOS.Core.Models;

/// <summary>
/// Applies tidal corrections to survey depth data
/// </summary>
public class TideCorrector
{
    private readonly TideData _tideData;
    private readonly bool _useFeet;

    public TideCorrector(TideData tideData, bool convertToFeet = false)
    {
        _tideData = tideData ?? throw new ArgumentNullException(nameof(tideData));
        _useFeet = convertToFeet;
    }

    /// <summary>
    /// Get tide value at a specific time
    /// </summary>
    /// <param name="dateTime">Time to get tide for</param>
    /// <returns>Tide value in meters or feet based on constructor setting</returns>
    public double? GetTide(DateTime dateTime)
    {
        if (_useFeet)
        {
            return _tideData.GetTideAtTimeInFeet(dateTime);
        }
        return _tideData.GetTideAtTime(dateTime);
    }

    /// <summary>
    /// Apply tidal correction to a single depth value
    /// </summary>
    /// <param name="depth">Original depth (positive down)</param>
    /// <param name="tide">Tide value (positive up from MSL)</param>
    /// <returns>Corrected depth</returns>
    public static double ApplyCorrection(double depth, double tide)
    {
        // Depth is positive downward, tide is positive upward
        // When tide is high (positive), water is higher, so true depth is less
        // Corrected depth = observed depth - tide
        return depth - tide;
    }

    /// <summary>
    /// Apply tidal corrections to all survey points
    /// </summary>
    /// <param name="points">Survey points to correct</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <returns>Number of points corrected</returns>
    public int ApplyToAll(IList<SurveyPoint> points, IProgress<int>? progress = null)
    {
        int corrected = 0;

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];

            if (point.Depth.HasValue)
            {
                var tide = GetTide(point.DateTime);
                if (tide.HasValue)
                {
                    point.TideCorrection = tide.Value;
                    point.CorrectedDepth = ApplyCorrection(point.Depth.Value, tide.Value);
                    corrected++;
                }
                else
                {
                    // No tide data available for this time
                    point.TideCorrection = null;
                    point.CorrectedDepth = point.Depth;
                }
            }

            if (progress != null && i % 100 == 0)
            {
                progress.Report((int)(100.0 * i / points.Count));
            }
        }

        return corrected;
    }

    /// <summary>
    /// Validate that tide data covers the survey time range
    /// </summary>
    /// <param name="points">Survey points to check</param>
    /// <returns>Validation result with any issues found</returns>
    public TideValidationResult Validate(IList<SurveyPoint> points)
    {
        var result = new TideValidationResult { IsValid = true };

        if (points.Count == 0)
        {
            result.Issues.Add("No survey points to validate");
            return result;
        }

        var surveyStart = points.Min(p => p.DateTime);
        var surveyEnd = points.Max(p => p.DateTime);

        if (!_tideData.CoversTimeRange(surveyStart, surveyEnd))
        {
            result.IsValid = false;
            result.Issues.Add($"Tide data does not fully cover survey period");
            result.Issues.Add($"Survey: {surveyStart:yyyy-MM-dd HH:mm} to {surveyEnd:yyyy-MM-dd HH:mm}");
            result.Issues.Add($"Tide: {_tideData.StartTime:yyyy-MM-dd HH:mm} to {_tideData.EndTime:yyyy-MM-dd HH:mm}");
        }

        // Check for gaps in tide data (assuming 1-minute interval expected)
        var tideRecords = _tideData.Records.OrderBy(r => r.DateTime).ToList();
        for (int i = 1; i < tideRecords.Count; i++)
        {
            var gap = (tideRecords[i].DateTime - tideRecords[i - 1].DateTime).TotalMinutes;
            if (gap > 5) // More than 5 minutes gap
            {
                result.Issues.Add($"Gap in tide data: {tideRecords[i - 1].DateTime:HH:mm} to {tideRecords[i].DateTime:HH:mm} ({gap:F0} min)");
            }
        }

        return result;
    }
}

/// <summary>
/// Result of tide data validation
/// </summary>
public class TideValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; } = new();
}
