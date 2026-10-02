using Microsoft.UI.Xaml;

namespace Easy_Copier.Behaviors
{
    /// <summary>
    /// Toggles the bound <see cref="IsFocusWithin"/> dependency property while any descendant
    /// element of the associated element holds keyboard focus, so the SmartAdder overlay stays
    /// expanded while the user is editing an entry.
    /// </summary>
    public sealed class FocusWithinBehavior : Microsoft.Xaml.Interactivity.Behavior<FrameworkElement>
    {
        /// <summary>
        /// Identifies the <see cref="IsFocusWithin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFocusWithinProperty =
            DependencyProperty.Register(nameof(IsFocusWithin), typeof(bool), typeof(FocusWithinBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether keyboard focus is currently within the associated control or any of its descendants.
        /// </summary>
        public bool IsFocusWithin
        {
            get => (bool)GetValue(IsFocusWithinProperty);
            set => SetValue(IsFocusWithinProperty, value);
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.GotFocus += AssociatedObject_GotFocus;
            AssociatedObject.LostFocus += AssociatedObject_LostFocus;
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
            AssociatedObject.LostFocus -= AssociatedObject_LostFocus;
            base.OnDetaching();
        }

        private void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
        {
            IsFocusWithin = true;
        }

        private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
        {
            IsFocusWithin = false;
        }
    }
}
