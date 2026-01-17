using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Expander
    /// A styled collapsible panel with header and content.
    /// </summary>
    public class FathomExpander : Expander
    {
        static FathomExpander()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomExpander),
                new FrameworkPropertyMetadata(typeof(FathomExpander)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ExpanderVariant),
                typeof(FathomExpander),
                new PropertyMetadata(ExpanderVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant of the expander.
        /// </summary>
        public ExpanderVariant Variant
        {
            get => (ExpanderVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomExpander),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the header.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region Subtitle Property

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(FathomExpander),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the subtitle text shown below the header.
        /// </summary>
        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FathomExpander),
                new PropertyMetadata(new CornerRadius(6)));

        /// <summary>
        /// Gets or sets the corner radius.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Visual variants for FathomExpander.
    /// </summary>
    public enum ExpanderVariant
    {
        /// <summary>Default flat expander.</summary>
        Default,
        /// <summary>Card-style elevated expander.</summary>
        Card,
        /// <summary>Bordered expander.</summary>
        Bordered,
        /// <summary>Subtle/ghost expander.</summary>
        Subtle
    }
}
