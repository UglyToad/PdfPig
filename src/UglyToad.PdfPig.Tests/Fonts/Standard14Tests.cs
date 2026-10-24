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

        [Theory]
        [InlineData("Helvetica", Standard14Font.Helvetica)]
        [InlineData("Arial,Bold", Standard14Font.HelveticaBold)]
        [InlineData("Symbol,Italic", Standard14Font.Symbol)]
        [InlineData("ABCDEF+Helvetica", Standard14Font.Helvetica)]
        [InlineData("ABCDEF+Arial,Bold", Standard14Font.HelveticaBold)]
        [InlineData("helvetica-bold", Standard14Font.HelveticaBold)]
        [InlineData("ZAPFDINGBATS", Standard14Font.ZapfDingbats)]
        [InlineData("Helvetica,Bold", Standard14Font.HelveticaBold)]
        [InlineData("Arial-Italic", Standard14Font.HelveticaOblique)]
        [InlineData("Times New Roman,Bold", Standard14Font.TimesBold)]
        [InlineData("TimesRoman", Standard14Font.TimesRoman)]
        [InlineData("Courier New", Standard14Font.Courier)]
        public void TryGetStandard14FontMatchesNamesLoosely(string name, Standard14Font expected)
        {
            Assert.True(Standard14.TryGetStandard14Font(name, out var font));
            Assert.Equal(expected, font);
        }

        [Theory]
        [InlineData("")]
        [InlineData("+")]
        [InlineData("ABCDEF+")]
        [InlineData("NotAFont")]
        [InlineData("ABCDEF+NotAFont")]
        [InlineData("Helvetica-Narrow")]
        [InlineData("TimesNewRomanPSMT")]
        public void TryGetStandard14FontRejectsOtherNames(string name)
        {
            Assert.False(Standard14.TryGetStandard14Font(name, out _));
        }

        [Theory]
        [InlineData("ABCDEF+Helvetica")]
        [InlineData("helvetica")]
        [InlineData("Arial-Italic")]
        public void ExactLookupsAreUnchanged(string name)
        {
            // Only TryGetStandard14Font matches loosely.
            Assert.Null(Standard14.GetAdobeFontMetrics(name));
            Assert.False(Standard14.IsFontInStandard14(name));
            Assert.Null(Standard14.GetMappedFontName(name));
        }
    }
}
