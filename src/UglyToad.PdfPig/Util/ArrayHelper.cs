namespace UglyToad.PdfPig.Util
{
    using System;

    internal static class ArrayHelper
    {
        public static void Fill<T>(T[] array, T value)
        {
            Fill(array, 0, array.Length - 1, value);
        }

        public static void Fill<T>(T[] array, int start, int end, T value)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (start < 0 || start >= end)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (end >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            for (int i = start; i < end; i++)
            {
                array[i] = value;
            }
        }
    }
}
