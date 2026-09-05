using CommunityToolkit.Mvvm.ComponentModel;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Represents a single dynamic number-entry row within the SmartAdder overlay.
    /// </summary>
    public partial class SmartAdderEntry : ObservableObject
    {
        public int Index { get; set; }

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private double? _value;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
    }
}
