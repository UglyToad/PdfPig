namespace UglyToad.PdfPig.Graphics
{
    using PdfPig.Core;

    internal static class PerformantRectangleTransformer
    {
        public static PdfRectangle Transform(TransformationMatrix first, TransformationMatrix second, TransformationMatrix third, PdfRectangle rectangle)
        {
            var tl = rectangle.TopLeft;
            var tr = rectangle.TopRight;
            var bl = rectangle.BottomLeft;
            var br = rectangle.BottomRight;

            var topLeftX = tl.X;
            var topLeftY = tl.Y;

            var topRightX = tr.X;
            var topRightY = tr.Y;

            var bottomLeftX = bl.X;
            var bottomLeftY = bl.Y;

            var bottomRightX = br.X;
            var bottomRightY = br.Y;

            // First
            var x = first.A * topLeftX + first.C * topLeftY + first.E;
            var y = first.B * topLeftX + first.D * topLeftY + first.F;
            topLeftX = x;
            topLeftY = y;

            x = first.A * topRightX + first.C * topRightY + first.E;
            y = first.B * topRightX + first.D * topRightY + first.F;
            topRightX = x;
            topRightY = y;

            x = first.A * bottomLeftX + first.C * bottomLeftY + first.E;
            y = first.B * bottomLeftX + first.D * bottomLeftY + first.F;
            bottomLeftX = x;
            bottomLeftY = y;

            x = first.A * bottomRightX + first.C * bottomRightY + first.E;
            y = first.B * bottomRightX + first.D * bottomRightY + first.F;
            bottomRightX = x;
            bottomRightY = y;

            // Second
            x = second.A * topLeftX + second.C * topLeftY + second.E;
            y = second.B * topLeftX + second.D * topLeftY + second.F;
            topLeftX = x;
            topLeftY = y;

            x = second.A * topRightX + second.C * topRightY + second.E;
            y = second.B * topRightX + second.D * topRightY + second.F;
            topRightX = x;
            topRightY = y;

            x = second.A * bottomLeftX + second.C * bottomLeftY + second.E;
            y = second.B * bottomLeftX + second.D * bottomLeftY + second.F;
            bottomLeftX = x;
            bottomLeftY = y;

            x = second.A * bottomRightX + second.C * bottomRightY + second.E;
            y = second.B * bottomRightX + second.D * bottomRightY + second.F;
            bottomRightX = x;
            bottomRightY = y;

            // Third
            x = third.A * topLeftX + third.C * topLeftY + third.E;
            y = third.B * topLeftX + third.D * topLeftY + third.F;
            topLeftX = x;
            topLeftY = y;

            x = third.A * topRightX + third.C * topRightY + third.E;
            y = third.B * topRightX + third.D * topRightY + third.F;
            topRightX = x;
            topRightY = y;

            x = third.A * bottomLeftX + third.C * bottomLeftY + third.E;
            y = third.B * bottomLeftX + third.D * bottomLeftY + third.F;
            bottomLeftX = x;
            bottomLeftY = y;

            x = third.A * bottomRightX + third.C * bottomRightY + third.E;
            y = third.B * bottomRightX + third.D * bottomRightY + third.F;
            bottomRightX = x;
            bottomRightY = y;

            return new PdfRectangle(new PdfPoint(topLeftX, topLeftY), new PdfPoint(topRightX, topRightY),
                new PdfPoint(bottomLeftX, bottomLeftY), new PdfPoint(bottomRightX, bottomRightY));
        }
    }
}
