namespace UglyToad.PdfPig.Tests.Graphics
{
    using System.Collections.Generic;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.IO;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.Core;

    internal class TestOperationContext : IOperationContext
    {
        public Stack<CurrentGraphicsState> StateStack { get; }
            = new Stack<CurrentGraphicsState>();

        public int StackSize => StateStack.Count;

        public TextMatrices TextMatrices { get; set; }
            = new TextMatrices();

        public TransformationMatrix CurrentTransformationMatrix
        {
            get { return GetCurrentState().CurrentTransformationMatrix; }
        }

        public PdfPath CurrentPath { get; set; }

        public IColorspaceContext ColorspaceContext { get; } = new ColorspaceContext();

        public PdfPoint CurrentPosition { get; set; }

        public TestOperationContext()
        {
            StateStack.Push(new CurrentGraphicsState());
            CurrentPath = new PdfPath(CurrentTransformationMatrix);
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

        public void ApplyXObject(NameToken xObjectName)
        {
        }

        public void BeginSubpath()
        {
        }

        public void StrokePath(bool close)
        {
        }
        public void FillPath(bool close)
        {
        }

        public void ClosePath()
        {
        }

        public void SetNamedGraphicsState(NameToken stateName)
        {
        }
    }

    public class TestColorspaceContext : IColorspaceContext
    {
        public void SetStrokingColorspace(NameToken colorspace)
        {
        }

        public void SetNonStrokingColorspace(NameToken colorspace)
        {
        }

        public void SetStrokingColorGray(decimal gray)
        {
        }

        public void SetStrokingColorRgb(decimal r, decimal g, decimal b)
        {
        }

        public void SetStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
        }

        public void SetNonStrokingColorGray(decimal gray)
        {
        }

        public void SetNonStrokingColorRgb(decimal r, decimal g, decimal b)
        {
        }

        public void SetNonStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
        }
    }
}
