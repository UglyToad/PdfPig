namespace UglyToad.Pdf.Fonts.Cmap
{
    using System;
    using System.Collections.Generic;
    using Util.JetBrains.Annotations;

    public class CMap
    {
        public CharacterIdentifierSystemInfo Info { get; }

        public int Type { get; }

        public int WMode { get; }

        public string Name { get; }

        public string Version { get; }

        [NotNull]
        public IReadOnlyDictionary<int, string> BaseFontCharacterMap { get; }

        [NotNull]
        public IReadOnlyList<CodespaceRange> CodespaceRanges { get; }

        [NotNull]
        public IReadOnlyList<CidRange> CidRanges { get; }

        [NotNull]
        public IReadOnlyList<CidCharacterMapping> CidCharacterMappings { get; }

        public bool HasCidMappings => CidCharacterMappings.Count > 0 || CidRanges.Count > 0;

        public bool HasUnicodeMappings => BaseFontCharacterMap.Count > 0;

        public CMap(CharacterIdentifierSystemInfo info, int type, int wMode, string name, string version, IReadOnlyDictionary<int, string> baseFontCharacterMap, IReadOnlyList<CodespaceRange> codespaceRanges, IReadOnlyList<CidRange> cidRanges, IReadOnlyList<CidCharacterMapping> cidCharacterMappings)
        {
            Info = info;
            Type = type;
            WMode = wMode;
            Name = name;
            Version = version;
            BaseFontCharacterMap = baseFontCharacterMap ?? throw new ArgumentNullException(nameof(baseFontCharacterMap));
            CodespaceRanges = codespaceRanges ?? throw new ArgumentNullException(nameof(codespaceRanges));
            CidRanges = cidRanges ?? throw new ArgumentNullException(nameof(cidRanges));
            CidCharacterMappings = cidCharacterMappings ?? throw new ArgumentNullException(nameof(cidCharacterMappings));
        }

        private int wmode = 0;
        private string cmapName = null;
        private string cmapVersion = null;
        private int cmapType = -1;

        private string registry = null;
        private string ordering = null;
        private int supplement = 0;

        private int minCodeLength = 4;
        private int maxCodeLength;
        
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
         * Reads a character code from a string in the content stream.
         * <p>See "CMap Mapping" and "Handling Undefined Characters" in PDF32000 for more details.
         *
         * @param in string stream
         * @return character code
         * @throws IOException if there was an error reading the stream or CMap
         */
        //public int readCode(InputStream input)
        //{
        //    byte[] bytes = new byte[maxCodeLength];
        //    input.read(bytes, 0, minCodeLength);
        //    for (int i = minCodeLength - 1; i < maxCodeLength; i++)
        //    {
        //        var byteCount = i + 1;
        //        foreach (var range in codespaceRanges)
        //        {
        //            if (range.isFullMatch(bytes, byteCount))
        //            {
        //                return toInt(bytes, byteCount);
        //            }
        //        }
        //        if (byteCount < maxCodeLength)
        //        {
        //            bytes[byteCount] = (byte)input.read();
        //        }
        //    }

        //    throw new InvalidOperationException("CMap is invalid");
        //}

       
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
            return cmapName;
        }
    }

}
