namespace UglyToad.Pdf.Util
{
    using Filters;
    using Graphics;
    using Logging;
    using Parser;
    using Parser.PageTree;
    using Parser.Parts;
    using Parser.Parts.CrossReference;

    internal static class Bootstrapper
    {
        private static IContainer _testContainer;

        internal static void SetTestContainer(IContainer container)
        {
            _testContainer = container;
        }

        public static IContainer GenerateContainer(ILog logger)
        {
            if (_testContainer != null)
            {
                return _testContainer;
            }

            if (logger == null)
            {
                logger = new NoOpLog();
            }

            var headerParser = new FileHeaderParser(logger);
            var trailerParser = new FileTrailerParser();
            var nameParser = new CosNameParser();
            var dictionaryParser = new CosDictionaryParser(nameParser, logger);
            var baseParser = new CosBaseParser(nameParser, new CosStringParser(), dictionaryParser, new CosArrayParser());
            var streamParser = new CosStreamParser(logger);
            var filterProvider = new MemoryFilterProvider(new DecodeParameterResolver(logger), new PngPredictor(), logger);
            var crossReferenceParser = new CrossReferenceStreamParser(filterProvider);
            var objectStreamParser = new ObjectStreamParser(logger, filterProvider, baseParser);
            var dynamicParser = new DynamicParser(logger, baseParser, streamParser, objectStreamParser);

            var crossReferenceTableParser = new FileCrossReferenceTableParser(logger, dictionaryParser, baseParser, streamParser, crossReferenceParser,
                new CrossReferenceTableParser(logger, dictionaryParser, baseParser));

            var resourceDictionaryParser = new ResourceDictionaryParser();
            var pageParser = new PageParser();
            var simpleFontParser = new SimpleFontParser();
            var compositeFontParser = new CompositeFontParser();
            var fontParser = new FontParser();
            var pageContentParser = new PageContentParser();
            var operationFactory = new ReflectionGraphicsStateOperationFactory();

            var container = new Container();
            container.Register(headerParser);
            container.Register(trailerParser);
            container.Register(nameParser);
            container.Register(dictionaryParser);
            container.Register(baseParser);
            container.Register(streamParser);
            container.Register(crossReferenceParser);
            container.Register(crossReferenceTableParser);
            container.Register(dynamicParser);
            container.Register(objectStreamParser);
            container.Register(filterProvider);
            container.Register(resourceDictionaryParser);
            container.Register(pageParser);
            container.Register(simpleFontParser);
            container.Register(compositeFontParser);
            container.Register(fontParser);
            container.Register(pageContentParser);
            container.Register(operationFactory);

            return container;
        }
    }
}