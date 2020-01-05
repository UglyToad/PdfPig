namespace UglyToad.PdfPig.Fonts.Encodings
{
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// An encoding built in to a TrueType font.
    /// </summary>
    public class BuiltInEncoding : Encoding
    {
        /// <inheritdoc />
        public override string EncodingName => "built-in (TTF)";

        /// <summary>
        /// Create a new <see cref="BuiltInEncoding"/>.
        /// </summary>
        public BuiltInEncoding(IReadOnlyDictionary<int, string> codeToName)
        {
            foreach (var keyValuePair in codeToName)
            {
                Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }
}