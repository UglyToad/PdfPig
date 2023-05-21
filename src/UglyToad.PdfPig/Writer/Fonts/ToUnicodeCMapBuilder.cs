namespace UglyToad.PdfPig.Writer.Fonts
{
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

        public static IReadOnlyList<byte> ConvertToCMapStream(IReadOnlyDictionary<char, byte> unicodeToCharacterCode)
        {
            using (var memoryStream = new MemoryStream())
            {
                TokenWriter.WriteToken(NameToken.CidInit, memoryStream);
                TokenWriter.WriteToken(NameToken.ProcSet, memoryStream);
                memoryStream.WriteText(FindResourceToken, true);
                memoryStream.WriteText(BeginToken);

                memoryStream.WriteNewLine();

                memoryStream.WriteDecimal(12);
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
                memoryStream.WriteNumberText(1, "begincodespacerange");

                TokenWriter.WriteToken(new HexToken(new[] {'0', '0'}), memoryStream);
                TokenWriter.WriteToken(new HexToken(new[] {'F', 'F'}), memoryStream);

                memoryStream.WriteNewLine();

                memoryStream.WriteText("endcodespacerange");

                memoryStream.WriteNewLine();

                memoryStream.WriteNumberText(unicodeToCharacterCode.Count, "beginbfchar");
                
                foreach (var keyValuePair in unicodeToCharacterCode)
                {
                    var unicodeInt = (ushort) keyValuePair.Key;
                    var low = (byte) (unicodeInt >> 0);
                    var high = (byte) (unicodeInt >> 8);
                    var from = Hex.GetString(new[] {keyValuePair.Value});
                    var to = Hex.GetString(new[] {high, low});

                    TokenWriter.WriteToken(new HexToken(from.ToCharArray()), memoryStream);
                    TokenWriter.WriteToken(new HexToken(to.ToCharArray()), memoryStream);

                    memoryStream.WriteNewLine();
                }

                memoryStream.WriteText("endbfchar");

                memoryStream.WriteNewLine();

                memoryStream.WriteText("endcmap");

                memoryStream.WriteNewLine();

                memoryStream.WriteText("CMapName currentdict /CMap defineresource pop");

                memoryStream.WriteNewLine();

                memoryStream.WriteText("end");

                memoryStream.WriteNewLine();

                memoryStream.WriteText("end");

                memoryStream.WriteNewLine();

                return memoryStream.ToArray();
            }
        }
    }
}
