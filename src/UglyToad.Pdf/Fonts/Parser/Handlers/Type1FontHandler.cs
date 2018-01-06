namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using System;
    using Cmap;
    using ContentStream;
    using Cos;
    using Encodings;
    using Exceptions;
    using Filters;
    using IO;
    using Parts;
    using Pdf.Parser;
    using Simple;

    internal class Type1FontHandler : IFontHandler
    {
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly CMapCache cMapCache;
        private readonly IFilterProvider filterProvider;
        private readonly FontDescriptorFactory fontDescriptorFactory;

        public Type1FontHandler(IPdfObjectParser pdfObjectParser, CMapCache cMapCache, IFilterProvider filterProvider, FontDescriptorFactory fontDescriptorFactory)
        {
            this.pdfObjectParser = pdfObjectParser;
            this.cMapCache = cMapCache;
            this.filterProvider = filterProvider;
            this.fontDescriptorFactory = fontDescriptorFactory;
        }

        public IFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);

            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);

            var widths = FontDictionaryAccessHelper.GetWidths(pdfObjectParser, dictionary, reader, isLenientParsing);

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfObjectParser, fontDescriptorFactory, dictionary, reader, isLenientParsing);
            
            var name = FontDictionaryAccessHelper.GetName(dictionary, descriptor);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGetItemOfType(CosName.TO_UNICODE, out CosObject toUnicodeObj))
            {
                var toUnicode = pdfObjectParser.Parse(toUnicodeObj.ToIndirectReference(), reader, isLenientParsing) as PdfRawStream;

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }

            Encoding encoding = null;
            if (dictionary.TryGetValue(CosName.ENCODING, out var encodingBase))
            {
                // Symbolic fonts default to standard encoding.
                if (descriptor.Flags.HasFlag(FontFlags.Symbolic))
                {
                    encoding = StandardEncoding.Instance;
                }

                if (encodingBase is CosName encodingName)
                {
                    if (!Encoding.TryGetNamedEncoding(encodingName, out encoding))
                    {
                        // TODO: PDFBox would not throw here.
                        throw new InvalidFontFormatException($"Unrecognised encoding name: {encodingName}");
                    }
                }
                else if (encodingBase is CosDictionary encodingDictionary)
                {
                    throw new NotImplementedException("No support for reading encoding from dictionary yet.");
                }
                else
                {
                    throw new NotImplementedException("No support for reading encoding from font yet.");
                }
            }

            return new TrueTypeSimpleFont(name, firstCharacter, lastCharacter, widths, descriptor, toUnicodeCMap, encoding);
        }
    }
}
