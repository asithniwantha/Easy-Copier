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
        public partial string Text { get; set; } = string.Empty;

        [ObservableProperty]
        public partial double? Value { get; set; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
    }
}
