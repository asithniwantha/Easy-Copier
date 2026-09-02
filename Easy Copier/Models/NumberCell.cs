using CommunityToolkit.Mvvm.ComponentModel;

namespace Easy_Copier.Models
{
    public partial class NumberCell : ObservableObject
    {
        [ObservableProperty]
        public partial string InputValue { get; set; } = string.Empty;
    }
}
