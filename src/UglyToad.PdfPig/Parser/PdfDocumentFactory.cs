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
    using Exceptions;
    using FileStructure;
    using Filters;
    using Graphics;
    using Logging;
    using Outline;
    using Parts;
    using Parts.CrossReference;
    using PdfFonts;
    using PdfFonts.CompactFontFormat;
    using PdfFonts.CompactFontFormat.Dictionaries;
    using PdfFonts.Parser;
    using PdfFonts.Parser.Handlers;
    using PdfFonts.Parser.Parts;
    using PdfFonts.SystemFonts;
    using PdfFonts.Type1.Parser;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

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

            var document = OpenDocument(inputBytes, tokenScanner, container, isLenientParsing, passwords);

            return document;
        }

        private static PdfDocument OpenDocument(IInputBytes inputBytes, ISeekableTokenScanner scanner, IContainer container, bool isLenientParsing, IReadOnlyList<string> passwords)
        {
            var log = container.Get<ILog>();
            var filterProvider = container.Get<IFilterProvider>();

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
            
            var crossReferenceOffset = container.Get<FileTrailerParser>().GetFirstCrossReferenceOffset(inputBytes, scanner, isLenientParsing);
            
            // TODO: make this use the scanner.
            var validator = new CrossReferenceOffsetValidator(xrefValidator);

            crossReferenceOffset = validator.Validate(crossReferenceOffset, scanner, inputBytes, isLenientParsing);
            
            crossReferenceTable = crossReferenceParser.Parse(inputBytes, isLenientParsing, crossReferenceOffset, pdfScanner, scanner);
            
            var trueTypeFontParser = new TrueTypeFontParser();
            var fontDescriptorFactory = new FontDescriptorFactory();
            var compactFontFormatParser = new CompactFontFormatParser(new CompactFontFormatIndividualFontParser(new CompactFontFormatTopLevelDictionaryReader(), 
                        new CompactFontFormatPrivateDictionaryReader()));
            
            var (rootReference, rootDictionary) = ParseTrailer(crossReferenceTable, isLenientParsing, 
                pdfScanner, 
                out var encryptionDictionary);

            var encryptionHandler = encryptionDictionary != null ?
                (IEncryptionHandler)new EncryptionHandler(encryptionDictionary, crossReferenceTable.Trailer, passwords)
                : NoOpEncryptionHandler.Instance;

            pdfScanner.UpdateEncryptionHandler(encryptionHandler);

            var cidFontFactory = new CidFontFactory(pdfScanner, fontDescriptorFactory, trueTypeFontParser, compactFontFormatParser, filterProvider);
            var encodingReader = new EncodingReader(pdfScanner);

            var fontFactory = new FontFactory(log, new Type0FontHandler(cidFontFactory,
                filterProvider, pdfScanner),
                new TrueTypeFontHandler(log, pdfScanner, filterProvider, fontDescriptorFactory, trueTypeFontParser, encodingReader, new SystemFontFinder(new TrueTypeFontParser())),
                new Type1FontHandler(pdfScanner, filterProvider, fontDescriptorFactory, encodingReader, 
                    new Type1FontParser(new Type1EncryptedPortionParser()), compactFontFormatParser),
                new Type3FontHandler(pdfScanner, filterProvider, encodingReader));
            
            var resourceContainer = new ResourceStore(pdfScanner, fontFactory);
            
            var information = DocumentInformationFactory.Create(pdfScanner, crossReferenceTable.Trailer);

            var catalog = CatalogFactory.Create(rootReference, rootDictionary, pdfScanner, isLenientParsing);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider, 
                new PageContentParser(new ReflectionGraphicsStateOperationFactory()), 
                log);

            var caching = new ParsingCachingProviders(bruteForceSearcher, resourceContainer);

            var acroFormFactory = new AcroFormFactory(pdfScanner, filterProvider);
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
