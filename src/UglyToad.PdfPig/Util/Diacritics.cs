namespace UglyToad.PdfPig.Util
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Helper class for diacritics.
    /// </summary>
    public static class Diacritics
    {
        private static readonly HashSet<string> NonCombiningDiacritics =
        [
            "´",
            "^",
            "ˆ",
            "¨",
            "©",
            "™",
            "®",
            "`",
            "˜",
            "∼",
            "¸"
        ];

        internal static bool IsPotentialStandaloneDiacritic(string value) => NonCombiningDiacritics.Contains(value);

        /// <summary>
        /// Determines whether the specified string contains a single character 
        /// that falls within the Unicode combining diacritic range (U+0300 to U+036F).
        /// </summary>
        /// <param name="value">The string to check. It should contain exactly one character.</param>
        /// <returns>
        /// <c>true</c> if the character in the string is within the combining diacritic range; 
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is useful for identifying characters that are typically used as 
        /// combining diacritics in Unicode text.
        /// </remarks>
        public static bool IsInCombiningDiacriticRange(string value)
        {
            if (value.Length != 1)
            {
                return false;
            }

            var intVal = (int)value[0];

            if (intVal >= 768 && intVal <= 879)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to combine a diacritic character with the preceding letter to form a single combined character.
        /// </summary>
        /// <param name="diacritic">The diacritic character to combine.</param>
        /// <param name="previous">The preceding letter to combine with the diacritic.</param>
        /// <param name="result">
        /// When this method returns, contains the combined character if the combination was successful; 
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the diacritic was successfully combined with the preceding letter; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryCombineDiacriticWithPreviousLetter(string diacritic, string previous, [NotNullWhen(true)] out string? result)
        {
            result = null;

            if (previous is null)
            {
                return false;
            }

            result = previous + diacritic;

            // On combining the length should remain equal.
            var beforeCombination = MeasureDiacriticAwareLength(previous);
            var afterCombination = MeasureDiacriticAwareLength(result);

            return beforeCombination == afterCombination;
        }

        private static int MeasureDiacriticAwareLength(string input)
        {
            var length = 0;

            var enumerator = StringInfo.GetTextElementEnumerator(input);
            while (enumerator.MoveNext())
            {
                var grapheme = enumerator.GetTextElement();
                length++;
            }

            return length;
        }
    }
}
