namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using PdfPig.Filters;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    /*
     * catalog
     * the primary dictionary object containing references directly or indirectly to all other objects in the document with
     * the exception that there may be objects in the trailer that are not referred to by the catalog
     * 
     * General Format
     * A one-line header identifying the version of the PDF specification to which the file conforms
     * • A body containing the objects that make up the document contained in the file
     * • A cross-reference table containing information about the indirect objects in the file
     * • A trailer giving the location of the cross-reference table and of certain special objects within the body of the file
     * 
     * Each line shall be terminated by an end-of-line (EOL) marker, which may be a CARRIAGE RETURN (0Dh), a
     * LINE FEED (0Ah), or both. PDF files with binary data may have arbitrarily long lines. 
     * 
     * The first line of a PDF file shall be a header consisting of the 5 characters %PDF– followed by a version
     * number of the form 1.N, where N is a digit between 0 and 7.
     * A conforming reader shall accept files with any of the following headers:
     * %PDF–1.0
     * %PDF–1.1
     * %PDF–1.2
     * %PDF–1.3
     * %PDF–1.4
     * %PDF–1.5
     * %PDF–1.6
     * %PDF–1.7
     * Beginning with PDF 1.4, the Version entry in the document’s catalog dictionary (located via the Root entry in
     * the file’s trailer, as described in 7.5.5, "File Trailer"), if present, shall be used instead of the version specified in
     * the Header. 
     */

    public class PdfParserTests
    {
        [Fact]
        public void CanDecompressNormalObjectStream()
        {
            var bytes = File.ReadAllBytes(GetNthFilename());

            // The 7th object is encoded with no flate parameters.
            var obj7 = GetOffset(bytes, "7 0 obj");

            var streamPosition = GetOffset(bytes, "stream", obj7.end);

            var endStreamPosition = GetOffset(bytes, "endstream", streamPosition.end);

            var streamBytes = BytesBetween(streamPosition.end + 1, endStreamPosition.start - 1, bytes);

            var decodedBytes = new List<int>();
            using (var memo = new MemoryStream(streamBytes))
            {
                // Skip the first 2 header bytes, C#'s DeflateStream can't deal with them.
                memo.ReadByte();
                memo.ReadByte();

                using (var str = new DeflateStream(memo, CompressionMode.Decompress))
                {
                    var x = str.ReadByte();
                    while (x != -1)
                    {
                        decodedBytes.Add(x);
                        x = str.ReadByte();
                    }
                }
            }
        }

        [Fact]
        public void CanDecompressPngEncodedFlateStream()
        {
            // The Xref stream is encoded with parameters.
            // For flate as per http://www.adobe.com/content/dam/Adobe/en/devnet/acrobat/pdfs/pdf_reference_1-7.pdf
            var bytes = File.ReadAllBytes(GetNthFilename());

            var streamPosition = GetOffset(bytes, "stream");

            var endStreamPosition = GetOffset(bytes, "endstream", streamPosition.end);

            var streamBytes = BytesBetween(streamPosition.end + 1, endStreamPosition.start - 1, bytes);

            var paramsDict = new DictionaryToken(new Dictionary<IToken, IToken>
            {
                { NameToken.Predictor, new NumericToken(12) },
                { NameToken.Columns, new NumericToken(4) }
            });

            var dictionary = new DictionaryToken(new Dictionary<IToken, IToken>
            {
                {NameToken.Filter, NameToken.FlateDecode},
                {NameToken.DecodeParms, paramsDict}
            });

            var filter = new FlateFilter(new DecodeParameterResolver(null), new PngPredictor(), null);
            var filtered = filter.Decode(streamBytes, dictionary, 0);
            
            var expected =
                "1 0 15 0 1 0 216 0 1 2 160 0 1 2 210 0 1 3 84 0 1 4 46 0 1 7 165 0 1 70 229 0 1 72 84 0 1 96 235 0 1 98 18 0 2 0 12 0 2 0 12 1 2 0 12 2 2 0 12 3 2 0 12 4 2 0 12 5 2 0 12 6 2 0 12 7 2 0 12 8"
                .Split(' ')
                .Select(byte.Parse).ToArray();
            
            Assert.Equal(filtered, expected);
        }

        /// <summary>
        /// GetLongOrDefault the start and end offset of a search term.
        /// </summary>
        private static (int start, int end) GetOffset(IReadOnlyList<byte> input, string term, int offset = 0)
        {
            var streamBytes = Encoding.UTF8.GetBytes(term);

            var index = 0;
            var outerFound = false;
            for (int i = offset; i < input.Count; i++)
            {
                if (input[i] == streamBytes[0])
                {
                    bool found = true;
                    for (int j = 1; j < streamBytes.Length; j++)
                    {
                        if (input[i + j] != streamBytes[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        index = i;
                        outerFound = true;
                        break;
                    }
                }
            }

            if (!outerFound)
            {
                throw new InvalidOperationException("Could not find a stream in the bytes");
            }

            return (index, index + term.Length);
        }

        private static byte[] BytesBetween(int start, int endExclusive, byte[] bytes)
        {
            var result = new List<byte>();
            for (var i = start; i < endExclusive; i++)
            {
                result.Add(bytes[i]);
            }

            return result.ToArray();
        }

        private static string GetNthFilename(int n = 0)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Single Page Simple - from google drive.pdf");
        }
    }
}
