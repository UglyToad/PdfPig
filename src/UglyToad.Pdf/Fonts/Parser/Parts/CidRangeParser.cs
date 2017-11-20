namespace UglyToad.Pdf.Fonts.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using Cmap;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class CidRangeParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numeric, ITokenScanner scanner, CharacterMapBuilder builder, bool isLenientParsing)
        {
            var ranges = new List<CidRange>();

            for (var i = 0; i < numeric.Int; i++)
            {
                if (!scanner.TryReadToken(out HexToken startHexToken))
                {
                    // TODO: message
                    throw new InvalidOperationException();
                }

                if (!scanner.TryReadToken(out HexToken endHexToken))
                {
                    // TODO: message
                    throw new InvalidOperationException();
                }

                if (!scanner.TryReadToken(out NumericToken mappedCode))
                {
                    // TODO: message
                    throw new InvalidOperationException();
                }

                var start = HexToken.ConvertHexBytesToInt(startHexToken);
                var end = HexToken.ConvertHexBytesToInt(endHexToken);

                var range = new CidRange((char)start, (char)end, mappedCode.Int);

                ranges.Add(range);
            }

            builder.CidRanges = ranges;
        }
    }
}
