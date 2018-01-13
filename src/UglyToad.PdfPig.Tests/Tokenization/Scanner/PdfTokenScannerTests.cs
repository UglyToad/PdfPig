namespace UglyToad.PdfPig.Tests.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using PdfPig.ContentStream;
    using PdfPig.Cos;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    public class PdfTokenScannerTests
    {
        private readonly CrossReferenceTable table = new CrossReferenceTable(CrossReferenceType.Table, new Dictionary<CosObjectKey, long>(), 
            new PdfDictionary());

        [Fact]
        public void ReadsSimpleObject()
        {
            const string s = @"294 0 obj/WDKAAR+CMBX12 endobj";

            var pdfScanner = GetScanner(s); 

            pdfScanner.MoveNext();

            var objectToken = Assert.IsType<ObjectToken>(pdfScanner.CurrentToken);

            var name = Assert.IsType<NameToken>(objectToken.Data);

            Assert.Equal(294, objectToken.Number.ObjectNumber);
            Assert.Equal(0, objectToken.Number.Generation);

            Assert.Equal("WDKAAR+CMBX12", name.Data.Name);

            Assert.StartsWith("294 0 obj", s.Substring((int)objectToken.Position));
        }

        [Fact]
        public void ReadsNumericObjectWithComment()
        {
            const string s = @"%PDF-1.2

% I commented here too, tee hee
10383384 2 obj
%and here, I just love comments

45

endobj

%%EOF";

            var pdfScanner = GetScanner(s);

            pdfScanner.MoveNext();

            var obj = Assert.IsType<ObjectToken>(pdfScanner.CurrentToken);

            var num = Assert.IsType<NumericToken>(obj.Data);

            Assert.Equal(45, num.Int);

            Assert.Equal(10383384, obj.Number.ObjectNumber);
            Assert.Equal(2, obj.Number.Generation);

            Assert.StartsWith("10383384 2 obj", s.Substring((int)obj.Position));

            Assert.False(pdfScanner.MoveNext());
        }

        [Fact]
        public void ReadsArrayObject()
        {
            const string s = @"endobj295 0 obj[ 676 938 875 787 750 880 813 875 813 875 813 656 625 625 938 938 313 344 563 563 563 563 563 850 500 574 813 875 563 1019 1144 875 313]endobj";

            var pdfScanner = GetScanner(s);

            pdfScanner.MoveNext();

            var obj = Assert.IsType<ObjectToken>(pdfScanner.CurrentToken);

            var array = Assert.IsType<ArrayToken>(obj.Data);

            Assert.Equal(676, ((NumericToken)array.Data[0]).Int);

            Assert.Equal(33, array.Data.Count);

            Assert.Equal(295, obj.Number.ObjectNumber);
            Assert.Equal(0, obj.Number.Generation);

            Assert.StartsWith("295 0 obj", s.Substring((int)obj.Position));

            Assert.False(pdfScanner.MoveNext());
        }

        [Fact]
        public void ReadsDictionaryObjectThenNameThenDictionary()
        {
            const string s = @"

274 0 obj<< /Type /Pages /Count 2 /Parent 275 0 R /Kids [ 121 0 R 125 0 R ] >> endobj
%Other parts...310 0 obj/WPXNWT+CMR9 endobj 311 0 obj<< /Type /Font /Subtype /Type1 /FirstChar 0 /LastChar 127 /Widths 313 0 R /BaseFont 310 0 R /FontDescriptor 312 0 R >> endobj";

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);
            
            var dictionary = Assert.IsType<DictionaryToken>(tokens[0].Data);

            Assert.Equal(4, dictionary.Data.Count);
            Assert.Equal(274, tokens[0].Number.ObjectNumber);
            Assert.StartsWith("274 0 obj", s.Substring((int)tokens[0].Position));

            var nameObject = Assert.IsType<NameToken>(tokens[1].Data);

            Assert.Equal("WPXNWT+CMR9", nameObject.Data.Name);
            Assert.Equal(310, tokens[1].Number.ObjectNumber);
            Assert.StartsWith("310 0 obj", s.Substring((int)tokens[1].Position));

            dictionary = Assert.IsType<DictionaryToken>(tokens[2].Data);

            Assert.Equal(7, dictionary.Data.Count);
            Assert.Equal(311, tokens[2].Number.ObjectNumber);
            Assert.StartsWith("311 0 obj", s.Substring((int)tokens[2].Position));
        }

        [Fact]
        public void ReadsStringObject()
        {
            const string s = @"

58949797283757 0 obj    (An object begins with obj and ends with endobj...) endobj
";

            var scanner = GetScanner(s);

            var token = ReadToEnd(scanner)[0];

            Assert.Equal(58949797283757L, token.Number.ObjectNumber);
            Assert.Equal("An object begins with obj and ends with endobj...", Assert.IsType<StringToken>(token.Data).Data);

            Assert.StartsWith("58949797283757 0 obj", s.Substring((int)token.Position));
        }

        private PdfTokenScanner GetScanner(string s)
        {
            var input = StringBytesTestConverter.Convert(s, false);

            return new PdfTokenScanner(input.Bytes, table);
        }

        private static IReadOnlyList<ObjectToken> ReadToEnd(PdfTokenScanner scanner)
        {
            var result = new List<ObjectToken>();

            while (scanner.MoveNext())
            {
                if (scanner.CurrentToken is ObjectToken obj)
                {
                    result.Add(obj);
                }
                else
                {
                    throw new InvalidOperationException($"Pdf token scanner produced token which was not an object token: {scanner.CurrentToken}.");
                }
            }

            return result;
        }
    }
}
