namespace UglyToad.PdfPig.Tests.Graphics
{
    using Content;
    using PdfPig.Core;
    using PdfPig.Geometry;
    using PdfPig.Graphics;

    public class ContentStreamProcessorTests
    {
        [Fact]
        public void InitialMatrixHandlesDefaultCase()
        {
            // Normally the cropbox = mediabox, with origin 0,0
            // Take A4 as a sample page size
            var mediaBox = new PdfRectangle(0, 0, 595, 842);
            var cropBox = new PdfRectangle(0, 0, 595, 842);

            // Sample glyph at the top-left corner, with size 10x20
            var glyph = new PdfRectangle(cropBox.Left, cropBox.Top - 20, cropBox.Left + 10, cropBox.Top);

            GetInitialTransformationMatrices(mediaBox, cropBox, new PageRotationDegrees(0), out var m, out var i);
            var transformedGlyph = m.Transform(glyph);
            var inverseTransformedGlyph = i.Transform(transformedGlyph);
            AssertAreEqual(glyph, transformedGlyph);
            AssertAreEqual(glyph, inverseTransformedGlyph);
        }

        [Fact]
        public void InitialMatrixHandlesCropBoxOutsideMediaBox()
        {
            // Normally the cropbox = mediabox, with origin 0,0
            // Take A4 as a sample page size
            var mediaBox = new PdfRectangle(0, 0, 595, 842);
            var cropBox = new PdfRectangle(400, 400, 1000, 1000);
            // The "view box" is then x=[400..595] y=[400..842], i.e. size 195x442

            // Sample points
            var pointInsideViewBox = new PdfPoint(500, 500);
            var pointBelowViewBox = new PdfPoint(500, 100);
            var pointLeftOfViewBox = new PdfPoint(200, 500);
            var pointAboveViewBox = new PdfPoint(500, 1000);
            var pointRightOfViewBox = new PdfPoint(1000, 500);

            GetInitialTransformationMatrices(mediaBox, cropBox, new PageRotationDegrees(0), out var m, out var i);
            var pt = m.Transform(pointInsideViewBox);
            var p0 = i.Transform(pt);
            AssertAreEqual(pointInsideViewBox, p0);
            Assert.True(pt.X > 0 && pt.X < 195 && pt.Y > 0 && pt.Y < 442);

            pt = m.Transform(pointBelowViewBox);
            p0 = i.Transform(pt);
            AssertAreEqual(pointBelowViewBox, p0);
            Assert.True(pt.X > 0 && pt.X < 195 && pt.Y < 0);

            pt = m.Transform(pointLeftOfViewBox);
            p0 = i.Transform(pt);
            AssertAreEqual(pointLeftOfViewBox, p0);
            Assert.True(pt.X < 0 && pt.Y > 0 && pt.Y < 442);

            // When we rotate by 180 degrees, points above/right view box 
            // should get a negative coordinate.
            GetInitialTransformationMatrices(mediaBox, cropBox, new PageRotationDegrees(180), out m, out i);
            pt = m.Transform(pointInsideViewBox);
            p0 = i.Transform(pt);
            AssertAreEqual(pointInsideViewBox, p0);
            Assert.True(pt.X > 0 && pt.X < 195 && pt.Y > 0 && pt.Y < 442);

            pt = m.Transform(pointAboveViewBox);
            p0 = i.Transform(pt);
            AssertAreEqual(pointAboveViewBox, p0);
            Assert.True(pt.X > 0 && pt.X < 195 && pt.Y < 0);

            pt = m.Transform(pointRightOfViewBox);
            p0 = i.Transform(pt);
            AssertAreEqual(pointRightOfViewBox, p0);
            Assert.True(pt.X < 0 && pt.Y > 0 && pt.Y < 442);
        }

