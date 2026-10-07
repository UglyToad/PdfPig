namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Globalization;
    using System.Reflection;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.Export;

    public class SvgFontStyleTests
    {
        [Theory]
        [InlineData("ABCDEF+Example-BoLdItAlIc", "Example", "italic", "bold")]
        [InlineData("Example-LIGHTOBLIQUE", "Example", "oblique", "lighter")]
        [InlineData("Example-BOLDER", "Example", "normal", "bolder")]
        [InlineData("Example-LIGHTBOLDITALICOBLIQUE", "Example", "italic", "lighter")]
        [InlineData("Example-Regular", "Example", "normal", "normal")]
        [InlineData("Example-Bold-Italic", "Example", "italic", "bold")]
        public void RecognizesStylesRegardlessOfCurrentCulture(string name, string family, string style, string weight)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var method = typeof(SvgTextExporter).GetMethod("GetFontFamily", BindingFlags.NonPublic | BindingFlags.Static);
                object[] arguments = { name, null, null };

                Assert.Equal(family, method.Invoke(null, arguments));
                Assert.Equal(style, arguments[1]);
                Assert.Equal(weight, arguments[2]);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
