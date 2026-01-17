using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Toolbar
    /// A styled toolbar container for action buttons and controls.
    /// </summary>
    public class FathomToolbar : ItemsControl
    {
        static FathomToolbar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomToolbar),
                new FrameworkPropertyMetadata(typeof(FathomToolbar)));
        }

        #region Orientation Property

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(FathomToolbar),
                new PropertyMetadata(Orientation.Horizontal));

        /// <summary>
        /// Gets or sets the toolbar orientation.
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ToolbarVariant),
                typeof(FathomToolbar),
                new PropertyMetadata(ToolbarVariant.Default));

        /// <summary>
        /// Gets or sets the toolbar visual variant.
        /// </summary>
        public ToolbarVariant Variant
        {
            get => (ToolbarVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region ShowDividers Property

        public static readonly DependencyProperty ShowDividersProperty =
            DependencyProperty.Register(
                nameof(ShowDividers),
                typeof(bool),
                typeof(FathomToolbar),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show dividers between items.
        /// </summary>
        public bool ShowDividers
        {
            get => (bool)GetValue(ShowDividersProperty);
            set => SetValue(ShowDividersProperty, value);
        }

        #endregion

        #region ItemSpacing Property

        public static readonly DependencyProperty ItemSpacingProperty =
            DependencyProperty.Register(
                nameof(ItemSpacing),
                typeof(double),
                typeof(FathomToolbar),
                new PropertyMetadata(4.0));

        /// <summary>
        /// Gets or sets the spacing between toolbar items.
        /// </summary>
        public double ItemSpacing
        {
            get => (double)GetValue(ItemSpacingProperty);
            set => SetValue(ItemSpacingProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// FathomOS Design System - Toolbar Separator
    /// A visual separator for toolbar item groups.
    /// </summary>
    public class FathomToolbarSeparator : Control
    {
        static FathomToolbarSeparator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomToolbarSeparator),
                new FrameworkPropertyMetadata(typeof(FathomToolbarSeparator)));
        }
    }

    /// <summary>
    /// Visual variants for FathomToolbar.
    /// </summary>
    public enum ToolbarVariant
    {
        /// <summary>Default flat toolbar.</summary>
        Default,
        /// <summary>Raised toolbar with shadow.</summary>
        Raised,
        /// <summary>Bordered toolbar.</summary>
        Bordered
    }
}
