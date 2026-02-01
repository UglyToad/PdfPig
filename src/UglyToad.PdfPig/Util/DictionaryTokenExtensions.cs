namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// <see cref="DictionaryToken"/> extensions.
    /// </summary>
    public static class DictionaryTokenExtensions
    {
        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <returns>The token, if it is found. <c>null</c> otherwise.</returns>
        public static IToken? GetObjectOrDefault(this DictionaryToken dictionaryToken, NameToken name)
        {
            if (dictionaryToken.TryGet(name, out var obj))
            {
                return obj;
            }

            return null;
        }

        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="first">The name of the entry to retrieve first.</param>
        /// <param name="second">The name of the entry to retrieve if the first one is not found.</param>
        /// <returns>The token, if it is found. <c>null</c> otherwise.</returns>
        public static IToken? GetObjectOrDefault(this DictionaryToken dictionaryToken, NameToken first, NameToken second)
        {
            if (dictionaryToken.TryGet(first, out var obj))
            {
                return obj;
            }

            if (dictionaryToken.TryGet(second, out obj))
            {
                return obj;
            }

            return null;
        }

        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <returns>The <see langword="int"/> token.</returns>
        public static int GetInt(this DictionaryToken dictionaryToken, NameToken name)
        {
            if (dictionaryToken is null)
            {
                throw new ArgumentNullException(nameof(dictionaryToken));
            }

            var numeric = dictionaryToken.GetObjectOrDefault(name) as NumericToken;

            return numeric?.Int ?? throw new PdfDocumentFormatException($"The dictionary did not contain a number with the key {name}. Dictionary way: {dictionaryToken}.");
        }

        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <param name="defaultValue">The default value to return if entry is not found.</param>
        /// <returns>The <see langword="int"/> token, if it is found. The default value otherwise.</returns>
        public static int GetIntOrDefault(this DictionaryToken dictionaryToken, NameToken name, int defaultValue = 0)
        {
            if (dictionaryToken is null)
            {
                throw new ArgumentNullException(nameof(dictionaryToken));
            }

            var numeric = dictionaryToken.GetObjectOrDefault(name) as NumericToken;

            return numeric?.Int ?? defaultValue;
        }

        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="first">The name of the entry to retrieve first.</param>
        /// <param name="second">The name of the entry to retrieve if the first one is not found.</param>
        /// <param name="defaultValue">The default value to return if entry is not found.</param>
        /// <returns>The <see langword="int"/> token, if it is found. The default value otherwise.</returns>
        public static int GetIntOrDefault(this DictionaryToken dictionaryToken, NameToken first, NameToken second, int defaultValue = 0)
        {
            if (dictionaryToken is null)
            {
                throw new ArgumentNullException(nameof(dictionaryToken));
            }

            var numeric = dictionaryToken.GetObjectOrDefault(first, second) as NumericToken;

            return numeric?.Int ?? default;
        }

        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <returns>The token, if it is found. <c>null</c> otherwise.</returns>
        public static long? GetLongOrDefault(this DictionaryToken dictionaryToken, NameToken name)
        {
            if (dictionaryToken is null)
            {
                throw new ArgumentNullException(nameof(dictionaryToken));
            }

            var numeric = dictionaryToken.GetObjectOrDefault(name) as NumericToken;

            return numeric?.Long;
        }

        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <param name="defaultValue">The default value to return if entry is not found.</param>
        /// <returns>The <see langword="bool"/> token, if it is found. The default value otherwise.</returns>
        public static bool GetBooleanOrDefault(this DictionaryToken dictionaryToken, NameToken name, bool defaultValue)
        {
            if (dictionaryToken is null)
            {
                throw new ArgumentNullException(nameof(dictionaryToken));
            }

            var boolean = dictionaryToken.GetObjectOrDefault(name) as BooleanToken;

            return boolean?.Data ?? defaultValue;
        }

        /// <summary>
        /// Get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <returns>The token, if it is found. <c>null</c> otherwise.</returns>
        public static NameToken? GetNameOrDefault(this DictionaryToken dictionaryToken, NameToken name)
        {
            if (dictionaryToken is null)
            {
                throw new ArgumentNullException(nameof(dictionaryToken));
            }

            return dictionaryToken.GetObjectOrDefault(name) as NameToken;
        }

        /// <summary>
        /// Try and get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <param name="scanner">The Pdf token scanner</param>
        /// <param name="result">The entry.</param>
        /// <returns><see langword="true"/> if the token is found, <see langword="false"/> otherwise.</returns>
        public static bool TryGetOptionalTokenDirect<T>(this DictionaryToken dictionaryToken, NameToken name, IPdfTokenScanner scanner, [NotNullWhen(true)] out T? result)
            where T : class, IToken
        {
            result = null;
            if (dictionaryToken.TryGet(name, out var appearancesToken) && DirectObjectFinder.TryGet(appearancesToken, scanner, out T? innerResult))
            {
                result = innerResult;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try and get the entry with a given name.
        /// </summary>
        /// <param name="dictionaryToken">The <see cref="DictionaryToken"/>.</param>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <param name="scanner">The Pdf token scanner.</param>
        /// <param name="result">The entry.</param>
        /// <returns><see langword="true"/> if the token is found, <see langword="false"/> otherwise.</returns>
        public static bool TryGetOptionalStringDirect(this DictionaryToken dictionaryToken, NameToken name, IPdfTokenScanner scanner, [NotNullWhen(true)] out string? result)
        {
            result = null;
            if (dictionaryToken.TryGetOptionalTokenDirect(name, scanner, out StringToken? stringToken))
            {
                result = stringToken.Data;
                return true;
            }

            if (dictionaryToken.TryGetOptionalTokenDirect(name, scanner, out HexToken? hexToken))
            {
                result = hexToken.Data;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// <see cref="ArrayTokenExtensions"/> extensions.
    /// </summary>
    public static class ArrayTokenExtensions
    {
        /// <summary>
        /// Get the numeric value at the given index.
        /// </summary>
        /// <param name="array">The <see cref="ArrayToken"/>.</param>
        /// <param name="index">The index.</param>
        public static NumericToken GetNumeric(this ArrayToken array, int index)
        {
            if (array is null)
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

        /// <summary>
        /// Converts the <see cref="ArrayToken"/> into a <see cref="PdfRectangle"/>.
        /// </summary>
        /// <param name="array">The <see cref="ArrayToken"/>.</param>
        /// <param name="tokenScanner">The Pdf token scanner.</param>
        public static PdfRectangle ToRectangle(this ArrayToken array, IPdfTokenScanner tokenScanner)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Data.Count < 4)
            {
                // Should be exactly 4, but can be more (see issues 1238). We ignore the rest.
                throw new PdfDocumentFormatException($"Cannot convert array to rectangle, expected 4 values instead got: {array.Data.Count}.");
            }

            return new PdfRectangle(
                DirectObjectFinder.Get<NumericToken>(array[0], tokenScanner).Double,
                DirectObjectFinder.Get<NumericToken>(array[1], tokenScanner).Double,
                DirectObjectFinder.Get<NumericToken>(array[2], tokenScanner).Double,
                DirectObjectFinder.Get<NumericToken>(array[3], tokenScanner).Double);
        }

        /// <summary>
        /// Converts the <see cref="ArrayToken"/> into a <see cref="PdfRectangle"/>.
        /// </summary>
        /// <param name="array">The <see cref="ArrayToken"/>.</param>
        /// <param name="tokenScanner">The Pdf token scanner.</param>
        public static PdfRectangle ToIntRectangle(this ArrayToken array, IPdfTokenScanner tokenScanner)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Data.Count != 4)
            {
                throw new PdfDocumentFormatException($"Cannot convert array to rectangle, expected 4 values instead got: {array.Data.Count}.");
            }

            return new PdfRectangle(
                DirectObjectFinder.Get<NumericToken>(array[0], tokenScanner).Int,
                DirectObjectFinder.Get<NumericToken>(array[1], tokenScanner).Int,
                DirectObjectFinder.Get<NumericToken>(array[2], tokenScanner).Int,
                DirectObjectFinder.Get<NumericToken>(array[3], tokenScanner).Int);
        }
    }
}