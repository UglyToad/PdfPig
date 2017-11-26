namespace UglyToad.Pdf.Tests.Graphics
{
    using System.Collections.Generic;
    using Content;
    using Pdf.Graphics;

    internal class TestOperationContext : IOperationContext
    {
        public Stack<CurrentGraphicsState> StateStack { get; }
            = new Stack<CurrentGraphicsState>();

        public int StackSize => StateStack.Count;

        public TextMatrices TextMatrices { get; set; }
            = new TextMatrices();

        public TestOperationContext()
        {
            StateStack.Push(new CurrentGraphicsState());
        }

        public CurrentGraphicsState GetCurrentState()
        {
            return StateStack.Peek();
        }

        public void PopState()
        {
            StateStack.Pop();
        }

        public void PushState()
        {
            StateStack.Push(StateStack.Peek().DeepClone());
        }
    }

    internal class TestResourceStore : IResourceStore
    {
        
    }
}
