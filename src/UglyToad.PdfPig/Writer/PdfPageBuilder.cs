namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Fonts.TrueType;
    using Geometry;
    using Graphics.Operations;
    using Graphics.Operations.SpecialGraphicsState;
    using Graphics.Operations.TextObjects;
    using Graphics.Operations.TextShowing;
    using Graphics.Operations.TextState;

    internal class PdfPageBuilder
    {
        private readonly PdfDocumentBuilder documentBuilder;
        private readonly List<IGraphicsStateOperation> operations = new List<IGraphicsStateOperation>();
        private BeginText lastBeginText;

        public int PageNumber { get; }
        public IReadOnlyList<IGraphicsStateOperation> Operations => operations;

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

            var fm = TransformationMatrix.FromValues(1 / 1000m, 0, 1 / 1000m, 0, 0, 0);

            var widthRect = fm.Transform(new PdfRectangle(0, 0, width, 0));

            try
            {
                var ctm = TransformationMatrix.FromValues(position.X, 0, position.Y, 0, 0, 0);

                var realWidth = ctm.Transform(widthRect).Width;

                if (realWidth + position.X > PageSize.Width)
                {
                    throw new InvalidOperationException("Text would exceed the bounds.");
                }

                operations.Add(new ModifyCurrentTransformationMatrix(new[]
                {
                    position.X, 0, position.Y, 0, 0, 0
                }));

                var beginText = BeginText.Value;

                operations.Add(beginText);
                operations.Add(new SetFontAndSize(font.Name, fontSize));
                operations.Add(new ShowText(text));
                operations.Add(EndText.Value);

                beginText = null;
            }
            catch (Exception ex)
            {
                throw;
            }

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