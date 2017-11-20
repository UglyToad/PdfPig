namespace UglyToad.Pdf.Fonts.Parser.Parts
{
    using System;
    using Cmap;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class BaseFontCharacterParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numeric, ITokenScanner tokenScanner, CharacterMapBuilder builder, bool isLenientParsing)
        {
            for (var i = 0; i < numeric.Int; i++)
            {
                if (!tokenScanner.MoveNext() || !(tokenScanner.CurrentToken is HexToken inputCode))
                {
                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }

                if (!tokenScanner.MoveNext())
                {
                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }

                if (tokenScanner.CurrentToken is NameToken characterName)
                {
                    builder.AddBaseFontCharacter(inputCode.Bytes, characterName.Data.Name);
                }
                else if (tokenScanner.CurrentToken is HexToken characterCode)
                {
                    builder.AddBaseFontCharacter(inputCode.Bytes, characterCode.Bytes);
                }
                else
                {
                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }
            }
        }
    }
}
