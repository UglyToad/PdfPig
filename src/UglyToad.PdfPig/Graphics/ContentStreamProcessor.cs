namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Content;
    using Fonts;
    using Geometry;
    using IO;
    using Operations;
    using PdfPig.Core;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using XObjects;

    internal class ContentStreamProcessor : IOperationContext
    {
        private readonly List<PdfPath> paths = new List<PdfPath>();
        private readonly IResourceStore resourceStore;
        private readonly UserSpaceUnit userSpaceUnit;
        private readonly bool isLenientParsing;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly XObjectFactory xObjectFactory;

        private Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();

        public TextMatrices TextMatrices { get; } = new TextMatrices();

        public PdfPath CurrentPath { get; private set; }
        public PdfPoint CurrentPosition { get; set; }

        public int StackSize => graphicsStack.Count;

        private readonly Dictionary<XObjectType, List<XObjectContentRecord>> xObjects = new Dictionary<XObjectType, List<XObjectContentRecord>>
        {
            {XObjectType.Form, new List<XObjectContentRecord>()},
            {XObjectType.Image, new List<XObjectContentRecord>()},
            {XObjectType.PostScript, new List<XObjectContentRecord>()}
        };

        public List<Letter> Letters = new List<Letter>();

        public ContentStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore, UserSpaceUnit userSpaceUnit, bool isLenientParsing, 
            IPdfTokenScanner pdfScanner,
            XObjectFactory xObjectFactory)
        {
            this.resourceStore = resourceStore;
            this.userSpaceUnit = userSpaceUnit;
            this.isLenientParsing = isLenientParsing;
            this.pdfScanner = pdfScanner;
            this.xObjectFactory = xObjectFactory;
            graphicsStack.Push(new CurrentGraphicsState());
        }

        public PageContent Process(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            var currentState = CloneAllStates();

            ProcessOperations(operations);
            
            return new PageContent(operations, Letters, xObjects, pdfScanner, xObjectFactory, isLenientParsing);
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
        }

        public void PushState()
        {
            graphicsStack.Push(graphicsStack.Peek().DeepClone());
        }

        public void ShowText(IInputBytes bytes)
        {
            var currentState = GetCurrentState();

            var font = resourceStore.GetFont(currentState.FontState.FontName);

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
            var pointSize = decimal.Round(fontSize * transformationMatrix.A, 2);
            
            while (bytes.MoveNext())
            {
                var code = font.ReadCharacterCode(bytes, out int codeLength);

                var foundUnicode = font.TryGetUnicode(code, out var unicode);

                if (!foundUnicode && !isLenientParsing)
                {
                    // TODO: record warning
                    // throw new InvalidOperationException($"We could not find the corresponding character with code {code} in font {font.Name}.");
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

                var transformedGlyphBounds = transformationMatrix
                    .Transform(TextMatrices.TextMatrix
                    .Transform(renderingMatrix
                    .Transform(boundingBox.GlyphBounds)));
                var transformedPdfBounds = transformationMatrix
                    .Transform(TextMatrices.TextMatrix
                        .Transform(renderingMatrix.Transform(new PdfRectangle(0, 0, boundingBox.Width, 0))));

                ShowGlyph(font, transformedGlyphBounds, transformedPdfBounds.BottomLeft, transformedPdfBounds.Width, unicode, fontSize, pointSize);

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

                var translate = TransformationMatrix.GetTranslationMatrix(tx, ty);

                TextMatrices.TextMatrix = translate.Multiply(TextMatrices.TextMatrix);
            }
        }

        public void ShowPositionedText(IReadOnlyList<IToken> tokens)
        {
            var currentState = GetCurrentState();

            var textState = currentState.FontState;

            var fontSize = textState.FontSize;
            var horizontalScaling = textState.HorizontalScaling/100m;
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
                        bytes = OtherEncodings.StringAsLatin1Bytes(((StringToken) token).Data);
                    }

                    ShowText(new ByteArrayInputBytes(bytes));
                }
            }
        }

        public void ApplyXObject(NameToken xObjectName)
        {
            var xObjectStream = resourceStore.GetXObject(xObjectName);

            // For now we will determine the type and store the object with the graphics state information preceding it.
            // Then consumers of the page can request the object/s to be retrieved by type.
            var subType = (NameToken)xObjectStream.StreamDictionary.Data[NameToken.Subtype.Data];

            var state = GetCurrentState();

            var matrix = state.CurrentTransformationMatrix;

            if (subType.Equals(NameToken.Ps))
            {
               xObjects[XObjectType.PostScript].Add(new XObjectContentRecord(XObjectType.PostScript, xObjectStream, matrix));
            }
            else if (subType.Equals(NameToken.Image))
            {
                xObjects[XObjectType.Image].Add(new XObjectContentRecord(XObjectType.Image, xObjectStream, matrix));
            }
            else if (subType.Equals(NameToken.Form))
            {
                xObjects[XObjectType.Form].Add(new XObjectContentRecord(XObjectType.Form, xObjectStream, matrix));
            }
            else
            {
                throw new InvalidOperationException($"XObject encountered with unexpected SubType {subType}. {xObjectStream.StreamDictionary}.");
            }
        }

        public void BeginSubpath()
        {
            CurrentPath = new PdfPath();
        }

        public void StrokePath(bool close)
        {
            if (close)
            {
                ClosePath();
            }
        }

        public void ClosePath()
        {
            CurrentPath.ClosePath();
            paths.Add(CurrentPath);
            CurrentPath = null;
        }

        private void AdjustTextMatrix(decimal tx, decimal ty)
        {
            var matrix = TransformationMatrix.GetTranslationMatrix(tx, ty);

            var newMatrix = matrix.Multiply(TextMatrices.TextMatrix);

            TextMatrices.TextMatrix = newMatrix;
        }

        private void ShowGlyph(IFont font, PdfRectangle glyphRectangle, PdfPoint position, decimal width, string unicode, decimal fontSize, decimal pointSize)
        {
            var letter = new Letter(unicode, glyphRectangle, position, width, fontSize, font.Name.Data, pointSize);

            Letters.Add(letter);
        }
    }
}