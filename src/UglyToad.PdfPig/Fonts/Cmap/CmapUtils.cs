namespace UglyToad.PdfPig.Fonts.Cmap
{
    using System.Collections.Generic;

    internal static class CMapUtils
    {
        public static int ToInt(this IReadOnlyList<byte> data, int length)
        {
            int code = 0;
            for (int i = 0; i < length; ++i)
            {
                code <<= 8;
                code |= (data[i] & 0xFF);
            }
            return code;
        }

        public static void PutAll<TKey, TValue>(this Dictionary<TKey, TValue> target,
            IReadOnlyDictionary<TKey, TValue> source)
        {
            foreach (var pair in source)
            {
                target[pair.Key] = pair.Value;
            }
        }
    }
}
