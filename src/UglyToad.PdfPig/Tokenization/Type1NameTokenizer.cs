namespace UglyToad.PdfPig.Tokenization
{
    using System.Text;
    using Core;
    using Parser.Parts;
    using Tokens;

    internal class Type1NameTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '/')
            {
                return false;
            }

            var builder = new StringBuilder();
            while (inputBytes.MoveNext())
            {
                if (ReadHelper.IsWhitespace(inputBytes.CurrentByte)
                    || inputBytes.CurrentByte == '{'
                    || inputBytes.CurrentByte == '<'
                    || inputBytes.CurrentByte == '/'
                    || inputBytes.CurrentByte == '['
                    || inputBytes.CurrentByte == '(')
                {
                    break;
                }

                builder.Append((char)inputBytes.CurrentByte);
            }

            token = NameToken.Create(builder.ToString());

            return true;
        }
    }
}
