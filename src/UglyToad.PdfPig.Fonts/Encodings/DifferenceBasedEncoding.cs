namespace UglyToad.PdfPig.Fonts.Encodings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <inheritdoc />
    /// <summary>
    /// Created by combining a base encoding with the differences.
    /// </summary>
    public sealed class DifferenceBasedEncoding : Encoding
    {
        /// <inheritdoc />
        public override string EncodingName { get; } = "Difference Encoding";

        /// <summary>
        /// Create a new <see cref="DifferenceBasedEncoding"/>.
        /// </summary>
        public DifferenceBasedEncoding(Encoding baseEncoding, IReadOnlyList<(int, string)> differences)
        {
            if (baseEncoding == null)
            {
                throw new ArgumentNullException(nameof(baseEncoding));
            }

            if (differences == null)
            {
                throw new ArgumentNullException(nameof(differences));
            }

            EncodingName = "Difference " + baseEncoding.EncodingName;

            foreach (var difference in differences)
            {
                Add(difference.Item1, difference.Item2);
            }

            foreach (var pair in baseEncoding.CodeToNameMap)
            {
                if (differences.All(x => x.Item1 != pair.Key))
                {
                    Add(pair.Key, pair.Value);
                }
            }
        }
    }
}
