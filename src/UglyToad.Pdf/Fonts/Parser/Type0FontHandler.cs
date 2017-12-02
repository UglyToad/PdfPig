namespace UglyToad.Pdf.Fonts.Parser
{
    using Cmap;
    using ContentStream;
    using Cos;
    using Filters;
    using IO;
    using Pdf.Parser;
    using Util.JetBrains.Annotations;

    internal class Type0FontHandler : IFontHandler
    {
        public IFont Generate(ContentStreamDictionary dictionary, ParsingArguments arguments)
        {
            var dynamicParser = arguments.Get<DynamicParser>();

            var baseFont = dictionary.GetName(CosName.BASE_FONT);

            if (TryGetFirstDescendant(dictionary, out var descendantObject))
            {
                var parsed = dynamicParser.Parse(arguments, descendantObject, false);
            }

            CMap toUnicodeCMap = null;
            if (dictionary.ContainsKey(CosName.TO_UNICODE))
            {
                var toUnicodeValue = dictionary[CosName.TO_UNICODE];

                var toUnicode = dynamicParser.Parse(arguments, toUnicodeValue as CosObject, false) as RawCosStream;

                var decodedUnicodeCMap = toUnicode?.Decode(arguments.Container.Get<IFilterProvider>());

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = arguments.Container.Get<CMapParser>()
                        .Parse(new ByteArrayInputBytes(decodedUnicodeCMap), arguments.IsLenientParsing);
                }
            }

            var font = new CompositeFont
            {
                SubType = CosName.TYPE0,
                ToUnicode = toUnicodeCMap,
                BaseFont = baseFont
            };

            return font;
        }


        [CanBeNull]
        private bool TryGetFirstDescendant(ContentStreamDictionary dictionary, out CosObject descendant)
        {
            descendant = null;

            if (!dictionary.TryGetValue(CosName.DESCENDANT_FONTS, out var value))
            {
                return false;
            }

            if (value is CosObject obj)
            {
                descendant = obj;
                return true;
            }

            if (value is COSArray array && array.Count > 0 && array.get(0) is CosObject objArr)
            {
                descendant = objArr;
                return true;
            }

            return false;
        }
    }

    internal interface IFontHandler
    {
        IFont Generate(ContentStreamDictionary dictionary, ParsingArguments parsingArguments);
    }
}
