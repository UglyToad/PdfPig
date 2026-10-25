namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using System.Threading;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Operations;
    using PdfPig.Logging;
    using PdfPig.Outline.Destinations;
    using PdfPig.Parser;
    using PdfPig.Tests.Tokens;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.PdfFonts;

    public class BasePageFactoryCancellationTests
    {
        private static readonly ParsingOptions Options = new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() };

        [Fact]
        public void Create_CancelledWhileDecodingContentsArray_StopsBeforeTheNextStream()
        {
            var scanner = new TestPdfTokenScanner();
            var first = AddContentStream(scanner, 10);
            var second = AddContentStream(scanner, 11);

            var page = CreatePageDictionary(new ArrayToken([first, second]));

            using var cts = new CancellationTokenSource();
            var filterProvider = new CountingFilterProvider { OnDecode = cts.Cancel };
            var factory = CreateFactory(scanner, filterProvider);

            Assert.Throws<OperationCanceledException>(() => factory.Create(1, page, new PageTreeMembers(), null!, cts.Token));

            Assert.Equal(1, filterProvider.DecodeCount);
            Assert.False(factory.ProcessPageCalled);
        }

        [Fact]
        public void Create_CancelledBeforeSingleContentStream_DoesNotDecodeIt()
        {
            var scanner = new TestPdfTokenScanner();
            var contents = AddContentStream(scanner, 10);

            // Loading the page's resources is where a caller's cancel is most likely to land before decoding starts.
            var page = CreatePageDictionary(contents);

            using var cts = new CancellationTokenSource();
            var filterProvider = new CountingFilterProvider();
            var factory = CreateFactory(scanner, filterProvider, new CancellingResourceStore(scanner, filterProvider, cts));

            Assert.Throws<OperationCanceledException>(() => factory.Create(1, page, new PageTreeMembers(), null!, cts.Token));

            Assert.Equal(0, filterProvider.DecodeCount);
            Assert.False(factory.ProcessPageCalled);
        }

        [Fact]
        public void Create_NullDictionaryWithCancelledToken_ThrowsArgumentNullException()
        {
            var factory = CreateFactory(new TestPdfTokenScanner(), new CountingFilterProvider());

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<ArgumentNullException>(() => factory.Create(1, null!, new PageTreeMembers(), null!, cts.Token));
        }

        private static IndirectReferenceToken AddContentStream(TestPdfTokenScanner scanner, long objectNumber)
        {
            var reference = new IndirectReference(objectNumber, 0);
            var stream = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>()),
                OtherEncodings.StringAsLatin1Bytes("q Q"));
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, stream);
            return new IndirectReferenceToken(reference);
        }

        private static DictionaryToken CreatePageDictionary(IToken contents)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Page },
                { NameToken.MediaBox, new ArrayToken([new NumericToken(0), new NumericToken(0), new NumericToken(612), new NumericToken(792)]) },
                { NameToken.Resources, new DictionaryToken(new Dictionary<NameToken, IToken>()) },
                { NameToken.Contents, contents }
            });
        }

        private static RecordingPageFactory CreateFactory(
            TestPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider,
            IResourceStore? resourceStore = null)
        {
            resourceStore ??= new ResourceStore(scanner, new NoOpFontFactory(), filterProvider, null, Options);

            return new RecordingPageFactory(
                scanner,
                resourceStore,
                filterProvider,
                new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256)),
                Options);
        }

        private sealed class RecordingPageFactory : BasePageFactory<object>
        {
            public bool ProcessPageCalled { get; private set; }

            public RecordingPageFactory(
                IPdfTokenScanner pdfScanner,
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ParsingOptions parsingOptions)
                : base(pdfScanner, resourceStore, filterProvider, pageContentParser, parsingOptions)
            {
            }

            protected override object ProcessPage(
                int pageNumber,
                DictionaryToken dictionary,
                NamedDestinations namedDestinations,
                MediaBox mediaBox,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                TransformationMatrix initialMatrix,
                IReadOnlyList<IGraphicsStateOperation> operations,
                CancellationToken token)
            {
                ProcessPageCalled = true;
                return new object();
            }
        }

        /// <summary>
        /// A resource store that cancels the token when the page's resources are loaded.
        /// </summary>
        private sealed class CancellingResourceStore : IResourceStore
        {
            private readonly ResourceStore inner;
            private readonly CancellationTokenSource source;

            public CancellingResourceStore(IPdfTokenScanner scanner, ILookupFilterProvider filterProvider, CancellationTokenSource source)
            {
                inner = new ResourceStore(scanner, new NoOpFontFactory(), filterProvider, null, Options);
                this.source = source;
            }

            public void LoadResourceDictionary(DictionaryToken resourceDictionary)
            {
                inner.LoadResourceDictionary(resourceDictionary);
                source.Cancel();
            }

            public void UnloadResourceDictionary() => inner.UnloadResourceDictionary();

            public IFont? GetFont(NameToken name) => inner.GetFont(name);

            public bool TryGetXObject(NameToken name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out StreamToken? stream) =>
                inner.TryGetXObject(name, out stream);

            public bool TryGetXObjectReference(NameToken name, out IndirectReference reference) =>
                inner.TryGetXObjectReference(name, out reference);

            public DictionaryToken? GetExtendedGraphicsStateDictionary(NameToken name) => inner.GetExtendedGraphicsStateDictionary(name);

            public IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken) => inner.GetFontDirectly(fontReferenceToken);

            public bool TryGetNamedColorSpace(NameToken name, out ResourceColorSpace namedColorSpace) =>
                inner.TryGetNamedColorSpace(name, out namedColorSpace);

            public ColorSpaceDetails GetColorSpaceDetails(NameToken? name, DictionaryToken? dictionary) =>
                inner.GetColorSpaceDetails(name, dictionary);

            public ColorSpaceDetails GetDeviceColorSpaceDetails(ColorSpace deviceColorSpace) =>
                inner.GetDeviceColorSpaceDetails(deviceColorSpace);

            public DictionaryToken? GetMarkedContentPropertiesDictionary(NameToken name) => inner.GetMarkedContentPropertiesDictionary(name);

            public IReadOnlyDictionary<NameToken, PatternColor> GetPatterns() => inner.GetPatterns();

            public Shading GetShading(NameToken name) => inner.GetShading(name);

            public IIccProfileService? IccProfileService => inner.IccProfileService;

            public ILog Logger => inner.Logger;

            public IReadOnlyList<OutputIntent> DocumentOutputIntents => inner.DocumentOutputIntents;

            public IReadOnlyList<OutputIntent> GetPageOutputIntents(DictionaryToken? pageDictionary) => inner.GetPageOutputIntents(pageDictionary);

            public IIccProfile? GetPageOutputIntentProfile(DictionaryToken? pageDictionary) =>
                inner.GetPageOutputIntentProfile(pageDictionary);
        }
    }
}
