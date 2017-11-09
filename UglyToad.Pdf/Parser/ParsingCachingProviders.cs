namespace UglyToad.Pdf.Parser
{
    using System;
    using Cos;
    using Parts;

    /// <summary>
    /// For objects which provide document scoped caching.
    /// </summary>
    internal class ParsingCachingProviders
    {
        public CosObjectPool ObjectPool { get; }

        public BruteForceSearcher BruteForceSearcher { get; }

        public ParsingCachingProviders(CosObjectPool objectPool, BruteForceSearcher bruteForceSearcher)
        {
            ObjectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
            BruteForceSearcher = bruteForceSearcher ?? throw new ArgumentNullException(nameof(bruteForceSearcher));
        }
    }
}
