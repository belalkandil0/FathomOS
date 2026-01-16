using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.SoundVelocity.Models;

namespace FathomOS.Modules.SoundVelocity.Services;

/// <summary>
/// Service for processing CTD data - sorting, interpolation, and calculations
/// Translated from VBA SV.bas WyborGleb sub
/// </summary>
public class DataProcessingService
{
    /// <summary>
    /// Process raw CTD data: sort, interpolate, and calculate derived values
    /// </summary>
    public List<CtdDataPoint> ProcessData(List<CtdDataPoint> rawData, ProcessingSettings settings, 
        double latitude, bool useLatitudeGravity)
    {
        if (rawData.Count == 0) return new List<CtdDataPoint>();

        // Step 1: Filter out excluded points and sort by depth
        var sortedData = rawData
            .Where(p => !p.IsExcluded && p.Depth >= settings.TransducerDepth)
            .OrderBy(p => p.Depth)
            .ToList();

        if (sortedData.Count < 2) return sortedData;

        // Step 2: Create depth table at regular intervals
        var depthTable = CreateDepthTable(
            sortedData.Min(p => p.Depth),
            sortedData.Max(p => p.Depth),
            settings.DepthInterval,
            settings.TransducerDepth);

        // Step 3: Interpolate data at each depth in the table
        var interpolatedData = InterpolateData(sortedData, depthTable);

        // Step 4: Calculate derived values if needed
        CalculateDerivedValues(interpolatedData, settings, latitude, useLatitudeGravity);

        return interpolatedData;
    }

    /// <summary>
    /// Create depth table at regular intervals
    /// Translated from VBA Create_DepthTable function
    /// </summary>
    private List<double> CreateDepthTable(double minDepth, double maxDepth, double interval, double transducerDepth)
    {
        var depths = new List<double>();

        // Start at first interval above transducer or first data point
        double startDepth = minDepth > transducerDepth 
            ? Math.Floor(minDepth) + interval 
            : Math.Floor(transducerDepth);

        double currentDepth = startDepth;
        while (currentDepth < maxDepth - interval)
        {
            depths.Add(currentDepth);
            currentDepth += interval;
        }

        // Add final depth point
        if (depths.Count == 0 || depths[^1] < maxDepth)
        {
            depths.Add(maxDepth);
        }

        return depths;
    }

    /// <summary>
    /// Interpolate data to regular depth intervals
    /// Translated from VBA interpolation logic in WyborGleb
    /// </summary>
    private List<CtdDataPoint> InterpolateData(List<CtdDataPoint> sortedData, List<double> depthTable)
    {
        var result = new List<CtdDataPoint>();

        for (int j = 0; j < depthTable.Count; j++)
        {
            double targetDepth = depthTable[j];

            // Find bracketing points for interpolation
            int lowerIdx = -1, upperIdx = -1;
            for (int i = 0; i < sortedData.Count - 1; i++)
            {
                if (sortedData[i].Depth <= targetDepth && sortedData[i + 1].Depth >= targetDepth)
                {
                    lowerIdx = i;
                    upperIdx = i + 1;
                    break;
                }
            }

            if (lowerIdx < 0 || upperIdx < 0)
            {
                // Target depth outside data range - use nearest point
                if (targetDepth <= sortedData[0].Depth)
                {
                    lowerIdx = upperIdx = 0;
                }
                else
                {
                    lowerIdx = upperIdx = sortedData.Count - 1;
                }
            }

            var lower = sortedData[lowerIdx];
            var upper = sortedData[upperIdx];

            // Interpolate all values
            var interpolated = new CtdDataPoint
            {
                Index = j + 1,
                Depth = targetDepth,
                IsInterpolated = true
            };

            if (lowerIdx == upperIdx)
            {
                // Use exact values
                interpolated.SoundVelocity = lower.SoundVelocity;
                interpolated.Temperature = lower.Temperature;
                interpolated.SalinityOrConductivity = lower.SalinityOrConductivity;
                interpolated.Density = lower.Density;
                interpolated.Pressure = lower.Pressure;
            }
            else
            {
                // Linear interpolation
                interpolated.SoundVelocity = OceanographicCalculations.Interpolate(
                    lower.SoundVelocity, upper.SoundVelocity, lower.Depth, targetDepth, upper.Depth);
                interpolated.Temperature = OceanographicCalculations.Interpolate(
                    lower.Temperature, upper.Temperature, lower.Depth, targetDepth, upper.Depth);
                interpolated.SalinityOrConductivity = OceanographicCalculations.Interpolate(
                    lower.SalinityOrConductivity, upper.SalinityOrConductivity, lower.Depth, targetDepth, upper.Depth);
                interpolated.Density = OceanographicCalculations.Interpolate(
                    lower.Density, upper.Density, lower.Depth, targetDepth, upper.Depth);
                interpolated.Pressure = OceanographicCalculations.Interpolate(
                    lower.Pressure, upper.Pressure, lower.Depth, targetDepth, upper.Depth);
            }

            result.Add(interpolated);
        }

        return result;
    }

