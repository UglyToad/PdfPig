namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using Logging;
    using PdfPig.Core;
    using PdfPig.Parser.FileStructure;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    public class FileHeaderParserTests
    {
        private readonly ILog log = new NoOpLog();
        [Fact]
        public void NullScannerThrows()
        {
            Action action = () => FileHeaderParser.Parse(null, null, false, log);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Theory]
        [InlineData("PDF-1.0", 1.0)]
        [InlineData("PDF-1.1", 1.1)]
        [InlineData("PDF-1.7", 1.7)]
        [InlineData("PDF-1.9", 1.9)]
        [InlineData("PDF-2.0", 2.0)]
        [InlineData("PDF-2.9", 2.9)]
        [InlineData("pdf-2.0", 2.0)]
        [InlineData("FDF-1.0", 1.0)]
        [InlineData("FDF-1.9", 1.9)]
        [InlineData("FDF-2.0", 2.0)]
        [InlineData("fdf-2.0", 2.0)]
        public void ReadsConformingHeader(string format, double expectedVersion)
        {
            var input = $"%{format}\nany garbage";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(expectedVersion, result.Version);
            Assert.Equal(format, result.VersionString);
            Assert.Equal(0, result.OffsetInFile);
        }

        [Theory]
        [InlineData("PDF-2.0", 2.0)]
        [InlineData("FDF-2.0", 2.0)]
        public void ReadsVersion2HeaderWhenLenient(string format, double expectedVersion)
        {
            // A 2.x header used to fall through to the missing version handling, which silently
            // reported 1.4 when lenient rather than the actual version.
            var input = $"%{format}\nany garbage";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Equal(expectedVersion, result.Version);
            Assert.Equal(format, result.VersionString);
        }

        [Fact]
        public void ReadsVersion2HeaderPrecededByJunk()
        {
            const string input = "one two\nthree %PDF-2.0";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(2.0, result.Version);
            Assert.Equal("PDF-2.0", result.VersionString);
            Assert.Equal(input.IndexOf('%'), result.OffsetInFile);
        }

        [Fact]
        public void ReadsVersion2HeaderFoundByBruteForce()
        {
            // More junk tokens than the parser tolerates before the header, so the version
            // has to be located by scanning the raw bytes instead.
            var junk = string.Join(" ", Enumerable.Repeat("junk", 40));

            var input = $"{junk}\n%PDF-2.0\n1 0 obj";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(2.0, result.Version);
            Assert.Equal("PDF-2.0", result.VersionString);
            Assert.Equal(input.IndexOf('%'), result.OffsetInFile);
        }

        [Theory]
        [InlineData("%PDF-20")]
        [InlineData("%PDF-2")]
        [InlineData("%PDF-")]
        [InlineData("%PDF-x.y")]
        [InlineData("%PDF2.0")]
        public void MalformedVersionNumberNotLenientThrows(string input)
        {
            var scanner = StringBytesTestConverter.Scanner(input);

            Action action = () => FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Theory]
        [InlineData("%PDF-20")]
        [InlineData("%PDF-2")]
        [InlineData("%PDF-x.y")]
        public void MalformedVersionNumberLenientDefaults1Point4(string input)
        {
            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Equal(1.4, result.Version);
        }

        [Fact]
        public void ReadsHeaderWithBlankSpaceBefore()
        {
            const string input = @"     

%PDF-1.2";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(1.2, result.Version);
            Assert.Equal(TestEnvironment.IsSingleByteNewLine(input) ? 7 : 9, result.OffsetInFile);
        }

        [Fact]
        public void EmptyInputThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(string.Empty);

            Action action = () => FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void HeaderPrecededByJunkNonLenientDoesNotThrow()
        {
            var input = @"one    
    %PDF-1.2";
            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(1.2, result.Version);
            Assert.Equal(TestEnvironment.IsSingleByteNewLine(input) ? 12 : 13, result.OffsetInFile);
        }

        [Fact]
        public void HeaderPrecededByJunkLenientReads()
        {
            var input = @"one    
    %PDF-1.7";
            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Equal(1.7, result.Version);
            Assert.Equal(TestEnvironment.IsSingleByteNewLine(input) ? 12 : 13, result.OffsetInFile);
        }

        [Fact]
        public void HeaderPrecededByJunkDoesNotThrow()
        {
            var s = @"one two
three %PDF-1.6";

            var scanner = StringBytesTestConverter.Scanner(s);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Equal(1.6, result.Version);
            Assert.Equal(TestEnvironment.IsSingleByteNewLine(s) ? 14 : 15, result.OffsetInFile);
        }

        [Fact]
        public void JunkThenEndThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one two");

            Action action = () => FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void VersionFormatInvalidNotLenientThrows()
        {
            var scanner = StringBytesTestConverter.Scanner("%Pdeef-1.69");

            Action action = () => FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void VersionFormatInvalidLenientDefaults1Point4()
        {
            var scanner = StringBytesTestConverter.Scanner("%Pdeef-1.69");

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Equal(1.4, result.Version);
        }

        [Fact]
        public void ParsingResetsPosition()
        {
            var scanner = StringBytesTestConverter.Scanner(@"%FDF-1.6");

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(0, scanner.scanner.CurrentPosition);
            Assert.Equal(0, result.OffsetInFile);
        }

        [Fact]
        public void Issue334()
        {
            var input = OtherEncodings.StringAsLatin1Bytes("%PDF-1.7\r\n%âãÏÓ\r\n1 0 obj\r\n<</Lang(en-US)>>\r\nendobj");

            var bytes = new MemoryInputBytes(input);

            var scanner = new CoreTokenScanner(bytes, true, new StackDepthGuard(256), ScannerScope.None);

            var result = FileHeaderParser.Parse(scanner, bytes, false, log);

            Assert.Equal(1.7, result.Version);
        }

        [Fact]
        public void Issue443()
        {
            const string hex =
                @"00 0F 4A 43 42 31 33 36 36 31 32 32 37 2E 70 64 66 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 50 44 46 20 43 41 52 4F 01 00 FF FF FF FF 00 00 00 00 00 04 DF 28 00 00 00 00 AF 51 7E 82 AF 52 D7 09 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 81 81 03 0D 00 00 25 50 44 46 2D 31 2E 31 0A 25 E2 E3 CF D3 0D 0A 31 20 30 20 6F 62 6A";

            var bytes = hex.Split(' ').Where(x => x.Length > 0).Select(x => HexToken.ConvertPair(x[0], x[1]));

            var str = OtherEncodings.BytesAsLatin1String(bytes.ToArray());

            var scanner = StringBytesTestConverter.Scanner(str);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(0, scanner.scanner.CurrentPosition);
            Assert.Equal(128, result.OffsetInFile);
            Assert.Equal(1.1, result.Version);
            Assert.Equal("PDF-1.1", result.VersionString);
        }
    }
}
