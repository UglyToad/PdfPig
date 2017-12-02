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

        private Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();

        public TextMatrices TextMatrices { get; } = new TextMatrices();

        public int StackSize => graphicsStack.Count;
        
        public List<string> Texts = new List<string>();

        public ContentStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore)
        {
            this.resourceStore = resourceStore;
            graphicsStack.Push(new CurrentGraphicsState());
        }

        public PageContent Process(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            var currentState = CloneAllStates();

            ProcessOperations(operations);
            
            return new PageContent
            {
                GraphicsStateOperations = operations,
                Text = Texts
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

            var fontSize = GetCurrentState().FontState.FontSize;
            var horizontalScaling = GetCurrentState().FontState.HorizontalScaling;
            var characterSpacing = GetCurrentState().FontState.CharacterSpacing;

            while (bytes.MoveNext())
            {
                var code = font.ReadCharacterCode(bytes, out int codeLength);

                var unicode = font.GetUnicode(code);

                var wordSpacing = 0m;
                if (code == ' ' && codeLength == 1)
                {
                    wordSpacing += GetCurrentState().FontState.WordSpacing;
                }
                
                var renderingMatrix = TextMatrices.GetRenderingMatrix(GetCurrentState());

                if (font.IsVertical)
                {
                    throw new NotImplementedException("Vertical fonts are currently unsupported, please submit a pull request.");
                }

                var displacement = font.GetDisplacement(code);

                ShowGlyph(renderingMatrix, font, code, unicode, displacement);

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

        private void ShowGlyph(TransformationMatrix renderingMatrix, IFont font, 
            int characterCode, string unicode, PdfVector displacement)
        {
            Texts.Add(unicode);
        }
    }
}