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
                new double[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                },
                new double[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                },
                new double[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                }
            },
            new object[]
            {
                new double[]
                {
                    65, 9, 3,
                    5, 2, 7,
                    11, 1, 6
                },
                new double[]
                {
                    1, 2, 3,
                    4, 5, 6,
                    7, 8, 9
                },
                new double[]
                {
                    122, 199, 276,
                    62, 76, 90,
                    57, 75, 93
                }
            },
            new object[]
            {
                new double[]
                {
                    3, 5, 7,
                    11, 13, -3,
                    17, -6, -9
                },
                new double[]
                {
                    5, 4, 3,
                    3, 7, 12,
                    1, 0, 6
                },
                new double[]
                {
                    37, 47, 111,
                    91, 135, 171,
                    58, 26, -75
                }
            }
        };

        [Theory]
        [MemberData(nameof(MultiplicationData))]
        public void MultipliesCorrectly(double[] a, double[] b, double[] expected)
        {
            var matrixA = TransformationMatrix.FromArray(a);
            var matrixB = TransformationMatrix.FromArray(b);

            var expectedMatrix = TransformationMatrix.FromArray(expected);

            var result = matrixA.Multiply(matrixB);

            Assert.Equal(expectedMatrix, result);
        }

        public static IEnumerable<object[]> InversionData => new[]
        {
            new object[]
            {
                new double[]
                {
                    0, 2, 2,
                    1, 1, 1,
                    0, 1, 2
                },
                new double[]
                {
                    -.5, 1,  0,
                      1, 0, -1,
                    -.5, 0,  1
                }
            },
            new object[]
            {
                new double[]
                {
                    1, 1, 0,
                    0, 1, 0,
                    2, 1, 1
                },
                new double[]
                {
                     1, -1, 0,
                     0,  1, 0,
                    -2,  1, 1
                }
            },
            new object[]
            {
                new double[]
                {
                    2, 0, 0,
                    0, 2, 1,
                    2, 0, 2
                },
                new double[]
                {
                     .5,   0,   0,
                     .25, .5, -.25,
                    -.5,   0,  .5
                }
            },
            new object[]
            {
                new double[]
                {
                    -4.68,  2.47,  3.12,
                     5.00, -6.19, -0.58,
                     9.37, -7.11,  4.51
                },
                new double[]
                {
                    -0.212368047525923, -0.220866560683805,  0.118511242369019,
                    -0.18548392709254,  -0.333665068307247,  0.0854066769202931,
                     0.14880219150553,  -0.0671483286158035, 0.110153244324962
                }
            },
        };

        [Theory]
        [MemberData(nameof(InversionData))]
        public void InversesCorrectly(double[] a, double[] expected)
        {
            var matrixA = TransformationMatrix.FromArray(a);

            var expectedMatrix = TransformationMatrix.FromArray(expected);

            var result = matrixA.Inverse();

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    Assert.Equal(expectedMatrix[i, j], result[i, j], 6);
                }
            }
        }
    }
}
