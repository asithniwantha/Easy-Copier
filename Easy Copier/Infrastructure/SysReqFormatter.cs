using System;
using System.Text.RegularExpressions;

namespace Easy_Copier.Infrastructure
{
    public static partial class SysReqFormatter
    {
        [GeneratedRegex(@"(?<!\n)\s*Processor:")]
        private static partial Regex ProcessorRegex();

        [GeneratedRegex(@"(?<!\n)\s*Graphics:")]
        private static partial Regex GraphicsRegex();

        [GeneratedRegex(@"(?<!\n)\s*Memory:")]
        private static partial Regex MemoryRegex();

        [GeneratedRegex(@"(?<!\n)\s*OS\s*\*?:")]
        private static partial Regex OsRegex();

        [GeneratedRegex(@"(?<!\n)\s*Storage:")]
        private static partial Regex StorageRegex();

        [GeneratedRegex(@"(?<!\n)\s*DirectX:")]
        private static partial Regex DirectXRegex();

        [GeneratedRegex(@"(?<!\n)\s*Sound Card:")]
        private static partial Regex SoundCardRegex();

        [GeneratedRegex(@"(?<!\n)\s*VR Support:")]
        private static partial Regex VrSupportRegex();

        [GeneratedRegex(@"(?<!\n)\s*Additional Notes:")]
        private static partial Regex AdditionalNotesRegex();

        [GeneratedRegex(@"(?<!\n)\s*Requires a 64-bit processor and operating system")]
        private static partial Regex Requires64BitRegex();

        [GeneratedRegex(@"CPU:\s*Minimum:")]
        private static partial Regex CpuMinimumRegex();

        [GeneratedRegex(@"CPU:\s*Recommended:")]
        private static partial Regex CpuRecommendedRegex();

        [GeneratedRegex(@"(?<!\n)\s*Minimum:")]
        private static partial Regex MinimumRegex();

        [GeneratedRegex(@"(?<!\n)\s*Recommended:")]
        private static partial Regex RecommendedRegex();

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
