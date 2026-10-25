namespace UglyToad.PdfPig.Tests.Fonts
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Fonts.CompactFontFormat;
    using UglyToad.PdfPig.Fonts.TrueType.Subsetting;

    /// <summary>
    /// Small TrueType and OpenType programs built from the test fonts, for cases no test font covers: a given cmap,
    /// PostScript name, CFF outlines or font collection.
    /// </summary>
    internal static class TestFontPrograms
    {
        public static byte[] Roboto => File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType", "Roboto-Regular.ttf"));

        public static byte[] MinionPro => File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "CompactFontFormat", "MinionPro.bin"));

        /// <summary>
        /// Roboto cut down to its <paramref name="glyph"/> glyph, as glyph 1, with no post table and a cmap whose only
        /// entry maps <paramref name="code"/> to it: a (3,1) Unicode cmap if <paramref name="unicode"/>, else a (3,0)
        /// symbol one. With <paramref name="postScriptName"/>, its name table holds only that PostScript name.
        /// </summary>
        public static byte[] SingleGlyphFont(int code, bool unicode, char glyph = 'a', string? postScriptName = null)
        {
            var font = TrueTypeSubsetter.Subset(Roboto, new TrueTypeSubsetEncoding(new[] { glyph }));
            font = ReplaceTable(font, "cmap", Format4Cmap(code, 1, unicode));
            if (postScriptName is not null)
            {
                font = ReplaceTable(font, "name", PostScriptNameTable(postScriptName));
            }

            return font;
        }

        /// <summary>
        /// An OpenType font with PostScript outlines ("OTTO"): the <see cref="SingleGlyphFont"/> tables with MinionPro's
        /// CFF program in place of glyf, and a cmap mapping <paramref name="code"/> to MinionPro's /A: a (3,1) Unicode
        /// cmap if <paramref name="unicode"/>, else a (3,0) symbol one.
        /// </summary>
        public static byte[] OpenTypeCffFont(int code, bool unicode = true)
        {
            var minionPro = MinionPro;
            Assert.True(CompactFontFormatParser.Parse(new CompactFontFormatData(minionPro)).FirstFont.TryGetGlyphIndexByName("A", out int glyphA));

            var font = TrueTypeSubsetter.Subset(Roboto, new TrueTypeSubsetEncoding(new[] { 'a' }));
            font = ReplaceTable(font, "glyf", minionPro, "CFF ");
            font = ReplaceTable(font, "cmap", Format4Cmap(code, glyphA, unicode));
            "OTTO"u8.CopyTo(font);

            return font;
        }

        /// <summary>
        /// A TrueType collection ("ttcf") of <paramref name="faces"/>, in order.
        /// </summary>
        public static byte[] FontCollection(params byte[][] faces)
        {
            // Header: tag, version 1.0, number of fonts, then each font's offset; faces follow, 4-byte aligned.
            int headerLength = 12 + (4 * faces.Length);
            var faceStarts = new int[faces.Length];
            int length = headerLength;
            for (int f = 0; f < faces.Length; f++)
            {
                faceStarts[f] = Align(length);
                length = faceStarts[f] + faces[f].Length;
            }

            var collection = new byte[length];
            var span = collection.AsSpan();
            "ttcf"u8.CopyTo(span);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4), 0x00010000);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(8), (uint)faces.Length);

            for (int f = 0; f < faces.Length; f++)
            {
                int start = faceStarts[f];
                BinaryPrimitives.WriteUInt32BigEndian(span.Slice(12 + (4 * f)), (uint)start);
                faces[f].CopyTo(collection, start);

                // Table offsets in a collection are from the start of the file.
                int numTables = BinaryPrimitives.ReadUInt16BigEndian(faces[f].AsSpan(4));
                for (int t = 0; t < numTables; t++)
                {
                    var offset = span.Slice(start + 12 + (16 * t) + 8);
                    BinaryPrimitives.WriteUInt32BigEndian(offset, BinaryPrimitives.ReadUInt32BigEndian(offset) + (uint)start);
                }
            }

            return collection;
        }

        /// <summary>
        /// <paramref name="font"/> with <paramref name="table"/> appended and the directory entry for
        /// <paramref name="tag"/> pointing at it, renamed to <paramref name="newTag"/> if given.
        /// </summary>
        public static byte[] ReplaceTable(byte[] font, string tag, byte[] table, string? newTag = null)
        {
            int tableOffset = Align(font.Length);
            var result = new byte[tableOffset + table.Length];
            font.CopyTo(result, 0);
            table.CopyTo(result, tableOffset);

            var entry = TableRecord(result, tag);
            if (newTag is not null)
            {
                Encoding.ASCII.GetBytes(newTag).CopyTo(entry);
            }

            BinaryPrimitives.WriteUInt32BigEndian(entry.Slice(8), (uint)tableOffset);
            BinaryPrimitives.WriteUInt32BigEndian(entry.Slice(12), (uint)table.Length);
            return result;
        }

        /// <summary>
        /// The 16-byte table directory record for <paramref name="tag"/> in <paramref name="font"/>: tag, checksum,
        /// offset and length.
        /// </summary>
        public static Span<byte> TableRecord(byte[] font, string tag)
        {
            var tagBytes = Encoding.ASCII.GetBytes(tag);
            int numTables = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
            for (int i = 0; i < numTables; i++)
            {
                var entry = font.AsSpan(12 + (16 * i), 16);
                if (entry.Slice(0, 4).SequenceEqual(tagBytes.AsSpan()))
                {
                    return entry;
                }
            }

            throw new InvalidOperationException($"The font has no {tag} table.");
        }

        /// <summary>
        /// A cmap with a single platform 3 record, (3,1) or (3,0), whose format 4 subtable maps only
        /// <paramref name="code"/> to <paramref name="glyphIndex"/>.
        /// </summary>
        public static byte[] Format4Cmap(int code, int glyphIndex, bool unicode)
        {
            return BigEndian(
                // Header: version 0, one encoding record at offset 12.
                0, 1, 3, unicode ? 1 : 0, 0, 12,
                // Format 4, length 32, two segments: [code, code] -> glyphIndex and the closing [0xFFFF, 0xFFFF].
                4, 32, 0, 4, 4, 1, 0,
                code, 0xFFFF, 0, code, 0xFFFF,
                (glyphIndex - code) & 0xFFFF, 1, 0, 0);
        }

        /// <summary>
        /// A format 0 name table holding only <paramref name="postScriptName"/>, as a Windows Unicode name 6 record.
        /// </summary>
        public static byte[] PostScriptNameTable(string postScriptName)
        {
            var value = Encoding.BigEndianUnicode.GetBytes(postScriptName);
            var header = BigEndian(
                0, 1, 6 + 12,
                3, 1, 0x0409, 6, value.Length, 0);

            return header.Concat(value).ToArray();
        }

        private static byte[] BigEndian(params int[] uint16s)
        {
            var bytes = new byte[2 * uint16s.Length];
            for (int i = 0; i < uint16s.Length; i++)
            {
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2 * i), checked((ushort)uint16s[i]));
            }

            return bytes;
        }

        private static int Align(int length) => (length + 3) & ~3;
    }
}
