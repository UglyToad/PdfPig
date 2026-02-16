namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;

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

            // Use a dictionary keyed by (Value, FontName) to look up candidate duplicates in O(1)
            var duplicateIndex = new Dictionary<(string, string), List<int>>();
            var cleanLetters = new List<Letter>();

            foreach (var letter in letters)
            {
                bool addLetter = true;
                int duplicatesOverlappingIndex = -1;

                var key = (letter.Value, letter.FontName);
                if (duplicateIndex.TryGetValue(key, out var candidateIndices))
                {
                    double tolerance = letter.GlyphRectangle.Width / (letter.Value.Length == 0 ? 1 : letter.Value.Length) / 3.0;
                    double minX = letter.GlyphRectangle.BottomLeft.X - tolerance;
                    double maxX = letter.GlyphRectangle.BottomLeft.X + tolerance;
                    double minY = letter.GlyphRectangle.BottomLeft.Y - tolerance;
                    double maxY = letter.GlyphRectangle.BottomLeft.Y + tolerance;

                    for (int ci = 0; ci < candidateIndices.Count; ci++)
                    {
                        int idx = candidateIndices[ci];
                        var l = cleanLetters[idx];
                        if (minX <= l.GlyphRectangle.BottomLeft.X &&
                            maxX >= l.GlyphRectangle.BottomLeft.X &&
                            minY <= l.GlyphRectangle.BottomLeft.Y &&
                            maxY >= l.GlyphRectangle.BottomLeft.Y)
                        {
                            addLetter = false;
                            duplicatesOverlappingIndex = idx;
                            break;
                        }
                    }
                }

                if (addLetter)
                {
                    int newIndex = cleanLetters.Count;
                    cleanLetters.Add(letter);

                    if (!duplicateIndex.TryGetValue(key, out var list))
                    {
                        list = new List<int>();
                        duplicateIndex[key] = list;
                    }
                    list.Add(newIndex);
                }
                else if (duplicatesOverlappingIndex != -1)
                {
                    // TODO: need to update the bounding box
                    // TODO: need to update bottom left/right
                    // TODO: need to update width
                    // update textSequence?

                    // update font details to bold

                    // update markedContentStack?

                    // update letters
                    cleanLetters[duplicatesOverlappingIndex] = letter.AsBold();
                }
            }

            return cleanLetters;
        }
    }
}
