using CommunityToolkit.Mvvm.ComponentModel;

namespace Easy_Copier.Models
{
    public partial class NumberCell : ObservableObject
    {
        [ObservableProperty]
        public partial string InputValue { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsNegative { get; set; }

        partial void OnInputValueChanged(string value)
        {
            IsNegative = !string.IsNullOrEmpty(value) && value.StartsWith('-');
        }
    }
}
