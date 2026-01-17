using System.Windows;
using System.Windows.Controls;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Skeleton Loader
    /// A loading placeholder that mimics content layout.
    /// Owned by: UI-AGENT
    /// </summary>
    public class SkeletonLoader : Control
    {
        static SkeletonLoader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SkeletonLoader),
                new FrameworkPropertyMetadata(typeof(SkeletonLoader)));
        }

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(SkeletonVariant),
                typeof(SkeletonLoader),
                new PropertyMetadata(SkeletonVariant.Rectangle));

        /// <summary>
        /// Gets or sets the skeleton shape variant.
        /// </summary>
        public SkeletonVariant Variant
        {
            get => (SkeletonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region IsAnimated Property

        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.Register(
                nameof(IsAnimated),
                typeof(bool),
                typeof(SkeletonLoader),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the skeleton has a shimmer animation.
        /// </summary>
        public bool IsAnimated
        {
            get => (bool)GetValue(IsAnimatedProperty);
            set => SetValue(IsAnimatedProperty, value);
        }

        #endregion

        #region AnimationSpeed Property

        public static readonly DependencyProperty AnimationSpeedProperty =
            DependencyProperty.Register(
                nameof(AnimationSpeed),
                typeof(SkeletonAnimationSpeed),
                typeof(SkeletonLoader),
                new PropertyMetadata(SkeletonAnimationSpeed.Normal));

        /// <summary>
        /// Gets or sets the animation speed.
        /// </summary>
        public SkeletonAnimationSpeed AnimationSpeed
        {
            get => (SkeletonAnimationSpeed)GetValue(AnimationSpeedProperty);
            set => SetValue(AnimationSpeedProperty, value);
        }

        #endregion

        #region CornerRadius Property

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(SkeletonLoader),
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

        #region Lines Property

        public static readonly DependencyProperty LinesProperty =
            DependencyProperty.Register(
                nameof(Lines),
                typeof(int),
                typeof(SkeletonLoader),
                new PropertyMetadata(1));

        /// <summary>
        /// Gets or sets the number of lines for text variant.
        /// </summary>
        public int Lines
        {
            get => (int)GetValue(LinesProperty);
            set => SetValue(LinesProperty, value);
        }

        #endregion

        #region LineHeight Property

        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register(
                nameof(LineHeight),
                typeof(double),
                typeof(SkeletonLoader),
                new PropertyMetadata(16.0));

        /// <summary>
        /// Gets or sets the height of each line for text variant.
        /// </summary>
        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        #endregion

        #region LineSpacing Property

        public static readonly DependencyProperty LineSpacingProperty =
            DependencyProperty.Register(
                nameof(LineSpacing),
                typeof(double),
                typeof(SkeletonLoader),
                new PropertyMetadata(8.0));

        /// <summary>
        /// Gets or sets the spacing between lines for text variant.
        /// </summary>
        public double LineSpacing
        {
            get => (double)GetValue(LineSpacingProperty);
            set => SetValue(LineSpacingProperty, value);
        }

        #endregion

        #region AvatarSize Property

        public static readonly DependencyProperty AvatarSizeProperty =
            DependencyProperty.Register(
                nameof(AvatarSize),
                typeof(double),
                typeof(SkeletonLoader),
                new PropertyMetadata(40.0));

        /// <summary>
        /// Gets or sets the avatar size for Avatar variant.
        /// </summary>
        public double AvatarSize
        {
            get => (double)GetValue(AvatarSizeProperty);
            set => SetValue(AvatarSizeProperty, value);
        }

        #endregion

        #region ShowAvatar Property

        public static readonly DependencyProperty ShowAvatarProperty =
            DependencyProperty.Register(
                nameof(ShowAvatar),
                typeof(bool),
                typeof(SkeletonLoader),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show an avatar circle with text lines.
        /// </summary>
        public bool ShowAvatar
        {
            get => (bool)GetValue(ShowAvatarProperty);
            set => SetValue(ShowAvatarProperty, value);
        }

        #endregion

        #region LastLineWidth Property

        public static readonly DependencyProperty LastLineWidthProperty =
            DependencyProperty.Register(
                nameof(LastLineWidth),
                typeof(double),
                typeof(SkeletonLoader),
                new PropertyMetadata(0.6)); // 60% of full width

        /// <summary>
        /// Gets or sets the width factor of the last line (0.0 to 1.0).
        /// </summary>
        public double LastLineWidth
        {
            get => (double)GetValue(LastLineWidthProperty);
            set => SetValue(LastLineWidthProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Shape variants for SkeletonLoader.
    /// </summary>
    public enum SkeletonVariant
    {
        /// <summary>Rectangle placeholder.</summary>
        Rectangle,
        /// <summary>Circle placeholder (for avatars).</summary>
        Circle,
        /// <summary>Text lines placeholder.</summary>
        Text,
        /// <summary>Card layout placeholder.</summary>
        Card,
        /// <summary>List item placeholder.</summary>
        ListItem,
        /// <summary>Table row placeholder.</summary>
        TableRow,
        /// <summary>Button placeholder.</summary>
        Button,
        /// <summary>Image placeholder.</summary>
        Image
    }

    /// <summary>
    /// Animation speed options for SkeletonLoader.
    /// </summary>
    public enum SkeletonAnimationSpeed
    {
        /// <summary>Slow shimmer (2s).</summary>
        Slow,
        /// <summary>Normal shimmer (1.5s).</summary>
        Normal,
        /// <summary>Fast shimmer (1s).</summary>
        Fast
    }
}
