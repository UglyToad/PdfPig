namespace UglyToad.PdfPig.PdfFonts
{
    using Core;

    /// <summary>
    /// CharacterBoundingBox
    /// </summary>
    public class CharacterBoundingBox
    {
        /// <summary>
        /// GlyphBounds
        /// </summary>
        public PdfRectangle GlyphBounds { get; }

        /// <summary>
        /// Width
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// CharacterBoundingBox
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="width"></param>
        public CharacterBoundingBox(PdfRectangle bounds, double width)
        {
            GlyphBounds = bounds;
            Width = width;
        }
    }
}
