namespace UglyToad.PdfPig.Writer
{
    using Content;
    using Core;
    using Fonts;
    using Graphics.Colors;
    using Graphics.Operations;
    using Graphics.Operations.General;
    using Graphics.Operations.PathConstruction;
    using Graphics.Operations.SpecialGraphicsState;
    using Graphics.Operations.TextObjects;
    using Graphics.Operations.TextPositioning;
    using Graphics.Operations.TextShowing;
    using Graphics.Operations.TextState;
    using Images;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using PdfFonts;
    using Tokens;
    using Graphics.Operations.PathPainting;
    using Images.Png;

    /// <summary>
    /// A builder used to add construct a page in a PDF document.
    /// </summary>
    public class PdfPageBuilder
    {
        private readonly PdfDocumentBuilder documentBuilder;
        private readonly List<IGraphicsStateOperation> operations = new List<IGraphicsStateOperation>();
        private readonly Dictionary<NameToken, IToken> resourcesDictionary = new Dictionary<NameToken, IToken>();

        //a sequence number of ShowText operation to determine whether letters belong to same operation or not (letters that belong to different operations have less changes to belong to same word)
        private int textSequence;

        private int imageKey = 1;

        internal IReadOnlyList<IGraphicsStateOperation> Operations => operations;

        internal IReadOnlyDictionary<NameToken, IToken> Resources => resourcesDictionary;

        /// <summary>
        /// The number of this page, 1-indexed.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The current size of the page.
        /// </summary>
        public PdfRectangle PageSize { get; set; }

        /// <summary>
        /// Access to the underlying data structures for advanced use cases.
        /// </summary>
        public AdvancedEditing Advanced { get; }

        internal PdfPageBuilder(int number, PdfDocumentBuilder documentBuilder)
        {
            this.documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            PageNumber = number;
            Advanced = new AdvancedEditing(operations);
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

            operations.Add(new BeginNewSubpath((decimal)from.X, (decimal)from.Y));
            operations.Add(new AppendStraightLineSegment((decimal)to.X, (decimal)to.Y));
            operations.Add(StrokePath.Value);

            if (lineWidth != 1)
            {
                operations.Add(new SetLineWidth(1));
            }
        }

        /// <summary>
        /// Draws a rectangle on the current page starting at the specified point with the given width, height and line width.
        /// </summary>
        /// <param name="position">The position of the rectangle, for positive width and height this is the bottom-left corner.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="lineWidth">The width of the line border of the rectangle.</param>
        /// <param name="fill">Whether to fill with the color set by <see cref="SetTextAndFillColor"/>.</param>
        public void DrawRectangle(PdfPoint position, decimal width, decimal height, decimal lineWidth = 1, bool fill = false)
        {
            if (lineWidth != 1)
            {
                operations.Add(new SetLineWidth(lineWidth));
            }

            operations.Add(new AppendRectangle((decimal)position.X, (decimal)position.Y, width, height));

            if (fill)
            {
                operations.Add(FillPathEvenOddRuleAndStroke.Value);
            }
            else
            {
                operations.Add(StrokePath.Value);
            }

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
            operations.Add(new MoveToNextLineWithOffset((decimal)position.X, (decimal)position.Y));
            var bytesPerShow = new List<byte>();
            foreach (var letter in text)
            {
                if (char.IsWhiteSpace(letter))
                {
                    operations.Add(new ShowText(bytesPerShow.ToArray()));
                    bytesPerShow.Clear();
                }

                var b = fontProgram.GetValueForCharacter(letter);
                bytesPerShow.Add(b);
            }

            if (bytesPerShow.Count > 0)
            {
                operations.Add(new ShowText(bytesPerShow.ToArray()));
            }

            operations.Add(EndText.Value);

            return letters;
        }

        /// <summary>
        /// Adds the JPEG image represented by the input bytes at the specified location.
        /// </summary>
        public AddedImage AddJpeg(byte[] fileBytes, PdfRectangle placementRectangle)
        {
            using (var stream = new MemoryStream(fileBytes))
            {
                return AddJpeg(stream, placementRectangle);
            }
        }
        
