using System;
using System.Collections.Generic;

namespace FathomOS.Modules.MruCalibration.Models
{
    /// <summary>
    /// Certificate request for MRU Calibration module.
    /// Ready for FathomOS.Core.Certificates integration.
    /// </summary>
    public class MruCertificateRequest
    {
        #region Identification (from ModuleInfo.json)
        
        /// <summary>
        /// Module ID - "MruCalibration"
        /// </summary>
        public string ModuleId { get; set; } = "MruCalibration";
        
        #endregion
        
        #region Project Info (user-provided)
        
        /// <summary>
        /// Name of the project/survey
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;
        
        /// <summary>
        /// Geographic location (optional)
        /// </summary>
        public string? ProjectLocation { get; set; }
        
        #endregion
        
        #region Optional Context
        
        /// <summary>
        /// Vessel or platform name (optional)
        /// </summary>
        public string? Vessel { get; set; }
        
        /// <summary>
        /// Client company name (optional)
        /// </summary>
        public string? Client { get; set; }
        
        /// <summary>
        /// MRU equipment name
        /// </summary>
        public string? Equipment { get; set; }
        
        /// <summary>
        /// Equipment serial number
        /// </summary>
        public string? EquipmentSerial { get; set; }
        
        #endregion
        
        #region Signatory
        
        /// <summary>
        /// Full name of person signing certificate
        /// </summary>
        public string SignatoryName { get; set; } = string.Empty;
        
        /// <summary>
        /// Professional title (e.g., "Survey Engineer", "Calibration Engineer")
        /// </summary>
        public string SignatoryTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional credentials (e.g., "BSc, MSc")
        /// </summary>
        public string? SignatoryCredentials { get; set; }
        
        #endregion
        
        #region Processing Data (MRU-specific)
        
        /// <summary>
        /// MRU calibration-specific processing data.
        /// Flexible dictionary of key-value pairs for certificate display.
        /// </summary>
        public Dictionary<string, string> ProcessingData { get; set; } = new();
        
        #endregion
        
        #region Files
        
        /// <summary>
        /// List of input files processed
        /// </summary>
        public List<CertificateFile> InputFiles { get; set; } = new();
        
        /// <summary>
        /// List of output files generated
        /// </summary>
        public List<CertificateFile> OutputFiles { get; set; } = new();
        
        #endregion
        
        #region Additional Conditions
        
        /// <summary>
        /// Additional validity conditions specific to MRU calibration
        /// </summary>
        public List<string> AdditionalConditions { get; set; } = new()
        {
            "Re-calibration recommended after any major system changes.",
            "Calibration validity: 12 months from issue date or after system modifications.",
            "Results valid only for the specified MRU equipment serial number."
        };
        
        #endregion
    }
    
    /// <summary>
    /// File reference for certificate audit trail.
    /// </summary>
    public class CertificateFile
    {
        /// <summary>
        /// File name (without path)
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional MD5 hash for integrity verification
        /// </summary>
        public string? FileHash { get; set; }
    }
    
    /// <summary>
    /// Helper class to build MRU-specific processing data for certificates.
    /// </summary>
    public static class MruCertificateDataBuilder
    {
        /// <summary>
        /// Builds the ProcessingData dictionary for MRU calibration certificate.
        /// </summary>
        public static Dictionary<string, string> BuildProcessingData(
            string calibrationType,
            CalibrationStatistics pitchStats,
            CalibrationStatistics rollStats,
            string? equipment = null,
            string? serialNumber = null)
        {
            var data = new Dictionary<string, string>
            {
                ["Calibration Type"] = calibrationType,
                ["Pitch C-O Value"] = FormatCO(pitchStats.MeanCO_Accepted),
                ["Pitch Std Dev"] = FormatStdDev(pitchStats.StdDevCO_Accepted),
                ["Pitch RMSE"] = FormatStdDev(pitchStats.RMSE),
                ["Pitch Points (Accepted/Total)"] = $"{pitchStats.AcceptedCount} / {pitchStats.TotalObservations}",
                ["Roll C-O Value"] = FormatCO(rollStats.MeanCO_Accepted),
                ["Roll Std Dev"] = FormatStdDev(rollStats.StdDevCO_Accepted),
                ["Roll RMSE"] = FormatStdDev(rollStats.RMSE),
                ["Roll Points (Accepted/Total)"] = $"{rollStats.AcceptedCount} / {rollStats.TotalObservations}",
                ["Statistical Method"] = "3-Sigma Outlier Rejection",
                ["Result"] = DetermineResult(pitchStats, rollStats)
            };
            
            if (!string.IsNullOrEmpty(equipment))
            {
                data["Equipment"] = equipment;
            }
            
            if (!string.IsNullOrEmpty(serialNumber))
            {
                data["Serial Number"] = serialNumber;
            }
            
            return data;
        }
        
        private static string FormatCO(double value)
        {
            string sign = value >= 0 ? "+" : "";
            return $"{sign}{value:F4}°";
        }
        
        private static string FormatStdDev(double value)
        {
            return $"{value:F4}°";
        }
        
        private static string DetermineResult(CalibrationStatistics pitchStats, CalibrationStatistics rollStats)
        {
            // Typical acceptance criteria for MRU calibration
            bool pitchOk = pitchStats.StdDevCO_Accepted < 0.05 && pitchStats.RejectionPercentage < 10;
            bool rollOk = rollStats.StdDevCO_Accepted < 0.05 && rollStats.RejectionPercentage < 10;
            
            if (pitchOk && rollOk)
                return "PASS - Within Tolerance";
            else if (!pitchOk && !rollOk)
                return "REVIEW REQUIRED - Pitch and Roll exceed tolerances";
            else if (!pitchOk)
                return "REVIEW REQUIRED - Pitch exceeds tolerance";
            else
                return "REVIEW REQUIRED - Roll exceeds tolerance";
        }
    }
}
