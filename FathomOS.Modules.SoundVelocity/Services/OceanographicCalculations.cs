using System;
using FathomOS.Modules.SoundVelocity.Models;

namespace FathomOS.Modules.SoundVelocity.Services;

/// <summary>
/// Oceanographic calculation formulas translated from VBA Formula.bas
/// </summary>
public static class OceanographicCalculations
{
    private const double Pi = Math.PI;

    #region Sound Velocity Formulas

    /// <summary>
    /// Chen-Millero Sound Velocity Formula (for depth ≤1000m)
    /// </summary>
    public static double ChenMillero(double pressure, double temperature, double salinity)
    {
        const double A00 = 1.389, A01 = -0.01262, A02 = 0.00007164, A03 = 0.000002006, A04 = -0.0000000321;
        const double A10 = 0.000094742, A11 = -0.00001258, A12 = -0.000000064885, A13 = 0.000000010507, A14 = -0.00000000020122;
        const double A20 = -0.00000039064, A21 = 0.0000000091041, A22 = -0.00000000016002, A23 = 0.000000000007988;
        const double A30 = 0.00000000011, A31 = 0.000000000006649, A32 = -3.389E-13;

        const double B00 = -0.01922, B01 = -0.0000442;
        const double B10 = 0.000073637, B11 = 0.00000017945;

        const double C00 = 1402.388, C01 = 5.03711, C02 = -0.0580852, C03 = 0.0003342, C04 = -0.000001478, C05 = 0.0000000031464;
        const double C10 = 0.153563, C11 = 0.00068982, C12 = -0.0000081788, C13 = 0.00000013621, C14 = -0.00000000061185;
        const double C20 = 0.00003126, C21 = -0.0000017107, C22 = 0.000000025974, C23 = -0.00000000025335, C24 = 1.0405E-12;
        const double C30 = -0.0000000097729, C31 = 0.00000000038504, C32 = -2.3643E-12;

        const double D00 = 0.001727, D10 = -0.0000079836;

        double P = pressure / 10;
        double T = temperature;
        double S = salinity;

        double T2 = T * T, T3 = T2 * T, T4 = T3 * T, T5 = T3 * T2;
        double P2 = P * P, P3 = P * P2;
        double S2 = S * S;
        double SRoot = Math.Sqrt(S);
        double S3by2 = SRoot * SRoot * SRoot;

        double Cwtp = C00 + (C01 * T) + (C02 * T2) + (C03 * T3) + (C04 * T4) + (C05 * T5)
                    + (C10 + (C11 * T) + (C12 * T2) + (C13 * T3) + (C14 * T4)) * P
                    + (C20 + (C21 * T) + (C22 * T2) + (C23 * T3) + (C24 * T4)) * P2
                    + (C30 + (C31 * T) + (C32 * T2)) * P3;

        double AtP = A00 + (A01 * T) + (A02 * T2) + (A03 * T3) + (A04 * T4)
                   + (A10 + (A11 * T) + (A12 * T2) + (A13 * T3) + (A14 * T4)) * P
                   + (A20 + (A21 * T) + (A22 * T2) + (A23 * T3)) * P2
                   + (A30 + (A31 * T) + (A32 * T2)) * P3;

        double Btp = B00 + (B01 * T) + ((B10 + (B11 * T)) * P);
        double dTP = D00 + (D10 * P);

        return Cwtp + (AtP * S) + (Btp * S3by2) + (dTP * S2);
    }

    /// <summary>
    /// Del Grosso Sound Velocity Formula (for depth >1000m)
    /// </summary>
    public static double DelGrosso(double pressure, double temperature, double salinity)
    {
        double T = temperature;
        double S = salinity;
        double P = pressure * 0.1019716;
        const double C0 = 1402.392;

        double DLTACT = 5.01109398873 * T - 0.0550946843172 * T * T + 0.00022153596924 * T * T * T;
        double DLTACS = 1.32952290781 * S + 0.000128955756844 * S * S;
        double DLTACP = 0.156059257041 * P + 0.000024499868841 * P * P - 8.83392332513E-09 * P * P * P;

        double DCSTP = -0.0127562783426 * T * S + 0.00635191613389 * T * P
                     + 2.65484716608E-08 * T * T * P * P - 1.59349479045E-06 * T * P * P
                     + 5.22116437235E-10 * T * P * P * P - 4.38031096213E-07 * T * T * T * P
                     - 1.61674495909E-09 * S * S * P * P + 0.000096840315641 * T * T * S
                     + 4.85639620015E-06 * T * S * S * P - 0.000340597039004 * T * S * P;

        return C0 + DLTACT + DLTACS + DLTACP + DCSTP;
    }

    #endregion

    #region Density Formulas

