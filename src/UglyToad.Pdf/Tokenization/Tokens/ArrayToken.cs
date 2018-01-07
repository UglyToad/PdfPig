namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System.Collections.Generic;
    using System.Text;

    internal class ArrayToken : IDataToken<IReadOnlyList<IToken>>
    {
        public IReadOnlyList<IToken> Data { get; }

        public ArrayToken(IReadOnlyList<IToken> data)
        {
            Data = data;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("[ ");

            foreach (var token in Data)
            {
                builder.Append(token).Append(' ');
            }

            builder.Append(']');

            return builder.ToString();
        }
    }
}
