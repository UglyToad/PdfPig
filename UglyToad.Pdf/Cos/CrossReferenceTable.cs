namespace UglyToad.Pdf.Cos
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Util.JetBrains.Annotations;

    public class CrossReferenceTable
    {
        public CrossReferenceType Type { get; }

        private readonly Dictionary<CosObjectKey, long> objectOffsets;

        [NotNull]
        public IReadOnlyDictionary<CosObjectKey, long> ObjectOffsets => objectOffsets;

        [NotNull]
        public ContentStreamDictionary Dictionary { get; }

        public CrossReferenceTable(CrossReferenceType type, IReadOnlyDictionary<CosObjectKey, long> objectOffsets, ContentStreamDictionary dictionary)
        {
            if (objectOffsets == null)
            {
                throw new ArgumentNullException(nameof(objectOffsets));
            }

            Type = type;
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

            var result = new Dictionary<CosObjectKey, long>();
            foreach (var objectOffset in objectOffsets)
            {
                result[objectOffset.Key] = objectOffset.Value;
            }

            this.objectOffsets = result;
        }

        public void UpdateOffset(CosObjectKey key, long offset)
        {
            objectOffsets[key] = offset;
        }
    }
}
