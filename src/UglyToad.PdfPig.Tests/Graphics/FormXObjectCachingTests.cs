namespace UglyToad.PdfPig.Tests.Graphics
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Operations;
    using PdfPig.Logging;
    using PdfPig.Parser;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// Issue #1390: a page can invoke the same form XObject thousands of times. Every invocation used to
    /// resolve the form's stream again and re-parse its content stream.
    /// </summary>
    public class FormXObjectCachingTests
    {
        private static readonly NameToken FormName = NameToken.Create("Fm0");

        private static readonly IndirectReference FormReference = new IndirectReference(5, 0);

        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        private sealed class CountingPageContentParser : IPageContentParser
        {
            private readonly IPageContentParser inner;

            public CountingPageContentParser(IPageContentParser inner) => this.inner = inner;

            public int ParseCallCount { get; private set; }

            public IReadOnlyList<IGraphicsStateOperation> Parse(int pageNumber, IInputBytes inputBytes, ILog log)
            {
                ParseCallCount++;

                return inner.Parse(pageNumber, inputBytes, log);
            }
        }

        private static TestPdfTokenScanner CreateScannerWithForm()
        {
            var scanner = new TestPdfTokenScanner();

            var formDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Xobject },
                { NameToken.Subtype, NameToken.Form },
                {
                    NameToken.Bbox, new ArrayToken(new IToken[]
                    {
                        new NumericToken(0), new NumericToken(0), new NumericToken(10), new NumericToken(10)
                    })
                }
            });

            var formStream = new StreamToken(formDictionary, OtherEncodings.StringAsLatin1Bytes("0 0 10 10 re f\n"));

            scanner.Objects[FormReference] = new ObjectToken(XrefLocation.File(0), FormReference, formStream);

            return scanner;
        }

        private static ContentStreamProcessor CreateProcessor(TestPdfTokenScanner scanner,
            IPageContentParser pageContentParser)
        {
            var parsingOptions = new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true };

            var resourceStore = new ResourceStore(scanner, new NoOpFontFactory(), new TestFilterProvider(), null, parsingOptions);

            resourceStore.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Xobject, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { FormName, new IndirectReferenceToken(FormReference) }
                    })
                }
            }));

            return new ContentStreamProcessor(
                1,
                resourceStore,
                scanner,
                pageContentParser,
                new TestFilterProvider(),
                new CropBox(new PdfRectangle(0, 0, 612, 792)),
                UserSpaceUnit.Default,
                new PageRotationDegrees(0),
                TransformationMatrix.Identity,
                null,
                parsingOptions);
        }

        private static CountingPageContentParser CreateParser()
        {
            return new CountingPageContentParser(
                new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256)));
        }

        [Fact]
        public void RepeatedFormInvocationParsesTheContentStreamOnce()
        {
            var scanner = CreateScannerWithForm();
            var parser = CreateParser();
            var processor = CreateProcessor(scanner, parser);

            processor.ApplyXObject(FormName);
            processor.ApplyXObject(FormName);

            Assert.Equal(1, parser.ParseCallCount);
        }

        [Fact]
        public void RepeatedFormInvocationResolvesTheStreamOnce()
        {
            var scanner = CreateScannerWithForm();
            var processor = CreateProcessor(scanner, CreateParser());

            processor.ApplyXObject(FormName);
            var afterFirstInvocation = scanner.GetCallCount;

            processor.ApplyXObject(FormName);

            Assert.Equal(afterFirstInvocation, scanner.GetCallCount);
        }

        [Fact]
        public void RepeatedFormInvocationRunsTheContentEveryTime()
        {
            var scanner = CreateScannerWithForm();
            var processor = CreateProcessor(scanner, CreateParser());

            processor.ApplyXObject(FormName);
            processor.ApplyXObject(FormName);
            processor.ApplyXObject(FormName);

            var content = processor.Process(1, new List<IGraphicsStateOperation>());

            Assert.Equal(3, content.Paths.Count);
        }
    }
}
