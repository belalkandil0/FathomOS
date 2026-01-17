using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Theme Toggle Button
    /// A toggle button for switching between light and dark themes.
    /// Owned by: UI-AGENT
    /// </summary>
    public class ThemeToggleButton : ToggleButton
    {
        static ThemeToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ThemeToggleButton),
                new FrameworkPropertyMetadata(typeof(ThemeToggleButton)));
        }

        #region Events

        /// <summary>
        /// Occurs when the theme is changed.
        /// </summary>
        public static readonly RoutedEvent ThemeChangedEvent =
            EventManager.RegisterRoutedEvent(
                "ThemeChanged",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ThemeToggleButton));

        public event RoutedEventHandler ThemeChanged
        {
            add => AddHandler(ThemeChangedEvent, value);
            remove => RemoveHandler(ThemeChangedEvent, value);
        }

        #endregion

        #region IsDarkTheme Property

        public static readonly DependencyProperty IsDarkThemeProperty =
            DependencyProperty.Register(
                nameof(IsDarkTheme),
                typeof(bool),
                typeof(ThemeToggleButton),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsDarkThemeChanged));

        /// <summary>
        /// Gets or sets whether dark theme is active.
        /// </summary>
        public bool IsDarkTheme
        {
            get => (bool)GetValue(IsDarkThemeProperty);
            set => SetValue(IsDarkThemeProperty, value);
        }

        private static void OnIsDarkThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThemeToggleButton button)
            {
                button.IsChecked = (bool)e.NewValue;
                button.RaiseEvent(new RoutedEventArgs(ThemeChangedEvent, button));
            }
        }

        #endregion

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ThemeToggleVariant),
                typeof(ThemeToggleButton),
                new PropertyMetadata(ThemeToggleVariant.IconOnly));

        /// <summary>
        /// Gets or sets the visual variant of the toggle.
        /// </summary>
        public ThemeToggleVariant Variant
        {
            get => (ThemeToggleVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ControlSize),
                typeof(ThemeToggleButton),
                new PropertyMetadata(ControlSize.Medium));

        /// <summary>
        /// Gets or sets the size of the toggle button.
        /// </summary>
        public ControlSize Size
        {
            get => (ControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region LightModeIcon Property

        public static readonly DependencyProperty LightModeIconProperty =
            DependencyProperty.Register(
                nameof(LightModeIcon),
                typeof(object),
                typeof(ThemeToggleButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon for light mode.
        /// </summary>
        public object? LightModeIcon
        {
            get => GetValue(LightModeIconProperty);
            set => SetValue(LightModeIconProperty, value);
        }

        #endregion

        #region DarkModeIcon Property

        public static readonly DependencyProperty DarkModeIconProperty =
            DependencyProperty.Register(
                nameof(DarkModeIcon),
                typeof(object),
                typeof(ThemeToggleButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon for dark mode.
        /// </summary>
        public object? DarkModeIcon
        {
            get => GetValue(DarkModeIconProperty);
            set => SetValue(DarkModeIconProperty, value);
        }

        #endregion

        #region ShowLabel Property

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register(
                nameof(ShowLabel),
                typeof(bool),
                typeof(ThemeToggleButton),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show a label next to the toggle.
        /// </summary>
        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        #endregion

        #region LightModeLabel Property

        public static readonly DependencyProperty LightModeLabelProperty =
            DependencyProperty.Register(
                nameof(LightModeLabel),
                typeof(string),
                typeof(ThemeToggleButton),
                new PropertyMetadata("Light"));

        /// <summary>
        /// Gets or sets the label text for light mode.
        /// </summary>
        public string LightModeLabel
        {
            get => (string)GetValue(LightModeLabelProperty);
            set => SetValue(LightModeLabelProperty, value);
        }

        #endregion

        #region DarkModeLabel Property

        public static readonly DependencyProperty DarkModeLabelProperty =
            DependencyProperty.Register(
                nameof(DarkModeLabel),
                typeof(string),
                typeof(ThemeToggleButton),
                new PropertyMetadata("Dark"));

        /// <summary>
        /// Gets or sets the label text for dark mode.
        /// </summary>
        public string DarkModeLabel
        {
            get => (string)GetValue(DarkModeLabelProperty);
            set => SetValue(DarkModeLabelProperty, value);
        }

        #endregion

        #region ThemeChangeCommand Property

        public static readonly DependencyProperty ThemeChangeCommandProperty =
            DependencyProperty.Register(
                nameof(ThemeChangeCommand),
                typeof(ICommand),
                typeof(ThemeToggleButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the theme changes.
        /// </summary>
        public ICommand? ThemeChangeCommand
        {
            get => (ICommand?)GetValue(ThemeChangeCommandProperty);
            set => SetValue(ThemeChangeCommandProperty, value);
        }

        #endregion

        #region Overrides

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            IsDarkTheme = true;
            ThemeChangeCommand?.Execute(true);
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            IsDarkTheme = false;
            ThemeChangeCommand?.Execute(false);
        }

        #endregion
    }

    /// <summary>
    /// Visual variants for ThemeToggleButton.
    /// </summary>
    public enum ThemeToggleVariant
    {
        /// <summary>Icon only toggle.</summary>
        IconOnly,
        /// <summary>Switch toggle with icons.</summary>
        Switch,
        /// <summary>Button with text.</summary>
        Button,
        /// <summary>Segmented control with both options.</summary>
        Segmented
    }
}
