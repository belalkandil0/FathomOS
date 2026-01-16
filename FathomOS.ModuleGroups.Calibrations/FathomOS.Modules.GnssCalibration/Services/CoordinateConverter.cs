using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Coordinate conversion utilities for WGS84 ↔ UTM transformations.
/// Uses standard WGS84 ellipsoid parameters.
/// </summary>
public class CoordinateConverter
{
    // WGS84 Ellipsoid Parameters
    private const double SemiMajorAxis = 6378137.0;           // a (meters)
    private const double InverseFlattening = 298.257223563;   // 1/f
    private const double Flattening = 1.0 / InverseFlattening; // f
    private const double SemiMinorAxis = SemiMajorAxis * (1 - Flattening); // b
    
    // Derived constants
    private static readonly double EccentricitySquared = 2 * Flattening - Flattening * Flattening; // e²
    private static readonly double EccentricityPrimeSquared = EccentricitySquared / (1 - EccentricitySquared); // e'²
    
    // UTM parameters
    private const double K0 = 0.9996;  // Scale factor at central meridian
    private const double FalseEasting = 500000.0;  // meters
    private const double FalseNorthingSouth = 10000000.0;  // meters (for southern hemisphere)
    
    /// <summary>
    /// Convert geographic coordinates (Lat/Lon) to UTM.
    /// </summary>
    /// <param name="latitude">Latitude in degrees (-90 to 90)</param>
    /// <param name="longitude">Longitude in degrees (-180 to 180)</param>
    /// <param name="zone">Output UTM zone number (1-60)</param>
    /// <param name="isNorthern">Output hemisphere indicator</param>
    /// <returns>Tuple of (Easting, Northing) in meters</returns>
    public static (double Easting, double Northing) LatLonToUtm(
        double latitude, 
        double longitude, 
        out int zone, 
        out bool isNorthern)
    {
        isNorthern = latitude >= 0;
        
        // Calculate UTM zone
        zone = (int)Math.Floor((longitude + 180) / 6) + 1;
        
        // Handle special zones for Norway and Svalbard
        if (latitude >= 56 && latitude < 64 && longitude >= 3 && longitude < 12)
            zone = 32;
        else if (latitude >= 72 && latitude < 84)
        {
            if (longitude >= 0 && longitude < 9) zone = 31;
            else if (longitude >= 9 && longitude < 21) zone = 33;
            else if (longitude >= 21 && longitude < 33) zone = 35;
            else if (longitude >= 33 && longitude < 42) zone = 37;
        }
        
        return LatLonToUtm(latitude, longitude, zone);
    }
    
    /// <summary>
    /// Convert geographic coordinates to UTM with specified zone.
    /// </summary>
    public static (double Easting, double Northing) LatLonToUtm(
        double latitude, 
        double longitude, 
        int zone)
    {
        double latRad = DegreesToRadians(latitude);
        double lonRad = DegreesToRadians(longitude);
        
        // Central meridian of zone
        double centralMeridian = DegreesToRadians((zone - 1) * 6 - 180 + 3);
        
        // Calculate intermediate values
        double N = SemiMajorAxis / Math.Sqrt(1 - EccentricitySquared * Math.Sin(latRad) * Math.Sin(latRad));
        double T = Math.Tan(latRad) * Math.Tan(latRad);
        double C = EccentricityPrimeSquared * Math.Cos(latRad) * Math.Cos(latRad);
        double A = Math.Cos(latRad) * (lonRad - centralMeridian);
        
        // Meridional arc
        double M = SemiMajorAxis * (
            (1 - EccentricitySquared / 4 - 3 * Math.Pow(EccentricitySquared, 2) / 64 - 
             5 * Math.Pow(EccentricitySquared, 3) / 256) * latRad -
            (3 * EccentricitySquared / 8 + 3 * Math.Pow(EccentricitySquared, 2) / 32 + 
             45 * Math.Pow(EccentricitySquared, 3) / 1024) * Math.Sin(2 * latRad) +
            (15 * Math.Pow(EccentricitySquared, 2) / 256 + 
             45 * Math.Pow(EccentricitySquared, 3) / 1024) * Math.Sin(4 * latRad) -
            (35 * Math.Pow(EccentricitySquared, 3) / 3072) * Math.Sin(6 * latRad));
        
        // Calculate Easting
        double easting = K0 * N * (
            A + 
            (1 - T + C) * Math.Pow(A, 3) / 6 +
            (5 - 18 * T + T * T + 72 * C - 58 * EccentricityPrimeSquared) * Math.Pow(A, 5) / 120
        ) + FalseEasting;
        
        // Calculate Northing
        double northing = K0 * (
            M + N * Math.Tan(latRad) * (
                Math.Pow(A, 2) / 2 +
                (5 - T + 9 * C + 4 * C * C) * Math.Pow(A, 4) / 24 +
                (61 - 58 * T + T * T + 600 * C - 330 * EccentricityPrimeSquared) * Math.Pow(A, 6) / 720
            )
        );
        
        // Adjust for southern hemisphere
        if (latitude < 0)
            northing += FalseNorthingSouth;
        
        return (easting, northing);
    }
    
