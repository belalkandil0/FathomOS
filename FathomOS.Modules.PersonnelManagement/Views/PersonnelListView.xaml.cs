using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.Views;

/// <summary>
/// Personnel list view with search/filter capabilities
/// </summary>
public partial class PersonnelListView : UserControl
{
    private readonly PersonnelDatabaseService? _dbService;

    public PersonnelListView()
    {
        InitializeComponent();
    }

    public PersonnelListView(PersonnelDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        Loaded += PersonnelListView_Loaded;
    }

    private async void PersonnelListView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPersonnelAsync();
    }

    private async Task LoadPersonnelAsync()
    {
        if (_dbService == null) return;

        try
        {
            var personnelService = _dbService.GetPersonnelService();
            var personnel = await personnelService.GetAllPersonnelAsync();
            PersonnelGrid.ItemsSource = personnel;

            var totalCount = await personnelService.GetTotalPersonnelCountAsync();
            var offshoreCount = await personnelService.GetOffshorePersonnelCountAsync();
            StatusText.Text = $"{totalCount} personnel";
            OffshoreText.Text = $"{offshoreCount} offshore";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading personnel: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var detailView = new PersonnelDetailView(_dbService!, null);
        var window = new Window
        {
            Title = "Add Personnel",
            Content = detailView,
            Width = 800,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            _ = LoadPersonnelAsync();
        }
    }

    private void ViewButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedPersonnel(readOnly: true);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedPersonnel(readOnly: false);
    }

    private void PersonnelGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelectedPersonnel(readOnly: false);
    }

    private void OpenSelectedPersonnel(bool readOnly)
    {
        if (PersonnelGrid.SelectedItem is not Personnel selected)
        {
            MessageBox.Show("Please select a personnel record.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var detailView = new PersonnelDetailView(_dbService!, selected.PersonnelId);
        var window = new Window
        {
            Title = readOnly ? $"View: {selected.FullName}" : $"Edit: {selected.FullName}",
            Content = detailView,
            Width = 800,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            _ = LoadPersonnelAsync();
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Export functionality will be implemented.", "Export",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
