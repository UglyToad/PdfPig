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
    using Operations.TextPositioning;
    using Util;
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
        private readonly ILookupFilterProvider filterProvider;
        private readonly InternalParsingOptions parsingOptions;
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

        public PdfPoint CurrentPosition { get; set; }

        public int StackSize => graphicsStack.Count;

        private readonly Dictionary<XObjectType, List<XObjectContentRecord>> xObjects = new Dictionary<XObjectType, List<XObjectContentRecord>>
        {
            {XObjectType.Image, new List<XObjectContentRecord>()},
            {XObjectType.PostScript, new List<XObjectContentRecord>()}
        };

        public ContentStreamProcessor(
            int pageNumber,
            IResourceStore resourceStore,
            UserSpaceUnit userSpaceUnit,
            MediaBox mediaBox,
            CropBox cropBox,
            PageRotationDegrees rotation,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            InternalParsingOptions parsingOptions)
        {
            this.pageNumber = pageNumber;
            this.resourceStore = resourceStore;
            this.userSpaceUnit = userSpaceUnit;
            this.rotation = rotation;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pageContentParser = pageContentParser ?? throw new ArgumentNullException(nameof(pageContentParser));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.parsingOptions = parsingOptions;

            // initiate CurrentClippingPath to cropBox
            var clippingSubpath = new PdfSubpath();
            clippingSubpath.Rectangle(cropBox.Bounds.BottomLeft.X, cropBox.Bounds.BottomLeft.Y, cropBox.Bounds.Width, cropBox.Bounds.Height);
            var clippingPath = new PdfPath() { clippingSubpath };
            clippingPath.SetClipping(FillingRule.EvenOdd);

            graphicsStack.Push(new CurrentGraphicsState()
            {
                CurrentTransformationMatrix = GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, parsingOptions.Logger),
                CurrentClippingPath = clippingPath,
                ColorSpaceContext = new ColorSpaceContext(GetCurrentState, resourceStore)
            });
        }

        [System.Diagnostics.Contracts.Pure]
        internal static TransformationMatrix GetInitialMatrix(UserSpaceUnit userSpaceUnit,
            MediaBox mediaBox,
            CropBox cropBox,
            PageRotationDegrees rotation,
            ILog log)
        {
            // Cater for scenario where the cropbox is larger than the mediabox.
            // If there is no intersection (method returns null), fall back to the cropbox.
            var viewBox = mediaBox.Bounds.Intersect(cropBox.Bounds) ?? cropBox.Bounds;

            if (rotation.Value == 0
                && viewBox.Left == 0 
                && viewBox.Bottom == 0
                && userSpaceUnit.PointMultiples == 1)
            {
                return TransformationMatrix.Identity;
            }

            // Move points so that (0,0) is equal to the viewbox bottom left corner.
            var t1 = TransformationMatrix.GetTranslationMatrix(-viewBox.Left, -viewBox.Bottom);

            if (userSpaceUnit.PointMultiples != 1)
            {
                log.Warn("User space unit other than 1 is not implemented");
            }

            // After rotating around the origin, our points will have negative x/y coordinates.
            // Fix this by translating them by a certain dx/dy after rotation based on the viewbox.
            double dx, dy;
            switch (rotation.Value)
            {
                case 0:
                    // No need to rotate / translate after rotation, just return the initial
                    // translation matrix.
                    return t1;
                case 90:
                    // Move rotated points up by our (unrotated) viewbox width
                    dx = 0;
                    dy = viewBox.Width;
                    break;
                case 180:
                    // Move rotated points up/right using the (unrotated) viewbox width/height
                    dx = viewBox.Width;
                    dy = viewBox.Height;
                    break;
                case 270:
                    // Move rotated points right using the (unrotated) viewbox height
                    dx = viewBox.Height;
                    dy = 0;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value for page rotation: {rotation.Value}.");
            }

            // GetRotationMatrix uses counter clockwise angles, whereas our page rotation
            // is a clockwise angle, so flip the sign.
            var r = TransformationMatrix.GetRotationMatrix(-rotation.Value);

            // Fix up negative coordinates after rotation
            var t2 = TransformationMatrix.GetTranslationMatrix(dx, dy);

            // Now get the final combined matrix T1 > R > T2
            return t1.Multiply(r.Multiply(t2));
        }

        public PageContent Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            pageNumber = pageNumberCurrent;
            CloneAllStates();

            ProcessOperations(operations);

            return new PageContent(operations, letters, paths, images, markedContents, pdfScanner, filterProvider, resourceStore);
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
                if (parsingOptions.SkipMissingFonts)
                {
                    parsingOptions.Logger.Warn($"Skipping a missing font with name {currentState.FontState.FontName} " +
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
                    parsingOptions.Logger.Warn($"We could not find the corresponding character with code {code} in font {font.Name}.");

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

                      
                Letter letter = null;
                if (Diacritics.IsInCombiningDiacriticRange(unicode) && bytes.CurrentOffset > 0 && letters.Count > 0)
                {
                    var attachTo = letters[letters.Count - 1];

                    if (attachTo.TextSequence == textSequence
                        && Diacritics.TryCombineDiacriticWithPreviousLetter(unicode, attachTo.Value, out var newLetter))
                    {
                        // TODO: union of bounding boxes.
                        letters.Remove(attachTo);

                        letter = new Letter(
                            newLetter,
                            attachTo.GlyphRectangle,
                            attachTo.StartBaseLine,
                            attachTo.EndBaseLine,
                            attachTo.Width,
                            attachTo.FontSize,
                            attachTo.Font,
                            attachTo.RenderingMode,
                            attachTo.StrokeColor,
                            attachTo.FillColor,
                            attachTo.PointSize,
                            attachTo.TextSequence);
                    }
                }

                // If we did not create a letter for a combined diacritic, create one here.
                if (letter == null)
                {
                    letter = new Letter(
                        unicode,
                        transformedGlyphBounds,
                        transformedPdfBounds.BottomLeft,
                        transformedPdfBounds.BottomRight,
                        transformedPdfBounds.Width,
                        fontSize,
                        font.Details,
                        currentState.FontState.TextRenderingMode,
                        currentState.CurrentStrokingColor,
                        currentState.CurrentNonStrokingColor,
                        pointSize,
                        textSequence);
                }

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
            if (!resourceStore.TryGetXObject(xObjectName, out var xObjectStream))
            {
                if (parsingOptions.SkipMissingFonts)
                {
                    return;
                }

                throw new PdfDocumentFormatException($"No XObject with name {xObjectName} found on page {pageNumber}.");
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
                resourceStore.LoadResourceDictionary(formResources, parsingOptions);
            }

            // 1. Save current state.
            PushState();

            var startState = GetCurrentState();

            // Transparency Group XObjects
            if (formStream.StreamDictionary.TryGet(NameToken.Group, pdfScanner, out DictionaryToken formGroupToken))
            {
                if (!formGroupToken.TryGet<NameToken>(NameToken.S, pdfScanner, out var sToken) || sToken != NameToken.Transparency)
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

                if (formGroupToken.TryGet(NameToken.Cs, pdfScanner, out NameToken csNameToken))
                {
                    startState.ColorSpaceContext.SetNonStrokingColorspace(csNameToken);
                }
                else if (formGroupToken.TryGet(NameToken.Cs, pdfScanner, out ArrayToken csArrayToken)
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
                if (formGroupToken.TryGet(NameToken.I, pdfScanner, out BooleanToken isolatedToken))
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
                if (formGroupToken.TryGet(NameToken.K, pdfScanner, out BooleanToken knockoutToken))
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
            if (formStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Matrix, pdfScanner, out var formMatrixToken))
            {
                formMatrix = TransformationMatrix.FromArray(formMatrixToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray());
            }

            // 2. Update current transformation matrix.
            startState.CurrentTransformationMatrix = formMatrix.Multiply(startState.CurrentTransformationMatrix);

            var contentStream = formStream.Decode(filterProvider, pdfScanner);

            var operations = pageContentParser.Parse(pageNumber, new ByteArrayInputBytes(contentStream), parsingOptions.Logger);

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
                if (!parsingOptions.ClipPaths)
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

            if (parsingOptions.ClipPaths)
            {
                var clippedPath = currentState.CurrentClippingPath.Clip(CurrentPath, parsingOptions.Logger);
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

            if (parsingOptions.ClipPaths)
            {
                var currentClipping = GetCurrentState().CurrentClippingPath;
                currentClipping.SetClipping(clippingRule);

                var newClippings = CurrentPath.Clip(currentClipping, parsingOptions.Logger);
                if (newClippings == null)
                {
                    parsingOptions.Logger.Warn("Empty clipping path found. Clipping path not updated.");
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

            if (state.TryGet(NameToken.Ais, pdfScanner, out BooleanToken aisToken))
            {
                // The alpha source flag (“alpha is shape”), specifying
                // whether the current soft mask and alpha constant are to be interpreted as
                // shape values (true) or opacity values (false).
                currentGraphicsState.AlphaSource = aisToken.Data;
            }

            if (state.TryGet(NameToken.Ca, pdfScanner, out NumericToken caToken))
            {
                // (Optional; PDF 1.4) The current stroking alpha constant, specifying the constant
                // shape or constant opacity value to be used for stroking operations in the
                // transparent imaging model (see “Source Shape and Opacity” on page 526 and
                // “Constant Shape and Opacity” on page 551).
                currentGraphicsState.AlphaConstantStroking = caToken.Data;
            }

            if (state.TryGet(NameToken.CaNs, pdfScanner, out NumericToken cansToken))
            {
                // (Optional; PDF 1.4) The current stroking alpha constant, specifying the constant
                // shape or constant opacity value to be used for NON-stroking operations in the
                // transparent imaging model (see “Source Shape and Opacity” on page 526 and
                // “Constant Shape and Opacity” on page 551).
                currentGraphicsState.AlphaConstantNonStroking = cansToken.Data;
            }

            if (state.TryGet(NameToken.Op, pdfScanner, out BooleanToken OPToken))
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

            if (state.TryGet(NameToken.OpNs, pdfScanner, out BooleanToken opToken))
            {
                // (Optional; PDF 1.3) A flag specifying whether to apply overprint (see Section
                // 4.5.6, “Overprint Control”) for painting operations other than stroking. If
                // this entry is absent, the OP entry, if any, sets this parameter.
                currentGraphicsState.NonStrokingOverprint = opToken.Data;
            }

            if (state.TryGet(NameToken.Opm, pdfScanner, out NumericToken opmToken))
            {
                // (Optional; PDF 1.3) The overprint mode (see Section 4.5.6, “Overprint Control”).
                currentGraphicsState.OverprintMode = opmToken.Data;
            }

            if (state.TryGet(NameToken.Sa, pdfScanner, out BooleanToken saToken))
            {
                // (Optional) A flag specifying whether to apply automatic stroke adjustment
                // (see Section 6.5.4, “Automatic Stroke Adjustment”).
                currentGraphicsState.StrokeAdjustment = saToken.Data;
            }
        }

        public void BeginInlineImage()
        {
            if (inlineImageBuilder != null)
            {
                parsingOptions.Logger.Error("Begin inline image (BI) command encountered while another inline image was active.");
            }

            inlineImageBuilder = new InlineImageBuilder();
        }

        public void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
            if (inlineImageBuilder == null)
            {
                parsingOptions.Logger.Error("Begin inline image data (ID) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Properties = properties;
        }

        public void EndInlineImage(IReadOnlyList<byte> bytes)
        {
            if (inlineImageBuilder == null)
            {
                parsingOptions.Logger.Error("End inline image (EI) command encountered without a corresponding begin inline image (BI) command.");
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

        public void PaintShading(NameToken shadingName)
        {
            // We do nothing for the moment
            // Do the following if you need to access the shading:
            // var shading = resourceStore.GetShading(shadingName);
        }
    }
}