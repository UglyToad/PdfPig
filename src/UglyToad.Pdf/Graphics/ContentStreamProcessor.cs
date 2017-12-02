namespace UglyToad.Pdf.Graphics
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Geometry;
    using IO;
    using Operations;

    internal class ContentStreamProcessor : IOperationContext
    {
        private readonly IResourceStore resourceStore;
        private readonly Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();

        public TextMatrices TextMatrices { get; } = new TextMatrices();

        public int StackSize => graphicsStack.Count;


        public ContentStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore)
        {
            this.resourceStore = resourceStore;
        }

        public void Process(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            var currentState = CloneAllStates();


        }

        private void ProcessOperations(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            foreach (var stateOperation in operations)
            {
                // stateOperation.Run();
            }
        }

        private Stack<CurrentGraphicsState> CloneAllStates()
        {
            throw new NotImplementedException();
        }

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
            var renderingMatrix = TextMatrices.GetRenderingMatrix(GetCurrentState());

            var font = resourceStore.GetFont(GetCurrentState().FontState.FontName);
        }
    }
}