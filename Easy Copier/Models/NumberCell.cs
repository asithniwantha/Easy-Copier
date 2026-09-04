using CommunityToolkit.Mvvm.ComponentModel;

namespace Easy_Copier.Models
{
    public partial class NumberCell : ObservableObject
    {
        [ObservableProperty]
        private string _inputValue = string.Empty;

        [ObservableProperty]
        private bool _isNegative;

        partial void OnInputValueChanged(string value)
        {
            IsNegative = !string.IsNullOrEmpty(value) && value.StartsWith('-');
        }
    }
}
