namespace UglyToad.PdfPig.Tests.Fonts.Encodings
{
    using System;
    using PdfPig.Fonts;
    using PdfPig.Fonts.Encodings;
    using CorePdfDocEncoding = PdfPig.Core.PdfDocEncoding;

    public class PdfDocEncodingTests
    {
        /// <summary>
        /// Codes defined by PDFDocEncoding as control characters. They have no glyph name.
        /// </summary>
        private static readonly int[] ControlCodes = { 9, 10, 13 };

        /// <summary>
        /// Every code in the glyph name table must map, via the Adobe Glyph List, to exactly the
        /// character the (independent) <see cref="CorePdfDocEncoding"/> byte to string table produces.
        /// </summary>
        [Fact]
        public void CodeToNameMatchesPdfDocEncodingCharacterTable()
        {
            var glyphList = GlyphList.AdobeGlyphList;

            for (var code = 0; code <= 255; code++)
            {
                var isDefined = CorePdfDocEncoding.TryConvertBytesToString(new[] { (byte)code }, out var expected);

                var name = PdfDocEncoding.Instance.GetName(code);

                if (!isDefined || Array.IndexOf(ControlCodes, code) >= 0)
                {
                    Assert.Equal(".notdef", name);
                    continue;
                }

                Assert.NotEqual(".notdef", name);
                Assert.Equal(expected, glyphList.NameToUnicode(name));
            }
        }

        [Fact]
        public void MatchesAsciiForPrintableRange()
        {
            // PDFDocEncoding matches ASCII (and therefore WinAnsiEncoding) for codes 32 - 126.
            for (var code = 32; code <= 126; code++)
            {
                Assert.Equal(WinAnsiEncoding.Instance.GetName(code), PdfDocEncoding.Instance.GetName(code));
            }
        }
    }
}
