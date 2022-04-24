namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using System;
    using Logging;
    using PdfPig.Core;
    using PdfPig.Parser.FileStructure;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;
    using System.Linq;
    using Xunit;

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
        [InlineData("PDF-1.0")]
        [InlineData("PDF-1.1")]
        [InlineData("PDF-1.7")]
        [InlineData("PDF-1.9")]
        [InlineData("FDF-1.0")]
        [InlineData("FDF-1.9")]
        public void ReadsConformingHeader(string format)
        {
            var input = $"%{format}\nany garbage";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(format, result.VersionString);
            Assert.Equal(0, result.OffsetInFile);
        }

        [Fact]
        public void ReadsHeaderWithBlankSpaceBefore()
        {
            const string input = @"     

%PDF-1.2";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(1.2m, result.Version);
            Assert.Equal(TestEnvironment.IsUnixPlatform ? 7 : 9, result.OffsetInFile);
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
            var scanner = StringBytesTestConverter.Scanner(@"one    
    %PDF-1.2");

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(1.2m, result.Version);
            Assert.Equal(TestEnvironment.IsUnixPlatform ? 12 : 13, result.OffsetInFile);
        }

        [Fact]
        public void HeaderPrecededByJunkLenientReads()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one    
    %PDF-1.7");

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Equal(1.7m, result.Version);
            Assert.Equal(TestEnvironment.IsUnixPlatform ? 12 : 13, result.OffsetInFile);
        }

        [Fact]
        public void HeaderPrecededByJunkDoesNotThrow()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one two
three %PDF-1.6");

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, true, log);

            Assert.Equal(1.6m, result.Version);
            Assert.Equal(TestEnvironment.IsUnixPlatform ? 14 : 15, result.OffsetInFile);
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

            Assert.Equal(1.4m, result.Version);
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

            var bytes = new ByteArrayInputBytes(input);

            var scanner = new CoreTokenScanner(bytes, ScannerScope.None);

            var result = FileHeaderParser.Parse(scanner, bytes, false, log);

            Assert.Equal(1.7m, result.Version);
        }

        [Fact]
        public void Issue443()
        {
            const string hex =
                @"00 0F 4A 43 42 31 33 36 36 31 32 32 37 2E 70 64 66 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 50 44 46 20 43 41 52 4F 01 00 FF FF FF FF 00 00 00 00 00 04 DF 28 00 00 00 00 AF 51 7E 82 AF 52 D7 09 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 81 81 03 0D 00 00 25 50 44 46 2D 31 2E 31 0A 25 E2 E3 CF D3 0D 0A 31 20 30 20 6F 62 6A";

            var bytes = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => HexToken.Convert(x[0], x[1]));

            var str = OtherEncodings.BytesAsLatin1String(bytes.ToArray());

            var scanner = StringBytesTestConverter.Scanner(str);

            var result = FileHeaderParser.Parse(scanner.scanner, scanner.bytes, false, log);

            Assert.Equal(0, scanner.scanner.CurrentPosition);
            Assert.Equal(128, result.OffsetInFile);
            Assert.Equal(1.1m, result.Version);
            Assert.Equal("PDF-1.1", result.VersionString);
        }
    }
}
