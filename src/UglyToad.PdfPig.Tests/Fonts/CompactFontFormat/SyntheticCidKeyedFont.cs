namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Builds a minimal CID keyed Compact Font Format program, in the shape a
    /// subsetter leaves behind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three glyphs, of which only the middle one has outlines. FDSelect assigns
    /// glyph 1 to font dictionary 1 and glyphs 0 and 2 to font dictionary 0, so
    /// the first entry is referenced but owns nothing that is drawn. A subsetter
    /// produces exactly this: the FDArray keeps its original length so the
    /// indices FDSelect yields stay valid, and every entry whose glyphs were
    /// dropped is emptied.
    /// </para>
    /// <para>
    /// Written out byte by byte rather than pasted in as a blob so that the
    /// structure a test relies on can be read here instead of guessed at. Nothing
    /// is taken from a real font.
    /// </para>
    /// </remarks>
    internal static class SyntheticCidKeyedFont
    {
        /// <summary>Custom string identifiers, which start after the 390 standard ones.</summary>
        private const int RegistrySid = 391;
        private const int OrderingSid = 392;
        private const int FontNameSid = 393;

        /// <summary>How the first FDArray entry, the one no drawn glyph uses, is written.</summary>
        public enum FirstFontDictionary
        {
            /// <summary>A zero length entry, as a subsetter leaves it.</summary>
            Empty,

            /// <summary>A complete entry, so that the unchanged path of the parser is exercised.</summary>
            WithPrivateDictionary,

            /// <summary>Present, but declaring a size of zero.</summary>
            WithZeroSizePrivateDictionary
        }

        public static byte[] Build(FirstFontDictionary first)
        {
            // The registry and ordering are carried through as strings and never
            // compared against anything, so they need not name a real character
            // collection. What makes the font CID keyed is the presence of the ROS
            // operator, not its operands.
            var nameIndex = Index(new[] { Encoding.ASCII.GetBytes("Synthetic") });
            var stringIndex = Index(new[]
            {
                Encoding.ASCII.GetBytes("Test"),
                Encoding.ASCII.GetBytes("Sample"),
                Encoding.ASCII.GetBytes("Synthetic")
            });
            var globalSubroutineIndex = Index(Array.Empty<byte[]>());

            // Every offset in the top dictionary is written as a five byte integer, so
            // the dictionary keeps its size whatever the offsets turn out to be and the
            // layout can be computed in a single pass.
            var topDictionarySize = TopDictionary(0, 0, 0, 0).Length;
            var topDictionaryIndexSize = Index(new[] { new byte[topDictionarySize] }).Length;

            var position = 4 + nameIndex.Length + topDictionaryIndexSize + stringIndex.Length
                + globalSubroutineIndex.Length;

            // Charset, format 0: one 16 bit CID per glyph after .notdef.
            var charsetOffset = position;
            var charset = new byte[] { 0x00, 0x00, 0x01, 0x00, 0x02 };
            position += charset.Length;

            // FDSelect, format 3: three ranges and the sentinel.
            var fdSelectOffset = position;
            var fdSelect = new byte[]
            {
                0x03,
                0x00, 0x03,             // three ranges
                0x00, 0x00, 0x00,       // from glyph 0, font dictionary 0
                0x00, 0x01, 0x01,       // from glyph 1, font dictionary 1
                0x00, 0x02, 0x00,       // from glyph 2, font dictionary 0
                0x00, 0x03              // sentinel
            };
            position += fdSelect.Length;

            // Only the middle glyph has outlines: 50 100 hmoveto endchar. The other two
            // are zero length, which is what the subsetter leaves for a dropped glyph.
            var charStringsOffset = position;
            var charStrings = Index(new[]
            {
                Array.Empty<byte>(),
                new byte[] { 189, 239, 22, 14 },
                Array.Empty<byte>()
            });
            position += charStrings.Length;

            // The FDArray entries carry the location of the private dictionary, so it is
            // placed last and its offset is known before the entries are written. The
            // array is built twice: once to learn its size, once with the real offset.
            var privateDictionary = new byte[] { 139, 20, 139, 21 };  // defaultWidthX 0, nominalWidthX 0

            var fdArrayOffset = position;
            var fdArraySize = Index(new[]
            {
                FirstEntry(first, privateDictionary.Length, 0),
                FontDictionary(privateDictionary.Length, 0)
            }).Length;
            position += fdArraySize;

            var privateDictionaryOffset = position;

            var fdArray = Index(new[]
            {
                FirstEntry(first, privateDictionary.Length, privateDictionaryOffset),
                FontDictionary(privateDictionary.Length, privateDictionaryOffset)
            });

            if (fdArray.Length != fdArraySize)
            {
                throw new InvalidOperationException(
                    $"The FDArray changed size once the offset was known: {fdArraySize} to {fdArray.Length}.");
            }

            var topDictionaryIndex = Index(new[]
            {
                TopDictionary(charsetOffset, charStringsOffset, fdArrayOffset, fdSelectOffset)
            });

            if (topDictionaryIndex.Length != topDictionaryIndexSize)
            {
                throw new InvalidOperationException("The top dictionary index changed size.");
            }

            var font = new List<byte>();
            font.AddRange(new byte[] { 0x01, 0x00, 0x04, 0x04 });  // major, minor, header size, offset size
            font.AddRange(nameIndex);
            font.AddRange(topDictionaryIndex);
            font.AddRange(stringIndex);
            font.AddRange(globalSubroutineIndex);
            font.AddRange(charset);
            font.AddRange(fdSelect);
            font.AddRange(charStrings);
            font.AddRange(fdArray);
            font.AddRange(privateDictionary);
            return font.ToArray();
        }

        private static byte[] FirstEntry(FirstFontDictionary first, int privateSize, int privateOffset)
        {
            switch (first)
            {
                case FirstFontDictionary.WithPrivateDictionary:
                    return FontDictionary(privateSize, privateOffset);
                case FirstFontDictionary.WithZeroSizePrivateDictionary:
                    return FontDictionary(0, privateOffset);
                default:
                    return Array.Empty<byte>();
            }
        }

        private static byte[] TopDictionary(int charsetOffset, int charStringsOffset, int fdArrayOffset,
            int fdSelectOffset)
        {
            var dictionary = new List<byte>();
            StringId(dictionary, RegistrySid);
            StringId(dictionary, OrderingSid);
            dictionary.Add(139);                          // supplement 0
            dictionary.AddRange(new byte[] { 12, 30 });   // ROS, which is what makes it CID keyed
            dictionary.Add(142);                          // CIDCount 3
            dictionary.AddRange(new byte[] { 12, 34 });
            Offset(dictionary, charsetOffset);
            dictionary.Add(15);                           // charset
            Offset(dictionary, charStringsOffset);
            dictionary.Add(17);                           // CharStrings
            Offset(dictionary, fdArrayOffset);
            dictionary.AddRange(new byte[] { 12, 36 });   // FDArray
            Offset(dictionary, fdSelectOffset);
            dictionary.AddRange(new byte[] { 12, 37 });   // FDSelect
            return dictionary.ToArray();
        }

        private static byte[] FontDictionary(int privateSize, int privateOffset)
        {
            var dictionary = new List<byte>();
            StringId(dictionary, FontNameSid);
            dictionary.AddRange(new byte[] { 12, 38 });   // FontName
            Offset(dictionary, privateSize);
            Offset(dictionary, privateOffset);
            dictionary.Add(18);                           // Private
            return dictionary.ToArray();
        }

        /// <summary>A 16 bit operand, the encoding string identifiers use.</summary>
        private static void StringId(List<byte> target, int value)
        {
            target.Add(28);
            target.Add((byte)(value >> 8));
            target.Add((byte)value);
        }

        /// <summary>
        /// A 32 bit operand, used for every offset so that a dictionary keeps its size no
        /// matter which value ends up in it.
        /// </summary>
        private static void Offset(List<byte> target, int value)
        {
            target.Add(29);
            target.Add((byte)(value >> 24));
            target.Add((byte)(value >> 16));
            target.Add((byte)(value >> 8));
            target.Add((byte)value);
        }

        /// <summary>
        /// Writes an INDEX: the count, the width of an offset, the offsets themselves
        /// which are one based, and then the entries.
        /// </summary>
        private static byte[] Index(IReadOnlyList<byte[]> entries)
        {
            if (entries.Count == 0)
            {
                return new byte[] { 0x00, 0x00 };
            }

            var offsets = new int[entries.Count + 1];
            offsets[0] = 1;
            for (var i = 0; i < entries.Count; i++)
            {
                offsets[i + 1] = offsets[i] + entries[i].Length;
            }

            var last = offsets[offsets.Length - 1];
            var offsetSize = last <= 0xFF ? 1 : last <= 0xFFFF ? 2 : last <= 0xFFFFFF ? 3 : 4;

            var result = new List<byte>
            {
                (byte)(entries.Count >> 8),
                (byte)entries.Count,
                (byte)offsetSize
            };

            foreach (var offset in offsets)
            {
                for (var shift = (offsetSize - 1) * 8; shift >= 0; shift -= 8)
                {
                    result.Add((byte)(offset >> shift));
                }
            }

            foreach (var entry in entries)
            {
                result.AddRange(entry);
            }

            return result.ToArray();
        }
    }
}
