namespace UglyToad.PdfPig.Tests.Graphics
{
    using System.Collections.Generic;
    using Content;
    using PdfFonts;
    using PdfPig.Graphics;
    using PdfPig.Tokens;
    using PdfPig.Core;
    using Tokens;

    internal class TestOperationContext : IOperationContext
    {
        public Stack<CurrentGraphicsState> StateStack { get; }
            = new Stack<CurrentGraphicsState>();

        public int StackSize => StateStack.Count;

        public TextMatrices TextMatrices { get; set; }
            = new TextMatrices();

        public TransformationMatrix CurrentTransformationMatrix => GetCurrentState().CurrentTransformationMatrix;

        public PdfSubpath CurrentSubpath { get; set; }

        public PdfPath CurrentPath { get; set; }

        public IColorSpaceContext ColorSpaceContext { get; }

        public PdfPoint CurrentPosition { get; set; }

        public TestOperationContext()
        {
            StateStack.Push(new CurrentGraphicsState());
            CurrentSubpath = new PdfSubpath();
            ColorSpaceContext = new ColorSpaceContext(GetCurrentState, new ResourceStore(new TestPdfTokenScanner(), new TestFontFactory()));
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

        public PdfPoint CloseSubpath()
        {
            return new PdfPoint();
        }

        public void AddCurrentSubpath()
        {

        }

        public void StrokePath(bool close)
        {
        }

        public void FillPath(FillingRule fillingRule, bool close)
        {
        }
        public void FillStrokePath(FillingRule fillingRule, bool close)
        {

        }

        public void EndPath()
        {
        }

        public void ClosePath()
        {
        }

        public void SetNamedGraphicsState(NameToken stateName)
        {
        }

        public void BeginInlineImage()
        {
        }

        public void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
        }

        public void EndInlineImage(IReadOnlyList<byte> bytes)
        {
        }

        public void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties)
        {
        }

        public void EndMarkedContent()
        {
        }

        public void ModifyClippingIntersect(FillingRule clippingRule)
        {
        }

        private class TestFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary)
            {
                return null;
            }
        }
    }
}
