namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Colors;
    using Content;
    using Core;
    using Exceptions;
    using Filters;
    using Fonts;
    using Geometry;
    using IO;
    using Logging;
    using Operations;
    using Parser;
    using PdfPig.Core;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using XObjects;

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

        private readonly IResourceStore resourceStore;
        private readonly UserSpaceUnit userSpaceUnit;
        private readonly PageRotationDegrees rotation;
        private readonly bool isLenientParsing;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IPageContentParser pageContentParser;
        private readonly IFilterProvider filterProvider;
        private readonly ILog log;

        private Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();
        private IFont activeExtendedGraphicsStateFont;
        private InlineImageBuilder inlineImageBuilder;

        /// <summary>
        /// A counter to track individual calls to <see cref="ShowText"/> operations used to determine if letters are likely to be
        /// in the same word/group. This exposes internal grouping of letters used by the PDF creator which may correspond to the
        /// intended grouping of letters into words.
        /// </summary>
        private int textSequence;

        public TextMatrices TextMatrices { get; } = new TextMatrices();

        public TransformationMatrix CurrentTransformationMatrix => GetCurrentState().CurrentTransformationMatrix;

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
            bool isLenientParsing,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            IFilterProvider filterProvider,
            ILog log)
        {
            this.resourceStore = resourceStore;
            this.userSpaceUnit = userSpaceUnit;
            this.rotation = rotation;
            this.isLenientParsing = isLenientParsing;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pageContentParser = pageContentParser ?? throw new ArgumentNullException(nameof(pageContentParser));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.log = log;
            graphicsStack.Push(new CurrentGraphicsState());
            ColorSpaceContext = new ColorSpaceContext(GetCurrentState);
        }

        public PageContent Process(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            CloneAllStates();

            ProcessOperations(operations);

            return new PageContent(operations, letters, paths, images, pdfScanner, filterProvider, resourceStore, isLenientParsing);
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
            var horizontalScaling = currentState.FontState.HorizontalScaling / 100m;
            var characterSpacing = currentState.FontState.CharacterSpacing;
            var rise = currentState.FontState.Rise;

            var transformationMatrix = currentState.CurrentTransformationMatrix;

            var renderingMatrix =
                TransformationMatrix.FromValues(fontSize * horizontalScaling, 0, 0, fontSize, 0, rise);

            // TODO: this does not seem correct, produces the correct result for now but we need to revisit.
            // see: https://stackoverflow.com/questions/48010235/pdf-specification-get-font-size-in-points
            var pointSize = decimal.Round(rotation.Rotate(transformationMatrix).Multiply(TextMatrices.TextMatrix).Multiply(fontSize).A, 2);

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

                var wordSpacing = 0m;
                if (code == ' ' && codeLength == 1)
                {
                    wordSpacing += GetCurrentState().FontState.WordSpacing;
                }

                if (font.IsVertical)
                {
                    throw new NotImplementedException("Vertical fonts are currently unsupported, please submit a pull request or issue with an example file.");
                }

                var boundingBox = font.GetBoundingBox(code);

                var transformedGlyphBounds = rotation.Rotate(transformationMatrix)
                    .Transform(TextMatrices.TextMatrix
                    .Transform(renderingMatrix
                    .Transform(boundingBox.GlyphBounds)));

                var transformedPdfBounds = rotation.Rotate(transformationMatrix)
                    .Transform(TextMatrices.TextMatrix
                        .Transform(renderingMatrix.Transform(new PdfRectangle(0, 0, boundingBox.Width, 0))));

                // If the text rendering mode calls for filling, the current nonstroking color in the graphics state is used; 
                // if it calls for stroking, the current stroking color is used.
                // In modes that perform both filling and stroking, the effect is as if each glyph outline were filled and then stroked in separate operations.
                // TODO: expose color as something more advanced
                var color = currentState.FontState.TextRenderingMode != TextRenderingMode.Stroke
                    ? currentState.CurrentNonStrokingColor
                    : currentState.CurrentStrokingColor;

                ShowGlyph(font, transformedGlyphBounds,
                    transformedPdfBounds.BottomLeft,
                    transformedPdfBounds.BottomRight,
                    transformedPdfBounds.Width,
                    unicode,
                    fontSize,
                    color,
                    pointSize,
                    textSequence);

                decimal tx, ty;
                if (font.IsVertical)
                {
                    tx = 0;
                    ty = boundingBox.GlyphBounds.Height * fontSize + characterSpacing + wordSpacing;
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
            var horizontalScaling = textState.HorizontalScaling / 100m;
            var font = resourceStore.GetFont(textState.FontName);

            var isVertical = font.IsVertical;

            foreach (var token in tokens)
            {
                if (token is NumericToken number)
                {
                    var positionAdjustment = number.Data;

                    decimal tx, ty;
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
                xObjects[XObjectType.PostScript].Add(new XObjectContentRecord(XObjectType.PostScript, xObjectStream, matrix, state.RenderingIntent));
            }
            else if (subType.Equals(NameToken.Image))
            {
                images.Add(Union<XObjectContentRecord, InlineImage>.One(new XObjectContentRecord(XObjectType.Image, xObjectStream, matrix, state.RenderingIntent)));
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

            if (formStream.StreamDictionary.TryGet<DictionaryToken>(NameToken.Resources, pdfScanner, out var formResources))
            {
                resourceStore.LoadResourceDictionary(formResources, isLenientParsing);
            }

            // 1. Save current state.
            PushState();

            var startState = GetCurrentState();

            var formMatrix = TransformationMatrix.Identity;
            if (formStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Matrix, pdfScanner, out var formMatrixToken))
            {
                formMatrix = TransformationMatrix.FromArray(formMatrixToken.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
            }
            
            // 2. Update current transformation matrix.
            var resultingTransformationMatrix = startState.CurrentTransformationMatrix.Multiply(formMatrix);

            startState.CurrentTransformationMatrix = resultingTransformationMatrix;

            var contentStream = formStream.Decode(filterProvider);

            var operations = pageContentParser.Parse(new ByteArrayInputBytes(contentStream));

            // 3. We don't respect clipping currently.

            // 4. Paint the objects.
            ProcessOperations(operations);

            // 5. Restore saved state.
            PopState();
        }

        public void BeginSubpath()
        {
            if (CurrentPath != null && CurrentPath.Commands.Count > 0 && !paths.Contains(CurrentPath))
            {
                paths.Add(CurrentPath);
            }

            CurrentPath = new PdfPath();
        }

        public void StrokePath(bool close)
        {
            if (close)
            {
                ClosePath();
            }
            else
            {
                paths.Add(CurrentPath);
            }
        }

        public void FillPath(bool close)
        {
            if (close)
            {
                ClosePath();
            }
            else
            {
                paths.Add(CurrentPath);
            }
        }

        public void ClosePath()
        {
            CurrentPath.ClosePath();
            paths.Add(CurrentPath);
            CurrentPath = null;
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
                currentGraphicsState.FontState.FontSize = sizeToken.Data;
                activeExtendedGraphicsStateFont = resourceStore.GetFontDirectly(fontReference, isLenientParsing);
            }
        }

        public void BeginInlineImage()
        {
            if (inlineImageBuilder != null && !isLenientParsing)
            {
                throw new PdfDocumentFormatException("Begin inline image (BI) command encountered while another inline image was active.");
            }

            inlineImageBuilder = new InlineImageBuilder();
        }

        public void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
            if (inlineImageBuilder == null)
            {
                if (isLenientParsing)
                {
                    return;
                }

                throw new PdfDocumentFormatException("Begin inline image data (ID) command encountered without a corresponding begin inline image (BI) command.");
            }

            inlineImageBuilder.Properties = properties;
        }

        public void EndInlineImage(IReadOnlyList<byte> bytes)
        {
            if (inlineImageBuilder == null)
            {
                if (isLenientParsing)
                {
                    return;
                }

                throw new PdfDocumentFormatException("End inline image (EI) command encountered without a corresponding begin inline image (BI) command.");
            }

            inlineImageBuilder.Bytes = bytes;

            var image = inlineImageBuilder.CreateInlineImage(CurrentTransformationMatrix, filterProvider, pdfScanner, GetCurrentState().RenderingIntent, resourceStore);

            images.Add(Union<XObjectContentRecord, InlineImage>.Two(image));

            inlineImageBuilder = null;
        }

        private void AdjustTextMatrix(decimal tx, decimal ty)
        {
            var matrix = TransformationMatrix.GetTranslationMatrix(tx, ty);

            var newMatrix = matrix.Multiply(TextMatrices.TextMatrix);

            TextMatrices.TextMatrix = newMatrix;
        }

        private void ShowGlyph(IFont font, PdfRectangle glyphRectangle,
            PdfPoint startBaseLine,
            PdfPoint endBaseLine,
            decimal width,
            string unicode,
            decimal fontSize,
            IColor color,
            decimal pointSize,
            int textSequence)
        {
            var letter = new Letter(unicode, glyphRectangle,
                startBaseLine,
                endBaseLine,
                width,
                fontSize,
                font.Name.Data,
                color,
                pointSize,
                textSequence);

            letters.Add(letter);
        }
    }
}