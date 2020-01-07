namespace UglyToad.PdfPig.Util
{
    using System;
    using Core;
    using Exceptions;
    using Geometry;
    using JetBrains.Annotations;
    using Parser.Parts;
    using Tokenization.Scanner;
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

        public static bool TryGetOptionalTokenDirect<T>(this DictionaryToken token, NameToken name, IPdfTokenScanner scanner, out T result) where T : IToken
        {
            result = default(T);
            if (token.TryGet(name, out var appearancesToken) && DirectObjectFinder.TryGet(appearancesToken, scanner, out T innerResult))
            {
                result = innerResult;
                return true;
            }

            return false;
        }

        public static bool TryGetOptionalStringDirect(this DictionaryToken token, NameToken name, IPdfTokenScanner scanner, out string result)
        {
            result = default(string);
            if (token.TryGetOptionalTokenDirect(name, scanner, out StringToken stringToken))
            {
                result = stringToken.Data;
                return true;
            }

            if (token.TryGetOptionalTokenDirect(name, scanner, out HexToken hexToken))
            {
                result = hexToken.Data;
                return true;
            }

            return false;
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

            return new PdfRectangle(array.GetNumeric(0).Double,
                array.GetNumeric(1).Double,
                array.GetNumeric(2).Double,
                array.GetNumeric(3).Double);
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