    /// <summary>
    /// UNESCO EOS-80 Density Formula
    /// </summary>
    public static double DensityEOS80(double pressure, double temperature, double salinity, bool returnAnomaly = true)
    {
        double PP = pressure / 10;
        double T = temperature, S = salinity;
        double T2 = T * T, T3 = T2 * T, T4 = T3 * T, T5 = T4 * T;

        double A = 999.842594 + 0.06793952 * T - 0.00909529 * T2 + 0.0001001685 * T3 - 0.000001120083 * T4 + 0.000000006536332 * T5;
        double B = 0.824493 - 0.0040899 * T + 0.000076438 * T2 - 0.00000082467 * T3 + 0.0000000053875 * T4;
        double C = -0.00572466 + 0.00010227 * T - 0.0000016546 * T2;
        double D = 0.00048314;
        double E = 19652.21 + 148.4206 * T - 2.327105 * T2 + 0.01360477 * T3 - 0.00005155288 * T4;
        double F = 54.6746 - 0.603459 * T + 0.0109987 * T2 - 0.00006167 * T3;
        double G = 0.07944 + 0.016483 * T - 0.00053009 * T2;
        double H = 3.239908 + 0.00143713 * T + 0.000116092 * T2 - 0.000000577905 * T3;
        double I = 0.0022838 - 0.000010981 * T - 0.0000016078 * T2;
        double J = 0.000191075;
        double M = 0.0000850935 - 0.00000612293 * T + 0.000000052787 * T2;
        double N = -0.00000099348 + 0.000000020816 * T + 0.00000000091697 * T2;

        double S15 = Math.Pow(S, 1.5), S20 = S * S;
        double P0 = A + B * S + C * S15 + D * S20;
        double K = E + F * S + G * S15 + (H + I * S + J * S15) * PP + (M + N * S) * PP * PP;
        double Denom = 1.0 - PP / K;

        return returnAnomaly ? P0 / Denom - 1000 : P0 / Denom / 1000;
    }

    #endregion

    #region Pressure/Depth Conversion

    /// <summary>
    /// Convert depth to pressure using Leroy &amp; Parthiot formula
    /// </summary>
    public static double DepthToPressure(double depth, double latitude, bool useLatitudeGravity = true)
    {
        double Z = depth, Z2 = Z * Z, Z3 = Z2 * Z, Z4 = Z3 * Z;
        double H = 0.0100818 * Z + 0.00000002465 * Z2 - 0.000000000000125 * Z3 + 2.8E-19 * Z4;

        double G = useLatitudeGravity 
            ? 9.7803 * (1 + 0.0053 * Math.Pow(Math.Sin(latitude * Pi / 180), 2))
            : 9.780318;

        double K = (G - 0.00002 * Z) / (9.80612 - 0.00002 * Z);
        return H * K * 100; // Convert to dbar
    }

    #endregion

    #region Salinity Conversion

    /// <summary>
    /// Convert Conductivity to Salinity using UNESCO 1981 formula
    /// </summary>
    public static double ConductivityToSalinity(double pressure, double temperature, double conductivity)
    {
        double P = pressure / 10, T = temperature, C = conductivity;
        double T2 = T * T, T3 = T2 * T, T4 = T3 * T;
        double R = C / 42.914;

        double Rt = 0.6766097 + 0.0200564 * T + 0.0001104259 * T2 - 0.00000069698 * T3 + 0.0000000010031 * T4;
        double Denom = 1 + 0.03426 * T + 0.0004464 * T2 + (0.4215 - 0.003107 * T) * R;
        double Rp = 1.0 + P * (0.0000207 - 0.000000000637 * P + 3.989E-15 * P * P) / Denom;

        Rt = R / Rp / Rt;
        double R1 = Math.Sqrt(Rt), R3 = Rt * R1, R4 = Rt * Rt, R5 = R4 * R1;
        double Ft = (T - 15.0) / (1.0 + 0.0162 * (T - 15.0));
        double DelS = Ft * (0.0005 - 0.0056 * R1 - 0.0066 * Rt - 0.0375 * R3 + 0.0636 * R4 - 0.0144 * R5);

        return 0.008 - 0.1692 * R1 + 25.3851 * Rt + 14.0941 * R3 - 7.0262 * R4 + 2.7081 * R5 + DelS;
    }

    #endregion

    #region Interpolation

    /// <summary>
    /// Linear interpolation
    /// </summary>
    public static double Interpolate(double y1, double y2, double x1, double x, double x2)
    {
        if (Math.Abs(x2 - x1) < 1e-10) return y1;
        return y1 + (y2 - y1) * (x - x1) / (x2 - x1);
    }

    #endregion

    #region Coordinate Parsing

    /// <summary>
    /// Parse coordinate string to decimal degrees
    /// </summary>
    public static double ParseCoordinate(string coordString, GeoCoordinateFormat format, out bool isValid)
    {
        isValid = false;
        if (string.IsNullOrWhiteSpace(coordString)) return 0;

        string trimmed = coordString.Trim().ToUpper();
        char direction = trimmed[^1];
        if (direction != 'N' && direction != 'S' && direction != 'E' && direction != 'W') return 0;

        string numPart = trimmed[..^1].Trim();
        double result = 0;

        try
        {
            switch (format)
            {
                case GeoCoordinateFormat.DMS:
                    var dmsParts = numPart.Split(';');
                    result = double.Parse(dmsParts[0]) + (dmsParts.Length > 1 ? double.Parse(dmsParts[1]) : 0) / 60 
                           + (dmsParts.Length > 2 ? double.Parse(dmsParts[2]) : 0) / 3600;
                    break;
                case GeoCoordinateFormat.DM:
                    var dmParts = numPart.Split(';');
                    result = double.Parse(dmParts[0]) + (dmParts.Length > 1 ? double.Parse(dmParts[1]) : 0) / 60;
                    break;
                case GeoCoordinateFormat.DD:
                    result = double.Parse(numPart);
                    break;
            }
            if (direction == 'S' || direction == 'W') result = -result;
            isValid = true;
        }
        catch { isValid = false; }

        return result;
    }

    #endregion
}
