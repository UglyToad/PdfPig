namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;

    internal class DefaultWordExtractor : IWordExtractor
    {
        public IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters)
        {
            var lettersOrder = letters.OrderByDescending(x => x.Origin.Y)
                .ThenBy(x => x.Origin.X);

            var lettersSoFar = new List<Letter>(10);

            var y = default(decimal?);
            var lastX = default(decimal?);
            var lastLetter = default(Letter);
            foreach (var letter in lettersOrder)
            {
                if (!y.HasValue)
                {
                    y = letter.Origin.Y;
                }

                if (!lastX.HasValue)
                {
                    lastX = letter.Origin.X;
                }

                if (lastLetter == null)
                {
                    if (string.IsNullOrWhiteSpace(letter.Value))
                    {
                        continue;
                    }

                    lettersSoFar.Add(letter);
                    lastLetter = letter;
                    continue;
                }

                if (letter.Origin.Y > y.Value + 0.5m)
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

                    y = letter.Origin.Y;
                    lastX = letter.Origin.X;
                    lastLetter = letter;

                    continue;
                }

                var gap = letter.Origin.X - (lastLetter.Origin.X + lastLetter.Width);
                var nextToLeft = letter.Origin.X < lastX.Value - 1;
                var nextBigSpace = gap > Math.Max(lastLetter.GlyphRectangle.Height, letter.GlyphRectangle.Height) * 0.39m;
                var nextIsWhiteSpace = string.IsNullOrWhiteSpace(letter.Value);
                var nextFontDiffers = !string.Equals(letter.FontName, lastLetter.FontName, StringComparison.OrdinalIgnoreCase) && gap > letter.Width * 0.1m;
                var nextFontSizeDiffers = Math.Abs(letter.FontSize - lastLetter.FontSize) > 0.1m;

                if (nextToLeft || nextBigSpace || nextIsWhiteSpace || nextFontDiffers || nextFontSizeDiffers)
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

                lastX = letter.Origin.X;
            }

            if (lettersSoFar.Count > 0)
            {
                yield return GenerateWord(lettersSoFar);
            }
        }

        private static Word GenerateWord(List<Letter> letters)
        {
            return new Word(letters);
        }

        public static IWordExtractor Instance { get; } = new DefaultWordExtractor();

        private DefaultWordExtractor()
        {
        }
    }
}