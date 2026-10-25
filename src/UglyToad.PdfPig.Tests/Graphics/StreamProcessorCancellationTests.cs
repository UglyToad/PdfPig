namespace UglyToad.PdfPig.Tests.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Content;
    using Logging;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Operations;
    using PdfPig.Parser;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using Tokens;
    using Xunit;

    public class StreamProcessorCancellationTests
    {
        [Fact]
        public void ProcessOperations_WhenPreCancelled_ThrowsImmediatelyAndDoesNotRunOperations()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var processor = CreateTestProcessor(cts.Token);
            var op = new MockOperation();

            Assert.Throws<OperationCanceledException>(() => processor.Process(1, new[] { op }));
            Assert.Equal(0, op.ExecutionCount);
        }

        [Fact]
        public void ProcessOperations_WhenTokenCannotBeCanceled_ExecutesAllOperations()
        {
            var processor = CreateTestProcessor(CancellationToken.None);
            var operations = new List<MockOperation>();
            for (int i = 0; i < 50; i++)
            {
                operations.Add(new MockOperation());
            }

            processor.Process(1, operations);

            Assert.All(operations, op => Assert.Equal(1, op.ExecutionCount));
        }

        [Fact]
        public void ProcessOperations_StopsBeforeTheOperationAfterTheCancel()
        {
            using var cts = new CancellationTokenSource();
            var processor = CreateTestProcessor(cts.Token);

            var operations = new List<IGraphicsStateOperation>();
            int executedCount = 0;

            for (int i = 0; i < 150; i++)
            {
                int index = i;
                operations.Add(new CallbackOperation(() =>
                {
                    executedCount++;
                    if (index == 50)
                    {
                        cts.Cancel();
                    }
                }));
            }

            Assert.Throws<OperationCanceledException>(() => processor.Process(1, operations));

            // The operation at index 50 cancelled, so it and the 50 before it ran.
            Assert.Equal(51, executedCount);
        }

        [Fact]
        public void ProcessOperations_WithLiveToken_RunsOperations()
        {
            using var cts = new CancellationTokenSource();
            var processor = CreateTestProcessor(cts.Token);

            var standardOp = new MockOperation();
            processor.Process(1, new[] { standardOp });

            Assert.Equal(1, standardOp.ExecutionCount);
        }

        [Fact]
        public void FormXObject_WhenCancelledBeforeInvocation_DoesNotRunTheForm()
        {
            var formName = NameToken.Create("Fm0");
            var formRef = new IndirectReference(10, 0);

            var scanner = new TestPdfTokenScanner();
            var entries = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Xobject },
                { NameToken.Subtype, NameToken.Form }
            };
            var formStream = new StreamToken(new DictionaryToken(entries), OtherEncodings.StringAsLatin1Bytes("1 0 0 1 0 0 cm"));
            scanner.Objects[formRef] = new ObjectToken(XrefLocation.File(0), formRef, formStream);

            var filterProvider = new CountingFilterProvider();
            var resourceStore = new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                filterProvider,
                null,
                new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() });

            resourceStore.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Xobject, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { formName, new IndirectReferenceToken(formRef) }
                    })
                }
            }));

            using var cts = new CancellationTokenSource();

            var processor = new TestProcessor(resourceStore, scanner, cts.Token, filterProvider);

            // The page's loop sees the cancel before the form operation runs.
            var operations = new IGraphicsStateOperation[]
            {
                new CallbackOperation(cts.Cancel),
                new InvokeNamedXObject(formName)
            };

            Assert.Throws<OperationCanceledException>(() => processor.Process(1, operations));
            Assert.Equal(1, processor.StackSize);
            Assert.Equal(0, filterProvider.DecodeCount);
        }

        [Fact]
        public void FormXObject_WithResources_WhenCancelled_UnloadsFormResources()
        {
            var formName = NameToken.Create("Fm0");
            var formOnlyName = NameToken.Create("FormOnly");
            var formRef = new IndirectReference(10, 0);

            var formResources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Xobject, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { formOnlyName, new IndirectReferenceToken(new IndirectReference(11, 0)) }
                    })
                }
            });

            var scanner = new TestPdfTokenScanner();
            var entries = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Xobject },
                { NameToken.Subtype, NameToken.Form },
                { NameToken.Resources, formResources }
            };
            var formStream = new StreamToken(new DictionaryToken(entries), OtherEncodings.StringAsLatin1Bytes("1 0 0 1 0 0 cm"));
            scanner.Objects[formRef] = new ObjectToken(XrefLocation.File(0), formRef, formStream);

            using var cts = new CancellationTokenSource();

            var filterProvider = new CountingFilterProvider { OnDecode = cts.Cancel };
            var resourceStore = new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                filterProvider,
                null,
                new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() });

            var pageResources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Xobject, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { formName, new IndirectReferenceToken(formRef) }
                    })
                }
            });
            resourceStore.LoadResourceDictionary(pageResources);

            var processor = new TestProcessor(resourceStore, scanner, cts.Token, filterProvider);

            // The form loads its /Resources, the cancel lands while its content is decoded, and the check
            // before parsing throws.
            var operations = new IGraphicsStateOperation[]
            {
                new InvokeNamedXObject(formName)
            };

            Assert.Throws<OperationCanceledException>(() => processor.Process(1, operations));
            Assert.Equal(1, filterProvider.DecodeCount);
            Assert.Equal(1, processor.StackSize);

            // The form's level is gone, so its names no longer resolve while the page's still do.
            Assert.False(resourceStore.TryGetXObjectReference(formOnlyName, out _));
            Assert.True(resourceStore.TryGetXObjectReference(formName, out _));

            // Only the page level is left.
            resourceStore.UnloadResourceDictionary();
            Assert.Throws<InvalidOperationException>(() => resourceStore.UnloadResourceDictionary());
        }

        [Fact]
        public void FormXObject_CancelledWhileLoadingFormResources_DoesNotDecodeTheForm()
        {
            var formName = NameToken.Create("Fm0");
            var formRef = new IndirectReference(10, 0);

            // Loading the form's inline font is where the cancel lands.
            var formResources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Font, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Create("F1"), new DictionaryToken(new Dictionary<NameToken, IToken> { { NameToken.Type, NameToken.Font } }) }
                    })
                }
            });

            var scanner = new TestPdfTokenScanner();
            var entries = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Xobject },
                { NameToken.Subtype, NameToken.Form },
                { NameToken.Resources, formResources }
            };
            var formStream = new StreamToken(new DictionaryToken(entries), OtherEncodings.StringAsLatin1Bytes("1 0 0 1 0 0 cm"));
            scanner.Objects[formRef] = new ObjectToken(XrefLocation.File(0), formRef, formStream);

            using var cts = new CancellationTokenSource();

            var filterProvider = new CountingFilterProvider();
            var resourceStore = new ResourceStore(
                scanner,
                new CancellingFontFactory(cts),
                filterProvider,
                null,
                new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() });

            resourceStore.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Xobject, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { formName, new IndirectReferenceToken(formRef) }
                    })
                }
            }));

            var processor = new TestProcessor(resourceStore, scanner, cts.Token, filterProvider);

            Assert.Throws<OperationCanceledException>(() => processor.Process(1, new IGraphicsStateOperation[] { new InvokeNamedXObject(formName) }));
            Assert.Equal(0, filterProvider.DecodeCount);

            // The form's level was unloaded, so only the page level is left.
            resourceStore.UnloadResourceDictionary();
            Assert.Throws<InvalidOperationException>(() => resourceStore.UnloadResourceDictionary());
        }

        private static TestProcessor CreateTestProcessor(CancellationToken token)
        {
            var scanner = new TestPdfTokenScanner();
            var resourceStore = new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() });

            return new TestProcessor(resourceStore, scanner, token);
        }

        private sealed class TestProcessor : ContentStreamProcessor
        {
            public TestProcessor(IResourceStore resourceStore, IPdfTokenScanner scanner, CancellationToken token,
                ILookupFilterProvider? filterProvider = null)
                : base(
                    1,
                    resourceStore,
                    scanner,
                    new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256)),
                    filterProvider ?? new TestFilterProvider(),
                    new CropBox(new PdfRectangle(0, 0, 612, 792)),
                    UserSpaceUnit.Default,
                    new PageRotationDegrees(0),
                    TransformationMatrix.Identity,
                    null,
                    new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() },
                    token)
            {
            }
        }

        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        private sealed class CancellingFontFactory(CancellationTokenSource source) : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary)
            {
                source.Cancel();
                return null!;
            }
        }

        private sealed class MockOperation : IGraphicsStateOperation
        {
            public string Operator => "mock";

            public int ExecutionCount { get; private set; }

            public void Run(IOperationContext operationContext)
            {
                ExecutionCount++;
            }

            public void Write(Stream stream)
            {
            }
        }

        private sealed class CallbackOperation : IGraphicsStateOperation
        {
            private readonly Action action;

            public string Operator => "callback";

            public CallbackOperation(Action action)
            {
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public void Run(IOperationContext operationContext)
            {
                action();
            }

            public void Write(Stream stream)
            {
            }
        }
    }
}