    /// <summary>
    /// Convert UTM coordinates to geographic (Lat/Lon).
    /// </summary>
    /// <param name="easting">UTM Easting in meters</param>
    /// <param name="northing">UTM Northing in meters</param>
    /// <param name="zone">UTM zone number (1-60)</param>
    /// <param name="isNorthern">True if northern hemisphere</param>
    /// <returns>Tuple of (Latitude, Longitude) in degrees</returns>
    public static (double Latitude, double Longitude) UtmToLatLon(
        double easting, 
        double northing, 
        int zone, 
        bool isNorthern)
    {
        // Remove false easting and northing
        double x = easting - FalseEasting;
        double y = isNorthern ? northing : northing - FalseNorthingSouth;
        
        // Central meridian
        double centralMeridian = DegreesToRadians((zone - 1) * 6 - 180 + 3);
        
        // Footpoint latitude
        double M = y / K0;
        double mu = M / (SemiMajorAxis * (1 - EccentricitySquared / 4 - 
                                          3 * Math.Pow(EccentricitySquared, 2) / 64 - 
                                          5 * Math.Pow(EccentricitySquared, 3) / 256));
        
        double e1 = (1 - Math.Sqrt(1 - EccentricitySquared)) / (1 + Math.Sqrt(1 - EccentricitySquared));
        
        double phi1 = mu +
            (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32) * Math.Sin(2 * mu) +
            (21 * e1 * e1 / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu) +
            (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu) +
            (1097 * Math.Pow(e1, 4) / 512) * Math.Sin(8 * mu);
        
        // Calculate intermediate values
        double N1 = SemiMajorAxis / Math.Sqrt(1 - EccentricitySquared * Math.Sin(phi1) * Math.Sin(phi1));
        double T1 = Math.Tan(phi1) * Math.Tan(phi1);
        double C1 = EccentricityPrimeSquared * Math.Cos(phi1) * Math.Cos(phi1);
        double R1 = SemiMajorAxis * (1 - EccentricitySquared) / 
                    Math.Pow(1 - EccentricitySquared * Math.Sin(phi1) * Math.Sin(phi1), 1.5);
        double D = x / (N1 * K0);
        
        // Calculate latitude
        double lat = phi1 - (N1 * Math.Tan(phi1) / R1) * (
            D * D / 2 -
            (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * EccentricityPrimeSquared) * Math.Pow(D, 4) / 24 +
            (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * EccentricityPrimeSquared - 3 * C1 * C1) * 
            Math.Pow(D, 6) / 720
        );
        
        // Calculate longitude
        double lon = centralMeridian + (
            D -
            (1 + 2 * T1 + C1) * Math.Pow(D, 3) / 6 +
            (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * EccentricityPrimeSquared + 24 * T1 * T1) * 
            Math.Pow(D, 5) / 120
        ) / Math.Cos(phi1);
        
        return (RadiansToDegrees(lat), RadiansToDegrees(lon));
    }
    
    /// <summary>
    /// Determine UTM zone from longitude.
    /// </summary>
    public static int GetUtmZone(double longitude)
    {
        return (int)Math.Floor((longitude + 180) / 6) + 1;
    }
    
