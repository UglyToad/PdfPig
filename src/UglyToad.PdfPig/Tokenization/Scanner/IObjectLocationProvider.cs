namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using Core;
    using CrossReference;
    using Parser.Parts;
    using Tokens;

    internal interface IObjectLocationProvider
    {
        bool TryGetOffset(IndirectReference reference, out long offset);

        void UpdateOffset(IndirectReference reference, long offset);

        bool TryGetCached(IndirectReference reference, out ObjectToken objectToken);

        void Cache(ObjectToken objectToken);
    }

    internal class ObjectLocationProvider : IObjectLocationProvider
    {
        private readonly Dictionary<IndirectReference, ObjectToken> cache = new Dictionary<IndirectReference, ObjectToken>();

        /// <summary>
        /// Since we want to scan objects while reading the cross reference table we lazily load it when it's ready.
        /// </summary>
        private readonly Func<CrossReferenceTable> crossReferenceTable;
        private readonly BruteForceSearcher searcher;

        /// <summary>
        /// Indicates whether we now have a cross reference table.
        /// </summary>
        private bool loadedFromTable;

        private readonly Dictionary<IndirectReference, long> offsets = new Dictionary<IndirectReference, long>();

        public ObjectLocationProvider(Func<CrossReferenceTable> crossReferenceTable, BruteForceSearcher searcher)
        {
            this.crossReferenceTable = crossReferenceTable;
            this.searcher = searcher;
        }

        public bool TryGetOffset(IndirectReference reference, out long offset)
        {
            if (!loadedFromTable)
            {
                var table = crossReferenceTable.Invoke();

                if (table != null)
                {
                    foreach (var objectOffset in table.ObjectOffsets)
                    {
                        offsets[objectOffset.Key] = objectOffset.Value;
                    }

                    loadedFromTable = true;
                }
            }

            if (offsets.TryGetValue(reference, out offset))
            {
                return true;
            }

            var locations = searcher.GetObjectLocations();

            if (locations.TryGetValue(reference, out offset))
            {
                return true;
            }

            return false;
        }

        public void UpdateOffset(IndirectReference reference, long offset)
        {
            offsets[reference] = offset;
        }

        public bool TryGetCached(IndirectReference reference, out ObjectToken objectToken)
        {
            return cache.TryGetValue(reference, out objectToken);
        }

        public void Cache(ObjectToken objectToken)
        {
            if (objectToken == null)
            {
                throw new ArgumentNullException();
            }

            if (cache.TryGetValue(objectToken.Number, out var existing))
            {
                var crossReference = crossReferenceTable();
                if (crossReference != null && crossReference.ObjectOffsets.TryGetValue(objectToken.Number, out var expected)
                    && existing.Position == expected)
                {
                    return;
                }
            }

            cache[objectToken.Number] = objectToken;
        }
    }
}