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

        public PdfPoint? CloseSubpath()
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

        public void MoveTo(double x, double y)
        {
            BeginSubpath();
            var point = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            CurrentPosition = point;
            CurrentSubpath.MoveTo(point.X, point.Y);
        }

        public void BezierCurveTo(double x2, double y2, double x3, double y3)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(CurrentPosition.X, CurrentPosition.Y, controlPoint2.X, controlPoint2.Y, end.X, end.Y);
            CurrentPosition = end;
        }

        public void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var controlPoint1 = CurrentTransformationMatrix.Transform(new PdfPoint(x1, y1));
            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, end.X, end.Y);
            CurrentPosition = end;
        }

        public void LineTo(double x, double y)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var endPoint = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));

            CurrentSubpath.LineTo(endPoint.X, endPoint.Y);
            CurrentPosition = endPoint;
        }

        public void Rectangle(double x, double y, double width, double height)
        {
            BeginSubpath();
            var lowerLeft = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            var upperRight = CurrentTransformationMatrix.Transform(new PdfPoint(x + width, y + height));

            CurrentSubpath.Rectangle(lowerLeft.X, lowerLeft.Y, upperRight.X - lowerLeft.X, upperRight.Y - lowerLeft.Y);
            AddCurrentSubpath();
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
