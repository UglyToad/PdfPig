namespace UglyToad.Pdf.Tests.Text.Operators
{
    using System.Collections.Generic;
    using System.Linq;
    using Pdf.Text;
    using Pdf.Text.Operators;
    using Xunit;

    public class NumericTextComponentApproachTests
    {
        private readonly NumericTextComponentApproach approach = new NumericTextComponentApproach();

        public static IEnumerable<object[]> TestData = new []
        {
            new object[] { "123" },
            new object[] { "43445" },
            new object[] { "+17" },
            new object[] { "-98" },
            new object[] { "0" },
            new object[] { "34.5" },
            new object[] { "-3.62" },
            new object[] { "+123.6" },
            new object[] { "4." },
            new object[] { "-.002" },
            new object[] { "0.0" },
        };
        
        [Theory]
        [MemberData(nameof(TestData))]
        public void CanReadNumbers(string number)
        {
            var bytes = number.Select(x => (byte) x).ToArray();

            var canRead = approach.CanRead(bytes[0], 0);
            
            Assert.True(canRead);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ReadsNumbers(string number)
        {
            var bytes = number.Select(x => (byte)x);

            var result = approach.Read(new byte[0], bytes, out var offset);

            Assert.NotNull(result);

            Assert.Equal(TextObjectComponentType.Numeric, result.Type);
        }
    }
}
