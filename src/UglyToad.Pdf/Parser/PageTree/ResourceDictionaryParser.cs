namespace UglyToad.Pdf.Parser.PageTree
{
    using System.Collections.Generic;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;

    internal class ResourceDictionaryParser
    {
        public ResourceDictionary Parse(ContentStreamDictionary dictionary, ParsingArguments arguments)
        {
            var fontDictionary = dictionary.GetDictionaryOrDefault(CosName.FONT);

            var fontMap = new Dictionary<CosName, CosObjectKey>();

            if (fontDictionary != null)
            {
                foreach (var entry in fontDictionary)
                {
                    fontMap[entry.Key] = new CosObjectKey(entry.Value as CosObject);
                }
            }

            var result = new ResourceDictionary();

            result.SetFonts(fontMap);

            return result;
        }
    }
}