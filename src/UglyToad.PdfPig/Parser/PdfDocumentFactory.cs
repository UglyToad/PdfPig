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
            var initialPosition = stream.Position;

            var streamInput = new StreamInputBytes(stream, false);

            try
            {
                return Open(streamInput, options);
            }
            catch (Exception ex)
            {
                if (initialPosition != 0)
                {
                    throw new InvalidOperationException(
                        "Could not parse document due to an error, the input stream was not at position zero when provided to the Open method.",
                        ex);
                }

                throw;
            }
        }

        private static PdfDocument Open(IInputBytes inputBytes, ParsingOptions options = null)
        {
            options ??= new ParsingOptions()
            {
                UseLenientParsing = true,
                ClipPaths = false,
                SkipMissingFonts = false
            };

            var tokenScanner = new CoreTokenScanner(inputBytes, true, useLenientParsing: options.UseLenientParsing);

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

            options.Passwords = passwords;

            var document = OpenDocument(inputBytes, tokenScanner, options);

            return document;
        }

        private static PdfDocument OpenDocument(
            IInputBytes inputBytes,
            ISeekableTokenScanner scanner,
            ParsingOptions parsingOptions)
        {
            var filterProvider = new FilterProviderWithLookup(DefaultFilterProvider.Instance);

            CrossReferenceTable crossReferenceTable = null;

            var xrefValidator = new XrefOffsetValidator(parsingOptions.Logger);

            // We're ok with this since our intent is to lazily load the cross reference table.
            // ReSharper disable once AccessToModifiedClosure
            var locationProvider = new ObjectLocationProvider(() => crossReferenceTable, inputBytes);
            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, filterProvider, NoOpEncryptionHandler.Instance, parsingOptions);

            var crossReferenceStreamParser = new CrossReferenceStreamParser(filterProvider);
            var crossReferenceParser = new CrossReferenceParser(parsingOptions.Logger, xrefValidator, crossReferenceStreamParser);

            var version = FileHeaderParser.Parse(scanner, inputBytes, parsingOptions.UseLenientParsing, parsingOptions.Logger);

            var crossReferenceOffset = FileTrailerParser.GetFirstCrossReferenceOffset(
                inputBytes,
                scanner,
                parsingOptions.UseLenientParsing) + version.OffsetInFile;

            // TODO: make this use the scanner.
            var validator = new CrossReferenceOffsetValidator(xrefValidator);

            crossReferenceOffset = validator.Validate(crossReferenceOffset, scanner, inputBytes, parsingOptions.UseLenientParsing);

            crossReferenceTable = crossReferenceParser.Parse(
                inputBytes,
                parsingOptions.UseLenientParsing,
                crossReferenceOffset,
                version.OffsetInFile,
                pdfScanner,
                scanner);

            var (rootReference, rootDictionary) = ParseTrailer(
                crossReferenceTable,
                parsingOptions.UseLenientParsing,
                pdfScanner,
                out var encryptionDictionary);

            var encryptionHandler = encryptionDictionary != null ?
                (IEncryptionHandler)new EncryptionHandler(
                    encryptionDictionary,
                    crossReferenceTable.Trailer,
                    parsingOptions.Passwords)
                : NoOpEncryptionHandler.Instance;

            pdfScanner.UpdateEncryptionHandler(encryptionHandler);

            var cidFontFactory = new CidFontFactory(
                parsingOptions.Logger,
                pdfScanner,
                filterProvider);

            var encodingReader = new EncodingReader(pdfScanner);

            var type0Handler = new Type0FontHandler(
                cidFontFactory,
                filterProvider,
                pdfScanner,
                parsingOptions.Logger);

            var type1Handler = new Type1FontHandler(pdfScanner, filterProvider, encodingReader);

            var trueTypeHandler = new TrueTypeFontHandler(parsingOptions.Logger,
                pdfScanner,
                filterProvider,
                encodingReader,
                SystemFontFinder.Instance,
                type1Handler);

            var fontFactory = new FontFactory(
                parsingOptions.Logger,
                type0Handler,
                trueTypeHandler,
                type1Handler,
                new Type3FontHandler(pdfScanner, filterProvider, encodingReader));

            var resourceContainer = new ResourceStore(pdfScanner, fontFactory, filterProvider, parsingOptions);

            var information = DocumentInformationFactory.Create(
                pdfScanner,
                crossReferenceTable.Trailer,
                parsingOptions.UseLenientParsing);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider,
                new PageContentParser(new ReflectionGraphicsStateOperationFactory(), parsingOptions.UseLenientParsing), parsingOptions);

            var catalog = CatalogFactory.Create(
                rootReference,
                rootDictionary,
                pdfScanner,
                pageFactory,
                parsingOptions.Logger,
                parsingOptions.UseLenientParsing);

            var acroFormFactory = new AcroFormFactory(pdfScanner, filterProvider, crossReferenceTable);
            var bookmarksProvider = new BookmarksProvider(parsingOptions.Logger, pdfScanner);

            return new PdfDocument(
                inputBytes,
                version,
                crossReferenceTable,
                catalog,
                information,
                encryptionDictionary,
                pdfScanner,
                filterProvider,
                acroFormFactory,
                bookmarksProvider,
                parsingOptions);
        }

        private static (IndirectReference, DictionaryToken) ParseTrailer(CrossReferenceTable crossReferenceTable, bool isLenientParsing, IPdfTokenScanner pdfTokenScanner,
            out EncryptionDictionary encryptionDictionary)
        {
            encryptionDictionary = GetEncryptionDictionary(crossReferenceTable, pdfTokenScanner);

            var rootDictionary = DirectObjectFinder.Get<DictionaryToken>(crossReferenceTable.Trailer.Root, pdfTokenScanner);

            if (!rootDictionary.ContainsKey(NameToken.Type) && isLenientParsing)
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return (crossReferenceTable.Trailer.Root, rootDictionary);
        }

        private static EncryptionDictionary GetEncryptionDictionary(CrossReferenceTable crossReferenceTable, IPdfTokenScanner pdfTokenScanner)
        {
            if (crossReferenceTable.Trailer.EncryptionToken == null)
            {
                return null;
            }

            if (!DirectObjectFinder.TryGet(crossReferenceTable.Trailer.EncryptionToken, pdfTokenScanner, out DictionaryToken encryptionDictionaryToken))
            {
                if (DirectObjectFinder.TryGet(crossReferenceTable.Trailer.EncryptionToken, pdfTokenScanner, out NullToken _))
                {
                    return null;
                }

                throw new PdfDocumentFormatException($"Unrecognized encryption token in trailer: {crossReferenceTable.Trailer.EncryptionToken}.");
            }

            var result = EncryptionDictionaryFactory.Read(encryptionDictionaryToken, pdfTokenScanner);

            return result;
        }
    }
}
