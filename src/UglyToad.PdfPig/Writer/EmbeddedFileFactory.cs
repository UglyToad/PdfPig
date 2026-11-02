namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Tokens;
    using Util;

    /// <summary>
    /// Creates the objects of an embedded file for <see cref="PdfDocumentBuilder"/> and <see cref="PdfIncrementalUpdateBuilder"/>.
    /// </summary>
    internal static class EmbeddedFileFactory
    {
        // A type and a subtype, as PDF/A-3 requires for the MIME type of an embedded file (ISO 19005-3, 6.8).
        private static readonly Regex MimeTypeRegex = new Regex(@"^[-A-Za-z0-9_+.]+/[-A-Za-z0-9_+.]+\z");

        private static ReadOnlySpan<byte> Utf16ByteOrderMark => [0xFE, 0xFF];

        /// <summary>
        /// Checks the name and the options of a file to add.
        /// </summary>
        public static void Validate(string name, EmbeddedFileOptions options)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("The name of an embedded file must not be empty.", nameof(name));
            }

            if (options.MimeType is null || !MimeTypeRegex.IsMatch(options.MimeType))
            {
                throw new ArgumentException($"The MIME type of an embedded file must be a type and a subtype, such as text/xml, but was '{options.MimeType}'.", nameof(options));
            }
        }

        /// <summary>
        /// Creates the embedded file stream with the compressed bytes, the MIME type and the parameters of the file.
        /// </summary>
        public static StreamToken CreateStream(ReadOnlyMemory<byte> bytes, EmbeddedFileOptions options)
        {
            var data = bytes.ToArray();

            // The checksum is the MD5 hash of the uncompressed file (ISO 32000-1, Table 46).
            byte[] checksum;
            using (var md5 = MD5.Create())
            {
                checksum = md5.ComputeHash(data);
            }

            // PDF/A-3 requires ModDate for associated files (Arlington PDF Model, AFEmbeddedFileParameter),
            // so without a date in the options the time the file is added is used.
            var parameters = new Dictionary<NameToken, IToken>
            {
                {NameToken.Size, new NumericToken(data.Length)},
                {NameToken.CheckSum, new HexToken(Hex.GetString(checksum).AsSpan())},
                {NameToken.ModDate, new StringToken(ToPdfDate(options.ModifiedDate ?? DateTimeOffset.Now))}
            };

            return new StreamToken(
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    {NameToken.Type, NameToken.EmbeddedFile},
                    {NameToken.Subtype, NameToken.Create(options.MimeType)},
                    {NameToken.Filter, NameToken.FlateDecode},
                    {NameToken.Params, new DictionaryToken(parameters)}
                }),
                DataCompressor.CompressBytes(data));
        }

        /// <summary>
        /// Creates the file specification of the file, which refers to the stream from both entries of EF.
        /// </summary>
        public static Dictionary<NameToken, IToken> CreateFileSpecification(StringToken name, IndirectReferenceToken stream, string? description)
        {
            var fileSpecification = new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.Filespec},
                {NameToken.F, name},
                {NameToken.Uf, name},
                {NameToken.Ef, new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    {NameToken.F, stream},
                    {NameToken.Uf, stream}
                })}
            };

            if (description is not null)
            {
                fileSpecification[NameToken.Desc] = new StringToken(description);
            }

            return fileSpecification;
        }

        /// <summary>
        /// Gets the value of AFRelationship. Each relationship is written as the name of its value, such as /Alternative.
        /// </summary>
        public static NameToken GetRelationship(AssociatedFileRelationship relationship)
        {
            return NameToken.Create(relationship.ToString());
        }

        /// <summary>
        /// Compares two keys of a name tree byte by byte, as their order requires (ISO 32000-1, 7.9.6).
        /// </summary>
        public static int CompareKeys(IToken x, IToken y)
        {
            return GetBytes(x).SequenceCompareTo(GetBytes(y));
        }

        /// <summary>
        /// Whether a key of a name tree is the name, as text or byte by byte (ISO 32000-1, 7.9.6).
        /// </summary>
        public static bool HasName(IToken key, StringToken name)
        {
            return (key is IDataToken<string> text && text.Data == name.Data)
                || (key is StringToken or HexToken && GetBytes(key).SequenceEqual(name.Bytes));
        }

        /// <summary>
        /// Whether a key of a name tree is written as UTF-16 with the byte order mark FE FF.
        /// </summary>
        public static bool IsUtf16(IToken key)
        {
            return GetBytes(key).StartsWith(Utf16ByteOrderMark);
        }

        /// <summary>
        /// Gets the name written as UTF-16 with the byte order mark FE FF.
        /// </summary>
        public static StringToken ToUtf16(StringToken name)
        {
            if (IsUtf16(name))
            {
                return name;
            }

            var text = Encoding.BigEndianUnicode.GetBytes(name.Data);
            var bytes = new byte[Utf16ByteOrderMark.Length + text.Length];
            Utf16ByteOrderMark.CopyTo(bytes);
            text.CopyTo(bytes, Utf16ByteOrderMark.Length);

            return new StringToken(bytes);
        }

        private static ReadOnlySpan<byte> GetBytes(IToken key)
        {
            return key switch
            {
                StringToken text => text.Bytes,
                HexToken hex => hex.Bytes,
                _ => ReadOnlySpan<byte>.Empty
            };
        }

        private static string ToPdfDate(DateTimeOffset date)
        {
            // D:YYYYMMDDHHmmSSOHH'mm (ISO 32000-1, 7.9.4).
            var offset = date.Offset;
            var zone = offset == TimeSpan.Zero
                ? "Z"
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1:00}'{2:00}",
                    offset < TimeSpan.Zero ? '-' : '+',
                    Math.Abs(offset.Hours),
                    Math.Abs(offset.Minutes));

            return "D:" + date.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + zone;
        }
    }
}
