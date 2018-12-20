namespace UglyToad.PdfPig.Util
{
    using System;
    using Exceptions;
    using Geometry;
    using JetBrains.Annotations;
    using Tokens;

    internal static class DictionaryTokenExtensions
    {
        public static int GetInt(this DictionaryToken token, NameToken name)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!token.TryGet(name, out var keyedToken) || !(keyedToken is NumericToken numeric))
            {
                throw new PdfDocumentFormatException($"The dictionary did not contain a number with the key {name}. Dictionary way: {token}.");
            }

            return numeric.Int;
        }

        public static int GetIntOrDefault(this DictionaryToken token, NameToken name, int defaultValue = 0)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!token.TryGet(name, out var keyedToken) || !(keyedToken is NumericToken numeric))
            {
                return defaultValue;
            }

            return numeric.Int;
        }

        public static long? GetLongOrDefault(this DictionaryToken token, NameToken name)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!token.TryGet(name, out var keyedToken) || !(keyedToken is NumericToken numeric))
            {
                return null;
            }

            return numeric.Long;
        }

        [CanBeNull]
        public static NameToken GetNameOrDefault(this DictionaryToken token, NameToken name)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!token.TryGet(name, out var nameToken) || !(nameToken is NameToken result))
            {
                return null;
            }

            return result;
        }
    }

    internal static class ArrayTokenExtensions
    {
        public static NumericToken GetNumeric(this ArrayToken array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0 || index >= array.Data.Count)
            {
                throw new ArgumentOutOfRangeException($"Cannot index into array at index {index}. Array was: {array}.");
            }

            if (array.Data[index] is NumericToken numeric)
            {
                return numeric;
            }

            throw new PdfDocumentFormatException($"The array did not contain a number at index {index}. Array was: {array}.");
        }

        public static PdfRectangle ToRectangle(this ArrayToken array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Data.Count != 4)
            {
                throw new PdfDocumentFormatException($"Cannot convert array to rectangle, expected 4 values instead got: {array}.");
            }

            return new PdfRectangle(array.GetNumeric(0).Data,
                array.GetNumeric(1).Data,
                array.GetNumeric(2).Data,
                array.GetNumeric(3).Data);
        }

        public static PdfRectangle ToIntRectangle(this ArrayToken array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Data.Count != 4)
            {
                throw new PdfDocumentFormatException($"Cannot convert array to rectangle, expected 4 values instead got: {array}.");
            }

            return new PdfRectangle(array.GetNumeric(0).Int,
                array.GetNumeric(1).Int,
                array.GetNumeric(2).Int,
                array.GetNumeric(3).Int);
        }
    }
}
