namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Filters;
    using Logging;
    using Tokens;

    internal static class DataCompresser
    {
        public static byte[] CompressBytes(IReadOnlyList<byte> bytes) => CompressBytes(bytes.ToArray());
        public static byte[] CompressBytes(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
                var flater = new FlateFilter(new DecodeParameterResolver(new NoOpLog()), new PngPredictor(), new NoOpLog());
                var result = flater.Encode(memoryStream, parameters, 0);
                return result;
            }
        }

        public static StreamToken CompressToStream(IReadOnlyList<byte> bytes) => CompressToStream(bytes.ToArray());
        public static StreamToken CompressToStream(byte[] bytes)
        {
            var compressed = CompressBytes(bytes);
            var stream = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Length, new NumericToken(compressed.Length) },
                { NameToken.Length1, new NumericToken(bytes.Length) },
                { NameToken.Filter, new ArrayToken(new []{ NameToken.FlateDecode }) }
            }), compressed);

            return stream;
        }
    }
}
