namespace UglyToad.Pdf.Fonts.Cmap
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IO;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The CMap (character code map) maps character codes to character identifiers (CIDs).
    /// The set of characters which a CMap refers to is the "character set" (charset).
    /// </summary>
    internal class CMap
    {
        public CharacterIdentifierSystemInfo Info { get; }

        /// <summary>
        /// Defines the type of the internal organization of the CMap file.
        /// </summary>
        public int Type { get; }

        /// <summary>
        /// Defines the name of the CMap file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The version number of the CIDFont file.
        /// </summary>
        [CanBeNull]
        public string Version { get; }

        [NotNull]
        public IReadOnlyDictionary<int, string> BaseFontCharacterMap { get; }

        /// <summary>
        /// Describes the set of valid input character codes.
        /// </summary>
        [NotNull]
        public IReadOnlyList<CodespaceRange> CodespaceRanges { get; }

        [NotNull]
        public IReadOnlyList<CidRange> CidRanges { get; }

        [NotNull]
        public IReadOnlyList<CidCharacterMapping> CidCharacterMappings { get; }

        /// <summary>
        /// Controls whether the font associated with the CMap writes horizontally or vertically.
        /// </summary>
        public WritingMode WritingMode { get; }

        public bool HasCidMappings => CidCharacterMappings.Count > 0 || CidRanges.Count > 0;

        public bool HasUnicodeMappings => BaseFontCharacterMap.Count > 0;

        private readonly int minCodeLength = 4;
        private readonly int maxCodeLength;

        public CMap(CharacterIdentifierSystemInfo info, int type, int wMode, string name, string version, IReadOnlyDictionary<int, string> baseFontCharacterMap, IReadOnlyList<CodespaceRange> codespaceRanges, IReadOnlyList<CidRange> cidRanges, IReadOnlyList<CidCharacterMapping> cidCharacterMappings)
        {
            Info = info;
            Type = type;
            WritingMode = (WritingMode)wMode;
            Name = name;
            Version = version;
            BaseFontCharacterMap = baseFontCharacterMap ?? throw new ArgumentNullException(nameof(baseFontCharacterMap));
            CodespaceRanges = codespaceRanges ?? throw new ArgumentNullException(nameof(codespaceRanges));
            CidRanges = cidRanges ?? throw new ArgumentNullException(nameof(cidRanges));
            CidCharacterMappings = cidCharacterMappings ?? throw new ArgumentNullException(nameof(cidCharacterMappings));
            maxCodeLength = CodespaceRanges.Max(x => x.CodeLength);
            minCodeLength = CodespaceRanges.Min(x => x.CodeLength);
        }
        
        // CID mappings
        private readonly Dictionary<int, int> codeToCid = new Dictionary<int, int>();
        private readonly List<CidRange> codeToCidRanges = new List<CidRange>();

        private static readonly string SPACE = " ";
        private int spaceMapping = -1;

        /// <summary>
        /// Returns the sequence of Unicode characters for the given character code.
        /// </summary>
        /// <param name="code">Character code</param>
        /// <param name="result">Unicode characters(may be more than one, e.g "fi" ligature)</param>
        /// <returns><see langword="true"/> if this character map contains an entry for this code, <see langword="false"/> otherwise.</returns>
        public bool TryConvertToUnicode(int code, out string result)
        {
            var found = BaseFontCharacterMap.TryGetValue(code, out result);

            return found;
        }

        /**
         * Returns the CID for the given character code.
         *
         * @param code character code
         * @return CID
         */
        public int ConvertToCid(int code)
        {
            if (codeToCid.TryGetValue(code, out var cid))
            {
                return cid;
            }

            foreach (CidRange range in codeToCidRanges)
            {
                int ch = range.Map((char)code);
                if (ch != -1)
                {
                    return ch;
                }
            }

            return 0;
        }
        
        
        public override string ToString()
        {
            return Name;
        }

        public int ReadCode(IInputBytes bytes)
        {
            byte[] result = new byte[maxCodeLength];

            result[0] = bytes.CurrentByte;

            for (int i = 1; i < minCodeLength; i++)
            {
                result[i] = ReadByte(bytes);
            }

            for (int i = minCodeLength - 1; i < maxCodeLength; i++)
            {
                int byteCount = i + 1;
                foreach (CodespaceRange range in CodespaceRanges)
                {
                    if (range.IsFullMatch(result, byteCount))
                    {
                        return ByteArrayToInt(result, byteCount);
                    }
                }
                if (byteCount < maxCodeLength)
                {
                    result[byteCount] = ReadByte(bytes);
                }
            }

            throw new InvalidOperationException("CMap is invalid");
        }

        private static byte ReadByte(IInputBytes bytes)
        {
            if (!bytes.MoveNext())
            {
                throw new InvalidOperationException("Read byte called on input bytes which was at end of byte set. Current offset: " + bytes.CurrentOffset);
            }

            return bytes.CurrentByte;
        }

        private static int ByteArrayToInt(byte[] data, int dataLen)
        {
            int code = 0;
            for (int i = 0; i < dataLen; ++i)
            {
                code <<= 8;
                code |= (data[i] & 0xFF);
            }
            return code;
        }

    }
}
