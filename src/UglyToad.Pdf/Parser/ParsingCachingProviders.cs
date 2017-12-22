namespace UglyToad.Pdf.Parser
{
    using System;
    using Content;
    using Cos;
    using Parts;

    /// <summary>
    /// For objects which provide document scoped caching.
    /// </summary>
    internal class ParsingCachingProviders
    {
        public CosObjectPool ObjectPool { get; }

        public BruteForceSearcher BruteForceSearcher { get; }

        public IResourceStore ResourceContainer { get; }

        public ParsingCachingProviders(CosObjectPool objectPool, BruteForceSearcher bruteForceSearcher, IResourceStore resourceContainer)
        {
            ObjectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
            BruteForceSearcher = bruteForceSearcher ?? throw new ArgumentNullException(nameof(bruteForceSearcher));
            ResourceContainer = resourceContainer ?? throw new ArgumentNullException(nameof(resourceContainer));
        }
    }
}
