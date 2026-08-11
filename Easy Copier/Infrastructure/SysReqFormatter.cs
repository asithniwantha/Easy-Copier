using System;
using System.Text.RegularExpressions;

namespace Easy_Copier.Infrastructure
{
    public static class SysReqFormatter
    {
        public static string FormatText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            text = Regex.Replace(text, @"(?<!\n)\s*Processor:", "\nCPU:");
            text = Regex.Replace(text, @"(?<!\n)\s*Graphics:", "\nGPU:");
            text = Regex.Replace(text, @"(?<!\n)\s*Memory:", "\nRAM:");
            text = Regex.Replace(text, @"(?<!\n)\s*OS\s*\*?:", "\nOS:");
            text = Regex.Replace(text, @"(?<!\n)\s*Storage:", "\nStorage:");
            text = Regex.Replace(text, @"(?<!\n)\s*DirectX:", "\nDirectX:");
            text = Regex.Replace(text, @"(?<!\n)\s*Sound Card:", "\nSound Card:");
            text = Regex.Replace(text, @"(?<!\n)\s*VR Support:", "\nVR Support:");
            text = Regex.Replace(text, @"(?<!\n)\s*Additional Notes:", "\nAdditional Notes:");
            text = Regex.Replace(text, @"(?<!\n)\s*Requires a 64-bit processor and operating system", "\nRequires a 64-bit processor and operating system");

            // Remove the spurious "CPU:" before "Minimum:" and "Recommended:" if it exists
            text = Regex.Replace(text, @"CPU:\s*Minimum:", "Minimum:");
            text = Regex.Replace(text, @"CPU:\s*Recommended:", "Recommended:");

            text = Regex.Replace(text, @"(?<!\n)\s*Minimum:", "\nMinimum:");
            text = Regex.Replace(text, @"(?<!\n)\s*Recommended:", "\nRecommended:");
            text = text.Replace("&amp;", "&");
            return text;
        }
    }
}
