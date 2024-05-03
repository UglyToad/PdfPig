﻿namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using Cmap;
    using Tokenization.Scanner;
    using Tokens;

    internal class CidCharacterParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numeric, ITokenScanner scanner, CharacterMapBuilder builder)
        {
            var results = new List<CidCharacterMapping>();

            for (var i = 0; i < numeric.Int; i++)
            {
                if (!scanner.TryReadToken(out HexToken sourceCode))
                {
                    throw new InvalidOperationException("The first token in a line for Cid Characters should be a hex, instead it was: " + scanner.CurrentToken);
                }

                if (!scanner.TryReadToken(out NumericToken destinationCode))
                {
                    throw new InvalidOperationException("The destination token in a line for Cid Character should be an integer, instead it was: " + scanner.CurrentToken);
                }

                var sourceInteger = sourceCode.Bytes.ToInt();
                var mapping = new CidCharacterMapping(sourceInteger, destinationCode.Int);

                results.Add(mapping);
            }
            
            builder.CidCharacterMappings = results;
        }
    }
}
