namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using Cmap;
    using Tokenization.Scanner;
    using Tokens;

    internal class CodespaceRangeParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numeric, ITokenScanner tokenScanner, CharacterMapBuilder builder)
        {
            /*
             * For example:
             3 begincodespacerange
                <00>    <80>
                <8140>  <9ffc>
                <a0>    <de>
             endcodespacerange
             */

            var ranges = new List<CodespaceRange>(numeric.Int);

            for (var i = 0; i < numeric.Int; i++)
            {
                if (!tokenScanner.MoveNext())
                {
                    throw new InvalidOperationException("Codespace range have reach an unexpected end");
                }

                if (tokenScanner.CurrentToken is OperatorToken operatorToken && operatorToken.Data == "endcodespacerange")
                {
                    // Don't add this code space range
                    break;
                }

                if (!(tokenScanner.CurrentToken is HexToken start))
                {
                    throw new InvalidOperationException("Codespace range contains an unexpected token: " + tokenScanner.CurrentToken);
                }

                if (!tokenScanner.MoveNext() || !(tokenScanner.CurrentToken is HexToken end))
                {
                    throw new InvalidOperationException("Codespace range contains an unexpected token: " + tokenScanner.CurrentToken);
                }

                ranges.Add(new CodespaceRange(start.Memory, end.Memory));
            }

            builder.CodespaceRanges = ranges;
        }
    }
}
