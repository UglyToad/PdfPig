namespace UglyToad.Pdf.Tests.Graphics
{
    using System.Collections.Generic;
    using Content;
    using ContentStream;
    using IO;
    using Pdf.ContentStream;
    using Pdf.Cos;
    using Pdf.Fonts;
    using Pdf.Graphics;
    using Pdf.Tokenization.Tokens;

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

        public void ShowText(IInputBytes bytes)
        {
        }

        public void ShowPositionedText(IReadOnlyList<IToken> tokens)
        {
        }
    }

    internal class TestResourceStore : IResourceStore
    {
        public void LoadResourceDictionary(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
        }

        public IFont GetFont(CosName name)
        {
            return null;
        }
    }
}
