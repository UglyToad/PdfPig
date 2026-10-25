namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// A resource dictionary whose load throws partway must not stay on the resource stack: the caller never counts a
    /// load that threw, so never unloads it.
    /// </summary>
    public class ResourceStorePartialLoadTests
    {
        private static ResourceStore BuildStore()
        {
            return new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                });
        }

        private static DictionaryToken ResourcesWithExtGState(string name, IToken? shading = null)
        {
            var resources = new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ExtGState, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Create(name), new DictionaryToken(new Dictionary<NameToken, IToken>()) }
                    })
                }
            };

            if (shading is not null)
            {
                resources[NameToken.Shading] = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Create("Sh0"), shading }
                });
            }

            return new DictionaryToken(resources);
        }

        [Fact]
        public void ALoadThatThrowsLeavesNothingOnTheStore()
        {
            var store = BuildStore();

            store.LoadResourceDictionary(ResourcesWithExtGState("Outer"));

            Assert.Throws<NotImplementedException>(() =>
                store.LoadResourceDictionary(ResourcesWithExtGState("Inner", NameToken.Create("NotAShading"))));

            Assert.Null(store.GetExtendedGraphicsStateDictionary(NameToken.Create("Inner")));
            Assert.NotNull(store.GetExtendedGraphicsStateDictionary(NameToken.Create("Outer")));

            store.UnloadResourceDictionary();

            Assert.Null(store.GetExtendedGraphicsStateDictionary(NameToken.Create("Outer")));
            Assert.Throws<InvalidOperationException>(() => store.UnloadResourceDictionary());
        }
    }
}
