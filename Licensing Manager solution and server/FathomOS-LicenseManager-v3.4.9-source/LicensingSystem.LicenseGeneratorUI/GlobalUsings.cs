// Global using directives to resolve WPF vs WinForms conflicts
global using Window = System.Windows.Window;
global using MessageBox = System.Windows.MessageBox;
global using MessageBoxButton = System.Windows.MessageBoxButton;
global using MessageBoxImage = System.Windows.MessageBoxImage;
global using MessageBoxResult = System.Windows.MessageBoxResult;
global using Clipboard = System.Windows.Clipboard;
global using Application = System.Windows.Application;

// Controls - use WPF versions
global using RadioButton = System.Windows.Controls.RadioButton;
global using Button = System.Windows.Controls.Button;
global using TextBox = System.Windows.Controls.TextBox;
global using ComboBox = System.Windows.Controls.ComboBox;
global using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
global using Label = System.Windows.Controls.Label;
global using CheckBox = System.Windows.Controls.CheckBox;

// Dialogs - use Microsoft.Win32 versions (WPF-compatible)
global using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
global using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

// Visibility and Media
global using Visibility = System.Windows.Visibility;
global using Color = System.Windows.Media.Color;
global using Colors = System.Windows.Media.Colors;
global using ColorConverter = System.Windows.Media.ColorConverter;
global using SolidColorBrush = System.Windows.Media.SolidColorBrush;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;

// Layout and enums - resolve WPF vs WinForms conflicts
global using Orientation = System.Windows.Controls.Orientation;
global using CharacterCasing = System.Windows.Controls.CharacterCasing;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
global using VerticalAlignment = System.Windows.VerticalAlignment;
