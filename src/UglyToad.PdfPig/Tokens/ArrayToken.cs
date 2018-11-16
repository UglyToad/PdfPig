namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using ContentStream;

    internal class ArrayToken : IDataToken<IReadOnlyList<IToken>>
    {
        public IReadOnlyList<IToken> Data { get; }

        public ArrayToken(IReadOnlyList<IToken> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var previousPrevious = default(IToken);
            var previous = default(IToken);

            var result = new List<IToken>();
            foreach (var token in data)
            {
                // Roll any "number number R" sequence into an indirect reference
                if (ReferenceEquals(token, OperatorToken.R) && previous is NumericToken generation && previousPrevious is NumericToken objectNumber)
                {
                    // Clear the previous 2 tokens.
                    result.RemoveRange(result.Count - 2, 2);

                    result.Add(new IndirectReferenceToken(new IndirectReference(objectNumber.Long, generation.Int)));
                }
                else
                {
                    result.Add(token);
                }

                previousPrevious = previous;
                previous = token;
            }

            Data = result;
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
