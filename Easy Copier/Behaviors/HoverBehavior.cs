using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace Easy_Copier.Behaviors
{
    /// <summary>
    /// Toggles the bound <see cref="IsHovered"/> dependency property while the pointer
    /// is over the associated element, allowing the SmartAdder overlay to expand on hover.
    /// </summary>
    public sealed class HoverBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// Identifies the <see cref="IsHovered"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register(nameof(IsHovered), typeof(bool), typeof(HoverBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the pointer cursor is currently positioned over the associated element.
        /// </summary>
        public bool IsHovered
        {
            get => (bool)GetValue(IsHoveredProperty);
            set => SetValue(IsHoveredProperty, value);
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited += AssociatedObject_PointerExited;
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited -= AssociatedObject_PointerExited;
            base.OnDetaching();
        }

        /// <summary>
        /// Handles the pointer entered event on the associated element to set <see cref="IsHovered"/> to <see langword="true"/>.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">The pointer event arguments.</param>
        private void AssociatedObject_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            IsHovered = true;
        }

        /// <summary>
        /// Handles the pointer exited event on the associated element to set <see cref="IsHovered"/> to <see langword="false"/>.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">The pointer event arguments.</param>
        private void AssociatedObject_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            IsHovered = false;
        }
    }
}
