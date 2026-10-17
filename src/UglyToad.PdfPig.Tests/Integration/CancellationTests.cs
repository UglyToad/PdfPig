namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Linq;
    using System.Threading;
    using Content;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Filters;
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
