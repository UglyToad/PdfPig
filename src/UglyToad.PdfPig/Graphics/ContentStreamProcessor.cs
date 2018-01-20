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
    using Tokenization.Tokens;
    using Util;

    internal class ContentStreamProcessor : IOperationContext
    {
        private readonly IResourceStore resourceStore;
        private readonly UserSpaceUnit userSpaceUnit;
        private readonly bool isLenientParsing;

        private Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();

        public TextMatrices TextMatrices { get; } = new TextMatrices();

        public int StackSize => graphicsStack.Count;
        
        public List<Letter> Letters = new List<Letter>();

        public ContentStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore, UserSpaceUnit userSpaceUnit, bool isLenientParsing)
        {
            this.resourceStore = resourceStore;
            this.userSpaceUnit = userSpaceUnit;
            this.isLenientParsing = isLenientParsing;
            graphicsStack.Push(new CurrentGraphicsState());
        }

        public PageContent Process(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            var currentState = CloneAllStates();

            ProcessOperations(operations);
            
            return new PageContent
            {
                GraphicsStateOperations = operations,
                Letters = Letters
            };
        }

        private void ProcessOperations(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            foreach (var stateOperation in operations)
            {
                stateOperation.Run(this, resourceStore);
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

            var transformationMatrix = currentState.CurrentTransformationMatrix;

            var fontMatrix = font.GetFontMatrix();

            // TODO: this does not seem correct, produces the correct result for now but we need to revisit.
            // see: https://stackoverflow.com/questions/48010235/pdf-specification-get-font-size-in-points
            var pointSize = decimal.Round(fontSize * transformationMatrix.A, 2);
            
            while (bytes.MoveNext())
            {
                var code = font.ReadCharacterCode(bytes, out int codeLength);

                var foundUnicode = font.TryGetUnicode(code, out var unicode);

                if (!foundUnicode && !isLenientParsing)
                {
                    throw new InvalidOperationException($"We could not find the corresponding character with code {code} in font {font.Name}.");
                }

                var wordSpacing = 0m;
                if (code == ' ' && codeLength == 1)
                {
                    wordSpacing += GetCurrentState().FontState.WordSpacing;
                }
                
                var renderingMatrix = TextMatrices.GetRenderingMatrix(GetCurrentState());

                if (font.IsVertical)
                {
                    throw new NotImplementedException("Vertical fonts are# currently unsupported, please submit a pull request or issue with an example file.");
                }

                var displacement = font.GetDisplacement(code);
                
                var width = displacement.X * fontSize * TextMatrices.TextMatrix.GetScalingFactorX() * transformationMatrix.A;

                ShowGlyph(renderingMatrix, font, unicode, width, fontSize, pointSize);

                decimal tx, ty;
                if (font.IsVertical)
                {
                    tx = 0;
                    ty = displacement.Y * fontSize + characterSpacing + wordSpacing;
                }
                else
                {
                    tx = (displacement.X * fontSize + characterSpacing + wordSpacing) * horizontalScaling;
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

        private void AdjustTextMatrix(decimal tx, decimal ty)
        {
            var matrix = TransformationMatrix.GetTranslationMatrix(tx, ty);

            var newMatrix = matrix.Multiply(TextMatrices.TextMatrix);

            TextMatrices.TextMatrix = newMatrix;
        }

        private void ShowGlyph(TransformationMatrix renderingMatrix, IFont font, string unicode, decimal width, decimal fontSize,
            decimal pointSize)
        {
            var location = new PdfPoint(renderingMatrix.E, renderingMatrix.F);
            
            var letter = new Letter(unicode, location, width, fontSize, font.Name.Data, pointSize);

            Letters.Add(letter);
        }
    }
}