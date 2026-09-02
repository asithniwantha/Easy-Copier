using Easy_Copier.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace Easy_Copier.Views
{
    public sealed partial class SmartAdderControl : UserControl
    {
        public SmartAdderViewModel ViewModel { get; }

        public SmartAdderControl()
        {
            InitializeComponent();

            ViewModel = ((App)Microsoft.UI.Xaml.Application.Current).Services.GetRequiredService<SmartAdderViewModel>();
            DataContext = ViewModel;

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateVisibility();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartAdderViewModel.IsHovering) ||
                e.PropertyName == nameof(SmartAdderViewModel.IsListFocused))
            {
                UpdateVisibility();
            }
        }

        private void UpdateVisibility()
        {
            InputListPanel.Visibility = (ViewModel.IsHovering || ViewModel.IsListFocused) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
