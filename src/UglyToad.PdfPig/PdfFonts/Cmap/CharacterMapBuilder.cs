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
    internal class CharacterMapBuilder
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

        private List<CidRange> cidRanges = new List<CidRange>();
        public IReadOnlyList<CidRange> CidRanges => cidRanges;

        public Dictionary<int, string> BaseFontCharacterMap { get; } = new Dictionary<int, string>();

        public void AddBaseFontCharacter(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> value)
        {
            AddBaseFontCharacter(bytes, CreateStringFromBytes(value));
        }

        public void AddBaseFontCharacter(ReadOnlySpan<byte> bytes, string value)
        {
            var code = GetCodeFromArray(bytes);

            BaseFontCharacterMap[code] = value;
        }

        public CMap Build()
        {
            return new CMap(GetCidSystemInfo(), Type, WMode, Name, Version,
                BaseFontCharacterMap ?? new Dictionary<int, string>(),
                CodespaceRanges ?? new CodespaceRange[0],
                CidRanges ?? new CidRange[0],
                CidCharacterMappings ?? new CidCharacterMapping[0]);
        }

        private CharacterIdentifierSystemInfo GetCidSystemInfo()
        {
            if (CharacterIdentifierSystemInfo.Registry != null)
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

            if (other.BaseFontCharacterMap != null)
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
                return new T[0];
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

        private int GetCodeFromArray(ReadOnlySpan<byte> data)
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
