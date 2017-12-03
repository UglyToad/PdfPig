namespace UglyToad.Pdf.Parser.PageTree
{
    using System;
    using Content;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using Filters;
    using Fonts;

    internal class PageParser
    {
        public Page Parse(int number, PdfDictionary dictionary, ParsingArguments arguments)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (!dictionary.IsType(CosName.PAGE))
            {
                throw new InvalidOperationException("Expected a Dictionary of Type Page, instead got this: " + dictionary);
            }

            return new Page(number, dictionary, new PageTreeMembers(), arguments);
        }
    }

    internal class FontParser
    {
        public Font Parse(PdfDictionary dictionary, ParsingArguments arguments)
        {
            var type = dictionary.GetName(CosName.SUBTYPE);

            if (CosName.Equals(type, CosName.TYPE0))
            {
                var compositeFont = arguments.Container.Get<CompositeFontParser>()
                    .Parse(dictionary, arguments);
            }
            else
            {
                var simpleFont = arguments.Container.Get<SimpleFontParser>()
                    .Parse(dictionary, arguments);
            }

            return new Font();
        }
    }

    internal class CompositeFontParser
    {
        public CompositeFont Parse(PdfDictionary dictionary, ParsingArguments arguments)
        {
            var descendants = dictionary.GetItemOrDefault(CosName.DESCENDANT_FONTS) as COSArray;

            if (descendants == null)
            {
                throw new InvalidOperationException("DescendantFonts is required for a Type0 composite font. It was not found: " + dictionary);
            }

            if (descendants.Count < 1)
            {
                throw new InvalidOperationException("Descendant fonts should be a single element array. There were no elements: " + dictionary);
            }

            var descendantKey = descendants.get(0) as CosObject;

            var descendant = arguments.Container.Get<DynamicParser>()
                .Parse(arguments, descendantKey, false);

            var toUnicode = dictionary.GetObjectKey(CosName.TO_UNICODE);

            if (toUnicode != null)
            {
                var toUnicodeValue = arguments.Container.Get<DynamicParser>()
                    .Parse(arguments, toUnicode, false);

                if (toUnicodeValue is RawCosStream stream)
                {
                    var decoded = stream.Decode(arguments.Container.Get<IFilterProvider>());

                    // This is described on page 472 of the spec.
                    var str = UglyToad.Pdf.Util.OtherEncodings.BytesAsLatin1String(decoded);
                }
            }

            return new CompositeFont();
        }
    }

    internal class SimpleFontParser
    {
        public SimpleFont Parse(PdfDictionary dictionary, ParsingArguments arguments)
        {
            return new SimpleFont();
        }
    }

    public class SimpleFont
    {

    }

    public class Font
    {

    }
}
