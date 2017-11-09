namespace UglyToad.Pdf.Tests.Text.Operators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Pdf.Text.Operators;
    using Xunit;

    public class StringTextComponentApproachTests
    {
        private readonly StringTextComponentApproach approach = new StringTextComponentApproach();
        
        [Theory]
        [InlineData("<03)")]
        [InlineData("<03AR>")]
        [InlineData("<9-3>")]
        public void InvalidHexThrows(string s)
        {
            Action action = () => approach.Read(new List<byte>(), s.Select(x => (byte)x), out var _);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Theory]
        [InlineData("<03>")]
        [InlineData("<03BA>")]
        [InlineData("<9a37eF>")]
        public void CanReadValidHex(string s)
        {
            var result = approach.Read(new List<byte>(), s.Select(x => (byte)x), out var _);

            Assert.NotNull(result);
            Assert.Equal(s.Select(x => (byte)x).ToArray(), result.AsOperand.RawBytes);

        }
    }
}
