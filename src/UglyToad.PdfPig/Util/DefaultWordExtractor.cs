namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;

    /// <summary>
    /// Default Word Extractor.
    /// </summary>
    public class DefaultWordExtractor : IWordExtractor
    {
        /// <summary>
        /// Gets the words.
        /// </summary>
        /// <param name="letters">The letters in the page.</param>
        public IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters)
        {
            var lettersOrder = letters.OrderByDescending(x => x.Location.Y)
                .ThenBy(x => x.Location.X);

            var lettersSoFar = new List<Letter>(10);

            var gapCountsSoFarByFontSize = new Dictionary<double, Dictionary<double, int>>();

            var y = default(double?);
            var lastX = default(double?);
            var lastLetter = default(Letter);
            foreach (var letter in lettersOrder)
            {
                if (!y.HasValue)
                {
                    y = letter.Location.Y;
                }

                if (!lastX.HasValue)
                {
                    lastX = letter.Location.X;
                }

                if (lastLetter is null)
                {
                    if (string.IsNullOrWhiteSpace(letter.Value))
                    {
                        continue;
                    }

                    lettersSoFar.Add(letter);
                    lastLetter = letter;
                    y = letter.Location.Y;
                    lastX = letter.Location.X;
                    continue;
                }

                if (letter.Location.Y < y.Value - 0.5)
                {
                    if (lettersSoFar.Count > 0)
                    {
                        yield return GenerateWord(lettersSoFar);
                        lettersSoFar.Clear();
                    }

                    if (!string.IsNullOrWhiteSpace(letter.Value))
                    {
                        lettersSoFar.Add(letter);
                    }

                    y = letter.Location.Y;
                    lastX = letter.Location.X;
                    lastLetter = letter;

                    continue;
                }

                var letterHeight = Math.Max(lastLetter.GlyphRectangle.Height, letter.GlyphRectangle.Height);

                var gap = letter.Location.X - (lastLetter.Location.X + lastLetter.Width);
                var nextToLeft = letter.Location.X < lastX.Value - 1;
                var nextBigSpace = gap > letterHeight * 0.39;
                var nextIsWhiteSpace = string.IsNullOrWhiteSpace(letter.Value);
                var nextFontDiffers = !string.Equals(letter.FontName, lastLetter.FontName, StringComparison.OrdinalIgnoreCase) && gap > letter.Width * 0.1;
                var nextFontSizeDiffers = Math.Abs(letter.FontSize - lastLetter.FontSize) > 0.1;
                var nextTextOrientationDiffers = letter.TextOrientation != lastLetter.TextOrientation;

                var suspectGap = false;

                if (!nextFontSizeDiffers && letter.FontSize > 0 && gap >= 0)
                {
                    var fontSize = Math.Round(letter.FontSize);
                    if (!gapCountsSoFarByFontSize.TryGetValue(fontSize, out var gapCounts))
                    {
                        gapCounts = new Dictionary<double, int>();
                        gapCountsSoFarByFontSize[fontSize] = gapCounts;
                    }

                    var gapRounded = Math.Round(gap, 2);
                    if (!gapCounts.ContainsKey(gapRounded))
                    {
                        gapCounts[gapRounded] = 0;
                    }

                    gapCounts[gapRounded]++;

                    // More than one type of gap.
                    if (gapCounts.Count > 1 && gap > letterHeight * 0.16)
                    {
                        var mostCommonGap = gapCounts.OrderByDescending(x => x.Value).First();

                        if (gap > (mostCommonGap.Key * 5) && mostCommonGap.Value > 1)
                        {
                            suspectGap = true;
                        }
                    }
                }

                if (nextToLeft || nextBigSpace || nextIsWhiteSpace || nextFontDiffers || nextFontSizeDiffers || nextTextOrientationDiffers || suspectGap)
                {
                    if (lettersSoFar.Count > 0)
                    {
                        yield return GenerateWord(lettersSoFar);
                        lettersSoFar.Clear();
                    }
                }

                if (!string.IsNullOrWhiteSpace(letter.Value))
                {
                    lettersSoFar.Add(letter);
                }

                lastLetter = letter;

                lastX = letter.Location.X;
            }

            if (lettersSoFar.Count > 0)
            {
                yield return GenerateWord(lettersSoFar);
            }
        }

        private static Word GenerateWord(List<Letter> letters)
        {
            return new Word(letters.ToList());
        }

        /// <summary>
        /// Create an instance of Default Word Extractor, <see cref="DefaultWordExtractor"/>.
        /// </summary>
        public static IWordExtractor Instance { get; } = new DefaultWordExtractor();

        private DefaultWordExtractor()
        {
        }
    }
}