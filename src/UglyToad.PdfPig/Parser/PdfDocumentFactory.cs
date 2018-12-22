namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.IO;
    using Content;
    using CrossReference;
    using FileStructure;
    using Filters;
    using Fonts;
    using Fonts.CompactFontFormat;
    using Fonts.CompactFontFormat.Dictionaries;
    using Fonts.Parser;
    using Fonts.Parser.Handlers;
    using Fonts.Parser.Parts;
    using Fonts.SystemFonts;
    using Fonts.TrueType.Parser;
    using Fonts.Type1.Parser;
    using Graphics;
    using IO;
    using Logging;
    using Parts;
    using Parts.CrossReference;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using XObjects;

    internal static class PdfDocumentFactory
    {
        public static PdfDocument Open(byte[] fileBytes, ParsingOptions options = null)
        {
            var inputBytes = new ByteArrayInputBytes(fileBytes);

            return Open(inputBytes, options);
        }

        public static PdfDocument Open(string filename, ParsingOptions options = null)
        {
            if (!File.Exists(filename))
            {
                throw new InvalidOperationException("No file exists at: " + filename);
            }

            return Open(File.ReadAllBytes(filename), options);
        }

        internal static PdfDocument Open(Stream stream, ParsingOptions options)
        {
            var streamInput = new StreamInputBytes(stream, false);

            return Open(streamInput, options);
        }

        private static PdfDocument Open(IInputBytes inputBytes, ParsingOptions options = null)
        {
            var container = Bootstrapper.GenerateContainer(options?.Logger);

            var isLenientParsing = options?.UseLenientParsing ?? true;
            
            var tokenScanner = new CoreTokenScanner(inputBytes);

            var document = OpenDocument(inputBytes, tokenScanner, container, isLenientParsing);

            return document;
        }

        private static PdfDocument OpenDocument(IInputBytes inputBytes, ISeekableTokenScanner scanner, IContainer container, bool isLenientParsing)
        {
            var log = container.Get<ILog>();
            var filterProvider = container.Get<IFilterProvider>();
            var catalogFactory = new CatalogFactory();
            var cMapCache = new CMapCache(new CMapParser());

            CrossReferenceTable crossReferenceTable = null;

            var bruteForceSearcher = new BruteForceSearcher(inputBytes);
            var xrefValidator = new XrefOffsetValidator(log);
            var objectChecker = new XrefCosOffsetChecker(log, bruteForceSearcher);

            // We're ok with this since our intent is to lazily load the cross reference table.
            // ReSharper disable once AccessToModifiedClosure
            var locationProvider = new ObjectLocationProvider(() => crossReferenceTable, bruteForceSearcher);
            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, filterProvider);


            var crossReferenceStreamParser = new CrossReferenceStreamParser(filterProvider);
            var crossReferenceParser = new CrossReferenceParser(log, xrefValidator, objectChecker, crossReferenceStreamParser, new CrossReferenceTableParser());
            
            var version = container.Get<FileHeaderParser>().Parse(scanner, isLenientParsing);
            
            var crossReferenceOffset = container.Get<FileTrailerParser>().GetFirstCrossReferenceOffset(inputBytes, scanner, isLenientParsing);
            
            // TODO: make this use the scanner.
            var validator = new CrossReferenceOffsetValidator(xrefValidator);

            crossReferenceOffset = validator.Validate(crossReferenceOffset, scanner, inputBytes, isLenientParsing);
            
            crossReferenceTable = crossReferenceParser.Parse(inputBytes, isLenientParsing, crossReferenceOffset, pdfScanner, scanner);
            
            var trueTypeFontParser = new TrueTypeFontParser();
            var fontDescriptorFactory = new FontDescriptorFactory();

            var cidFontFactory = new CidFontFactory(pdfScanner, fontDescriptorFactory, trueTypeFontParser, filterProvider);
            var encodingReader = new EncodingReader(pdfScanner);
            
            var compactFontFormatIndexReader = new CompactFontFormatIndexReader();

            var fontFactory = new FontFactory(log, new Type0FontHandler(cidFontFactory,
                cMapCache, 
                filterProvider, pdfScanner),
                new TrueTypeFontHandler(log, pdfScanner, filterProvider, cMapCache, fontDescriptorFactory, trueTypeFontParser, encodingReader, new SystemFontFinder(new TrueTypeFontParser())),
                new Type1FontHandler(pdfScanner, cMapCache, filterProvider, fontDescriptorFactory, encodingReader, 
                    new Type1FontParser(new Type1EncryptedPortionParser()),
                    new CompactFontFormatParser(new CompactFontFormatIndividualFontParser(compactFontFormatIndexReader, new CompactFontFormatTopLevelDictionaryReader(), 
                        new CompactFontFormatPrivateDictionaryReader()), compactFontFormatIndexReader)),
                new Type3FontHandler(pdfScanner, cMapCache, filterProvider, encodingReader));
            
            var resourceContainer = new ResourceContainer(pdfScanner, fontFactory);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider, new PageContentParser(new ReflectionGraphicsStateOperationFactory()), new XObjectFactory());
            var informationFactory = new DocumentInformationFactory();
            

            var rootDictionary = ParseTrailer(crossReferenceTable, isLenientParsing, pdfScanner);
            
            var information = informationFactory.Create(pdfScanner, crossReferenceTable.Trailer);

            var catalog = catalogFactory.Create(pdfScanner, rootDictionary);

            var caching = new ParsingCachingProviders(bruteForceSearcher, resourceContainer);
            
            return new PdfDocument(log, inputBytes, version, crossReferenceTable, isLenientParsing, caching, pageFactory, catalog, information,
                pdfScanner);
        }

        private static DictionaryToken ParseTrailer(CrossReferenceTable crossReferenceTable, bool isLenientParsing, IPdfTokenScanner pdfTokenScanner)
        {
            if (crossReferenceTable.Trailer.EncryptionToken != null)
            {
                throw new NotSupportedException("Cannot currently parse a document using encryption: " + crossReferenceTable.Trailer.EncryptionToken);
            }
            
            var rootDictionary = DirectObjectFinder.Get<DictionaryToken>(crossReferenceTable.Trailer.Root, pdfTokenScanner);
            
            if (!rootDictionary.ContainsKey(NameToken.Type) && isLenientParsing)
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return rootDictionary;
        }
    }
}
