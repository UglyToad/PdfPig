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
            var processor = CreateTestProcessor();
            var op = new MockOperation();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => processor.Process(1, new[] { op }, cts.Token));
            Assert.Equal(0, op.ExecutionCount);
        }

        [Fact]
        public void ProcessOperations_WhenTokenCannotBeCanceled_ExecutesAllOperations()
        {
            var processor = CreateTestProcessor();
            var operations = new List<MockOperation>();
            for (int i = 0; i < 50; i++)
            {
                operations.Add(new MockOperation());
            }

            processor.Process(1, operations, CancellationToken.None);

            Assert.All(operations, op => Assert.Equal(1, op.ExecutionCount));
        }

        [Fact]
        public void ProcessOperations_ChecksCancellationEvery100Operations()
        {
            var processor = CreateTestProcessor();
            using var cts = new CancellationTokenSource();

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

            Assert.Throws<OperationCanceledException>(() => processor.Process(1, operations, cts.Token));

            // The 100th operation triggered the cancellation check before running, so 99 operations ran.
            Assert.Equal(99, executedCount);
        }

        [Fact]
        public void ProcessOperations_DispatchesToCancellableOperations()
        {
            var processor = CreateTestProcessor();
            using var cts = new CancellationTokenSource();

            var cancellableOp = new MockCancellableOperation();
            processor.Process(1, new[] { cancellableOp }, cts.Token);

            Assert.Equal(1, cancellableOp.CancellableExecutionCount);
            Assert.Equal(0, cancellableOp.NonCancellableExecutionCount);
            Assert.Equal(cts.Token, cancellableOp.ReceivedToken);
        }

        [Fact]
        public void ProcessOperations_DispatchesToStandardOperations()
        {
            var processor = CreateTestProcessor();
            using var cts = new CancellationTokenSource();

            var standardOp = new MockOperation();
            processor.Process(1, new[] { standardOp }, cts.Token);

            Assert.Equal(1, standardOp.ExecutionCount);
        }

        [Fact]
        public void InvokeNamedXObject_RunWithoutToken_PassesNoneTokenToContext()
        {
            var name = NameToken.Create("Fm0");
            var op = new InvokeNamedXObject(name);
            var context = new TrackingOperationContext();

            op.Run(context);

            Assert.Equal(name, context.LastAppliedXObjectName);
            Assert.Equal(CancellationToken.None, context.LastReceivedToken);
        }

        [Fact]
        public void InvokeNamedXObject_RunWithToken_PassesTokenToContext()
        {
            var name = NameToken.Create("Fm1");
            var op = new InvokeNamedXObject(name);
            var context = new TrackingOperationContext();
            using var cts = new CancellationTokenSource();

            op.Run(context, cts.Token);

            Assert.Equal(name, context.LastAppliedXObjectName);
            Assert.Equal(cts.Token, context.LastReceivedToken);
        }

        [Fact]
        public void FormXObject_WhenTokenCancelled_ThrowsOperationCanceledException()
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

            var resourceStore = new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() });

            var resourcesDict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Xobject, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { formName, new IndirectReferenceToken(formRef) }
                    })
                }
            });
            resourceStore.LoadResourceDictionary(resourcesDict);

            var processor = new TestProcessor(resourceStore, scanner);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => processor.ApplyXObject(formName, cts.Token));
        }

        private static TestProcessor CreateTestProcessor()
        {
            var scanner = new TestPdfTokenScanner();
            var resourceStore = new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() });

            return new TestProcessor(resourceStore, scanner);
        }

        private sealed class TestProcessor : ContentStreamProcessor
        {
            public TestProcessor(IResourceStore resourceStore, IPdfTokenScanner scanner)
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
                    new ParsingOptions { UseLenientParsing = true, Logger = new NoOpLog() })
            {
            }
        }

        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
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

        private sealed class MockCancellableOperation : ICancellableGraphicsStateOperation
        {
            public string Operator => "mock_cancellable";

            public int CancellableExecutionCount { get; private set; }
            public int NonCancellableExecutionCount { get; private set; }
            public CancellationToken ReceivedToken { get; private set; }

            public void Run(IOperationContext operationContext)
            {
                NonCancellableExecutionCount++;
            }

            public void Run(IOperationContext operationContext, CancellationToken token)
            {
                CancellableExecutionCount++;
                ReceivedToken = token;
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

        private sealed class TrackingOperationContext : TestOperationContext, IOperationContext
        {
            public NameToken? LastAppliedXObjectName { get; private set; }
            public CancellationToken LastReceivedToken { get; private set; }

            void IOperationContext.ApplyXObject(NameToken xObjectName, CancellationToken token)
            {
                LastAppliedXObjectName = xObjectName;
                LastReceivedToken = token;
                base.ApplyXObject(xObjectName, token);
            }
        }
    }
}
