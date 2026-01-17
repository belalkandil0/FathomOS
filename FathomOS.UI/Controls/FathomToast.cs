using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Toast/Notification
    /// A non-modal notification for alerts and messages.
    /// Owned by: UI-AGENT
    /// </summary>
    public class FathomToast : ContentControl
    {
        static FathomToast()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomToast),
                new FrameworkPropertyMetadata(typeof(FathomToast)));
        }

        #region Events

        /// <summary>
        /// Occurs when the toast is closed.
        /// </summary>
        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent(
                "Closed",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FathomToast));

        public event RoutedEventHandler Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        /// <summary>
        /// Occurs when an action button is clicked.
        /// </summary>
        public static readonly RoutedEvent ActionClickedEvent =
            EventManager.RegisterRoutedEvent(
                "ActionClicked",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FathomToast));

        public event RoutedEventHandler ActionClicked
        {
            add => AddHandler(ActionClickedEvent, value);
            remove => RemoveHandler(ActionClickedEvent, value);
        }

        #endregion

        #region Variant Property

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ToastVariant),
                typeof(FathomToast),
                new PropertyMetadata(ToastVariant.Default));

        /// <summary>
        /// Gets or sets the toast variant/type.
        /// </summary>
        public ToastVariant Variant
        {
            get => (ToastVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        #endregion

        #region Title Property

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(FathomToast),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the toast title.
        /// </summary>
        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region Message Property

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(FathomToast),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the toast message.
        /// </summary>
        public string? Message
        {
            get => (string?)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        #endregion

        #region Icon Property

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(FathomToast),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the toast icon.
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
                typeof(FathomToast),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the toast shows a close button.
        /// </summary>
        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        #endregion

        #region ShowProgress Property

        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register(
                nameof(ShowProgress),
                typeof(bool),
                typeof(FathomToast),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether to show a progress indicator for auto-dismiss.
        /// </summary>
        public bool ShowProgress
        {
            get => (bool)GetValue(ShowProgressProperty);
            set => SetValue(ShowProgressProperty, value);
        }

        #endregion

        #region Duration Property

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(TimeSpan),
                typeof(FathomToast),
                new PropertyMetadata(TimeSpan.FromSeconds(5)));

        /// <summary>
        /// Gets or sets the auto-dismiss duration. Set to TimeSpan.Zero for persistent toast.
        /// </summary>
        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        #endregion

        #region ActionText Property

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(
                nameof(ActionText),
                typeof(string),
                typeof(FathomToast),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the action button text.
        /// </summary>
        public string? ActionText
        {
            get => (string?)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        #endregion

        #region ActionCommand Property

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(
                nameof(ActionCommand),
                typeof(ICommand),
                typeof(FathomToast),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the action button command.
        /// </summary>
        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        #endregion

        #region CloseCommand Property

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(
                nameof(CloseCommand),
                typeof(ICommand),
                typeof(FathomToast),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the close button command.
        /// </summary>
        public ICommand? CloseCommand
        {
            get => (ICommand?)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        #endregion

        #region Position Property

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(
                nameof(Position),
                typeof(ToastPosition),
                typeof(FathomToast),
                new PropertyMetadata(ToastPosition.TopRight));

        /// <summary>
        /// Gets or sets the toast position on screen.
        /// </summary>
        public ToastPosition Position
        {
            get => (ToastPosition)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Closes the toast notification.
        /// </summary>
        public void Close()
        {
            RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
        }

        #endregion
    }

    /// <summary>
    /// Toast notification variants.
    /// </summary>
    public enum ToastVariant
    {
        /// <summary>Default neutral toast.</summary>
        Default,
        /// <summary>Success toast.</summary>
        Success,
        /// <summary>Warning toast.</summary>
        Warning,
        /// <summary>Error/danger toast.</summary>
        Error,
        /// <summary>Informational toast.</summary>
        Info,
        /// <summary>Loading/progress toast.</summary>
        Loading
    }

    /// <summary>
    /// Toast position on screen.
    /// </summary>
    public enum ToastPosition
    {
        /// <summary>Top left corner.</summary>
        TopLeft,
        /// <summary>Top center.</summary>
        TopCenter,
        /// <summary>Top right corner.</summary>
        TopRight,
        /// <summary>Bottom left corner.</summary>
        BottomLeft,
        /// <summary>Bottom center.</summary>
        BottomCenter,
        /// <summary>Bottom right corner.</summary>
        BottomRight
    }
}
