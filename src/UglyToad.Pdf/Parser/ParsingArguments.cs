namespace UglyToad.Pdf.Parser
{
    using System;
    using Cos;
    using IO;
    using Util;

    internal class ParsingArguments
    {
        public IRandomAccessRead Reader { get; }

        public CrossReferenceTable CrossReferenceTable { get; }

        public ParsingCachingProviders CachingProviders { get; }

        public IContainer Container { get; }

        public bool IsLenientParsing { get; }

        public ParsingArguments(IRandomAccessRead reader, CrossReferenceTable crossReferenceTable, ParsingCachingProviders cachingProviders, IContainer container, bool isLenientParsing)
        {
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            CrossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
            CachingProviders = cachingProviders ?? throw new ArgumentNullException(nameof(cachingProviders));
            Container = container ?? throw new ArgumentNullException(nameof(container));
            IsLenientParsing = isLenientParsing;
        }
    }

}
