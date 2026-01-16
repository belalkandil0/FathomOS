using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using FathomOS.Core.Models;
using FathomOS.Modules.SurveyListing.Services;
using MessageBox = System.Windows.MessageBox;

namespace FathomOS.Modules.SurveyListing.ViewModels;

/// <summary>
/// ViewModel for Step 1: Project Setup
/// </summary>
public class Step1ViewModel : INotifyPropertyChanged
{
    private string _projectName = string.Empty;
    private string _clientName = string.Empty;
    private string _vesselName = string.Empty;
    private string _processorName = string.Empty;
    private string _productName = string.Empty;
    private string _rovName = string.Empty;
    private DateTime _surveyDate = DateTime.Today;
    private SurveyType _surveyType = SurveyType.Seabed;
    private string _coordinateSystem = string.Empty;
    private LengthUnit _coordinateUnit = LengthUnit.USSurveyFeet;
    private LengthUnit _inputUnit = LengthUnit.USSurveyFeet;
    private LengthUnit _outputUnit = LengthUnit.USSurveyFeet;
    private LengthUnit _kpUnit = LengthUnit.Kilometer;
    private KpDccMode _kpDccMode = KpDccMode.Both;
    
    // Coordinate Conversion
    private bool _enableCoordinateConversion = false;
    private string _sourceCoordinateSystem = string.Empty;
    private string _targetCoordinateSystem = string.Empty;

