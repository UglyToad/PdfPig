namespace UglyToad.PdfPig.Annotations
{
    using Tokens;

    internal static class AnnotationExtensions
    {
        public static AnnotationType ToAnnotationType(this NameToken name)
        {
            if (name.Data == NameToken.Text.Data)
            {
                return AnnotationType.Text;
            }

            if (name.Data == NameToken.Link.Data)
            {
                return AnnotationType.Link;
            }

            if (name.Data == NameToken.FreeText.Data)
            {
                return AnnotationType.FreeText;
            }

            if (name.Data == NameToken.Line.Data)
            {
                return AnnotationType.Line;
            }

            if (name.Data == NameToken.Square.Data)
            {
                return AnnotationType.Square;
            }

            if (name.Data == NameToken.Circle.Data)
            {
                return AnnotationType.Circle;
            }

            if (name.Data == NameToken.Polygon.Data)
            {
                return AnnotationType.Polygon;
            }

            if (name.Data == NameToken.PolyLine.Data)
            {
                return AnnotationType.PolyLine;
            }

            if (name.Data == NameToken.Highlight.Data)
            {
                return AnnotationType.Highlight;
            }

            if (name.Data == NameToken.Underline.Data)
            {
                return AnnotationType.Underline;
            }

            if (name.Data == NameToken.Squiggly.Data)
            {
                return AnnotationType.Squiggly;
            }

            if (name.Data == NameToken.StrikeOut.Data)
            {
                return AnnotationType.StrikeOut;
            }

            if (name.Data == NameToken.Stamp.Data)
            {
                return AnnotationType.Stamp;
            }

            if (name.Data == NameToken.Caret.Data)
            {
                return AnnotationType.Caret;
            }

            if (name.Data == NameToken.Ink.Data)
            {
                return AnnotationType.Ink;
            }

            if (name.Data == NameToken.Popup.Data)
            {
                return AnnotationType.Popup;
            }

            if (name.Data == NameToken.FileAttachment.Data)
            {
                return AnnotationType.FileAttachment;
            }

            if (name.Data == NameToken.Sound.Data)
            {
                return AnnotationType.Sound;
            }

            if (name.Data == NameToken.Movie.Data)
            {
                return AnnotationType.Movie;
            }

            if (name.Data == NameToken.Widget.Data)
            {
                return AnnotationType.Widget;
            }

            if (name.Data == NameToken.Screen.Data)
            {
                return AnnotationType.Screen;
            }

            if (name.Data == NameToken.PrinterMark.Data)
            {
                return AnnotationType.PrinterMark;
            }

            if (name.Data == NameToken.TrapNet.Data)
            {
                return AnnotationType.TrapNet;
            }

            if (name.Data == NameToken.Watermark.Data)
            {
                return AnnotationType.Watermark;
            }

            if (name.Data == NameToken.Annotation3D.Data)
            {
                return AnnotationType.Artwork3D;
            }

            return AnnotationType.Other;
        }
    }
}