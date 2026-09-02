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
        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register(nameof(IsHovered), typeof(bool), typeof(HoverBehavior), new PropertyMetadata(false));

        public bool IsHovered
        {
            get => (bool)GetValue(IsHoveredProperty);
            set => SetValue(IsHoveredProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited += AssociatedObject_PointerExited;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
            AssociatedObject.PointerExited -= AssociatedObject_PointerExited;
            base.OnDetaching();
        }

        private void AssociatedObject_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            IsHovered = true;
        }

        private void AssociatedObject_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            IsHovered = false;
        }
    }
}
