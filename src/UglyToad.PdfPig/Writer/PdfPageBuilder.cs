namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using Geometry;
    using Graphics.Operations;
    using Graphics.Operations.TextObjects;
    using Graphics.Operations.TextPositioning;
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

        public List<Letter> AddText(string text, decimal fontSize, PdfPoint position, PdfDocumentBuilder.AddedFont font)
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

            var fm = TransformationMatrix.FromValues(1 / 1000m, 0, 0, 1 / 1000m, 0, 0);
            var textMatrix = TransformationMatrix.FromValues(1, 0, 0, 1, position.X, position.Y);

            var letters = DrawLetters(text, fontProgram, fm, fontSize, textMatrix);
            
            try
            {
                //var realWidth = widthRect.Width;

                //if (realWidth + position.X > PageSize.Width)
                //{
                //    throw new InvalidOperationException("Text would exceed the bounds.");
                //}

                var beginText = BeginText.Value;

                operations.Add(beginText);
                operations.Add(new SetFontAndSize(font.Name, fontSize));
                operations.Add(new MoveToNextLineWithOffset(position.X, position.Y));
                operations.Add(new ShowText(text));
                operations.Add(EndText.Value);

                beginText = null;
            }
            catch (Exception ex)
            {
                throw;
            }

            return letters;
        }

        private static List<Letter> DrawLetters(string text, IWritingFont font, TransformationMatrix fontMatrix, decimal fontSize, TransformationMatrix textMatrix)
        {
            var horizontalScaling = 1;
            var rise = 0;
            var letters = new List<Letter>();

            var renderingMatrix =
                TransformationMatrix.FromValues(fontSize * horizontalScaling, 0, 0, fontSize, 0, rise);

            var width = 0m;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if(!font.TryGetBoundingBox(c, out var rect))
                {
                    throw new InvalidOperationException($"The font does not contain a character: {c}.");
                }

                if (!font.TryGetAdvanceWidth(c, out var charWidth))
                {
                    throw new InvalidOperationException($"The font does not contain a character: {c}.");
                }

                var advanceRect = new PdfRectangle(0, 0, charWidth, 0);
                advanceRect = textMatrix.Transform(renderingMatrix.Transform(fontMatrix.Transform(advanceRect)));

                var documentSpace = textMatrix.Transform(renderingMatrix.Transform(fontMatrix.Transform(rect)));

                var letter = new Letter(c.ToString(), documentSpace, advanceRect.BottomLeft, width, fontSize, font.Name, fontSize);
                letters.Add(letter);
                
                var tx = advanceRect.Width * horizontalScaling;
                var ty = 0;

                var translate = TransformationMatrix.GetTranslationMatrix(tx, ty);

                width += tx;

                textMatrix = translate.Multiply(textMatrix);
            }

            return letters;
        }
    }
}