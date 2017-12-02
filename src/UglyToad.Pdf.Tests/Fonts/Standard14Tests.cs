namespace UglyToad.Pdf.Tests.Fonts
{
    using Pdf.Fonts;
    using Xunit;

    public class Standard14Tests
    {
        [Fact]
        public void CanCreateStandard14()
        {
            var names = Standard14.GetNames().Count;

            Assert.Equal(34, names);
        }
    }
}
