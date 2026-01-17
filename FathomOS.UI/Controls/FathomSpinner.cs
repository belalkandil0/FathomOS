using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Spinner
    /// A loading indicator for async operations.
    /// </summary>
    public class FathomSpinner : Control
    {
        static FathomSpinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomSpinner),
                new FrameworkPropertyMetadata(typeof(FathomSpinner)));
        }

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(SpinnerSize),
                typeof(FathomSpinner),
                new PropertyMetadata(SpinnerSize.Medium));

        /// <summary>
        /// Gets or sets the spinner size.
        /// </summary>
        public SpinnerSize Size
        {
            get => (SpinnerSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region SpinnerColor Property

        public static readonly DependencyProperty SpinnerColorProperty =
            DependencyProperty.Register(
                nameof(SpinnerColor),
                typeof(SpinnerVariant),
                typeof(FathomSpinner),
                new PropertyMetadata(SpinnerVariant.Primary));

        /// <summary>
        /// Gets or sets the spinner color variant.
        /// </summary>
        public SpinnerVariant SpinnerColor
        {
            get => (SpinnerVariant)GetValue(SpinnerColorProperty);
            set => SetValue(SpinnerColorProperty, value);
        }

        #endregion

        #region IsSpinning Property

        public static readonly DependencyProperty IsSpinningProperty =
            DependencyProperty.Register(
                nameof(IsSpinning),
                typeof(bool),
                typeof(FathomSpinner),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the spinner is actively spinning.
        /// </summary>
        public bool IsSpinning
        {
            get => (bool)GetValue(IsSpinningProperty);
            set => SetValue(IsSpinningProperty, value);
        }

        #endregion

        #region Label Property

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(FathomSpinner),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the loading label text.
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        #endregion

        #region LabelPlacement Property

        public static readonly DependencyProperty LabelPlacementProperty =
            DependencyProperty.Register(
                nameof(LabelPlacement),
                typeof(SpinnerLabelPlacement),
                typeof(FathomSpinner),
                new PropertyMetadata(SpinnerLabelPlacement.Right));

        /// <summary>
        /// Gets or sets the label placement relative to the spinner.
        /// </summary>
        public SpinnerLabelPlacement LabelPlacement
        {
            get => (SpinnerLabelPlacement)GetValue(LabelPlacementProperty);
            set => SetValue(LabelPlacementProperty, value);
        }

        #endregion

        #region StrokeThickness Property

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(FathomSpinner),
                new PropertyMetadata(3.0));

        /// <summary>
        /// Gets or sets the stroke thickness of the spinner.
        /// </summary>
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Size options for FathomSpinner.
    /// </summary>
    public enum SpinnerSize
    {
        /// <summary>Extra small spinner (16px).</summary>
        ExtraSmall,
        /// <summary>Small spinner (24px).</summary>
        Small,
        /// <summary>Medium spinner (32px).</summary>
        Medium,
        /// <summary>Large spinner (48px).</summary>
        Large,
        /// <summary>Extra large spinner (64px).</summary>
        ExtraLarge
    }

    /// <summary>
    /// Color variants for FathomSpinner.
    /// </summary>
    public enum SpinnerVariant
    {
        /// <summary>Primary brand color.</summary>
        Primary,
        /// <summary>Secondary/neutral color.</summary>
        Secondary,
        /// <summary>White (for dark backgrounds).</summary>
        White
    }

    /// <summary>
    /// Label placement options for FathomSpinner.
    /// </summary>
    public enum SpinnerLabelPlacement
    {
        /// <summary>Label below the spinner.</summary>
        Bottom,
        /// <summary>Label to the right of the spinner.</summary>
        Right
    }
}
