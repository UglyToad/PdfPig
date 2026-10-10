namespace UglyToad.PdfPig.Tests.Graphics
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Operations;
    using PdfPig.Parser;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// Two form XObjects that invoke each other recursed until the stack overflowed, which kills the
    /// process rather than throwing. The self reference check only sees a form naming itself.
    /// </summary>
    public class FormXObjectNestingTests
    {
        private static readonly NameToken FormAName = NameToken.Create("Fm0");
        private static readonly NameToken FormBName = NameToken.Create("Fm1");

        private static readonly IndirectReference FormAReference = new IndirectReference(5, 0);
        private static readonly IndirectReference FormBReference = new IndirectReference(6, 0);

        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        private static DictionaryToken XObjectResources(NameToken name, IndirectReference reference)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Xobject, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { name, new IndirectReferenceToken(reference) }
                    })
                }
            });
        }

        private static StreamToken Form(string content, DictionaryToken resources)
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Xobject },
                { NameToken.Subtype, NameToken.Form },
                { NameToken.Resources, resources }
            });

            return new StreamToken(dictionary, OtherEncodings.StringAsLatin1Bytes(content));
        }

        private static ContentStreamProcessor CreateProcessor(bool useLenientParsing)
        {
            var scanner = new TestPdfTokenScanner();

            // A paints a path and invokes B, B invokes A.
            scanner.Objects[FormAReference] = new ObjectToken(XrefLocation.File(0), FormAReference,
                Form("0 0 10 10 re f\n/Fm1 Do\n", XObjectResources(FormBName, FormBReference)));
            scanner.Objects[FormBReference] = new ObjectToken(XrefLocation.File(0), FormBReference,
                Form("/Fm0 Do\n", XObjectResources(FormAName, FormAReference)));

            var parsingOptions = new ParsingOptions { UseLenientParsing = useLenientParsing, SkipMissingFonts = true };

            var resourceStore = new ResourceStore(scanner, new NoOpFontFactory(), new TestFilterProvider(), null, parsingOptions);

            resourceStore.LoadResourceDictionary(XObjectResources(FormAName, FormAReference));

            return new ContentStreamProcessor(
                1,
                resourceStore,
                scanner,
                new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256)),
                new TestFilterProvider(),
                new CropBox(new PdfRectangle(0, 0, 612, 792)),
                UserSpaceUnit.Default,
                new PageRotationDegrees(0),
                TransformationMatrix.Identity,
                null,
                parsingOptions);
        }

        [Fact]
        public void MutuallyRecursiveFormsStopAtTheNestingLimitWhenLenient()
        {
            var processor = CreateProcessor(useLenientParsing: true);

            processor.ApplyXObject(FormAName);

            var content = processor.Process(1, new List<IGraphicsStateOperation>());

            // Every other nesting level is A, which paints one path.
            Assert.Equal(ContentStreamProcessor.MaxFormXObjectDepth / 2, content.Paths.Count);
        }

        [Fact]
        public void MutuallyRecursiveFormsThrowWhenNotLenient()
        {
            var processor = CreateProcessor(useLenientParsing: false);

            Assert.Throws<PdfDocumentFormatException>(() => processor.ApplyXObject(FormAName));
        }
    }
}