        /// <summary>
        /// Adds the JPEG image represented by the input stream at the specified location.
        /// </summary>
        public AddedImage AddJpeg(Stream fileStream, PdfRectangle placementRectangle)
        {
            var startFrom = fileStream.Position;
            var info = JpegHandler.GetInformation(fileStream);

            byte[] data;
            using (var memory = new MemoryStream())
            {
                fileStream.Seek(startFrom, SeekOrigin.Begin);
                fileStream.CopyTo(memory);
                data = memory.ToArray();
            }

            var imgDictionary = new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.Xobject },
                {NameToken.Subtype, NameToken.Image },
                {NameToken.Width, new NumericToken(info.Width) },
                {NameToken.Height, new NumericToken(info.Height) },
                {NameToken.BitsPerComponent, new NumericToken(info.BitsPerComponent)},
                {NameToken.ColorSpace, NameToken.Devicergb},
                {NameToken.Filter, NameToken.DctDecode},
                {NameToken.Length, new NumericToken(data.Length)}
            };

            var reference = documentBuilder.AddImage(new DictionaryToken(imgDictionary), data);

            if (!resourcesDictionary.TryGetValue(NameToken.Xobject, out var xobjectsDict) 
                || !(xobjectsDict is DictionaryToken xobjects))
            {
                xobjects = new DictionaryToken(new Dictionary<NameToken, IToken>());
                resourcesDictionary[NameToken.Xobject] = xobjects;
            }

            var key = NameToken.Create($"I{imageKey++}");

            resourcesDictionary[NameToken.Xobject] = xobjects.With(key, new IndirectReferenceToken(reference));

            operations.Add(Push.Value);
            // This needs to be the placement rectangle.
            operations.Add(new ModifyCurrentTransformationMatrix(new []
            {
                (decimal)placementRectangle.Width, 0,
                0, (decimal)placementRectangle.Height,
                (decimal)placementRectangle.BottomLeft.X, (decimal)placementRectangle.BottomLeft.Y
            }));
            operations.Add(new InvokeNamedXObject(key));
            operations.Add(Pop.Value);

