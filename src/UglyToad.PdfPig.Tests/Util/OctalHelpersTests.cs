namespace UglyToad.PdfPig.Tests.Util
{
    using PdfPig.Core;
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
