namespace UglyToad.Pdf.Cos
{
    using System.Collections.Generic;
    using ContentStream;

    internal class CrossReferenceTablePartBuilder
    {
        private readonly Dictionary<CosObjectKey, long> objects = new Dictionary<CosObjectKey, long>();

        public long Offset { get; set; }

        public long Previous { get; set; }

        public PdfDictionary Dictionary { get; set; }

        public CrossReferenceType XRefType { get; set; }

        public void Add(long objectId, int generationNumber, long offset)
        {
            CosObjectKey objKey = new CosObjectKey(objectId, generationNumber);

            if (!objects.ContainsKey(objKey))
            {
                objects[objKey] = offset;
            }
        }

        public CrossReferenceTablePart AsCrossReferenceTablePart()
        {
            return new CrossReferenceTablePart(objects, Offset, Previous, Dictionary, XRefType);
        }
    }
}