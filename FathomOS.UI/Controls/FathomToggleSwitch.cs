using System.Windows;
using System.Windows.Controls.Primitives;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// Modern toggle switch control for FathomOS.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomToggleSwitch : ToggleButton
    {
        static FathomToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomToggleSwitch),
                new FrameworkPropertyMetadata(typeof(FathomToggleSwitch)));
        }

        #region OnLabel Property

        public static readonly DependencyProperty OnLabelProperty =
            DependencyProperty.Register(
                nameof(OnLabel),
                typeof(string),
                typeof(FathomToggleSwitch),
                new PropertyMetadata("On"));

        /// <summary>
        /// Gets or sets the label shown when toggle is on.
        /// </summary>
        public string OnLabel
        {
            get => (string)GetValue(OnLabelProperty);
            set => SetValue(OnLabelProperty, value);
        }

        #endregion

        #region OffLabel Property

        public static readonly DependencyProperty OffLabelProperty =
            DependencyProperty.Register(
                nameof(OffLabel),
                typeof(string),
                typeof(FathomToggleSwitch),
                new PropertyMetadata("Off"));

        /// <summary>
        /// Gets or sets the label shown when toggle is off.
        /// </summary>
        public string OffLabel
        {
            get => (string)GetValue(OffLabelProperty);
            set => SetValue(OffLabelProperty, value);
        }

        #endregion

        #region ShowLabel Property

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register(
                nameof(ShowLabel),
                typeof(bool),
                typeof(FathomToggleSwitch),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show On/Off labels.
        /// </summary>
        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        #endregion

        #region LabelPlacement Property

        public static readonly DependencyProperty LabelPlacementProperty =
            DependencyProperty.Register(
                nameof(LabelPlacement),
                typeof(ToggleLabelPlacement),
                typeof(FathomToggleSwitch),
                new PropertyMetadata(ToggleLabelPlacement.Right));

        /// <summary>
        /// Gets or sets the placement of the label relative to the switch.
        /// </summary>
        public ToggleLabelPlacement LabelPlacement
        {
            get => (ToggleLabelPlacement)GetValue(LabelPlacementProperty);
            set => SetValue(LabelPlacementProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ControlSize),
                typeof(FathomToggleSwitch),
                new PropertyMetadata(ControlSize.Medium));

        /// <summary>
        /// Gets or sets the size of the toggle switch.
        /// </summary>
        public ControlSize Size
        {
            get => (ControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Label placement options for toggle switch.
    /// </summary>
    public enum ToggleLabelPlacement
    {
        /// <summary>Label on the left of the switch.</summary>
        Left,
        /// <summary>Label on the right of the switch.</summary>
        Right
    }
}
