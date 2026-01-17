using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - TabControl
    /// A styled tab control with consistent theming.
    /// </summary>
    public class FathomTabControl : TabControl
    {
        static FathomTabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomTabControl),
                new FrameworkPropertyMetadata(typeof(FathomTabControl)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(TabVariant),
                typeof(FathomTabControl),
                new PropertyMetadata(TabVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant of the tab control.
        /// </summary>
        public TabVariant Variant
        {
            get => (TabVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region TabStripPlacement Override

        // TabStripPlacement is already a property on TabControl, so we just use it

        #endregion
    }

    /// <summary>
    /// FathomOS Design System - TabItem
    /// A styled tab item with icon support.
    /// </summary>
    public class FathomTabItem : TabItem
    {
        static FathomTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomTabItem),
                new FrameworkPropertyMetadata(typeof(FathomTabItem)));
        }

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomTabItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the tab header.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region IsClosable Property

        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(
                nameof(IsClosable),
                typeof(bool),
                typeof(FathomTabItem),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the tab can be closed.
        /// </summary>
        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        #endregion

        #region CloseCommand Property

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(
                nameof(CloseCommand),
                typeof(System.Windows.Input.ICommand),
                typeof(FathomTabItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command to execute when the close button is clicked.
        /// </summary>
        public System.Windows.Input.ICommand? CloseCommand
        {
            get => (System.Windows.Input.ICommand?)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Visual variants for FathomTabControl.
    /// </summary>
    public enum TabVariant
    {
        /// <summary>Default underline style tabs.</summary>
        Default,
        /// <summary>Enclosed boxed tabs.</summary>
        Enclosed,
        /// <summary>Pill/capsule style tabs.</summary>
        Pills
    }
}
