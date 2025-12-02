namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
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
    using Outline;
    using Parts;
    using PdfFonts;
    using PdfFonts.Parser;
    using PdfFonts.Parser.Handlers;
    using PdfFonts.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.PdfFonts.Cmap;

    internal static class PdfDocumentFactory
    {
        public static PdfDocument Open(ReadOnlyMemory<byte> memory, ParsingOptions? options = null)
        {
            var inputBytes = new MemoryInputBytes(memory);

            return Open(inputBytes, options);
        }

        public static PdfDocument Open(string filename, ParsingOptions? options = null)
        {
            if (!File.Exists(filename))
            {
                throw new InvalidOperationException("No file exists at: " + filename);
            }

            return Open(File.ReadAllBytes(filename), options);
        }

        internal static PdfDocument Open(Stream stream, ParsingOptions? options)
        {
            StreamInputBytes streamInput;
            long initialPosition;
            
            if (stream is { CanRead: true, CanSeek: false })
            {
                // We need the stream to be seekable
                var ms = new MemoryStream();
                stream.CopyTo(ms); // Copy the non seekable stream in memory (seekable)
                ms.Position = 0;
                streamInput = new StreamInputBytes(ms, true); // The created memory stream will be disposed on document dispose
                initialPosition = ms.Position;
            }
            else
            {
                streamInput = new StreamInputBytes(stream, false);
                initialPosition = stream.Position;
            }

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

        private static PdfDocument Open(IInputBytes inputBytes, ParsingOptions? options = null)
        {
            options ??= new ParsingOptions()
            {
                UseLenientParsing = true,
                ClipPaths = false,
                SkipMissingFonts = false
            };

            var stackDepthGuard = new StackDepthGuard(options.MaxStackDepth);

            var tokenScanner = new CoreTokenScanner(inputBytes, true, stackDepthGuard, useLenientParsing: options.UseLenientParsing);

            var passwords = new List<string>();

            if (options.Password != null)
            {
                passwords.Add(options.Password);
            }

            if (options.Passwords != null)
            {
                passwords.AddRange(options.Passwords.Where(x => x != null));
            }

            if (!passwords.Contains(string.Empty))
            {
                passwords.Add(string.Empty);
            }

            options.Passwords = passwords;

            var document = OpenDocument(inputBytes, tokenScanner, options, stackDepthGuard);

            return document;
        }

        private static PdfDocument OpenDocument(
            IInputBytes inputBytes,
            ISeekableTokenScanner scanner,
            ParsingOptions parsingOptions,
            StackDepthGuard stackDepthGuard)
        {
            var filterProvider = new FilterProviderWithLookup(parsingOptions.FilterProvider ?? DefaultFilterProvider.Instance);

            var version = FileHeaderParser.Parse(scanner, inputBytes, parsingOptions.UseLenientParsing, parsingOptions.Logger);

            var fileHeaderOffset = new FileHeaderOffset((int)version.OffsetInFile);

            var initialParse = FirstPassParser.Parse(
                fileHeaderOffset,
                inputBytes,
                scanner,
                parsingOptions.Logger);

            if (initialParse.Trailer == null)
            {
                throw new PdfDocumentFormatException(
                    "Could not find an xref trailer or stream dictionary in the input file.");
            }

            var trailer = new TrailerDictionary(initialParse.Trailer, parsingOptions.UseLenientParsing);

            var locationProvider = new ObjectLocationProvider(
                initialParse.XrefOffsets,
                initialParse.BruteForceOffsets,
                inputBytes);

            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, filterProvider, NoOpEncryptionHandler.Instance, fileHeaderOffset, parsingOptions, stackDepthGuard);

            var (rootReference, rootDictionary) = ParseTrailer(
                trailer,
                parsingOptions.UseLenientParsing,
                pdfScanner,
                out var encryptionDictionary);

            var encryptionHandler = encryptionDictionary != null ?
                (IEncryptionHandler)new EncryptionHandler(
                    encryptionDictionary,
                    trailer,
                    parsingOptions.Passwords)
                : NoOpEncryptionHandler.Instance;

            pdfScanner.UpdateEncryptionHandler(encryptionHandler);

            var cidFontFactory = new CidFontFactory(
                parsingOptions.Logger,
                pdfScanner,
                filterProvider);

            var encodingReader = new EncodingReader(pdfScanner);

            var cmapCache = new CMapLocalCache(filterProvider, pdfScanner);

            var type0Handler = new Type0FontHandler(
                cidFontFactory,
                pdfScanner,
                cmapCache,
                parsingOptions);

            var type1Handler = new Type1FontHandler(
                pdfScanner,
                filterProvider,
                encodingReader,
                cmapCache,
                stackDepthGuard,
                parsingOptions.UseLenientParsing);

            var trueTypeHandler = new TrueTypeFontHandler(
                parsingOptions.Logger,
                pdfScanner,
                filterProvider,
                encodingReader,
                cmapCache,
                SystemFontFinder.Instance,
                type1Handler);

            var fontFactory = new FontFactory(
                parsingOptions.Logger,
                type0Handler,
                trueTypeHandler,
                type1Handler,
                new Type3FontHandler(pdfScanner, encodingReader, cmapCache));

            var resourceContainer = new ResourceStore(pdfScanner, fontFactory, filterProvider, parsingOptions);

            var information = DocumentInformationFactory.Create(
                pdfScanner,
                trailer,
                parsingOptions.UseLenientParsing);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider,
                new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, stackDepthGuard, parsingOptions.UseLenientParsing), parsingOptions);

            var catalog = CatalogFactory.Create(
                rootReference,
                rootDictionary,
                pdfScanner,
                pageFactory,
                parsingOptions.Logger,
                parsingOptions.UseLenientParsing);

            var acroFormFactory = new AcroFormFactory(pdfScanner,
                filterProvider,
                initialParse.BruteForceOffsets ?? initialParse.XrefOffsets);

            var bookmarksProvider = new BookmarksProvider(parsingOptions.Logger, pdfScanner);

            return new PdfDocument(
                inputBytes,
                version,
                catalog,
                information,
                encryptionDictionary,
                pdfScanner,
                filterProvider,
                acroFormFactory,
                bookmarksProvider,
                parsingOptions);
        }

        private static (IndirectReference, DictionaryToken) ParseTrailer(
            TrailerDictionary trailer,
            bool isLenientParsing,
            IPdfTokenScanner pdfTokenScanner,
            [NotNullWhen(true)] out EncryptionDictionary? encryptionDictionary)
        {
            encryptionDictionary = GetEncryptionDictionary(trailer, pdfTokenScanner);

            var rootDictionary = DirectObjectFinder.Get<DictionaryToken>(trailer.Root, pdfTokenScanner)!;

            if (rootDictionary is null)
            {
                throw new PdfDocumentFormatException($"The root object in the trailer did not resolve to a readable dictionary.");
            }

            if (!rootDictionary.ContainsKey(NameToken.Type) && isLenientParsing)
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return (trailer.Root, rootDictionary);
        }

        private static EncryptionDictionary? GetEncryptionDictionary(TrailerDictionary trailer, IPdfTokenScanner pdfTokenScanner)
        {
            if (trailer.EncryptionToken is null)
            {
                return null;
            }

            if (!DirectObjectFinder.TryGet(trailer.EncryptionToken, pdfTokenScanner, out DictionaryToken? encryptionDictionaryToken))
            {
                if (DirectObjectFinder.TryGet(trailer.EncryptionToken, pdfTokenScanner, out NullToken? _))
                {
                    return null;
                }

                throw new PdfDocumentFormatException($"Unrecognized encryption token in trailer: {trailer.EncryptionToken}.");
            }

            var result = EncryptionDictionaryFactory.Read(encryptionDictionaryToken, pdfTokenScanner);

            return result;
        }
    }
}
