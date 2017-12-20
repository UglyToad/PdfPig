namespace UglyToad.Pdf.Fonts.Cmap
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Util;

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

        /// <summary>
        /// Defines the version of this CIDFont file.
        /// </summary>
        /// <remarks>
        /// Defined as optional.
        /// </remarks>
        public string Version { get; set; }

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

        public IReadOnlyList<CidRange> CidRanges { get; set; }

        public Dictionary<int, string> BaseFontCharacterMap { get; } = new Dictionary<int, string>();

        public void AddBaseFontCharacter(IReadOnlyList<byte> bytes, IReadOnlyList<byte> value)
        {
            AddBaseFontCharacter(bytes, CreateStringFromBytes(value.ToArray()));
        }

        public void AddBaseFontCharacter(IReadOnlyList<byte> bytes, string value)
        {
            var code = GetCodeFromArray(bytes, bytes.Count);

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
                return new CharacterIdentifierSystemInfo(SystemInfoBuilder.Registry, SystemInfoBuilder.Ordering, SystemInfoBuilder.Supplement);
            }

            throw new InvalidOperationException("The Character Identifier System Information was never set.");
        }

        private int GetCodeFromArray(IReadOnlyList<byte> data, int length)
        {
            int code = 0;
            for (int i = 0; i < length; i++)
            {
                code <<= 8;
                code |= (data[i] + 256) % 256;
            }
            return code;
        }

        private static string CreateStringFromBytes(byte[] bytes)
        {
            return bytes.Length == 1
                ? OtherEncodings.BytesAsLatin1String(bytes)
                : Encoding.BigEndianUnicode.GetString(bytes);
        }
    }
}