    public Step1ViewModel(Project project)
    {
        LoadProject(project);
        
        // Initialize dropdown options
        SurveyTypes = new ObservableCollection<SurveyType> 
        { 
            SurveyType.Pipelay,
            SurveyType.EFL,
            SurveyType.SFL,
            SurveyType.Umbilical,
            SurveyType.Cable,
            SurveyType.Touchdown,
            SurveyType.AsBuilt,
            SurveyType.PreLay,
            SurveyType.PostLay,
            SurveyType.FreeSpan,
            SurveyType.Inspection,
            SurveyType.Seabed, 
            SurveyType.RovDynamic,
            SurveyType.Custom
        };

        CoordinateUnits = new ObservableCollection<LengthUnit>
        {
            LengthUnit.USSurveyFeet,
            LengthUnit.Meter,
            LengthUnit.InternationalFeet,
            LengthUnit.Kilometer,
            LengthUnit.Yard,
            LengthUnit.NauticalMile
        };

        KpUnits = new ObservableCollection<LengthUnit>
        {
            LengthUnit.Kilometer,
            LengthUnit.Meter,
            LengthUnit.USSurveyFeet,
            LengthUnit.NauticalMile
        };
        
        KpDccModes = new ObservableCollection<KpDccMode>
        {
            KpDccMode.Both,
            KpDccMode.KpOnly,
            KpDccMode.DccOnly,
            KpDccMode.None
        };

        CoordinateSystems = new ObservableCollection<string>
        {
            // --- GULF OF MEXICO / US OFFSHORE ---
            "NAD83 / Louisiana South (ftUS) - EPSG:3452",
            "NAD83 / Louisiana Offshore (ftUS) - EPSG:3453",
            "NAD83 / Texas South (ftUS) - EPSG:2279",
            "NAD83 / Texas South Central (ftUS) - EPSG:2278",
            "NAD83 / Texas Central (ftUS) - EPSG:2277",
            "NAD27 / Louisiana South - EPSG:32019",
            "NAD27 / Louisiana Offshore - EPSG:32020",
            "NAD27 / Texas South - EPSG:32041",
            "NAD27 / Texas South Central - EPSG:32040",
            
            // --- UTM ZONES - WGS84 (Northern Hemisphere) ---
            "WGS 84 / UTM zone 1N - EPSG:32601",
            "WGS 84 / UTM zone 2N - EPSG:32602",
            "WGS 84 / UTM zone 3N - EPSG:32603",
            "WGS 84 / UTM zone 4N - EPSG:32604",
            "WGS 84 / UTM zone 5N - EPSG:32605",
            "WGS 84 / UTM zone 6N - EPSG:32606",
            "WGS 84 / UTM zone 7N - EPSG:32607",
            "WGS 84 / UTM zone 8N - EPSG:32608",
            "WGS 84 / UTM zone 9N - EPSG:32609",
            "WGS 84 / UTM zone 10N - EPSG:32610",
            "WGS 84 / UTM zone 11N - EPSG:32611",
            "WGS 84 / UTM zone 12N - EPSG:32612",
            "WGS 84 / UTM zone 13N - EPSG:32613",
            "WGS 84 / UTM zone 14N - EPSG:32614",
            "WGS 84 / UTM zone 15N - EPSG:32615",
            "WGS 84 / UTM zone 16N - EPSG:32616",
            "WGS 84 / UTM zone 17N - EPSG:32617",
            "WGS 84 / UTM zone 18N - EPSG:32618",
            "WGS 84 / UTM zone 19N - EPSG:32619",
            "WGS 84 / UTM zone 20N - EPSG:32620",
            "WGS 84 / UTM zone 21N - EPSG:32621",
            "WGS 84 / UTM zone 22N - EPSG:32622",
            "WGS 84 / UTM zone 23N - EPSG:32623",
            "WGS 84 / UTM zone 24N - EPSG:32624",
            "WGS 84 / UTM zone 25N - EPSG:32625",
            "WGS 84 / UTM zone 26N - EPSG:32626",
            "WGS 84 / UTM zone 27N - EPSG:32627",
            "WGS 84 / UTM zone 28N - EPSG:32628",
            "WGS 84 / UTM zone 29N - EPSG:32629",
            "WGS 84 / UTM zone 30N - EPSG:32630",
            "WGS 84 / UTM zone 31N - EPSG:32631",
            "WGS 84 / UTM zone 32N - EPSG:32632",
            "WGS 84 / UTM zone 33N - EPSG:32633",
            "WGS 84 / UTM zone 34N - EPSG:32634",
            "WGS 84 / UTM zone 35N - EPSG:32635",
            "WGS 84 / UTM zone 36N - EPSG:32636",
            "WGS 84 / UTM zone 37N - EPSG:32637",
            "WGS 84 / UTM zone 38N - EPSG:32638",
            "WGS 84 / UTM zone 39N - EPSG:32639",
            "WGS 84 / UTM zone 40N - EPSG:32640",
            "WGS 84 / UTM zone 41N - EPSG:32641",
            "WGS 84 / UTM zone 42N - EPSG:32642",
            "WGS 84 / UTM zone 43N - EPSG:32643",
            "WGS 84 / UTM zone 44N - EPSG:32644",
            "WGS 84 / UTM zone 45N - EPSG:32645",
            "WGS 84 / UTM zone 46N - EPSG:32646",
            "WGS 84 / UTM zone 47N - EPSG:32647",
            "WGS 84 / UTM zone 48N - EPSG:32648",
            "WGS 84 / UTM zone 49N - EPSG:32649",
            "WGS 84 / UTM zone 50N - EPSG:32650",
            "WGS 84 / UTM zone 51N - EPSG:32651",
            "WGS 84 / UTM zone 52N - EPSG:32652",
            "WGS 84 / UTM zone 53N - EPSG:32653",
            "WGS 84 / UTM zone 54N - EPSG:32654",
            "WGS 84 / UTM zone 55N - EPSG:32655",
            "WGS 84 / UTM zone 56N - EPSG:32656",
            "WGS 84 / UTM zone 57N - EPSG:32657",
            "WGS 84 / UTM zone 58N - EPSG:32658",
            "WGS 84 / UTM zone 59N - EPSG:32659",
            "WGS 84 / UTM zone 60N - EPSG:32660",
            
            // --- UTM ZONES - WGS84 (Southern Hemisphere) ---
            "WGS 84 / UTM zone 1S - EPSG:32701",
            "WGS 84 / UTM zone 2S - EPSG:32702",
            "WGS 84 / UTM zone 3S - EPSG:32703",
            "WGS 84 / UTM zone 4S - EPSG:32704",
            "WGS 84 / UTM zone 5S - EPSG:32705",
            "WGS 84 / UTM zone 6S - EPSG:32706",
            "WGS 84 / UTM zone 7S - EPSG:32707",
            "WGS 84 / UTM zone 8S - EPSG:32708",
            "WGS 84 / UTM zone 9S - EPSG:32709",
            "WGS 84 / UTM zone 10S - EPSG:32710",
            "WGS 84 / UTM zone 11S - EPSG:32711",
            "WGS 84 / UTM zone 12S - EPSG:32712",
            "WGS 84 / UTM zone 13S - EPSG:32713",
            "WGS 84 / UTM zone 14S - EPSG:32714",
            "WGS 84 / UTM zone 15S - EPSG:32715",
            "WGS 84 / UTM zone 16S - EPSG:32716",
            "WGS 84 / UTM zone 17S - EPSG:32717",
            "WGS 84 / UTM zone 18S - EPSG:32718",
            "WGS 84 / UTM zone 19S - EPSG:32719",
            "WGS 84 / UTM zone 20S - EPSG:32720",
            "WGS 84 / UTM zone 21S - EPSG:32721",
            "WGS 84 / UTM zone 22S - EPSG:32722",
            "WGS 84 / UTM zone 23S - EPSG:32723",
            "WGS 84 / UTM zone 24S - EPSG:32724",
            "WGS 84 / UTM zone 25S - EPSG:32725",
            "WGS 84 / UTM zone 26S - EPSG:32726",
            "WGS 84 / UTM zone 27S - EPSG:32727",
            "WGS 84 / UTM zone 28S - EPSG:32728",
            "WGS 84 / UTM zone 29S - EPSG:32729",
            "WGS 84 / UTM zone 30S - EPSG:32730",
            "WGS 84 / UTM zone 31S - EPSG:32731",
            "WGS 84 / UTM zone 32S - EPSG:32732",
            "WGS 84 / UTM zone 33S - EPSG:32733",
            "WGS 84 / UTM zone 34S - EPSG:32734",
            "WGS 84 / UTM zone 35S - EPSG:32735",
            "WGS 84 / UTM zone 36S - EPSG:32736",
            "WGS 84 / UTM zone 37S - EPSG:32737",
            "WGS 84 / UTM zone 38S - EPSG:32738",
            "WGS 84 / UTM zone 39S - EPSG:32739",
            "WGS 84 / UTM zone 40S - EPSG:32740",
            "WGS 84 / UTM zone 41S - EPSG:32741",
            "WGS 84 / UTM zone 42S - EPSG:32742",
            "WGS 84 / UTM zone 43S - EPSG:32743",
            "WGS 84 / UTM zone 44S - EPSG:32744",
            "WGS 84 / UTM zone 45S - EPSG:32745",
            "WGS 84 / UTM zone 46S - EPSG:32746",
            "WGS 84 / UTM zone 47S - EPSG:32747",
            "WGS 84 / UTM zone 48S - EPSG:32748",
            "WGS 84 / UTM zone 49S - EPSG:32749",
            "WGS 84 / UTM zone 50S - EPSG:32750",
            "WGS 84 / UTM zone 51S - EPSG:32751",
            "WGS 84 / UTM zone 52S - EPSG:32752",
            "WGS 84 / UTM zone 53S - EPSG:32753",
            "WGS 84 / UTM zone 54S - EPSG:32754",
            "WGS 84 / UTM zone 55S - EPSG:32755",
            "WGS 84 / UTM zone 56S - EPSG:32756",
            "WGS 84 / UTM zone 57S - EPSG:32757",
            "WGS 84 / UTM zone 58S - EPSG:32758",
            "WGS 84 / UTM zone 59S - EPSG:32759",
            "WGS 84 / UTM zone 60S - EPSG:32760",
            
            // --- UTM ZONES - NAD83 ---
            "NAD83 / UTM zone 10N - EPSG:26910",
            "NAD83 / UTM zone 11N - EPSG:26911",
            "NAD83 / UTM zone 12N - EPSG:26912",
            "NAD83 / UTM zone 13N - EPSG:26913",
            "NAD83 / UTM zone 14N - EPSG:26914",
            "NAD83 / UTM zone 15N - EPSG:26915",
            "NAD83 / UTM zone 16N - EPSG:26916",
            "NAD83 / UTM zone 17N - EPSG:26917",
            "NAD83 / UTM zone 18N - EPSG:26918",
            "NAD83 / UTM zone 19N - EPSG:26919",
            "NAD83 / UTM zone 20N - EPSG:26920",
            
            // --- NORTH SEA / EUROPE ---
            "ED50 / UTM zone 29N - EPSG:23029",
            "ED50 / UTM zone 30N - EPSG:23030",
            "ED50 / UTM zone 31N - EPSG:23031",
            "ED50 / UTM zone 32N - EPSG:23032",
            "ETRS89 / UTM zone 29N - EPSG:25829",
            "ETRS89 / UTM zone 30N - EPSG:25830",
            "ETRS89 / UTM zone 31N - EPSG:25831",
            "ETRS89 / UTM zone 32N - EPSG:25832",
            
            // --- BRAZIL OFFSHORE ---
            "SIRGAS 2000 / UTM zone 21S - EPSG:31981",
            "SIRGAS 2000 / UTM zone 22S - EPSG:31982",
            "SIRGAS 2000 / UTM zone 23S - EPSG:31983",
            "SIRGAS 2000 / UTM zone 24S - EPSG:31984",
            "SIRGAS 2000 / UTM zone 25S - EPSG:31985",
            
            // --- WEST AFRICA ---
            "WGS 84 / TM 6 NE - EPSG:2312",
            "Minna / UTM zone 31N - EPSG:26331",
            "Minna / UTM zone 32N - EPSG:26332",
            
            // --- AUSTRALIA / ASIA-PACIFIC ---
            "GDA94 / MGA zone 49 - EPSG:28349",
            "GDA94 / MGA zone 50 - EPSG:28350",
            "GDA94 / MGA zone 51 - EPSG:28351",
            "GDA94 / MGA zone 52 - EPSG:28352",
            "GDA94 / MGA zone 53 - EPSG:28353",
            "GDA94 / MGA zone 54 - EPSG:28354",
            "GDA94 / MGA zone 55 - EPSG:28355",
            "GDA94 / MGA zone 56 - EPSG:28356",
            
            // --- MIDDLE EAST ---
            "WGS 84 / UTM zone 38N - EPSG:32638",
            "WGS 84 / UTM zone 39N - EPSG:32639",
            "WGS 84 / UTM zone 40N - EPSG:32640",
            "Ain el Abd / UTM zone 37N - EPSG:20437",
            "Ain el Abd / UTM zone 38N - EPSG:20438",
            "Ain el Abd / UTM zone 39N - EPSG:20439",
            
            // --- GEOGRAPHIC (Lat/Long) ---
            "WGS 84 (Geographic) - EPSG:4326",
            "NAD83 (Geographic) - EPSG:4269",
            "NAD27 (Geographic) - EPSG:4267",
            
            // --- OTHER ---
            "Custom / Project-specific CRS"
        };
    }

