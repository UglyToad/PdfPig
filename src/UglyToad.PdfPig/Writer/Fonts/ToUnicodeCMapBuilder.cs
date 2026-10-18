namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Graphics.Operations;
    using Tokens;
    using Util;

    internal static class ToUnicodeCMapBuilder
    {
        // The CMap specification allows at most 100 mappings per bfchar block, the same limit as in PDFBox's ToUnicodeWriter.
        private const int MaxMappingsPerBlock = 100;

        private static ReadOnlySpan<byte> BeginToken => "begin"u8;
        private static ReadOnlySpan<byte> BeginCMapToken => "begincmap"u8;
        private static ReadOnlySpan<byte> DefToken => "def"u8;
        private static ReadOnlySpan<byte> DictToken => "dict"u8;
        private static ReadOnlySpan<byte> FindResourceToken => "findresource"u8;

        private static readonly TokenWriter TokenWriter = new TokenWriter();

        /// <summary>
        /// Writes a ToUnicode CMap for a simple font with 1-byte character codes.
        /// </summary>
        public static byte[] ConvertToCMapStream(IReadOnlyDictionary<char, byte> unicodeToCharacterCode)
        {
            return ConvertToCMapStream(unicodeToCharacterCode.Select(x => new KeyValuePair<char, int>(x.Key, x.Value)).ToList(), 1);
        }

        /// <summary>
        /// Writes a ToUnicode CMap for a composite font with 2-byte character codes.
        /// </summary>
        public static byte[] ConvertToCMapStream(IReadOnlyDictionary<char, ushort> unicodeToCharacterCode)
        {
            return ConvertToCMapStream(unicodeToCharacterCode.Select(x => new KeyValuePair<char, int>(x.Key, x.Value)).ToList(), 2);
        }

        private static byte[] ConvertToCMapStream(IReadOnlyList<KeyValuePair<char, int>> unicodeToCharacterCode, int characterCodeLength)
        {
            using var memoryStream = new MemoryStream();

            TokenWriter.WriteToken(NameToken.CidInit, memoryStream);
            TokenWriter.WriteToken(NameToken.ProcSet, memoryStream);
            memoryStream.WriteText(FindResourceToken, true);
            memoryStream.WriteText(BeginToken);

            memoryStream.WriteNewLine();

            memoryStream.WriteDouble(12);
            memoryStream.WriteWhiteSpace();
            memoryStream.WriteText(DictToken, true);
            memoryStream.WriteText(BeginToken);
            memoryStream.WriteNewLine();

            memoryStream.WriteText(BeginCMapToken);
            memoryStream.WriteNewLine();

            TokenWriter.WriteToken(NameToken.CidSystemInfo, memoryStream);

            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken> {
                { NameToken.Registry, new StringToken("Adobe") },
                { NameToken.Ordering, new StringToken("UCS") },
                { NameToken.Supplement, new NumericToken(0) }
            });

            TokenWriter.WriteToken(dictionary, memoryStream);
            memoryStream.WriteWhiteSpace();

            memoryStream.WriteText(DefToken);
            memoryStream.WriteNewLine();

            TokenWriter.WriteToken(NameToken.Cmapname, memoryStream);
            TokenWriter.WriteToken(NameToken.Create("Adobe-Identity-UCS"), memoryStream);
            memoryStream.WriteText(DefToken);

            memoryStream.WriteNewLine();

            TokenWriter.WriteToken(NameToken.CmapType, memoryStream);
            memoryStream.WriteNumberText(2, DefToken);
            memoryStream.WriteNumberText(1, "begincodespacerange"u8);

            TokenWriter.WriteToken(new HexToken(new string('0', characterCodeLength * 2).AsSpan()), memoryStream);
            TokenWriter.WriteToken(new HexToken(new string('F', characterCodeLength * 2).AsSpan()), memoryStream);

            memoryStream.WriteNewLine();

            memoryStream.WriteText("endcodespacerange"u8);
            memoryStream.WriteNewLine();

            for (var start = 0; start < unicodeToCharacterCode.Count; start += MaxMappingsPerBlock)
            {
                var count = Math.Min(MaxMappingsPerBlock, unicodeToCharacterCode.Count - start);

                memoryStream.WriteNumberText(count, "beginbfchar"u8);

                for (var i = start; i < start + count; i++)
                {
                    var keyValuePair = unicodeToCharacterCode[i];
                    var unicodeInt = (ushort)keyValuePair.Key;
                    var low = (byte)(unicodeInt >> 0);
                    var high = (byte)(unicodeInt >> 8);
                    var from = characterCodeLength == 1
                        ? Hex.GetString([(byte)keyValuePair.Value])
                        : Hex.GetString([(byte)(keyValuePair.Value >> 8), (byte)keyValuePair.Value]);
                    var to = Hex.GetString([high, low]);

                    TokenWriter.WriteToken(new HexToken(from.AsSpan()), memoryStream);
                    TokenWriter.WriteToken(new HexToken(to.AsSpan()), memoryStream);

                    memoryStream.WriteNewLine();
                }

                memoryStream.WriteText("endbfchar"u8);
                memoryStream.WriteNewLine();
            }

            memoryStream.WriteText("endcmap"u8);
            memoryStream.WriteNewLine();

            memoryStream.WriteText("CMapName currentdict /CMap defineresource pop"u8);
            memoryStream.WriteNewLine();

            memoryStream.WriteText("end"u8);
            memoryStream.WriteNewLine();

            memoryStream.WriteText("end"u8);
            memoryStream.WriteNewLine();

            return memoryStream.ToArray();
        }
    }
}