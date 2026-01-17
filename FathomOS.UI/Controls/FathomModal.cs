using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FathomOS.UI.Controls
{
    /// <summary>
    /// FathomOS Design System - Modal Dialog
    /// A styled modal dialog with header, content, and footer sections.
    /// </summary>
    public class FathomModal : ContentControl
    {
        static FathomModal()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FathomModal),
                new FrameworkPropertyMetadata(typeof(FathomModal)));
        }

        public FathomModal()
        {
            // Handle Escape key to close
            KeyDown += OnKeyDown;
        }

        #region Title Property

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(FathomModal),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the modal title.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region Footer Property

        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register(
                nameof(Footer),
                typeof(object),
                typeof(FathomModal),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the footer content (typically action buttons).
        /// </summary>
        public object? Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        #endregion

        #region Size Property

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(ModalSize),
                typeof(FathomModal),
                new PropertyMetadata(ModalSize.Medium));

        /// <summary>
        /// Gets or sets the modal size.
        /// </summary>
        public ModalSize Size
        {
            get => (ModalSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region IsOpen Property

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(FathomModal),
                new PropertyMetadata(false, OnIsOpenChanged));

        /// <summary>
        /// Gets or sets whether the modal is open/visible.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modal = (FathomModal)d;
            modal.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;

            if ((bool)e.NewValue)
            {
                modal.RaiseEvent(new RoutedEventArgs(OpenedEvent));
            }
            else
            {
                modal.RaiseEvent(new RoutedEventArgs(ClosedEvent));
            }
        }

        #endregion

        #region ShowCloseButton Property

        public static readonly DependencyProperty ShowCloseButtonProperty =
            DependencyProperty.Register(
                nameof(ShowCloseButton),
                typeof(bool),
                typeof(FathomModal),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to show the close button in the header.
        /// </summary>
        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        #endregion

        #region CloseOnOverlayClick Property

        public static readonly DependencyProperty CloseOnOverlayClickProperty =
            DependencyProperty.Register(
                nameof(CloseOnOverlayClick),
                typeof(bool),
                typeof(FathomModal),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether clicking the overlay closes the modal.
        /// </summary>
        public bool CloseOnOverlayClick
        {
            get => (bool)GetValue(CloseOnOverlayClickProperty);
            set => SetValue(CloseOnOverlayClickProperty, value);
        }

        #endregion

        #region CloseOnEscape Property

        public static readonly DependencyProperty CloseOnEscapeProperty =
            DependencyProperty.Register(
                nameof(CloseOnEscape),
                typeof(bool),
                typeof(FathomModal),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether pressing Escape closes the modal.
        /// </summary>
        public bool CloseOnEscape
        {
            get => (bool)GetValue(CloseOnEscapeProperty);
            set => SetValue(CloseOnEscapeProperty, value);
        }

        #endregion

        #region Events

        public static readonly RoutedEvent OpenedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Opened),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FathomModal));

        public event RoutedEventHandler Opened
        {
            add => AddHandler(OpenedEvent, value);
            remove => RemoveHandler(OpenedEvent, value);
        }

        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Closed),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FathomModal));

        public event RoutedEventHandler Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        #endregion

        #region Methods

        public void Open()
        {
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && CloseOnEscape && IsOpen)
            {
                Close();
                e.Handled = true;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Wire up overlay click
            if (GetTemplateChild("PART_Overlay") is Border overlay)
            {
                overlay.MouseLeftButtonDown += OnOverlayClick;
            }

            // Wire up close button
            if (GetTemplateChild("PART_CloseButton") is Button closeButton)
            {
                closeButton.Click += (s, e) => Close();
            }
        }

        private void OnOverlayClick(object sender, MouseButtonEventArgs e)
        {
            if (CloseOnOverlayClick)
            {
                Close();
            }
        }

        #endregion
    }

    /// <summary>
    /// Size options for FathomModal.
    /// </summary>
    public enum ModalSize
    {
        /// <summary>Small modal (320px).</summary>
        Small,
        /// <summary>Medium modal (480px).</summary>
        Medium,
        /// <summary>Large modal (640px).</summary>
        Large,
        /// <summary>Extra large modal (800px).</summary>
        ExtraLarge,
        /// <summary>Full width modal.</summary>
        Full
    }
}
