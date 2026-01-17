using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// Premium text input control for FathomOS with validation states.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomTextBox : TextBox
    {
        static FathomTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomTextBox),
                new FrameworkPropertyMetadata(typeof(FathomTextBox)));
        }

        #region Placeholder Property

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(FathomTextBox),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text shown when empty.
        /// </summary>
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FathomTextBox),
                new PropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the text box.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region HasError Property

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(
                nameof(HasError),
                typeof(bool),
                typeof(FathomTextBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the text box shows an error state.
        /// </summary>
        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            set => SetValue(HasErrorProperty, value);
        }

        #endregion

        #region ErrorMessage Property

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage),
                typeof(string),
                typeof(FathomTextBox),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the error message to display.
        /// </summary>
        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        #endregion

        #region Label Property

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(FathomTextBox),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the label text above the input.
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomTextBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the text box.
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
                typeof(FathomTextBox),
                new PropertyMetadata(IconPlacement.Left));

        /// <summary>
        /// Gets or sets the placement of the icon.
        /// </summary>
        public IconPlacement IconPlacement
        {
            get => (IconPlacement)GetValue(IconPlacementProperty);
            set => SetValue(IconPlacementProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ControlSize),
                typeof(FathomTextBox),
                new PropertyMetadata(ControlSize.Medium));

        /// <summary>
        /// Gets or sets the size of the text box.
        /// </summary>
        public ControlSize Size
        {
            get => (ControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion
    }
}
