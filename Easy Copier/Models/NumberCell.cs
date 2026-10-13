using CommunityToolkit.Mvvm.ComponentModel;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Represents an individual cell within the SmartAdder grid, tracking user input and negative state.
    /// </summary>
    public partial class NumberCell : ObservableObject
    {
        /// <summary>
        /// Gets or sets the text input value of the cell.
        /// </summary>
        [ObservableProperty]
        public partial string InputValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the input value represents a negative number.
        /// </summary>
        [ObservableProperty]
        public partial bool IsNegative { get; set; }

        /// <summary>
        /// Called when <see cref="InputValue"/> changes to update <see cref="IsNegative"/>.
        /// </summary>
        /// <param name="value">The new input string value.</param>
        partial void OnInputValueChanged(string value)
        {
            IsNegative = !string.IsNullOrEmpty(value) && value.StartsWith('-');
        }

        /// <summary>
        /// Attempts to parse the numeric value from <see cref="InputValue"/> using standard invariant float formatting.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed numeric value if successful; otherwise 0.</param>
        /// <returns><c>true</c> if <see cref="InputValue"/> was successfully parsed; otherwise <c>false</c>.</returns>
        public bool TryGetNumericValue(out double value)
        {
            if (string.IsNullOrWhiteSpace(InputValue))
            {
                value = 0;
                return false;
            }

            return double.TryParse(
                InputValue,
                System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }
    }
}
