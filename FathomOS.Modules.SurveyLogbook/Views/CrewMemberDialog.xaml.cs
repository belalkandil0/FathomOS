// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Views/CrewMemberDialog.xaml.cs
// Purpose: Dialog for adding/editing crew members in DPR
// ============================================================================

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Dialog for adding or editing a crew member.
/// </summary>
public partial class CrewMemberDialog : MetroWindow, INotifyPropertyChanged
{
    #region Fields
    
    private string _name = string.Empty;
    private string _rank = "Surveyor";
    private string _shift = "Day";
    private string _employer = string.Empty;
    private DateTime _dateOnBoard = DateTime.Today;
    
    #endregion
    
    #region Properties

    public string CrewMemberName
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Rank
    {
        get => _rank;
        set { _rank = value; OnPropertyChanged(); }
    }

    public string Shift
    {
        get => _shift;
        set { _shift = value; OnPropertyChanged(); }
    }

    public string Employer
    {
        get => _employer;
        set { _employer = value; OnPropertyChanged(); }
    }

    public DateTime DateOnBoard
    {
        get => _dateOnBoard;
        set { _dateOnBoard = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets the created crew member after dialog closes with OK result.
    /// </summary>
    public CrewMember? CreatedCrewMember { get; private set; }

    #endregion

    #region Constructor

    public CrewMemberDialog()
    {
        // Load theme before InitializeComponent
        var themeUri = new Uri("/FathomOS.Modules.SurveyLogbook;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

        InitializeComponent();
        DataContext = this;

        // Focus on name field
        Loaded += (s, e) => NameTextBox.Focus();
    }

    /// <summary>
    /// Creates a dialog pre-populated with an existing crew member for editing.
    /// </summary>
    public CrewMemberDialog(CrewMember existingMember) : this()
    {
        Title = "Edit Crew Member";
        _name = existingMember.Name;
        _rank = existingMember.Rank;
        _shift = existingMember.Shift;
        _employer = existingMember.Employer;
        _dateOnBoard = existingMember.DateOnBoard;
        
        // Update button text
        if (AddButton.Content is System.Windows.Controls.StackPanel sp && 
            sp.Children.Count > 1 && 
            sp.Children[1] is System.Windows.Controls.TextBlock tb)
        {
            tb.Text = "Save";
        }
    }

    #endregion

    #region Event Handlers

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(_name))
        {
            System.Windows.MessageBox.Show("Please enter a name for the crew member.",
                "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        // Create the crew member
        CreatedCrewMember = new CrewMember
        {
            Name = _name.Trim(),
            Rank = _rank ?? "Surveyor",
            Shift = _shift ?? "Day",
            Employer = _employer?.Trim() ?? string.Empty,
            DateOnBoard = _dateOnBoard
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
