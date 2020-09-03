namespace UglyToad.PdfPig.Tests.Fonts.Parser
{
    using System.Text.RegularExpressions;
    using PdfFonts.Parser;
    using PdfPig.Core;
    using PdfPig.Encryption;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;
    using Xunit;

    public class EncodingReaderTests
    {
        [Fact]
        public void GetWinAnsiFromNameObject()
        {
            const string input = @"
1 0 obj
<< /Type /Font /Encoding 12 0 R /Name /WindowsFont1>>
endobj

12 0 obj
/WinAnsiEncoding
endobj";

            var scanner = GetScanner(input);

            var reader = new EncodingReader(scanner);

            var fontDictionary = scanner.Get(new IndirectReference(1, 0));

            var encoding = reader.Read(fontDictionary.Data as DictionaryToken);

            Assert.NotNull(encoding);
        }

        private static PdfTokenScanner GetScanner(string s)
        {
            var input = StringBytesTestConverter.Convert(s, false);

            var locationProvider = new TestObjectLocationProvider();

            var regex = new Regex(@"\n(\d+)\s(\d+)\sobj");

            foreach (Match match in regex.Matches(s))
            {
                var objN = match.Groups[1].Value;
                var gen = match.Groups[2].Value;

                locationProvider.Offsets[new IndirectReference(int.Parse(objN), int.Parse(gen))] = match.Index + 1;
            }
            
            return new PdfTokenScanner(input.Bytes, locationProvider,
                new TestFilterProvider(), NoOpEncryptionHandler.Instance);
        }
    }
}
