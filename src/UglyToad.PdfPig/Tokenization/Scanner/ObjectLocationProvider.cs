namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using CrossReference;
    using Parser.Parts;
    using Tokens;

    internal class ObjectLocationProvider : IObjectLocationProvider
    {
        private readonly Dictionary<IndirectReference, ObjectToken> cache = new Dictionary<IndirectReference, ObjectToken>();

        /// <summary>
        /// Since we want to scan objects while reading the cross reference table we lazily load it when it's ready.
        /// </summary>
        private readonly Func<CrossReferenceTable?> crossReferenceTable;

        private readonly IInputBytes bytes;

        private IReadOnlyDictionary<IndirectReference, long>? bruteForcedOffsets;

        /// <summary>
        /// Indicates whether we now have a cross reference table.
        /// </summary>
        private bool loadedFromTable;

        private readonly Dictionary<IndirectReference, long> offsets = new Dictionary<IndirectReference, long>();

        public ObjectLocationProvider(Func<CrossReferenceTable?> crossReferenceTable, IInputBytes bytes)
        {
            this.crossReferenceTable = crossReferenceTable;
            this.bytes = bytes;
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

            if (bruteForcedOffsets is null)
            {
                bruteForcedOffsets = BruteForceSearcher.GetObjectLocations(bytes);
            }

            return bruteForcedOffsets.TryGetValue(reference, out offset);
        }

        public void UpdateOffset(IndirectReference reference, long offset)
        {
            offsets[reference] = offset;
        }

        public bool TryGetCached(IndirectReference reference, [NotNullWhen(true)] out ObjectToken? objectToken)
        {
            return cache.TryGetValue(reference, out objectToken);
        }

        public void Cache(ObjectToken objectToken, bool force = false)
        {
            if (objectToken is null)
            {
                throw new ArgumentNullException();
            }

            // Don't cache incorrect locations.
            var crossReference = crossReferenceTable();
            if (!force && crossReference != null && crossReference.ObjectOffsets.TryGetValue(objectToken.Number, out var expected)
                && objectToken.Position != expected)
            {
                return;
            }

            cache[objectToken.Number] = objectToken;
        }
    }
}