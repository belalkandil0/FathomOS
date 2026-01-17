using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - ProgressBar
    /// A styled progress indicator with multiple variants.
    /// </summary>
    public class FathomProgressBar : ProgressBar
    {
        static FathomProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomProgressBar),
                new FrameworkPropertyMetadata(typeof(FathomProgressBar)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ProgressVariant),
                typeof(FathomProgressBar),
                new PropertyMetadata(ProgressVariant.Primary));

        /// <summary>
        /// Gets or sets the color variant.
        /// </summary>
        public ProgressVariant Variant
        {
            get => (ProgressVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ProgressSize),
                typeof(FathomProgressBar),
                new PropertyMetadata(ProgressSize.Medium));

        /// <summary>
        /// Gets or sets the size/thickness of the progress bar.
        /// </summary>
        public ProgressSize Size
        {
            get => (ProgressSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region ShowLabel Property

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register(
                nameof(ShowLabel),
                typeof(bool),
                typeof(FathomProgressBar),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show the percentage label.
        /// </summary>
        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        #endregion

        #region LabelFormat Property

        public static readonly DependencyProperty LabelFormatProperty =
            DependencyProperty.Register(
                nameof(LabelFormat),
                typeof(string),
                typeof(FathomProgressBar),
                new PropertyMetadata("{0:0}%"));

        /// <summary>
        /// Gets or sets the format string for the percentage label.
        /// </summary>
        public string LabelFormat
        {
            get => (string)GetValue(LabelFormatProperty);
            set => SetValue(LabelFormatProperty, value);
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FathomProgressBar),
                new PropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region IsStriped Property

        public static readonly DependencyProperty IsStripedProperty =
            DependencyProperty.Register(
                nameof(IsStriped),
                typeof(bool),
                typeof(FathomProgressBar),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show striped pattern.
        /// </summary>
        public bool IsStriped
        {
            get => (bool)GetValue(IsStripedProperty);
            set => SetValue(IsStripedProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Color variants for FathomProgressBar.
    /// </summary>
    public enum ProgressVariant
    {
        /// <summary>Primary brand color.</summary>
        Primary,
        /// <summary>Success/positive color.</summary>
        Success,
        /// <summary>Warning color.</summary>
        Warning,
        /// <summary>Error/danger color.</summary>
        Error,
        /// <summary>Informational color.</summary>
        Info
    }

    /// <summary>
    /// Size options for FathomProgressBar.
    /// </summary>
    public enum ProgressSize
    {
        /// <summary>Small/thin progress bar (4px).</summary>
        Small,
        /// <summary>Medium progress bar (8px).</summary>
        Medium,
        /// <summary>Large/thick progress bar (16px).</summary>
        Large
    }
}
