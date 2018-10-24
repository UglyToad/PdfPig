namespace UglyToad.PdfPig.Tests.Fonts.Type1
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfPig.Fonts.Type1.Parser;
    using PdfPig.IO;
    using Xunit;

    public class Type1FontParserTests
    {
        private readonly Type1FontParser parser = new Type1FontParser(new Type1EncryptedPortionParser());

        [Fact]
        public void CanReadHexEncryptedPortion()
        {
            var bytes = GetFileBytes("AdobeUtopia.pfa");
            
            parser.Parse(new ByteArrayInputBytes(bytes),0, 0);
        }

        [Fact]
        public void CanReadBinaryEncryptedPortionOfFullPfb()
        {
            // TODO: support reading in these pfb files
            var bytes = GetFileBytes("Raleway-Black.pfb");
            
            parser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void CanReadCharStrings()
        {
            var bytes = GetFileBytes("CMBX10.pfa");

            parser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void CanReadEncryptedPortion()
        {
            var bytes = GetFileBytes("CMCSC10");

            parser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void CanReadAsciiPart()
        {
            var bytes = GetFileBytes("CMBX12");

            parser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        private static byte[] GetFileBytes(string name)
        {
            var manifestFiles = typeof(Type1FontParserTests).Assembly.GetManifestResourceNames();

            var match = manifestFiles.Single(x => x.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0);

            using (var memoryStream = new MemoryStream())
            using (var stream = typeof(Type1FontParserTests).Assembly.GetManifestResourceStream(match))
            {
                stream.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
        }
    }
}
