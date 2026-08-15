namespace UglyToad.PdfPig.Tests.Writer
{
    using System.Collections.Generic;
    using System.Text;
    using PdfPig.Tokens;
    using PdfPig.Writer.Colors;
    using Xunit;

    /// <summary>
    /// 8.6.5.5 requires the <c>/N</c> of a profile stream to match the profile it carries. On the writing
    /// side PdfPig owns that profile, so there is no excuse for restating its component count by hand.
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

            Assert.Equal(ProfileStreamReader.SRgb2014NumberOfComponents, n.Int);
            Assert.Equal(3, n.Int);
        }

        [Fact]
        public void TheEmbeddedProfileIsTheThreeComponentOneThatCountDescribes()
        {
            var profileBytes = ProfileStreamReader.GetSRgb2014();

            // Data colour space signature, offset 16 of the 128-byte header (ICC.1, Table 14).
            Assert.True(profileBytes.Length >= 128);
            Assert.Equal("RGB ", Encoding.ASCII.GetString(profileBytes, 16, 4));

            Assert.Equal(3, ProfileStreamReader.SRgb2014NumberOfComponents);
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
}
