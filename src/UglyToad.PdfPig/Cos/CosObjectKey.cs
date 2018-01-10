namespace UglyToad.PdfPig.Cos
{
    using System;

    internal class CosObjectKey : IComparable<CosObjectKey>
    {
        public long Number { get; }
        public long Generation { get; }

        /**
         * PDFObjectKey constructor comment.
         *
         * @param object The object that this key will represent.
         */
        public CosObjectKey(CosObject obj) : this(GetNumber(obj), obj.GetGenerationNumber())
        {
        }

        private static long GetNumber(CosObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.GetObjectNumber();
        }
        
        public CosObjectKey(long num, int gen)
        {
            Number = num;
            Generation = gen;
        }
        
        public override bool Equals(object obj)
        {
            CosObjectKey objToBeCompared = obj is CosObjectKey ? (CosObjectKey)obj : null;
            return objToBeCompared != null &&
                    objToBeCompared.Number == Number &&
                    objToBeCompared.Generation == Generation;
        }

        protected bool Equals(CosObjectKey other)
        {
            return Number == other.Number && Generation == other.Generation;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                return (int)((Number.GetHashCode() * 397) ^ Generation);
            }
        }


        public override string ToString()
        {
            return $"{Number} {Generation} R";
        }

        public int CompareTo(CosObjectKey other)
        {
            if (Number < other.Number)
            {
                return -1;
            }
            else if (Number > other.Number)
            {
                return 1;
            }
            else
            {
                if (Generation < other.Generation)
                {
                    return -1;
                }
                else if (Generation > other.Generation)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

    }

}
