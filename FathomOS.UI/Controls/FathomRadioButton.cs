using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// Premium radio button control for FathomOS.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomRadioButton : RadioButton
    {
        static FathomRadioButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomRadioButton),
                new FrameworkPropertyMetadata(typeof(FathomRadioButton)));
        }

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ControlSize),
                typeof(FathomRadioButton),
                new PropertyMetadata(ControlSize.Medium));

        /// <summary>
        /// Gets or sets the size of the radio button.
        /// </summary>
        public ControlSize Size
        {
            get => (ControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Description Property

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(FathomRadioButton),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a description text shown below the label.
        /// </summary>
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        #endregion
    }
}
