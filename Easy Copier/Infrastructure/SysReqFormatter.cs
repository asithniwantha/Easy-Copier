using System;
using System.Text.RegularExpressions;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides text formatting and normalization methods for system requirement descriptions.
    /// </summary>
    public static partial class SysReqFormatter
    {
        /// <summary>
        /// Gets the compiled regular expression for matching processor headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Processor:")]
        private static partial Regex ProcessorRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching graphics headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Graphics:")]
        private static partial Regex GraphicsRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching memory headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Memory:")]
        private static partial Regex MemoryRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching OS headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*OS\s*\*?:")]
        private static partial Regex OsRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching storage headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Storage:")]
        private static partial Regex StorageRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching DirectX headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*DirectX:")]
        private static partial Regex DirectXRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching sound card headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Sound Card:")]
        private static partial Regex SoundCardRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching VR support headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*VR Support:")]
        private static partial Regex VrSupportRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching additional notes headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Additional Notes:")]
        private static partial Regex AdditionalNotesRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching 64-bit architecture requirement headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Requires a 64-bit processor and operating system")]
        private static partial Regex Requires64BitRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching minimum CPU headers.
        /// </summary>
        [GeneratedRegex(@"CPU:\s*Minimum:")]
        private static partial Regex CpuMinimumRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching recommended CPU headers.
        /// </summary>
        [GeneratedRegex(@"CPU:\s*Recommended:")]
        private static partial Regex CpuRecommendedRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching minimum requirement headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Minimum:")]
        private static partial Regex MinimumRegex();

        /// <summary>
        /// Gets the compiled regular expression for matching recommended requirement headers not preceded by a newline.
        /// </summary>
        [GeneratedRegex(@"(?<!\n)\s*Recommended:")]
        private static partial Regex RecommendedRegex();

        /// <summary>
        /// Formats and standardizes raw system requirement strings by adding line breaks before key headers.
        /// </summary>
        /// <param name="text">The raw system requirements text to format.</param>
        /// <returns>The formatted and normalized system requirements string.</returns>
        public static string FormatText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            text = ProcessorRegex().Replace(text, "\nCPU:");
            text = GraphicsRegex().Replace(text, "\nGPU:");
            text = MemoryRegex().Replace(text, "\nRAM:");
            text = OsRegex().Replace(text, "\nOS:");
            text = StorageRegex().Replace(text, "\nStorage:");
            text = DirectXRegex().Replace(text, "\nDirectX:");
            text = SoundCardRegex().Replace(text, "\nSound Card:");
            text = VrSupportRegex().Replace(text, "\nVR Support:");
            text = AdditionalNotesRegex().Replace(text, "\nAdditional Notes:");
            text = Requires64BitRegex().Replace(text, "\nRequires a 64-bit processor and operating system");

            // Remove the spurious "CPU:" before "Minimum:" and "Recommended:" if it exists
            text = CpuMinimumRegex().Replace(text, "Minimum:");
            text = CpuRecommendedRegex().Replace(text, "Recommended:");

            text = MinimumRegex().Replace(text, "\nMinimum:");
            text = RecommendedRegex().Replace(text, "\nRecommended:");

            text = text.Replace("&amp;", "&", StringComparison.Ordinal);

            return text;
        }
    }
}