    // Dropdown collections
    public ObservableCollection<SurveyType> SurveyTypes { get; }
    public ObservableCollection<LengthUnit> CoordinateUnits { get; }
    public ObservableCollection<LengthUnit> KpUnits { get; }
    public ObservableCollection<string> CoordinateSystems { get; }
    public ObservableCollection<KpDccMode> KpDccModes { get; }

    // Properties
    public string ProjectName
    {
        get => _projectName;
        set { _projectName = value; OnPropertyChanged(); }
    }

    public string ClientName
    {
        get => _clientName;
        set { _clientName = value; OnPropertyChanged(); }
    }

    public string VesselName
    {
        get => _vesselName;
        set { _vesselName = value; OnPropertyChanged(); }
    }

    public string ProcessorName
    {
        get => _processorName;
        set { _processorName = value; OnPropertyChanged(); }
    }
    
    public string ProductName
    {
        get => _productName;
        set { _productName = value; OnPropertyChanged(); }
    }
    
    public string RovName
    {
        get => _rovName;
        set { _rovName = value; OnPropertyChanged(); }
    }
    
    public DateTime SurveyDate
    {
        get => _surveyDate;
        set { _surveyDate = value; OnPropertyChanged(); }
    }

    public SurveyType SelectedSurveyType
    {
        get => _surveyType;
        set 
        { 
            _surveyType = value; 
            OnPropertyChanged();
            // Update depth exaggeration default based on survey type
            OnPropertyChanged(nameof(DefaultDepthExaggeration));
        }
    }

