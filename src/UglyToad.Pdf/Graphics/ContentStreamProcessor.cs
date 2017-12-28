namespace UglyToad.Pdf.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Content;
    using Fonts;
    using Geometry;
    using IO;
    using Operations;
    using Pdf.Core;

    internal class ContentStreamProcessor : IOperationContext
    {
        private readonly IResourceStore resourceStore;
        private readonly UserSpaceUnit userSpaceUnit;

        private Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();

        public TextMatrices TextMatrices { get; } = new TextMatrices();

        public int StackSize => graphicsStack.Count;
        
        public List<Letter> Letters = new List<Letter>();

        public ContentStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore, UserSpaceUnit userSpaceUnit)
        {
            this.resourceStore = resourceStore;
            this.userSpaceUnit = userSpaceUnit;
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
            var font = resourceStore.GetFont(GetCurrentState().FontState.FontName);

            var currentState = GetCurrentState();

            var fontSize = currentState.FontState.FontSize;
            var horizontalScaling = currentState.FontState.HorizontalScaling;
            var characterSpacing = currentState.FontState.CharacterSpacing;

            var transformationMatrix = currentState.CurrentTransformationMatrix;

            // TODO: this does not seem correct, produces the correct result for now but we need to revisit.
            // see: https://stackoverflow.com/questions/48010235/pdf-specification-get-font-size-in-points
            var pointSize = decimal.Round(fontSize * transformationMatrix.A, 2);
            
            while (bytes.MoveNext())
            {
                var code = font.ReadCharacterCode(bytes, out int codeLength);

                font.TryGetUnicode(code, out var unicode);

                var wordSpacing = 0m;
                if (code == ' ' && codeLength == 1)
                {
                    wordSpacing += GetCurrentState().FontState.WordSpacing;
                }
                
                var renderingMatrix = TextMatrices.GetRenderingMatrix(GetCurrentState());

                if (font.IsVertical)
                {
                    throw new NotImplementedException("Vertical fonts are currently unsupported, please submit a pull request or issue with an example file.");
                }

                var displacement = font.GetDisplacement(code);

                var width = (displacement.X * fontSize) * transformationMatrix.A;

                ShowGlyph(renderingMatrix, font, code, unicode, width, fontSize, pointSize);

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

        private void ShowGlyph(TransformationMatrix renderingMatrix, IFont font, int characterCode, string unicode, decimal width, decimal fontSize,
            decimal pointSize)
        {
            var location = new PdfPoint(renderingMatrix.E, renderingMatrix.F);
            
            var letter = new Letter(unicode, location, width, fontSize, font.Name.Name, pointSize);

            Letters.Add(letter);
        }
    }
}