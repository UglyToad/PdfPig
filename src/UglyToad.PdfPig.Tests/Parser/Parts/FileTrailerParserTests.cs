﻿namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using PdfPig.Core;
    using PdfPig.Parser.FileStructure;
    using PdfPig.Tokenization.Scanner;

    public class FileTrailerFileTrailerParserTests
    {
        [Fact]
        public void FindsCompliantStartXref()
        {
            var input = StringBytesTestConverter.Convert(@"sta455%r endstream
endobj

12 0 obj
1234  %eof
endobj

startxref
    456

%%EOF", false);

            var result = FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Equal(456, result);
        }

        [Fact]
        public void IncludesStartXrefFollowingEndOfFile()
        {
            var input = StringBytesTestConverter.Convert(@"11 0 obj
<< /Type/Something /W[12 0 5 6] >>
endobj

12 0 obj
1234  %eof
endobj

startxref
    1384733

%%EOF

% I decided to put some nonsense here:
% because I could hahaha
startxref
17", false);

            var result = FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Equal(17, result);
        }

        [Fact]
        public void MissingStartXrefThrows()
        {
            var input = StringBytesTestConverter.Convert(@"11 0 obj
<< /Type/Something /W[12 0 5 6] >>
endobj

12 0 obj
1234  %eof
endobj

startref
    1384733

%%EOF

% I decided to put some nonsense here:
% because I could hahaha
start_rexf
17", false);

            Action action = () => FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void NullInputBytesThrows()
        {
            var input = StringBytesTestConverter.Convert("11 0 obj", false);

            Action action = () => FileTrailerParser.GetFirstCrossReferenceOffset(null, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void NullScannerThrows()
        {
            var input = StringBytesTestConverter.Convert("11 0 obj", false);

            Action action = () => FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, null, false);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void InvalidTokensAfterStartXrefThrows()
        {
            var input = StringBytesTestConverter.Convert(@"11 0 obj
        << /Type/Font >>
endobj

startxref 
<< /Why (am i here?) >> 69
%EOF", false);

            Action action = () => FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void MissingNumericAfterStartXrefThrows()
        {
            var input = StringBytesTestConverter.Convert(@"11 0 obj
        << /Type/Font >>
endobj

startxref 
   ", false);

            Action action = () => FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void TakesLastStartXrefPrecedingEndOfFile()
        {
            var input = StringBytesTestConverter.Convert(@"11 0 obj
<< /Type/Something /W[12 0 5 6] >>
endobj

12 0 obj
1234  %eof
endobj

startxref
    1384733

%actually I changed my mind

startxref
         1274665676543

%%EOF", false);

            var result = FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Equal(1274665676543, result);
        }

        [Fact]
        public void CanReadStartXrefIfCommentsPresent()
        {
            var input = StringBytesTestConverter.Convert(@"
startxref %Commented here
    57695

%%EOF", false);

            var result = FileTrailerParser.GetFirstCrossReferenceOffset(input.Bytes, new CoreTokenScanner(input.Bytes, true), false);

            Assert.Equal(57695, result);
        }
    }
}
