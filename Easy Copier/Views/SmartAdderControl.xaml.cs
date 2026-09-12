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
                if (e.NewValue is SmartAdderViewModel newVm)
                {
                    control.DataContext = newVm;
                }
            }
        }
    }
}
