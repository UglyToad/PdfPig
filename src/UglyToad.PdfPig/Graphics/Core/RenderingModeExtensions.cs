namespace UglyToad.PdfPig.Graphics.Core
{
    internal static class RenderingModeExtensions
    {
        public static bool IsFill(this RenderingMode mode)
        {
            return mode == RenderingMode.Fill
                   || mode == RenderingMode.FillThenStroke
                   || mode == RenderingMode.FillClip
                   || mode == RenderingMode.FillThenStrokeClip;
        }

        public static bool IsStroke(this RenderingMode mode)
        {
            return mode == RenderingMode.Stroke
                   || mode == RenderingMode.FillThenStroke
                   || mode == RenderingMode.StrokeClip
                   || mode == RenderingMode.FillThenStrokeClip;
        }

        public static bool IsClip(this RenderingMode mode)
        {
            return mode == RenderingMode.FillClip
                   || mode == RenderingMode.StrokeClip
                   || mode == RenderingMode.FillThenStrokeClip
                   || mode == RenderingMode.NeitherClip;
        }
    }
}