            return new AddedImage(reference, info.Width, info.Height);
        }

        /// <summary>
        /// Adds the JPEG image previously added using <see cref="AddJpeg(byte[],PdfRectangle)"/>,
        /// this will share the same image data to prevent duplication.
        /// </summary>
        /// <param name="image">An image previously added to this page or another page.</param>
        /// <param name="placementRectangle">The size and location to draw the image on this page.</param>
        public void AddJpeg(AddedImage image, PdfRectangle placementRectangle) => AddImage(image, placementRectangle);

        /// <summary>
        /// Adds the image previously added using <see cref="AddJpeg(byte[], PdfRectangle)"/>
        /// or <see cref="AddPng(byte[], PdfRectangle)"/> sharing the same image to prevent duplication.
        /// </summary>
        public void AddImage(AddedImage image, PdfRectangle placementRectangle)
        {
            if (!resourcesDictionary.TryGetValue(NameToken.Xobject, out var xobjectsDict) 
                || !(xobjectsDict is DictionaryToken xobjects))
            {
                xobjects = new DictionaryToken(new Dictionary<NameToken, IToken>());
                resourcesDictionary[NameToken.Xobject] = xobjects;
            }

            var key = NameToken.Create($"I{imageKey++}");

            resourcesDictionary[NameToken.Xobject] = xobjects.With(key, new IndirectReferenceToken(image.Reference));

            operations.Add(Push.Value);
            // This needs to be the placement rectangle.
            operations.Add(new ModifyCurrentTransformationMatrix(new[]
            {
                (decimal)placementRectangle.Width, 0,
                0, (decimal)placementRectangle.Height,
                (decimal)placementRectangle.BottomLeft.X, (decimal)placementRectangle.BottomLeft.Y
            }));
            operations.Add(new InvokeNamedXObject(key));
            operations.Add(Pop.Value);
        }

        /// <summary>
        /// Adds the PNG image represented by the input bytes at the specified location.
        /// </summary>
        public AddedImage AddPng(byte[] pngBytes, PdfRectangle placementRectangle)
        {
            using (var memoryStream = new MemoryStream(pngBytes))
            {
                return AddPng(memoryStream, placementRectangle);
            }
        }

        /// <summary>
        /// Adds the PNG image represented by the input stream at the specified location.
        /// </summary>
        public AddedImage AddPng(Stream pngStream, PdfRectangle placementRectangle)
        {
            var png = Png.Open(pngStream);

            byte[] data;
            var pixelBuffer = new byte[3];
            using (var memoryStream = new MemoryStream())
            {
                for (var rowIndex = 0; rowIndex < png.Height; rowIndex++)
                {
                    for (var colIndex = 0; colIndex < png.Width; colIndex++)
                    {
                        var pixel = png.GetPixel(colIndex, rowIndex);

                        pixelBuffer[0] = pixel.R;
                        pixelBuffer[1] = pixel.G;
                        pixelBuffer[2] = pixel.B;

                        memoryStream.Write(pixelBuffer, 0, pixelBuffer.Length);
                    }
                }

                data = memoryStream.ToArray();
            }

            var compressed = DataCompresser.CompressBytes(data);

            var imgDictionary = new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.Xobject },
                {NameToken.Subtype, NameToken.Image },
                {NameToken.Width, new NumericToken(png.Width) },
                {NameToken.Height, new NumericToken(png.Height) },
                {NameToken.BitsPerComponent, new NumericToken(png.Header.BitDepth)},
                {NameToken.ColorSpace, NameToken.Devicergb},
                {NameToken.Filter, NameToken.FlateDecode},
                {NameToken.Length, new NumericToken(compressed.Length)}
            };
            
            var reference = documentBuilder.AddImage(new DictionaryToken(imgDictionary), compressed);

            if (!resourcesDictionary.TryGetValue(NameToken.Xobject, out var xobjectsDict)
                || !(xobjectsDict is DictionaryToken xobjects))
            {
                xobjects = new DictionaryToken(new Dictionary<NameToken, IToken>());
                resourcesDictionary[NameToken.Xobject] = xobjects;
            }

            var key = NameToken.Create($"I{imageKey++}");

            resourcesDictionary[NameToken.Xobject] = xobjects.With(key, new IndirectReferenceToken(reference));

            operations.Add(Push.Value);
            // This needs to be the placement rectangle.
            operations.Add(new ModifyCurrentTransformationMatrix(new[]
            {
                (decimal)placementRectangle.Width, 0,
                0, (decimal)placementRectangle.Height,
                (decimal)placementRectangle.BottomLeft.X, (decimal)placementRectangle.BottomLeft.Y
            }));
            operations.Add(new InvokeNamedXObject(key));
            operations.Add(Pop.Value);

            return new AddedImage(reference, png.Width, png.Height);
        }

        private List<Letter> DrawLetters(string text, IWritingFont font, TransformationMatrix fontMatrix, decimal fontSize, TransformationMatrix textMatrix)
        {
            var horizontalScaling = 1;
            var rise = 0;
            var letters = new List<Letter>();

            var renderingMatrix =
                TransformationMatrix.FromValues((double)fontSize * horizontalScaling, 0, 0, (double)fontSize, 0, rise);

            var width = 0.0;

            textSequence++;

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

                var letter = new Letter(c.ToString(), documentSpace, advanceRect.BottomLeft, advanceRect.BottomRight, width, (double)fontSize, FontDetails.GetDefault(font.Name),
                    GrayColor.Black,
                    (double)fontSize,
                    textSequence);

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
            res = Math.Round(Math.Min(1, res), 4);

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

        /// <summary>
        /// Provides access to the raw page data structures for advanced editing use cases.
        /// </summary>
        public class AdvancedEditing
        {
            /// <summary>
            /// The operations making up the page content stream.
            /// </summary>
            public List<IGraphicsStateOperation> Operations { get; }

            /// <summary>
            /// Create a new <see cref="AdvancedEditing"/>.
            /// </summary>
            internal AdvancedEditing(List<IGraphicsStateOperation> operations)
            {
                Operations = operations;
            }
        }

        /// <summary>
        /// A key representing an image available to use for the current document builder.
        /// Create it by adding an image to a page using <see cref="AddJpeg(byte[],PdfRectangle)"/>.
        /// </summary>
        public class AddedImage
        {
            /// <summary>
            /// The Id uniquely identifying this image on the builder.
            /// </summary>
            internal Guid Id { get; }

            /// <summary>
            /// The reference to the stored image XObject.
            /// </summary>
            internal IndirectReference Reference { get; }

            /// <summary>
            /// The width of the raw image in pixels.
            /// </summary>
            public int Width { get; }

            /// <summary>
            /// The height of the raw image in pixels.
            /// </summary>
            public int Height { get; }

            /// <summary>
            /// Create a new <see cref="AddedImage"/>.
            /// </summary>
            internal AddedImage(IndirectReference reference, int width, int height)
            {
                Id = Guid.NewGuid();
                Reference = reference;
                Width = width;
                Height = height;
            }
        }
    }
}