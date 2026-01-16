using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MahApps.Metro.Controls;

namespace FathomOS.Modules.MruCalibration.Views
{
    /// <summary>
    /// Dialog for collecting signatory information for certificate generation.
    /// </summary>
    public partial class SignatoryDialog : MetroWindow, INotifyPropertyChanged
    {
        #region Fields
        
        private string _projectName = string.Empty;
        private string _projectLocation = string.Empty;
        private string _vessel = string.Empty;
        private string _equipment = string.Empty;
        private string _equipmentSerial = string.Empty;
        private string _signatoryName = string.Empty;
        private string _signatoryTitle = string.Empty;
        private string _signatoryCredentials = string.Empty;
        
        #endregion
        
        #region Constructor
        
        public SignatoryDialog()
        {
            // Load theme before InitializeComponent
            LoadTheme();
            
            InitializeComponent();
            DataContext = this;
        }
        
        public SignatoryDialog(string projectName, string vessel) : this()
        {
            ProjectName = projectName;
            Vessel = vessel;
        }
        
        #endregion
        
        #region Properties
        
        public string ProjectName
        {
            get => _projectName;
            set { _projectName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGenerate)); }
        }
        
        public string ProjectLocation
        {
            get => _projectLocation;
            set { _projectLocation = value; OnPropertyChanged(); }
        }
        
        public string Vessel
        {
            get => _vessel;
            set { _vessel = value; OnPropertyChanged(); }
        }
        
        public string Equipment
        {
            get => _equipment;
            set { _equipment = value; OnPropertyChanged(); }
        }
        
        public string EquipmentSerial
        {
            get => _equipmentSerial;
            set { _equipmentSerial = value; OnPropertyChanged(); }
        }
        
        public string SignatoryName
        {
            get => _signatoryName;
            set { _signatoryName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGenerate)); }
        }
        
        public string SignatoryTitle
        {
            get => _signatoryTitle;
            set { _signatoryTitle = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGenerate)); }
        }
        
        public string SignatoryCredentials
        {
            get => _signatoryCredentials;
            set { _signatoryCredentials = value; OnPropertyChanged(); }
        }
        
        public bool CanGenerate => 
            !string.IsNullOrWhiteSpace(ProjectName) &&
            !string.IsNullOrWhiteSpace(SignatoryName) &&
            !string.IsNullOrWhiteSpace(SignatoryTitle);
        
        /// <summary>
        /// Result containing all signatory information when dialog closes with OK.
        /// </summary>
        public SignatoryInfo? Result { get; private set; }
        
        #endregion
        
        #region Event Handlers
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanGenerate)
            {
                System.Windows.MessageBox.Show(
                    "Please fill in all required fields:\n• Project Name\n• Signatory Name\n• Professional Title",
                    "Missing Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            Result = new SignatoryInfo
            {
                ProjectName = ProjectName,
                ProjectLocation = ProjectLocation,
                Vessel = Vessel,
                Equipment = Equipment,
                EquipmentSerial = EquipmentSerial,
                SignatoryName = SignatoryName,
                SignatoryTitle = SignatoryTitle,
                SignatoryCredentials = SignatoryCredentials
            };
            
            DialogResult = true;
            Close();
        }
        
        #endregion
        
        #region Theme Support
        
        private void LoadTheme()
        {
            try
            {
                var themeUri = new Uri("/FathomOS.Modules.MruCalibration;component/Themes/Subsea7DarkTheme.xaml", UriKind.Relative);
                Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load theme: {ex.Message}");
            }
        }
        
        #endregion
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
    
    /// <summary>
    /// Contains signatory and project information for certificate generation.
    /// </summary>
    public class SignatoryInfo
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectLocation { get; set; } = string.Empty;
        public string Vessel { get; set; } = string.Empty;
        public string Equipment { get; set; } = string.Empty;
        public string EquipmentSerial { get; set; } = string.Empty;
        public string SignatoryName { get; set; } = string.Empty;
        public string SignatoryTitle { get; set; } = string.Empty;
        public string SignatoryCredentials { get; set; } = string.Empty;
    }
}
