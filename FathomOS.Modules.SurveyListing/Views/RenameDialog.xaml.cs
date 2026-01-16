using System.Windows;
using MahApps.Metro.Controls;

namespace FathomOS.Modules.SurveyListing.Views;

public partial class RenameDialog : MetroWindow
{
    public string NewName { get; private set; } = string.Empty;
    
    public RenameDialog(string currentName)
    {
        InitializeComponent();
        TxtName.Text = currentName;
        TxtName.SelectAll();
        TxtName.Focus();
    }
    
    public RenameDialog(string currentName, string title, string message) : this(currentName)
    {
        Title = title;
        if (TxtMessage != null)
        {
            TxtMessage.Text = message;
            TxtMessage.Visibility = Visibility.Visible;
        }
    }
    
    private void OK_Click(object sender, RoutedEventArgs e)
    {
        NewName = TxtName.Text.Trim();
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
