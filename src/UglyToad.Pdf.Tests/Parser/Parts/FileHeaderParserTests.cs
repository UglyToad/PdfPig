namespace UglyToad.Pdf.Tests.Parser.Parts
{
    using System;
    using Exceptions;
    using Pdf.Parser.Parts;
    using Xunit;

    public class FileHeaderParserTests
    {
        private readonly FileHeaderParser parser = new FileHeaderParser(new TestingLog());

        [Fact]
        public void NullScannerThrows()
        {
            Action action = () => parser.Parse(null, false);

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

            var result = parser.Parse(scanner, false);

            Assert.Equal(format, result.VersionString);
        }

        [Fact]
        public void ReadsHeaderWithBlankSpaceBefore()
        {
            const string input = @"     

%PDF-1.2";

            var scanner = StringBytesTestConverter.Scanner(input);

            var result = parser.Parse(scanner, false);

            Assert.Equal(1.2m, result.Version);
        }

        [Fact]
        public void EmptyInputThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(string.Empty);
            
            Action action = () => parser.Parse(scanner, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void HeaderPrecededByJunkNonLenientThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one    
    %PDF-1.2");

            Action action = () => parser.Parse(scanner, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void HeaderPrecededByJunkLenientReads()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one    
    %PDF-1.7");

            var result = parser.Parse(scanner, true);

            Assert.Equal(1.7m, result.Version);
        }

        [Fact]
        public void HeaderPrecededByTooMuchJunkThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one two
three %PDF-1.6");

            Action action = () => parser.Parse(scanner, true);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void JunkThenEndThrows()
        {
            var scanner = StringBytesTestConverter.Scanner(@"one two");

            Action action = () => parser.Parse(scanner, true);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void VersionFormatInvalidNotLenientThrows()
        {
            var scanner = StringBytesTestConverter.Scanner("%Pdeef-1.69");

            Action action = () => parser.Parse(scanner, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void VersionFormatInvalidLenientDefaults1Point4()
        {
            var scanner = StringBytesTestConverter.Scanner("%Pdeef-1.69");

            var result = parser.Parse(scanner, true);

            Assert.Equal(1.4m, result.Version);
        }

        [Fact]
        public void ParsingResetsPosition()
        {
            var scanner = StringBytesTestConverter.Scanner(@"%FDF-1.6");

            parser.Parse(scanner, false);

            Assert.Equal(0, scanner.CurrentPosition);
        }
    }
}
