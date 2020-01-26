namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using System;
    using Logging;
    using PdfPig.Core;
    using PdfPig.Parser.FileStructure;
    using Xunit;

    public class FileHeaderParserTests
    {
        private readonly ILog log = new NoOpLog();
        [Fact]
        public void NullScannerThrows()
        {
            Action action = () => FileHeaderParser.Parse(null, false, log);

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

            var result = FileHeaderParser.Parse(scanner, false, log);

            Assert.Equal(format, result.VersionString);
            Assert.Equal(0, result.OffsetInFile);
        }

        [Fact]
        public void ReadsHeaderWithBlankSpaceBefore()
        {
            const string input = @"     

%PDF-1.2";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = FileHeaderParser.Parse(scanner, false, log);

            Assert.Equal(1.2m, result.Version);
            Assert.Equal(9, result.OffsetInFile);
        }

        [Fact]
        public void EmptyInputThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(string.Empty);
            
            Action action = () => FileHeaderParser.Parse(scanner, false, log);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void HeaderPrecededByJunkNonLenientDoesNotThrow()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one    
    %PDF-1.2");

            var result = FileHeaderParser.Parse(scanner, false, log);

            Assert.Equal(1.2m, result.Version);
            Assert.Equal(13, result.OffsetInFile);
        }

        [Fact]
        public void HeaderPrecededByJunkLenientReads()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one    
    %PDF-1.7");

            var result = FileHeaderParser.Parse(scanner, true, log);

            Assert.Equal(1.7m, result.Version);
            Assert.Equal(13, result.OffsetInFile);
        }

        [Fact]
        public void HeaderPrecededByJunkDoesNotThrow()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one two
three %PDF-1.6");

            var result = FileHeaderParser.Parse(scanner, true, log);

            Assert.Equal(1.6m, result.Version);
            Assert.Equal(15, result.OffsetInFile);
        }

        [Fact]
        public void JunkThenEndThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one two");

            Action action = () => FileHeaderParser.Parse(scanner, true, log);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void VersionFormatInvalidNotLenientThrows()
        {
            var scanner = StringBytesTestConverter.Scanner("%Pdeef-1.69");

            Action action = () => FileHeaderParser.Parse(scanner, false, log);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void VersionFormatInvalidLenientDefaults1Point4()
        {
            var scanner = StringBytesTestConverter.Scanner("%Pdeef-1.69");

            var result = FileHeaderParser.Parse(scanner, true, log);

            Assert.Equal(1.4m, result.Version);
        }

        [Fact]
        public void ParsingResetsPosition()
        {
            var scanner = StringBytesTestConverter.Scanner(@"%FDF-1.6");

            var result = FileHeaderParser.Parse(scanner, false, log);

            Assert.Equal(0, scanner.CurrentPosition);
            Assert.Equal(0, result.OffsetInFile);
        }
    }
}
