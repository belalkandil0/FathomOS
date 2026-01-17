using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - ComboBox
    /// A styled dropdown selection control.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomComboBox : ComboBox
    {
        static FathomComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomComboBox),
                new FrameworkPropertyMetadata(typeof(FathomComboBox)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ComboBoxVariant),
                typeof(FathomComboBox),
                new PropertyMetadata(ComboBoxVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant of the combo box.
        /// </summary>
        public ComboBoxVariant Variant
        {
            get => (ComboBoxVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ControlSize),
                typeof(FathomComboBox),
                new PropertyMetadata(ControlSize.Medium));

        /// <summary>
        /// Gets or sets the size of the combo box.
        /// </summary>
        public ControlSize Size
        {
            get => (ControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Placeholder Property

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(FathomComboBox),
                new PropertyMetadata("Select an option..."));

        /// <summary>
        /// Gets or sets the placeholder text when no item is selected.
        /// </summary>
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        #endregion

        #region Label Property

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(FathomComboBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the label text above the combo box.
        /// </summary>
        public string? Label
        {
            get => (string?)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        #endregion

        #region HelperText Property

        public static readonly DependencyProperty HelperTextProperty =
            DependencyProperty.Register(
                nameof(HelperText),
                typeof(string),
                typeof(FathomComboBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the helper/description text below the combo box.
        /// </summary>
        public string? HelperText
        {
            get => (string?)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }

        #endregion

        #region ErrorMessage Property

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage),
                typeof(string),
                typeof(FathomComboBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the error message to display.
        /// </summary>
        public string? ErrorMessage
        {
            get => (string?)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        #endregion

        #region HasError Property

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(
                nameof(HasError),
                typeof(bool),
                typeof(FathomComboBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the combo box is in an error state.
        /// </summary>
        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            set => SetValue(HasErrorProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomComboBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the combo box.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FathomComboBox),
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

        #region IsSearchable Property

        public static readonly DependencyProperty IsSearchableProperty =
            DependencyProperty.Register(
                nameof(IsSearchable),
                typeof(bool),
                typeof(FathomComboBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the combo box allows text search.
        /// </summary>
        public bool IsSearchable
        {
            get => (bool)GetValue(IsSearchableProperty);
            set => SetValue(IsSearchableProperty, value);
        }

        #endregion

        #region IsClearable Property

        public static readonly DependencyProperty IsClearableProperty =
            DependencyProperty.Register(
                nameof(IsClearable),
                typeof(bool),
                typeof(FathomComboBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the combo box shows a clear button.
        /// </summary>
        public bool IsClearable
        {
            get => (bool)GetValue(IsClearableProperty);
            set => SetValue(IsClearableProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Visual variants for FathomComboBox.
    /// </summary>
    public enum ComboBoxVariant
    {
        /// <summary>Default bordered combo box.</summary>
        Default,
        /// <summary>Filled/solid background combo box.</summary>
        Filled,
        /// <summary>Outline-only combo box.</summary>
        Outline,
        /// <summary>Underline-only combo box.</summary>
        Underline
    }
}
