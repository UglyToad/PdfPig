namespace UglyToad.PdfPig.Tests.Fonts
{
    using PdfPig.Fonts.Standard14Fonts;
    using Xunit;

    public class Standard14Tests
    {
        [Fact]
        public void CanCreateStandard14()
        {
            var names = Standard14.GetNames().Count;

            Assert.Equal(38, names);
        }
    }
}
