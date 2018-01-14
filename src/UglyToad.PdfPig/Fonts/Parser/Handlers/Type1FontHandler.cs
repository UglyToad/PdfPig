namespace UglyToad.PdfPig.Fonts.Parser.Handlers
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
    using PdfPig.Parser;
    using Simple;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class Type1FontHandler : IFontHandler
    {
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly CMapCache cMapCache;
        private readonly IFilterProvider filterProvider;
        private readonly FontDescriptorFactory fontDescriptorFactory;
        private readonly IEncodingReader encodingReader;
        private readonly IPdfObjectScanner scanner;

        public Type1FontHandler(IPdfObjectParser pdfObjectParser, CMapCache cMapCache, IFilterProvider filterProvider, 
            FontDescriptorFactory fontDescriptorFactory, IEncodingReader encodingReader,
            IPdfObjectScanner scanner)
        {
            this.pdfObjectParser = pdfObjectParser;
            this.cMapCache = cMapCache;
            this.filterProvider = filterProvider;
            this.fontDescriptorFactory = fontDescriptorFactory;
            this.encodingReader = encodingReader;
            this.scanner = scanner;
        }

        public IFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var usingStandard14Only = !dictionary.ContainsKey(CosName.FIRST_CHAR) || !dictionary.ContainsKey(CosName.WIDTHS);

            if (usingStandard14Only)
            {
                // TODO: some fonts combine standard 14 font with other metrics.
                if (!dictionary.TryGetName(CosName.BASE_FONT, out var standard14Name))
                {
                    throw new InvalidFontFormatException($"The Type 1 font did not contain a first character entry but also did not reference a standard 14 font: {dictionary}");
                }

                var metrics = Standard14.GetAdobeFontMetrics(standard14Name.Name);

                return new Type1Standard14Font(metrics);
            }

            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);

            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);

            var widths = FontDictionaryAccessHelper.GetWidths(pdfObjectParser, dictionary, reader, isLenientParsing);

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfObjectParser, fontDescriptorFactory, dictionary, reader, isLenientParsing);

            ParseType1Font(descriptor, isLenientParsing);

            var name = FontDictionaryAccessHelper.GetName(pdfObjectParser, dictionary, descriptor, reader, isLenientParsing);
            
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

            Encoding encoding = encodingReader.Read(dictionary, reader, isLenientParsing, descriptor);

            return new Type1Font(name, firstCharacter, lastCharacter, widths, descriptor, encoding, toUnicodeCMap);
        }

        private void ParseType1Font(FontDescriptor descriptor, bool isLenientParsing)
        {
            if (descriptor?.FontFile == null)
            {
                return;
            }

            if (descriptor.FontFile.ObjectKey.ObjectNumber == 0)
            {
                return;
            }
            
            try
            {
                var stream = scanner.Get(descriptor.FontFile.ObjectKey).Data as StreamToken;

                if (stream == null)
                {
                    return;
                }

                var raw = new PdfRawStream(stream);

                var bytes = raw.Decode(filterProvider);

                // TODO: parse
            }
            catch
            {
                if (!isLenientParsing)
                {
                    throw;
                }
            }
        }
    }
}
