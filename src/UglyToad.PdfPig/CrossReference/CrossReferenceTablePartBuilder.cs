namespace UglyToad.PdfPig.CrossReference
{
    using System.Collections.Generic;
    using Core;
    using Tokens;

    internal class CrossReferenceTablePartBuilder
    {
        private readonly Dictionary<IndirectReference, long> objects = new Dictionary<IndirectReference, long>();

        public long Offset { get; set; }

        public long Previous { get; set; }

        public DictionaryToken Dictionary { get; set; }

        public CrossReferenceType XRefType { get; set; }

        public long? TiedToPreviousAtOffset { get; set; }

        public void Add(long objectId, int generationNumber, long offset)
        {
            IndirectReference objKey = new IndirectReference(objectId, generationNumber);

            if (!objects.ContainsKey(objKey))
            {
                objects[objKey] = offset;
            }
        }

        public CrossReferenceTablePart Build()
        {
            return new CrossReferenceTablePart(objects, Offset, Previous, Dictionary, XRefType, TiedToPreviousAtOffset);
        }
    }
}