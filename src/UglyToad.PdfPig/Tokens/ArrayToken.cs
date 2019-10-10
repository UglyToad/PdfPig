namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// An array object is a one-dimensional collection of objects arranged sequentially.
    /// PDF arrays may be heterogeneous; that is, an array's elements may be any combination of numbers, strings,
    /// dictionaries, or any other objects, including other arrays.
    /// </summary>
    public class ArrayToken : IDataToken<IReadOnlyList<IToken>>
    {
        /// <summary>
        /// The tokens contained in this array.
        /// </summary>
        [NotNull]
        public IReadOnlyList<IToken> Data { get; }

        /// <summary>
        /// The number of tokens in this array.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Indexer into <see cref="Data"/> for convenience.
        /// </summary>
        public IToken this[int i] => Data[i];

        /// <summary>
        /// Create a new <see cref="ArrayToken"/>.
        /// </summary>
        /// <param name="data">The tokens contained by this array.</param>
        public ArrayToken([NotNull] IReadOnlyList<IToken> data)
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
            Length = Data.Count;
        }

        /// <inheritdoc />
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
