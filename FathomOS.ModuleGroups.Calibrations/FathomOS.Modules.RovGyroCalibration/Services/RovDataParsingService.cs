using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FathomOS.Modules.RovGyroCalibration.Models;

namespace FathomOS.Modules.RovGyroCalibration.Services;

/// <summary>
/// ROV-specific data parsing service that applies geometric corrections.
/// Formula: C = V + D + θ + FacingDirectionOffset
/// Where: C = Calculated ROV heading, V = Vessel heading, D = Baseline offset,
///        θ = Baseline angle, FacingDirectionOffset = ROV facing correction
/// </summary>
public class RovDataParsingService
{
    /// <summary>
    /// Parse file with ROV geometric corrections applied
    /// </summary>
    public List<RovGyroDataPoint> ParseWithMapping(
        RawFileData rawData, 
        RovGyroColumnMapping mapping,
        RovConfiguration config)
    {
        var points = new List<RovGyroDataPoint>();
        int offset = mapping.HasDateTimeSplit ? 1 : 0;

        // Pre-calculate geometric corrections
        double baselineAngle = config.CalculateBaselineAngle();
        double facingOffset = config.FacingDirectionOffset;
        double totalCorrection = config.BaselineOffset + baselineAngle + facingOffset;

        for (int i = 0; i < rawData.DataRows.Count; i++)
        {
            try
            {
                var values = rawData.DataRows[i];
                if (values.Length < 3) continue;

                var point = new RovGyroDataPoint { Index = i + 1 };

                // Parse DateTime
                point.DateTime = ParseDateTime(values, mapping);

                // Parse Vessel Heading
                int vesselIdx = mapping.VesselHeadingColumnIndex + offset;
                if (vesselIdx >= 0 && vesselIdx < values.Length)
                {
                    if (double.TryParse(values[vesselIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var vh))
                        point.VesselHeading = NormalizeHeading(vh);
                }

                // Parse ROV Heading (observed)
                int rovIdx = mapping.RovHeadingColumnIndex + offset;
                if (rovIdx >= 0 && rovIdx < values.Length)
                {
                    if (double.TryParse(values[rovIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var rh))
                        point.RovHeading = NormalizeHeading(rh);
                }

                // Store geometric parameters
                point.BaselineOffset = config.BaselineOffset;
                point.BaselineAngle = baselineAngle;
                point.FacingDirectionOffset = facingOffset;

                // Calculate expected heading: C = V + D + θ + FacingOffset
                point.CalculatedHeading = NormalizeHeading(
                    point.VesselHeading + totalCorrection);

                // Calculate C-O (Calculated minus Observed)
                point.CalculatedCO = CalculateCO(point.CalculatedHeading, point.RovHeading);

                points.Add(point);
            }
            catch { }
        }

        return points;
    }

    private DateTime ParseDateTime(string[] values, RovGyroColumnMapping mapping)
    {
        if (mapping.HasDateTimeSplit && values.Length >= 2)
        {
            if (DateTime.TryParse($"{values[0]} {values[1]}", out var dt))
                return dt;
        }
        else if (mapping.TimeColumnIndex >= 0 && mapping.TimeColumnIndex < values.Length)
        {
            if (DateTime.TryParse(values[mapping.TimeColumnIndex], out var dt))
                return dt;
        }
        return DateTime.MinValue;
    }

    private static double NormalizeHeading(double heading)
    {
        while (heading < 0) heading += 360;
        while (heading >= 360) heading -= 360;
        return heading;
    }

    /// <summary>
    /// Calculate C-O with 360° wrap-around
    /// </summary>
    public static double CalculateCO(double calculated, double observed)
    {
        double diff = calculated - observed;
        if (diff < -180) return diff + 360;
        if (diff > 180) return diff - 360;
        return diff;
    }
}
