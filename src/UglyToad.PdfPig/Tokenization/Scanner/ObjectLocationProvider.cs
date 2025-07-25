namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Parser.Parts;
    using Tokens;

    internal class ObjectLocationProvider : IObjectLocationProvider
    {
        private readonly Dictionary<IndirectReference, ObjectToken> cache = new Dictionary<IndirectReference, ObjectToken>();

        private readonly IInputBytes bytes;

        private IReadOnlyDictionary<IndirectReference, long>? bruteForcedOffsets;

        private readonly Dictionary<IndirectReference, long> offsets;

        public ObjectLocationProvider(
            IReadOnlyDictionary<IndirectReference, long> xrefOffsets,
            IReadOnlyDictionary<IndirectReference, long>? bruteForcedOffsets,
            IInputBytes bytes)
        {
            offsets = new Dictionary<IndirectReference, long>();
            foreach (var xrefOffset in xrefOffsets)
            {
                offsets[xrefOffset.Key] = xrefOffset.Value;
            }

            this.bruteForcedOffsets = bruteForcedOffsets;
            this.bytes = bytes;
        }

        public bool TryGetOffset(IndirectReference reference, out long offset)
        {
            if (offsets.TryGetValue(reference, out offset))
            {
                if (offset + reference.ObjectNumber == 0)
                {
                    // We have a case where 'offset' and
                    // 'reference.ObjectNumber' have the same value
                    // and opposite signs.
                    // This results in an infinite recursion in
                    // PdfTokenScanner.GetObjectFromStream() where 
                    // `var streamObjectNumber = offset * -1;`
                    throw new PdfDocumentFormatException("Avoiding infinite recursion in ObjectLocationProvider.TryGetOffset() as 'offset' and 'reference.ObjectNumber' have the same value and opposite signs.");
                }
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
                throw new ArgumentNullException(nameof(objectToken));
            }

            // Don't cache incorrect locations.
            if (!force && offsets.TryGetValue(objectToken.Number, out var expected)
                && objectToken.Position != expected)
            {
                return;
            }

            cache[objectToken.Number] = objectToken;
        }
    }
}