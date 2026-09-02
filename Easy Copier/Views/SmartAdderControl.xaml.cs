using Easy_Copier.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Easy_Copier.Views
{
    public sealed partial class SmartAdderControl : UserControl
    {
        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register(nameof(IsHovered), typeof(bool), typeof(SmartAdderControl), new PropertyMetadata(false, OnHoverOrFocusChanged));

        public static readonly DependencyProperty IsFocusWithinProperty =
            DependencyProperty.Register(nameof(IsFocusWithin), typeof(bool), typeof(SmartAdderControl), new PropertyMetadata(false, OnHoverOrFocusChanged));

        public SmartAdderViewModel ViewModel { get; }

        public bool IsHovered
        {
            get => (bool)GetValue(IsHoveredProperty);
            set => SetValue(IsHoveredProperty, value);
        }

        public bool IsFocusWithin
        {
            get => (bool)GetValue(IsFocusWithinProperty);
            set => SetValue(IsFocusWithinProperty, value);
        }

        public SmartAdderControl()
        {
            InitializeComponent();

            ViewModel = ((App)Microsoft.UI.Xaml.Application.Current).Services.GetRequiredService<SmartAdderViewModel>();
            DataContext = ViewModel;
        }

        private static void OnHoverOrFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartAdderControl control)
            {
                control.ViewModel.IsExpanded = control.IsHovered || control.IsFocusWithin;
            }
        }
    }
}
