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

        private IReadOnlyDictionary<IndirectReference, XrefLocation>? bruteForcedOffsets;

        private readonly Dictionary<IndirectReference, XrefLocation> offsets;

        public ObjectLocationProvider(
            IReadOnlyDictionary<IndirectReference, XrefLocation> xrefOffsets,
            IReadOnlyDictionary<IndirectReference, XrefLocation>? bruteForcedOffsets,
            IInputBytes bytes)
        {
            offsets = new Dictionary<IndirectReference, XrefLocation>();
            foreach (var xrefOffset in xrefOffsets)
            {
                offsets[xrefOffset.Key] = xrefOffset.Value;
            }

            this.bruteForcedOffsets = bruteForcedOffsets;
            this.bytes = bytes;
        }

        public bool TryGetOffset(IndirectReference reference, out XrefLocation offset)
        {
            if (bruteForcedOffsets != null && bruteForcedOffsets.TryGetValue(reference, out var bfOffset))
            {
                offset = bfOffset;
                return true;
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

        public void UpdateOffset(IndirectReference reference, XrefLocation offset)
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
            if (!force
                && offsets.TryGetValue(objectToken.Number, out var expected)
                && (objectToken.Position.Type != expected.Type || objectToken.Position.Value1 != expected.Value1))
            {
                return;
            }

            cache[objectToken.Number] = objectToken;
        }
    }
}