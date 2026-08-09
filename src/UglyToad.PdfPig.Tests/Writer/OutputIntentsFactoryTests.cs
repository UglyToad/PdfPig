namespace UglyToad.PdfPig.Tests.Writer
{
    using System.Collections.Generic;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Tokens;
    using PdfPig.Writer.Colors;
    using Xunit;

    /// <summary>
    /// 8.6.5.5 requires the <c>/N</c> of a profile stream to match the profile it carries. On the writing
    /// side PdfPig owns that profile, so there is no excuse for restating its component count by hand -
    /// a mismatch is exactly what readers then have to write code to work around (PDFBOX-4801).
    /// </summary>
    public class OutputIntentsFactoryTests
    {
        private static DictionaryToken WriteAndGetProfileStreamDictionary()
        {
            var written = new List<IToken>();

            var array = OutputIntentsFactory.GetOutputIntentsArray(token =>
            {
                written.Add(token);
                return new IndirectReferenceToken(new PdfPig.Core.IndirectReference(written.Count, 0));
            });

            Assert.Single(array.Data);
            var stream = Assert.IsType<StreamToken>(Assert.Single(written));
            return stream.StreamDictionary;
        }

        [Fact]
        public void ProfileStreamDeclaresTheComponentCountOfTheProfileItCarries()
        {
            var dictionary = WriteAndGetProfileStreamDictionary();

            Assert.True(dictionary.TryGet(NameToken.N, out NumericToken n));

            // The bundled profile is sRGB, so three - but read from the profile rather than assumed.
            var profileBytes = ProfileStreamReader.GetSRgb2014();
            Assert.True(IccProfileHeader.TryGetNumberOfComponents(profileBytes, out int expected));

            Assert.Equal(expected, n.Int);
            Assert.Equal(3, n.Int);
        }

        [Fact]
        public void TheWrittenIntentIsAPdfA1OutputIntent()
        {
            var written = new List<IToken>();

            var array = OutputIntentsFactory.GetOutputIntentsArray(token =>
            {
                written.Add(token);
                return new IndirectReferenceToken(new PdfPig.Core.IndirectReference(written.Count, 0));
            });

            var intent = Assert.IsType<DictionaryToken>(array.Data[0]);

            Assert.True(intent.TryGet(NameToken.S, out NameToken subtype));
            Assert.Equal("GTS_PDFA1", subtype.Data);
            Assert.True(intent.ContainsKey(NameToken.DestOutputProfile));
        }
    }

    public class IccProfileHeaderTests
    {
        private static byte[] HeaderWithDataColourSpace(string signature)
        {
            var header = new byte[128];
            for (int i = 0; i < 4; i++)
            {
                header[16 + i] = (byte)signature[i];
            }

            return header;
        }

        [Theory]
        [InlineData("GRAY", 1)]
        [InlineData("RGB ", 3)]
        [InlineData("Lab ", 3)]
        [InlineData("XYZ ", 3)]
        [InlineData("CMY ", 3)]
        [InlineData("CMYK", 4)]
        [InlineData("2CLR", 2)]
        [InlineData("7CLR", 7)]
        [InlineData("FCLR", 15)]
        public void ReadsTheComponentCountFromTheDataColourSpace(string signature, int expected)
        {
            Assert.True(IccProfileHeader.TryGetNumberOfComponents(HeaderWithDataColourSpace(signature),
                out int components));

            Assert.Equal(expected, components);
        }

        [Fact]
        public void RejectsAnUnknownDataColourSpace()
        {
            Assert.False(IccProfileHeader.TryGetNumberOfComponents(HeaderWithDataColourSpace("ZZZZ"), out _));
        }

        [Fact]
        public void RejectsATruncatedHeader()
        {
            Assert.False(IccProfileHeader.TryGetNumberOfComponents(new byte[127], out _));
        }
    }
}
