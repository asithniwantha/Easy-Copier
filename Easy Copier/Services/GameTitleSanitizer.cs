using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Utility class providing game title sanitization, candidate title generation, sequel installment analysis,
    /// title match scoring, and image payload validation for cover art retrieval.
    /// </summary>
    public static partial class GameTitleSanitizer
    {
        /// <summary>
        /// Regular expression matching scene release groups and repack tags commonly appended to game folder names.
        /// </summary>
        [GeneratedRegex(@"\[.*?(?:repack|fitgirl|dodi|chovka|kaos|tinyrepack|gog|steam|epic|elamigos|rip|portable|cracked|lossless|multi\d*).*?\]", RegexOptions.IgnoreCase)]
        private static partial Regex RepackAndBracketTagsRegex();

        /// <summary>
        /// Regular expression matching trailing scene release group names (e.g. -CODEX, -EMPRESS, -RUNE).
        /// </summary>
        [GeneratedRegex(@"-(?:codex|empress|cpy|skidrow|reloaded|prophet|plaza|rune|tenoke|flt|razor1911|tinyiso|deviance|darksiders|hoodlum|vace|dino|fairlight|i_know|unleashed|outlaws|alias|goldberg|rld|bat|simplex|chronos|elamigos)\b", RegexOptions.IgnoreCase)]
        private static partial Regex SceneGroupSuffixRegex();

        /// <summary>
        /// Regular expression matching build numbers, patch versions, and update identifiers.
        /// </summary>
        [GeneratedRegex(@"(?:\b(?:v|build|update|patch|hotfix)\s*[\d\.]+[a-z]?\b)|(?:\(build\s*\d+\))|(?:\(v[\d\.]+\))", RegexOptions.IgnoreCase)]
        private static partial Regex VersionAndBuildRegex();

        /// <summary>
        /// Regular expression matching parentheses containing non-essential metadata (e.g. cracked, portable, setup, multi-lang).
        /// Note: Disambiguation release years like (2016) are preserved.
        /// </summary>
        [GeneratedRegex(@"\((?!(?:19|20)\d{2}\b)[^)]*(?:dlc|pack|portable|cracked|setup|rip|bonus|multi|v\d)[^)]*\)", RegexOptions.IgnoreCase)]
        private static partial Regex MetadataParenthesesRegex();

        /// <summary>
        /// Regular expression matching common edition suffixes that should be stripped when falling back to base game titles.
        /// </summary>
        [GeneratedRegex(@"\b(?:digital\s+deluxe|deluxe|complete|definitive|collector'?s?|game\s+of\s+the\s+year|goty|standard|enhanced|director'?s?\s+cut|remastered|anniversary|special|day\s+one|legacy|premium|ultimate|prepare\s+to\s+die|scholar\s+of\s+the\s+first\s+sin)\s+edition\b|\bgoty\b|\bremastered\b|\bremake\b|\bdirector'?s?\s+cut\b|\bprepare\s+to\s+die\b", RegexOptions.IgnoreCase)]
        private static partial Regex EditionSuffixesRegex();

        /// <summary>
        /// Regular expression matching four-digit years in parentheses or at word boundaries (e.g. (2023)).
        /// </summary>
        [GeneratedRegex(@"\b\((?:19|20)\d{2}\)|\b(?:19|20)\d{2}\b")]
        private static partial Regex YearRegex();

        /// <summary>
        /// Mapping of uppercase Roman numerals (up to 20) to their corresponding decimal integer values.
        /// </summary>
        private static readonly Dictionary<string, int> RomanNumerals = new(StringComparer.OrdinalIgnoreCase)
        {
            { "I", 1 }, { "II", 2 }, { "III", 3 }, { "IV", 4 }, { "V", 5 },
            { "VI", 6 }, { "VII", 7 }, { "VIII", 8 }, { "IX", 9 }, { "X", 10 },
            { "XI", 11 }, { "XII", 12 }, { "XIII", 13 }, { "XIV", 14 }, { "XV", 15 },
            { "XVI", 16 }, { "XVII", 17 }, { "XVIII", 18 }, { "XIX", 19 }, { "XX", 20 }
        };

        /// <summary>
        /// Negative keywords in candidate titles that typically indicate non-game media (soundtracks, DLCs, servers, demos).
        /// </summary>
        private static readonly string[] NegativeKeywords =
        [
            "soundtrack", "original soundtrack", " ost ", "score", "dlc", "expansion pass",
            "season pass", "artbook", "artwork", "playtest", "dedicated server", " server",
            "trailer", "teaser", "wallpaper", "demo", "benchmark"
        ];

        /// <summary>
        /// Generates a prioritized list of search candidate queries for a given raw game folder or title string.
        /// </summary>
        /// <param name="rawName">The raw game folder name or file name.</param>
        /// <returns>A read-only list of candidate titles in descending order of query priority.</returns>
        public static IReadOnlyList<string> GenerateSearchCandidates(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return Array.Empty<string>();
            }

            List<string> candidates = [];

            // 1. Primary cleaned title
            string cleaned = CleanTitle(rawName);
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                candidates.Add(cleaned);

                // 2. Base title with edition suffixes stripped (e.g. "The Witcher 3: Wild Hunt Complete Edition" -> "The Witcher 3: Wild Hunt")
                string withoutEdition = StripEditions(cleaned);
                if (!string.IsNullOrWhiteSpace(withoutEdition) && !string.Equals(withoutEdition, cleaned, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(withoutEdition);
                }

                // 3. Title with year stripped (e.g. "Dead Space (2023)" -> "Dead Space")
                string withoutYear = YearRegex().Replace(cleaned, "").Trim();
                withoutYear = CleanPunctuation(withoutYear);
                if (!string.IsNullOrWhiteSpace(withoutYear) && !string.Equals(withoutYear, cleaned, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(withoutYear);

                    string withoutYearAndEdition = StripEditions(withoutYear);
                    if (!string.IsNullOrWhiteSpace(withoutYearAndEdition) && !string.Equals(withoutYearAndEdition, withoutYear, StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add(withoutYearAndEdition);
                    }
                }

                // 4. Subtitle stripped candidate if separated by colon or dash (e.g. "Baldur's Gate III: Enhanced Edition" -> "Baldur's Gate III")
                int sepIndex = cleaned.IndexOfAny([':', '–', '-']);
                if (sepIndex > 3)
                {
                    string baseBeforeSep = CleanPunctuation(cleaned[..sepIndex]);
                    if (!string.IsNullOrWhiteSpace(baseBeforeSep) && baseBeforeSep.Length >= 4)
                    {
                        candidates.Add(baseBeforeSep);
                    }
                }
            }

            // Fallback: original raw name trimmed if not already included
            string rawTrimmed = rawName.Trim();
            if (!string.IsNullOrWhiteSpace(rawTrimmed))
            {
                candidates.Add(rawTrimmed);
            }

            // Deduplicate preserving insertion order
            return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Cleans a raw folder or file name by stripping repack tags, scene suffixes, version tags, and normalizes separators.
        /// </summary>
        /// <param name="rawName">The raw game name string.</param>
        /// <returns>A cleaned, human-readable game title.</returns>
        public static string CleanTitle(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return string.Empty;
            }

            string result = rawName;

            // Strip repack and metadata brackets [FitGirl Repack], [DODI], [MULTi12], etc.
            result = RepackAndBracketTagsRegex().Replace(result, " ");

            // Strip scene group suffixes -CODEX, -EMPRESS, etc.
            result = SceneGroupSuffixRegex().Replace(result, " ");

            // Strip version and build strings (v1.0.4, build 12345, etc.)
            result = VersionAndBuildRegex().Replace(result, " ");

            // Strip non-essential metadata in parentheses
            result = MetadataParenthesesRegex().Replace(result, " ");

            // Normalize dots and underscores used as word separators (e.g. Grand.Theft.Auto.V -> Grand Theft Auto V)
            // Preserve decimal numbers (e.g. 2.0) by checking context
            result = Regex.Replace(result, @"(?<=[a-zA-Z])[\._](?=[a-zA-Z0-9])|(?<=[a-zA-Z0-9])[\._](?=[a-zA-Z])", " ");

            // Clean residual punctuation and multiple spaces
            return CleanPunctuation(result);
        }

        /// <summary>
        /// Strips common game edition keywords (e.g. "Game of the Year Edition", "Deluxe Edition", "Remastered") from a title.
        /// </summary>
        /// <param name="title">The game title string.</param>
        /// <returns>The game title without edition suffixes.</returns>
        public static string StripEditions(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            string stripped = EditionSuffixesRegex().Replace(title, " ");
            return CleanPunctuation(stripped);
        }

        /// <summary>
        /// Cleans multiple spaces and edge punctuation from a title string.
        /// </summary>
        /// <param name="input">The raw input string.</param>
        /// <returns>A clean string.</returns>
        private static string CleanPunctuation(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Collapse multiple spaces
            string text = Regex.Replace(input, @"\s+", " ").Trim();

            // Trim leading/trailing punctuation characters commonly left after stripping
            text = text.Trim('-', '–', '_', '.', ':', ';', ',', '~', ' ');
            return text;
        }

        /// <summary>
        /// Extracts an explicit sequel or installment number (Arabic or Roman numeral) from a game title.
        /// Returns <c>null</c> if no sequel number is detected (e.g. for base/initial games like "Dark Souls", "Doom").
        /// </summary>
        /// <param name="title">The game title to inspect.</param>
        /// <returns>The sequel number as an integer if present; otherwise, <c>null</c>.</returns>
        public static int? ExtractSequelNumber(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            // Strip edition suffixes and repacks first (e.g. "Dark Souls: Remastered" -> "Dark Souls", "Final Fantasy VII Remake" -> "Final Fantasy VII")
            string cleaned = StripEditions(title);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                cleaned = title;
            }

            // Strip 4-digit release years (e.g. (2023), 2016) so they are not treated as sequel numbers
            cleaned = YearRegex().Replace(cleaned, " ").Trim();

            // Focus on the main franchise title before any subtitle separator (: or -)
            int subtitleIdx = cleaned.IndexOfAny([':', '–', '-']);
            string basePart = subtitleIdx > 0 ? cleaned[..subtitleIdx].Trim() : cleaned;

            // Tokenize by non-alphanumeric boundaries
            string[] tokens = Regex.Split(basePart, @"[^\w]+").Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            if (tokens.Length == 0)
            {
                return null;
            }

            // Check the last token of the base title (e.g. "Dark Souls II" -> "II", "Left 4 Dead 2" -> "2", "Grand Theft Auto V" -> "V")
            string lastToken = tokens[^1];

            // 1. Is it a Roman numeral?
            if (RomanNumerals.TryGetValue(lastToken, out int romanVal))
            {
                return romanVal;
            }

            // 2. Is it an Arabic numeral?
            if (int.TryParse(lastToken, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int arabicVal))
            {
                // Numbers between 1 and 99 are valid sequel numbers (exclude years or franchise names like 2077 or 40000)
                if (arabicVal >= 1 && arabicVal <= 99)
                {
                    return arabicVal;
                }
            }

            // Also check for explicit "Part X", "Episode X", "Vol X", "Chapter X" in the full title
            Match partMatch = Regex.Match(title, @"\b(?:part|episode|vol|volume|chapter)\s+([ivx\d]+)\b", RegexOptions.IgnoreCase);
            if (partMatch.Success)
            {
                string numStr = partMatch.Groups[1].Value;
                if (RomanNumerals.TryGetValue(numStr, out int pRoman))
                {
                    return pRoman;
                }
                if (int.TryParse(numStr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int pArabic) && pArabic >= 1 && pArabic <= 99)
                {
                    return pArabic;
                }
            }

            return null;
        }

        /// <summary>
        /// Computes a title match similarity score between a target query title and an online service candidate result.
        /// Returns a value between 0.0 (no match) and 1.0 (exact match).
        /// Enforces strict sequel installment matching to prevent matching sequels (e.g. "Dark Souls II") to original titles ("Dark Souls").
        /// </summary>
        /// <param name="targetTitle">The target game title being queried.</param>
        /// <param name="candidateTitle">The candidate title returned by an online service.</param>
        /// <returns>A similarity score between 0.0 and 1.0.</returns>
        public static double ComputeMatchScore(string targetTitle, string candidateTitle)
        {
            if (string.IsNullOrWhiteSpace(targetTitle) || string.IsNullOrWhiteSpace(candidateTitle))
            {
                return 0.0;
            }

            // 1. Sequel / Installment Discrepancy Check:
            // Prevents "Dark Souls" from matching "Dark Souls II" or "Dark Souls III", and vice-versa
            int? targetSequel = ExtractSequelNumber(targetTitle);
            int? candidateSequel = ExtractSequelNumber(candidateTitle);

            // If target is unnumbered / game 1, but candidate is an explicit sequel >= 2: reject!
            if ((targetSequel == null || targetSequel == 1) && candidateSequel is >= 2)
            {
                return 0.0;
            }

            // If target is an explicit sequel >= 2, but candidate is unnumbered / game 1: reject!
            if (targetSequel is >= 2 && (candidateSequel == null || candidateSequel == 1))
            {
                return 0.0;
            }

            // If both specify sequels >= 2, but they do not match (e.g. Part 2 vs Part 3): reject!
            if (targetSequel is >= 2 && candidateSequel is >= 2 && targetSequel != candidateSequel)
            {
                return 0.0;
            }

            string normTarget = NormalizeForComparison(targetTitle);
            string normCandidate = NormalizeForComparison(candidateTitle);

            if (normTarget.Length == 0 || normCandidate.Length == 0)
            {
                return 0.0;
            }

            // 2. Check if candidate contains negative keywords (e.g. Soundtrack, DLC, Demo) that target does not
            foreach (string neg in NegativeKeywords)
            {
                if (normCandidate.Contains(neg, StringComparison.OrdinalIgnoreCase) &&
                    !normTarget.Contains(neg, StringComparison.OrdinalIgnoreCase))
                {
                    // Severe penalty: reject non-game asset
                    return 0.0;
                }
            }

            // Exact normalized match
            if (string.Equals(normTarget, normCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }

            // Token overlap calculation
            string[] targetTokens = normTarget.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] candidateTokens = normCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (targetTokens.Length == 0 || candidateTokens.Length == 0)
            {
                return 0.0;
            }

            int matchingTokens = targetTokens.Count(t => candidateTokens.Contains(t, StringComparer.OrdinalIgnoreCase));
            double tokenDice = (2.0 * matchingTokens) / (targetTokens.Length + candidateTokens.Length);

            // Coverage penalty: ensure most of the target's distinctive title words are represented
            double targetCoverage = (double)matchingTokens / targetTokens.Length;
            if (targetCoverage < 0.65)
            {
                tokenDice *= targetCoverage;
            }

            // Whole-word prefix match bonus
            if (normCandidate.StartsWith(normTarget + " ", StringComparison.OrdinalIgnoreCase) ||
                normTarget.StartsWith(normCandidate + " ", StringComparison.OrdinalIgnoreCase))
            {
                double lengthRatio = Math.Min((double)normTarget.Length / normCandidate.Length, (double)normCandidate.Length / normTarget.Length);
                return Math.Max(tokenDice, 0.70 * lengthRatio + 0.30);
            }

            return tokenDice;
        }

        /// <summary>
        /// Normalizes a title for comparison by uppercasing, removing punctuation, and collapsing whitespace.
        /// </summary>
        /// <param name="input">The raw string to normalize.</param>
        /// <returns>A normalized string suitable for fuzzy comparison.</returns>
        private static string NormalizeForComparison(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string upper = input.ToUpperInvariant();
            // Replace non-alphanumeric with spaces
            char[] chars = upper.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray();
            string cleaned = new(chars);
            return Regex.Replace(cleaned, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Validates that raw byte data represents a legitimate, non-empty image (JPEG, PNG, WebP, BMP)
        /// and is not an HTML error response or 1x1 spacer pixel.
        /// </summary>
        /// <param name="bytes">The byte array payload to inspect.</param>
        /// <returns><c>true</c> if the bytes represent a valid image with adequate size; otherwise, <c>false</c>.</returns>
        public static bool IsValidImage(byte[]? bytes)
        {
            // Minimum size check: covers must be at least 1500 bytes (real covers are 10KB - 500KB)
            if (bytes == null || bytes.Length < 1500)
            {
                return false;
            }

            // Check for HTML / XML error responses (e.g. <!DOCTYPE, <html, <?xml)
            if (bytes[0] == '<' || bytes[0] == '{')
            {
                return false;
            }

            // JPEG magic bytes: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                return true;
            }

            // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            {
                return true;
            }

            // WebP magic bytes: 'RIFF' .... 'WEBP'
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            {
                return true;
            }

            // BMP magic bytes: 'BM' (42 4D)
            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            {
                return true;
            }

            return false;
        }
    }
}
