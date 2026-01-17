using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Tooltip
    /// A styled tooltip with multiple variants.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomTooltip : ToolTip
    {
        static FathomTooltip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomTooltip),
                new FrameworkPropertyMetadata(typeof(FathomTooltip)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(TooltipVariant),
                typeof(FathomTooltip),
                new PropertyMetadata(TooltipVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant of the tooltip.
        /// </summary>
        public TooltipVariant Variant
        {
            get => (TooltipVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Placement Property Override

        // PlacementMode is already available from ToolTip base class

        #endregion

        #region ShowArrow Property

        public static readonly DependencyProperty ShowArrowProperty =
            DependencyProperty.Register(
                nameof(ShowArrow),
                typeof(bool),
                typeof(FathomTooltip),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show an arrow pointing to the target element.
        /// </summary>
        public bool ShowArrow
        {
            get => (bool)GetValue(ShowArrowProperty);
            set => SetValue(ShowArrowProperty, value);
        }

        #endregion

        #region Title Property

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(FathomTooltip),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the title text displayed above the content.
        /// </summary>
        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomTooltip),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the tooltip.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Visual variants for FathomTooltip.
    /// </summary>
    public enum TooltipVariant
    {
        /// <summary>Default dark tooltip.</summary>
        Default,
        /// <summary>Light theme tooltip.</summary>
        Light,
        /// <summary>Primary brand color tooltip.</summary>
        Primary,
        /// <summary>Success/positive tooltip.</summary>
        Success,
        /// <summary>Warning tooltip.</summary>
        Warning,
        /// <summary>Error/danger tooltip.</summary>
        Error,
        /// <summary>Informational tooltip.</summary>
        Info
    }
}
