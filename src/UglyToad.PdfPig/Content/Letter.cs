﻿namespace UglyToad.PdfPig.Content
{
    using Core;
    using Graphics.Colors;
    using PdfFonts;

    /// <summary>
    /// A glyph or combination of glyphs (characters) drawn by a PDF content stream.
    /// </summary>
    public class Letter
    {
        /// <summary>
        /// The text for this letter or unicode character.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Text orientation of the letter.
        /// </summary>
        public TextOrientation TextOrientation { get; }

        /// <summary>
        /// The placement position of the character in PDF space. See <see cref="StartBaseLine"/>
        /// </summary>
        public PdfPoint Location => StartBaseLine;

        /// <summary>
        /// The placement position of the character in PDF space (the start point of the baseline). See <see cref="Location"/>
        /// </summary>
        public PdfPoint StartBaseLine { get; }

        /// <summary>
        /// The end point of the baseline.
        /// </summary>
        public PdfPoint EndBaseLine { get; }

        /// <summary>
        /// The width occupied by the character within the PDF content.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Position of the bounding box for the glyph, this is the box surrounding the visible glyph as it appears on the page.
        /// For example letters with descenders, p, j, etc., will have a box extending below the <see cref="Location"/> they are placed at.
        /// The width of the glyph may also be more or less than the <see cref="Width"/> allocated for the character in the PDF content.
        /// </summary>
        public PdfRectangle GlyphRectangle { get; }

        /// <summary>
        /// Size as defined in the PDF file. This is not equivalent to font size in points but is relative to other font sizes on the page.
        /// </summary>
        public double FontSize { get; }

        /// <summary>
        /// The name of the font.
        /// </summary>
        public string? FontName => Font?.Name;

        /// <summary>
        /// Details about the font for this letter.
        /// </summary>
        public FontDetails Font { get; }

        /// <summary>
        /// Text rendering mode that indicates whether we should draw this letter's strokes,
        /// fill, both, neither (in case of hidden text), etc.
        /// If it calls for stroking the <see cref="StrokeColor" /> is used.
        /// If it calls for filling, the <see cref="FillColor"/> is used.
        /// In modes that perform both filling and stroking, the effect is as if each glyph outline were filled and then stroked in separate operations.
        /// </summary>
        public TextRenderingMode RenderingMode { get; }

        /// <summary>
        /// The primary color of the letter, which is either the <see cref="StrokeColor"/> in case
        /// <see cref="RenderingMode"/> is <see cref="TextRenderingMode.Stroke"/>, or otherwise
        /// it is the <see cref="FillColor"/>.
        /// </summary>
        public IColor Color { get; }

        /// <summary>
        /// Stroking color
        /// </summary>
        public IColor StrokeColor { get; }

        /// <summary>
        /// Non-stroking (fill) color
        /// </summary>
        public IColor FillColor { get; }

        /// <summary>
        /// The size of the font in points.
        /// </summary>
        public double PointSize { get; }

        /// <summary>
        /// Sequence number of the ShowText operation that printed this letter.
        /// </summary>
        public int TextSequence { get; }

        /// <summary>
        /// Create a new letter to represent some text drawn by the Tj operator.
        /// </summary>
        public Letter(string value, PdfRectangle glyphRectangle,
            PdfPoint startBaseLine,
            PdfPoint endBaseLine,
            double width,
            double fontSize,
            FontDetails font,
            TextRenderingMode renderingMode,
            IColor strokeColor,
            IColor fillColor,
            double pointSize,
            int textSequence)
        {
            Value = value;
            GlyphRectangle = glyphRectangle;
            StartBaseLine = startBaseLine;
            EndBaseLine = endBaseLine;
            Width = width;
            FontSize = fontSize;
            Font = font;
            RenderingMode = renderingMode;
            if (renderingMode == TextRenderingMode.Stroke)
            {
                Color = StrokeColor = strokeColor ?? GrayColor.Black;
                FillColor = fillColor;
            }
            else
            {
                Color = FillColor = fillColor ?? GrayColor.Black;
                StrokeColor = strokeColor;
            }
            PointSize = pointSize;
            TextSequence = textSequence;
            TextOrientation = GetTextOrientation();
        }

        private TextOrientation GetTextOrientation()
        {
            if (Math.Abs(StartBaseLine.Y - EndBaseLine.Y) < 10e-5)
            {
                if (Math.Abs(StartBaseLine.X - EndBaseLine.X) < 10e-5)
                {
                    // Start and End point are the same
                    return GetTextOrientationRot();
                }

                if (StartBaseLine.X > EndBaseLine.X)
                {
                    return TextOrientation.Rotate180;
                }

                return TextOrientation.Horizontal;
            }

            if (Math.Abs(StartBaseLine.X - EndBaseLine.X) < 10e-5)
            {
                if (Math.Abs(StartBaseLine.Y - EndBaseLine.Y) < 10e-5)
                {
                    // Start and End point are the same
                    return GetTextOrientationRot();
                }

                if (StartBaseLine.Y > EndBaseLine.Y)
                {
                    return TextOrientation.Rotate90;
                }

                return TextOrientation.Rotate270;
            }

            return TextOrientation.Other;
        }

        private TextOrientation GetTextOrientationRot()
        {
            double rotation = GlyphRectangle.Rotation;
            if (Math.Abs(rotation % 90) >= 10e-5)
            {
                return TextOrientation.Other;
            }

            int rotationInt = (int)Math.Round(rotation, MidpointRounding.AwayFromZero);
            switch (rotationInt)
            {
                case 0:
                    return TextOrientation.Horizontal;

                case -90:
                    return TextOrientation.Rotate90;

                case 180:
                case -180:
                    return TextOrientation.Rotate180;

                case 90:
                    return TextOrientation.Rotate270;
            }

            throw new Exception($"Could not find TextOrientation for rotation '{rotation}'.");
        }

        /// <summary>
        /// Produces a string representation of the letter and its position.
        /// </summary>
        public override string ToString()
        {
            return $"{Value} {Location} {FontName} {PointSize}";
        }
    }
}
