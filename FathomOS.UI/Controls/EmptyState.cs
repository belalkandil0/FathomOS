using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Empty State
    /// A placeholder for empty data states with optional actions.
    /// Owned by: UI-AGENT
    /// </summary>
    public class EmptyState : ContentControl
    {
        static EmptyState()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(EmptyState),
                new FrameworkPropertyMetadata(typeof(EmptyState)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(EmptyStateVariant),
                typeof(EmptyState),
                new PropertyMetadata(EmptyStateVariant.Default));

        /// <summary>
        /// Gets or sets the empty state variant.
        /// </summary>
        public EmptyStateVariant Variant
        {
            get => (EmptyStateVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Title Property

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(EmptyState),
                new PropertyMetadata("No data"));

        /// <summary>
        /// Gets or sets the title text.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region Description Property

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(EmptyState),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the description text.
        /// </summary>
        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(EmptyState),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed above the title.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region IconSize Property

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(EmptyState),
                new PropertyMetadata(64.0));

        /// <summary>
        /// Gets or sets the icon size.
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        #endregion

        #region Image Property

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                nameof(Image),
                typeof(object),
                typeof(EmptyState),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets an illustration image.
        /// </summary>
        public object? Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        #endregion

        #region ImageMaxWidth Property

        public static readonly DependencyProperty ImageMaxWidthProperty =
            DependencyProperty.Register(
                nameof(ImageMaxWidth),
                typeof(double),
                typeof(EmptyState),
                new PropertyMetadata(200.0));

        /// <summary>
        /// Gets or sets the maximum width for the illustration image.
        /// </summary>
        public double ImageMaxWidth
        {
            get => (double)GetValue(ImageMaxWidthProperty);
            set => SetValue(ImageMaxWidthProperty, value);
        }

        #endregion

        #region PrimaryActionText Property

        public static readonly DependencyProperty PrimaryActionTextProperty =
            DependencyProperty.Register(
                nameof(PrimaryActionText),
                typeof(string),
                typeof(EmptyState),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the primary action button text.
        /// </summary>
        public string? PrimaryActionText
        {
            get => (string?)GetValue(PrimaryActionTextProperty);
            set => SetValue(PrimaryActionTextProperty, value);
        }

        #endregion

        #region PrimaryActionCommand Property

        public static readonly DependencyProperty PrimaryActionCommandProperty =
            DependencyProperty.Register(
                nameof(PrimaryActionCommand),
                typeof(ICommand),
                typeof(EmptyState),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the primary action button command.
        /// </summary>
        public ICommand? PrimaryActionCommand
        {
            get => (ICommand?)GetValue(PrimaryActionCommandProperty);
            set => SetValue(PrimaryActionCommandProperty, value);
        }

        #endregion

        #region SecondaryActionText Property

        public static readonly DependencyProperty SecondaryActionTextProperty =
            DependencyProperty.Register(
                nameof(SecondaryActionText),
                typeof(string),
                typeof(EmptyState),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the secondary action button text.
        /// </summary>
        public string? SecondaryActionText
        {
            get => (string?)GetValue(SecondaryActionTextProperty);
            set => SetValue(SecondaryActionTextProperty, value);
        }

        #endregion

        #region SecondaryActionCommand Property

        public static readonly DependencyProperty SecondaryActionCommandProperty =
            DependencyProperty.Register(
                nameof(SecondaryActionCommand),
                typeof(ICommand),
                typeof(EmptyState),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the secondary action button command.
        /// </summary>
        public ICommand? SecondaryActionCommand
        {
            get => (ICommand?)GetValue(SecondaryActionCommandProperty);
            set => SetValue(SecondaryActionCommandProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(EmptyStateSize),
                typeof(EmptyState),
                new PropertyMetadata(EmptyStateSize.Medium));

        /// <summary>
        /// Gets or sets the overall size of the empty state.
        /// </summary>
        public EmptyStateSize Size
        {
            get => (EmptyStateSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Orientation Property

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(EmptyState),
                new PropertyMetadata(Orientation.Vertical));

        /// <summary>
        /// Gets or sets the layout orientation.
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Variants for EmptyState.
    /// </summary>
    public enum EmptyStateVariant
    {
        /// <summary>Default empty state.</summary>
        Default,
        /// <summary>No search results.</summary>
        NoResults,
        /// <summary>Error state.</summary>
        Error,
        /// <summary>No data available.</summary>
        NoData,
        /// <summary>No permissions/access.</summary>
        NoAccess,
        /// <summary>Offline state.</summary>
        Offline,
        /// <summary>Feature coming soon.</summary>
        ComingSoon,
        /// <summary>Success/completed state.</summary>
        Success
    }

    /// <summary>
    /// Size options for EmptyState.
    /// </summary>
    public enum EmptyStateSize
    {
        /// <summary>Small compact empty state.</summary>
        Small,
        /// <summary>Medium empty state.</summary>
        Medium,
        /// <summary>Large full-page empty state.</summary>
        Large
    }
}
