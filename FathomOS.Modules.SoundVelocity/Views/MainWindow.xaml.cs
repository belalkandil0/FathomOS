using System;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.SoundVelocity.Services;
using FathomOS.Modules.SoundVelocity.ViewModels;

namespace FathomOS.Modules.SoundVelocity.Views;

public partial class MainWindow : MetroWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        // Load theme BEFORE InitializeComponent
        ThemeService.Instance.ApplyTheme(this);
        ThemeService.Instance.ThemeChanged += OnThemeChanged;
        
        InitializeComponent();
        
        _viewModel = new MainViewModel();
        _viewModel.RequestThemeToggle += OnRequestThemeToggle;
        DataContext = _viewModel;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ThemeService.Instance.ApplyTheme(this);
    }

    private void OnRequestThemeToggle(object? sender, EventArgs e)
    {
        ThemeService.Instance.ToggleTheme();
    }

    /// <summary>
    /// Load a file directly (called from module's OpenFile method)
    /// </summary>
    public void LoadFile(string filePath)
    {
        _viewModel.LoadFile(filePath);
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeService.Instance.ThemeChanged -= OnThemeChanged;
        _viewModel.RequestThemeToggle -= OnRequestThemeToggle;
        base.OnClosed(e);
    }
}
