namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Core;
    using Tokens;
    using Tokenization;

    /// <inheritdoc />
    public sealed class Type1ArrayTokenizer : ITokenizer
    {
        /// <inheritdoc />
        public bool ReadsNextByte { get; } = false;

        private static readonly char[] Space = [' '];

        /// <inheritdoc />
        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '{')
            {
                return false;
            }

            var builder = new StringBuilder();

            while (inputBytes.MoveNext())
            {
                if (inputBytes.CurrentByte == '}')
                {
                    break;
                }

                builder.Append((char) inputBytes.CurrentByte);
            }

            var parts = builder.ToString().Split(Space, StringSplitOptions.RemoveEmptyEntries);

            var tokens = new List<IToken>();

            foreach (var part in parts)
            {
                if (char.IsNumber(part[0]) || part[0] == '-')
                {
                    if (double.TryParse(part, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
                    {
                        tokens.Add(new NumericToken(value));
                    }
                    else
                    {
                        tokens.Add(OperatorToken.Create(part.AsSpan()));
                    }

                    continue;
                }

                if (part[0] == '/')
                {
                    tokens.Add(NameToken.Create(part.Substring(1)));
                    continue;
                }

                if (part[0] == '(' && part[part.Length - 1] == ')')
                {
                    tokens.Add(new StringToken(part));
                    continue;
                }

                tokens.Add(OperatorToken.Create(part.AsSpan()));
            }

            token = new ArrayToken(tokens);

            return true;
        }
    }
}
