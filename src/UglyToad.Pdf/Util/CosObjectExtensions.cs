namespace UglyToad.Pdf.Util
{
    using System;
    using Cos;

    internal static class CosObjectExtensions
    {
        public static COSStream ToCosStream(this CosDictionary dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var stream = new COSStream();
            foreach (var entry in dictionary.entrySet())
            {
                stream.setItem(entry.Key, entry.Value);
            }

            return stream;
        }
    }
}
