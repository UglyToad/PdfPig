namespace UglyToad.Pdf.Cos
{
    using System;
    using System.Collections.Generic;

    internal class CosObjectPool
    {
        private readonly Dictionary<CosObjectKey, CosObject> objects = new Dictionary<CosObjectKey, CosObject>();

        public CosObject Get(CosObjectKey key)
        {
            if (key != null)
            {
                if (objects.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            // this was a forward reference, make "proxy" object
            var obj = new CosObject(null);
            if (key != null)
            {
                obj.SetObjectNumber(key.Number);
                obj.SetGenerationNumber((int)key.Generation);
                objects[key] = obj;
            }

            return obj;
        }

        public CosObject GetOrCreateDefault(CosObjectKey key)
        {
            if (!objects.TryGetValue(key, out CosObject obj))
            {
                obj = new CosObject(null);
            }

            return obj;
        }
    }
}
