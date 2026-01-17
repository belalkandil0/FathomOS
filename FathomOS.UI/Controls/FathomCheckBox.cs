using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// Premium checkbox control for FathomOS.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomCheckBox : CheckBox
    {
        static FathomCheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomCheckBox),
                new FrameworkPropertyMetadata(typeof(FathomCheckBox)));
        }

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ControlSize),
                typeof(FathomCheckBox),
                new PropertyMetadata(ControlSize.Medium));

        /// <summary>
        /// Gets or sets the size of the checkbox.
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
                typeof(FathomCheckBox),
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
