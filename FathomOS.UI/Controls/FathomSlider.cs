using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Slider
    /// A styled range input control.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomSlider : Slider
    {
        static FathomSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomSlider),
                new FrameworkPropertyMetadata(typeof(FathomSlider)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(SliderVariant),
                typeof(FathomSlider),
                new PropertyMetadata(SliderVariant.Primary));

        /// <summary>
        /// Gets or sets the color variant.
        /// </summary>
        public SliderVariant Variant
        {
            get => (SliderVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(SliderSize),
                typeof(FathomSlider),
                new PropertyMetadata(SliderSize.Medium));

        /// <summary>
        /// Gets or sets the size of the slider.
        /// </summary>
        public SliderSize Size
        {
            get => (SliderSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region ShowValue Property

        public static readonly DependencyProperty ShowValueProperty =
            DependencyProperty.Register(
                nameof(ShowValue),
                typeof(bool),
                typeof(FathomSlider),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show the current value.
        /// </summary>
        public bool ShowValue
        {
            get => (bool)GetValue(ShowValueProperty);
            set => SetValue(ShowValueProperty, value);
        }

        #endregion

        #region ValueFormat Property

        public static readonly DependencyProperty ValueFormatProperty =
            DependencyProperty.Register(
                nameof(ValueFormat),
                typeof(string),
                typeof(FathomSlider),
                new PropertyMetadata("{0:0}"));

        /// <summary>
        /// Gets or sets the format string for the value display.
        /// </summary>
        public string ValueFormat
        {
            get => (string)GetValue(ValueFormatProperty);
            set => SetValue(ValueFormatProperty, value);
        }

        #endregion

        #region ShowTicks Property

        public static readonly DependencyProperty ShowTicksProperty =
            DependencyProperty.Register(
                nameof(ShowTicks),
                typeof(bool),
                typeof(FathomSlider),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show tick marks.
        /// </summary>
        public bool ShowTicks
        {
            get => (bool)GetValue(ShowTicksProperty);
            set => SetValue(ShowTicksProperty, value);
        }

        #endregion

        #region Label Property

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(FathomSlider),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        public string? Label
        {
            get => (string?)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        #endregion

        #region ShowMinMax Property

        public static readonly DependencyProperty ShowMinMaxProperty =
            DependencyProperty.Register(
                nameof(ShowMinMax),
                typeof(bool),
                typeof(FathomSlider),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show min/max labels.
        /// </summary>
        public bool ShowMinMax
        {
            get => (bool)GetValue(ShowMinMaxProperty);
            set => SetValue(ShowMinMaxProperty, value);
        }

        #endregion

        #region ShowTooltip Property

        public static readonly DependencyProperty ShowTooltipProperty =
            DependencyProperty.Register(
                nameof(ShowTooltip),
                typeof(bool),
                typeof(FathomSlider),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to show a tooltip with the current value.
        /// </summary>
        public bool ShowTooltip
        {
            get => (bool)GetValue(ShowTooltipProperty);
            set => SetValue(ShowTooltipProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Color variants for FathomSlider.
    /// </summary>
    public enum SliderVariant
    {
        /// <summary>Primary brand color.</summary>
        Primary,
        /// <summary>Secondary/neutral color.</summary>
        Secondary,
        /// <summary>Success color.</summary>
        Success,
        /// <summary>Warning color.</summary>
        Warning,
        /// <summary>Error color.</summary>
        Error
    }

    /// <summary>
    /// Size options for FathomSlider.
    /// </summary>
    public enum SliderSize
    {
        /// <summary>Small slider (4px track).</summary>
        Small,
        /// <summary>Medium slider (6px track).</summary>
        Medium,
        /// <summary>Large slider (8px track).</summary>
        Large
    }
}
