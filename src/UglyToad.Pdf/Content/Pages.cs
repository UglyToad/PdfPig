namespace UglyToad.Pdf.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using IO;
    using Logging;
    using Parser;

    internal class Pages
    {
        private readonly ILog log;
        private readonly Catalog catalog;
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly IPageFactory pageFactory;
        private readonly IRandomAccessRead reader;
        private readonly bool isLenientParsing;
        private readonly PdfDictionary rootPageDictionary;
        private readonly Dictionary<int, PdfDictionary> locatedPages = new Dictionary<int, PdfDictionary>();

        public int Count { get; }

        internal Pages(ILog log, Catalog catalog, IPdfObjectParser pdfObjectParser, IPageFactory pageFactory, 
            IRandomAccessRead reader, bool isLenientParsing)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            rootPageDictionary = catalog.PagesDictionary;

            Count = rootPageDictionary.GetIntOrDefault(CosName.COUNT);

            this.log = log;
            this.catalog = catalog;
            this.pdfObjectParser = pdfObjectParser;
            this.pageFactory = pageFactory;
            this.reader = reader;
            this.isLenientParsing = isLenientParsing;
        }


        public Page GetPage(int pageNumber)
        {
            if (locatedPages.TryGetValue(pageNumber, out PdfDictionary targetPageDictionary))
            {
                // TODO: cache the page
                return pageFactory.Create(pageNumber, targetPageDictionary, new PageTreeMembers(), reader,
                    isLenientParsing);
            }

            var observed = new List<int>();

            // todo: running a search for a different, unloaded, page number, results in a bug.
            var isFound = FindPage(rootPageDictionary, pageNumber, observed);

            if (!isFound || !locatedPages.TryGetValue(pageNumber, out targetPageDictionary))
            {
                throw new ArgumentOutOfRangeException("Could not find the page with number: " + pageNumber);
            }

            var page = pageFactory.Create(pageNumber, targetPageDictionary, new PageTreeMembers(), reader, isLenientParsing);

            locatedPages[pageNumber] = targetPageDictionary;

            return page;
        }

        private static int GetNextPageNumber(IReadOnlyList<int> pages)
        {
            if (pages.Count == 0)
            {
                return 1;
            }

            return pages[pages.Count - 1] + 1;
        }

        public bool FindPage(PdfDictionary currentPageDictionary, int soughtPageNumber, List<int> pageNumbersObserved)
        {
            var type = currentPageDictionary.GetName(CosName.TYPE);

            if (type.Equals(CosName.PAGE))
            {
                var pageNumber = GetNextPageNumber(pageNumbersObserved);

                bool found = pageNumber == soughtPageNumber;

                locatedPages[pageNumber] = currentPageDictionary;

                return found;
            }

            if (!type.Equals(CosName.PAGES))
            {
                log.Warn("Did not find the expected type (Page or Pages) in dictionary: " + currentPageDictionary);

                return false;
            }

            var kids = currentPageDictionary.GetDictionaryObject(CosName.KIDS) as COSArray;

            pageFactory.LoadResources(currentPageDictionary, reader, isLenientParsing);

            bool childFound = false;
            foreach (var kid in kids.OfType<CosObject>())
            {
                // todo: exit early
                var child = pdfObjectParser.Parse(kid.ToIndirectReference(), reader, isLenientParsing) as PdfDictionary;

                var thisPageMatches = FindPage(child, soughtPageNumber, pageNumbersObserved);

                if (thisPageMatches)
                {
                    childFound = true;
                }
            }

            return childFound;
        }

        public IReadOnlyList<Page> GetAllPages()
        {
            return new Page[0];
        }

        public void LoadAll()
        {

        }
    }
}
