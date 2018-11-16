namespace UglyToad.PdfPig.Cos
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Tokens;
    using Util.JetBrains.Annotations;

    internal class CrossReferenceTable
    {
        public CrossReferenceType Type { get; }

        private readonly Dictionary<IndirectReference, long> objectOffsets;

        [NotNull]
        public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets => objectOffsets;

        [NotNull]
        public DictionaryToken Dictionary { get; }

        public CrossReferenceTable(CrossReferenceType type, IReadOnlyDictionary<IndirectReference, long> objectOffsets, DictionaryToken dictionary)
        {
            if (objectOffsets == null)
            {
                throw new ArgumentNullException(nameof(objectOffsets));
            }

            Type = type;
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

            var result = new Dictionary<IndirectReference, long>();
            foreach (var objectOffset in objectOffsets)
            {
                result[objectOffset.Key] = objectOffset.Value;
            }

            this.objectOffsets = result;
        }

        public void UpdateOffset(IndirectReference key, long offset)
        {
            objectOffsets[key] = offset;
        }
    }
}
