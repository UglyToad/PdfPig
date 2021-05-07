namespace UglyToad.PdfPig.Util
{
    using System;
    using Core;
    using JetBrains.Annotations;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class DictionaryTokenExtensions
    {
        [CanBeNull]
        public static IToken GetObjectOrDefault(this DictionaryToken token, NameToken name)
        {
            if (token.TryGet(name, out var obj))
            {
                return obj;
            }

            return null;
        }

        [CanBeNull]
        public static IToken GetObjectOrDefault(this DictionaryToken token, NameToken first, NameToken second)
        {
            if (token.TryGet(first, out var obj))
            {
                return obj;
            }

            if (token.TryGet(second, out obj))
            {
                return obj;
            }

            return null;
        }

        public static int GetInt(this DictionaryToken token, NameToken name)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var numeric = token.GetObjectOrDefault(name) as NumericToken;

            return numeric?.Int ?? throw new PdfDocumentFormatException($"The dictionary did not contain a number with the key {name}. Dictionary way: {token}.");
        }

        public static int GetIntOrDefault(this DictionaryToken token, NameToken name, int defaultValue = 0)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var numeric = token.GetObjectOrDefault(name) as NumericToken;

            return numeric?.Int ?? defaultValue;
        }

        public static int GetIntOrDefault(this DictionaryToken token, NameToken first, NameToken second, int defaultValue = 0)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var numeric = token.GetObjectOrDefault(first, second) as NumericToken;

            return numeric?.Int ?? default;
        }

        public static long? GetLongOrDefault(this DictionaryToken token, NameToken name)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var numeric = token.GetObjectOrDefault(name) as NumericToken;

            return numeric?.Long;
        }

        public static bool GetBooleanOrDefault(this DictionaryToken token, NameToken name, bool defaultValue)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var boolean = token.GetObjectOrDefault(name) as BooleanToken;

            return boolean?.Data ?? defaultValue;
        }

        [CanBeNull]
        public static NameToken GetNameOrDefault(this DictionaryToken token, NameToken name)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return token.GetObjectOrDefault(name) as NameToken;
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

        public static PdfRectangle ToRectangle(this ArrayToken array, IPdfTokenScanner tokenScanner)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Data.Count != 4)
            {
                throw new PdfDocumentFormatException($"Cannot convert array to rectangle, expected 4 values instead got: {array}.");
            }

            return new PdfRectangle(DirectObjectFinder.Get<NumericToken>(array[0], tokenScanner).Double,
                DirectObjectFinder.Get<NumericToken>(array[1], tokenScanner).Double,
                DirectObjectFinder.Get<NumericToken>(array[2], tokenScanner).Double,
                DirectObjectFinder.Get<NumericToken>(array[3], tokenScanner).Double);
        }

        public static PdfRectangle ToIntRectangle(this ArrayToken array, IPdfTokenScanner tokenScanner)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Data.Count != 4)
            {
                throw new PdfDocumentFormatException($"Cannot convert array to rectangle, expected 4 values instead got: {array}.");
            }

            return new PdfRectangle(DirectObjectFinder.Get<NumericToken>(array[0], tokenScanner).Int,
                DirectObjectFinder.Get<NumericToken>(array[1], tokenScanner).Int,
                DirectObjectFinder.Get<NumericToken>(array[2], tokenScanner).Int,
                DirectObjectFinder.Get<NumericToken>(array[3], tokenScanner).Int);
        }
    }
}
