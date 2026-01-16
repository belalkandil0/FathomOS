using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// Premium button control for FathomOS with multiple variants and sizes.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomButton : Button
    {
        static FathomButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomButton),
                new FrameworkPropertyMetadata(typeof(FathomButton)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ButtonVariant),
                typeof(FathomButton),
                new PropertyMetadata(ButtonVariant.Primary));

        /// <summary>
        /// Gets or sets the visual variant of the button.
        /// </summary>
        public ButtonVariant Variant
        {
            get => (ButtonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ControlSize),
                typeof(FathomButton),
                new PropertyMetadata(ControlSize.Medium));

        /// <summary>
        /// Gets or sets the size of the button.
        /// </summary>
        public ControlSize Size
        {
            get => (ControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region IsLoading Property

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(FathomButton),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the button shows a loading state.
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the button.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region IconPlacement Property

        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.Register(
                nameof(IconPlacement),
                typeof(IconPlacement),
                typeof(FathomButton),
                new PropertyMetadata(IconPlacement.Left));

        /// <summary>
        /// Gets or sets the placement of the icon relative to content.
        /// </summary>
        public IconPlacement IconPlacement
        {
            get => (IconPlacement)GetValue(IconPlacementProperty);
            set => SetValue(IconPlacementProperty, value);
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FathomButton),
                new PropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the button.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Button visual variants.
    /// </summary>
    public enum ButtonVariant
    {
        /// <summary>Primary action button with solid background.</summary>
        Primary,
        /// <summary>Secondary action button with muted appearance.</summary>
        Secondary,
        /// <summary>Outlined button with border only.</summary>
        Outline,
        /// <summary>Ghost button with no background.</summary>
        Ghost,
        /// <summary>Danger/destructive action button.</summary>
        Danger,
        /// <summary>Success action button.</summary>
        Success
    }

    /// <summary>
    /// Control size options.
    /// </summary>
    public enum ControlSize
    {
        /// <summary>Small size (28px height)</summary>
        Small,
        /// <summary>Medium size (36px height)</summary>
        Medium,
        /// <summary>Large size (44px height)</summary>
        Large
    }

    /// <summary>
    /// Icon placement options.
    /// </summary>
    public enum IconPlacement
    {
        /// <summary>Icon on the left of content.</summary>
        Left,
        /// <summary>Icon on the right of content.</summary>
        Right,
        /// <summary>Icon only, no text content.</summary>
        IconOnly
    }
}
