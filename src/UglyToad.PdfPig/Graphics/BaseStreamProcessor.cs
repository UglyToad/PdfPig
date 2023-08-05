namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.Graphics.Operations;
    using UglyToad.PdfPig.Graphics.Operations.TextPositioning;
    using UglyToad.PdfPig.Parser;
    using UglyToad.PdfPig.PdfFonts;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.XObjects;

    /// <summary>
    /// Stream processor Abstract class.
    /// </summary>
    /// <typeparam name="TPageContent"></typeparam>
    public abstract class BaseStreamProcessor<TPageContent> : IOperationContext
    {
        /// <summary>
        /// The resource store.
        /// </summary>
        protected readonly IResourceStore ResourceStore;

        /// <summary>
        /// The user space unit.
        /// </summary>
        protected readonly UserSpaceUnit UserSpaceUnit;

        /// <summary>
        /// The page rotation.
        /// </summary>
        protected readonly PageRotationDegrees Rotation;

        /// <summary>
        /// The scanner.
        /// </summary>
        protected readonly IPdfTokenScanner PdfScanner;

        /// <summary>
        /// The page content parser.
        /// </summary>
        protected readonly IPageContentParser PageContentParser;

        /// <summary>
        /// The filter provider.
        /// </summary>
        protected readonly ILookupFilterProvider FilterProvider;

        /// <summary>
        /// The parsing options.
        /// </summary>
        protected readonly IParsingOptions ParsingOptions;

        /// <summary>
        /// The graphics stack.
        /// </summary>
        protected Stack<CurrentGraphicsState> GraphicsStack = new Stack<CurrentGraphicsState>();

        /// <summary>
        /// The active ExtendedGraphicsState font.
        /// </summary>
        protected IFont ActiveExtendedGraphicsStateFont;

        /// <summary>
        /// Inline image builder.
        /// </summary>
        protected InlineImageBuilder InlineImageBuilder;

        /// <summary>
        /// The page number.
        /// </summary>
        protected int PageNumber;

        /// <inheritdoc/>
        public TextMatrices TextMatrices { get; } = new TextMatrices();

        /// <inheritdoc/>
        public TransformationMatrix CurrentTransformationMatrix => GetCurrentState().CurrentTransformationMatrix;

        /// <inheritdoc/>
        public PdfPoint CurrentPosition { get; set; }

        /// <inheritdoc/>
        public int StackSize => GraphicsStack.Count;

        /// <summary>
        /// A counter to track individual calls to <see cref="ShowText"/> operations used to determine if letters are likely to be
        /// in the same word/group. This exposes internal grouping of letters used by the PDF creator which may correspond to the
        /// intended grouping of letters into words.
        /// </summary>
        protected int TextSequence;

        private readonly Dictionary<XObjectType, List<XObjectContentRecord>> xObjects = new Dictionary<XObjectType, List<XObjectContentRecord>>
        {
            { XObjectType.Image, new List<XObjectContentRecord>() },
            { XObjectType.PostScript, new List<XObjectContentRecord>() }
        };

        /// <summary>
        /// Abstract constructor.
        /// </summary>
        protected BaseStreamProcessor(
            int pageNumber,
            IResourceStore resourceStore,
            UserSpaceUnit userSpaceUnit,
            MediaBox mediaBox,
            CropBox cropBox,
            PageRotationDegrees rotation,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            IParsingOptions parsingOptions)
        {
            this.PageNumber = pageNumber;
            this.ResourceStore = resourceStore;
            this.UserSpaceUnit = userSpaceUnit;
            this.Rotation = rotation;
            this.PdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.PageContentParser = pageContentParser ?? throw new ArgumentNullException(nameof(pageContentParser));
            this.FilterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.ParsingOptions = parsingOptions;

            TransformationMatrix initialMatrix = StreamProcessorHelper.GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, parsingOptions.Logger);

            GraphicsStack.Push(new CurrentGraphicsState()
            {
                CurrentTransformationMatrix = initialMatrix,
                CurrentClippingPath = StreamProcessorHelper.GetInitialClipping(cropBox, initialMatrix),
                ColorSpaceContext = new ColorSpaceContext(GetCurrentState, resourceStore)
            });
        }

        /// <summary>
        /// Process the <see cref="IGraphicsStateOperation"/>s and return content.
        /// </summary>
        public abstract TPageContent Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations);

        /// <summary>
        /// Process the <see cref="IGraphicsStateOperation"/>s.
        /// </summary>
        protected void ProcessOperations(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            foreach (var stateOperation in operations)
            {
                stateOperation.Run(this);
            }
        }

        /// <inheritdoc/>
        protected Stack<CurrentGraphicsState> CloneAllStates()
        {
            var saved = GraphicsStack;
            GraphicsStack = new Stack<CurrentGraphicsState>();
            GraphicsStack.Push(saved.Peek().DeepClone());
            return saved;
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        public CurrentGraphicsState GetCurrentState()
        {
            return GraphicsStack.Peek();
        }

        /// <inheritdoc/>
        public virtual void PopState()
        {
            GraphicsStack.Pop();
            ActiveExtendedGraphicsStateFont = null;
        }

        /// <inheritdoc/>
        public virtual void PushState()
        {
            GraphicsStack.Push(GraphicsStack.Peek().DeepClone());
        }

        /// <inheritdoc/>
        public virtual void ShowText(IInputBytes bytes)
        {
            var currentState = GetCurrentState();

            var font = currentState.FontState.FromExtendedGraphicsState ? ActiveExtendedGraphicsStateFont : ResourceStore.GetFont(currentState.FontState.FontName);

            if (font == null)
            {
                if (ParsingOptions.SkipMissingFonts)
                {
                    ParsingOptions.Logger.Warn($"Skipping a missing font with name {currentState.FontState.FontName} " +
                                               $"since it is not present in the document and {nameof(InternalParsingOptions.SkipMissingFonts)} " +
                                               "is set to true. This may result in some text being skipped and not included in the output.");

                    return;
                }

                throw new InvalidOperationException($"Could not find the font with name {currentState.FontState.FontName} in the resource store. It has not been loaded yet.");
            }

            var fontSize = currentState.FontState.FontSize;
            var horizontalScaling = currentState.FontState.HorizontalScaling / 100.0;
            var characterSpacing = currentState.FontState.CharacterSpacing;
            var rise = currentState.FontState.Rise;

            var transformationMatrix = currentState.CurrentTransformationMatrix;

            var renderingMatrix =
                TransformationMatrix.FromValues(fontSize * horizontalScaling, 0, 0, fontSize, 0, rise);

            var pointSize = Math.Round(transformationMatrix.Multiply(TextMatrices.TextMatrix).Transform(new PdfRectangle(0, 0, 1, fontSize)).Height, 2);

            while (bytes.MoveNext())
            {
                var code = font.ReadCharacterCode(bytes, out int codeLength);

                var foundUnicode = font.TryGetUnicode(code, out var unicode);

                if (!foundUnicode || unicode == null)
                {
                    ParsingOptions.Logger.Warn($"We could not find the corresponding character with code {code} in font {font.Name}.");

                    // Try casting directly to string as in PDFBox 1.8.
                    unicode = new string((char)code, 1);
                }

                var wordSpacing = 0.0;
                if (code == ' ' && codeLength == 1)
                {
                    wordSpacing += GetCurrentState().FontState.WordSpacing;
                }

                var textMatrix = TextMatrices.TextMatrix;

                if (font.IsVertical)
                {
                    if (!(font is IVerticalWritingSupported verticalFont))
                    {
                        throw new InvalidOperationException($"Font {font.Name} was in vertical writing mode but did not implement {nameof(IVerticalWritingSupported)}.");
                    }

                    var positionVector = verticalFont.GetPositionVector(code);

                    textMatrix = textMatrix.Translate(positionVector.X, positionVector.Y);
                }

                var boundingBox = font.GetBoundingBox(code);

                RenderGlyph(font,
                        currentState.CurrentStrokingColor,
                        currentState.CurrentNonStrokingColor,
                        currentState.FontState.TextRenderingMode,
                        fontSize,
                        pointSize,
                        code,
                        unicode,
                        bytes.CurrentOffset,
                        renderingMatrix,
                        textMatrix,
                        transformationMatrix,
                        boundingBox);

                double tx, ty;
                if (font.IsVertical)
                {
                    var verticalFont = (IVerticalWritingSupported)font;
                    var displacement = verticalFont.GetDisplacementVector(code);
                    tx = 0;
                    ty = (displacement.Y * fontSize) + characterSpacing + wordSpacing;
                }
                else
                {
                    tx = (boundingBox.Width * fontSize + characterSpacing + wordSpacing) * horizontalScaling;
                    ty = 0;
                }

                TextMatrices.TextMatrix = TextMatrices.TextMatrix.Translate(tx, ty);
            }
        }

        /// <summary>
        /// Render glyph implement.
        /// </summary>
        public abstract void RenderGlyph(IFont font, IColor strokingColor, IColor nonStrokingColor, TextRenderingMode textRenderingMode, double fontSize, double pointSize, int code, string unicode, long currentOffset,
            TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix, CharacterBoundingBox characterBoundingBox);

        /// <inheritdoc/>
        public virtual void ShowPositionedText(IReadOnlyList<IToken> tokens)
        {
            TextSequence++;

            var currentState = GetCurrentState();

            var textState = currentState.FontState;

            var fontSize = textState.FontSize;
            var horizontalScaling = textState.HorizontalScaling / 100.0;
            var font = ResourceStore.GetFont(textState.FontName);

            if (font == null)
            {
                if (ParsingOptions.SkipMissingFonts)
                {
                    ParsingOptions.Logger.Warn($"Skipping a missing font with name {currentState.FontState.FontName} " +
                                               $"since it is not present in the document and {nameof(InternalParsingOptions.SkipMissingFonts)} " +
                                               "is set to true. This may result in some text being skipped and not included in the output.");

                    return;
                }

                throw new InvalidOperationException($"Could not find the font with name {currentState.FontState.FontName} in the resource store. It has not been loaded yet.");
            }

            var isVertical = font.IsVertical;

            foreach (var token in tokens)
            {
                if (token is NumericToken number)
                {
                    var positionAdjustment = (double)number.Data;

                    double tx, ty;
                    if (isVertical)
                    {
                        tx = 0;
                        ty = -positionAdjustment / 1000 * fontSize;
                    }
                    else
                    {
                        tx = -positionAdjustment / 1000 * fontSize * horizontalScaling;
                        ty = 0;
                    }

                    AdjustTextMatrix(tx, ty);
                }
                else
                {
                    IReadOnlyList<byte> bytes;
                    if (token is HexToken hex)
                    {
                        bytes = hex.Bytes;
                    }
                    else
                    {
                        bytes = OtherEncodings.StringAsLatin1Bytes(((StringToken)token).Data);
                    }

                    ShowText(new ByteArrayInputBytes(bytes));
                }
            }
        }

        /// <inheritdoc/>
        public virtual void ApplyXObject(NameToken xObjectName)
        {
            if (!ResourceStore.TryGetXObject(xObjectName, out var xObjectStream))
            {
                if (ParsingOptions.SkipMissingFonts)
                {
                    return;
                }

                throw new PdfDocumentFormatException($"No XObject with name {xObjectName} found on page {PageNumber}.");
            }

            // For now we will determine the type and store the object with the graphics state information preceding it.
            // Then consumers of the page can request the object(s) to be retrieved by type.
            var subType = (NameToken)xObjectStream.StreamDictionary.Data[NameToken.Subtype.Data];

            var state = GetCurrentState();

            var matrix = state.CurrentTransformationMatrix;

            if (subType.Equals(NameToken.Ps))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.PostScript, xObjectStream, matrix, state.RenderingIntent,
                    state.ColorSpaceContext?.CurrentStrokingColorSpace ?? DeviceRgbColorSpaceDetails.Instance);
                xObjects[XObjectType.PostScript].Add(contentRecord);
            }
            else if (subType.Equals(NameToken.Image))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.Image, xObjectStream, matrix, state.RenderingIntent,
                    state.ColorSpaceContext?.CurrentStrokingColorSpace ?? DeviceRgbColorSpaceDetails.Instance);

                RenderXObjectImage(contentRecord);
            }
            else if (subType.Equals(NameToken.Form))
            {
                ProcessFormXObject(xObjectStream);
            }
            else
            {
                throw new InvalidOperationException($"XObject encountered with unexpected SubType {subType}. {xObjectStream.StreamDictionary}.");
            }
        }

        /// <summary>
        /// Process a XObject form.
        /// </summary>
        /// <param name="formStream"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected void ProcessFormXObject(StreamToken formStream)
        {
            /*
             * When a form XObject is invoked the following should happen:
             *
             * 1. Save the current graphics state, as if by invoking the q operator.
             * 2. Concatenate the matrix from the form dictionary's Matrix entry with the current transformation matrix.
             * 3. Clip according to the form dictionary's BBox entry.
             * 4. Paint the graphics objects specified in the form's content stream.
             * 5. Restore the saved graphics state, as if by invoking the Q operator.
             */

            var hasResources = formStream.StreamDictionary.TryGet<DictionaryToken>(NameToken.Resources, PdfScanner, out var formResources);
            if (hasResources)
            {
                ResourceStore.LoadResourceDictionary(formResources, ParsingOptions);
            }

            // 1. Save current state.
            PushState();

            var startState = GetCurrentState();

            // Transparency Group XObjects
            if (formStream.StreamDictionary.TryGet(NameToken.Group, PdfScanner, out DictionaryToken formGroupToken))
            {
                if (!formGroupToken.TryGet<NameToken>(NameToken.S, PdfScanner, out var sToken) || sToken != NameToken.Transparency)
                {
                    throw new InvalidOperationException($"Invalid Transparency Group XObject, '{NameToken.S}' token is not set or not equal to '{NameToken.Transparency}'.");
                }

                /* blend mode
                 * A conforming reader shall implicitly reset this parameter to its initial value at the beginning of execution of a
                 * transparency group XObject (see 11.6.6, "Transparency Group XObjects"). Initial value: Normal.
                 */
                //startState.BlendMode = BlendMode.Normal;

                /* soft mask
                 * A conforming reader shall implicitly reset this parameter implicitly reset to its initial value at the beginning
                 * of execution of a transparency group XObject (see 11.6.6, "Transparency Group XObjects"). Initial value: None.
                 */
                // TODO

                /* alpha constant
                 * A conforming reader shall implicitly reset this parameter to its initial value at the beginning of execution of a
                 * transparency group XObject (see 11.6.6, "Transparency Group XObjects"). Initial value: 1.0.
                 */
                startState.AlphaConstantNonStroking = 1.0m;
                startState.AlphaConstantStroking = 1.0m;

                if (formGroupToken.TryGet(NameToken.Cs, PdfScanner, out NameToken csNameToken))
                {
                    startState.ColorSpaceContext.SetNonStrokingColorspace(csNameToken);
                }
                else if (formGroupToken.TryGet(NameToken.Cs, PdfScanner, out ArrayToken csArrayToken)
                    && csArrayToken.Length > 0)
                {
                    if (csArrayToken.Data[0] is NameToken firstColorSpaceName)
                    {
                        startState.ColorSpaceContext.SetNonStrokingColorspace(firstColorSpaceName, formGroupToken);
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid color space in Transparency Group XObjects.");
                    }
                }

                bool isolated = false;
                if (formGroupToken.TryGet(NameToken.I, PdfScanner, out BooleanToken isolatedToken))
                {
                    /*
                     * (Optional) A flag specifying whether the transparency group is isolated (see “Isolated Groups”).
                     * If this flag is true, objects within the group shall be composited against a fully transparent
                     * initial backdrop; if false, they shall be composited against the group’s backdrop.
                     * Default value: false.
                     */
                    isolated = isolatedToken.Data;
                }

                bool knockout = false;
                if (formGroupToken.TryGet(NameToken.K, PdfScanner, out BooleanToken knockoutToken))
                {
                    /*
                     * (Optional) A flag specifying whether the transparency group is a knockout group (see “Knockout Groups”).
                     * If this flag is false, later objects within the group shall be composited with earlier ones with which
                     * they overlap; if true, they shall be composited with the group’s initial backdrop and shall overwrite
                     * (“knock out”) any earlier overlapping objects.
                     * Default value: false.
                     */
                    knockout = knockoutToken.Data;
                }
            }

            var formMatrix = TransformationMatrix.Identity;
            if (formStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Matrix, PdfScanner, out var formMatrixToken))
            {
                formMatrix = TransformationMatrix.FromArray(formMatrixToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray());
            }

            // 2. Update current transformation matrix.
            startState.CurrentTransformationMatrix = formMatrix.Multiply(startState.CurrentTransformationMatrix);

            var contentStream = formStream.Decode(FilterProvider, PdfScanner);

            var operations = PageContentParser.Parse(PageNumber, new ByteArrayInputBytes(contentStream), ParsingOptions.Logger);

            // 3. We don't respect clipping currently.

            // 4. Paint the objects.
            ProcessOperations(operations);

            // 5. Restore saved state.
            PopState();

            if (hasResources)
            {
                ResourceStore.UnloadResourceDictionary();
            }
        }

        /// <summary>
        /// Render XObject image implementation.
        /// </summary>
        /// <param name="xObjectContentRecord"></param>
        public abstract void RenderXObjectImage(XObjectContentRecord xObjectContentRecord);

        /// <inheritdoc/>
        public abstract void BeginSubpath();

        /// <inheritdoc/>
        public abstract PdfPoint? CloseSubpath();

        /// <inheritdoc/>
        public abstract void StrokePath(bool close);

        /// <inheritdoc/>
        public abstract void FillPath(FillingRule fillingRule, bool close);

        /// <inheritdoc/>
        public abstract void FillStrokePath(FillingRule fillingRule, bool close);

        /// <inheritdoc/>
        public abstract void MoveTo(double x, double y);

        /// <inheritdoc/>
        public abstract void BezierCurveTo(double x2, double y2, double x3, double y3);

        /// <inheritdoc/>
        public abstract void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3);

        /// <inheritdoc/>
        public abstract void LineTo(double x, double y);

        /// <inheritdoc/>
        public abstract void Rectangle(double x, double y, double width, double height);

        /// <inheritdoc/>
        public abstract void EndPath();

        /// <inheritdoc/>
        public abstract void ClosePath();

        /// <inheritdoc/>
        public abstract void ModifyClippingIntersect(FillingRule clippingRule);

        /// <inheritdoc/>
        public virtual void SetNamedGraphicsState(NameToken stateName)
        {
            var currentGraphicsState = GetCurrentState();

            var state = ResourceStore.GetExtendedGraphicsStateDictionary(stateName);

            if (state.TryGet(NameToken.Lw, PdfScanner, out NumericToken lwToken))
            {
                currentGraphicsState.LineWidth = lwToken.Data;
            }

            if (state.TryGet(NameToken.Lc, PdfScanner, out NumericToken lcToken))
            {
                currentGraphicsState.CapStyle = (LineCapStyle)lcToken.Int;
            }

            if (state.TryGet(NameToken.Lj, PdfScanner, out NumericToken ljToken))
            {
                currentGraphicsState.JoinStyle = (LineJoinStyle)ljToken.Int;
            }

            if (state.TryGet(NameToken.Font, PdfScanner, out ArrayToken fontArray) && fontArray.Length == 2
                && fontArray.Data[0] is IndirectReferenceToken fontReference && fontArray.Data[1] is NumericToken sizeToken)
            {
                currentGraphicsState.FontState.FromExtendedGraphicsState = true;
                currentGraphicsState.FontState.FontSize = (double)sizeToken.Data;
                ActiveExtendedGraphicsStateFont = ResourceStore.GetFontDirectly(fontReference);
            }

            if (state.TryGet(NameToken.Ais, PdfScanner, out BooleanToken aisToken))
            {
                // The alpha source flag (“alpha is shape”), specifying
                // whether the current soft mask and alpha constant are to be interpreted as
                // shape values (true) or opacity values (false).
                currentGraphicsState.AlphaSource = aisToken.Data;
            }

            if (state.TryGet(NameToken.Ca, PdfScanner, out NumericToken caToken))
            {
                // (Optional; PDF 1.4) The current stroking alpha constant, specifying the constant
                // shape or constant opacity value to be used for stroking operations in the
                // transparent imaging model (see “Source Shape and Opacity” on page 526 and
                // “Constant Shape and Opacity” on page 551).
                currentGraphicsState.AlphaConstantStroking = caToken.Data;
            }

            if (state.TryGet(NameToken.CaNs, PdfScanner, out NumericToken cansToken))
            {
                // (Optional; PDF 1.4) The current stroking alpha constant, specifying the constant
                // shape or constant opacity value to be used for NON-stroking operations in the
                // transparent imaging model (see “Source Shape and Opacity” on page 526 and
                // “Constant Shape and Opacity” on page 551).
                currentGraphicsState.AlphaConstantNonStroking = cansToken.Data;
            }

            if (state.TryGet(NameToken.Op, PdfScanner, out BooleanToken OPToken))
            {
                // (Optional) A flag specifying whether to apply overprint (see Section 4.5.6,
                // “Overprint Control”). In PDF 1.2 and earlier, there is a single overprint
                // parameter that applies to all painting operations. Beginning with PDF 1.3,
                // there are two separate overprint parameters: one for stroking and one for all
                // other painting operations. Specifying an OP entry sets both parameters unless there
                // is also an op entry in the same graphics state parameter dictionary,
                // in which case the OP entry sets only the overprint parameter for stroking.
                currentGraphicsState.Overprint = OPToken.Data;
            }

            if (state.TryGet(NameToken.OpNs, PdfScanner, out BooleanToken opToken))
            {
                // (Optional; PDF 1.3) A flag specifying whether to apply overprint (see Section
                // 4.5.6, “Overprint Control”) for painting operations other than stroking. If
                // this entry is absent, the OP entry, if any, sets this parameter.
                currentGraphicsState.NonStrokingOverprint = opToken.Data;
            }

            if (state.TryGet(NameToken.Opm, PdfScanner, out NumericToken opmToken))
            {
                // (Optional; PDF 1.3) The overprint mode (see Section 4.5.6, “Overprint Control”).
                currentGraphicsState.OverprintMode = opmToken.Data;
            }

            if (state.TryGet(NameToken.Sa, PdfScanner, out BooleanToken saToken))
            {
                // (Optional) A flag specifying whether to apply automatic stroke adjustment
                // (see Section 6.5.4, “Automatic Stroke Adjustment”).
                currentGraphicsState.StrokeAdjustment = saToken.Data;
            }
        }

        /// <inheritdoc/>
        public virtual void BeginInlineImage()
        {
            if (InlineImageBuilder != null)
            {
                ParsingOptions.Logger.Error("Begin inline image (BI) command encountered while another inline image was active.");
            }

            InlineImageBuilder = new InlineImageBuilder();
        }

        /// <inheritdoc/>
        public virtual void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
            if (InlineImageBuilder == null)
            {
                ParsingOptions.Logger.Error("Begin inline image data (ID) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            InlineImageBuilder.Properties = properties;
        }

        /// <inheritdoc/>
        public virtual void EndInlineImage(IReadOnlyList<byte> bytes)
        {
            if (InlineImageBuilder == null)
            {
                ParsingOptions.Logger.Error("End inline image (EI) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            InlineImageBuilder.Bytes = bytes;

            var image = InlineImageBuilder.CreateInlineImage(CurrentTransformationMatrix, FilterProvider, PdfScanner, GetCurrentState().RenderingIntent, ResourceStore);

            RenderInlineImage(image);

            InlineImageBuilder = null;
        }

        /// <summary>
        /// Render Inline image implementation.
        /// </summary>
        public abstract void RenderInlineImage(InlineImage inlineImage);

        /// <inheritdoc/>
        public abstract void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties);

        /// <inheritdoc/>
        public abstract void EndMarkedContent();

        private void AdjustTextMatrix(double tx, double ty)
        {
            var matrix = TransformationMatrix.GetTranslationMatrix(tx, ty);
            TextMatrices.TextMatrix = matrix.Multiply(TextMatrices.TextMatrix);
        }

        /// <inheritdoc/>
        public virtual void SetFlatnessTolerance(decimal tolerance)
        {
            GetCurrentState().Flatness = tolerance;
        }

        /// <inheritdoc/>
        public virtual void SetLineCap(LineCapStyle cap)
        {
            GetCurrentState().CapStyle = cap;
        }

        /// <inheritdoc/>
        public virtual void SetLineDashPattern(LineDashPattern pattern)
        {
            GetCurrentState().LineDashPattern = pattern;
        }

        /// <inheritdoc/>
        public virtual void SetLineJoin(LineJoinStyle join)
        {
            GetCurrentState().JoinStyle = join;
        }

        /// <inheritdoc/>
        public virtual void SetLineWidth(decimal width)
        {
            GetCurrentState().LineWidth = width;
        }

        /// <inheritdoc/>
        public virtual void SetMiterLimit(decimal limit)
        {
            GetCurrentState().MiterLimit = limit;
        }

        /// <inheritdoc/>
        public virtual void MoveToNextLineWithOffset()
        {
            var tdOperation = new MoveToNextLineWithOffset(0, -1 * (decimal)GetCurrentState().FontState.Leading);
            tdOperation.Run(this);
        }

        /// <inheritdoc/>
        public virtual void SetFontAndSize(NameToken font, double size)
        {
            var currentState = GetCurrentState();
            currentState.FontState.FontSize = size;
            currentState.FontState.FontName = font;
        }

        /// <inheritdoc/>
        public virtual void SetHorizontalScaling(double scale)
        {
            GetCurrentState().FontState.HorizontalScaling = scale;
        }

        /// <inheritdoc/>
        public virtual void SetTextLeading(double leading)
        {
            GetCurrentState().FontState.Leading = leading;
        }

        /// <inheritdoc/>
        public virtual void SetTextRenderingMode(TextRenderingMode mode)
        {
            GetCurrentState().FontState.TextRenderingMode = mode;
        }

        /// <inheritdoc/>
        public virtual void SetTextRise(double rise)
        {
            GetCurrentState().FontState.Rise = rise;
        }

        /// <inheritdoc/>
        public virtual void SetWordSpacing(double spacing)
        {
            GetCurrentState().FontState.WordSpacing = spacing;
        }

        /// <inheritdoc/>
        public virtual void ModifyCurrentTransformationMatrix(double[] value)
        {
            var ctm = GetCurrentState().CurrentTransformationMatrix;
            GetCurrentState().CurrentTransformationMatrix = TransformationMatrix.FromArray(value).Multiply(ctm);
        }

        /// <inheritdoc/>
        public virtual void SetCharacterSpacing(double spacing)
        {
            GetCurrentState().FontState.CharacterSpacing = spacing;
        }

        /// <inheritdoc/>
        public abstract void PaintShading(NameToken shadingName);
    }
}
