namespace UglyToad.Pdf.Tests.Fonts.TrueType.Parser
{
    using System;
    using System.IO;
    using System.Linq;
    using IO;
    using Pdf.Fonts.TrueType;
    using Pdf.Fonts.TrueType.Parser;
    using Xunit;

    public class TrueTypeFontParserTests
    {
        private static byte[] GetFileBytes(string name)
        {
            var manifestFiles = typeof(TrueTypeFontParserTests).Assembly.GetManifestResourceNames();

            var match = manifestFiles.Single(x => x.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0);

            using (var memoryStream = new MemoryStream())
            using (var stream = typeof(TrueTypeFontParserTests).Assembly.GetManifestResourceStream(match))
            {
                stream.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
        }

        private readonly TrueTypeFontParser parser = new TrueTypeFontParser();

        [Fact]
        public void ParseRegularRoboto()
        {
            var bytes = GetFileBytes("Roboto-Regular");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            parser.Parse(input);
        }

        [Fact]
        public void ParseEmbeddedSimpleGoogleDocssGautmi()
        {
            var bytes = GetFileBytes("google-simple-doc");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            parser.Parse(input);
        }
    }
}
