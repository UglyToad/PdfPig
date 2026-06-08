#nullable disable

namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A mutable class used when parsing and generating a <see cref="CMap"/>.
    /// </summary>
    internal sealed class CharacterMapBuilder
    {
        /// <summary>
        ///  Defines the character collection associated CIDFont/s for this CMap.
        /// </summary>
        public CharacterIdentifierSystemInfo CharacterIdentifierSystemInfo { get; set; }

        public CharacterIdentifierSystemInfoBuilder SystemInfoBuilder { get; } = new CharacterIdentifierSystemInfoBuilder();

        /// <summary>
        /// An <see langword="int"/> that determines the writing mode for any CIDFont combined with this CMap.
        /// 0: Horizontal
        /// 1: Vertical
        /// </summary>
        /// <remarks>
        /// Defined as optional.
        /// </remarks>
        public int WMode { get; set; } = 0;

        /// <summary>
        /// The PostScript name of the CMap.
        /// </summary>
        /// <remarks>
        /// Defined as required.
        /// </remarks>
        public string Name { get; set; }

#nullable enable

        /// <summary>
        /// Defines the version of this CIDFont file.
        /// </summary>
        /// <remarks>
        /// Defined as optional.
        /// </remarks>
        public string? Version { get; set; }

#nullable disable
        /// <summary>
        /// Defines changes to the internal structure of Character Map files
        /// or operator semantics.
        /// </summary>
        /// <remarks>
        /// Defined as required.
        /// </remarks>
        public int Type { get; set; } = -1;

        public IReadOnlyList<CodespaceRange> CodespaceRanges { get; set; }

        public IReadOnlyList<CidCharacterMapping> CidCharacterMappings { get; set; }

        private readonly List<CidRange> cidRanges = new List<CidRange>();
        public IReadOnlyList<CidRange> CidRanges => cidRanges;

        public Dictionary<int, string> BaseFontCharacterMap { get; } = new Dictionary<int, string>();

        public void AddBaseFontCharacter(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> value)
        {
            AddBaseFontCharacter(bytes, CreateStringFromBytes(value));
        }

        public void AddBaseFontCharacter(ReadOnlySpan<byte> bytes, string value)
        {
            var code = GetCodeFromArray(bytes);

            // Some (malformed) CMaps define the same code multiple times with different destinations.
            // Mirror PdfBox semantics, which key the ToUnicode mappings by code byte-length:
            //  - A shorter code wins over a longer one that collapses to the same integer key. This is
            //    the GHOSTSCRIPT-699178-0 case where the 2-byte <0020> -> 'X' must not override the
            //    1-byte <20> -> ' ' (otherwise an 'X' is rendered for every space on the page).
            //  - For codes of equal byte-length the first mapping encountered takes precedence
            //    (see GitHub issue #1309).
            //
            // NOTE: We could also store the byte lengths, but the code below works with the 2 caveats:
            // - Two genuine duplicates of the same length with no leading zero (e.g. <4142> defined twice) → stateless gives last-wins (matches pdfbox) instead of first-wins.
            // - Two padded codes of different lengths colliding (e.g. <0020> vs <000020>) → stateless keeps the first rather than the strictly-shortest.

            if (bytes.Length > 1 && bytes[0] == 0) // zero padded
            {
                // Longer / leading-zero code: keep whatever is already mapped, only fill if absent.
#if NET || NETSTANDARD2_1_OR_GREATER
                BaseFontCharacterMap.TryAdd(code, value);
#else
                if (!BaseFontCharacterMap.ContainsKey(code))
                {
                    BaseFontCharacterMap[code] = value;
                }
#endif
            }
            else
            {
                // Shortest representation of the value: it wins.
                BaseFontCharacterMap[code] = value;
            }
        }

        public CMap Build()
        {
#if NET
            BaseFontCharacterMap?.TrimExcess();
#endif

            return new CMap(GetCidSystemInfo(), Type, WMode, Name, Version,
                BaseFontCharacterMap ?? new Dictionary<int, string>(),
                CodespaceRanges ?? Array.Empty<CodespaceRange>(),
                CidRanges ?? Array.Empty<CidRange>(),
                CidCharacterMappings ?? Array.Empty<CidCharacterMapping>());
        }

        private CharacterIdentifierSystemInfo GetCidSystemInfo()
        {
            if (CharacterIdentifierSystemInfo.Registry is not null)
            {
                return CharacterIdentifierSystemInfo;
            }

            if (SystemInfoBuilder.HasOrdering && SystemInfoBuilder.HasRegistry && SystemInfoBuilder.HasSupplement)
            {
                return new CharacterIdentifierSystemInfo(SystemInfoBuilder.Registry!, SystemInfoBuilder.Ordering!, SystemInfoBuilder.Supplement);
            }

            return CharacterIdentifierSystemInfo;
        }

        public void UseCMap(CMap other)
        {
            CodespaceRanges = Combine(CodespaceRanges, other.CodespaceRanges);
            CidCharacterMappings = Combine(CidCharacterMappings, other.CidCharacterMappings.Values.ToList());
            cidRanges.AddRange(other.CidRanges);

            if (other.BaseFontCharacterMap is not null)
            {
                foreach (var keyValuePair in other.BaseFontCharacterMap)
                {
                    BaseFontCharacterMap[keyValuePair.Key] = keyValuePair.Value;
                }
            }
        }

        private static IReadOnlyList<T> Combine<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            if (a is null && b is null)
            {
                return Array.Empty<T>();
            }

            if (a is null)
            {
                return b;
            }

            if (b is null)
            {
                return a;
            }

            return [.. a, .. b];
        }

        private static int GetCodeFromArray(ReadOnlySpan<byte> data)
        {
            int code = 0;
            for (int i = 0; i < data.Length; i++)
            {
                code <<= 8;
                code |= (data[i] + 256) % 256;
            }
            return code;
        }

        private static string CreateStringFromBytes(ReadOnlySpan<byte> bytes)
        {
            return bytes.Length == 1
                ? OtherEncodings.BytesAsLatin1String(bytes)
                : Encoding.BigEndianUnicode.GetString(bytes);
        }

        public void AddCidRange(CidRange range)
        {
            cidRanges.Add(range);
        }
    }
}
