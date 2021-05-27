namespace UglyToad.PdfPig.Tests.Util
{
    using Xunit;
    using PdfPig.Util;

    public class Matrix3x3Tests
    {
        [Fact]
        public void CanCreate()
        {
            var matrix = new Matrix3x3(
               1, 2, 3,
               4, 5, 6,
               7, 8, 9);

            Assert.Equal(new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, matrix);
        }

        [Fact]
        public void ExposesIdentityMatrix()
        {
            var matrix = new Matrix3x3(
                1, 0, 0,
                0, 1, 0,
                0, 0, 1
            );

            Assert.Equal(matrix, Matrix3x3.Identity);
        }

        [Fact]
        public void CanMultiplyWithFactor()
        {
            var matrix = new Matrix3x3(
                1, 2, 3,
                4, 5, 6,
                7, 8, 9);

            var result = matrix.Multiply(3);

            Assert.Equal(new double[] { 3, 6, 9, 12, 15, 18, 21, 24, 27 }, result);
        }

        [Fact]
        public void CanMultiplyWithVector()
        {
            var matrix = new Matrix3x3(
                1, 2, 3,
                4, 5, 6,
                7, 8, 9);

            var vector = (1, 2, 3);

            var product = matrix.Multiply(vector);

            Assert.Equal((14, 32, 50), product);
            // Result can be verified here:
            // https://www.wolframalpha.com/input/?i=%7B%7B1%2C2%2C+3%7D%2C%7B4%2C5%2C6%7D%2C%7B7%2C8%2C9%7D%7D.%7B1%2C2%2C3%7D
        }

        [Fact]
        public void CanMultiplyWithMatrix()
        {
            var matrix = new Matrix3x3(
               1, 2, 3,
               4, 5, 6,
               7, 8, 9);

            var product = matrix.Multiply(matrix);

            var expected = new Matrix3x3(
                30, 36, 42,
                66, 81, 96,
                102, 126, 150);

            Assert.Equal(expected, product);
            // Result can be verified here:
            // https://www.wolframalpha.com/input/?i=%7B%7B1%2C2%2C3%7D%2C%7B4%2C5%2C6%7D%2C%7B7%2C8%2C9%7D%7D.%7B%7B1%2C2%2C3%7D%2C%7B4%2C5%2C6%7D%2C%7B7%2C8%2C9%7D%7D

        }

        [Fact]
        public void CanTranspose()
        {
            var matrix = new Matrix3x3(
                1, 2, 3,
                4, 5, 6,
                7, 8, 9);

            var transposed = matrix.Transpose();

            Assert.Equal(new double[] { 1, 4, 7, 2, 5, 8, 3, 6, 9 }, transposed);
        }

        [Fact]
        public void InverseReturnsMatrixIfPossible()
        {
            var matrix = new Matrix3x3(
                1, 2, 3,
                0, 1, 4,
                5, 6, 0);

            var inverse = matrix.Inverse();

            Assert.Equal(new double[] { -24, 18, 5, 20, -15, -4, -5, 4, 1 }, inverse);
            Assert.Equal(Matrix3x3.Identity, matrix.Multiply(inverse));
        }

        [Fact]
        public void InverseReturnsNullIfNotPossible()
        {
            var matrix = new Matrix3x3(
                1, 2, 3,
                4, 5, 6,
                7, 8, 9);

            var inverse = matrix.Inverse();
            Assert.Null(inverse);
        }
    }
}
