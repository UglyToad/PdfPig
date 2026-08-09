namespace UglyToad.PdfPig.Writer.Colors
{
    using System;
    using System.Collections.Generic;
    using Graphics.Colors.Icc;
    using Tokens;

    internal static class OutputIntentsFactory
    {
        private const string SrgbIec61966OutputCondition = "sRGB IEC61966-2.1";
        private const string RegistryName = "http://www.color.org";

        public static ArrayToken GetOutputIntentsArray(Func<IToken, IndirectReferenceToken> objectWriter)
        {
            var rgbColorCondition = new StringToken(SrgbIec61966OutputCondition);

            var profileBytes = ProfileStreamReader.GetSRgb2014();

            var compressedBytes = DataCompressor.CompressBytes(profileBytes);

            // /N shall match the profile (8.6.5.5), so read it from the profile rather than restating it.
            // Three is right for the sRGB profile shipped today, but a hardcoded three is a trap the moment
            // that profile is swapped or made configurable - and a mismatch is exactly what readers have to
            // write code to work around (see PDFBOX-4801).
            if (!IccProfileHeader.TryGetNumberOfComponents(profileBytes, out int numberOfComponents))
            {
                throw new InvalidOperationException(
                    "Could not determine the number of colour components of the embedded sRGB ICC profile.");
            }

            var profileStreamDictionary = new Dictionary<NameToken, IToken>
            {
                {NameToken.Length, new NumericToken(compressedBytes.Length)},
                {NameToken.N, new NumericToken(numberOfComponents)},
                {NameToken.Filter, NameToken.FlateDecode}
            };

            var stream = new StreamToken(new DictionaryToken(profileStreamDictionary), compressedBytes);

            var written = objectWriter(stream);

            return new ArrayToken(new IToken[]
            {
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    {NameToken.Type, NameToken.OutputIntent },
                    {NameToken.S, NameToken.GtsPdfa1},
                    {NameToken.OutputCondition, rgbColorCondition},
                    {NameToken.OutputConditionIdentifier, rgbColorCondition},
                    {NameToken.RegistryName, new StringToken(RegistryName)},
                    {NameToken.Info, rgbColorCondition},
                    {NameToken.DestOutputProfile, written}
                }), 
            });
        }
    }
}