        [Fact]
        public void InitialMatrixHandlesCropBoxAndRotation()
        {
            var mediaBox = new PdfRectangle(0, 0, 595, 842);

            // Cropbox with bottom left at (100,200) with size 300x400
            var cropBox = new PdfRectangle(100, 200, 400, 600);

            // Sample glyph at the top-left corner, with size 10x20
            var glyph = new PdfRectangle(cropBox.Left, cropBox.Top - 20, cropBox.Left + 10, cropBox.Top);

            // Test with 0 degrees (no rotation)
            GetInitialTransformationMatrices(mediaBox, cropBox, new PageRotationDegrees(0), out var initialMatrix, out var inverseMatrix);
            var transformedGlyph = initialMatrix.Transform(glyph);
            var inverseTransformedGlyph = inverseMatrix.Transform(transformedGlyph);
            AssertAreEqual(glyph, inverseTransformedGlyph);
            Assert.Equal(0, transformedGlyph.BottomLeft.X, 0);
            Assert.Equal(cropBox.Height - glyph.Height, transformedGlyph.BottomLeft.Y, 0);
            Assert.Equal(glyph.Width, transformedGlyph.TopRight.X, 0);
            Assert.Equal(cropBox.Height, transformedGlyph.TopRight.Y, 0);

            // Test with 90 degrees
            GetInitialTransformationMatrices(mediaBox, cropBox, new PageRotationDegrees(90), out initialMatrix, out inverseMatrix);
            transformedGlyph = initialMatrix.Transform(glyph);
            inverseTransformedGlyph = inverseMatrix.Transform(transformedGlyph);
            AssertAreEqual(glyph, inverseTransformedGlyph);
            Assert.Equal(cropBox.Height - glyph.Height, transformedGlyph.BottomLeft.X, 0);
            Assert.Equal(cropBox.Width, transformedGlyph.BottomLeft.Y, 0);
            Assert.Equal(cropBox.Height, transformedGlyph.TopRight.X, 0);
            Assert.Equal(cropBox.Width - glyph.Width, transformedGlyph.TopRight.Y, 0);

            // Test with 180 degrees
            GetInitialTransformationMatrices(mediaBox, cropBox, new PageRotationDegrees(180), out initialMatrix, out inverseMatrix);
            transformedGlyph = initialMatrix.Transform(glyph);
            inverseTransformedGlyph = inverseMatrix.Transform(transformedGlyph);
            AssertAreEqual(glyph, inverseTransformedGlyph);
            Assert.Equal(cropBox.Width, transformedGlyph.BottomLeft.X, 0);
            Assert.Equal(glyph.Height, transformedGlyph.BottomLeft.Y, 0);
            Assert.Equal(cropBox.Width - glyph.Width, transformedGlyph.TopRight.X, 0);
            Assert.Equal(0, transformedGlyph.TopRight.Y, 0);

            // Test with 270 degrees
            GetInitialTransformationMatrices(mediaBox, cropBox, new PageRotationDegrees(270), out initialMatrix, out inverseMatrix);
            transformedGlyph = initialMatrix.Transform(glyph);
            inverseTransformedGlyph = inverseMatrix.Transform(transformedGlyph);
            AssertAreEqual(glyph, inverseTransformedGlyph);
            Assert.Equal(glyph.Height, transformedGlyph.BottomLeft.X, 0);
            Assert.Equal(0, transformedGlyph.BottomLeft.Y, 0);
            Assert.Equal(0, transformedGlyph.TopRight.X, 0);
            Assert.Equal(glyph.Width, transformedGlyph.TopRight.Y, 0);
        }

        private static void GetInitialTransformationMatrices(
            MediaBox mediaBox,
            CropBox cropBox,
            PageRotationDegrees rotation,
            out TransformationMatrix initialMatrix,
            out TransformationMatrix inverseMatrix)
        {
            initialMatrix = OperationContextHelper.GetInitialMatrix(UserSpaceUnit.Default, mediaBox, cropBox, rotation, new TestingLog());
            inverseMatrix = initialMatrix.Inverse();
        }

        private static void GetInitialTransformationMatrices(
            PdfRectangle mediaBox,
            PdfRectangle cropBox,
            PageRotationDegrees rotation,
            out TransformationMatrix initialMatrix,
            out TransformationMatrix inverseMatrix)
        {
            GetInitialTransformationMatrices(new MediaBox(mediaBox), new CropBox(cropBox), rotation, out initialMatrix, out inverseMatrix);
        }

        private static void AssertAreEqual(PdfRectangle r1, PdfRectangle r2)
        {
            AssertAreEqual(r1.BottomLeft, r2.BottomLeft);
            AssertAreEqual(r1.TopRight, r2.TopRight);
        }

        private static void AssertAreEqual(PdfPoint p1, PdfPoint p2)
        {
            Assert.Equal(p1.X, p2.X, 0);
            Assert.Equal(p1.Y, p2.Y, 0);
        }
    }
}
