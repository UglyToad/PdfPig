namespace UglyToad.Pdf.Fonts.Parser
{
    using System;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;

    public class CharacterIdentifierFontParser
    {
        public CharacterIdentifierFont Parse(ContentStreamDictionary dictionary, bool isLenientParsing)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var isFont = dictionary.IsType(CosName.FONT);

            if (!isFont && !isLenientParsing)
            {
                throw new InvalidOperationException("The font dictionary was not of type 'Font': " + dictionary);
            }

            var systemInfo = new CharacterIdentifierSystemInfo(null, null, 0);

            var builder = new CharacterIdentifierFontBuilder(dictionary.GetName(CosName.SUBTYPE), 
                dictionary.GetName(CosName.BASE_FONT), systemInfo, dictionary.GetObjectKey(CosName.FONT_DESC));

            return builder.Build();
        }
    }
}
