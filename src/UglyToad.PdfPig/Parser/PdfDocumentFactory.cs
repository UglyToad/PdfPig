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

            var clipPaths = options?.ClipPaths ?? false;

            var suppressDuplicateOverlappingText = options?.SuppressDuplicateOverlappingText ?? true;

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

            var document = OpenDocument(inputBytes, tokenScanner, options?.Logger ?? new NoOpLog(), isLenientParsing, passwords, clipPaths, suppressDuplicateOverlappingText);

            return document;
        }

        private static PdfDocument OpenDocument(IInputBytes inputBytes, ISeekableTokenScanner scanner, ILog log, bool isLenientParsing,
            IReadOnlyList<string> passwords, bool clipPaths, bool suppressDuplicateOverlappingText)
        {
            var filterProvider = DefaultFilterProvider.Instance;

            CrossReferenceTable crossReferenceTable = null;

            var xrefValidator = new XrefOffsetValidator(log);

            // We're ok with this since our intent is to lazily load the cross reference table.
            // ReSharper disable once AccessToModifiedClosure
            var locationProvider = new ObjectLocationProvider(() => crossReferenceTable, inputBytes);
            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, filterProvider, NoOpEncryptionHandler.Instance);

            var crossReferenceStreamParser = new CrossReferenceStreamParser(filterProvider);
            var crossReferenceParser = new CrossReferenceParser(log, xrefValidator, crossReferenceStreamParser);

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
                new TrueTypeFontHandler(log, pdfScanner, filterProvider, encodingReader, SystemFontFinder.Instance,
                    type1Handler),
                type1Handler,
                new Type3FontHandler(pdfScanner, filterProvider, encodingReader));

            var resourceContainer = new ResourceStore(pdfScanner, fontFactory);

            var information = DocumentInformationFactory.Create(pdfScanner, crossReferenceTable.Trailer);

            var catalog = CatalogFactory.Create(rootReference, rootDictionary, pdfScanner, isLenientParsing);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider,
                new PageContentParser(new ReflectionGraphicsStateOperationFactory()),
                log);

            var caching = new ParsingCachingProviders(resourceContainer);

            var acroFormFactory = new AcroFormFactory(pdfScanner, filterProvider, crossReferenceTable);
            var bookmarksProvider = new BookmarksProvider(log, pdfScanner);

            return new PdfDocument(log, inputBytes, version, crossReferenceTable, caching, pageFactory, catalog, information,
                encryptionDictionary,
                pdfScanner,
                filterProvider,
                acroFormFactory,
                bookmarksProvider,
                clipPaths,
                suppressDuplicateOverlappingText);
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
