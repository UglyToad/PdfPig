namespace UglyToad.PdfPig.Graphics.Core
{
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Convenience predicates for <see cref="TextRenderingMode"/>.
    /// </summary>
    public static class RenderingModeExtensions
    {
        /// <summary>
        /// True for the rendering modes that paint the glyph fill (0, 2, 4, 6).
        /// </summary>
        public static bool IsFill(this TextRenderingMode mode)
        {
            return mode == TextRenderingMode.Fill
                   || mode == TextRenderingMode.FillThenStroke
                   || mode == TextRenderingMode.FillClip
                   || mode == TextRenderingMode.FillThenStrokeClip;
        }

        /// <summary>
        /// True for the rendering modes that stroke the glyph outline (1, 2, 5, 6).
        /// </summary>
        public static bool IsStroke(this TextRenderingMode mode)
        {
            return mode == TextRenderingMode.Stroke
                   || mode == TextRenderingMode.FillThenStroke
                   || mode == TextRenderingMode.StrokeClip
                   || mode == TextRenderingMode.FillThenStrokeClip;
        }

        /// <summary>
        /// True for the rendering modes that add the glyph outline to the text clipping path at
        /// ET (4, 5, 6, 7).
        /// </summary>
        public static bool IsClip(this TextRenderingMode mode)
        {
            return mode == TextRenderingMode.FillClip
                   || mode == TextRenderingMode.StrokeClip
                   || mode == TextRenderingMode.FillThenStrokeClip
                   || mode == TextRenderingMode.NeitherClip;
        }
    }
}
