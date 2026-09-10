using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace Easy_Copier.Views
{
    public sealed partial class SmartAdderControl : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(SmartAdderViewModel),
                typeof(SmartAdderControl),
                new PropertyMetadata(null, OnViewModelChanged));

        public SmartAdderViewModel? ViewModel
        {
            get => (SmartAdderViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public SmartAdderControl()
        {
            InitializeComponent();
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartAdderControl control)
            {
                if (e.OldValue is SmartAdderViewModel oldVm)
                {
                    oldVm.PropertyChanged -= control.ViewModel_PropertyChanged;
                }
                if (e.NewValue is SmartAdderViewModel newVm)
                {
                    newVm.PropertyChanged += control.ViewModel_PropertyChanged;
                    control.DataContext = newVm;
                    control.UpdateVisibility();
                }
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is (nameof(SmartAdderViewModel.IsHovering)) or
                (nameof(SmartAdderViewModel.IsListFocused)))
            {
                UpdateVisibility();
            }
        }

        private void UpdateVisibility()
        {
            if (ViewModel != null)
            {
                InputListPanel.Visibility = (ViewModel.IsHovering || ViewModel.IsListFocused) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
