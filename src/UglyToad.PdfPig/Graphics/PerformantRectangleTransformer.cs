using UglyToad.PdfPig.Core;

namespace UglyToad.PdfPig.Graphics
{
    internal static class PerformantRectangleTransformer
    {
        public static PdfRectangle Transform(TransformationMatrix first, TransformationMatrix second, TransformationMatrix third, PdfRectangle rectangle)
        {
            var mutable = new MutableRectangle(rectangle);

            mutable.Transform(first);
            mutable.Transform(second);
            mutable.Transform(third);

            return mutable.ToRectangle();
        }

        private struct MutableRectangle
        {
            private double topLeftX;
            private double topLeftY;

            private double topRightX;
            private double topRightY;

            private double bottomLeftX;
            private double bottomLeftY;

            private double bottomRightX;
            private double bottomRightY;

            public MutableRectangle(PdfRectangle rectangle)
            {
                topLeftX = rectangle.TopLeft.X;
                topLeftY = rectangle.TopLeft.Y;

                topRightX = rectangle.TopRight.X;
                topRightY = rectangle.TopRight.Y;

                bottomLeftX = rectangle.BottomLeft.X;
                bottomLeftY = rectangle.BottomLeft.Y;

                bottomRightX = rectangle.BottomRight.X;
                bottomRightY = rectangle.BottomRight.Y;
            }

            public void Transform(TransformationMatrix matrix)
            {
                /*
                 * TransformationMatrix.Transform(PdfPoint original)
                 * var x = A * original.X + C * original.Y + E;
                 * var y = B * original.X + D * original.Y + F;
                 * return new PdfPoint(x, y);
                 *
                 * For a rectangle runs on TopLeft, TopRight, BottomLeft and BottomRight
                 * and returns a new rectangle.
                 */

                var x = matrix.A * topLeftX + matrix.C * topLeftY + matrix.E;
                var y = matrix.B * topLeftX + matrix.D * topLeftY + matrix.F;
                topLeftX = x;
                topLeftY = y;

                x = matrix.A * topRightX + matrix.C * topRightY + matrix.E;
                y = matrix.B * topRightX + matrix.D * topRightY + matrix.F;
                topRightX = x;
                topRightY = y;

                x = matrix.A * bottomLeftX + matrix.C * bottomLeftY + matrix.E;
                y = matrix.B * bottomLeftX + matrix.D * bottomLeftY + matrix.F;
                bottomLeftX = x;
                bottomLeftY = y;

                x = matrix.A * bottomRightX + matrix.C * bottomRightY + matrix.E;
                y = matrix.B * bottomRightX + matrix.D * bottomRightY + matrix.F;
                bottomRightX = x;
                bottomRightY = y;
            }

            public PdfRectangle ToRectangle()
            {
                return new PdfRectangle(new PdfPoint(topLeftX, topLeftY), new PdfPoint(topRightX, topRightY),
                    new PdfPoint(bottomLeftX, bottomLeftY), new PdfPoint(bottomRightX, bottomRightY));
            }
        }
    }
}
