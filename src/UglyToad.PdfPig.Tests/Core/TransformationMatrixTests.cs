namespace UglyToad.PdfPig.Tests.Core
{
    using System.Collections.Generic;
    using PdfPig.Core;
    using Xunit;

    public class TransformationMatrixTests
    {
        public static IEnumerable<object[]> MultiplicationData => new[]
        {
            new object[]
            {
                new decimal[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                },
                new decimal[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                },
                new decimal[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                }
            },
            new object[]
            {
                new decimal[]
                {
                    65, 9, 3,
                    5, 2, 7,
                    11, 1, 6
                },
                new decimal[]
                {
                    1, 2, 3,
                    4, 5, 6,
                    7, 8, 9
                },
                new decimal[]
                {
                    122, 199, 276,
                    62, 76, 90,
                    57, 75, 93
                }
            },
            new object[]
            {
                new decimal[]
                {
                    3, 5, 7,
                    11, 13, -3,
                    17, -6, -9
                },
                new decimal[]
                {
                    5, 4, 3,
                    3, 7, 12,
                    1, 0, 6
                },
                new decimal[]
                {
                    37, 47, 111,
                    91, 135, 171,
                    58, 26, -75
                }
            }
        };

        [Theory]
        [MemberData(nameof(MultiplicationData))]
        public void MultipliesCorrectly(decimal[] a, decimal[] b, decimal[] expected)
        {
            var matrixA = TransformationMatrix.FromArray(a);
            var matrixB = TransformationMatrix.FromArray(b);

            var expectedMatrix = TransformationMatrix.FromArray(expected);

            var result = matrixA.Multiply(matrixB);

            Assert.Equal(expectedMatrix, result);
        }
    }
}
