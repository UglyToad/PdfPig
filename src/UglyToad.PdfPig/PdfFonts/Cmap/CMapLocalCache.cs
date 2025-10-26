namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    using Core;
    using Filters;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Provides a local (per document) cache for CMap objects, allowing efficient retrieval and storage of CMap instances based on
    /// their names and unique identifiers.
    /// </summary>
    /// <remarks>This class is designed to cache CMap objects to improve performance by avoiding redundant
    /// parsing of CMap data. It uses a combination of CMap names and GUIDs derived from the CMap data to uniquely
    /// identify and store CMap instances.</remarks>
    internal sealed class CMapLocalCache
    {
        private static ReadOnlySpan<byte> cmapNameTag => @"/CMapName "u8;
        private readonly object cacheLock = new object();

        private readonly Dictionary<string, Dictionary<Guid, CMap>> _cache = new();
        private readonly ILookupFilterProvider _filterProvider;
        private readonly IPdfTokenScanner _scanner;

        /// <summary>
        /// Provides a local (per document) cache for CMap objects, allowing efficient retrieval and storage of CMap instances based on
        /// their names and unique identifiers.
        /// </summary>
        /// <remarks>This class is designed to cache CMap objects to improve performance by avoiding redundant
        /// parsing of CMap data. It uses a combination of CMap names and GUIDs derived from the CMap data to uniquely
        /// identify and store CMap instances.</remarks>
        public CMapLocalCache(ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            _filterProvider = filterProvider;
            _scanner = scanner;
        }
        
        public bool TryGet(string name, [NotNullWhen(true)] out CMap? result)
        {
            return CMapCache.TryGet(name, out result);
        }

        private static Guid GetGuid(ReadOnlySpan<byte> bytes)
        {
            // Assumes MurmurHash3 is good enough for hashing CMap data to create unique identifiers,
            // i.e. collisions should be extremely rare.
            return new Guid(MurmurHash3.Compute_x64_128(bytes));
        }

        public bool TryGet(StreamToken token, [NotNullWhen(true)] out CMap? result)
        {
            if (token.Data.IsEmpty)
            {
                result = null;
                return false;
            }

            var decodedUnicodeCMap = token.Decode(_filterProvider, _scanner);
            
            if (!TryGetNameFast(decodedUnicodeCMap.Span, out string? cmapName))
            {
                result = CMapCache.Parse(new MemoryInputBytes(decodedUnicodeCMap));
                return true;
            }

            var guid = GetGuid(decodedUnicodeCMap.Span);

            lock (cacheLock)
            {
                if (!_cache.TryGetValue(cmapName!, out var cMaps))
                {
                    cMaps = new Dictionary<Guid, CMap>();
                    _cache[cmapName!] = cMaps;
                }

                if (cMaps.TryGetValue(guid, out result))
                {
                    return true;
                }

                result = CMapCache.Parse(new MemoryInputBytes(decodedUnicodeCMap));
                cMaps[guid] = result;
            }

            return true;
        }

        private static bool TryGetNameFast(ReadOnlySpan<byte> bytes, out string? name)
        {
            name = null;
            int nameIndex = bytes.IndexOf(cmapNameTag);

            if (nameIndex <= -1)
            {
                return false;
            }

            nameIndex += cmapNameTag.Length;

            int nameEndIndex = bytes.Slice(nameIndex).IndexOf("def"u8);

            if (nameEndIndex <= -1)
            {
                return false;
            }

            name = Encoding.UTF8.GetString(bytes.Slice(nameIndex, nameEndIndex - 1));
            return true;
        }
    }
}