    public string SelectedCoordinateSystem
    {
        get => _coordinateSystem;
        set { _coordinateSystem = value; OnPropertyChanged(); }
    }

    public LengthUnit SelectedCoordinateUnit
    {
        get => _coordinateUnit;
        set { _coordinateUnit = value; OnPropertyChanged(); }
    }
    
    public LengthUnit SelectedInputUnit
    {
        get => _inputUnit;
        set { _inputUnit = value; OnPropertyChanged(); }
    }
    
    public LengthUnit SelectedOutputUnit
    {
        get => _outputUnit;
        set { _outputUnit = value; OnPropertyChanged(); }
    }

    public LengthUnit SelectedKpUnit
    {
        get => _kpUnit;
        set { _kpUnit = value; OnPropertyChanged(); }
    }
    
    // Coordinate Conversion Properties
    public bool EnableCoordinateConversion
    {
        get => _enableCoordinateConversion;
        set 
        { 
            _enableCoordinateConversion = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConversionPanelVisibility));
        }
    }
    
    public string SourceCoordinateSystem
    {
        get => _sourceCoordinateSystem;
        set { _sourceCoordinateSystem = value; OnPropertyChanged(); }
    }
    
    public string TargetCoordinateSystem
    {
        get => _targetCoordinateSystem;
        set { _targetCoordinateSystem = value; OnPropertyChanged(); }
    }
    
    public Visibility ConversionPanelVisibility => 
        EnableCoordinateConversion ? Visibility.Visible : Visibility.Collapsed;
    
    public KpDccMode SelectedKpDccMode
    {
        get => _kpDccMode;
        set 
        { 
            if (_kpDccMode != value)
            {
                _kpDccMode = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(RequiresRouteFile));
                OnPropertyChanged(nameof(KpDccModeDescription));
            }
        }
    }
    
    public bool RequiresRouteFile => _kpDccMode != KpDccMode.None;
    
    public string KpDccModeDescription => _kpDccMode switch
    {
        KpDccMode.Both => "Calculate both KP and DCC (requires route file)",
        KpDccMode.KpOnly => "Calculate KP only (requires route file)",
        KpDccMode.DccOnly => "Calculate DCC only (requires route file)",
        KpDccMode.None => "Skip KP/DCC calculation (no route file needed)",
        _ => ""
    };

    public double DefaultDepthExaggeration => 
        _surveyType == SurveyType.Seabed ? 10.0 : 1.0;

    public void LoadProject(Project project)
    {
        ProjectName = project.ProjectName;
        ClientName = project.ClientName;
        VesselName = project.VesselName;
        ProcessorName = project.ProcessorName;
        ProductName = project.ProductName ?? string.Empty;
        RovName = project.RovName ?? string.Empty;
        SurveyDate = project.SurveyDate ?? DateTime.Today;
        SelectedSurveyType = project.SurveyType;
        SelectedCoordinateSystem = project.CoordinateSystem;
        SelectedCoordinateUnit = project.CoordinateUnit;
        SelectedInputUnit = project.InputUnit;
        SelectedOutputUnit = project.OutputUnit;
        SelectedKpUnit = project.KpUnit;
        SelectedKpDccMode = project.ProcessingOptions.KpDccMode;
        
        // Load coordinate conversion settings
        EnableCoordinateConversion = project.CoordinateConversion.EnableConversion;
        SourceCoordinateSystem = project.CoordinateConversion.SourceZone;
        TargetCoordinateSystem = project.CoordinateConversion.TargetZone;
    }

    public void SaveToProject(Project project)
    {
        project.ProjectName = ProjectName;
        project.ClientName = ClientName;
        project.VesselName = VesselName;
        project.ProcessorName = ProcessorName;
        project.ProductName = ProductName;
        project.RovName = RovName;
        project.SurveyDate = SurveyDate;
        project.SurveyType = SelectedSurveyType;
        project.CoordinateSystem = SelectedCoordinateSystem;
        project.CoordinateUnit = SelectedCoordinateUnit;
        project.InputUnit = SelectedInputUnit;
        project.OutputUnit = SelectedOutputUnit;
        project.KpUnit = SelectedKpUnit;
        project.ProcessingOptions.KpDccMode = SelectedKpDccMode;
        
        // Save coordinate conversion settings
        project.CoordinateConversion.EnableConversion = EnableCoordinateConversion;
        project.CoordinateConversion.SourceZone = SourceCoordinateSystem;
        project.CoordinateConversion.TargetZone = TargetCoordinateSystem;
        
        // Set default depth exaggeration based on survey type
        project.ProcessingOptions.DepthExaggeration = DefaultDepthExaggeration;
    }

    public bool Validate()
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(ProjectName))
            issues.Add("Project name is required");

        if (string.IsNullOrWhiteSpace(SelectedCoordinateSystem))
            issues.Add("Coordinate system must be selected");
            
        if (EnableCoordinateConversion)
        {
            if (string.IsNullOrWhiteSpace(SourceCoordinateSystem))
                issues.Add("Source coordinate system is required for conversion");
            if (string.IsNullOrWhiteSpace(TargetCoordinateSystem))
                issues.Add("Target coordinate system is required for conversion");
        }

        if (issues.Count > 0)
        {
            DialogService.Instance.ShowWarning(
                "Validation",
                "Please correct the following:\n\n" + string.Join("\n", issues));
            return false;
        }

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
