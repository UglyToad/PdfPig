namespace UglyToad.PdfPig.Tests.Fonts.Type1
{
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Fonts.Type1.Parser;

    public class Type1FontParserTests
    {
        [Fact]
        public void CanReadHexEncryptedPortion()
        {
            var bytes = GetFileBytes("AdobeUtopia.pfa");

            Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));
        }

        [Fact]
        public void CanReadBinaryEncryptedPortionOfFullPfb()
        {
            // TODO: support reading in these pfb files
            var bytes = GetFileBytes("Raleway-Black.pfb");

            Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));
        }

        [Fact]
        public void CanReadCharStrings()
        {
            var bytes = GetFileBytes("CMBX10.pfa");

            Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));
        }

        [Fact]
        public void CanReadEncryptedPortion()
        {
            var bytes = GetFileBytes("CMCSC10");

            Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));
        }

        [Fact]
        public void CanReadAsciiPart()
        {
            var bytes = GetFileBytes("CMBX12");

            Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));
        }

        [Fact]
        public void OutputCmbx10Svgs()
        {
            var bytes = GetFileBytes("CMBX10");

            var result = Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));

            var builder = new StringBuilder("<!DOCTYPE html><html><head></head><body>");
            foreach (var charString in result.CharStrings.CharStrings)
            {
                Assert.True(result.CharStrings.TryGenerate(charString.Key, out var path));
                //builder.AppendLine(path.ToFullSvg(0)); // TODO - to restore
            }

            builder.Append("</body></html>");

            File.WriteAllText("cmbx10.html", builder.ToString());
        }

        [Fact]
        public void CanReadFontWithCommentsInOtherSubrs()
        {
            var bytes = GetFileBytes("CMR10");

            Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));
        }

        [Fact]
        public void CanReadEncodingWhenFontInfoContainsComments()
        {
            var bytes = InsertBefore(GetFileBytes("CMR10"), "/FullName", "% A comment line\n% (c) another one\n");

            var font = Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256));

            Assert.Equal(9, font.Encoding.Count);
            Assert.Equal("C", font.Encoding[67]);
            Assert.Equal("s", font.Encoding[115]);
        }

        [Theory]
        [InlineData("ND")]
        [InlineData("|-")]
        public void CanReadSubrsArrayWithoutEntries(string endOfDefinition)
        {
            // macOS Quartz (e.g. Preview's "Export as PDF") writes subset Type 1 fonts whose
            // Subrs array is declared with a length but has no entries: "/Subrs 4 array ND".
            // "|-" is the other common name for the procedure that ends the definition.
            const string clearText = "%!FontType1-1.0: TestFont 1.0\n"
                                     + "12 dict begin\n"
                                     + "/FontName /TestFont def\n"
                                     + "/PaintType 0 def\n"
                                     + "/FontType 1 def\n"
                                     + "/FontMatrix [0.001 0 0 0.001 0 0] readonly def\n"
                                     + "/Encoding StandardEncoding def\n"
                                     + "/FontBBox {0 0 100 100} readonly def\n"
                                     + "currentdict end\n"
                                     + "currentfile eexec\n";

            // hsbw 0 100, rmoveto 10 0, rlineto 0 100, rlineto 50 0, rlineto 0 -100, closepath, endchar.
            byte[] square = [139, 239, 13, 149, 139, 21, 139, 239, 5, 189, 139, 5, 139, 39, 5, 9, 14];
            // hsbw 0 100, endchar.
            byte[] notdef = [139, 239, 13, 14];

            var privatePortion = new List<byte>();

            void Append(string text) => privatePortion.AddRange(Encoding.ASCII.GetBytes(text));

            void AppendCharString(string name, byte[] charString)
            {
                var encrypted = Encrypt([0, 0, 0, 0, .. charString], 4330);
                Append($"/{name} {encrypted.Length} RD ");
                privatePortion.AddRange(encrypted);
                Append(" ND\n");
            }

            Append("dup/Private 8 dict dup begin/RD{string currentfile exch readstring pop}executeonly def"
                   + "/ND{noaccess def}executeonly def/NP{noaccess put}executeonly def/BlueValues[]def"
                   + "/MinFeature{16 16}def/password 5839 def\n"
                   + $"/Subrs 4 array\n {endOfDefinition}\n"
                   + "2 index/CharStrings 2 dict dup begin\n");
            AppendCharString(".notdef", notdef);
            AppendCharString("A", square);
            Append("end\nend\nreadonly put\nnoaccess put\ndup/FontName get exch definefont pop\nmark currentfile closefile\n");

            var encryptedPortion = Encrypt([0, 0, 0, 0, .. privatePortion], 55665);
            byte[] bytes = [.. Encoding.ASCII.GetBytes(clearText), .. encryptedPortion];

            var font = Type1FontParser.Parse(new MemoryInputBytes(bytes), clearText.Length, encryptedPortion.Length, new StackDepthGuard(256));

            Assert.Empty(font.CharStrings.Subroutines);

            var box = font.GetCharacterBoundingBox("A");
            Assert.NotNull(box);
            Assert.Equal(10, box.Value.Left, 3);
            Assert.Equal(60, box.Value.Right, 3);
            Assert.Equal(0, box.Value.Bottom, 3);
            Assert.Equal(100, box.Value.Top, 3);
        }

        private static byte[] Encrypt(byte[] plain, int key)
        {
            var result = new byte[plain.Length];
            var r = key;
            for (var i = 0; i < plain.Length; i++)
            {
                var c = (byte)(plain[i] ^ (r >> 8));
                result[i] = c;
                r = ((c + r) * 52845 + 22719) & 0xFFFF;
            }

            return result;
        }

        private static byte[] InsertBefore(byte[] bytes, string marker, string text)
        {
            var index = bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes(marker));
            Assert.True(index > 0);

            return [.. bytes.AsSpan(0, index), .. Encoding.ASCII.GetBytes(text), .. bytes.AsSpan(index)];
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