    /// <summary>
    /// Calculate derived values (SV, Density) based on processing settings
    /// </summary>
    private void CalculateDerivedValues(List<CtdDataPoint> data, ProcessingSettings settings, 
        double latitude, bool useLatitudeGravity)
    {
        foreach (var point in data)
        {
            // Calculate pressure from depth if needed
            if (settings.InputType == DepthPressureType.Depth)
            {
                point.Pressure = OceanographicCalculations.DepthToPressure(point.Depth, latitude, useLatitudeGravity);
            }

            double salinity = point.SalinityOrConductivity;

            // Convert conductivity to salinity if using CTD mode with conductivity
            if (settings.CalculationMode == CalculationMode.CtdSvWithConductivity)
            {
                salinity = OceanographicCalculations.ConductivityToSalinity(
                    point.Pressure, point.Temperature, point.SalinityOrConductivity);
            }

            // Calculate sound velocity if not using external source
            if (settings.SvFormula != SoundVelocityFormula.ExternalSource)
            {
                // Determine which formula to use
                SoundVelocityFormula formulaToUse = settings.SvFormula;
                
                // Auto mode: select formula based on depth
                // Chen & Millero for depth ≤ 1000m, Del Grosso for depth > 1000m
                if (settings.SvFormula == SoundVelocityFormula.Auto)
                {
                    formulaToUse = point.Depth <= 1000 
                        ? SoundVelocityFormula.ChenMillero 
                        : SoundVelocityFormula.DelGrosso;
                }
                
                point.SoundVelocity = formulaToUse == SoundVelocityFormula.ChenMillero
                    ? OceanographicCalculations.ChenMillero(point.Pressure, point.Temperature, salinity)
                    : OceanographicCalculations.DelGrosso(point.Pressure, point.Temperature, salinity);
            }

            // Calculate density if not using external source
            if (settings.DensityFormula != DensityFormula.ExternalSource)
            {
                point.Density = OceanographicCalculations.DensityEOS80(point.Pressure, point.Temperature, salinity, true);
            }
        }
    }

    /// <summary>
    /// Calculate statistics for the data set
    /// </summary>
    public DataStatistics CalculateStatistics(List<CtdDataPoint> data)
    {
        if (data.Count == 0)
            return new DataStatistics();

        return new DataStatistics
        {
            PointCount = data.Count,
            MinDepth = data.Min(p => p.Depth),
            MaxDepth = data.Max(p => p.Depth),
            MinSoundVelocity = data.Min(p => p.SoundVelocity),
            MaxSoundVelocity = data.Max(p => p.SoundVelocity),
            AvgSoundVelocity = data.Average(p => p.SoundVelocity),
            MinTemperature = data.Min(p => p.Temperature),
            MaxTemperature = data.Max(p => p.Temperature),
            AvgTemperature = data.Average(p => p.Temperature),
            MinSalinity = data.Min(p => p.SalinityOrConductivity),
            MaxSalinity = data.Max(p => p.SalinityOrConductivity),
            AvgSalinity = data.Average(p => p.SalinityOrConductivity),
            MinDensity = data.Min(p => p.Density),
            MaxDensity = data.Max(p => p.Density),
            AvgDensity = data.Average(p => p.Density)
        };
    }
}
