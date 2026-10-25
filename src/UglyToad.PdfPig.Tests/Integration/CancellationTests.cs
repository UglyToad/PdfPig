namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Content;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Graphics;
    using UglyToad.PdfPig.Graphics.Operations;
    using UglyToad.PdfPig.Outline.Destinations;
    using UglyToad.PdfPig.Parser;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;
    using Xunit;

    public class CancellationTests
    {
        private static readonly string TestDocumentPath = IntegrationHelpers.GetDocumentPath("ICML03-081");

        [Fact]
        public void GetPage_WithCancelledToken_ThrowsOperationCanceledException()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => document.GetPage(1, cts.Token));
        }

        [Fact]
        public void GetPage_WithValidToken_ReturnsPage()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            using var cts = new CancellationTokenSource();

            var page = document.GetPage(1, cts.Token);

            Assert.NotNull(page);
            Assert.Equal(1, page.Number);
            Assert.NotEmpty(page.Letters);
        }

        [Fact]
        public void GetPage_DefaultToken_ReturnsPage()
        {
            using var document = PdfDocument.Open(TestDocumentPath);

            var page = document.GetPage(1);

            Assert.NotNull(page);
            Assert.Equal(1, page.Number);
        }

        [Fact]
        public void GetPageGeneric_WithCancelledToken_ThrowsOperationCanceledException()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            document.AddPageFactory<CancellationCustomPage, CancellationCustomPageFactory>();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => document.GetPage<CancellationCustomPage>(1, cts.Token));
        }

        [Fact]
        public void GetPageGeneric_WithValidToken_ReturnsCustomPage()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            document.AddPageFactory<CancellationCustomPage, CancellationCustomPageFactory>();

            using var cts = new CancellationTokenSource();

            var page = document.GetPage<CancellationCustomPage>(1, cts.Token);

            Assert.NotNull(page);
            Assert.Equal(1, page.Number);
        }

        [Fact]
        public void GetPages_WithCancelledToken_ThrowsOperationCanceledException()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var pagesEnumerable = document.GetPages(cts.Token);

            Assert.Throws<OperationCanceledException>(() => pagesEnumerable.ToList());
        }

        [Fact]
        public void GetPages_WithValidToken_EnumeratesAllPages()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            using var cts = new CancellationTokenSource();

            var pages = document.GetPages(cts.Token).ToList();

            Assert.Equal(document.NumberOfPages, pages.Count);
            Assert.All(pages, p => Assert.NotNull(p));
        }

        [Fact]
        public void GetPages_WhenCancelledDuringEnumeration_ThrowsOperationCanceledException()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            using var cts = new CancellationTokenSource();

            var pagesEnumerable = document.GetPages(cts.Token);
            using var enumerator = pagesEnumerable.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.Equal(1, enumerator.Current.Number);

            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void GetPagesGeneric_WithCancelledToken_ThrowsOperationCanceledException()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            document.AddPageFactory<CancellationCustomPage, CancellationCustomPageFactory>();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var pagesEnumerable = document.GetPages<CancellationCustomPage>(cts.Token);

            Assert.Throws<OperationCanceledException>(() => pagesEnumerable.ToList());
        }

        [Fact]
        public void GetPagesGeneric_WithValidToken_EnumeratesAllPages()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            document.AddPageFactory<CancellationCustomPage, CancellationCustomPageFactory>();

            using var cts = new CancellationTokenSource();

            var pages = document.GetPages<CancellationCustomPage>(cts.Token).ToList();

            Assert.Equal(document.NumberOfPages, pages.Count);
            Assert.All(pages, p => Assert.NotNull(p));
        }

        [Fact]
        public void GetPagesGeneric_WhenCancelledDuringEnumeration_ThrowsOperationCanceledException()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            document.AddPageFactory<CancellationCustomPage, CancellationCustomPageFactory>();

            using var cts = new CancellationTokenSource();

            var pagesEnumerable = document.GetPages<CancellationCustomPage>(cts.Token);
            using var enumerator = pagesEnumerable.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.Equal(1, enumerator.Current.Number);

            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void GetPage_CancelledMidPage_UnloadsPageResourcesSoLaterPagesAreUnaffected()
        {
            using var document = PdfDocument.Open(TestDocumentPath);
            using var cts = new CancellationTokenSource();

            CancelMidPageFactory? factory = null;
            document.AddPageFactory<CancelledPage, CancelMidPageFactory>(f =>
            {
                f.Source = cts;
                factory = f;
            });

            Assert.Throws<OperationCanceledException>(() => document.GetPage<CancelledPage>(1, cts.Token));

            // The token is cancelled half way through the page's operations, with its resources loaded.
            Assert.NotNull(factory);
            Assert.True(factory.OperationCount > 2, $"Page 1 has only {factory.OperationCount} operations.");

            // Every level the cancelled page loaded was unloaded, so the shared stack is empty again.
            Assert.Throws<InvalidOperationException>(() => factory.ResourceStore.UnloadResourceDictionary());

            using var freshDocument = PdfDocument.Open(TestDocumentPath);

            for (var pageNumber = 1; pageNumber <= Math.Min(2, document.NumberOfPages); pageNumber++)
            {
                var expected = freshDocument.GetPage(pageNumber).Letters;
                var actual = document.GetPage(pageNumber).Letters;

                Assert.Equal(expected.Select(l => (l.Value, l.FontName)), actual.Select(l => (l.Value, l.FontName)));
            }
        }

        private sealed class CancelledPage
        {
        }

        /// <summary>
        /// Runs the page through the default content stream processor, cancelling the caller's token
        /// half way through the page's operations.
        /// </summary>
        private sealed class CancelMidPageFactory : BasePageFactory<CancelledPage>
        {
            public CancellationTokenSource? Source { get; set; }

            public int OperationCount { get; private set; }

            public CancelMidPageFactory(
                IPdfTokenScanner pdfScanner,
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ParsingOptions parsingOptions)
                : base(pdfScanner, resourceStore, filterProvider, pageContentParser, parsingOptions)
            {
            }

            protected override CancelledPage ProcessPage(
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
                OperationCount = operations.Count;

                var processor = new ContentStreamProcessor(
                    pageNumber,
                    ResourceStore,
                    PdfScanner,
                    PageContentParser,
                    FilterProvider,
                    cropBox,
                    userSpaceUnit,
                    rotation,
                    initialMatrix,
                    ResourceStore.GetPageOutputIntentProfile(dictionary),
                    ParsingOptions,
                    token);

                var half = operations.Count / 2;
                var withCancel = new List<IGraphicsStateOperation>(operations.Count + 1);
                withCancel.AddRange(operations.Take(half));
                withCancel.Add(new CallbackOperation(() => Source!.Cancel()));
                withCancel.AddRange(operations.Skip(half));

                processor.Process(pageNumber, withCancel);

                return new CancelledPage();
            }
        }

        private sealed class CallbackOperation : IGraphicsStateOperation
        {
            private readonly Action action;

            public string Operator => "callback";

            public CallbackOperation(Action action)
            {
                this.action = action;
            }

            public void Run(IOperationContext operationContext) => action();

            public void Write(Stream stream)
            {
            }
        }

        private sealed class CancellationCustomPage
        {
            public int Number { get; }

            public CancellationCustomPage(int number)
            {
                Number = number;
            }
        }

        private sealed class CancellationCustomPageFactory : IPageFactory<CancellationCustomPage>
        {
            public CancellationCustomPageFactory()
            {
            }

            public CancellationCustomPageFactory(
                IPdfTokenScanner pdfScanner,
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ParsingOptions parsingOptions)
            {
            }

            public CancellationCustomPage Create(
                int number,
                DictionaryToken dictionary,
                PageTreeMembers pageTreeMembers,
                NamedDestinations namedDestinations,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return new CancellationCustomPage(number);
            }
        }
    }
}
