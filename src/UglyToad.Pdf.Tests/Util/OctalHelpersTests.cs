namespace UglyToad.Pdf.Tests.Util
{
    using Pdf.Util;
    using Xunit;

    public class OctalHelpersTests
    {
        [Fact]
        public void CorrectlyConverts()
        {
            var result = OctalHelpers.FromOctalInt(110);

            Assert.Equal(72, result);
        }
    }
}
