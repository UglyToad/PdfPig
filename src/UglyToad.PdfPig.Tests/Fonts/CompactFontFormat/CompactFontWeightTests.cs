namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
    using System.Globalization;
    using UglyToad.PdfPig.Fonts.CompactFontFormat;
    using UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries;
    using UglyToad.PdfPig.PdfFonts.CidFonts;

    public class CompactFontWeightTests
    {
        [Theory]
        [InlineData("LIGHT", false, 300)]
        [InlineData("SeMiBoLd", true, 600)]
        [InlineData("BOLD", true, 700)]
        [InlineData("BLACK", true, 900)]
        [InlineData("Regular", false, 500)]
        [InlineData("", false, 500)]
        [InlineData(null, false, 500)]
        public void RecognizesWeightsRegardlessOfCurrentCulture(string? weight, bool bold, int expectedWeight)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var dictionary = new CompactFontFormatTopLevelDictionary { Weight = weight, ItalicAngle = -12 };
                var font = new CompactFontFormatFont(dictionary, null, null, null, null);
                var collection = new CompactFontFormatFontCollection(default,
                    new Dictionary<string, CompactFontFormatFont>() { ["Example"] = font });

                var details = new PdfCidCompactFontFormatFont(collection).Details;

                Assert.Equal(bold, details.IsBold);
                Assert.Equal(expectedWeight, details.Weight);
                Assert.True(details.IsItalic);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
