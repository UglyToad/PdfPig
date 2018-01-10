namespace UglyToad.PdfPig.Fonts.Encodings
{
    using System.Collections.Generic;

    internal class BuiltInEncoding : Encoding
    {
        public override string EncodingName => "built-in (TTF)";

        public BuiltInEncoding(IReadOnlyDictionary<int, string> codeToName)
        {
            foreach (var keyValuePair in codeToName)
            {
                Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }
}