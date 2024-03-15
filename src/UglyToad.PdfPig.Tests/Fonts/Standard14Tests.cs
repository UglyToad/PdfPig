namespace UglyToad.PdfPig.Tests.Fonts
{
    using PdfPig.Fonts.Standard14Fonts;

    public class Standard14Tests
    {
        [Fact]
        public void CanCreateStandard14()
        {
            var names = Standard14.GetNames().Count;

            Assert.Equal(39, names);
        }
    }
}
