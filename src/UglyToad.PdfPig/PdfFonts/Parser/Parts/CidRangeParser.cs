namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using Cmap;
    using Fonts;
    using Tokenization.Scanner;
    using Tokens;

    internal class CidRangeParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numeric, ITokenScanner scanner, CharacterMapBuilder builder, bool isLenientParsing)
        {
            for (var i = 0; i < numeric.Int; i++)
            {
                if (!scanner.TryReadToken(out HexToken startHexToken))
                {
                    throw new InvalidFontFormatException("Could not find the starting hex token for the CIDRange in this font.");
                }

                if (!scanner.TryReadToken(out HexToken endHexToken))
                {
                    throw new InvalidFontFormatException("Could not find the end hex token for the CIDRange in this font.");
                }

                if (!scanner.TryReadToken(out NumericToken mappedCode))
                {
                    throw new InvalidFontFormatException("Could not find the starting CID numeric token for the CIDRange in this font.");
                }

                var start = HexToken.ConvertHexBytesToInt(startHexToken);
                var end = HexToken.ConvertHexBytesToInt(endHexToken);

                var range = new CidRange(start, end, mappedCode.Int);

                builder.AddCidRange(range);
            }
        }
    }
}
