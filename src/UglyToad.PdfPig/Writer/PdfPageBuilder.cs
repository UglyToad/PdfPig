namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using Geometry;
    using Graphics.Operations;
    using Graphics.Operations.General;
    using Graphics.Operations.PathConstruction;
    using Graphics.Operations.SpecialGraphicsState;
    using Graphics.Operations.TextObjects;
    using Graphics.Operations.TextPositioning;
    using Graphics.Operations.TextShowing;
    using Graphics.Operations.TextState;

    /// <summary>
    /// A builder used to add construct a page in a PDF document.
    /// </summary>
    public class PdfPageBuilder
    {
        private readonly PdfDocumentBuilder documentBuilder;
        private readonly List<IGraphicsStateOperation> operations = new List<IGraphicsStateOperation>();
        internal IReadOnlyList<IGraphicsStateOperation> Operations => operations;

        /// <summary>
        /// The number of this page, 1-indexed.
        /// </summary>
        public int PageNumber { get; }
        
        /// <summary>
        /// The current size of the page.
        /// </summary>
        public PdfRectangle PageSize { get; set; }

        internal PdfPageBuilder(int number, PdfDocumentBuilder documentBuilder)
        {
            this.documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            PageNumber = number;
        }

        /// <summary>
        /// Draws a line on the current page between two points with the specified line width.
        /// </summary>
        /// <param name="from">The first point on the line.</param>
        /// <param name="to">The last point on the line.</param>
        /// <param name="lineWidth">The width of the line in user space units.</param>
        public void DrawLine(PdfPoint from, PdfPoint to, decimal lineWidth = 1)
        {
            if (lineWidth != 1)
            {
                operations.Add(new SetLineWidth(lineWidth));
            }

            operations.Add(new BeginNewSubpath(from.X, from.Y));
            operations.Add(new AppendStraightLineSegment(to.X, to.Y));
            operations.Add(StrokePath.Value);

            if (lineWidth != 1)
            {
                operations.Add(new SetLineWidth(1));
            }
        }

        /// <summary>
        /// Draws a rectangle on the current page starting at the specified point with the given width, height and line width.
        /// </summary>
        /// <param name="position">The position of the rectangle, for positive width and height this is the top-left corner.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="lineWidth">The width of the line border of the rectangle.</param>
        public void DrawRectangle(PdfPoint position, decimal width, decimal height, decimal lineWidth = 1)
        {
            if (lineWidth != 1)
            {
                operations.Add(new SetLineWidth(lineWidth));
            }

            operations.Add(new AppendRectangle(position.X, position.Y, width, height));
            operations.Add(StrokePath.Value);

            if (lineWidth != 1)
            {
                operations.Add(new SetLineWidth(lineWidth));
            }
        }

        /// <summary>
        /// Sets the stroke color for any following operations to the RGB value. Use <see cref="ResetColor"/> to reset.
        /// </summary>
        /// <param name="r">Red - 0 to 255</param>
        /// <param name="g">Green - 0 to 255</param>
        /// <param name="b">Blue - 0 to 255</param>
        public void SetStrokeColor(byte r, byte g, byte b)
        {
            operations.Add(Push.Value);
            operations.Add(new SetStrokeColorDeviceRgb(RgbToDecimal(r), RgbToDecimal(g), RgbToDecimal(b)));
        }

        /// <summary>
        /// Sets the stroke color with the exact decimal value between 0 and 1 for any following operations to the RGB value. Use <see cref="ResetColor"/> to reset.
        /// </summary>
        /// <param name="r">Red - 0 to 1</param>
        /// <param name="g">Green - 0 to 1</param>
        /// <param name="b">Blue - 0 to 1</param>
        internal void SetStrokeColorExact(decimal r, decimal g, decimal b)
        {
            operations.Add(Push.Value);
            operations.Add(new SetStrokeColorDeviceRgb(CheckRgbDecimal(r, nameof(r)), 
                CheckRgbDecimal(g, nameof(g)), CheckRgbDecimal(b, nameof(b))));
        }

        /// <summary>
        /// Sets the fill and text color for any following operations to the RGB value. Use <see cref="ResetColor"/> to reset.
        /// </summary>
        /// <param name="r">Red - 0 to 255</param>
        /// <param name="g">Green - 0 to 255</param>
        /// <param name="b">Blue - 0 to 255</param>
        public void SetTextAndFillColor(byte r, byte g, byte b)
        {
            operations.Add(Push.Value);
            operations.Add(new SetNonStrokeColorDeviceRgb(RgbToDecimal(r), RgbToDecimal(g), RgbToDecimal(b)));
        }

        /// <summary>
        /// Restores the stroke, text and fill color to default (black).
        /// </summary>
        public void ResetColor()
        {
            operations.Add(Pop.Value);
        }

        /// <summary>
        /// Calculates the size and position of each letter in a given string in the provided font without changing the state of the page. 
        /// </summary>
        /// <param name="text">The text to measure each letter of.</param>
        /// <param name="fontSize">The size of the font in user space units.</param>
        /// <param name="position">The position of the baseline (lower-left corner) to start drawing the text from.</param>
        /// <param name="font">
        /// A font added to the document using <see cref="PdfDocumentBuilder.AddTrueTypeFont"/>
        /// or <see cref="PdfDocumentBuilder.AddStandard14Font"/> methods.
        /// </param> 
        /// <returns>The letters from the input text with their corresponding size and position.</returns>
        public IReadOnlyList<Letter> MeasureText(string text, decimal fontSize, PdfPoint position, PdfDocumentBuilder.AddedFont font)
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

            var fm = fontProgram.GetFontMatrix();

            var textMatrix = TransformationMatrix.FromValues(1, 0, 0, 1, position.X, position.Y);

            var letters = DrawLetters(text, fontProgram, fm, fontSize, textMatrix);

            return letters;
        }

        /// <summary>
        /// Draws the text in the provided font at the specified position and returns the letters which will be drawn. 
        /// </summary>
        /// <param name="text">The text to draw to the page.</param>
        /// <param name="fontSize">The size of the font in user space units.</param>
        /// <param name="position">The position of the baseline (lower-left corner) to start drawing the text from.</param>
        /// <param name="font">
        /// A font added to the document using <see cref="PdfDocumentBuilder.AddTrueTypeFont"/>
        /// or <see cref="PdfDocumentBuilder.AddStandard14Font"/> methods.
        /// </param> 
        /// <returns>The letters from the input text with their corresponding size and position.</returns>
        public IReadOnlyList<Letter> AddText(string text, decimal fontSize, PdfPoint position, PdfDocumentBuilder.AddedFont font)
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

            var fm = fontProgram.GetFontMatrix();

            var textMatrix = TransformationMatrix.FromValues(1, 0, 0, 1, position.X, position.Y);

            var letters = DrawLetters(text, fontProgram, fm, fontSize, textMatrix);

            operations.Add(BeginText.Value);
            operations.Add(new SetFontAndSize(font.Name, fontSize));
            operations.Add(new MoveToNextLineWithOffset(position.X, position.Y));
            operations.Add(new ShowText(text));
            operations.Add(EndText.Value);

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

                if (!font.TryGetBoundingBox(c, out var rect))
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

        private static decimal RgbToDecimal(byte value)
        {
            var res = Math.Max(0, value / (decimal)byte.MaxValue);
            res = Math.Min(1, res);

            return res;
        }

        private static decimal CheckRgbDecimal(decimal value, string argument)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(argument, $"Provided decimal for RGB color was less than zero: {value}.");
            }

            if (value > 1)
            {
                throw new ArgumentOutOfRangeException(argument, $"Provided decimal for RGB color was greater than one: {value}.");
            }

            return value;
        }
    }
}