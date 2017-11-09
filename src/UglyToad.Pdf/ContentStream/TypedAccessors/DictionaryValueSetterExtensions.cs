namespace UglyToad.Pdf.ContentStream.TypedAccessors
{
    using Cos;

    public static class DictionaryValueSetterExtensions
    {
        public static void SetLong(this ContentStreamDictionary dictionary, CosName key, long value)
        {
            var wrappedInt = CosInt.Get(value);

            dictionary.Set(key, wrappedInt);
        }

        public static void SetInt(this ContentStreamDictionary dictionary, CosName key, int value)
        {
            var wrappedInt = CosInt.Get(value);

            dictionary.Set(key, wrappedInt);
        }
    }
}
