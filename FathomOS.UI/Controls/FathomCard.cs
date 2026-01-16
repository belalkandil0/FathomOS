using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// Premium card control for FathomOS with elevation and variants.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomCard : ContentControl
    {
        static FathomCard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomCard),
                new FrameworkPropertyMetadata(typeof(FathomCard)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(CardVariant),
                typeof(FathomCard),
                new PropertyMetadata(CardVariant.Elevated));

        /// <summary>
        /// Gets or sets the visual variant of the card.
        /// </summary>
        public CardVariant Variant
        {
            get => (CardVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Elevation Property

        public static readonly DependencyProperty ElevationProperty =
            DependencyProperty.Register(
                nameof(Elevation),
                typeof(int),
                typeof(FathomCard),
                new PropertyMetadata(1, OnElevationChanged),
                ValidateElevation);

        /// <summary>
        /// Gets or sets the elevation level (0-5).
        /// </summary>
        public int Elevation
        {
            get => (int)GetValue(ElevationProperty);
            set => SetValue(ElevationProperty, value);
        }

        private static bool ValidateElevation(object value)
        {
            return (int)value >= 0 && (int)value <= 5;
        }

        private static void OnElevationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FathomCard card)
            {
                card.UpdateShadow();
            }
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FathomCard),
                new PropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the card.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region Header Property

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(FathomCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the header content.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        #endregion

        #region Footer Property

        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register(
                nameof(Footer),
                typeof(object),
                typeof(FathomCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the footer content.
        /// </summary>
        public object? Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        #endregion

        #region IsClickable Property

        public static readonly DependencyProperty IsClickableProperty =
            DependencyProperty.Register(
                nameof(IsClickable),
                typeof(bool),
                typeof(FathomCard),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the card responds to click interactions.
        /// </summary>
        public bool IsClickable
        {
            get => (bool)GetValue(IsClickableProperty);
            set => SetValue(IsClickableProperty, value);
        }

        #endregion

        #region Click Event

        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Click),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FathomCard));

        /// <summary>
        /// Occurs when the card is clicked (when IsClickable is true).
        /// </summary>
        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        #endregion

        private void UpdateShadow()
        {
            // Shadow will be applied via control template based on Elevation
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (IsClickable)
            {
                RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            }
        }
    }

    /// <summary>
    /// Card visual variants.
    /// </summary>
    public enum CardVariant
    {
        /// <summary>Card with shadow elevation.</summary>
        Elevated,
        /// <summary>Card with border outline.</summary>
        Outlined,
        /// <summary>Card with filled background.</summary>
        Filled
    }
}
