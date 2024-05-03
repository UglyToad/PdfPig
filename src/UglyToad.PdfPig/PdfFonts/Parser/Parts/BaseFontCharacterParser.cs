namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using Cmap;
    using Tokenization.Scanner;
    using Tokens;

    internal class BaseFontCharacterParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numeric, ITokenScanner tokenScanner, CharacterMapBuilder builder)
        {
            for (var i = 0; i < numeric.Int; i++)
            {
                if (!tokenScanner.MoveNext() || !(tokenScanner.CurrentToken is HexToken inputCode))
                {
                    if (tokenScanner.CurrentToken is OperatorToken op
                    && (string.Equals(op.Data, "endbfchar", StringComparison.OrdinalIgnoreCase) 
                        || string.Equals(op.Data, "endcmap", StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }

                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }

                if (!tokenScanner.MoveNext())
                {
                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }

                if (tokenScanner.CurrentToken is NameToken characterName)
                {
                    builder.AddBaseFontCharacter(inputCode.Bytes, characterName.Data);
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
