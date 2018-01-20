namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.IO;
    using Content;
    using Cos;
    using Exceptions;
    using FileStructure;
    using Filters;
    using Fonts;
    using Fonts.Parser;
    using Fonts.Parser.Handlers;
    using Fonts.Parser.Parts;
    using Fonts.TrueType.Parser;
    using Fonts.Type1.Parser;
    using Graphics;
    using IO;
    using Logging;
    using Parts;
    using Tokenization.Scanner;
    using Tokenization.Tokens;
    using Util;

    internal static class PdfDocumentFactory
    {
        public static PdfDocument Open(byte[] fileBytes, ParsingOptions options = null)
        {
            var container = Bootstrapper.GenerateContainer(options?.Logger);

            var isLenientParsing = options?.UseLenientParsing ?? true;

            var reader = new RandomAccessBuffer(fileBytes);

            var inputBytes = new ByteArrayInputBytes(fileBytes);

            var tokenScanner = new CoreTokenScanner(inputBytes);

            var document = OpenDocument(reader, inputBytes, tokenScanner, container,  isLenientParsing);

            return document;
        }

        public static PdfDocument Open(string filename, ParsingOptions options = null)
        {
            if (!File.Exists(filename))
            {
                throw new InvalidOperationException("No file exists at: " + filename);
            }

            return Open(File.ReadAllBytes(filename), options);
        }

        private static PdfDocument OpenDocument(IRandomAccessRead reader, IInputBytes inputBytes, ISeekableTokenScanner scanner, IContainer container, bool isLenientParsing)
        {
            var log = container.Get<ILog>();
            var filterProvider = container.Get<IFilterProvider>();
            var bruteForceSearcher = new BruteForceSearcher(reader);
            var pool = new CosObjectPool();

            CrossReferenceTable crossReferenceTable = null;

            // We're ok with this since our intent is to lazily load the cross reference table.
            // ReSharper disable once AccessToModifiedClosure
            var locationProvider = new ObjectLocationProvider(() => crossReferenceTable, pool, bruteForceSearcher);
            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, filterProvider);

            var version = container.Get<FileHeaderParser>().Parse(scanner, isLenientParsing);
            
            var crossReferenceOffset = container.Get<FileTrailerParser>().GetFirstCrossReferenceOffset(inputBytes, scanner, isLenientParsing);
            
            // TODO: make this use the scanner.
            var validator = new CrossReferenceOffsetValidator(new XrefOffsetValidator(log, reader, container.Get<CosDictionaryParser>(),
                container.Get<CosBaseParser>(), pool));

            crossReferenceOffset = validator.Validate(crossReferenceOffset, isLenientParsing);
            
            crossReferenceTable = container.Get<CrossReferenceParser>()
                .Parse(reader, isLenientParsing, crossReferenceOffset, pool, pdfScanner, scanner);
            
            var trueTypeFontParser = new TrueTypeFontParser();
            var fontDescriptorFactory = new FontDescriptorFactory();

            var cidFontFactory = new CidFontFactory(pdfScanner, fontDescriptorFactory, trueTypeFontParser, filterProvider);
            var encodingReader = new EncodingReader(pdfScanner);

            var cMapCache = new CMapCache(new CMapParser());

            var fontFactory = new FontFactory(log, new Type0FontHandler(cidFontFactory,
                cMapCache, 
                filterProvider, pdfScanner),
                new TrueTypeFontHandler(pdfScanner, filterProvider, cMapCache, fontDescriptorFactory, trueTypeFontParser, encodingReader),
                new Type1FontHandler(pdfScanner, cMapCache, filterProvider, fontDescriptorFactory, encodingReader, new Type1FontParser()),
                new Type3FontHandler(pdfScanner, cMapCache, filterProvider, encodingReader));
            
            var resourceContainer = new ResourceContainer(pdfScanner, fontFactory);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider, new PageContentParser(new ReflectionGraphicsStateOperationFactory()));
            var informationFactory = new DocumentInformationFactory();
            var catalogFactory = new CatalogFactory(pdfScanner);

            var rootDictionary = ParseTrailer(crossReferenceTable, isLenientParsing, pdfScanner);
            
            var information = informationFactory.Create(pdfScanner, crossReferenceTable.Dictionary);

            var catalog = catalogFactory.Create(rootDictionary, reader, isLenientParsing);

            var caching = new ParsingCachingProviders(pool, bruteForceSearcher, resourceContainer);
            
            return new PdfDocument(log, reader, version, crossReferenceTable, isLenientParsing, caching, pageFactory, catalog, information,
                pdfScanner);
        }

        private static DictionaryToken ParseTrailer(CrossReferenceTable crossReferenceTable, bool isLenientParsing, IPdfTokenScanner pdfTokenScanner)
        {
            if (crossReferenceTable.Dictionary.ContainsKey(NameToken.Encrypt))
            {
                throw new NotSupportedException("Cannot currently parse a document using encryption: " + crossReferenceTable.Dictionary);
            }

            if (!crossReferenceTable.Dictionary.TryGet(NameToken.Root, out var rootToken))
            {
                throw new PdfDocumentFormatException($"Missing root object specification in trailer: {crossReferenceTable.Dictionary}.");
            }

            var rootDictionary = DirectObjectFinder.Get<DictionaryToken>(rootToken, pdfTokenScanner);
            
            if (!rootDictionary.ContainsKey(NameToken.Type) && isLenientParsing)
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return rootDictionary;
        }
    }
}
