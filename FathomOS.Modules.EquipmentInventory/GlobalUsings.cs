// Global using directives to resolve namespace conflicts
// between WPF and WinForms when both are enabled

global using UserControl = System.Windows.Controls.UserControl;
global using MessageBox = System.Windows.MessageBox;
global using Application = System.Windows.Application;
global using Window = System.Windows.Window;
global using Button = System.Windows.Controls.Button;
global using TextBox = System.Windows.Controls.TextBox;
global using Label = System.Windows.Controls.Label;
global using ComboBox = System.Windows.Controls.ComboBox;
global using CheckBox = System.Windows.Controls.CheckBox;
global using ListBox = System.Windows.Controls.ListBox;
global using Panel = System.Windows.Controls.Panel;
global using RadioButton = System.Windows.Controls.RadioButton;
global using ContextMenu = System.Windows.Controls.ContextMenu;
global using MenuItem = System.Windows.Controls.MenuItem;
global using Separator = System.Windows.Controls.Separator;

// File dialogs - use WPF versions from Microsoft.Win32
global using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
global using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

// WPF core types
global using ResourceDictionary = System.Windows.ResourceDictionary;
