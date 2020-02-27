namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AcroForms;
    using Content;
    using Core;
    using CrossReference;
    using Encryption;
    using FileStructure;
    using Filters;
    using Fonts.SystemFonts;
    using Graphics;
    using Logging;
    using Outline;
    using Parts;
    using Parts.CrossReference;
    using PdfFonts;
    using PdfFonts.Parser;
    using PdfFonts.Parser.Handlers;
    using PdfFonts.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

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
            var isLenientParsing = options?.UseLenientParsing ?? true;
            
            var tokenScanner = new CoreTokenScanner(inputBytes);

            var passwords = new List<string>();

            if (options?.Password != null)
            {
                passwords.Add(options.Password);
            }

            if (options?.Passwords != null)
            {
                passwords.AddRange(options.Passwords.Where(x => x != null));
            }

            if (!passwords.Contains(string.Empty))
            {
                passwords.Add(string.Empty);
            }

            var document = OpenDocument(inputBytes, tokenScanner, options?.Logger ?? new NoOpLog(), isLenientParsing, passwords);

            return document;
        }

        private static PdfDocument OpenDocument(IInputBytes inputBytes, ISeekableTokenScanner scanner, ILog log, bool isLenientParsing, IReadOnlyList<string> passwords)
        {
            var filterProvider = new MemoryFilterProvider(new DecodeParameterResolver(log), new PngPredictor(), log);

            CrossReferenceTable crossReferenceTable = null;

            var bruteForceSearcher = new BruteForceSearcher(inputBytes);
            var xrefValidator = new XrefOffsetValidator(log);
            var objectChecker = new XrefCosOffsetChecker(log, bruteForceSearcher);

            // We're ok with this since our intent is to lazily load the cross reference table.
            // ReSharper disable once AccessToModifiedClosure
            var locationProvider = new ObjectLocationProvider(() => crossReferenceTable, bruteForceSearcher);
            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, filterProvider, NoOpEncryptionHandler.Instance);

            var crossReferenceStreamParser = new CrossReferenceStreamParser(filterProvider);
            var crossReferenceParser = new CrossReferenceParser(log, xrefValidator, objectChecker, crossReferenceStreamParser, new CrossReferenceTableParser());
            
            var version = FileHeaderParser.Parse(scanner, isLenientParsing, log);
            
            var crossReferenceOffset = FileTrailerParser.GetFirstCrossReferenceOffset(inputBytes, scanner,
                isLenientParsing) + version.OffsetInFile;
            
            // TODO: make this use the scanner.
            var validator = new CrossReferenceOffsetValidator(xrefValidator);

            crossReferenceOffset = validator.Validate(crossReferenceOffset, scanner, inputBytes, isLenientParsing);
            
            crossReferenceTable = crossReferenceParser.Parse(inputBytes, isLenientParsing, 
                crossReferenceOffset,
                version.OffsetInFile,
                pdfScanner, 
                scanner);
            
            var (rootReference, rootDictionary) = ParseTrailer(crossReferenceTable, isLenientParsing, 
                pdfScanner, 
                out var encryptionDictionary);

            var encryptionHandler = encryptionDictionary != null ?
                (IEncryptionHandler)new EncryptionHandler(encryptionDictionary, crossReferenceTable.Trailer, passwords)
                : NoOpEncryptionHandler.Instance;

            pdfScanner.UpdateEncryptionHandler(encryptionHandler);

            var cidFontFactory = new CidFontFactory(pdfScanner, filterProvider);
            var encodingReader = new EncodingReader(pdfScanner);

            var type1Handler = new Type1FontHandler(pdfScanner, filterProvider, encodingReader);

            var fontFactory = new FontFactory(log, new Type0FontHandler(cidFontFactory,
                filterProvider, pdfScanner),
                new TrueTypeFontHandler(log, pdfScanner, filterProvider, encodingReader, new SystemFontFinder(),
                    type1Handler),
                type1Handler,
                new Type3FontHandler(pdfScanner, filterProvider, encodingReader));
            
            var resourceContainer = new ResourceStore(pdfScanner, fontFactory);
            
            var information = DocumentInformationFactory.Create(pdfScanner, crossReferenceTable.Trailer);

            var catalog = CatalogFactory.Create(rootReference, rootDictionary, pdfScanner, isLenientParsing);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider, 
                new PageContentParser(new ReflectionGraphicsStateOperationFactory()), 
                log);

            var caching = new ParsingCachingProviders(bruteForceSearcher, resourceContainer);

            var acroFormFactory = new AcroFormFactory(pdfScanner, filterProvider, crossReferenceTable);
            var bookmarksProvider = new BookmarksProvider(log, pdfScanner, isLenientParsing);
            
            return new PdfDocument(log, inputBytes, version, crossReferenceTable, isLenientParsing, caching, pageFactory, catalog, information,
                encryptionDictionary,
                pdfScanner,
                filterProvider,
                acroFormFactory,
                bookmarksProvider);
        }

        private static (IndirectReference, DictionaryToken) ParseTrailer(CrossReferenceTable crossReferenceTable, bool isLenientParsing, IPdfTokenScanner pdfTokenScanner,
            out EncryptionDictionary encryptionDictionary)
        {
            encryptionDictionary = null;

            if (crossReferenceTable.Trailer.EncryptionToken != null)
            {
                if (!DirectObjectFinder.TryGet(crossReferenceTable.Trailer.EncryptionToken, pdfTokenScanner, out DictionaryToken encryptionDictionaryToken))
                {
                    throw new PdfDocumentFormatException($"Unrecognized encryption token in trailer: {crossReferenceTable.Trailer.EncryptionToken}.");
                }

                encryptionDictionary = EncryptionDictionaryFactory.Read(encryptionDictionaryToken, pdfTokenScanner);
            }
            
            var rootDictionary = DirectObjectFinder.Get<DictionaryToken>(crossReferenceTable.Trailer.Root, pdfTokenScanner);
            
            if (!rootDictionary.ContainsKey(NameToken.Type) && isLenientParsing)
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return (crossReferenceTable.Trailer.Root, rootDictionary);
        }
    }
}
