using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Badge/Tag
    /// A small label for status indicators, counts, or tags.
    /// </summary>
    public class FathomBadge : ContentControl
    {
        static FathomBadge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomBadge),
                new FrameworkPropertyMetadata(typeof(FathomBadge)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(BadgeVariant),
                typeof(FathomBadge),
                new PropertyMetadata(BadgeVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant/color of the badge.
        /// </summary>
        public BadgeVariant Variant
        {
            get => (BadgeVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(BadgeSize),
                typeof(FathomBadge),
                new PropertyMetadata(BadgeSize.Medium));

        /// <summary>
        /// Gets or sets the size of the badge.
        /// </summary>
        public BadgeSize Size
        {
            get => (BadgeSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region IsOutlined Property

        public static readonly DependencyProperty IsOutlinedProperty =
            DependencyProperty.Register(
                nameof(IsOutlined),
                typeof(bool),
                typeof(FathomBadge),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the badge has an outlined style.
        /// </summary>
        public bool IsOutlined
        {
            get => (bool)GetValue(IsOutlinedProperty);
            set => SetValue(IsOutlinedProperty, value);
        }

        #endregion

        #region IsPill Property

        public static readonly DependencyProperty IsPillProperty =
            DependencyProperty.Register(
                nameof(IsPill),
                typeof(bool),
                typeof(FathomBadge),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the badge has pill/capsule shape.
        /// </summary>
        public bool IsPill
        {
            get => (bool)GetValue(IsPillProperty);
            set => SetValue(IsPillProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomBadge),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed before the content.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region IsRemovable Property

        public static readonly DependencyProperty IsRemovableProperty =
            DependencyProperty.Register(
                nameof(IsRemovable),
                typeof(bool),
                typeof(FathomBadge),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the badge shows a remove button.
        /// </summary>
        public bool IsRemovable
        {
            get => (bool)GetValue(IsRemovableProperty);
            set => SetValue(IsRemovableProperty, value);
        }

        #endregion

        #region RemoveCommand Property

        public static readonly DependencyProperty RemoveCommandProperty =
            DependencyProperty.Register(
                nameof(RemoveCommand),
                typeof(System.Windows.Input.ICommand),
                typeof(FathomBadge),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the remove button is clicked.
        /// </summary>
        public System.Windows.Input.ICommand? RemoveCommand
        {
            get => (System.Windows.Input.ICommand?)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Color variants for FathomBadge.
    /// </summary>
    public enum BadgeVariant
    {
        /// <summary>Default neutral badge.</summary>
        Default,
        /// <summary>Primary brand color.</summary>
        Primary,
        /// <summary>Success/positive indicator.</summary>
        Success,
        /// <summary>Warning indicator.</summary>
        Warning,
        /// <summary>Error/danger indicator.</summary>
        Error,
        /// <summary>Informational indicator.</summary>
        Info
    }

    /// <summary>
    /// Size options for FathomBadge.
    /// </summary>
    public enum BadgeSize
    {
        /// <summary>Small badge.</summary>
        Small,
        /// <summary>Medium badge (default).</summary>
        Medium,
        /// <summary>Large badge.</summary>
        Large
    }
}
