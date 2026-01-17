using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - TreeView
    /// A styled hierarchical tree control.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomTreeView : TreeView
    {
        static FathomTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomTreeView),
                new FrameworkPropertyMetadata(typeof(FathomTreeView)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(TreeViewVariant),
                typeof(FathomTreeView),
                new PropertyMetadata(TreeViewVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant of the tree view.
        /// </summary>
        public TreeViewVariant Variant
        {
            get => (TreeViewVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region ShowLines Property

        public static readonly DependencyProperty ShowLinesProperty =
            DependencyProperty.Register(
                nameof(ShowLines),
                typeof(bool),
                typeof(FathomTreeView),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show connecting lines.
        /// </summary>
        public bool ShowLines
        {
            get => (bool)GetValue(ShowLinesProperty);
            set => SetValue(ShowLinesProperty, value);
        }

        #endregion

        #region IndentSize Property

        public static readonly DependencyProperty IndentSizeProperty =
            DependencyProperty.Register(
                nameof(IndentSize),
                typeof(double),
                typeof(FathomTreeView),
                new PropertyMetadata(20.0));

        /// <summary>
        /// Gets or sets the indentation size for nested items.
        /// </summary>
        public double IndentSize
        {
            get => (double)GetValue(IndentSizeProperty);
            set => SetValue(IndentSizeProperty, value);
        }

        #endregion

        #region ExpandOnClick Property

        public static readonly DependencyProperty ExpandOnClickProperty =
            DependencyProperty.Register(
                nameof(ExpandOnClick),
                typeof(bool),
                typeof(FathomTreeView),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether clicking an item expands/collapses it.
        /// </summary>
        public bool ExpandOnClick
        {
            get => (bool)GetValue(ExpandOnClickProperty);
            set => SetValue(ExpandOnClickProperty, value);
        }

        #endregion

        #region ShowCheckBoxes Property

        public static readonly DependencyProperty ShowCheckBoxesProperty =
            DependencyProperty.Register(
                nameof(ShowCheckBoxes),
                typeof(bool),
                typeof(FathomTreeView),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show checkboxes for selection.
        /// </summary>
        public bool ShowCheckBoxes
        {
            get => (bool)GetValue(ShowCheckBoxesProperty);
            set => SetValue(ShowCheckBoxesProperty, value);
        }

        #endregion

        #region SelectionMode Property

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(
                nameof(SelectionMode),
                typeof(TreeSelectionMode),
                typeof(FathomTreeView),
                new PropertyMetadata(TreeSelectionMode.Single));

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        public TreeSelectionMode SelectionMode
        {
            get => (TreeSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// FathomOS Design System - TreeViewItem
    /// A styled tree view item with icon support.
    /// </summary>
    public class FathomTreeViewItem : TreeViewItem
    {
        static FathomTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomTreeViewItem),
                new FrameworkPropertyMetadata(typeof(FathomTreeViewItem)));
        }

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomTreeViewItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed before the item content.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region ExpandedIcon Property

        public static readonly DependencyProperty ExpandedIconProperty =
            DependencyProperty.Register(
                nameof(ExpandedIcon),
                typeof(object),
                typeof(FathomTreeViewItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon shown when the item is expanded.
        /// </summary>
        public object? ExpandedIcon
        {
            get => GetValue(ExpandedIconProperty);
            set => SetValue(ExpandedIconProperty, value);
        }

        #endregion

        #region IsChecked Property

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool?),
                typeof(FathomTreeViewItem),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the item is checked (for checkbox mode).
        /// </summary>
        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        #endregion

        #region Badge Property

        public static readonly DependencyProperty BadgeProperty =
            DependencyProperty.Register(
                nameof(Badge),
                typeof(object),
                typeof(FathomTreeViewItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the badge content displayed after the item.
        /// </summary>
        public object? Badge
        {
            get => GetValue(BadgeProperty);
            set => SetValue(BadgeProperty, value);
        }

        #endregion

        #region Description Property

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(FathomTreeViewItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the description text below the header.
        /// </summary>
        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Visual variants for FathomTreeView.
    /// </summary>
    public enum TreeViewVariant
    {
        /// <summary>Default tree view.</summary>
        Default,
        /// <summary>Bordered container.</summary>
        Bordered,
        /// <summary>File explorer style.</summary>
        FileExplorer,
        /// <summary>Menu/navigation style.</summary>
        Navigation
    }

    /// <summary>
    /// Selection modes for FathomTreeView.
    /// </summary>
    public enum TreeSelectionMode
    {
        /// <summary>Only one item can be selected.</summary>
        Single,
        /// <summary>Multiple items can be selected.</summary>
        Multiple,
        /// <summary>Selection is disabled.</summary>
        None
    }
}
