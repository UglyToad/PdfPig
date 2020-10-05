namespace UglyToad.PdfPig
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Graphics;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.Graphics.Operations;
    using UglyToad.PdfPig.Logging;
    using UglyToad.PdfPig.Parser;
    using UglyToad.PdfPig.PdfFonts;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.XObjects;
    using static UglyToad.PdfPig.Core.PdfSubpath;

    /// <inheritdoc/>
    public abstract class BaseDrawingProcessor : IOperationContext, IDrawingProcessor
    {
        #region IOperationContext
        private IResourceStore resourceStore;
        private PageRotationDegrees rotation;
        private IPdfTokenScanner pdfScanner;
        private IPageContentParser pageContentParser;
        private  IFilterProvider filterProvider;
        private ILog log;
        private  PdfVector pageSize;

        private Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();
        private IFont activeExtendedGraphicsStateFont;
        private InlineImageBuilder inlineImageBuilder;
        private int pageNumber;

        /// <inheritdoc/>
        public BaseDrawingProcessor(ILog log)
        {
            this.log = log;
        }

        /// <inheritdoc/>
        public TextMatrices TextMatrices { get; } = new TextMatrices();

        /// <inheritdoc/>
        public TransformationMatrix CurrentTransformationMatrix => GetCurrentState().CurrentTransformationMatrix;

        /// <inheritdoc/>
        public PdfSubpath CurrentSubpath { get; private set; }

        /// <inheritdoc/>
        public PdfPath CurrentPath { get; private set; }

        /// <inheritdoc/>
        public IColorSpaceContext ColorSpaceContext { get; protected set; }

        /// <inheritdoc/>
        public PdfPoint CurrentPosition { get; set; }

        /// <inheritdoc/>
        public int StackSize => graphicsStack.Count;

        private readonly Dictionary<XObjectType, List<XObjectContentRecord>> xObjects = new Dictionary<XObjectType, List<XObjectContentRecord>>
        {
            {XObjectType.Image, new List<XObjectContentRecord>()},
            {XObjectType.PostScript, new List<XObjectContentRecord>()}
        };

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

        private void ProcessOperations(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            foreach (var stateOperation in operations)
            {
                stateOperation.Run(this);
            }
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        public CurrentGraphicsState GetCurrentState()
        {
            return graphicsStack.Peek();
        }

        /// <inheritdoc/>
        public void PopState()
        {
            graphicsStack.Pop();
            activeExtendedGraphicsStateFont = null;
            UpdateClipPath();
        }

        /// <inheritdoc/>
        public void PushState()
        {
            graphicsStack.Push(graphicsStack.Peek().DeepClone());
        }

        /// <inheritdoc/>
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
                    log?.Warn($"We could not find the corresponding character with code {code} in font {font.Name}.");
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

                // If the text rendering mode calls for filling, the current nonstroking color in the graphics state is used; 
                // if it calls for stroking, the current stroking color is used.
                // In modes that perform both filling and stroking, the effect is as if each glyph outline were filled and then stroked in separate operations.
                // TODO: expose color as something more advanced
                var color = currentState.FontState.TextRenderingMode != TextRenderingMode.Stroke
                    ? currentState.CurrentNonStrokingColor
                    : currentState.CurrentStrokingColor;

                if (font.TryGetPath(code, out var path))
                {
                    DrawLetter(path, color, renderingMatrix, textMatrix, transformationMatrix);
                }
                else
                {
                    var transformedGlyphBounds = PerformantRectangleTransformer
                        .Transform(renderingMatrix, textMatrix, transformationMatrix, boundingBox.GlyphBounds);

                    var transformedPdfBounds = PerformantRectangleTransformer
                        .Transform(renderingMatrix, textMatrix, transformationMatrix, new PdfRectangle(0, 0, boundingBox.Width, 0));

                    DrawLetter(unicode, transformedGlyphBounds,
                        transformedPdfBounds.BottomLeft,
                        transformedPdfBounds.BottomRight,
                        transformedPdfBounds.Width,
                        fontSize,
                        font.Details,
                        color,
                        pointSize);
                }

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

        /// <inheritdoc/>
        public void ShowPositionedText(IReadOnlyList<IToken> tokens)
        {
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

        /// <inheritdoc/>
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

                var image = Union<XObjectContentRecord, InlineImage>.One(contentRecord);
                if (image.TryGetFirst(out var xObjectContentRecord)) // always an inline image???
                {
                    DrawImage(XObjectFactory.ReadImage(xObjectContentRecord, pdfScanner, filterProvider, resourceStore));
                }
                else if (image.TryGetSecond(out var inlineImage))
                {
                    DrawImage(inlineImage);
                }
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void BeginSubpath()
        {
            if (CurrentPath == null)
            {
                CurrentPath = new PdfPath();
            }

            AddCurrentSubpath();
            CurrentSubpath = new PdfSubpath();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void AddCurrentSubpath()
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            CurrentPath.Add(CurrentSubpath);
            CurrentSubpath = null;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void EndPath()
        {
            if (CurrentPath == null)
            {
                return;
            }

            AddCurrentSubpath();

            /*
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
            */

            CurrentPath = null;
        }

        /// <inheritdoc/>
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

            DrawPath(CurrentPath);

            /*
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
            */

            CurrentPath = null;
        }

        /// <inheritdoc/>
        public void ModifyClippingIntersect(FillingRule clippingRule)
        {
            if (CurrentPath == null)
            {
                return;
            }

            AddCurrentSubpath();
            CurrentPath.SetClipping(clippingRule);

            //var currentClipping = GetCurrentState().CurrentClippingPath;
            //currentClipping.SetClipping(clippingRule);

            var newClippings = CurrentPath.Clip(GetCurrentState().CurrentClippingPath, log);
            if (newClippings == null)
            {
                log?.Warn("Empty clipping path found. Clipping path not updated.");
            }
            else
            {
                GetCurrentState().CurrentClippingPath = newClippings;
                UpdateClipPath();
            }

            /*
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
            */
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void BeginInlineImage()
        {
            if (inlineImageBuilder != null)
            {
                log?.Error("Begin inline image (BI) command encountered while another inline image was active.");
            }

            inlineImageBuilder = new InlineImageBuilder();
        }

        /// <inheritdoc/>
        public void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
            if (inlineImageBuilder == null)
            {
                log?.Error("Begin inline image data (ID) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Properties = properties;
        }

        /// <inheritdoc/>
        public void EndInlineImage(IReadOnlyList<byte> bytes)
        {
            if (inlineImageBuilder == null)
            {
                log?.Error("End inline image (EI) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Bytes = bytes;

            var image = inlineImageBuilder.CreateInlineImage(CurrentTransformationMatrix, filterProvider, pdfScanner, GetCurrentState().RenderingIntent, resourceStore);

            DrawImage(image);

            //images.Add(Union<XObjectContentRecord, InlineImage>.Two(image));

            //markedContentStack.AddImage(image);

            inlineImageBuilder = null;
        }

        /// <inheritdoc/>
        public void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties)
        {
            // do nothing
        }

        /// <inheritdoc/>
        public void EndMarkedContent()
        {
            // do nothing
        }

        /// <inheritdoc/>
        private void AdjustTextMatrix(double tx, double ty)
        {
            var matrix = TransformationMatrix.GetTranslationMatrix(tx, ty);

            var newMatrix = matrix.Multiply(TextMatrices.TextMatrix);

            TextMatrices.TextMatrix = newMatrix;
        }
        #endregion

        #region IDrawingSystem
        /// <summary>
        /// init
        /// </summary>
        /// <param name="page"></param>
        public void Init(Page page)
        {
            this.resourceStore = page.Content.resourceStore;

            // reload resources, unload after????
            //if (page.Dictionary.TryGet(NameToken.Resources, pdfScanner, out DictionaryToken resources))
            //{
            //    resourceStore.LoadResourceDictionary(resources);
            //}

            this.rotation = page.Rotation;
            this.pdfScanner = page.Content.pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pageContentParser = page.Content.pageContentParser ?? throw new ArgumentNullException(nameof(pageContentParser));
            this.filterProvider = page.Content.filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.pageSize = new PdfVector(page.MediaBox.Bounds.Width, page.MediaBox.Bounds.Height);
            this.pageNumber = page.Number;

            // initiate CurrentClippingPath to cropBox
            var clippingSubpath = new PdfSubpath();
            clippingSubpath.Rectangle(page.CropBox.Bounds.BottomLeft.X, page.CropBox.Bounds.BottomLeft.Y, page.CropBox.Bounds.Width, page.CropBox.Bounds.Height);
            var clippingPath = new PdfPath() { clippingSubpath };
            clippingPath.SetClipping(FillingRule.EvenOdd);

            graphicsStack.Push(new CurrentGraphicsState()
            {
                CurrentTransformationMatrix = GetInitialMatrix(),
                CurrentClippingPath = clippingPath
            });

            ColorSpaceContext = new ColorSpaceContext(GetCurrentState, resourceStore);
        }

        /// <inheritdoc/>
        public static (double x, double y) TransformPoint(TransformationMatrix first, TransformationMatrix second, TransformationMatrix third, PdfPoint tl)
        {
            var topLeftX = tl.X;
            var topLeftY = tl.Y;

            // First
            var x = first.A * topLeftX + first.C * topLeftY + first.E;
            var y = first.B * topLeftX + first.D * topLeftY + first.F;
            topLeftX = x;
            topLeftY = y;

            // Second
            x = second.A * topLeftX + second.C * topLeftY + second.E;
            y = second.B * topLeftX + second.D * topLeftY + second.F;
            topLeftX = x;
            topLeftY = y;

            // Third
            x = third.A * topLeftX + third.C * topLeftY + third.E;
            y = third.B * topLeftX + third.D * topLeftY + third.F;
            topLeftX = x;
            topLeftY = y;

            return (topLeftX, topLeftY);
        }






        /// <inheritdoc/>
        public abstract MemoryStream DrawPage(Page page, double scale);

        /// <inheritdoc/>
        public abstract void DrawLetter(string value, PdfRectangle glyphRectangle, PdfPoint startBaseLine, PdfPoint endBaseLine, double width, double fontSize, FontDetails font, IColor color, double pointSize);

        /// <inheritdoc/>
        public abstract void DrawLetter(List<PdfSubpath> pdfSubpaths, IColor color, TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix);

        /// <inheritdoc/>
        public abstract void DrawImage(IPdfImage image);

        /// <inheritdoc/>
        public abstract void DrawPath(PdfPath path);

        /// <inheritdoc/>
        public abstract void UpdateClipPath();
        #endregion
    }
}
