namespace UglyToad.Pdf.Tests.Fonts.Encodings
{
    using System;
    using System.IO;
    using Pdf.Fonts.Encodings;
    using Xunit;

    public class GlyphListFactoryTests
    {
        [Fact]
        public void CanGetAdobeGlyphList()
        {
            var result = GlyphListFactory.Get("glyphlist");

            var h = result.NameToUnicode("H");

            Assert.Equal("H", h);
        }

        [Fact]
        public void MissingResourceNameThrows()
        {
            Action action = () => GlyphListFactory.Get("missing resource");

            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SkipsBlankLine()
        {
            var input = @"# comment

one;0031";
            using (var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(input)))
            {
                var result = GlyphListFactory.Read(stream);

                Assert.Equal("1", result.NameToUnicode("one"));
            }
        }

        [Fact]
        public void ReadNullThrows()
        {
            Action action = () => GlyphListFactory.Read(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void InvalidFormatThrows()
        {

            var input = @"one;0031
twelve;";
            using (var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(input)))
            {
                Action action = () => GlyphListFactory.Read(stream);

                Assert.Throws<InvalidOperationException>(action);
            }
        }
    }
}
