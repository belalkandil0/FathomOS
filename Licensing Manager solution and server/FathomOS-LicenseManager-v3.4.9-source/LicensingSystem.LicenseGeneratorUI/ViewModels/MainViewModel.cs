using CommunityToolkit.Mvvm.ComponentModel;

namespace LicenseGeneratorUI.ViewModels;

/// <summary>
/// Main view model - placeholder for future MVVM expansion
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "License Generator";

    [ObservableProperty]
    private string _statusMessage = "Ready";
}
