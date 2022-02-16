namespace UglyToad.PdfPig.Graphics.Core
{
    using UglyToad.PdfPig.Core;

    internal static class RenderingModeExtensions
    {
        public static bool IsFill(this TextRenderingMode mode)
        {
            return mode == TextRenderingMode.Fill
                   || mode == TextRenderingMode.FillThenStroke
                   || mode == TextRenderingMode.FillClip
                   || mode == TextRenderingMode.FillThenStrokeClip;
        }

        public static bool IsStroke(this TextRenderingMode mode)
        {
            return mode == TextRenderingMode.Stroke
                   || mode == TextRenderingMode.FillThenStroke
                   || mode == TextRenderingMode.StrokeClip
                   || mode == TextRenderingMode.FillThenStrokeClip;
        }

        public static bool IsClip(this TextRenderingMode mode)
        {
            return mode == TextRenderingMode.FillClip
                   || mode == TextRenderingMode.StrokeClip
                   || mode == TextRenderingMode.FillThenStrokeClip
                   || mode == TextRenderingMode.NeitherClip;
        }
    }
}
