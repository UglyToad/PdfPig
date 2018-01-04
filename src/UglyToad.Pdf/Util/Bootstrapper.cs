namespace UglyToad.Pdf.Util
{
    using Filters;
    using Fonts.Parser;
    using Logging;
    using Parser;
    using Parser.FileStructure;
    using Parser.FileStructure.UglyToad.Pdf.Parser.Parts.CrossReference;
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

            var crossReferenceTableParser = new CrossReferenceParser(logger, dictionaryParser, baseParser, streamParser, crossReferenceParser, new CrossReferenceTableParser(),
                new OldCrossReferenceTableParser(logger, dictionaryParser, baseParser));
            
            var cmapParser = new CMapParser();
            var afmParser = new AdobeFontMetricsParser();
            
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
            container.Register(cmapParser);
            container.Register(afmParser);
            container.Register(logger);

            return container;
        }
    }
}