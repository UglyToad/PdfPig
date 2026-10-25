namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Operations;
    using PdfPig.Logging;
    using PdfPig.Parser;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// A page's resources must be unloaded even when reading the page throws, or a later page can resolve names
    /// against them.
    /// </summary>
    public class BasePageFactoryResourceUnloadTests
    {
        private sealed class CountingResourceStore(int failingLoad = 0) : IResourceStore
        {
            public int Loads { get; private set; }

            public int Unloads { get; private set; }

            // A load that throws leaves nothing loaded, as IResourceStore requires, so it is not counted.
            public void LoadResourceDictionary(DictionaryToken resourceDictionary)
            {
                if (Loads + 1 == failingLoad)
                {
                    throw new InvalidOperationException("Loading the resources failed.");
                }

                Loads++;
            }

            public void UnloadResourceDictionary() => Unloads++;

            public IFont? GetFont(NameToken name) => throw new NotSupportedException();
            public bool TryGetXObject(NameToken name, [NotNullWhen(true)] out StreamToken? stream) => throw new NotSupportedException();
            public bool TryGetXObjectReference(NameToken name, out IndirectReference reference) => throw new NotSupportedException();
            public DictionaryToken? GetExtendedGraphicsStateDictionary(NameToken name) => throw new NotSupportedException();
            public IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken) => throw new NotSupportedException();
            public bool TryGetNamedColorSpace(NameToken name, out ResourceColorSpace namedColorSpace) => throw new NotSupportedException();
            public ColorSpaceDetails GetColorSpaceDetails(NameToken? name, DictionaryToken? dictionary) => throw new NotSupportedException();
            public ColorSpaceDetails GetDeviceColorSpaceDetails(ColorSpace deviceColorSpace) => throw new NotSupportedException();
            public DictionaryToken? GetMarkedContentPropertiesDictionary(NameToken name) => throw new NotSupportedException();
            public IReadOnlyDictionary<NameToken, PatternColor> GetPatterns() => throw new NotSupportedException();
            public Shading GetShading(NameToken name) => throw new NotSupportedException();
            public IIccProfileService? IccProfileService => null;
            public ILog Logger => new NoOpLog();
            public IReadOnlyList<OutputIntent> DocumentOutputIntents => Array.Empty<OutputIntent>();
            public IReadOnlyList<OutputIntent> GetPageOutputIntents(DictionaryToken? pageDictionary) => Array.Empty<OutputIntent>();
            public IIccProfile? GetPageOutputIntentProfile(DictionaryToken? pageDictionary) => null;
        }

        private sealed class PageFactory : BasePageFactory<object>
        {
            private readonly bool throwWhileProcessing;

            public PageFactory(IResourceStore resourceStore, bool throwWhileProcessing)
                : base(
                    new TestPdfTokenScanner(),
                    resourceStore,
                    new TestFilterProvider(),
                    new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256)),
                    new ParsingOptions { UseLenientParsing = true })
            {
                this.throwWhileProcessing = throwWhileProcessing;
            }

            protected override object ProcessPage(
                int pageNumber,
                DictionaryToken dictionary,
                Outline.Destinations.NamedDestinations namedDestinations,
                MediaBox mediaBox,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                TransformationMatrix initialMatrix,
                IReadOnlyList<IGraphicsStateOperation> operations)
            {
                if (throwWhileProcessing)
                {
                    throw new InvalidOperationException("Reading the page failed.");
                }

                return new object();
            }
        }

        private static DictionaryToken PageWithResources()
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Page },
                { NameToken.Resources, new DictionaryToken(new Dictionary<NameToken, IToken>()) },
            });
        }

        [Fact]
        public void ResourcesAreUnloadedWhenReadingThePageThrows()
        {
            var store = new CountingResourceStore();
            var factory = new PageFactory(store, throwWhileProcessing: true);

            Assert.Throws<InvalidOperationException>(
                () => factory.Create(1, PageWithResources(), new PageTreeMembers(), null!));

            Assert.Equal(1, store.Loads);
            Assert.Equal(store.Loads, store.Unloads);
        }

        [Fact]
        public void LoadedResourcesAreUnloadedWhenALaterLoadThrows()
        {
            var store = new CountingResourceStore(failingLoad: 2);
            var factory = new PageFactory(store, throwWhileProcessing: false);
            var pageTreeMembers = new PageTreeMembers();
            pageTreeMembers.ParentResources.Enqueue(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Throws<InvalidOperationException>(
                () => factory.Create(1, PageWithResources(), pageTreeMembers, null!));

            Assert.Equal(1, store.Loads);
            Assert.Equal(store.Loads, store.Unloads);
        }

        [Fact]
        public void ResourcesAreUnloadedWhenThePageIsRead()
        {
            var store = new CountingResourceStore();
            var factory = new PageFactory(store, throwWhileProcessing: false);

            factory.Create(1, PageWithResources(), new PageTreeMembers(), null!);

            Assert.Equal(1, store.Loads);
            Assert.Equal(store.Loads, store.Unloads);
        }
    }
}
