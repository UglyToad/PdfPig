namespace UglyToad.PdfPig.ContentStream.TypedAccessors
{
    using Cos;

    internal static class DictionaryValueSetterExtensions
    {
        public static void SetLong(this PdfDictionary dictionary, CosName key, long value)
        {
            var wrappedInt = CosInt.Get(value);

            dictionary.Set(key, wrappedInt);
        }

        public static void SetInt(this PdfDictionary dictionary, CosName key, int value)
        {
            var wrappedInt = CosInt.Get(value);

            dictionary.Set(key, wrappedInt);
        }
    }
}
