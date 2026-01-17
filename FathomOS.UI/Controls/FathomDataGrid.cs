using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - DataGrid
    /// A premium styled data grid with advanced features.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomDataGrid : DataGrid
    {
        static FathomDataGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomDataGrid),
                new FrameworkPropertyMetadata(typeof(FathomDataGrid)));
        }

        #region Constructor

        public FathomDataGrid()
        {
            // Set default properties for a premium look
            AutoGenerateColumns = false;
            CanUserAddRows = false;
            CanUserDeleteRows = false;
            GridLinesVisibility = DataGridGridLinesVisibility.None;
            HeadersVisibility = DataGridHeadersVisibility.Column;
            SelectionUnit = DataGridSelectionUnit.FullRow;
            IsReadOnly = true;
        }

        #endregion

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DataGridVariant),
                typeof(FathomDataGrid),
                new PropertyMetadata(DataGridVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant of the data grid.
        /// </summary>
        public DataGridVariant Variant
        {
            get => (DataGridVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region ShowRowNumbers Property

        public static readonly DependencyProperty ShowRowNumbersProperty =
            DependencyProperty.Register(
                nameof(ShowRowNumbers),
                typeof(bool),
                typeof(FathomDataGrid),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show row numbers.
        /// </summary>
        public bool ShowRowNumbers
        {
            get => (bool)GetValue(ShowRowNumbersProperty);
            set => SetValue(ShowRowNumbersProperty, value);
        }

        #endregion

        #region ShowAlternatingRows Property

        public static readonly DependencyProperty ShowAlternatingRowsProperty =
            DependencyProperty.Register(
                nameof(ShowAlternatingRows),
                typeof(bool),
                typeof(FathomDataGrid),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to show alternating row colors.
        /// </summary>
        public bool ShowAlternatingRows
        {
            get => (bool)GetValue(ShowAlternatingRowsProperty);
            set => SetValue(ShowAlternatingRowsProperty, value);
        }

        #endregion

        #region IsStriped Property

        public static readonly DependencyProperty IsStripedProperty =
            DependencyProperty.Register(
                nameof(IsStriped),
                typeof(bool),
                typeof(FathomDataGrid),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether rows have a striped appearance.
        /// </summary>
        public bool IsStriped
        {
            get => (bool)GetValue(IsStripedProperty);
            set => SetValue(IsStripedProperty, value);
        }

        #endregion

        #region IsBordered Property

        public static readonly DependencyProperty IsBorderedProperty =
            DependencyProperty.Register(
                nameof(IsBordered),
                typeof(bool),
                typeof(FathomDataGrid),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether cells have visible borders.
        /// </summary>
        public bool IsBordered
        {
            get => (bool)GetValue(IsBorderedProperty);
            set => SetValue(IsBorderedProperty, value);
        }

        #endregion

        #region IsCompact Property

        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(
                nameof(IsCompact),
                typeof(bool),
                typeof(FathomDataGrid),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether rows have reduced padding.
        /// </summary>
        public bool IsCompact
        {
            get => (bool)GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }

        #endregion

        #region ShowHoverEffect Property

        public static readonly DependencyProperty ShowHoverEffectProperty =
            DependencyProperty.Register(
                nameof(ShowHoverEffect),
                typeof(bool),
                typeof(FathomDataGrid),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether rows show a hover effect.
        /// </summary>
        public bool ShowHoverEffect
        {
            get => (bool)GetValue(ShowHoverEffectProperty);
            set => SetValue(ShowHoverEffectProperty, value);
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FathomDataGrid),
                new PropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the grid container.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region EmptyMessage Property

        public static readonly DependencyProperty EmptyMessageProperty =
            DependencyProperty.Register(
                nameof(EmptyMessage),
                typeof(string),
                typeof(FathomDataGrid),
                new PropertyMetadata("No data available"));

        /// <summary>
        /// Gets or sets the message shown when there is no data.
        /// </summary>
        public string EmptyMessage
        {
            get => (string)GetValue(EmptyMessageProperty);
            set => SetValue(EmptyMessageProperty, value);
        }

        #endregion

        #region EmptyIcon Property

        public static readonly DependencyProperty EmptyIconProperty =
            DependencyProperty.Register(
                nameof(EmptyIcon),
                typeof(object),
                typeof(FathomDataGrid),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed when there is no data.
        /// </summary>
        public object? EmptyIcon
        {
            get => GetValue(EmptyIconProperty);
            set => SetValue(EmptyIconProperty, value);
        }

        #endregion

        #region IsLoading Property

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(FathomDataGrid),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the grid shows a loading state.
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Visual variants for FathomDataGrid.
    /// </summary>
    public enum DataGridVariant
    {
        /// <summary>Default clean data grid.</summary>
        Default,
        /// <summary>Simple minimal design.</summary>
        Simple,
        /// <summary>Striped rows design.</summary>
        Striped,
        /// <summary>Bordered cells design.</summary>
        Bordered
    }
}
