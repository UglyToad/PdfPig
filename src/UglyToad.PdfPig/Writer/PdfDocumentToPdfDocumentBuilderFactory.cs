namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using CrossReference;
    using Encryption;
    using Exceptions;
    using Filters;
    using Graphics;
    using Logging;
    using Parser;
    using Parser.FileStructure;
    using Parser.Parts;
    using PdfFonts;
    using PdfFonts.Parser;
    using PdfFonts.Parser.Handlers;
    using PdfFonts.Parser.Parts;
    using PdfPig.Fonts.SystemFonts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class PdfDocumentToPdfDocumentBuilderFactory
    {
        private static readonly ILog Log = new NoOpLog();
        private static readonly IFilterProvider FilterProvider = DefaultFilterProvider.Instance;

        public static PdfDocumentBuilder Convert(IInputBytes inputBytes)
        {
            if (inputBytes == null)
            {
                throw new ArgumentNullException(nameof(inputBytes));
                }

            var coreScanner = new CoreTokenScanner(inputBytes);

            const bool isLenientParsing = false;

            var version = FileHeaderParser.Parse(coreScanner, isLenientParsing, Log);

            var crossReferenceParser = new CrossReferenceParser(Log, new XrefOffsetValidator(Log),
                new Parser.Parts.CrossReference.CrossReferenceStreamParser(FilterProvider));

            CrossReferenceTable crossReference = null;

            // ReSharper disable once AccessToModifiedClosure
            var locationProvider = new ObjectLocationProvider(() => crossReference, inputBytes);

            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, FilterProvider, NoOpEncryptionHandler.Instance);

            var crossReferenceOffset = FileTrailerParser.GetFirstCrossReferenceOffset(inputBytes, coreScanner, isLenientParsing);
            crossReference = crossReferenceParser.Parse(inputBytes, isLenientParsing, crossReferenceOffset, version.OffsetInFile, pdfScanner, coreScanner);

            var (rootReference, rootDictionary) = ParseTrailer(crossReference, isLenientParsing,
                pdfScanner,
                out var encryptionDictionary);

            if (encryptionDictionary != null)
            {
                throw new PdfDocumentEncryptedException("Unable to edit document with password");
            }

            var cidFontFactory = new CidFontFactory(pdfScanner, FilterProvider);
            var encodingReader = new EncodingReader(pdfScanner);

            var type1Handler = new Type1FontHandler(pdfScanner, FilterProvider, encodingReader);

            var fontFactory = new FontFactory(Log, new Type0FontHandler(cidFontFactory,
                    FilterProvider, pdfScanner),
                new TrueTypeFontHandler(Log, pdfScanner, FilterProvider, encodingReader, SystemFontFinder.Instance,
                    type1Handler),
                type1Handler,
                new Type3FontHandler(pdfScanner, FilterProvider, encodingReader));

            var resourceContainer = new ResourceStore(pdfScanner, fontFactory);
            
            var catalog = CatalogFactory.Create(rootReference, rootDictionary, pdfScanner, isLenientParsing);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, FilterProvider,
                new PageContentParser(new ReflectionGraphicsStateOperationFactory()),
                Log);

            var builder = new PdfDocumentBuilder();

            var number = 1;
            foreach (var node in GetPages(catalog.PageTree))
            {
                // First, what resources can we define, fonts, etc.
                // Second, we need to copy resource and dictionary keys we don't understand.
                // Third, we need to re-use the inherited properties where possible to prevent double work.
                var page = Pages.CreateFromPageTreeNode(node, pdfScanner, pageFactory, number++, false);
                var pageBuilder = builder.AddPage(page.Width, page.Height);
                pageBuilder.Advanced.Operations.AddRange(page.Operations);
            }

            return builder;
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

        private static IEnumerable<PageTreeNode> GetPages(PageTreeNode root)
        {
            if (root.IsPage)
            {
                yield return root;
                yield break;
            }

            foreach (var child in root.Children)
            {
                foreach (var node in GetPages(child))
                {
                    yield return node;
                }
            }
        }
    }
}
