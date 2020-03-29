namespace UglyToad.PdfPig.Writer.Colors
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    internal static class OutputIntentsFactory
    {
        private const string SrgbIec61966OutputCondition = "sRGB IEC61966-2.1";
        private const string RegistryName = "http://www.color.org";

        public static ArrayToken GetOutputIntentsArray(Func<IToken, ObjectToken> objectWriter)
        {
            var rgbColorCondition = new StringToken(SrgbIec61966OutputCondition);

            var profileBytes = ProfileStreamReader.GetSRgb2014();

            var compressedBytes = DataCompresser.CompressBytes(profileBytes);

            var profileStreamDictionary = new Dictionary<NameToken, IToken>
            {
                {NameToken.Length, new NumericToken(compressedBytes.Length)},
                {NameToken.N, new NumericToken(3)},
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
                    {NameToken.DestOutputProfile, new IndirectReferenceToken(written.Number)}
                }), 
            });
        }
    }
}
