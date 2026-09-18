using CommunityToolkit.Mvvm.ComponentModel;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Represents a single dynamic number-entry row within the SmartAdder overlay.
    /// </summary>
    public partial class SmartAdderEntry : ObservableObject
    {
        /// <summary>
        /// Gets or sets the zero-based index of this entry row.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the text value entered by the user.
        /// </summary>
        [ObservableProperty]
        public partial string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parsed numeric value, or <see langword="null"/> if invalid/empty.
        /// </summary>
        [ObservableProperty]
        public partial double? Value { get; set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="Text"/> is empty or consists only of white-space characters.
        /// </summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
    }
}
