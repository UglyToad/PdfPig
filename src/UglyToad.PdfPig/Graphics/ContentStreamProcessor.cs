namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Content;
    using Core;
    using Filters;
    using Geometry;
    using Logging;
    using Operations;
    using Parser;
    using PdfFonts;
    using PdfPig.Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Graphics.Operations.TextPositioning;
    using XObjects;
    using static PdfPig.Core.PdfSubpath;

    internal class ContentStreamProcessor : IOperationContext
    {
        /// <summary>
        /// Stores each letter as it is encountered in the content stream.
        /// </summary>
        private readonly List<Letter> letters = new List<Letter>();

        /// <summary>
        /// Stores each path as it is encountered in the content stream.
        /// </summary>
        private readonly List<PdfPath> paths = new List<PdfPath>();

        /// <summary>
        /// Stores a link to each image (either inline or XObject) as it is encountered in the content stream.
        /// </summary>
        private readonly List<Union<XObjectContentRecord, InlineImage>> images = new List<Union<XObjectContentRecord, InlineImage>>();

        /// <summary>
        /// Stores each marked content as it is encountered in the content stream.
        /// </summary>
        private readonly List<MarkedContentElement> markedContents = new List<MarkedContentElement>();

        private readonly IResourceStore resourceStore;
        private readonly UserSpaceUnit userSpaceUnit;
        private readonly PageRotationDegrees rotation;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IPageContentParser pageContentParser;
        private readonly IFilterProvider filterProvider;
        private readonly ILog log;
        private readonly bool clipPaths;
        private readonly PdfVector pageSize;
        private readonly MarkedContentStack markedContentStack = new MarkedContentStack();

        private Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();
        private IFont activeExtendedGraphicsStateFont;
        private InlineImageBuilder inlineImageBuilder;
        private int pageNumber;

        /// <summary>
        /// A counter to track individual calls to <see cref="ShowText"/> operations used to determine if letters are likely to be
        /// in the same word/group. This exposes internal grouping of letters used by the PDF creator which may correspond to the
        /// intended grouping of letters into words.
        /// </summary>
        private int textSequence;

        public TextMatrices TextMatrices { get; } = new TextMatrices();

        public TransformationMatrix CurrentTransformationMatrix => GetCurrentState().CurrentTransformationMatrix;

        public PdfSubpath CurrentSubpath { get; private set; }

        public PdfPath CurrentPath { get; private set; }

        public IColorSpaceContext ColorSpaceContext { get; }

        public PdfPoint CurrentPosition { get; set; }

        public int StackSize => graphicsStack.Count;

        private readonly Dictionary<XObjectType, List<XObjectContentRecord>> xObjects = new Dictionary<XObjectType, List<XObjectContentRecord>>
        {
            {XObjectType.Image, new List<XObjectContentRecord>()},
            {XObjectType.PostScript, new List<XObjectContentRecord>()}
        };

        public ContentStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore, UserSpaceUnit userSpaceUnit, PageRotationDegrees rotation,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            IFilterProvider filterProvider,
            ILog log,
            bool clipPaths,
            PdfVector pageSize)
        {
            this.resourceStore = resourceStore;
            this.userSpaceUnit = userSpaceUnit;
            this.rotation = rotation;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pageContentParser = pageContentParser ?? throw new ArgumentNullException(nameof(pageContentParser));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.log = log;
            this.clipPaths = clipPaths;
            this.pageSize = pageSize;

            // initiate CurrentClippingPath to cropBox
            var clippingSubpath = new PdfSubpath();
            clippingSubpath.Rectangle(cropBox.BottomLeft.X, cropBox.BottomLeft.Y, cropBox.Width, cropBox.Height);
            var clippingPath = new PdfPath() { clippingSubpath };
            clippingPath.SetClipping(FillingRule.EvenOdd);

            graphicsStack.Push(new CurrentGraphicsState()
            {
                CurrentTransformationMatrix = GetInitialMatrix(),
                CurrentClippingPath = clippingPath
            });

            ColorSpaceContext = new ColorSpaceContext(GetCurrentState, resourceStore);
        }

        [System.Diagnostics.Contracts.Pure]
        private TransformationMatrix GetInitialMatrix()
        {
            // TODO: this is a bit of a hack because I don't understand matrices
            // TODO: use MediaBox (i.e. pageSize) or CropBox?

            /* 
             * There should be a single Affine Transform we can apply to any point resulting
             * from a content stream operation which will rotate the point and translate it back to
             * a point where the origin is in the page's lower left corner.
             *
             * For example this matrix represents a (clockwise) rotation and translation:
             * [  cos  sin  tx ]
             * [ -sin  cos  ty ]
             * [    0    0   1 ]
             * Warning: rotation is counter-clockwise here
             * 
             * The values of tx and ty are those required to move the origin back to the expected origin (lower-left).
             * The corresponding values should be:
             * Rotation:  0   90  180  270
             *       tx:  0    0    w    w
             *       ty:  0    h    h    0
             *
             * Where w and h are the page width and height after rotation.
            */

            double cos, sin;
            double dx = 0, dy = 0;
            switch (rotation.Value)
            {
                case 0:
                    cos = 1;
                    sin = 0;
                    break;
                case 90:
                    cos = 0;
                    sin = 1;
                    dy = pageSize.Y;
                    break;
                case 180:
                    cos = -1;
                    sin = 0;
                    dx = pageSize.X;
                    dy = pageSize.Y;
                    break;
                case 270:
                    cos = 0;
                    sin = -1;
                    dx = pageSize.X;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value for page rotation: {rotation.Value}.");
            }

            return new TransformationMatrix(
                cos, -sin, 0,
                sin, cos, 0,
                dx, dy, 1);
        }

        public PageContent Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            pageNumber = pageNumberCurrent;
            CloneAllStates();

            ProcessOperations(operations);

            return new PageContent(operations, letters, paths, images, markedContents, pdfScanner, pageContentParser, filterProvider, resourceStore);
        }

        private void ProcessOperations(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            foreach (var stateOperation in operations)
            {
                stateOperation.Run(this);
            }
        }

        private Stack<CurrentGraphicsState> CloneAllStates()
        {
            var saved = graphicsStack;
            graphicsStack = new Stack<CurrentGraphicsState>();
            graphicsStack.Push(saved.Peek().DeepClone());
            return saved;
        }

        [DebuggerStepThrough]
        public CurrentGraphicsState GetCurrentState()
        {
            return graphicsStack.Peek();
        }

        public void PopState()
        {
            graphicsStack.Pop();
            activeExtendedGraphicsStateFont = null;
        }

        public void PushState()
        {
            graphicsStack.Push(graphicsStack.Peek().DeepClone());
        }

        public void ShowText(IInputBytes bytes)
        {
            var currentState = GetCurrentState();

            var font = currentState.FontState.FromExtendedGraphicsState ? activeExtendedGraphicsStateFont : resourceStore.GetFont(currentState.FontState.FontName);

            if (font == null)
            {
                throw new InvalidOperationException($"Could not find the font with name {currentState.FontState.FontName} in the resource store. It has not been loaded yet.");
            }

            var fontSize = currentState.FontState.FontSize;
            var horizontalScaling = currentState.FontState.HorizontalScaling / 100.0;
            var characterSpacing = currentState.FontState.CharacterSpacing;
            var rise = currentState.FontState.Rise;

            var transformationMatrix = currentState.CurrentTransformationMatrix;

            var renderingMatrix =
                TransformationMatrix.FromValues(fontSize * horizontalScaling, 0, 0, fontSize, 0, rise);

            // TODO: this does not seem correct, produces the correct result for now but we need to revisit.
            // see: https://stackoverflow.com/questions/48010235/pdf-specification-get-font-size-in-points
            var fontSizeMatrix = transformationMatrix.Multiply(TextMatrices.TextMatrix).Multiply(fontSize);
            var pointSize = Math.Round(fontSizeMatrix.A, 2);
            // Assume a rotated letter
            if (pointSize == 0)
            {
                pointSize = Math.Round(fontSizeMatrix.B, 2);
            }

            if (pointSize < 0)
            {
                pointSize *= -1;
            }

            while (bytes.MoveNext())
            {
                var code = font.ReadCharacterCode(bytes, out int codeLength);

                var foundUnicode = font.TryGetUnicode(code, out var unicode);

                if (!foundUnicode || unicode == null)
                {
                    log.Warn($"We could not find the corresponding character with code {code} in font {font.Name}.");
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

                var transformedGlyphBounds = PerformantRectangleTransformer
                    .Transform(renderingMatrix, textMatrix, transformationMatrix, boundingBox.GlyphBounds);

                var transformedPdfBounds = PerformantRectangleTransformer
                    .Transform(renderingMatrix, textMatrix, transformationMatrix, new PdfRectangle(0, 0, boundingBox.Width, 0));

                // If the text rendering mode calls for filling, the current nonstroking color in the graphics state is used; 
                // if it calls for stroking, the current stroking color is used.
                // In modes that perform both filling and stroking, the effect is as if each glyph outline were filled and then stroked in separate operations.
                // TODO: expose color as something more advanced
                var color = currentState.FontState.TextRenderingMode != TextRenderingMode.Stroke
                    ? currentState.CurrentNonStrokingColor
                    : currentState.CurrentStrokingColor;

                var letter = new Letter(unicode, transformedGlyphBounds,
                    transformedPdfBounds.BottomLeft,
                    transformedPdfBounds.BottomRight,
                    transformedPdfBounds.Width,
                    fontSize,
                    font.Details,
                    color,
                    pointSize,
                    textSequence);

                letters.Add(letter);

                markedContentStack.AddLetter(letter);

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

        public void ShowPositionedText(IReadOnlyList<IToken> tokens)
        {
            textSequence++;

            var currentState = GetCurrentState();

            var textState = currentState.FontState;

            var fontSize = textState.FontSize;
            var horizontalScaling = textState.HorizontalScaling / 100.0;
            var font = resourceStore.GetFont(textState.FontName);

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

        public void ApplyXObject(NameToken xObjectName)
        {
            var xObjectStream = resourceStore.GetXObject(xObjectName);

            // For now we will determine the type and store the object with the graphics state information preceding it.
            // Then consumers of the page can request the object(s) to be retrieved by type.
            var subType = (NameToken)xObjectStream.StreamDictionary.Data[NameToken.Subtype.Data];

            var state = GetCurrentState();

            var matrix = state.CurrentTransformationMatrix;

            if (subType.Equals(NameToken.Ps))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.PostScript, xObjectStream, matrix, state.RenderingIntent,
                    state.CurrentStrokingColor?.ColorSpace ?? ColorSpace.DeviceRGB);

                xObjects[XObjectType.PostScript].Add(contentRecord);
            }
            else if (subType.Equals(NameToken.Image))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.Image, xObjectStream, matrix, state.RenderingIntent,
                    state.CurrentStrokingColor?.ColorSpace ?? ColorSpace.DeviceRGB);

                images.Add(Union<XObjectContentRecord, InlineImage>.One(contentRecord));

                markedContentStack.AddXObject(contentRecord, pdfScanner, filterProvider, resourceStore);
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

        private void ProcessFormXObject(StreamToken formStream)
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

            var hasResources = formStream.StreamDictionary.TryGet<DictionaryToken>(NameToken.Resources, pdfScanner, out var formResources);
            if (hasResources)
            {
                resourceStore.LoadResourceDictionary(formResources);
            }

            // 1. Save current state.
            PushState();

            var startState = GetCurrentState();

            var formMatrix = TransformationMatrix.Identity;
            if (formStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Matrix, pdfScanner, out var formMatrixToken))
            {
                formMatrix = TransformationMatrix.FromArray(formMatrixToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray());
            }

            // 2. Update current transformation matrix.
            var resultingTransformationMatrix = formMatrix.Multiply(startState.CurrentTransformationMatrix);

            startState.CurrentTransformationMatrix = resultingTransformationMatrix;

            var contentStream = formStream.Decode(filterProvider);

            var operations = pageContentParser.Parse(pageNumber, new ByteArrayInputBytes(contentStream), log);

            // 3. We don't respect clipping currently.

            // 4. Paint the objects.
            ProcessOperations(operations);

            // 5. Restore saved state.
            PopState();

            if (hasResources)
            {
                resourceStore.UnloadResourceDictionary();
            }
        }

        public void BeginSubpath()
        {
            if (CurrentPath == null)
            {
                CurrentPath = new PdfPath();
            }

            AddCurrentSubpath();
            CurrentSubpath = new PdfSubpath();
        }

        public PdfPoint? CloseSubpath()
        {
            if (CurrentSubpath == null)
            {
                return null;
            }

            PdfPoint point;
            if (CurrentSubpath.Commands[0] is Move move)
            {
                point = move.Location;
            }
            else
            {
                throw new ArgumentException("CloseSubpath(): first command not Move.");
            }

            CurrentSubpath.CloseSubpath();
            AddCurrentSubpath();
            return point;
        }

        public void AddCurrentSubpath()
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            CurrentPath.Add(CurrentSubpath);
            CurrentSubpath = null;
        }

        public void StrokePath(bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.SetStroked();

            if (close)
            {
                CurrentSubpath?.CloseSubpath();
            }

            ClosePath();
        }

        public void FillPath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.SetFilled(fillingRule);

            if (close)
            {
                CurrentSubpath?.CloseSubpath();
            }

            ClosePath();
        }

        public void FillStrokePath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.SetFilled(fillingRule);
            CurrentPath.SetStroked();

            if (close)
            {
                CurrentSubpath?.CloseSubpath();
            }

            ClosePath();
        }

        public void PaintShading(NameToken name)
        {
            
        }

        public void MoveTo(double x, double y)
        {
            BeginSubpath();
            var point = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            CurrentPosition = point;
            CurrentSubpath.MoveTo(point.X, point.Y);
        }

        public void BezierCurveTo(double x2, double y2, double x3, double y3)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(CurrentPosition.X, CurrentPosition.Y, controlPoint2.X, controlPoint2.Y, end.X, end.Y);
            CurrentPosition = end;
        }

        public void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var controlPoint1 = CurrentTransformationMatrix.Transform(new PdfPoint(x1, y1));
            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, end.X, end.Y);
            CurrentPosition = end;
        }

        public void LineTo(double x, double y)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var endPoint = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));

            CurrentSubpath.LineTo(endPoint.X, endPoint.Y);
            CurrentPosition = endPoint;
        }

        public void Rectangle(double x, double y, double width, double height)
        {
            BeginSubpath();
            var lowerLeft = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            var upperRight = CurrentTransformationMatrix.Transform(new PdfPoint(x + width, y + height));

            CurrentSubpath.Rectangle(lowerLeft.X, lowerLeft.Y, upperRight.X - lowerLeft.X, upperRight.Y - lowerLeft.Y);
            AddCurrentSubpath();
        }

        public void EndPath()
        {
            if (CurrentPath == null)
            {
                return;
            }

            AddCurrentSubpath();

            if (CurrentPath.IsClipping)
            {
                if (!clipPaths)
                {
                    // if we don't clip paths, add clipping path to paths
                    paths.Add(CurrentPath);
                    markedContentStack.AddPath(CurrentPath);
                }
                CurrentPath = null;
                return;
            }

            paths.Add(CurrentPath);
            markedContentStack.AddPath(CurrentPath);
            CurrentPath = null;
        }

        public void ClosePath()
        {
            AddCurrentSubpath();

            if (CurrentPath.IsClipping)
            {
                EndPath();
                return;
            }

            var currentState = GetCurrentState();
            if (CurrentPath.IsStroked)
            {
                CurrentPath.LineDashPattern = currentState.LineDashPattern;
                CurrentPath.StrokeColor = currentState.CurrentStrokingColor;
                CurrentPath.LineWidth = currentState.LineWidth;
                CurrentPath.LineCapStyle = currentState.CapStyle;
                CurrentPath.LineJoinStyle = currentState.JoinStyle;
            }

            if (CurrentPath.IsFilled)
            {
                CurrentPath.FillColor = currentState.CurrentNonStrokingColor;
            }

            if (clipPaths)
            {
                var clippedPath = currentState.CurrentClippingPath.Clip(CurrentPath, log);
                if (clippedPath != null)
                {
                    paths.Add(clippedPath);
                    markedContentStack.AddPath(clippedPath);
                }
            }
            else
            {
                paths.Add(CurrentPath);
                markedContentStack.AddPath(CurrentPath);
            }

            CurrentPath = null;
        }

        public void ModifyClippingIntersect(FillingRule clippingRule)
        {
            if (CurrentPath == null)
            {
                return;
            }

            AddCurrentSubpath();
            CurrentPath.SetClipping(clippingRule);

            if (clipPaths)
            {
                var currentClipping = GetCurrentState().CurrentClippingPath;
                currentClipping.SetClipping(clippingRule);

                var newClippings = CurrentPath.Clip(currentClipping, log);
                if (newClippings == null)
                {
                    log.Warn("Empty clipping path found. Clipping path not updated.");
                }
                else
                {
                    GetCurrentState().CurrentClippingPath = newClippings;
                }
            }
        }

        public void SetNamedGraphicsState(NameToken stateName)
        {
            var currentGraphicsState = GetCurrentState();

            var state = resourceStore.GetExtendedGraphicsStateDictionary(stateName);

            if (state.TryGet(NameToken.Lw, pdfScanner, out NumericToken lwToken))
            {
                currentGraphicsState.LineWidth = lwToken.Data;
            }

            if (state.TryGet(NameToken.Lc, pdfScanner, out NumericToken lcToken))
            {
                currentGraphicsState.CapStyle = (LineCapStyle)lcToken.Int;
            }

            if (state.TryGet(NameToken.Lj, pdfScanner, out NumericToken ljToken))
            {
                currentGraphicsState.JoinStyle = (LineJoinStyle)ljToken.Int;
            }

            if (state.TryGet(NameToken.Font, pdfScanner, out ArrayToken fontArray) && fontArray.Length == 2
                && fontArray.Data[0] is IndirectReferenceToken fontReference && fontArray.Data[1] is NumericToken sizeToken)
            {
                currentGraphicsState.FontState.FromExtendedGraphicsState = true;
                currentGraphicsState.FontState.FontSize = (double)sizeToken.Data;
                activeExtendedGraphicsStateFont = resourceStore.GetFontDirectly(fontReference);
            }
        }

        public void BeginInlineImage()
        {
            if (inlineImageBuilder != null)
            {
                log?.Error("Begin inline image (BI) command encountered while another inline image was active.");
            }

            inlineImageBuilder = new InlineImageBuilder();
        }

        public void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
            if (inlineImageBuilder == null)
            {
                log?.Error("Begin inline image data (ID) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Properties = properties;
        }

        public void EndInlineImage(IReadOnlyList<byte> bytes)
        {
            if (inlineImageBuilder == null)
            {
                log?.Error("End inline image (EI) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Bytes = bytes;

            var image = inlineImageBuilder.CreateInlineImage(CurrentTransformationMatrix, filterProvider, pdfScanner, GetCurrentState().RenderingIntent, resourceStore);

            images.Add(Union<XObjectContentRecord, InlineImage>.Two(image));

            markedContentStack.AddImage(image);

            inlineImageBuilder = null;
        }

        public void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties)
        {
            if (propertyDictionaryName != null)
            {
                var actual = resourceStore.GetMarkedContentPropertiesDictionary(propertyDictionaryName);

                properties = actual ?? properties;
            }

            markedContentStack.Push(name, properties);
        }

        public void EndMarkedContent()
        {
            if (markedContentStack.CanPop)
            {
                var mc = markedContentStack.Pop(pdfScanner);
                if (mc != null)
                {
                    markedContents.Add(mc);
                }
            }
        }

        private void AdjustTextMatrix(double tx, double ty)
        {
            var matrix = TransformationMatrix.GetTranslationMatrix(tx, ty);
            TextMatrices.TextMatrix = matrix.Multiply(TextMatrices.TextMatrix);
        }

        public void SetFlatnessTolerance(decimal tolerance)
        {
            GetCurrentState().Flatness = tolerance;
        }

        public void SetLineCap(LineCapStyle cap)
        {
            GetCurrentState().CapStyle = cap;
        }

        public void SetLineDashPattern(LineDashPattern pattern)
        {
            GetCurrentState().LineDashPattern = pattern;
        }

        public void SetLineJoin(LineJoinStyle join)
        {
            GetCurrentState().JoinStyle = join;
        }

        public void SetLineWidth(decimal width)
        {
            GetCurrentState().LineWidth = width;
        }

        public void SetMiterLimit(decimal limit)
        {
            GetCurrentState().MiterLimit = limit;
        }

        public void MoveToNextLineWithOffset()
        {
            var tdOperation = new MoveToNextLineWithOffset(0, -1 * (decimal)GetCurrentState().FontState.Leading);
            tdOperation.Run(this);
        }

        public void SetFontAndSize(NameToken font, double size)
        {
            var currentState = GetCurrentState();
            currentState.FontState.FontSize = size;
            currentState.FontState.FontName = font;
        }

        public void SetHorizontalScaling(double scale)
        {
            GetCurrentState().FontState.HorizontalScaling = scale;
        }

        public void SetTextLeading(double leading)
        {
            GetCurrentState().FontState.Leading = leading;
        }

        public void SetTextRenderingMode(TextRenderingMode mode)
        {
            GetCurrentState().FontState.TextRenderingMode = mode;
        }

        public void SetTextRise(double rise)
        {
            GetCurrentState().FontState.Rise = rise;
        }

        public void SetWordSpacing(double spacing)
        {
            GetCurrentState().FontState.WordSpacing = spacing;
        }

        public void ModifyCurrentTransformationMatrix(double[] value)
        {
            var ctm = GetCurrentState().CurrentTransformationMatrix;
            GetCurrentState().CurrentTransformationMatrix = TransformationMatrix.FromArray(value).Multiply(ctm);
        }

        public void SetCharacterSpacing(double spacing)
        {
            GetCurrentState().FontState.CharacterSpacing = spacing;
        }

        public void BeginText()
        {
            TextMatrices.TextMatrix = TransformationMatrix.Identity;
            TextMatrices.TextLineMatrix = TransformationMatrix.Identity;
        }

        public void EndText()
        {
            TextMatrices.TextMatrix = TransformationMatrix.Identity;
            TextMatrices.TextLineMatrix = TransformationMatrix.Identity;
        }

        public void SetTextMatrix(double[] value)
        {
            var newMatrix = TransformationMatrix.FromArray(value);

            TextMatrices.TextMatrix = newMatrix;
            TextMatrices.TextLineMatrix = newMatrix;
        }

        public void MoveToNextLineWithOffset(double tx, double ty)
        {
            var currentTextLineMatrix = TextMatrices.TextLineMatrix;

            var matrix = TransformationMatrix.FromValues(1, 0, 0, 1, (double)tx, (double)ty);

            var transformed = matrix.Multiply(currentTextLineMatrix);

            TextMatrices.TextLineMatrix = transformed;
            TextMatrices.TextMatrix = transformed;
        }
    }
}