namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.PdfFonts;

    /// <summary>
    /// Checks if each letter is a duplicate and overlaps any other letter and remove the duplicate, and flag the remaining as bold.
    /// <para>Logic inspired from PdfBox's PDFTextStripper class.</para>
    /// </summary>
    public static class DuplicateOverlappingTextProcessor
    {
        /// <summary>
        /// Checks if each letter is a duplicate and overlaps any other letter and remove the duplicate, and flag the remaining as bold.
        /// <para>Logic inspired from PdfBox's PDFTextStripper class.</para>
        /// </summary>
        /// <param name="letters">Letters to be processed.</param>
        /// <returns>Letters with no duplicate overlapping.</returns>
        public static IReadOnlyList<Letter> Get(IEnumerable<Letter> letters)
        {
            if (letters?.Any() != true)
            {
                return letters?.ToList();
            }

            var queue = new Queue<Letter>(letters);
            var cleanLetters = new List<Letter>() { queue.Dequeue() }; // dequeue the first letter

            while (queue.Count > 0)
            {
                var letter = queue.Dequeue();
                bool addLetter = true;
                int duplicatesOverlappingIndex = -1;

                var duplicates = cleanLetters.Where(l => l.Value.Equals(letter.Value) && l.FontName.Equals(letter.FontName)); // do other checks?

                if (duplicates.Any())
                {
                    double tolerance = letter.GlyphRectangle.Width / (letter.Value.Length == 0 ? 1 : letter.Value.Length) / 3.0;
                    double minX = letter.GlyphRectangle.BottomLeft.X - tolerance;
                    double maxX = letter.GlyphRectangle.BottomLeft.X + tolerance;
                    double minY = letter.GlyphRectangle.BottomLeft.Y - tolerance;
                    double maxY = letter.GlyphRectangle.BottomLeft.Y + tolerance;

                    var duplicatesOverlapping = duplicates.FirstOrDefault(l => minX <= l.GlyphRectangle.BottomLeft.X &&
                                                                               maxX >= l.GlyphRectangle.BottomLeft.X &&
                                                                               minY <= l.GlyphRectangle.BottomLeft.Y &&
                                                                               maxY >= l.GlyphRectangle.BottomLeft.Y);

                    if (duplicatesOverlapping != default)
                    {
                        // duplicate overlapping letter was found, keeping the existing one and not adding this one.
                        addLetter = false;
                        duplicatesOverlappingIndex = cleanLetters.IndexOf(duplicatesOverlapping);
                    }
                }

                if (addLetter)
                {
                    cleanLetters.Add(letter);
                }
                else if (duplicatesOverlappingIndex != -1)
                {
                    // TODO: need to update the bounding box
                    // TODO: need to update bottom left/right
                    // TODO: need to update width
                    // update textSequence?

                    // update font details to bold
                    var fontDetails = new FontDetails(letter.Font.Name, letter.Font.FontFamily, true, letter.Font.Weight, letter.Font.IsItalic);

                    var newLetter = new Letter(letter.Value,
                        letter.GlyphRectangle,
                        letter.StartBaseLine,
                        letter.EndBaseLine,
                        letter.Width,
                        letter.FontSize,
                        fontDetails,
                        letter.Color,
                        letter.PointSize,
                        letter.TextSequence);

                    // update markedContentStack?

                    // update letters
                    cleanLetters[duplicatesOverlappingIndex] = newLetter;
                }
            }

            return cleanLetters;
        }
    }
}
