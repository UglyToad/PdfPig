namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using Integration;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    /// <summary>
    /// What the filter tests build over and over: dictionaries of numbers, stream dictionaries with
    /// decode parameters, rows of random data for the predictors, and the check of a real document
    /// stream against the content it is known to decode to.
    /// </summary>
    internal static class FilterTestHelpers
    {
        /// <summary>A dictionary of numeric entries.</summary>
        public static DictionaryToken Dictionary(params (NameToken Key, int Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>();

            foreach (var (key, value) in entries)
            {
                data[key] = new NumericToken(value);
            }

            return new DictionaryToken(data);
        }

        /// <summary>
        /// A stream dictionary naming one filter, its decode parameters, and any further numeric
        /// entries such as the height of an image.
        /// </summary>
        public static DictionaryToken StreamDictionary(NameToken filter, (NameToken Key, int Value)[] decodeParameters, params (NameToken Key, int Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>
            {
                [NameToken.Filter] = filter,
                [NameToken.DecodeParms] = Dictionary(decodeParameters)
            };

            foreach (var (key, value) in entries)
            {
                data[key] = new NumericToken(value);
            }

            return new DictionaryToken(data);
        }

        /// <summary>The distance between rows in the encoded data: a row plus its filter type byte for the PNG predictors.</summary>
        public static int Stride(int predictor, int rowLength) => predictor >= 10 ? rowLength + 1 : rowLength;

        /// <summary>
        /// Random bytes for <paramref name="length"/> bytes of predicted rows, with the filter type
        /// byte of every PNG row set to types 0 to 4 in turn, and to the undefined type 9 on every
        /// sixth row when asked.
        /// </summary>
        public static byte[] RandomRows(Random random, int length, int predictor, int stride, bool withUndefinedType = false)
        {
            var data = new byte[length];
            random.NextBytes(data);

            if (predictor >= 10)
            {
                for (var row = 0; row * stride < data.Length; row++)
                {
                    data[row * stride] = (byte)(withUndefinedType && row % 6 == 5 ? 9 : row % 5);
                }
            }

            return data;
        }

        /// <summary>What the whole-buffer decoder makes of a copy of <paramref name="data"/>.</summary>
        public static byte[] DecodedAtOnce(byte[] data, int predictor, int colors, int bitsPerComponent, int columns)
        {
            return PngPredictor.Decode((byte[])data.Clone(), predictor, colors, bitsPerComponent, columns).ToArray();
        }

        /// <summary>
        /// Decodes one stream of a test document and checks its length and SHA-256 against the
        /// content it is known to have.
        /// </summary>
        public static void AssertStreamDecodesTo(string documentName, int objectNumber, int expectedLength, string expectedSha256, bool lenient = false)
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath(documentName), new ParsingOptions { UseLenientParsing = lenient });

            var stream = Assert.IsType<StreamToken>(document.Structure.GetObject(new IndirectReference(objectNumber, 0)).Data);

            var decoded = stream.Decode(DefaultFilterProvider.Instance);

            Assert.Equal(expectedLength, decoded.Length);

            using var sha256 = SHA256.Create();
            var hash = BitConverter.ToString(sha256.ComputeHash(decoded.ToArray())).Replace("-", string.Empty);

            Assert.Equal(expectedSha256, hash);
        }
    }
}
