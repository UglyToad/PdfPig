namespace UglyToad.PdfPig.Cos
{
    using System.Collections.Generic;
    using ContentStream;

    internal class CosObjectPool
    {
        private readonly Dictionary<IndirectReference, CosObject> objects = new Dictionary<IndirectReference, CosObject>();

        public CosObject Get(IndirectReference key)
        {
            if (objects.TryGetValue(key, out var value))
            {
                return value;
            }

            // this was a forward reference, make "proxy" object
            var obj = new CosObject(null);
            obj.SetObjectNumber(key.ObjectNumber);
            obj.SetGenerationNumber(key.Generation);
            objects[key] = obj;

            return obj;
        }

        public CosObject GetOrCreateDefault(IndirectReference key)
        {
            if (!objects.TryGetValue(key, out CosObject obj))
            {
                obj = new CosObject(null);
            }

            return obj;
        }
    }
}
