namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Filters;
    using Tokens;

    /// <summary>
    /// Data Compressor for Token Stream
    /// </summary>
    public static class DataCompresser
    {
        /// <summary>
        /// Compress token bytes
        /// </summary>
        public static byte[] CompressBytes(IReadOnlyList<byte> bytes) => CompressBytes(bytes.ToArray());
        /// <summary>
        /// Compress token bytes
        /// </summary>
        public static byte[] CompressBytes(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
                var flater = new FlateFilter();
                var result = flater.Encode(memoryStream, parameters, 0);
                return result;
            }
        }
        /// <summary>
        /// Compress token bytes
        /// </summary>

        public static StreamToken CompressToStream(IReadOnlyList<byte> bytes) => CompressToStream(bytes.ToArray());
        /// <summary>
        /// Compress token bytes
        /// </summary>
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
