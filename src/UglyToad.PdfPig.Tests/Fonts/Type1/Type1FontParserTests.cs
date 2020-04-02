namespace UglyToad.PdfPig.Tests.Fonts.Type1
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Fonts.Type1.Parser;
    using PdfPig.Geometry;
    using Xunit;

    public class Type1FontParserTests
    {
        [Fact]
        public void CanReadHexEncryptedPortion()
        {
            var bytes = GetFileBytes("AdobeUtopia.pfa");

            Type1FontParser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void CanReadBinaryEncryptedPortionOfFullPfb()
        {
            // TODO: support reading in these pfb files
            var bytes = GetFileBytes("Raleway-Black.pfb");

            Type1FontParser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void CanReadCharStrings()
        {
            var bytes = GetFileBytes("CMBX10.pfa");

            Type1FontParser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void CanReadEncryptedPortion()
        {
            var bytes = GetFileBytes("CMCSC10");

            Type1FontParser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void CanReadAsciiPart()
        {
            var bytes = GetFileBytes("CMBX12");

            Type1FontParser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        [Fact]
        public void OutputCmbx10Svgs()
        {
            var bytes = GetFileBytes("CMBX10");

            var result = Type1FontParser.Parse(new ByteArrayInputBytes(bytes), 0, 0);

            var builder = new StringBuilder("<!DOCTYPE html><html><head></head><body>");
            foreach (var charString in result.CharStrings.CharStrings)
            {
                Assert.True(result.CharStrings.TryGenerate(charString.Key, out var path));
                builder.AppendLine(path.ToFullSvg(0));
            }

            builder.Append("</body></html>");

            File.WriteAllText("cmbx10.html", builder.ToString());
        }

        [Fact]
        public void CanReadFontWithCommentsInOtherSubrs()
        {
            var bytes = GetFileBytes("CMR10");

            Type1FontParser.Parse(new ByteArrayInputBytes(bytes), 0, 0);
        }

        private static byte[] GetFileBytes(string name)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fonts", "Type1"));
            var files = Directory.GetFiles(documentFolder);

            var file = files.FirstOrDefault(x => x.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

            if (file == null)
            {
                throw new InvalidOperationException($"Could not find test file {name} in folder {documentFolder}.");
            }

            return File.ReadAllBytes(file);
        }
    }
}
