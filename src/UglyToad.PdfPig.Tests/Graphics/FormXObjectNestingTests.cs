namespace UglyToad.PdfPig.Tests.Graphics
{
    using System.Collections.Generic;
    using System.Threading;
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

        /// <summary>
        /// Counts the forms it processes, and stops a run that would otherwise process an exponential
        /// number of them so that a regression fails rather than hangs.
        /// </summary>
        private sealed class CountingProcessor : ContentStreamProcessor
        {
            private const int RunawayLimit = 10_000;

            public CountingProcessor(IResourceStore resourceStore, TestPdfTokenScanner scanner, ParsingOptions parsingOptions)
                : base(
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
                    parsingOptions,
                    CancellationToken.None)
            {
            }

            public int FormsProcessed { get; private set; }

            protected override void ProcessFormXObject(StreamToken formStream, NameToken xObjectName)
            {
                FormsProcessed++;

                if (FormsProcessed > RunawayLimit)
                {
                    throw new InvalidOperationException($"More than {RunawayLimit} forms were processed.");
                }

                base.ProcessFormXObject(formStream, xObjectName);
            }
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

        private static StreamToken Form(string content, DictionaryToken? resources)
        {
            var entries = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Xobject },
                { NameToken.Subtype, NameToken.Form }
            };

            if (resources is not null)
            {
                entries[NameToken.Resources] = resources;
            }

            return new StreamToken(new DictionaryToken(entries), OtherEncodings.StringAsLatin1Bytes(content));
        }

        private static TestPdfTokenScanner MutuallyRecursiveForms(string formAContent, string formBContent)
        {
            var scanner = new TestPdfTokenScanner();

            scanner.Objects[FormAReference] = new ObjectToken(XrefLocation.File(0), FormAReference,
                Form(formAContent, XObjectResources(FormBName, FormBReference)));
            scanner.Objects[FormBReference] = new ObjectToken(XrefLocation.File(0), FormBReference,
                Form(formBContent, XObjectResources(FormAName, FormAReference)));

            return scanner;
        }

        /// <summary>
        /// Distinct forms Fm0 to Fm(n - 1), each painting a path and invoking the next.
        /// </summary>
        private static TestPdfTokenScanner FormChain(int length)
        {
            var scanner = new TestPdfTokenScanner();

            for (var i = 0; i < length; i++)
            {
                var reference = new IndirectReference(FormAReference.ObjectNumber + i, 0);
                var next = new IndirectReference(FormAReference.ObjectNumber + i + 1, 0);
                var nextName = NameToken.Create($"Fm{i + 1}");

                var form = i < length - 1
                    ? Form($"0 0 10 10 re f\n/{nextName.Data} Do\n", XObjectResources(nextName, next))
                    : Form("0 0 10 10 re f\n", null);

                scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, form);
            }

            return scanner;
        }

        private static CountingProcessor CreateProcessor(TestPdfTokenScanner scanner, bool useLenientParsing)
        {
            var parsingOptions = new ParsingOptions { UseLenientParsing = useLenientParsing, SkipMissingFonts = true };

            var resourceStore = new ResourceStore(scanner, new NoOpFontFactory(), new TestFilterProvider(), null, parsingOptions);

            resourceStore.LoadResourceDictionary(XObjectResources(FormAName, FormAReference));

            return new CountingProcessor(resourceStore, scanner, parsingOptions);
        }

        [Fact]
        public void MutuallyRecursiveFormsArePaintedOnceWhenLenient()
        {
            // A paints a path and invokes B, B invokes A.
            var processor = CreateProcessor(MutuallyRecursiveForms("0 0 10 10 re f\n/Fm1 Do\n", "/Fm0 Do\n"), useLenientParsing: true);

            processor.ApplyXObject(FormAName);

            var content = processor.Process(1, new List<IGraphicsStateOperation>());

            // B's invocation of A is skipped because A is still being processed.
            Assert.Single(content.Paths);
        }

        [Fact]
        public void MutuallyRecursiveFormsThrowWhenNotLenient()
        {
            var processor = CreateProcessor(MutuallyRecursiveForms("0 0 10 10 re f\n/Fm1 Do\n", "/Fm0 Do\n"), useLenientParsing: false);

            Assert.Throws<PdfDocumentFormatException>(() => processor.ApplyXObject(FormAName));
        }

        [Fact]
        public void FormsInvokingEachOtherTwiceAreNotRepeatedExponentially()
        {
            // A invokes B twice, B invokes A twice. Repeating them until the nesting limit takes about 2^50 invocations.
            var processor = CreateProcessor(MutuallyRecursiveForms("/Fm1 Do\n/Fm1 Do\n", "/Fm0 Do\n/Fm0 Do\n"), useLenientParsing: true);

            processor.ApplyXObject(FormAName);

            // A once and B once for each of A's two invocations of it.
            Assert.Equal(3, processor.FormsProcessed);
        }

        [Fact]
        public void FormInvokingItselfThroughInheritedResourcesIsPaintedOnce()
        {
            // A has no resources of its own, so its name resolves through the page's resources. The self
            // reference check compares A's own resources and does not see this.
            var scanner = new TestPdfTokenScanner();
            scanner.Objects[FormAReference] = new ObjectToken(XrefLocation.File(0), FormAReference,
                Form("0 0 10 10 re f\n/Fm0 Do\n", null));

            var processor = CreateProcessor(scanner, useLenientParsing: true);

            processor.ApplyXObject(FormAName);

            var content = processor.Process(1, new List<IGraphicsStateOperation>());

            Assert.Single(content.Paths);
        }

        [Fact]
        public void DistinctFormsNestedPastTheLimitStopAtTheLimitWhenLenient()
        {
            var processor = CreateProcessor(FormChain(10_000), useLenientParsing: true);

            processor.ApplyXObject(FormAName);

            var content = processor.Process(1, new List<IGraphicsStateOperation>());

            Assert.Equal(ContentStreamProcessor.MaxFormXObjectDepth, content.Paths.Count);
        }

        [Fact]
        public void DistinctFormsNestedPastTheLimitThrowWhenNotLenient()
        {
            var processor = CreateProcessor(FormChain(10_000), useLenientParsing: false);

            Assert.Throws<PdfDocumentFormatException>(() => processor.ApplyXObject(FormAName));
        }
    }
}
