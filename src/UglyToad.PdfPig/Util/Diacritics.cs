namespace UglyToad.PdfPig.Util
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    internal static class Diacritics
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

        public static bool IsPotentialStandaloneDiacritic(string value) => NonCombiningDiacritics.Contains(value);

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
