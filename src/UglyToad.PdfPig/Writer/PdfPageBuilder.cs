namespace UglyToad.PdfPig.Writer
{
    using System;
    using Fonts.TrueType;
    using Geometry;

    internal class PdfPageBuilder
    {
        private readonly PdfDocumentBuilder documentBuilder;

        public int PageNumber { get; }

        public PdfRectangle PageSize { get; set; }

        public PdfPageBuilder(int number, PdfDocumentBuilder documentBuilder)
        {
            this.documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            PageNumber = number;
        }

        public PdfPageBuilder AddText(string text, decimal fontSize, PdfPoint position, PdfDocumentBuilder.AddedFont font)
        {
            if (font == null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!documentBuilder.Fonts.TryGetValue(font.Id, out var fontProgram))
            {
                throw new ArgumentException($"No font has been added to the PdfDocumentBuilder with Id: {font.Id}. " +
                                            $"Use {nameof(documentBuilder.AddTrueTypeFont)} to register a font.", nameof(font));
            }

            if (fontSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be greater than 0");
            }

            var width = CalculateGlyphSpaceTextWidth(text, fontProgram);

            Console.WriteLine(width);

            return this;
        }

        private static decimal CalculateGlyphSpaceTextWidth(string text, TrueTypeFontProgram font)
        {
            var width = 0m;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if(!font.TryGetBoundingBox(c, out var rect))
                {
                    throw new InvalidOperationException($"The font does not contain a character: {c}.");
                }

                width += rect.Width;
            }

            return width;
        }
    }
}