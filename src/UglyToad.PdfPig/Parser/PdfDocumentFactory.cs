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

            var version = container.Get<FileHeaderParser>().Parse(scanner, isLenientParsing);
            
            var crossReferenceOffset = container.Get<FileTrailerParser>().GetFirstCrossReferenceOffset(inputBytes, scanner, isLenientParsing);

            var pool = new CosObjectPool();

            // TODO: make this use the scanner.
            var validator = new CrossReferenceOffsetValidator(new XrefOffsetValidator(log, reader, container.Get<CosDictionaryParser>(),
                container.Get<CosBaseParser>(), pool));

            crossReferenceOffset = validator.Validate(crossReferenceOffset, isLenientParsing);
            
            var crossReferenceTable = container.Get<CrossReferenceParser>()
                .Parse(reader, isLenientParsing, crossReferenceOffset, pool);

            // container.Get<CrossReferenceParser>().ParseNew(crossReferenceOffset, scanner, isLenientParsing);
            
            var filterProvider = container.Get<IFilterProvider>();
            var bruteForceSearcher = new BruteForceSearcher(reader);
            var pdfObjectParser = new PdfObjectParser(container.Get<ILog>(), container.Get<CosBaseParser>(),
                container.Get<CosStreamParser>(), crossReferenceTable, bruteForceSearcher, pool, container.Get<ObjectStreamParser>());

            var trueTypeFontParser = new TrueTypeFontParser();
            var fontDescriptorFactory = new FontDescriptorFactory();

            var cidFontFactory = new CidFontFactory(fontDescriptorFactory, trueTypeFontParser, pdfObjectParser, filterProvider);
            var encodingReader = new EncodingReader(pdfObjectParser);

            var cMapCache = new CMapCache(new CMapParser());

            var pdfScanner = new PdfTokenScanner(inputBytes, new ObjectLocationProvider(crossReferenceTable, pool, bruteForceSearcher));
            var fontFactory = new FontFactory(log, new Type0FontHandler(cidFontFactory,
                cMapCache, 
                filterProvider,
                pdfObjectParser),
                new TrueTypeFontHandler(pdfObjectParser, filterProvider, cMapCache, fontDescriptorFactory, trueTypeFontParser, encodingReader),
                new Type1FontHandler(pdfObjectParser, cMapCache, filterProvider, fontDescriptorFactory, encodingReader, pdfScanner, new Type1FontParser()),
                new Type3FontHandler(pdfObjectParser, cMapCache, filterProvider, encodingReader));

            var dynamicParser = container.Get<DynamicParser>();
            var resourceContainer = new ResourceContainer(pdfObjectParser, fontFactory);

            var pageFactory = new PageFactory(resourceContainer, pdfObjectParser, filterProvider, new PageContentParser(new ReflectionGraphicsStateOperationFactory()));
            var informationFactory = new DocumentInformationFactory();
            var catalogFactory = new CatalogFactory(pdfScanner);

            var rootDictionary = ParseTrailer(reader, crossReferenceTable, dynamicParser, bruteForceSearcher, pool,
                isLenientParsing, pdfScanner);
            
            var information = informationFactory.Create(pdfObjectParser, crossReferenceTable.Dictionary, reader, isLenientParsing);

            var catalog = catalogFactory.Create(rootDictionary, reader, isLenientParsing);

            var caching = new ParsingCachingProviders(pool, bruteForceSearcher, resourceContainer);

            
            return new PdfDocument(log, reader, version, crossReferenceTable, isLenientParsing, caching, pageFactory, pdfObjectParser, catalog, information,
                pdfScanner);
        }

        private static DictionaryToken ParseTrailer(IRandomAccessRead reader, CrossReferenceTable crossReferenceTable,
            DynamicParser dynamicParser, BruteForceSearcher bruteForceSearcher, CosObjectPool pool, bool isLenientParsing, IPdfObjectScanner pdfObjectScanner)
        {
            if (crossReferenceTable.Dictionary.ContainsKey(CosName.ENCRYPT))
            {
                throw new NotSupportedException("Cannot currently parse a document using encryption: " + crossReferenceTable.Dictionary);
            }

            foreach (var keyValuePair in crossReferenceTable.Dictionary)
            {
                if (keyValuePair.Value is CosObject temporaryObject && !keyValuePair.Key.Equals(CosName.ROOT))
                {
                    // Loads these objects into the object pool for access later.
                    dynamicParser.Parse(reader, temporaryObject, pool, crossReferenceTable, bruteForceSearcher,
                        isLenientParsing, false);
                }
            }
            
            CosObject root = (CosObject)crossReferenceTable.Dictionary.GetItemOrDefault(CosName.ROOT);
            if (root == null)
            {
                throw new InvalidOperationException("Missing root object specification in trailer.");
            }

            var obj = pdfObjectScanner.Get(root.ToIndirectReference());

            if (!(obj.Data is DictionaryToken rootDictionary))
            {
                throw new PdfDocumentFormatException($"Could not find the root dictionary, instead found: {obj.Data}");
            }

            if (!rootDictionary.ContainsKey(NameToken.Type) && isLenientParsing)
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return rootDictionary;
        }
    }
}
