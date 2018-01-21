namespace UglyToad.PdfPig.Parser
{
    using System;
    using Content;
    using Parts;

    /// <summary>
    /// For objects which provide document scoped caching.
    /// </summary>
    internal class ParsingCachingProviders
    {
        public BruteForceSearcher BruteForceSearcher { get; }

        public IResourceStore ResourceContainer { get; }

        public ParsingCachingProviders(BruteForceSearcher bruteForceSearcher, IResourceStore resourceContainer)
        {
            BruteForceSearcher = bruteForceSearcher ?? throw new ArgumentNullException(nameof(bruteForceSearcher));
            ResourceContainer = resourceContainer ?? throw new ArgumentNullException(nameof(resourceContainer));
        }
    }
}
