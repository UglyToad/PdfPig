namespace UglyToad.PdfPig.Tests.Util
{
    using System.Collections.Generic;
    using PdfPig.Util;
    using Xunit;

    public class HexTests
    {
        public static IEnumerable<object[]> TestData => new[]
        {
            new object[] {new byte[0], string.Empty},
            new object[] {new byte[] {37}, "25"},
            new object[] {new byte[] {0}, "00"},
            new object[] {new byte[] {255}, "FF"},
            new object[]
            {
                new byte[]
                {
                    37, 80, 68,
                    70, 45, 49,
                    46, 54, 13,
                    37
                },
                "255044462D312E360D25"
            }
        };

        [Theory]
        [MemberData(nameof(TestData))]
        public void ConvertsToString(byte[] input, string expected)
        {
            var result = Hex.GetString(input);

            Assert.Equal(expected, result);
        }
    }
}
