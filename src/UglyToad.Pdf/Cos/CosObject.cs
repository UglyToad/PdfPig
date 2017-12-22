namespace UglyToad.Pdf.Cos
{
    using ContentStream;

    public class CosObject : CosBase, ICosUpdateInfo
    {
        private CosBase baseObject;
        private long objectNumber;
        private int generationNumber;

        /**
         * Constructor.
         *
         * @param object The object that this encapsulates.
         *
         */
        public CosObject(CosBase obj)
        {
            SetObject(obj);
        }

        public CosObjectKey GetObjectKey()
        {
            return new CosObjectKey(objectNumber, generationNumber);
        }

        /**
         * This will get the dictionary object in this object that has the name key and
         * if it is a pdfobjref then it will dereference that and return it.
         *
         * @param key The key to the value that we are searching for.
         *
         * @return The pdf object that matches the key.
         */
        public CosBase GetDictionaryObject(CosName key)
        {
            CosBase retval = null;
            if (baseObject is CosDictionary)
            {
                retval = ((CosDictionary)baseObject).getDictionaryObject(key);
            }
            return retval;
        }

        /**
         * This will get the dictionary object in this object that has the name key.
         *
         * @param key The key to the value that we are searching for.
         *
         * @return The pdf object that matches the key.
         */
        public CosBase GetItem(CosName key)
        {
            CosBase retval = null;
            if (baseObject is CosDictionary)
            {
                retval = ((CosDictionary)baseObject).getItem(key);
            }
            return retval;
        }

        /**
         * This will get the object that this object encapsulates.
         *
         * @return The encapsulated object.
         */
        public CosBase GetObject()
        {
            return baseObject;
        }

        /**
         * This will set the object that this object encapsulates.
         *
         * @param object The new object to encapsulate.
         */
        public void SetObject(CosBase obj)
        {
            baseObject = obj;
        }

        public override string ToString()
        {
            return "COSObject{" + objectNumber + ", " + generationNumber + "}";
        }

        /** 
         * Getter for property objectNumber.
         * @return Value of property objectNumber.
         */
        public long GetObjectNumber()
        {
            return objectNumber;
        }

        /** 
         * Setter for property objectNumber.
         * @param objectNum New value of property objectNumber.
         */
        public void SetObjectNumber(long objectNum)
        {
            objectNumber = objectNum;
        }

        /** 
         * Getter for property generationNumber.
         * @return Value of property generationNumber.
         */
        public int GetGenerationNumber()
        {
            return generationNumber;
        }

        /** 
         * Setter for property generationNumber.
         * @param generationNumberValue New value of property generationNumber.
         */
        public void SetGenerationNumber(int generationNumberValue)
        {
            generationNumber = generationNumberValue;
        }

        public override object Accept(ICosVisitor visitor)
        {
            return GetObject() != null ? GetObject().Accept(visitor) : CosNull.Null.Accept(visitor);
        }

        public bool NeedsToBeUpdated { get; set; }

        public IndirectReference ToIndirectReference()
        {
            return new IndirectReference(objectNumber, generationNumber);
        }
    }
}
