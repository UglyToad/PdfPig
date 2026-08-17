namespace UglyToad.PdfPig.Tests.Writer
{
    using PdfPig.Core;
    using PdfPig.Graphics.Operations;
    using PdfPig.Graphics.Operations.SpecialGraphicsState;
    using PdfPig.Writer;

    public class PdfContentTransformationReaderTests
    {
        private static ModifyCurrentTransformationMatrix Cm(double a, double b, double c, double d, double e, double f)
        {
            return new ModifyCurrentTransformationMatrix([a, b, c, d, e, f]);
        }

        private static void AssertMatrix(TransformationMatrix expected, TransformationMatrix? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.A, actual.Value.A, 6);
            Assert.Equal(expected.B, actual.Value.B, 6);
            Assert.Equal(expected.C, actual.Value.C, 6);
            Assert.Equal(expected.D, actual.Value.D, 6);
            Assert.Equal(expected.E, actual.Value.E, 6);
            Assert.Equal(expected.F, actual.Value.F, 6);
        }

        [Fact]
        public void ReturnsNullWhenNoTransformIsApplied()
        {
            var operations = new IGraphicsStateOperation[]
            {
                Push.Value,
                Cm(2, 0, 0, 2, 0, 0),
                Pop.Value
            };

            Assert.Null(PdfContentTransformationReader.GetGlobalTransform(operations));
        }

        [Fact]
        public void ReturnsSingleTopLevelTransform()
        {
            var operations = new IGraphicsStateOperation[]
            {
                Cm(0.75, 0, 0, -0.75, 0, 841.89)
            };

            AssertMatrix(
                TransformationMatrix.FromValues(0.75, 0, 0, -0.75, 0, 841.89),
                PdfContentTransformationReader.GetGlobalTransform(operations));
        }

        [Fact]
        public void ConcatenatesMultipleTopLevelTransforms()
        {
            var first = TransformationMatrix.FromValues(0.75, 0, 0, -0.75, 0, 841.89);
            var second = TransformationMatrix.FromValues(2, 0, 0, 3, 10, 20);

            var operations = new IGraphicsStateOperation[]
            {
                Cm(first.A, first.B, first.C, first.D, first.E, first.F),
                Cm(second.A, second.B, second.C, second.D, second.E, second.F)
            };

            AssertMatrix(second.Multiply(first), PdfContentTransformationReader.GetGlobalTransform(operations));
        }

        [Fact]
        public void SelfCancellingTopLevelTransformsResultInIdentity()
        {
            // The pattern produced by writers that flip the co-ordinate system at the start of the
            // content stream and undo the flip at the end. See https://github.com/UglyToad/PdfPig/issues/1163.
            var operations = new IGraphicsStateOperation[]
            {
                Cm(0.75, 0, 0, -0.75, 0, 841.89),
                Push.Value,
                Cm(289, 0, 0, -453, 0, 453),
                Pop.Value,
                Cm(1 / 0.75d, 0, 0, -1 / 0.75d, 0, 841.89 / 0.75d)
            };

            AssertMatrix(TransformationMatrix.Identity, PdfContentTransformationReader.GetGlobalTransform(operations));
        }

        [Fact]
        public void IgnoresTransformsInsideSaveRestore()
        {
            var operations = new IGraphicsStateOperation[]
            {
                Cm(2, 0, 0, 2, 0, 0),
                Push.Value,
                Cm(5, 0, 0, 5, 100, 100),
                Push.Value,
                Cm(7, 0, 0, 7, 0, 0),
                Pop.Value,
                Pop.Value
            };

            AssertMatrix(
                TransformationMatrix.FromValues(2, 0, 0, 2, 0, 0),
                PdfContentTransformationReader.GetGlobalTransform(operations));
        }
    }
}
