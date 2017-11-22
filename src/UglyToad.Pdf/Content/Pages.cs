namespace UglyToad.Pdf.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using Logging;
    using Parser;
    using Parser.PageTree;

    public class Pages
    {
        private readonly Catalog catalog;
        private readonly ParsingArguments arguments;
        private readonly ContentStreamDictionary rootPageDictionary;
        private readonly Dictionary<int, ContentStreamDictionary> locatedPages = new Dictionary<int, ContentStreamDictionary>();

        public int Count { get; }

        internal Pages(Catalog catalog, ParsingArguments arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var pages = catalog.Get(CosName.PAGES) as CosObject;

            if (pages == null)
            {
                throw new InvalidOperationException("No pages were present in the catalog for this PDF document");
            }

            var pageObject = arguments.Container.Get<DynamicParser>().Parse(arguments, pages, false);

            if (!(pageObject is ContentStreamDictionary catalogPageDictionary))
            {
                throw new InvalidOperationException("Could not find the root pages object: " + pages);
            }

            var count = catalogPageDictionary.GetIntOrDefault(CosName.COUNT);

            rootPageDictionary = catalogPageDictionary;

            Count = count;

            this.catalog = catalog;
            this.arguments = arguments;
        }


        public Page GetPage(int pageNumber)
        {
            if (locatedPages.TryGetValue(pageNumber, out ContentStreamDictionary targetPageDictionary))
            {
                return new Page(pageNumber, targetPageDictionary, new PageTreeMembers(), arguments);
            }

            var observed = new List<int>();

            // todo: running a search for a different, unloaded, page number, results in a bug.
            var isFound = FindPage(rootPageDictionary, pageNumber, observed);

            if (!isFound || !locatedPages.TryGetValue(pageNumber, out targetPageDictionary))
            {
                throw new InvalidOperationException("Could not find the page with number: " + pageNumber);
            }

            var page = arguments.Container.Get<PageParser>()
                .Parse(pageNumber, targetPageDictionary, arguments);

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

        public bool FindPage(ContentStreamDictionary currentPageDictionary, int soughtPageNumber, List<int> pageNumbersObserved)
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
                arguments.Container.Get<ILog>()
                    .Warn("Did not find the expected type (Page or Pages) in dictionary: " + currentPageDictionary);

                return false;
            }

            var kids = currentPageDictionary.GetDictionaryObject(CosName.KIDS) as COSArray;

            bool childFound = false;
            foreach (var kid in kids.OfType<CosObject>())
            {
                // todo: exit early
                var child = arguments.Container.Get<DynamicParser>().Parse(arguments, kid, false) as ContentStreamDictionary;

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
