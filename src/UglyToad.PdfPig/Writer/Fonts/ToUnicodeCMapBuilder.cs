namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Graphics.Operations;
    using Tokens;
    using Util;

    internal static class ToUnicodeCMapBuilder
    {
        private const string BeginToken = "begin";
        private const string BeginCMapToken = "begincmap";
        private const string DefToken = "def";
        private const string DictToken = "dict";
        private const string FindResourceToken = "findresource";

        private static readonly TokenWriter TokenWriter = new TokenWriter();

        public static byte[] ConvertToCMapStream(IReadOnlyDictionary<char, byte> unicodeToCharacterCode)
        {
            using (var memoryStream = new MemoryStream())
            {
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

                var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
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

                TokenWriter.WriteToken(new HexToken(['0', '0']), memoryStream);
                TokenWriter.WriteToken(new HexToken(['F', 'F']), memoryStream);

                memoryStream.WriteNewLine();

                memoryStream.WriteText("endcodespacerange"u8);

                memoryStream.WriteNewLine();

                memoryStream.WriteNumberText(unicodeToCharacterCode.Count, "beginbfchar"u8);
                
                foreach (var keyValuePair in unicodeToCharacterCode)
                {
                    var unicodeInt = (ushort) keyValuePair.Key;
                    var low = (byte) (unicodeInt >> 0);
                    var high = (byte) (unicodeInt >> 8);
                    var from = Hex.GetString([keyValuePair.Value]);
                    var to = Hex.GetString([high, low]);

                    TokenWriter.WriteToken(new HexToken(from.AsSpan()), memoryStream);
                    TokenWriter.WriteToken(new HexToken(to.AsSpan()), memoryStream);

                    memoryStream.WriteNewLine();
                }

                memoryStream.WriteText("endbfchar"u8);

                memoryStream.WriteNewLine();

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
}
