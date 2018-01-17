namespace UglyToad.PdfPig.Tokenization.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class ArrayToken : IDataToken<IReadOnlyList<IToken>>
    {
        public IReadOnlyList<IToken> Data { get; }

        public ArrayToken(IReadOnlyList<IToken> data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public override string ToString()
        {
            var builder = new StringBuilder("[ ");

            for (var i = 0; i < Data.Count; i++)
            {
                var token = Data[i];

                builder.Append(token);

                if (i < Data.Count - 1)
                {
                    builder.Append(',');
                }

                builder.Append(' ');
            }

            builder.Append(']');

            return builder.ToString();
        }
    }
}
