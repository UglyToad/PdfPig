namespace UglyToad.PdfPig.Util
{
    using Filters;
    using Logging;
    using Parser.FileStructure;
    using PdfFonts.Parser;

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

            var trailerParser = new FileTrailerParser();
            var filterProvider = new MemoryFilterProvider(new DecodeParameterResolver(logger), new PngPredictor(), logger);
            
            var cmapParser = new CMapParser();
            
            var container = new Container();
            container.Register(trailerParser);
            container.Register(filterProvider);
            container.Register(cmapParser);
            container.Register(logger);

            return container;
        }
    }
}