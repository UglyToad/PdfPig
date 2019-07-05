namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class Pages
    {
        private readonly ILog log;
        private readonly Catalog catalog;
        private readonly IPageFactory pageFactory;
        private readonly bool isLenientParsing;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly DictionaryToken rootPageDictionary;
        private readonly Dictionary<int, DictionaryToken> locatedPages = new Dictionary<int, DictionaryToken>();

        public int Count { get; }

        internal Pages(ILog log, Catalog catalog, IPageFactory pageFactory, bool isLenientParsing, IPdfTokenScanner pdfScanner)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            rootPageDictionary = catalog.PagesDictionary;

            Count = rootPageDictionary.GetIntOrDefault(NameToken.Count);

            this.log = log;
            this.catalog = catalog;
            this.pageFactory = pageFactory;
            this.isLenientParsing = isLenientParsing;
            this.pdfScanner = pdfScanner;
        }
        
        public Page GetPage(int pageNumber)
        {
            if (locatedPages.TryGetValue(pageNumber, out DictionaryToken targetPageDictionary))
            {
                // TODO: cache the page
                return pageFactory.Create(pageNumber, targetPageDictionary, new PageTreeMembers(),
                    isLenientParsing);
            }

            var observed = new List<int>();

            var pageTreeMembers = new PageTreeMembers();

            // todo: running a search for a different, unloaded, page number, results in a bug.
            var isFound = FindPage(rootPageDictionary, pageNumber, observed, pageTreeMembers);

            if (!isFound || !locatedPages.TryGetValue(pageNumber, out targetPageDictionary))
            {
                throw new ArgumentOutOfRangeException("Could not find the page with number: " + pageNumber);
            }

            var page = pageFactory.Create(pageNumber, targetPageDictionary, pageTreeMembers, isLenientParsing);

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

        public bool FindPage(DictionaryToken currentPageDictionary, int soughtPageNumber, List<int> pageNumbersObserved, PageTreeMembers pageTreeMembers)
        {
            var type = currentPageDictionary.GetNameOrDefault(NameToken.Type);

            if (type?.Equals(NameToken.Page) == true)
            {
                var pageNumber = GetNextPageNumber(pageNumbersObserved);

                bool found = pageNumber == soughtPageNumber;

                locatedPages[pageNumber] = currentPageDictionary;
                pageNumbersObserved.Add(pageNumber);

                return found;
            }

            if (type?.Equals(NameToken.Pages) != true)
            {
                log.Warn("Did not find the expected type (Page or Pages) in dictionary: " + currentPageDictionary);

                return false;
            }

            if (currentPageDictionary.TryGet(NameToken.MediaBox, out var token))
            {
                var mediaBox = DirectObjectFinder.Get<ArrayToken>(token, pdfScanner);

                pageTreeMembers.MediaBox = new MediaBox(mediaBox.ToRectangle());
            }

            if (currentPageDictionary.TryGet(NameToken.Rotate, pdfScanner, out NumericToken rotateToken))
            {
                pageTreeMembers.Rotation = rotateToken.Int;
            }

            if (!currentPageDictionary.TryGet(NameToken.Kids, out var kids)
            || !(kids is ArrayToken kidsArray))
            {
                return false;
            }
            
            pageFactory.LoadResources(currentPageDictionary, isLenientParsing);

            bool childFound = false;
            foreach (var kid in kidsArray.Data)
            {
                // todo: exit early
                var child = DirectObjectFinder.Get<DictionaryToken>(kid, pdfScanner);
                
                var thisPageMatches = FindPage(child, soughtPageNumber, pageNumbersObserved, pageTreeMembers);

                if (thisPageMatches)
                {
                    childFound = true;
                    break;
                }
            }

            return childFound;
        }

        public IReadOnlyList<Page> GetAllPages()
        {
            return new Page[0];
        }
    }
}