    /// <summary>
    /// Get the central meridian of a UTM zone.
    /// </summary>
    public static double GetCentralMeridian(int zone)
    {
        return (zone - 1) * 6 - 180 + 3;
    }
    
    /// <summary>
    /// Convert POS data points from geographic to projected coordinates.
    /// </summary>
    public static void ConvertToProjected(List<PosDataPoint> points, int utmZone, bool northern)
    {
        foreach (var point in points)
        {
            if (point.Latitude.HasValue && point.Longitude.HasValue)
            {
                var (easting, northing) = LatLonToUtm(
                    point.Latitude.Value, 
                    point.Longitude.Value, 
                    utmZone);
                
                point.Easting = easting;
                point.Northing = northing;
            }
        }
    }
    
    /// <summary>
    /// Convert POS data points from projected to geographic coordinates.
    /// </summary>
    public static void ConvertToGeographic(List<PosDataPoint> points, int utmZone, bool northern)
    {
        foreach (var point in points)
        {
            if (!double.IsNaN(point.Easting) && !double.IsNaN(point.Northing))
            {
                var (lat, lon) = UtmToLatLon(
                    point.Easting, 
                    point.Northing, 
                    utmZone, 
                    northern);
                
                point.Latitude = lat;
                point.Longitude = lon;
            }
        }
    }
    
    /// <summary>
    /// Detect UTM zone from a set of geographic coordinates.
    /// </summary>
    public static (int Zone, bool IsNorthern) DetectUtmZone(List<PosDataPoint> points)
    {
        var withGeo = points.Where(p => p.Latitude.HasValue && p.Longitude.HasValue).ToList();
        
        if (withGeo.Count == 0)
            return (32, true); // Default: UTM Zone 32N (North Sea)
        
        double avgLat = withGeo.Average(p => p.Latitude!.Value);
        double avgLon = withGeo.Average(p => p.Longitude!.Value);
        
        int zone = GetUtmZone(avgLon);
        bool northern = avgLat >= 0;
        
        return (zone, northern);
    }
    
    /// <summary>
    /// Calculate distance between two points (Haversine formula for geographic, Euclidean for projected).
    /// </summary>
    public static double CalculateDistance(
        double e1, double n1, 
        double e2, double n2, 
        bool isGeographic = false)
    {
        if (isGeographic)
        {
            return HaversineDistance(n1, e1, n2, e2); // lat1, lon1, lat2, lon2
        }
        else
        {
            return Math.Sqrt(Math.Pow(e2 - e1, 2) + Math.Pow(n2 - n1, 2));
        }
    }
    
    /// <summary>
    /// Calculate distance using Haversine formula (for geographic coordinates).
    /// </summary>
    public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000; // Earth's radius in meters
        
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);
        
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }
    
    // Conversion helpers
    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
}

/// <summary>
/// Extension methods for coordinate conversion on POS data points.
/// </summary>
public static class PosCoordinateExtensions
{
    /// <summary>
    /// Ensure point has both geographic and projected coordinates.
    /// </summary>
    public static void EnsureBothCoordinateSystems(
        this PosDataPoint point, 
        int utmZone, 
        bool northern,
        PosCoordinateSystem sourceSystem)
    {
        if (sourceSystem == PosCoordinateSystem.Geographic)
        {
            // Convert from geographic to projected
            if (point.Latitude.HasValue && point.Longitude.HasValue)
            {
                var (e, n) = CoordinateConverter.LatLonToUtm(
                    point.Latitude.Value, 
                    point.Longitude.Value, 
                    utmZone);
                point.Easting = e;
                point.Northing = n;
            }
        }
        else if (sourceSystem == PosCoordinateSystem.Projected)
        {
            // Convert from projected to geographic
            if (!double.IsNaN(point.Easting) && !double.IsNaN(point.Northing))
            {
                var (lat, lon) = CoordinateConverter.UtmToLatLon(
                    point.Easting, 
                    point.Northing, 
                    utmZone, 
                    northern);
                point.Latitude = lat;
                point.Longitude = lon;
            }
        }
    }
}
